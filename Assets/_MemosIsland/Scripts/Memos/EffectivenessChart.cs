using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>
    /// Tabla tipo × terreno (GDD §8). Se edita como grilla en el inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Memos Island/Memos/Tabla de efectividad")]
    public class EffectivenessChart : ScriptableObject
    {
        public const float StrongMultiplier = 1.30f;
        public const float NormalMultiplier = 1.00f;
        public const float WeakMultiplier = 0.75f;

        public List<MemoType> types = new();
        public List<RaceTerrain> terrains = new();
        [SerializeField] List<Effectiveness> cells = new(); // fila = tipo, columna = terreno

        public void Resize()
        {
            int needed = types.Count * terrains.Count;
            while (cells.Count < needed) cells.Add(Effectiveness.Normal);
            if (cells.Count > needed) cells.RemoveRange(needed, cells.Count - needed);
        }

        public Effectiveness Get(MemoType type, RaceTerrain terrain)
        {
            int t = types.IndexOf(type), r = terrains.IndexOf(terrain);
            if (t < 0 || r < 0) return Effectiveness.Normal;
            int i = t * terrains.Count + r;
            return i < cells.Count ? cells[i] : Effectiveness.Normal;
        }

        public void Set(MemoType type, RaceTerrain terrain, Effectiveness value)
        {
            Resize();
            int t = types.IndexOf(type), r = terrains.IndexOf(terrain);
            if (t >= 0 && r >= 0) cells[t * terrains.Count + r] = value;
        }

        /// <summary>Efectividad de un Memo (uno o dos tipos) sobre un terreno, con la regla de tipo doble.</summary>
        public Effectiveness Get(MemoType primary, MemoType secondary, RaceTerrain terrain) =>
            secondary == null ? Get(primary, terrain) : Combine(Get(primary, terrain), Get(secondary, terrain));

        /// <summary>
        /// Regla de tipo doble: ▲ si alguno es ▲ y ninguno ▼; ▼ si alguno es ▼ y ninguno ▲; si no, normal.
        /// </summary>
        public static Effectiveness Combine(Effectiveness a, Effectiveness b)
        {
            bool strong = a == Effectiveness.Strong || b == Effectiveness.Strong;
            bool weak = a == Effectiveness.Weak || b == Effectiveness.Weak;
            if (strong && !weak) return Effectiveness.Strong;
            if (weak && !strong) return Effectiveness.Weak;
            return Effectiveness.Normal;
        }

        public static float Multiplier(Effectiveness e) => e switch
        {
            Effectiveness.Strong => StrongMultiplier,
            Effectiveness.Weak => WeakMultiplier,
            _ => NormalMultiplier,
        };
    }
}
