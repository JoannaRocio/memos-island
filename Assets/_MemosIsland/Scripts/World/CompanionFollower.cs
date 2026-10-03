using System.Collections.Generic;
using MemosIsland.Core;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// El Memo compañero: te sigue por la isla un paso detrás (pisa la casilla de la que salís),
    /// gana confianza mientras camina con vos (+2 por hora) y se puede cuidar con A.
    /// Vive en el GameRoot, así que pasa de mapa en mapa con vos.
    /// </summary>
    public class CompanionFollower : MonoBehaviour, IInteractable
    {
        [SerializeField] GridMover mover;
        [SerializeField] MemoSpriteView view;
        [SerializeField] EmoteBubble bubble;
        [SerializeField] Collider2D interactCollider;

        public MemoInstance Memo { get; private set; }
        public Vector2Int Cell => mover.Cell;

        readonly List<string> _pending = new();
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
            if (at.HasValue) mover.Teleport(at.Value, mover.Facing);
            else SnapBehind();
        }

        /// <summary>Se esconde durante las carreras y vuelve después.</summary>
        public void SetHidden(bool hidden)
        {
            _hidden = hidden;
            Refresh();
        }

        public void SnapBehind()
        {
            var player = GameRoot.Instance.Player.Mover;
            var behind = player.Cell - player.Facing.ToVector();
            var cell = player.CanEnter(behind) ? behind : player.Cell;
            mover.Teleport(cell, player.Facing);
        }

        void OnPlayerStepStarted(GridMover player, Vector2Int from)
        {
            if (Memo == null || _hidden) return;
            mover.WalkSpeed = player.WalkSpeed;
            if (GridPath.Manhattan(mover.Cell, from) > 1)
            {
                mover.Teleport(from, player.Facing);
                return;
            }
            mover.ForceStep(from, player.IsRunning);
        }

        void Update()
        {
            if (Memo == null || _hidden || GameRoot.Instance.Maps.IsTransitioning) return;

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
