using System.Collections.Generic;
using MemosIsland.Memos;
using MemosIsland.UI;
using UnityEngine;

namespace MemosIsland.Race
{
    /// <summary>
    /// Dibuja la carrera de perfil (como Monster Race): un carril por corredor, el suelo de cada tramo según su terreno
    /// (incluso cuando una habilidad lo cambia), los Memos corriendo y sus estados.
    /// 1 unidad de mundo = 1 metro de pista = 16 pixels.
    /// </summary>
    public class RaceView : MonoBehaviour
    {
        public const float LaneSpacing = 1.5f;
        public const float FrontLaneY = -3.5f;

        [SerializeField] PixelFont font;
        [SerializeField] Sprite pixelSprite;
        [SerializeField] Sprite flagSprite;
        [SerializeField] Sprite finishSprite;
        [SerializeField] List<Sprite> terrainSprites = new(); // nombre "track_<id>"

        RaceSimulation _sim;
        Transform _cameraAnchor;
        readonly Dictionary<string, Sprite> _terrainById = new();
        readonly List<(SpriteRenderer r, int lane, int row, int column)> _tiles = new();
        readonly List<SpriteRenderer> _finishTiles = new();
        readonly List<RunnerView> _runners = new();
        readonly List<SpriteRenderer> _zoneViews = new();
        const int Columns = 34;

        class RunnerView
        {
            public Racer racer;
            public SpriteRenderer sprite;
            public PixelText status, popup;
            public float popupTime, animTime;
            public int lastActive = -1;
        }

        public Transform CameraAnchor => _cameraAnchor;

        public void Setup(PixelFont newFont, Sprite pixel, Sprite flag, Sprite finish, List<Sprite> terrains)
        {
            font = newFont;
            pixelSprite = pixel;
            flagSprite = flag;
            finishSprite = finish;
            terrainSprites = terrains;
        }

        public static float LaneY(int lane) => FrontLaneY + lane * LaneSpacing;

        public void Build(RaceSimulation sim)
        {
            _sim = sim;
            foreach (var s in terrainSprites)
                if (s != null) _terrainById[s.name.Replace("track_", "")] = s;

            _cameraAnchor = new GameObject("Camera Anchor").transform;
            _cameraAnchor.SetParent(transform, false);

            int lanes = sim.Racers.Count;
            // Suelo: 2 filas por carril (el de adelante tiene 3 para llegar hasta la interfaz).
            for (int lane = lanes - 1; lane >= 0; lane--)
            {
                int rows = lane == 0 ? 4 : 2;
                for (int row = 0; row < rows; row++)
                for (int c = 0; c < Columns; c++)
                {
                    var r = NewRenderer($"Tile{lane}_{row}_{c}", null, (lanes - lane) * 10 - 9);
                    _tiles.Add((r, lane, row, c));
                }
                var finish = NewRenderer($"Finish{lane}", finishSprite, (lanes - lane) * 10 - 8);
                finish.transform.position = new Vector3(sim.Track.Length, LaneY(lane), 0f);
                finish.transform.localScale = new Vector3(0.5f, 1f, 1f);
                _finishTiles.Add(finish);
            }

            // Banderas y carteles al principio de cada tramo (detrás del carril del fondo).
            float flagY = LaneY(lanes - 1) + 0.2f;
            for (int i = 0; i < sim.Track.Segments.Count; i++)
            {
                var seg = sim.Track.Segments[i];
                var flag = NewRenderer($"Flag{i}", flagSprite, 0);
                flag.transform.position = new Vector3(seg.start + 0.5f, flagY, 0f);
                var label = NewText($"FlagLabel{i}", new Vector3(seg.start + 0.4f, flagY + 5.2f, 0f), 2, Color.white);
                label.SetText(seg.terrain.displayName);
            }

            foreach (var racer in sim.Racers)
            {
                int order = (lanes - racer.lane) * 10 - 5;
                var view = new RunnerView
                {
                    racer = racer,
                    sprite = NewRenderer($"Runner {racer.name}", null, order),
                    status = NewText("Status", Vector3.zero, order + 2, Color.white),
                    popup = NewText("Popup", Vector3.zero, order + 3, new Color32(0xff, 0xcd, 0x75, 0xff)),
                };
                _runners.Add(view);
            }
            LateUpdate();
        }

        public void ShowPopup(Racer racer, string text)
        {
            var v = _runners.Find(r => r.racer == racer);
            if (v == null) return;
            v.popup.SetText(text);
            v.popupTime = 1.6f;
        }

        void LateUpdate()
        {
            if (_sim == null) return;
            var player = _sim.Player;
            // La cámara deja al jugador en el tercio izquierdo de la pantalla.
            _cameraAnchor.position = new Vector3(player.position + 8f, 0.25f, 0f);
            float left = Mathf.Floor(_cameraAnchor.position.x - 17f);

            foreach (var (r, lane, row, column) in _tiles)
            {
                float x = left + column;
                var terrain = _sim.Track.TerrainAt(Mathf.Clamp(x + 0.5f, 0f, _sim.Track.Length - 0.01f));
                r.sprite = terrain != null && _terrainById.TryGetValue(terrain.id, out var s) ? s : null;
                r.transform.position = new Vector3(x, LaneY(lane) - row, 0f);
            }

            UpdateZones();
            foreach (var v in _runners) UpdateRunner(v);
        }

