using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Control del protagonista: un toque gira sin moverse, mantener camina, B (X/Shift) corre, A interactúa.
    /// </summary>
    [RequireComponent(typeof(GridMover))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Cuánto hay que mantener una dirección nueva para empezar a caminar (si no, solo gira).")]
        [SerializeField] float turnDelay = 0.09f;

        public GridMover Mover { get; private set; }

        float _holdTime;
        bool _keepWalking;

        void Awake()
        {
            Mover = GetComponent<GridMover>();
            Mover.StepFinished += OnStepFinished;
        }

        void OnDestroy() => Mover.StepFinished -= OnStepFinished;

        void Update()
        {
            if (GameRoot.InputLocked || Mover.IsBusy) return;

            if (GameInput.ConfirmPressed)
            {
                TryInteract();
                return;
            }

            var dir = DirectionExtensions.FromInput(GameInput.Move, Mover.Facing);
            if (dir == null)
            {
                _holdTime = 0f;
                _keepWalking = false;
                return;
            }

            if (dir.Value != Mover.Facing && !_keepWalking)
            {
                Mover.Facing = dir.Value;
                _holdTime = 0f;
                return;
            }

            _holdTime += Time.deltaTime;
            if (!_keepWalking && _holdTime < turnDelay) return;

            if (!Mover.TryStep(dir.Value, GameInput.CancelHeld))
            {
                Mover.Bump(dir.Value);
                _keepWalking = false;
            }
        }

        void OnStepFinished(GridMover mover)
        {
            _keepWalking = true;
            AudioManager.Sfx("step", mover.IsRunning ? 0.35f : 0.25f, Random.Range(0.9f, 1.1f));
            MapManager.Instance?.OnPlayerStepped(mover.Cell);
        }

        void TryInteract()
        {
            var cell = Mover.Cell + Mover.Facing.ToVector();
            foreach (var hit in Physics2D.OverlapPointAll(GridMover.CellCenter(cell)))
            {
                var interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(this);
                    return;
                }
            }
            CellInteractables.At(cell)?.InteractCell(this, cell);
        }
    }
}
