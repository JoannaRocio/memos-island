using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.UI;
using UnityEngine;

namespace MemosIsland.Race
{
    /// <summary>
    /// Escena de carrera: arma la simulación desde RaceLauncher.Pending, la dibuja, lee los controles
    /// (←→ elegir Memo · B cambiar · A habilidad) y maneja el flujo: presentación → cuenta regresiva →
    /// carrera → resultados → (captura: dar de comer) → volver al mapa.
    /// Si se le da Play directo a esta escena, corre una Copa de prueba.
    /// </summary>
    public class RaceController : MonoBehaviour
    {
        [SerializeField] RaceView view;
        [SerializeField] PixelFont font;
        [SerializeField] Sprite boxSprite;
        [SerializeField] Sprite pixelSprite;
        [SerializeField] Material uiMaterial;
        [SerializeField] Color skyColor = new Color32(0x73, 0xc4, 0xf7, 0xff);

        static readonly Dictionary<string, string> FoodNames = new()
        {
            ["bayamemo"] = "Bayamemo", ["frutilla"] = "Frutilla", ["zanahoria"] = "Zanahoria", ["zapallo"] = "Zapallo",
        };

        RaceSetup _setup;
        RaceSimulation _sim;
        RaceHud _hud;
        readonly List<RaceAI> _ais = new();
        int _cursor;
        float _cursorRepeat;
        bool _running;
        Color _previousBackground;
        Camera _camera;
        GameObject _clock;
        bool _locked;

        public void Setup(RaceView raceView, PixelFont newFont, Sprite box, Sprite pixel, Material material)
        {
            view = raceView;
            font = newFont;
            boxSprite = box;
            pixelSprite = pixel;
            uiMaterial = material;
        }

        IEnumerator Start()
        {
            var root = GameRoot.Instance;
            while (root == null)
            {
                yield return null;
                root = GameRoot.Instance;
            }
            root.Player.gameObject.SetActive(false);
            GameRoot.InputLocks++;
            _locked = true;

            _setup = RaceLauncher.Pending ?? DebugCup(root.State);
            var db = MemoDatabase.Instance;
            var track = new RaceTrack(_setup.segments.Select(s => (db.GetTerrain(s.terrainId), s.length)));
            _sim = new RaceSimulation(track, db.chart, _setup.player, _setup.rivals, _setup.IsCapture, _setup.seed);
            for (int i = 1; i < _sim.Racers.Count; i++)
                _ais.Add(new RaceAI(_sim, _sim.Racers[i], SkillFor(i)));

            view.Build(_sim);
            root.Camera.SetTarget(view.CameraAnchor);
            root.Camera.SnapNow();
            _camera = root.Camera.GetComponent<Camera>();
            _previousBackground = _camera.backgroundColor;
            _camera.backgroundColor = skyColor;

            var ui = root.Camera.transform.Find("UI");
            _clock = ui.Find("Clock")?.gameObject;
            if (_clock != null) _clock.SetActive(false); // el minimapa ocupa esa esquina
            _hud = new RaceHud(ui, _sim, font, boxSprite, pixelSprite, uiMaterial);
            _sim.Message += (r, text) => { if (r.isPlayer || r.isWild) _hud.ShowToast(text); else view.ShowPopup(r, text); };
            _sim.AbilityUsed += (r, a) =>
            {
                view.ShowPopup(r, $"¡{a.displayName}!");
                if (!r.isPlayer) _hud.ShowToast($"{r.name}: ¡{a.displayName}!");
            };
            _sim.Switched += r => { if (r.isPlayer) _cursor = r.activeIndex; };
            _cursor = 0;
            _hud.Refresh(_cursor);

            yield return Intro();
            yield return Countdown();
            _running = true;
            while (!_sim.IsOver) yield return null;
            _running = false;
            yield return new WaitForSeconds(1f);
            yield return Results();
        }

        float SkillFor(int rivalIndex) => _setup.format switch
        {
            RaceFormat.Capture => 0.5f,
            RaceFormat.Friendly => 0.4f,
            RaceFormat.Trainer => 0.65f,
            _ => 0.65f + rivalIndex * 0.1f,
        };

        void Update()
        {
            if (!_running || _sim == null) return;
            HandleInput();
            foreach (var ai in _ais) ai.Tick(Time.deltaTime);
            _sim.Advance(Time.deltaTime);
            _hud.Refresh(_cursor);
        }

        void HandleInput()
        {
            var player = _sim.Player;
            int dir = GameInput.Move.x > 0.5f ? 1 : GameInput.Move.x < -0.5f ? -1 : 0;
            if (dir == 0) _cursorRepeat = 0f;
            else if ((_cursorRepeat -= Time.deltaTime) <= 0f)
            {
                _cursor = (_cursor + dir + player.memos.Count) % player.memos.Count;
                _cursorRepeat = 0.18f;
            }

            if (GameInput.CancelPressed && player.memos.Count > 1)
            {
                if (_cursor == player.activeIndex) _hud.ShowToast("Elegí otro Memo con ← →");
                else if (player.switchCooldown > 0f) _hud.ShowToast($"Esperá {Mathf.CeilToInt(player.switchCooldown)} segundos para cambiar");
                else _sim.RequestSwitch(player, _cursor);
            }
            if (GameInput.ConfirmPressed)
            {
                if (!player.Active.AbilityReady) _hud.ShowToast("La habilidad todavía no está lista");
                else _sim.TryUseAbility(player);
            }
        }

