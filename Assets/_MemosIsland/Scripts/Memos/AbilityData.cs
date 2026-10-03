using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Habilidad única de una especie (GDD §7). La Fase 3 implementa sus efectos en carrera.</summary>
    [CreateAssetMenu(menuName = "Memos Island/Memos/Habilidad")]
    public class AbilityData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public AbilityArchetype archetype;
        public AbilityEffect effect;
        [Tooltip("Segundos que dura el efecto (0 = instantáneo).")]
        public float duration;
        [Tooltip("Intensidad del efecto: multiplicador de velocidad, energía recuperada, etc.")]
        public float strength;
        [Tooltip("Terreno que crea (solo para habilidades que cambian el terreno).")]
        public RaceTerrain terrain;
        [Tooltip("Afecta a todos los rivales cercanos, no solo a uno.")]
        public bool affectsAll;
    }
}
