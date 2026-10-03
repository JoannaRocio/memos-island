using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.Farm
{
    public enum CraftStation { Workbench, Smelter }

    [Serializable]
    public struct ItemAmount
    {
        public string id;
        public int count;

        public ItemAmount(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }

    /// <summary>Receta de la mesa de trabajo o de la fundición (GDD §15).</summary>
    [CreateAssetMenu(menuName = "Memos Island/Objetos/Receta")]
    public class RecipeData : ScriptableObject
    {
        public string id;
        public CraftStation station;
        public List<ItemAmount> inputs = new();
        public ItemAmount output;
        [Tooltip("Solo fundición: minutos reales que tarda (un Memo de fuego contento lo hace 3 veces más rápido).")]
        public float minutes;
    }
}
