using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Minijuego de calma (GDD §4, Acto 2): no hay combate. Quedarse quieto lo calma; acercarse despacio está bien;
    /// correr o moverse rápido lo asusta y retrocede. Cuando está tranquilo y estás al lado, A le ofrece comida.
    /// </summary>
    public class CalmMinigame : MonoBehaviour, IInteractable
    {
        public const float StillGain = 14f;      // calma por segundo quieto
        public const float RushLoss = 30f;       // correr o dar pasos muy seguidos
        public const float ApproachMin = 35f;    // por debajo de esto, acercarse también lo asusta
        public const float OfferMin = 70f;       // para poder ofrecerle comida
        const float StepGap = 0.9f;              // pasos más seguidos que esto cuentan como "rápido"

        public float Calm { get; private set; } = 15f;

        MemoInstance _memo;
        GridMover _mover;
        EmoteBubble _bubble;
        MemoSpriteView _view;
        RectInt _area;
        Action<string> _onCalmed;
        GridMover _player;
        float _lastStep = -10f, _emoteTimer;
        bool _done;

        public void Setup(MemoInstance memo, RectInt area, Action<string> onCalmed)
        {
            _memo = memo;
            _area = area;
            _onCalmed = onCalmed;
            _mover = GetComponent<GridMover>();
            _bubble = GetComponentInChildren<EmoteBubble>();
            _view = GetComponentInChildren<MemoSpriteView>();
            _player = GameRoot.Instance.Player.Mover;
            _player.StepStarted += OnPlayerStep;
        }

        void OnDestroy()
        {
            if (_player != null) _player.StepStarted -= OnPlayerStep;
        }

        /// <summary>Lógica pura del paso (con tests): cuánto cambia la calma.</summary>
        public static float StepDelta(float calm, bool running, float secondsSinceLastStep, bool closer)
        {
            if (running || secondsSinceLastStep < StepGap) return -RushLoss;
            if (closer && calm < ApproachMin) return -10f;
            return 0f;
        }

        void OnPlayerStep(GridMover player, Vector2Int cell)
        {
            if (_done) return;
            float since = Time.time - _lastStep;
            _lastStep = Time.time;
            bool closer = GridPath.Manhattan(cell, _mover.Cell) < GridPath.Manhattan(player.Cell, _mover.Cell)
                          || GridPath.Manhattan(cell, _mover.Cell) <= 2;
            float delta = StepDelta(Calm, player.IsRunning, since, closer);
            if (delta >= 0f) return;
            Calm = Mathf.Max(0f, Calm + delta);
            _bubble.Show(Emote.Angry, 1f);
            Retreat(cell);
        }

        void Update()
        {
            if (_done || _memo == null) return;
            _view.Trembling = Calm < OfferMin;
            if (!_player.IsMoving && Time.time - _lastStep > 0.8f && !GameRoot.InputLocked)
                Calm = Mathf.Min(100f, Calm + StillGain * Time.deltaTime);
            if ((_emoteTimer -= Time.deltaTime) <= 0f)
            {
                _emoteTimer = 2.5f;
                _bubble.Show(Calm < ApproachMin ? Emote.Angry : Calm < OfferMin ? Emote.Scared : Emote.Question, 1.2f);
            }
        }

        /// <summary>Se aleja un paso del jugador (dentro del claro). Si no puede, tiembla en el lugar.</summary>
        void Retreat(Vector2Int playerCell)
        {
            var away = _mover.Cell - playerCell;
            var options = new List<Direction>();
            if (Mathf.Abs(away.y) >= Mathf.Abs(away.x)) options.Add(away.y >= 0 ? Direction.Up : Direction.Down);
            options.Add(away.x >= 0 ? Direction.Right : Direction.Left);
            options.Add(away.x >= 0 ? Direction.Left : Direction.Right);
            foreach (var d in options)
            {
                var c = _mover.Cell + d.ToVector();
                if (!_area.Contains(c) || !_mover.CanEnter(c)) continue;
                _mover.TryStep(d, true);
                return;
            }
            _mover.Bump(options[0]);
        }

        public void Interact(PlayerController player)
        {
            if (_done) return;
            var root = GameRoot.Instance;
            _mover.Facing = DirectionTo(player.Mover.Cell);
            if (Calm < OfferMin)
            {
                Calm = Mathf.Max(0f, Calm - 10f);
                _bubble.Show(Emote.Angry, 1.2f);
                root.Dialogue.Show(new[] { $"({_memo.DisplayName} te gruñe y se encoge. Todavía no confía. Quedate quiet{{o/a/e}} un ratito más.)" });
                return;
            }

            var foods = root.State.inventory.Select(s => MemoDatabase.Instance.GetItem(s.id))
                .Where(i => i != null && i.kind == ItemKind.Food).ToList();
            var labels = foods.Select(f => $"{f.displayName} ×{root.State.CountOf(f.id)}").ToList();
            labels.Add("Extender la mano");
            root.Dialogue.ShowChoice($"({_memo.DisplayName} te mira. Ya no gruñe.) ¿Qué le ofrecés?", labels, i =>
            {
                _done = true;
                _view.Trembling = false;
                string food = null;
                if (i < foods.Count)
                {
                    food = foods[i].id;
                    root.State.RemoveItem(food);
                }
                _onCalmed?.Invoke(food);
            });
        }

        Direction DirectionTo(Vector2Int cell)
        {
            var d = cell - _mover.Cell;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return d.x >= 0 ? Direction.Right : Direction.Left;
            return d.y >= 0 ? Direction.Up : Direction.Down;
        }
    }
}
