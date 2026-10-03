using System;
using System.Collections.Generic;
using MemosIsland.Memos;

namespace MemosIsland.Race
{
    public enum RaceFormat { Capture, Friendly, Trainer, Cup }

    [Serializable]
    public class RaceSegmentDef
    {
        public string terrainId;
        public float length;

        public RaceSegmentDef() { }

        public RaceSegmentDef(string terrainId, float length)
        {
            this.terrainId = terrainId;
            this.length = length;
        }
    }

    /// <summary>Un corredor: el jugador, un entrenador rival o un Memo salvaje.</summary>
    public class RacerSetup
    {
        public string name;
        public List<MemoInstance> team = new();
        public bool isWild;
    }

    /// <summary>Todo lo necesario para armar una carrera (GDD §9 y §10).</summary>
    public class RaceSetup
    {
        public RaceFormat format;
        public string title;
        public List<RaceSegmentDef> segments = new();
        public RacerSetup player;
        public List<RacerSetup> rivals = new();
        public int seed = Environment.TickCount;

        public bool IsCapture => format == RaceFormat.Capture;

        public static RaceSetup Capture(MemoInstance wild, string terrainId, MemoInstance runner)
        {
            return new RaceSetup
            {
                format = RaceFormat.Capture,
                title = $"¡Un {wild.DisplayName} salvaje!",
                segments = { new RaceSegmentDef(terrainId, 220f) },
                player = new RacerSetup { name = "Vos", team = { runner } },
                rivals = { new RacerSetup { name = wild.DisplayName, team = { wild }, isWild = true } },
            };
        }
    }
}
