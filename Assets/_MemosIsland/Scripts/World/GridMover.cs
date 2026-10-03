using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Movimiento por casillas (estilo Pokémon) para el jugador y, más adelante, vecinos y Memos.
    /// La posición del objeto es el borde inferior central de su casilla (los sprites tienen pivote abajo).
    /// </summary>
    public class GridMover : MonoBehaviour
    {
        public const float PixelsPerUnit = 16f;

        [SerializeField] float walkSpeed = 4f; // casillas por segundo
        [SerializeField] float runSpeed = 8f;
        [SerializeField] Direction facing = Direction.Down;
        [Tooltip("Si es false no ocupa su casilla (el compañero que te sigue no te bloquea el paso).")]
        [SerializeField] bool occupiesCell = true;

        public Vector2Int Cell { get; private set; }
        public Direction Facing { get => facing; set => facing = value; }
        public bool IsMoving { get; private set; }
        public bool IsBumping => _bumpTime > 0f;
        public bool IsBusy => IsMoving || IsBumping;
        public bool IsRunning { get; private set; }
        /// <summary>Avance del paso actual (o del "choque" contra una pared), de 0 a 1.</summary>
        public float StepProgress { get; private set; }
        /// <summary>Cantidad de pasos dados; sirve para alternar el pie en la animación.</summary>
        public int StepCount { get; private set; }

        public event Action<GridMover> StepFinished;
        /// <summary>Empezó un paso: (quién, casilla de la que sale).</summary>
        public event Action<GridMover, Vector2Int> StepStarted;

        public float WalkSpeed { get => walkSpeed; set => walkSpeed = value; }
        public bool OccupiesCell { get => occupiesCell; set => occupiesCell = value; }

        const float BumpDuration = 0.25f;
        static readonly Dictionary<Vector2Int, GridMover> Occupied = new();
        static int _solidMask = -1;

        Vector2Int _target;
        float _bumpTime;

        public static int SolidMask
        {
            get
            {
                if (_solidMask < 0) _solidMask = LayerMask.GetMask("Solid");
                return _solidMask;
            }
        }

        public static Vector3 CellToWorld(Vector2Int cell) => new(cell.x + 0.5f, cell.y, 0f);
        public static Vector2 CellCenter(Vector2Int cell) => new(cell.x + 0.5f, cell.y + 0.5f);
        public static Vector2Int WorldToCell(Vector3 p) => new(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y + 0.01f));

        void OnEnable()
        {
            Cell = WorldToCell(transform.position);
            Occupy(Cell);
        }

        void OnDisable()
        {
            Release(Cell);
            if (IsMoving) Release(_target);
            IsMoving = false;
        }

        /// <summary>Coloca al personaje en una casilla sin animación (cambios de mapa, carga de partida).</summary>
        public void Teleport(Vector2Int cell, Direction newFacing)
        {
            Release(Cell);
            if (IsMoving) Release(_target);
            IsMoving = false;
            _bumpTime = 0f;
            Cell = cell;
            facing = newFacing;
            transform.position = CellToWorld(cell);
            Occupy(cell);
        }

        public bool CanEnter(Vector2Int cell)
        {
            var map = MapInfo.Current;
            if (map != null && !map.Contains(cell)) return false;
            if (Physics2D.OverlapPoint(CellCenter(cell), SolidMask) != null) return false;
            return !Occupied.TryGetValue(cell, out var other) || other == this;
        }

        /// <summary>Intenta dar un paso. Devuelve false si la casilla está bloqueada (igual gira).</summary>
        public bool TryStep(Direction dir, bool run)
        {
            if (IsBusy) return false;
            facing = dir;
            var target = Cell + dir.ToVector();
            if (!CanEnter(target)) return false;

            BeginStep(target, run);
            return true;
        }

        /// <summary>Da un paso sin chequear colisiones (lo usa el compañero para pisar donde estabas vos).</summary>
        public void ForceStep(Vector2Int target, bool run)
        {
            if (IsBusy || target == Cell) return;
            var delta = target - Cell;
            facing = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? Direction.Right : Direction.Left)
                : (delta.y > 0 ? Direction.Up : Direction.Down);
            BeginStep(target, run);
        }

        void BeginStep(Vector2Int target, bool run)
        {
            _target = target;
            Occupy(target);
            IsMoving = true;
            IsRunning = run;
            StepProgress = 0f;
            StepStarted?.Invoke(this, Cell);
        }

        void Occupy(Vector2Int cell)
        {
            if (occupiesCell) Occupied[cell] = this;
        }

        /// <summary>Anima un paso en el lugar al chocar contra algo, como en los juegos de GBA.</summary>
        public void Bump(Direction dir)
        {
            if (IsBusy) return;
            facing = dir;
            _bumpTime = BumpDuration;
            StepProgress = 0f;
        }

        void Update()
        {
            if (IsMoving)
            {
                StepProgress += (IsRunning ? runSpeed : walkSpeed) * Time.deltaTime;
                if (StepProgress >= 1f)
                {
                    Release(Cell);
                    Cell = _target;
                    IsMoving = false;
                    StepProgress = 1f;
                    StepCount++;
                    transform.position = CellToWorld(Cell);
                    StepFinished?.Invoke(this);
                    return;
                }
                var p = Vector3.Lerp(CellToWorld(Cell), CellToWorld(_target), StepProgress);
                // Siempre sobre la grilla de pixels para que nada tiemble.
                p.x = Mathf.Round(p.x * PixelsPerUnit) / PixelsPerUnit;
                p.y = Mathf.Round(p.y * PixelsPerUnit) / PixelsPerUnit;
                transform.position = p;
            }
            else if (_bumpTime > 0f)
            {
                _bumpTime -= Time.deltaTime;
                StepProgress = 1f - Mathf.Clamp01(_bumpTime / BumpDuration);
                if (_bumpTime <= 0f) StepCount++;
            }
        }

        void Release(Vector2Int cell)
        {
            if (Occupied.TryGetValue(cell, out var who) && who == this) Occupied.Remove(cell);
        }
    }
}
