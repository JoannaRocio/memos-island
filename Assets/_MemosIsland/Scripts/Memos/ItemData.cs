using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.Memos
{
    public enum ItemKind { Food, Equipment, Amulet, Accessory, Material }

    /// <summary>Cómo cambia un equipo la efectividad en ciertos terrenos (GDD §14).</summary>
    public enum TerrainRule
    {
        None,
        UpgradeOneStep,  // Herraduras: ▼ → · → ▲
        ForceStrong,     // Aletas, Alas de planeo: siempre ▲
        RemoveWeakness,  // Botas de clavos: ▼ pasa a ·
    }

    public enum AmuletEffect { None, Relay, ChargeStart, CalmBell, FriendshipBow, TerrainGem }

    public enum AccessorySlot { Head, Neck }

    /// <summary>Un objeto: comida, equipo, amuleto, accesorio estético o material (Fase 6).</summary>
    [CreateAssetMenu(menuName = "Memos Island/Objetos/Objeto")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public ItemKind kind;
        [Tooltip("Precio de venta en la tienda (Fase 6).")]
        public int price;
        public Sprite icon;

        [Header("Equipo")]
        public TerrainRule terrainRule;
        public List<RaceTerrain> terrains = new();
        public int speedBonus;
        public int staminaBonus;

        [Header("Amuleto")]
        public AmuletEffect amuletEffect;
        [Tooltip("Para las gemas: el terreno que pasa a ser ▲.")]
        public RaceTerrain gemTerrain;

        [Header("Accesorio")]
        public AccessorySlot accessorySlot;
        public Sprite accessorySprite;
    }

    /// <summary>Efectos del equipo y los amuletos en carrera (GDD §14).</summary>
    public static class EquipmentRules
    {
        public static Effectiveness Adjust(Effectiveness e, ItemData equipment, ItemData amulet, RaceTerrain terrain)
        {
            if (terrain == null) return e;
            if (equipment != null && equipment.terrains.Contains(terrain))
            {
                e = equipment.terrainRule switch
                {
                    TerrainRule.UpgradeOneStep => e == Effectiveness.Strong ? e : e + 1,
                    TerrainRule.ForceStrong => Effectiveness.Strong,
                    TerrainRule.RemoveWeakness => e == Effectiveness.Weak ? Effectiveness.Normal : e,
                    _ => e,
                };
            }
            if (amulet != null && amulet.amuletEffect == AmuletEffect.TerrainGem && amulet.gemTerrain == terrain)
                e = Effectiveness.Strong;
            return e;
        }

        /// <summary>Stats base con el equipo puesto (sin bajar de 1 ni pasar de 10).</summary>
        public static MemoStats WithEquipment(MemoStats s, ItemData equipment)
        {
            if (equipment == null) return s;
            s.speed = Mathf.Clamp(s.speed + equipment.speedBonus, 1, 10);
            s.stamina = Mathf.Clamp(s.stamina + equipment.staminaBonus, 1, 10);
            return s;
        }

        public static bool Has(ItemData amulet, AmuletEffect effect) => amulet != null && amulet.amuletEffect == effect;
    }
}
