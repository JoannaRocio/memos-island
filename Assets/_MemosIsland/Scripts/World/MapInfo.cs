using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Datos del mapa cargado: nombre visible y límites caminables.</summary>
    public class MapInfo : MonoBehaviour
    {
        [SerializeField] string displayName = "Mapa";
        [SerializeField] RectInt bounds = new(0, 0, 32, 22);

        public static MapInfo Current { get; private set; }

        public string DisplayName => displayName;
        public RectInt Bounds => bounds;

        public void Setup(string newName, RectInt newBounds)
        {
            displayName = newName;
            bounds = newBounds;
        }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        public bool Contains(Vector2Int cell) => bounds.Contains(cell);

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.width, bounds.height));
        }
    }
}
