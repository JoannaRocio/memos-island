using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Búsqueda de camino en la grilla (BFS): suficiente para mapas chicos como el refugio.</summary>
    public static class GridPath
    {
        static readonly Direction[] Dirs = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        /// <summary>
        /// Devuelve las direcciones para ir de "from" a "to" (o hasta una casilla vecina si "to" está ocupada).
        /// Lista vacía si no hay camino dentro de maxNodes.
        /// </summary>
        public static List<Direction> Find(GridMover mover, Vector2Int from, Vector2Int to, int maxNodes = 600, bool adjacentIsEnough = false)
        {
            var result = new List<Direction>();
            if (from == to) return result;
            var cameFrom = new Dictionary<Vector2Int, (Vector2Int prev, Direction dir)>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            cameFrom[from] = (from, Direction.Down);
            Vector2Int? reached = null;

            while (queue.Count > 0 && cameFrom.Count < maxNodes)
            {
                var cell = queue.Dequeue();
                if (cell == to || adjacentIsEnough && Manhattan(cell, to) == 1)
                {
                    reached = cell;
                    break;
                }
                foreach (var d in Dirs)
                {
                    var next = cell + d.ToVector();
                    if (cameFrom.ContainsKey(next)) continue;
                    if (next != to && !mover.CanEnter(next)) continue;
                    if (next == to && !adjacentIsEnough && !mover.CanEnter(next)) continue;
                    cameFrom[next] = (cell, d);
                    queue.Enqueue(next);
                }
            }

            if (reached == null) return result;
            var c = reached.Value;
            while (c != from)
            {
                var (prev, dir) = cameFrom[c];
                result.Add(dir);
                c = prev;
            }
            result.Reverse();
            return result;
        }

        public static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
