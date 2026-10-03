using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Personalidad de un Memo capturado (GDD §7): cambia un poco sus stats y su conducta.</summary>
    [CreateAssetMenu(menuName = "Memos Island/Memos/Temperamento")]
    public class Temperament : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string raceDescription;
        [TextArea] public string refugeDescription;

        [Header("Multiplicadores de stats")]
        public float speedMultiplier = 1f;
        public float accelerationMultiplier = 1f;
        public float staminaMultiplier = 1f;
        public float chargeMultiplier = 1f;

        [Header("Otros")]
        public float trustGainMultiplier = 1f;
        public TemperamentTrait trait;
    }
}
