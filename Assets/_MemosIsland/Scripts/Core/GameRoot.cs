using MemosIsland.UI;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Core
{
    /// <summary>
    /// Sistemas que viven durante toda la partida (jugador, cámara, interfaz, reloj, luz, estado de la partida).
    /// Se crea solo desde Resources/GameRoot al darle Play a cualquier escena de mapa.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        /// <summary>Mientras haya bloqueos (diálogo, cambio de mapa…), el jugador no se mueve.</summary>
        public static int InputLocks;
        public static bool InputLocked => InputLocks > 0;

        /// <summary>Cuadro en el que una pantalla usó Esc/B para cerrarse (evita que otra se abra en el mismo cuadro).</summary>
        public static int UiConsumedFrame = -1;
        public static void ConsumeUiInput() => UiConsumedFrame = Time.frameCount;
        public static bool UiInputConsumed => UiConsumedFrame == Time.frameCount;

        [SerializeField] PlayerController player;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] DialogueBox dialogue;
        [SerializeField] ScreenFader fader;
        [SerializeField] MapNameBanner banner;
        [SerializeField] MapManager maps;
        [SerializeField] CompanionFollower companion;
        [SerializeField] MyMemosScreen myMemos;
        [SerializeField] MemoBoxScreen memoBox;
        [SerializeField] EvolutionScreen evolution;
        [SerializeField] ListScreen lists;
        [SerializeField] GameState state = new();
        [Tooltip("Mientras no exista la elección del inicial (Fase 8), arranca con un equipo de prueba.")]
        [SerializeField] bool giveDebugTeam = true;

        public PlayerController Player => player;
        public CameraFollow Camera => cameraFollow;
        public DialogueBox Dialogue => dialogue;
        public ScreenFader Fader => fader;
        public MapNameBanner Banner => banner;
        public MapManager Maps => maps;
        public CompanionFollower Companion => companion;
        public MyMemosScreen MyMemos => myMemos;
        public MemoBoxScreen MemoBox => memoBox;
        public EvolutionScreen Evolution => evolution;
        public ListScreen Lists => lists;
        public GameState State => state;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            if (FindAnyObjectByType<MapInfo>() == null && FindAnyObjectByType<Race.RaceController>() == null) return;
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
            UiKit.FullScreens = 0;
            NeighborDirector.Reserved.Clear();
            DontDestroyOnLoad(gameObject);
            if (GetComponent<Story.StoryDirector>() == null) gameObject.AddComponent<Story.StoryDirector>();
            // Al darle Play a un mapa directo (sin pasar por el título), se juega con la partida de prueba.
            bool title = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Story.TitleScreen.SceneName;
            if (giveDebugTeam && !title && state.team.Count == 0) state.GiveDebugTeam();
        }

        void Start() => maps.EnterCurrentScene();

        /// <summary>Nueva partida de historia (Fase 8): sin Memos, con 500 de dinero, el prólogo y la creación de personaje.</summary>
        public void StartNewGame()
        {
            state = new GameState();
            state.story.active = true;
            companion.Refresh();
        }

        /// <summary>Partida de prueba: el equipo y los objetos de prueba, sin escenas de historia.</summary>
        public void StartDebugGame()
        {
            state = new GameState();
            state.GiveDebugTeam();
            companion.Refresh();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
