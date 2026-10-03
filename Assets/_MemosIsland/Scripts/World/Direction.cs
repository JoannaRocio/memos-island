using UnityEngine;

namespace MemosIsland.World
{
    public enum Direction { Down, Up, Left, Right }

    public static class DirectionExtensions
    {
        public static Vector2Int ToVector(this Direction d) => d switch
        {
            Direction.Up => Vector2Int.up,
            Direction.Down => Vector2Int.down,
            Direction.Left => Vector2Int.left,
            Direction.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };

        public static Direction Opposite(this Direction d) => d switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            _ => Direction.Left
        };

        /// <summary>
        /// Convierte el stick/teclas en una de 4 direcciones. Si se aprietan dos a la vez (diagonal),
        /// se mantiene la dirección actual cuando es una de las dos; si no, gana la vertical.
        /// </summary>
        public static Direction? FromInput(Vector2 v, Direction current, float deadZone = 0.5f)
        {
            bool h = Mathf.Abs(v.x) >= deadZone * 0.7f;
            bool vert = Mathf.Abs(v.y) >= deadZone * 0.7f;
            if (!h && !vert) return null;

            var horizontal = v.x > 0 ? Direction.Right : Direction.Left;
            var vertical = v.y > 0 ? Direction.Up : Direction.Down;
            if (h && !vert) return horizontal;
            if (vert && !h) return vertical;

            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y);
            if (Mathf.Abs(ax - ay) > 0.2f) return ax > ay ? horizontal : vertical;
            if (current == horizontal || current == vertical) return current;
            return vertical;
        }
    }
}