        void UpdateRunner(RunnerView v)
        {
            var r = v.racer;
            var frames = r.Active.instance.RaceFrames;
            if (v.lastActive != r.activeIndex)
            {
                v.lastActive = r.activeIndex;
                v.animTime = 0f;
            }
            // La animación de correr va más rápido cuanto más rápido corre.
            v.animTime += Time.deltaTime * Mathf.Lerp(2f, 10f, Mathf.Clamp01(r.speed / 14f));
            if (frames != null && frames.Length > 0)
                v.sprite.sprite = r.speed > 0.05f ? frames[(int)v.animTime % frames.Length] : frames[0];

            float y = LaneY(r.lane);
            float x = r.position;
            if (r.Has(StatusKind.Zigzag)) y += Mathf.Sin(Time.time * 18f) * 0.25f;
            if (r.Has(StatusKind.Stun)) x += Mathf.Sin(Time.time * 50f) * 0.08f;
            v.sprite.transform.position = Snap(new Vector3(x, y, 0f));

            v.sprite.color = TintFor(r);
            string status = StatusText(r);
            v.status.SetText(status);
            v.status.transform.position = Snap(new Vector3(x - 0.4f, y + 4.3f, 0f));

            if (v.popupTime > 0f)
            {
                v.popupTime -= Time.deltaTime;
                v.popup.transform.position = Snap(new Vector3(x - 2f, y + 5.2f + (1.6f - v.popupTime) * 0.4f, 0f));
                if (v.popupTime <= 0f) v.popup.SetText("");
            }
        }

        static Color TintFor(Racer r)
        {
            if (r.IsHidden) return new Color(1f, 1f, 1f, 0.25f);
            if (r.Has(StatusKind.Root)) return new Color(0.55f, 0.95f, 0.55f);
            if (r.Has(StatusKind.Sleep)) return new Color(0.65f, 0.7f, 1f);
            if (r.Has(StatusKind.Stun)) return Time.time % 0.2f < 0.1f ? Color.white : new Color(1f, 1f, 0.5f);
            if (r.Has(StatusKind.Blind)) return new Color(1f, 1f, 1f, 0.75f);
            if (r.Has(StatusKind.Slow) || r.Has(StatusKind.Magnet)) return new Color(1f, 0.7f, 0.65f);
            if (r.Has(StatusKind.Sprint)) return Time.time % 0.16f < 0.08f ? Color.white : new Color(1f, 0.95f, 0.8f);
            if (r.Has(StatusKind.Switching)) return new Color(1f, 1f, 1f, 0.6f);
            return Color.white;
        }

        static string StatusText(Racer r)
        {
            if (r.Has(StatusKind.Sleep)) return "zz";
            if (r.Has(StatusKind.Stun)) return "!!";
            if (r.Has(StatusKind.Root)) return "##";
            if (r.Has(StatusKind.Zigzag)) return "??";
            if (r.Has(StatusKind.Blind)) return "**";
            if (r.Has(StatusKind.Magnet)) return "<<";
            if (r.Has(StatusKind.Sprint)) return ">>";
            if (r.gaveUp) return "zz";
            return "";
        }

        void UpdateZones()
        {
            int lanes = _sim.Racers.Count;
            float bottom = LaneY(0) - 0.2f, top = LaneY(lanes - 1) + 0.6f;
            for (int i = 0; i < Mathf.Max(_sim.Zones.Count, _zoneViews.Count); i++)
            {
                if (i >= _zoneViews.Count)
                {
                    var zr = NewRenderer($"Zone{i}", pixelSprite, lanes * 10 + 1);
                    _zoneViews.Add(zr);
                }
                var view = _zoneViews[i];
                if (i >= _sim.Zones.Count)
                {
                    view.enabled = false;
                    continue;
                }
                var z = _sim.Zones[i];
                view.enabled = true;
                float spritePx = pixelSprite.rect.width / 16f;
                view.transform.localScale = new Vector3((z.end - z.start) / spritePx, 0.25f / spritePx, 1f);
                view.transform.position = new Vector3((z.start + z.end) / 2f, bottom + 0.15f, 0f);
                view.color = new Color(1f, 0.45f, 0.2f, 0.55f + Mathf.Sin(Time.time * 12f) * 0.15f);
            }
        }

        static Vector3 Snap(Vector3 p) => new(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, p.z);

        SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }

        PixelText NewText(string name, Vector3 position, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var t = go.AddComponent<PixelText>();
            t.Setup(font, null, order, color, true);
            return t;
        }
    }
}
