using System.Collections.Generic;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.Race
{
    /// <summary>Pista: tramos de terreno uno detrás de otro. Las habilidades pueden cambiar un tramo por unos segundos.</summary>
    public class RaceTrack
    {
        public class Segment
        {
            public RaceTerrain terrain;
            public float start, length;
            public float End => start + length;
            public RaceTerrain overrideTerrain;
            public float overrideTime;
            public RaceTerrain Current => overrideTime > 0f && overrideTerrain != null ? overrideTerrain : terrain;
        }

        public readonly List<Segment> Segments = new();
        public float Length { get; private set; }

        public RaceTrack(IEnumerable<(RaceTerrain terrain, float length)> parts)
        {
            foreach (var (terrain, length) in parts)
            {
                Segments.Add(new Segment { terrain = terrain, start = Length, length = length });
                Length += length;
            }
        }

        public int IndexAt(float x)
        {
            for (int i = 0; i < Segments.Count; i++)
                if (x < Segments[i].End) return i;
            return Segments.Count - 1;
        }

        public Segment At(float x) => Segments[IndexAt(x)];
        public RaceTerrain TerrainAt(float x) => At(Mathf.Clamp(x, 0f, Length - 0.01f)).Current;

        /// <summary>Terreno del tramo siguiente (o el mismo si es el último).</summary>
        public RaceTerrain NextTerrain(float x)
        {
            int i = IndexAt(x);
            return Segments[Mathf.Min(i + 1, Segments.Count - 1)].Current;
        }

        public float DistanceToNextSegment(float x) => At(x).End - x;

        public void Override(float x, RaceTerrain terrain, float seconds)
        {
            var s = At(x);
            s.overrideTerrain = terrain;
            s.overrideTime = seconds;
        }

        public void Tick(float dt)
        {
            foreach (var s in Segments)
                if (s.overrideTime > 0f) s.overrideTime -= dt;
        }
    }
}
