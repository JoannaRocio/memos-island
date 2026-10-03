using System.Collections;
using MemosIsland.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemosIsland.Core
{
    /// <summary>Cambios de mapa con fundido a negro y cartel con el nombre del lugar.</summary>
    public class MapManager : MonoBehaviour
    {
        [SerializeField] float fadeDuration = 0.25f;

        public static MapManager Instance { get; private set; }
        public bool IsTransitioning { get; private set; }

        string _lastMapName;

        void Awake() => Instance = this;

        /// <summary>Ubica al jugador en el mapa ya cargado (al darle Play).</summary>
        public void EnterCurrentScene(string spawnId = null)
        {
            PlacePlayer(spawnId);
            ShowBannerIfNewMap();
        }

        public void OnPlayerStepped(Vector2Int cell)
        {
            if (IsTransitioning) return;
            var warp = Warp.At(cell);
            if (warp != null)
            {
                GoTo(warp.TargetScene, warp.TargetSpawn);
                return;
            }
            WildEncounters.Current?.TryEncounter(cell);
        }

        public void GoTo(string sceneName, string spawnId)
        {
            if (!IsTransitioning) StartCoroutine(Transition(sceneName, spawnId));
        }

        IEnumerator Transition(string sceneName, string spawnId)
        {
            IsTransitioning = true;
            GameRoot.InputLocks++;

            var root = GameRoot.Instance;
            yield return root.Fader.Fade(1f, fadeDuration);

            var load = SceneManager.LoadSceneAsync(sceneName);
            if (load == null)
            {
                Debug.LogError($"[MapManager] No se pudo cargar '{sceneName}'. ¿Está en Build Settings?");
            }
            else
            {
                while (!load.isDone) yield return null;
                yield return null; // que los objetos del mapa terminen de registrarse
                PlacePlayer(spawnId);
            }

            yield return root.Fader.Fade(0f, fadeDuration);
            GameRoot.InputLocks--;
            IsTransitioning = false;
            ShowBannerIfNewMap();
        }

        // ------------------------------------------------------------------ Carreras

        string _returnScene;
        Vector2Int _returnCell;
        Direction _returnFacing;

        /// <summary>Guarda dónde está el jugador y pasa a la escena de carrera.</summary>
        public void EnterRace()
        {
            if (IsTransitioning) return;
            var root = GameRoot.Instance;
            _returnScene = SceneManager.GetActiveScene().name;
            _returnCell = root.Player.Mover.Cell;
            _returnFacing = root.Player.Mover.Facing;
            StartCoroutine(LoadRaceScene());
        }

        IEnumerator LoadRaceScene()
        {
            IsTransitioning = true;
            GameRoot.InputLocks++;
            var root = GameRoot.Instance;
            yield return root.Fader.Fade(1f, fadeDuration);
            root.Player.gameObject.SetActive(false);
            root.Companion?.SetHidden(true);
            var load = SceneManager.LoadSceneAsync(Race.RaceLauncher.SceneName);
            while (load != null && !load.isDone) yield return null;
            yield return null;
            yield return root.Fader.Fade(0f, fadeDuration);
            GameRoot.InputLocks--;
            IsTransitioning = false;
        }

        /// <summary>Vuelve al mapa y a la casilla donde estaba el jugador antes de la carrera.</summary>
        public void ReturnFromRace(System.Action afterReturn)
        {
            StartCoroutine(LoadReturnScene(afterReturn));
        }

        IEnumerator LoadReturnScene(System.Action afterReturn)
        {
            IsTransitioning = true;
            GameRoot.InputLocks++;
            var root = GameRoot.Instance;
            yield return root.Fader.Fade(1f, fadeDuration);

            bool hasReturn = !string.IsNullOrEmpty(_returnScene);
            var load = hasReturn ? SceneManager.LoadSceneAsync(_returnScene) : SceneManager.LoadSceneAsync(0);
            while (load != null && !load.isDone) yield return null;
            yield return null;

            root.Player.gameObject.SetActive(true);
            if (hasReturn) root.Player.Mover.Teleport(_returnCell, _returnFacing);
            else PlacePlayer(null);
            root.Companion?.SetHidden(false);
            root.Camera.SetTarget(root.Player.transform);
            root.Camera.SnapNow();
            _returnScene = null;

            yield return root.Fader.Fade(0f, fadeDuration);
            GameRoot.InputLocks--;
            IsTransitioning = false;
            afterReturn?.Invoke();
        }

        void PlacePlayer(string spawnId)
        {
            var root = GameRoot.Instance;
            var spawn = SpawnPoint.Find(spawnId);
            if (spawn != null) root.Player.Mover.Teleport(spawn.Cell, spawn.Facing);
            root.Companion?.SnapBehind();
            root.Camera.SnapNow();
        }

        void ShowBannerIfNewMap()
        {
            var map = MapInfo.Current;
            if (map == null || map.DisplayName == _lastMapName) return;
            _lastMapName = map.DisplayName;
            GameRoot.Instance.Banner.Show(map.DisplayName);
        }
    }
}
