using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Tipo de Memo (Planta, Fuego, Agua…).</summary>
    [CreateAssetMenu(menuName = "Memos Island/Memos/Tipo")]
    public class MemoType : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("Color del texto del tipo en la interfaz (legible sobre fondo blanco).")]
        public Color color = Color.black;
    }
}
