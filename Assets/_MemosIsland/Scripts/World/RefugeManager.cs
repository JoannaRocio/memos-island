using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MemosIsland.World
{
    /// <summary>
    /// Refugio (exterior o interior): hace aparecer a los Memos que están de este lado de la puerta,
    /// les da lugares (escondites, camitas, comedero), suma amistad cuando comparten momentos y
    /// premia quedarse quieto cerca de los que todavía tienen miedo.
    /// </summary>
    public class RefugeManager : MonoBehaviour
    {
        [SerializeField] bool isInterior;
        [SerializeField] RectInt area = new(1, 1, 10, 6);
        [SerializeField] Vector2Int doorCell;
        [SerializeField] List<Vector2Int> hideSpots = new();
        [SerializeField] List<Vector2Int> bedSpots = new();
        [SerializeField] bool hasSoulmateSpot;
        [SerializeField] Vector2Int soulmateSpot;
        [SerializeField] Bowl bowl;
        [SerializeField] int maxGreeters = 2;
        [SerializeField] bool hasWorkSpots;
        [SerializeField] Vector2Int farmSpot, smelterSpot, processorSpot;

        const float RerollAfterMinutes = 15f;
        /// <summary>Las amistades tardan días reales en formarse, no minutos.</summary>
        const float FriendshipRate = 0.15f;

        public static RefugeManager Current { get; private set; }
        public bool IsInterior => isInterior;
        public Vector2Int DoorCell => doorCell;
        public Vector2Int PlayerCell => GameRoot.Instance.Player.Mover.Cell;
        public bool BowlHasFood => bowl != null && GameRoot.Instance.State.bowlServings > 0;
        public Vector2Int? BowlCell => bowl != null ? GridMover.WorldToCell(bowl.transform.position) : null;
        public IReadOnlyList<MemoLife> Memos => _lives;

        readonly List<MemoLife> _lives = new();
        readonly Queue<List<string>> _messages = new();
        int _greeters;
        float _playerStillSince;

        static long _lastRollTicks;

        public void Setup(bool interior, RectInt walkArea, Vector2Int door, List<Vector2Int> hides, List<Vector2Int> beds,
            Vector2Int? soulmate, Bowl foodBowl)
        {
            isInterior = interior;
            area = walkArea;
            doorCell = door;
            hideSpots = hides;
            bedSpots = beds;
            hasSoulmateSpot = soulmate.HasValue;
            soulmateSpot = soulmate ?? default;
            bowl = foodBowl;
        }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        IEnumerator Start()
        {
            while (GameRoot.Instance == null) yield return null;
            yield return null; // que el jugador ya esté ubicado
            var root = GameRoot.Instance;
            var state = root.State;
            var now = GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;

            foreach (var m in state.AllMemos) MemoNeeds.CatchUp(m, now);
            foreach (var (memo, result) in state.RunDailyVisit(now))
                if (result.newMemory) QueueMemory(memo, result.level);

            // Vida en la isla (Fase 6): pasan los días de la huerta, los envíos y el trabajo de los Memos.
            var report = IslandDay.Process(state, MemoDatabase.Instance, now);
            if (report.messages.Count > 0) _messages.Enqueue(new List<string>(report.messages));
            if (IslandDay.HasHelper(state, now, WorkRole.Water) || Weather.IsRainy(now)) // con lluvia también
            {
                var field = FindAnyObjectByType<FarmField>();
                if (field != null) field.WaterAllByMemos();
            }

            RollInsideOutside(state, now);
            foreach (var m in state.AllMemos.Where(m => m.uid != state.companionUid && m.inside == isInterior
                                                         && m.awayUntilTicks <= now.Ticks))
                Spawn(m, RandomFreeCellNear(PlayerCell, 8, minDistance: 2) ?? doorCell);

            root.Player.Mover.StepFinished += OnPlayerStep;
            _playerStillSince = Time.time;
            bowl?.Refresh();
            StartCoroutine(StayCloseLoop());
            StartCoroutine(MessageLoop());
        }

        void OnDestroy()
        {
            if (GameRoot.Instance != null) GameRoot.Instance.Player.Mover.StepFinished -= OnPlayerStep;
        }

        void OnPlayerStep(GridMover _) => _playerStillSince = Time.time;

        /// <summary>Cuando llegás de lejos, cada Memo decide si está adentro o afuera (de noche, casi todos adentro).</summary>
        static void RollInsideOutside(GameState state, DateTime now)
        {
            if (_lastRollTicks != 0 && (now - new DateTime(_lastRollTicks)).TotalMinutes < RerollAfterMinutes) return;
            _lastRollTicks = now.Ticks;
            bool night = GameClock.PhaseFor(GameClock.ToHourOfDay(now)) == DayPhase.Night;
            foreach (var m in state.AllMemos)
            {
                if (m.TrustLevel == TrustLevel.Hostile)
                {
                    // Los hostiles de día andan por ahí; vuelven de noche y se quedan afuera.
                    m.inside = false;
                    if (!night && Random.value < 0.6f) m.awayUntilTicks = NextEvening(now).Ticks;
                }
                else if (m.TrustLevel <= TrustLevel.Fear) m.inside = true; // los asustados se quedan adentro
                else m.inside = Random.value < (night ? 0.75f : 0.3f);
            }
        }

        /// <summary>Las 20:00 de hoy (o de mañana si ya pasaron).</summary>
        /// <summary>Dónde trabajan los Memos (huerta, fundición, procesadora). Solo en el exterior (Fase 6).</summary>
        public void SetWorkSpots(Vector2Int farm, Vector2Int smelter, Vector2Int processor)
        {
            hasWorkSpots = true;
            (farmSpot, smelterSpot, processorSpot) = (farm, smelter, processor);
        }

        public Vector2Int? WorkSpotFor(WorkRole role)
        {
            if (!hasWorkSpots) return null;
            return role switch
            {
                WorkRole.Smelt => smelterSpot,
                WorkRole.Process => processorSpot,
                WorkRole.None => null,
                _ => farmSpot,
            };
        }

        public static DateTime NextEvening(DateTime now)
        {
            var evening = now.Date.AddHours(20);
            return now < evening ? evening : evening.AddDays(1);
        }

        // ------------------------------------------------------------------ Actores

        public MemoLife Spawn(MemoInstance memo, Vector2Int cell)
        {
            var prefab = Resources.Load<GameObject>("MemoActor");
            // Se crea ya en su casilla: el GridMover registra su posición al activarse.
            var go = Instantiate(prefab, GridMover.CellToWorld(cell), Quaternion.identity, transform);
            var life = go.AddComponent<MemoLife>();
            life.Setup(memo, this);
            _lives.Add(life);
            return life;
        }

        public void Despawn(MemoLife life)
        {
            _lives.Remove(life);
            Destroy(life.gameObject);
        }

        /// <summary>Se llamó desde el menú de cuidados: aparece o desaparece el compañero.</summary>
        public void OnCompanionChanged()
        {
            var root = GameRoot.Instance;
            var companion = root.State.Companion;
            var asActor = _lives.Find(l => l.Memo == companion);
            Vector2Int? spawnAt = null;
            if (asActor != null)
            {
                spawnAt = asActor.Mover.Cell;
                Despawn(asActor);
            }
            // El que dejó de ser compañero se queda acá, donde estaba el seguidor.
            var former = root.Companion.Memo;
            if (former != null && former != companion && _lives.All(l => l.Memo != former))
            {
                former.inside = isInterior;
                Spawn(former, root.Companion.Cell);
            }
            root.Companion.Refresh(spawnAt);
        }

        public bool ShouldGreet(MemoLife life)
        {
            if (life.Memo.TrustLevel < TrustLevel.Friend || _greeters >= maxGreeters) return false;
            _greeters++;
            return true;
        }

        // ------------------------------------------------------------------ Lugares

        public Vector2Int? ClosestHideSpot(Vector2Int from) =>
            hideSpots.Where(c => c == from || GameRoot.Instance.Player.Mover.CanEnter(c))
                .OrderBy(c => GridPath.Manhattan(c, from)).Cast<Vector2Int?>().FirstOrDefault();

        public Vector2Int? BedFor(MemoLife life)
        {
            if (hasSoulmateSpot && life.Memo.TrustLevel == TrustLevel.Soulmate) return soulmateSpot;
            var free = bedSpots.Where(c => c == life.Mover.Cell || life.Mover.CanEnter(c)).ToList();
            if (free.Count > 0) return free.OrderBy(c => GridPath.Manhattan(c, life.Mover.Cell)).First();
            return RandomFreeCellNear(life.Mover.Cell, 3);
        }

        public Vector2Int? CellAwayFromPlayer(Vector2Int from, int radius)
        {
            Vector2Int? best = null;
            int bestDist = GridPath.Manhattan(from, PlayerCell);
            for (int i = 0; i < 12; i++)
            {
                var c = RandomFreeCellNear(from, radius);
                if (!c.HasValue) continue;
                int d = GridPath.Manhattan(c.Value, PlayerCell);
                if (d > bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        public Vector2Int? RandomFreeCellNear(Vector2Int from, int radius, int minDistance = 0)
        {
            var mover = GameRoot.Instance.Player.Mover;
            for (int i = 0; i < 25; i++)
            {
                var c = new Vector2Int(from.x + Random.Range(-radius, radius + 1), from.y + Random.Range(-radius, radius + 1));
                if (!area.Contains(c) || GridPath.Manhattan(c, from) < minDistance) continue;
                if (c != mover.Cell && mover.CanEnter(c)) return c;
            }
            return null;
        }

        // ------------------------------------------------------------------ Otros Memos

        public MemoLife ScaredGodchildOf(MemoLife life) =>
            _lives.Find(l => l.Memo.godparentUid == life.Memo.uid && l.Memo.TrustLevel <= TrustLevel.Distrust);

        public MemoLife ClosestOther(MemoLife life) =>
            _lives.Where(l => l != life && !l.IsAsleep)
                .OrderBy(l => GridPath.Manhattan(l.Mover.Cell, life.Mover.Cell)).FirstOrDefault();

        /// <summary>Prefiere jugar con amigos que estén despiertos y cerca.</summary>
        public MemoLife PlaymateFor(MemoLife life)
        {
            var rels = GameRoot.Instance.State.relationships;
            return _lives.Where(l => l != life && !l.IsAsleep && l.Memo.TrustLevel >= TrustLevel.Neutral
                                     && GridPath.Manhattan(l.Mover.Cell, life.Mover.Cell) <= 6)
                .OrderByDescending(l => Friendship.Points(rels, l.Memo.uid, life.Memo.uid) + Random.value * 20f)
                .FirstOrDefault();
        }

        /// <summary>Comer, dormir o jugar cerca de otros suma amistad (GDD §12).</summary>
        public void ShareMoment(MemoLife life, float amount, bool sleeping = false, bool playing = false)
        {
            amount *= FriendshipRate;
            var state = GameRoot.Instance.State;
            foreach (var other in _lives)
            {
                if (other == life || GridPath.Manhattan(other.Mover.Cell, life.Mover.Cell) > 2) continue;
                float bonus = (sleeping && other.IsAsleep ? 2f : 1f) * (playing && other.IsPlaying ? 2f : 1f);
                if (!Friendship.Add(state.relationships, life.Memo, other.Memo, amount * bonus)) continue;
                var level = Friendship.Level(state.relationships, life.Memo.uid, other.Memo.uid);
                if (level >= FriendshipLevel.Friends)
                {
                    life.Bubble.Show(Emote.Love, 2f);
                    other.Bubble.Show(Emote.Love, 2f);
                    _messages.Enqueue(new List<string>
                    {
                        level == FriendshipLevel.BestFriends
                            ? $"¡{life.Memo.DisplayName} y {other.Memo.DisplayName} ahora son mejores amigos! ♥ " +
                              "En las carreras, el relevo entre ellos les da un impulso."
                            : $"{life.Memo.DisplayName} y {other.Memo.DisplayName} se hicieron amigos. ♪",
                    });
                }
            }
        }

        // ------------------------------------------------------------------ Comedero y regalos

        public bool TakeServing()
        {
            var state = GameRoot.Instance.State;
            if (state.bowlServings <= 0) return false;
            state.bowlServings--;
            bowl?.Refresh();
            return true;
        }

        public void OfferGift(MemoLife life)
        {
            string[] gifts = { "una piedrita brillante", "una flor", "una pluma azul", "una conchita" };
            _messages.Enqueue(new List<string> { $"{life.Memo.DisplayName} te trajo {gifts[Random.Range(0, gifts.Length)]}. ♥" });
        }

        // ------------------------------------------------------------------ Mensajes y "quedarse cerca"

        public void QueueMemory(MemoInstance memo, TrustLevel level)
        {
            var pages = new List<string>();
            MemoCareMenu.AddMemoryPages(memo, level, pages);
            _messages.Enqueue(pages);
        }

        IEnumerator MessageLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (_messages.Count == 0 || GameRoot.InputLocked || GameRoot.Instance.Player.Mover.IsBusy) continue;
                GameRoot.Instance.Dialogue.Show(_messages.Dequeue());
            }
        }

        /// <summary>Quedarse quieto cerca de un Memo con miedo o desconfianza le da confianza de a poco.</summary>
        IEnumerator StayCloseLoop()
        {
            var now = (Func<DateTime>)(() => GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now);
            while (true)
            {
                yield return new WaitForSeconds(4f);
                if (GameRoot.InputLocked || Time.time - _playerStillSince < 3f) continue;
                foreach (var life in _lives.ToList())
                {
                    if (life.Memo.TrustLevel > TrustLevel.Distrust) continue;
                    if (GridPath.Manhattan(life.Mover.Cell, PlayerCell) > 2) continue;
                    var result = MemoCare.Apply(life.Memo, CareAction.StayClose, now());
                    if (!result.success) continue;
                    life.Bubble.Show(Random.value < 0.7f ? Emote.Dots : Emote.Love, 1.5f);
                    if (result.newMemory) QueueMemory(life.Memo, result.level);
                }
            }
        }
    }
}
