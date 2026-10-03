using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Casilla que lleva a otro mapa al pisarla.</summary>
    public class Warp : MonoBehaviour
    {
        [SerializeField] string targetScene;
        [SerializeField] string targetSpawn;

        static readonly List<Warp> All = new();

        public string TargetScene => targetScene;
        public string TargetSpawn => targetSpawn;
        public Vector2Int Cell => GridMover.WorldToCell(transform.position);

        public void Setup(string scene, string spawn)
        {
            targetScene = scene;
            targetSpawn = spawn;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Warp At(Vector2Int cell) => All.Find(w => w.Cell == cell);

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.9f, 0.5f);
            Gizmos.DrawCube(GridMover.CellCenter(Cell), Vector3.one * 0.9f);
        }
    }
}
