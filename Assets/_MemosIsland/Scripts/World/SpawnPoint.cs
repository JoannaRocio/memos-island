using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Lugar donde aparece el jugador al llegar a un mapa.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] string id = "default";
        [SerializeField] Direction facing = Direction.Down;

        static readonly List<SpawnPoint> All = new();

        public string Id => id;
        public Direction Facing => facing;
        public Vector2Int Cell => GridMover.WorldToCell(transform.position);

        public void Setup(string newId, Direction newFacing)
        {
            id = newId;
            facing = newFacing;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static SpawnPoint Find(string spawnId)
        {
            if (!string.IsNullOrEmpty(spawnId))
            {
                var exact = All.Find(s => s.id == spawnId);
                if (exact != null) return exact;
            }
            return All.Find(s => s.id == "default") ?? (All.Count > 0 ? All[0] : null);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            Gizmos.DrawWireCube(GridMover.CellCenter(Cell), Vector3.one * 0.9f);
        }
    }
}
