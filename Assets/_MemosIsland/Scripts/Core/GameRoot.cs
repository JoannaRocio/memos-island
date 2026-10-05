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

        /// <summary>Ranura de guardado de esta partida (-1 = partida de prueba: no se guarda).</summary>
        public int CurrentSlot { get; private set; } = -1;
        public float PlaySeconds { get; private set; }

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
            if (GetComponent<AudioManager>() == null) gameObject.AddComponent<AudioManager>();
            // Al darle Play a un mapa directo (sin pasar por el título), se juega con la partida de prueba.
            bool title = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Story.TitleScreen.SceneName;
            if (giveDebugTeam && !title && state.team.Count == 0) state.GiveDebugTeam();
            GameSettings.Load();
            MapManager.SceneEntered += OnSceneEntered;
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
            CurrentSlot = -1;
            companion.Refresh();
        }

        void OnDestroy()
        {
            MapManager.SceneEntered -= OnSceneEntered;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (CurrentSlot >= 0) PlaySeconds += Time.unscaledDeltaTime;
        }

        // ------------------------------------------------------------------ Guardado (Fase 9A)

        /// <summary>Autoguardado: cada vez que llegás a un mapa (y al volver de una carrera).</summary>
        void OnSceneEntered(string scene) => Autosave();

        public void Autosave()
        {
            if (CurrentSlot >= 0 && MapInfo.Current != null) SaveNow();
        }

        /// <summary>Guarda en la ranura actual. Devuelve false si es una partida de prueba o falló la escritura.</summary>
        public bool SaveNow()
        {
            if (CurrentSlot < 0) return false;
            var cell = player.Mover.Cell;
            return SaveSystem.Write(CurrentSlot, new SaveData
            {
                savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                mapName = MapInfo.Current != null ? MapInfo.Current.DisplayName : "",
                x = cell.x,
                y = cell.y,
                facing = player.Mover.Facing,
                playSeconds = PlaySeconds,
                state = state,
            });
        }

        /// <summary>Carga una partida y lleva al jugador adonde estaba.</summary>
        public void LoadGame(SaveData data, int slot)
        {
            state = data.state;
            CurrentSlot = slot;
            PlaySeconds = data.playSeconds;
            NeighborDirector.Reserved.Clear();
            Story.PlayerLook.Apply(player.Mover, state.story.profile);
            companion.Refresh();
            maps.GoToCell(data.scene, new Vector2Int(data.x, data.y), data.facing);
        }

        /// <summary>Elige la ranura de una partida nueva (se guarda por primera vez al llegar al pueblo).</summary>
        public void UseSlot(int slot)
        {
            CurrentSlot = slot;
            PlaySeconds = 0f;
        }
    }
}
