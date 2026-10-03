using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Índice de todos los datos de Memos. Vive en Resources/MemoDatabase.</summary>
    [CreateAssetMenu(menuName = "Memos Island/Memos/Base de datos")]
    public class MemoDatabase : ScriptableObject
    {
        public EffectivenessChart chart;
        public List<MemoType> types = new();
        public List<RaceTerrain> terrains = new();
        public List<AbilityData> abilities = new();
        public List<Temperament> temperaments = new();
        public List<MemoSpecies> species = new();
        public List<ItemData> items = new();

        static MemoDatabase _instance;

        public static MemoDatabase Instance
        {
            get
            {
                if (_instance == null) _instance = Resources.Load<MemoDatabase>("MemoDatabase");
                return _instance;
            }
        }

        /// <summary>Especies ordenadas por número de MemoBox.</summary>
        public IEnumerable<MemoSpecies> SpeciesByNumber => species.Where(s => s != null).OrderBy(s => s.number);

        public MemoSpecies GetSpecies(string id) => species.Find(s => s != null && s.id == id);
        public MemoType GetMemoType(string id) => types.Find(t => t != null && t.id == id);
        public RaceTerrain GetTerrain(string id) => terrains.Find(t => t != null && t.id == id);
        public Temperament GetTemperament(string id) => temperaments.Find(t => t != null && t.id == id);
        public ItemData GetItem(string id) => string.IsNullOrEmpty(id) ? null : items.Find(i => i != null && i.id == id);

        /// <summary>Qué tan bien corre una especie sobre un terreno.</summary>
        public Effectiveness EffectivenessFor(MemoSpecies s, RaceTerrain terrain) =>
            chart.Get(s.primaryType, s.secondaryType, terrain);
    }
}
