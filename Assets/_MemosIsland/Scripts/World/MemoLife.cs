using System.Collections;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Vida de un Memo en el refugio (GDD §12). Elige qué hacer según su confianza, sus necesidades,
    /// su personalidad, la hora y quién anda cerca: esconderse, observarte de lejos, comer, dormir,
    /// jugar (solo o con un amigo), buscar compañía, seguirte, acompañar a su ahijado o entrar/salir de la casa.
    /// </summary>
    public class MemoLife : MonoBehaviour, IInteractable
    {
        public MemoInstance Memo { get; private set; }
        public GridMover Mover { get; private set; }
        public MemoSpriteView View { get; private set; }
        public EmoteBubble Bubble { get; private set; }
        public bool IsAsleep { get; private set; }
        public bool IsPlaying { get; private set; }
        public string ActivityName { get; private set; } = "";

        RefugeManager _refuge;
        bool _busyWithPlayer;

        public void Setup(MemoInstance memo, RefugeManager refuge)
        {
            Memo = memo;
            _refuge = refuge;
            Mover = GetComponent<GridMover>();
            View = GetComponentInChildren<MemoSpriteView>();
            Bubble = GetComponentInChildren<EmoteBubble>();
            View.Setup(Mover, memo);
            Mover.WalkSpeed = 2.5f;
            name = $"Memo {memo.DisplayName}";
        }

        void Start() => StartCoroutine(Live());

        void Update()
        {
            MemoNeeds.Decay(Memo, Time.deltaTime / 3600f, IsAsleep);
            View.Trembling = Memo.TrustLevel == TrustLevel.Fear && PlayerDistance() <= 3;
        }

        public void Interact(PlayerController player)
        {
            _busyWithPlayer = true;
            Mover.Facing = DirectionTo(player.Mover.Cell);
            if (IsAsleep) WakeUp();
            MemoCareMenu.Open(Memo, e => Bubble.Show(e), () => _refuge.OnCompanionChanged());
            StartCoroutine(ReleaseWhenDialogueCloses());
        }

        IEnumerator ReleaseWhenDialogueCloses()
        {
            yield return null;
            while (GameRoot.InputLocked) yield return null;
            _busyWithPlayer = false;
        }

        // ------------------------------------------------------------------ Bucle de vida

        IEnumerator Live()
        {
            yield return new WaitForSeconds(Random.Range(0.1f, 1.2f));
            if (_refuge.ShouldGreet(this)) yield return Greet();

            while (true)
            {
                while (_busyWithPlayer) yield return null;
                yield return Decide();
                yield return new WaitForSeconds(Random.Range(0.4f, 1.6f));
            }
        }

        IEnumerator Decide()
        {
            var level = Memo.TrustLevel;
            int playerDist = PlayerDistance();
            bool night = GameClock.Instance != null && GameClock.Instance.Phase == DayPhase.Night;

            // Hostil (legendarios): gruñe, mantiene distancia y de día se va del refugio.
            if (level == TrustLevel.Hostile)
            {
                if (!night && Random.value < 0.25f) return LeaveForTheDay();
                if (playerDist <= 4) return Growl();
                if (Memo.hunger < 50f && _refuge.BowlHasFood && playerDist > 5) return Eat();
                return Wander(3);
            }

            // Miedo: se esconde si estás cerca; si no, a veces se anima a comer.
            if (level <= TrustLevel.Fear)
            {
                if (playerDist <= 4) return Hide();
                if (Memo.hunger < 60f && _refuge.BowlHasFood) return Eat();
                return Random.value < 0.6f ? Hide() : Wander(2);
            }
            // Desconfianza: te observa de lejos.
            if (level == TrustLevel.Distrust && playerDist <= 3) return KeepDistance();

            // Padrino: acompaña a su ahijado asustado.
            var godchild = _refuge.ScaredGodchildOf(this);
            if (godchild != null && Random.value < 0.6f) return StayWith(godchild);

            if (Memo.hunger < 45f && _refuge.BowlHasFood) return Eat();
            if (Memo.sleep < 30f || night && Random.value < 0.35f) return Sleep();

            // Entrar o salir de la casa.
            if (Random.value < (night ? 0.12f : 0.05f) && !_refuge.IsInterior) return ChangeSide(true);
            if (Random.value < (night ? 0.02f : 0.08f) && _refuge.IsInterior) return ChangeSide(false);

            if (!night && Random.value < 0.3f && WorkSpot().HasValue) return Work();
            if (level >= TrustLevel.Trusting && playerDist <= 7 && Random.value < 0.3f) return FollowPlayer();
            if (Memo.fun < 60f || Random.value < 0.2f) return Play();
            if (Memo.social < 60f || Random.value < 0.25f) return Socialize();
            return Wander(4);
        }

        // ------------------------------------------------------------------ Conductas

        /// <summary>Lugar de trabajo de este Memo, si hoy trabaja (confianza y ánimo suficientes, GDD §15).</summary>
        Vector2Int? WorkSpot()
        {
            var now = GameClock.Instance != null ? GameClock.Instance.Now : System.DateTime.Now;
            if (!IslandDay.Workers(GameRoot.Instance.State, now).Contains(Memo)) return null;
            return _refuge.WorkSpotFor(IslandDay.RoleOf(Memo));
        }

        IEnumerator Work()
        {
            var role = IslandDay.RoleOf(Memo);
            ActivityName = IslandDay.RoleName(role);
            yield return WalkNear(WorkSpot().Value, 1);
            for (int i = 0; i < 3; i++)
            {
                Bubble.Show(i == 1 ? Emote.Love : Emote.Music, 1.2f);
                yield return new WaitForSeconds(Random.Range(1.5f, 2.5f));
            }
        }

        IEnumerator Greet()
        {
            ActivityName = "saludar";
            yield return WalkNear(_refuge.PlayerCell, 1);
            Mover.Facing = DirectionTo(_refuge.PlayerCell);
            Bubble.Show(Emote.Love, 2.5f);
            View.Hopping = true;
            yield return new WaitForSeconds(1.2f);
            View.Hopping = false;
            if (Memo.TrustLevel >= TrustLevel.Friend && Random.value < 0.4f)
                _refuge.OfferGift(this);
        }

        IEnumerator Hide()
        {
            ActivityName = "esconderse";
            var spot = _refuge.ClosestHideSpot(Mover.Cell);
            if (spot.HasValue) yield return WalkTo(spot.Value, run: true);
            if (Random.value < 0.4f) Bubble.Show(Emote.Scared);
            yield return new WaitForSeconds(Random.Range(2f, 4f));
        }

        IEnumerator Growl()
        {
            ActivityName = "gruñir";
            Mover.Facing = DirectionTo(_refuge.PlayerCell);
            Bubble.Show(Emote.Angry, 1.5f);
            yield return new WaitForSeconds(0.8f);
            var away = _refuge.CellAwayFromPlayer(Mover.Cell, 5);
            if (away.HasValue) yield return WalkTo(away.Value);
            yield return new WaitForSeconds(Random.Range(1.5f, 3f));
        }

        /// <summary>Los hostiles salen por la puerta (o se alejan) y no vuelven hasta la noche.</summary>
        IEnumerator LeaveForTheDay()
        {
            ActivityName = "irse";
            yield return WalkTo(_refuge.DoorCell, run: true);
            View.FadeTo(0f);
            while (!View.FadeDone) yield return null;
            Memo.awayUntilTicks = RefugeManager.NextEvening(Now()).Ticks;
            Memo.inside = false;
            _refuge.Despawn(this);
        }

        IEnumerator KeepDistance()
        {
            ActivityName = "observarte";
            var away = _refuge.CellAwayFromPlayer(Mover.Cell, 4);
            if (away.HasValue) yield return WalkTo(away.Value);
            Mover.Facing = DirectionTo(_refuge.PlayerCell);
            Bubble.Show(Random.value < 0.5f ? Emote.Question : Emote.Dots);
            yield return new WaitForSeconds(Random.Range(1.5f, 3f));
        }

        IEnumerator Eat()
        {
            ActivityName = "comer";
            var bowl = _refuge.BowlCell;
            if (!bowl.HasValue) yield break;
            yield return WalkNear(bowl.Value, 1);
            if (GridPath.Manhattan(Mover.Cell, bowl.Value) > 1) yield break;
            // Los que tienen miedo no comen si los estás mirando.
            if (Memo.TrustLevel <= TrustLevel.Fear && PlayerDistance() <= 3) yield break;
            Mover.Facing = DirectionTo(bowl.Value);
            yield return new WaitForSeconds(2.5f);
            if (_refuge.TakeServing())
            {
                var result = MemoCare.Apply(Memo, CareAction.Feed, Now());
                Bubble.Show(Emote.Music);
                _refuge.ShareMoment(this, 1f);
                if (result.newMemory) _refuge.QueueMemory(Memo, result.level);
            }
        }

        IEnumerator Sleep()
        {
            ActivityName = "dormir";
            var bed = _refuge.BedFor(this);
            if (bed.HasValue) yield return WalkTo(bed.Value);
            IsAsleep = true;
            View.Asleep = true;
            float duration = Random.Range(10f, 20f);
            for (float t = 0; t < duration && IsAsleep; t += 3f)
            {
                Bubble.Show(Emote.Sleep, 2f);
                _refuge.ShareMoment(this, 0.5f, sleeping: true);
                yield return new WaitForSeconds(3f);
            }
            WakeUp();
        }

        void WakeUp()
        {
            IsAsleep = false;
            View.Asleep = false;
        }

        IEnumerator Play()
        {
            ActivityName = "jugar";
            var friend = _refuge.PlaymateFor(this);
            IsPlaying = true;
            if (friend != null)
            {
                // Se persiguen un rato.
                for (int i = 0; i < 4; i++)
                {
                    yield return WalkNear(friend.Mover.Cell, 1, run: true, maxSteps: 4);
                    Bubble.Show(Emote.Music, 1f);
                    _refuge.ShareMoment(this, 2f, playing: true);
                    yield return Wander(2, run: true);
                }
            }
            else
            {
                View.Hopping = true;
                Bubble.Show(Emote.Music);
                for (int i = 0; i < 4; i++)
                {
                    Mover.Facing = (Direction)Random.Range(0, 4);
                    yield return new WaitForSeconds(0.5f);
                }
                View.Hopping = false;
            }
            IsPlaying = false;
            MemoNeeds.Satisfy(Memo, Need.Fun, 15f);
        }

        IEnumerator Socialize()
        {
            ActivityName = "buscar compañía";
            var other = _refuge.ClosestOther(this);
            if (other == null) yield return Wander(3);
            else
            {
                yield return WalkNear(other.Mover.Cell, 1);
                Mover.Facing = DirectionTo(other.Mover.Cell);
                _refuge.ShareMoment(this, 1.5f);
                MemoNeeds.Satisfy(Memo, Need.Social, 15f);
                if (Random.value < 0.5f) Bubble.Show(Emote.Love, 1.2f);
                yield return new WaitForSeconds(Random.Range(1.5f, 3f));
            }
        }

        IEnumerator FollowPlayer()
        {
            ActivityName = "seguirte";
            float until = Time.time + Random.Range(6f, 10f);
            while (Time.time < until)
            {
                if (PlayerDistance() > 1) yield return WalkNear(_refuge.PlayerCell, 1, maxSteps: 3);
                else
                {
                    Mover.Facing = DirectionTo(_refuge.PlayerCell);
                    if (Random.value < 0.2f) Bubble.Show(Emote.Love, 1f);
                    yield return new WaitForSeconds(0.6f);
                }
            }
        }

        IEnumerator StayWith(MemoLife godchild)
        {
            ActivityName = "acompañar";
            yield return WalkNear(godchild.Mover.Cell, 1);
            Mover.Facing = DirectionTo(godchild.Mover.Cell);
            Bubble.Show(Emote.Love, 1.5f);
            _refuge.ShareMoment(this, 1f);
            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }

        IEnumerator ChangeSide(bool goInside)
        {
            ActivityName = goInside ? "entrar a la casa" : "salir al patio";
            yield return WalkTo(_refuge.DoorCell);
            if (Mover.Cell != _refuge.DoorCell) yield break;
            View.FadeTo(0f);
            while (!View.FadeDone) yield return null;
            Memo.inside = goInside;
            _refuge.Despawn(this);
        }

        IEnumerator Wander(int radius, bool run = false)
        {
            ActivityName = "pasear";
            var target = _refuge.RandomFreeCellNear(Mover.Cell, radius);
            if (target.HasValue) yield return WalkTo(target.Value, run, maxSteps: radius * 2);
        }

        // ------------------------------------------------------------------ Movimiento

        IEnumerator WalkNear(Vector2Int target, int distance, bool run = false, int maxSteps = 30)
        {
            if (GridPath.Manhattan(Mover.Cell, target) <= distance) yield break;
            var path = GridPath.Find(Mover, Mover.Cell, target, adjacentIsEnough: true);
            yield return FollowPath(path, run, maxSteps);
        }

        IEnumerator WalkTo(Vector2Int target, bool run = false, int maxSteps = 30)
        {
            var path = GridPath.Find(Mover, Mover.Cell, target);
            yield return FollowPath(path, run, maxSteps);
        }

        IEnumerator FollowPath(System.Collections.Generic.List<Direction> path, bool run, int maxSteps)
        {
            int steps = 0;
            foreach (var dir in path)
            {
                if (_busyWithPlayer || steps++ >= maxSteps) yield break;
                while (Mover.IsBusy) yield return null;
                if (!Mover.TryStep(dir, run))
                {
                    // Algo se cruzó (el jugador u otro Memo): espera un poquito y abandona.
                    Mover.Bump(dir);
                    yield return new WaitForSeconds(0.4f);
                    yield break;
                }
                while (Mover.IsMoving) yield return null;
            }
        }

        // ------------------------------------------------------------------ Ayudas

        int PlayerDistance() => GridPath.Manhattan(Mover.Cell, _refuge.PlayerCell);

        Direction DirectionTo(Vector2Int cell)
        {
            var d = cell - Mover.Cell;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return d.x >= 0 ? Direction.Right : Direction.Left;
            return d.y >= 0 ? Direction.Up : Direction.Down;
        }

        static System.DateTime Now() => GameClock.Instance != null ? GameClock.Instance.Now : System.DateTime.Now;
    }
}
