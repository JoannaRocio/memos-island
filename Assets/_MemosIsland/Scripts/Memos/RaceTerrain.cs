using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Terreno de un tramo de carrera (Pradera, Río, Hielo…).</summary>
    [CreateAssetMenu(menuName = "Memos Island/Memos/Terreno")]
    public class RaceTerrain : ScriptableObject
    {
        public string id;
        public string displayName;
        public Color color = Color.gray;
    }
}
