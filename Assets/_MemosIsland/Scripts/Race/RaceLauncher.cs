using System;
using MemosIsland.Core;
using MemosIsland.Memos;

namespace MemosIsland.Race
{
    public class RaceResult
    {
        public RaceFormat format;
        public bool playerWon;
        public int playerRank;
        public int racers;
        /// <summary>En captura: el Memo que se unió (null si se escapó).</summary>
        public MemoInstance captured;
    }

    /// <summary>Lanza una carrera desde el mundo y vuelve al mapa cuando termina.</summary>
    public static class RaceLauncher
    {
        public const string SceneName = "Race";

        public static RaceSetup Pending { get; private set; }
        static Action<RaceResult> _onFinished;

        public static void Start(RaceSetup setup, Action<RaceResult> onFinished = null)
        {
            Pending = setup;
            _onFinished = onFinished;
            MapManager.Instance.EnterRace();
        }

        /// <summary>Lo llama la escena de carrera al terminar: vuelve al mapa y avisa el resultado.</summary>
        public static void Complete(RaceResult result)
        {
            var callback = _onFinished;
            Pending = null;
            _onFinished = null;
            MapManager.Instance.ReturnFromRace(() => callback?.Invoke(result));
        }
    }
}
