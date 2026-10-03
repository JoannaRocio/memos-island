using MemosIsland.UI;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Core
{
    /// <summary>
    /// Sistemas que viven durante toda la partida (jugador, cámara, interfaz, reloj, luz).
    /// Se crea solo desde Resources/GameRoot al darle Play a cualquier escena de mapa.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        /// <summary>Mientras haya bloqueos (diálogo, cambio de mapa…), el jugador no se mueve.</summary>
        public static int InputLocks;
        public static bool InputLocked => InputLocks > 0;

        [SerializeField] PlayerController player;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] DialogueBox dialogue;
        [SerializeField] ScreenFader fader;
        [SerializeField] MapNameBanner banner;
        [SerializeField] MapManager maps;

        public PlayerController Player => player;
        public CameraFollow Camera => cameraFollow;
        public DialogueBox Dialogue => dialogue;
        public ScreenFader Fader => fader;
        public MapNameBanner Banner => banner;
        public MapManager Maps => maps;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null || FindAnyObjectByType<MapInfo>() == null) return;
            var prefab = Resources.Load<GameRoot>("GameRoot");
            if (prefab == null)
            {
                Debug.LogError("[GameRoot] Falta Resources/GameRoot. Usá Memos Island ▸ Fase 1 ▸ Construir mundo de prueba.");
                return;
            }
            Instantiate(prefab).name = "GameRoot";
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InputLocks = 0;
            DontDestroyOnLoad(gameObject);
        }

        void Start() => maps.EnterCurrentScene();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
