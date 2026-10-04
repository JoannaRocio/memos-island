using System;
using System.Collections;
using MemosIsland.Town;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Un vecino caminando por el mapa (Fase 7). El NeighborDirector le dice adónde ir según su rutina;
    /// con A se charla (NeighborTalk).
    /// </summary>
    public class NeighborNpc : MonoBehaviour, IInteractable
    {
        public NeighborData Data { get; private set; }
        public GridMover Mover { get; private set; }
        public EmoteBubble Bubble { get; private set; }
        public ScheduleEntry Entry { get; private set; }
        public bool Leaving { get; private set; }
        public bool Talking { get; private set; }
        public bool Arrived => Entry != null && !Leaving && Mover.Cell == Entry.cell && !Mover.IsBusy;

        Coroutine _walk;

        public void Setup(NeighborData data)
        {
            Data = data;
            Mover = GetComponent<GridMover>();
            Mover.WalkSpeed = 3f;
            Bubble = GetComponentInChildren<EmoteBubble>();
            GetComponentInChildren<CharacterSpriteAnimator>().Setup(Mover, data.down, data.up, data.left, data.right);
            name = $"Vecino {data.displayName}";
        }

        /// <summary>Ir (caminando) al lugar de esta entrada de la rutina.</summary>
        public void GoTo(ScheduleEntry entry)
        {
            Entry = entry;
            Leaving = false;
            Restart(WalkTo(entry.cell, entry.facing, null));
        }

        /// <summary>Ubicarlo ya en su lugar (al cargar el mapa).</summary>
        public void PlaceAt(ScheduleEntry entry)
        {
            Entry = entry;
            Leaving = false;
            Mover.Teleport(entry.cell, entry.facing);
        }

        /// <summary>Irse por una salida del mapa; al llegar (o si no puede) desaparece.</summary>
        public void Leave(Vector2Int exit, Action onGone)
        {
            Leaving = true;
            Restart(WalkTo(exit, Mover.Facing, onGone));
        }

        void Restart(IEnumerator routine)
        {
            if (_walk != null) StopCoroutine(_walk);
            _walk = StartCoroutine(routine);
        }

        IEnumerator WalkTo(Vector2Int target, Direction facing, Action done)
        {
            float stuck = 0f;
            while (Mover.Cell != target)
            {
                while (Talking || Mover.IsBusy) yield return null;
                var path = GridPath.Find(Mover, Mover.Cell, target, 4000);
                bool moved = false;
                foreach (var dir in path)
                {
                    while (Talking || Mover.IsBusy) yield return null;
                    if (!Mover.TryStep(dir, false)) break; // algo se cruzó: recalcular
                    moved = true;
                    while (Mover.IsMoving) yield return null;
                }
                if (moved) stuck = 0f;
                else
                {
                    stuck += 0.5f;
                    yield return new WaitForSeconds(0.5f);
                    // Si lleva rato trabado (por ejemplo, la casilla está tapada), aparece en su lugar.
                    if (stuck > 8f && !GridMover.IsOccupied(target))
                    {
                        Mover.Teleport(target, facing);
                        break;
                    }
                }
            }
            Mover.Facing = facing;
            _walk = null;
            done?.Invoke();
        }

        public void Interact(PlayerController player)
        {
            if (Talking || Leaving) return;
            Talking = true;
            var d = player.Mover.Cell - Mover.Cell;
            Mover.Facing = Mathf.Abs(d.x) >= Mathf.Abs(d.y)
                ? d.x >= 0 ? Direction.Right : Direction.Left
                : d.y >= 0 ? Direction.Up : Direction.Down;
            NeighborTalk.Start(this, () =>
            {
                Talking = false;
                if (Arrived) Mover.Facing = Entry.facing;
            });
        }
    }
}