        // ------------------------------------------------------------------ Flujo

        IEnumerator Intro()
        {
            var lines = new List<string>
            {
                _setup.title,
                "Pista: " + string.Join(", ", _sim.Track.Segments.Select(s => s.terrain.displayName)),
            };
            if (!_setup.IsCapture)
                lines.Add("Rivales: " + string.Join(", ", _sim.Racers.Skip(1).Select(r => r.name)));
            lines.Add(_setup.IsCapture ? "Ganale o cansalo para que confíe en vos." : "← → elegir · B cambiar · A habilidad");
            lines.Add("A: ¡A correr!");
            _hud.SetPanel(string.Join("\n", lines));
            yield return WaitConfirm();
            _hud.SetPanel(null);
        }

        IEnumerator Countdown()
        {
            foreach (var text in new[] { "3", "2", "1", "¡YA!" })
            {
                _hud.SetPanel(text);
                yield return new WaitForSeconds(0.6f);
            }
            _hud.SetPanel(null);
        }

        IEnumerator Results()
        {
            var result = new RaceResult
            {
                format = _setup.format,
                playerWon = _sim.PlayerWon,
                playerRank = _sim.Player.rank,
                racers = _sim.Racers.Count,
            };

            if (_setup.IsCapture)
            {
                yield return CaptureEnding(result);
            }
            else
            {
                var lines = _sim.Racers.OrderBy(r => r.rank).Select(r => $"{r.rank}º {r.name}").ToList();
                lines.Insert(0, result.playerWon ? "¡Ganaste!" : $"Llegaste {result.playerRank}º");
                if (_sim.Player.AllMemosRan && _sim.Player.memos.Count > 1)
                    lines.Add("¡Equipo completo! Corrieron todos.");
                lines.Add("A: continuar");
                _hud.SetPanel(string.Join("\n", lines));
                yield return WaitConfirm();
                _hud.SetPanel(null);
            }
            Finish(result);
        }

        IEnumerator CaptureEnding(RaceResult result)
        {
            var wildRacer = _sim.Racers[1];
            var wild = wildRacer.Active.instance;
            var dialogue = GameRoot.Instance.Dialogue;

            if (!result.playerWon)
            {
                yield return Say($"{wild.DisplayName} llegó primero y se perdió entre el pasto…");
                yield break;
            }

            yield return Say(wildRacer.gaveUp
                ? $"¡{wild.DisplayName} está agotado! Te mira con desconfianza…"
                : $"¡Le ganaste a {wild.DisplayName}! Respira agitado y te mira…");

            var foods = wild.Species.favoriteFoods.Where(FoodNames.ContainsKey).Select(f => FoodNames[f]).ToList();
            var options = new List<string>(foods) { "Comida básica", "Nada" };
            int chosen = -1;
            dialogue.ShowChoice($"¿Qué le das de comer a {wild.DisplayName}?", options, i => chosen = i, options.Count - 1);
            while (chosen < 0) yield return null;

            float acceptChance = chosen < foods.Count ? 0.9f : chosen == foods.Count ? 0.6f : 0f;
            if (_sim.NextRandom() < acceptChance)
            {
                // Los rescatados de un collar llegan con miedo; los salvajes, con desconfianza (GDD §10).
                wild.trust = wild.collared ? 50 : 150;
                wild.collared = false;
                bool toTeam = GameRoot.Instance.State.AddMemo(wild);
                result.captured = wild;
                string liked = chosen < foods.Count ? " ¡Le encantó!" : "";
                yield return Say($"¡{wild.DisplayName} aceptó la comida!{liked} " +
                                 (toTeam ? "Se unió a tu equipo." : "Te espera en el refugio."));
            }
            else
            {
                yield return Say(chosen == options.Count - 1
                    ? $"{wild.DisplayName} se alejó, todavía con miedo."
                    : $"{wild.DisplayName} olfateó la comida… y salió corriendo.");
            }
        }

        IEnumerator Say(string text)
        {
            bool done = false;
            GameRoot.Instance.Dialogue.Show(new[] { text }, () => done = true);
            while (!done) yield return null;
        }

        static IEnumerator WaitConfirm()
        {
            yield return null;
            while (!GameInput.ConfirmPressed) yield return null;
        }

        void Finish(RaceResult result)
        {
            Cleanup();
            RaceLauncher.Complete(result);
        }

        void Cleanup()
        {
            _hud?.Destroy();
            _hud = null;
            if (_camera != null) _camera.backgroundColor = _previousBackground;
            if (_clock != null) _clock.SetActive(true);
            if (_locked)
            {
                GameRoot.InputLocks--;
                _locked = false;
            }
        }

        void OnDestroy() => Cleanup();

        /// <summary>Copa de prueba para cuando se le da Play directo a la escena de carrera.</summary>
        static RaceSetup DebugCup(GameState state) => RaceSetups.Cup(state.team);
    }
}
