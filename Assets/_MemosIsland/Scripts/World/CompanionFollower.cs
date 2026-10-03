using System.Collections.Generic;
using MemosIsland.Core;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// El Memo compañero: te sigue por la isla unas casillas detrás, pisando el camino que vas dejando
    /// (los Memos miden 2 casillas de ancho: así no tapan al jugador),
    /// gana confianza mientras camina con vos (+2 por hora) y se puede cuidar con A.
    /// Vive en el GameRoot, así que pasa de mapa en mapa con vos.
    /// </summary>
    public class CompanionFollower : MonoBehaviour, IInteractable
    {
        [SerializeField] GridMover mover;
        [SerializeField] MemoSpriteView view;
        [SerializeField] EmoteBubble bubble;
        [SerializeField] Collider2D interactCollider;
        [Tooltip("A cuántas casillas detrás del jugador camina.")]
        [SerializeField, Range(1, 3)] int followDistance = 2;

        public MemoInstance Memo { get; private set; }
        public Vector2Int Cell => mover.Cell;

        readonly List<string> _pending = new();
        /// <summary>Casillas que dejó el jugador y que el compañero todavía tiene que pisar.</summary>
        readonly Queue<Vector2Int> _trail = new();
        float _emoteTimer = 12f;
        bool _hidden;

        public void Setup(GridMover gridMover, MemoSpriteView spriteView, EmoteBubble emoteBubble, Collider2D col)
        {
            mover = gridMover;
            view = spriteView;
            bubble = emoteBubble;
            interactCollider = col;
        }

        void Start()
        {
            GameRoot.Instance.Player.Mover.StepStarted += OnPlayerStepStarted;
            Refresh();
        }

        void OnDestroy()
        {
            if (GameRoot.Instance != null) GameRoot.Instance.Player.Mover.StepStarted -= OnPlayerStepStarted;
        }

        /// <summary>Vuelve a leer quién es el compañero y lo ubica detrás del jugador (o en "at").</summary>
        public void Refresh(Vector2Int? at = null)
        {
            Memo = GameRoot.Instance.State.Companion;
            bool visible = Memo != null && !_hidden;
            view.gameObject.SetActive(visible);
            interactCollider.enabled = visible;
            if (Memo == null) return;
            view.Setup(mover, Memo); // conecta la vista con el movimiento (dirección y animación)
            if (at.HasValue)
            {
                _trail.Clear();
                mover.Teleport(at.Value, mover.Facing);
            }
            else SnapBehind();
        }

        /// <summary>Se esconde durante las carreras y vuelve después.</summary>
        public void SetHidden(bool hidden)
        {
            _hidden = hidden;
            Refresh();
        }

        /// <summary>Lo ubica detrás del jugador, a la distancia que entre (si hay pared, más cerca).</summary>
        public void SnapBehind()
        {
            var player = GameRoot.Instance.Player.Mover;
            var back = player.Facing.ToVector();
            _trail.Clear();
            var cell = player.Cell;
            var between = new List<Vector2Int>();
            for (int i = 1; i <= followDistance; i++)
            {
                var c = player.Cell - back * i;
                if (!player.CanEnter(c)) break;
                if (i > 1) between.Add(cell);
                cell = c;
            }
            // Las casillas entre el compañero y el jugador quedan como camino pendiente (de la más lejana a la más cercana).
            between.Reverse();
            foreach (var c in between) _trail.Enqueue(c);
            mover.Teleport(cell, player.Facing);
        }

        void OnPlayerStepStarted(GridMover player, Vector2Int from)
        {
            if (Memo == null || _hidden) return;
            _trail.Enqueue(from);
        }

        /// <summary>Avanza por el camino del jugador manteniendo la distancia.</summary>
        void FollowTrail()
        {
            var player = GameRoot.Instance.Player.Mover;
            mover.WalkSpeed = player.WalkSpeed;
            if (mover.IsBusy || _trail.Count < followDistance) return;
            var next = _trail.Dequeue();
            if (GridPath.Manhattan(mover.Cell, next) > 1) mover.Teleport(next, player.Facing);
            else mover.ForceStep(next, player.IsRunning);
        }

        void Update()
        {
            if (Memo == null || _hidden || GameRoot.Instance.Maps.IsTransitioning) return;
            FollowTrail();

            // Quieto, mira hacia donde mira el jugador (si el jugador gira en el lugar, el compañero también).
            var player = GameRoot.Instance.Player.Mover;
            if (!mover.IsMoving && !player.IsMoving && !GameRoot.InputLocked) mover.Facing = player.Facing;

            MemoNeeds.Decay(Memo, Time.deltaTime / 3600f);
            MemoNeeds.Satisfy(Memo, Need.Social, Time.deltaTime / 60f); // con vos no se siente solo
            var result = MemoCare.AddFollowTime(Memo, Time.deltaTime);
            if (result.newMemory) MemoCareMenu.AddMemoryPages(Memo, result.level, _pending);

            if (_pending.Count > 0 && !GameRoot.InputLocked && !GameRoot.Instance.Player.Mover.IsBusy)
            {
                GameRoot.Instance.Dialogue.Show(new List<string>(_pending));
                _pending.Clear();
            }

            _emoteTimer -= Time.deltaTime;
            if (_emoteTimer <= 0f)
            {
                _emoteTimer = Random.Range(15f, 30f);
                bubble.Show(Memo.mood >= 60 ? (Random.value < 0.5f ? Emote.Music : Emote.Love) : Emote.Sad);
            }
        }

        public void Interact(PlayerController player)
        {
            mover.Facing = (player.Mover.Cell - mover.Cell).x < 0 ? Direction.Left : Direction.Right;
            MemoCareMenu.Open(Memo, e => bubble.Show(e), OnCompanionChanged);
        }

        void OnCompanionChanged()
        {
            if (RefugeManager.Current != null) RefugeManager.Current.OnCompanionChanged();
            else Refresh();
        }
    }
}
