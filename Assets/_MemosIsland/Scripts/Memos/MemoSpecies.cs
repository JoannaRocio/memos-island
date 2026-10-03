using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Una especie de Memo: todo lo que muestra la MemoBox y lo que usan las carreras.</summary>
    [CreateAssetMenu(menuName = "Memos Island/Memos/Especie")]
    public class MemoSpecies : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Número en la MemoBox (1, 2, 3…).")]
        public int number;
        public string id;
        public string displayName;
        public MemoCategory category;
        public MemoAvailability availability;

        [Header("Tipo y carrera")]
        public MemoType primaryType;
        public MemoType secondaryType;
        public Mobility mobility;
        public MemoStats baseStats = new(5, 5, 5, 5);
        public AbilityData ability;

        [Header("Evolución")]
        public MemoSpecies evolvesTo;
        [Tooltip("Nivel al que evoluciona (0 = no evoluciona por nivel).")]
        public int evolutionLevel;
        [Tooltip("Evoluciona al llegar a Alma gemela (GDD §13).")]
        public bool evolvesWithTrust;

        [Header("MemoBox")]
        public string habitat;
        [TextArea(2, 5)] public string description;
        [Tooltip("Comidas favoritas (ids de objetos; los objetos llegan en la Fase 6).")]
        public List<string> favoriteFoods = new();

        [Header("Sprites")]
        [Tooltip("Vista lateral de 64x64 para carreras y MemoBox (cuadros de la animación de correr).")]
        public Sprite[] raceFrames;
        public Sprite[] shinyRaceFrames;
        [Tooltip("Desaturado: cómo se ve con el collar de Ápice.")]
        public Sprite[] collarRaceFrames;
        [Tooltip("Sprite de 32x32 de perfil para el mundo y el refugio (2 cuadros).")]
        public Sprite[] worldFrames;
        public Sprite[] shinyWorldFrames;
        public Sprite[] collarWorldFrames;
        [Tooltip("Dónde va un accesorio de cabeza / de cuello, en pixels del sprite de mundo (32x32, mirando a la derecha, y desde arriba).")]
        public Vector2Int headAnchor = new(16, 8);
        public Vector2Int neckAnchor = new(16, 18);

        public bool HasType(MemoType type) => primaryType == type || (secondaryType != null && secondaryType == type);

        public Sprite Portrait => raceFrames != null && raceFrames.Length > 0 ? raceFrames[0] : null;

        public string TypesText => secondaryType != null
            ? $"{primaryType.displayName}/{secondaryType.displayName}"
            : primaryType != null ? primaryType.displayName : "";

        public static string MobilityName(Mobility m) => m switch
        {
            Mobility.Swims => "Nada",
            Mobility.Flies => "Vuela",
            Mobility.Digs => "Excava",
            _ => "Corre",
        };

        public static string CategoryName(MemoCategory c) => c switch
        {
            MemoCategory.Starter => "Inicial",
            MemoCategory.Legendary => "Legendario",
            MemoCategory.Exclusive => "Exclusivo",
            _ => "Salvaje",
        };
    }
}
