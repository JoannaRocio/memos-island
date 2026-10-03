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
            if (warp != null) GoTo(warp.TargetScene, warp.TargetSpawn);
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

        void PlacePlayer(string spawnId)
        {
            var root = GameRoot.Instance;
            var spawn = SpawnPoint.Find(spawnId);
            if (spawn != null) root.Player.Mover.Teleport(spawn.Cell, spawn.Facing);
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
