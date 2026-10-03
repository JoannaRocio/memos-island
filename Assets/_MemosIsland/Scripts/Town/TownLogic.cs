using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;

namespace MemosIsland.Town
{
    /// <summary>Dónde está cada vecino según el día, la hora y el clima (GDD §5 "Rutinas").</summary>
    public static class NeighborSchedule
    {
        public static bool Matches(Days days, DayOfWeek day) => (days & (Days)(1 << (int)day)) != 0;

        static bool Matches(WeatherFilter filter, WeatherKind weather) =>
            filter == WeatherFilter.Any || (filter == WeatherFilter.Rainy) == (weather == WeatherKind.Rainy);

        /// <summary>
        /// La entrada vigente: la última que empezó hoy antes de esta hora y vale para el día y el clima.
        /// A la misma hora gana la que pide un clima concreto. Null = está en su casa.
        /// </summary>
        public static ScheduleEntry Resolve(IEnumerable<ScheduleEntry> schedule, DateTime now, WeatherKind weather)
        {
            float hour = GameClock.ToHourOfDay(now);
            ScheduleEntry best = null;
            foreach (var e in schedule)
            {
                if (e == null || e.hour > hour || !Matches(e.days, now.DayOfWeek) || !Matches(e.weather, weather)) continue;
                if (best == null || e.hour > best.hour || e.hour == best.hour && e.weather != WeatherFilter.Any)
                    best = e;
            }
            return best == null || best.AtHome ? null : best;
        }

        public static ScheduleEntry Resolve(NeighborData n, DateTime now) => Resolve(n.schedule, now, Weather.For(now));

        /// <summary>Texto de horario para carteles: "9:00 a 17:00".</summary>
        public static string HoursText(NeighborData n, string activity, DateTime day)
        {
            var entries = n.schedule.Where(e => Matches(e.days, day.DayOfWeek)).OrderBy(e => e.hour).ToList();
            var start = entries.FirstOrDefault(e => e.activity == activity);
            if (start == null) return "";
            var end = entries.FirstOrDefault(e => e.hour > start.hour && e.activity != activity && e.weather == WeatherFilter.Any);
            static string H(float h) => $"{(int)h}:{(int)Math.Round((h - (int)h) * 60):00}";
            return end == null ? $"desde las {H(start.hour)}" : $"de {H(start.hour)} a {H(end.hour)}";
        }
    }

    public enum GiftReaction { Loved, Liked, Neutral, Disliked, AlreadyToday }

    /// <summary>Amistad con los vecinos: 0 a 10 corazones (GDD §5).</summary>
    public static class NeighborFriendship
    {
        public const int PointsPerHeart = 100;
        public const int MaxHearts = 10;
        public const int MaxPoints = PointsPerHeart * MaxHearts;
        public const int TalkPoints = 20;
        public const int LovedPoints = 80, LikedPoints = 45, NeutralPoints = 20, DislikedPoints = -20;
        public const int RequestPoints = 60;

        public static int Hearts(NeighborState s) => Math.Min(MaxHearts, Math.Max(0, s.points) / PointsPerHeart);

        public static void Add(NeighborState s, int points) => s.points = Math.Clamp(s.points + points, 0, MaxPoints);

        /// <summary>Charla diaria: suma solo la primera vez del día. Devuelve true si sumó.</summary>
        public static bool Talk(NeighborState s, DateTime now)
        {
            s.met = true;
            var today = IslandState.DateKey(now);
            if (s.talkedOn == today) return false;
            s.talkedOn = today;
            Add(s, TalkPoints);
            return true;
        }

        public static GiftReaction ReactionTo(NeighborData n, string itemId)
        {
            if (n.loved.Contains(itemId)) return GiftReaction.Loved;
            if (n.liked.Contains(itemId)) return GiftReaction.Liked;
            if (n.disliked.Contains(itemId)) return GiftReaction.Disliked;
            return GiftReaction.Neutral;
        }

        /// <summary>Un regalo por día y por vecino. No consume el objeto (lo hace quien llama).</summary>
        public static GiftReaction Gift(NeighborData n, NeighborState s, string itemId, DateTime now)
        {
            var today = IslandState.DateKey(now);
            if (s.giftedOn == today) return GiftReaction.AlreadyToday;
            s.giftedOn = today;
            s.met = true;
            var reaction = ReactionTo(n, itemId);
            Add(s, reaction switch
            {
                GiftReaction.Loved => LovedPoints,
                GiftReaction.Liked => LikedPoints,
                GiftReaction.Disliked => DislikedPoints,
                _ => NeutralPoints,
            });
            return reaction;
        }

        /// <summary>El próximo evento de amistad que corresponde ver (o null).</summary>
        public static FriendshipEvent PendingEvent(NeighborData n, NeighborState s, string scene = null) =>
            n.events.Where(e => e != null && e.hearts <= Hearts(s) && !s.seenEvents.Contains(e.id)
                                && (string.IsNullOrEmpty(e.scene) || e.scene == scene))
                .OrderBy(e => e.hearts).FirstOrDefault();

        /// <summary>Las líneas de charla según los corazones.</summary>
        public static List<string> LinesFor(NeighborData n, NeighborState s)
        {
            int h = Hearts(s);
            var lines = h >= 7 ? n.linesHigh : h >= 3 ? n.linesMid : n.linesLow;
            if (lines.Count == 0) lines = n.linesLow;
            return lines;
        }
    }

    /// <summary>Tablón de pedidos (GDD §15): hasta 3 pedidos por día real, de vecinos distintos.</summary>
    public static class RequestBoard
    {
        public const int PerDay = 3;

        public static void Refresh(TownState town, IList<NeighborData> neighbors, DateTime now)
        {
            var today = IslandState.DateKey(now);
            if (town.boardDate == today) return;
            town.boardDate = today;
            town.board.Clear();
            var rng = new Random(Mining.Seed(today, 4242));
            var pool = neighbors.Where(n => n != null && n.requests.Count > 0).OrderBy(n => n.id).ToList();
            foreach (var n in pool.OrderBy(_ => rng.Next()).Take(PerDay))
                town.board.Add(new DailyRequest { neighborId = n.id, template = rng.Next(n.requests.Count) });
        }

        public static RequestTemplate TemplateOf(DailyRequest r, NeighborData n) =>
            n != null && r.template >= 0 && r.template < n.requests.Count ? n.requests[r.template] : null;

        /// <summary>El pedido aceptado y sin terminar de este vecino (o null).</summary>
        public static DailyRequest ActiveFor(TownState town, string neighborId) =>
            town.board.Find(r => r.neighborId == neighborId && r.accepted && !r.done);

        public static bool CanComplete(RequestTemplate t, GameState state) => t.kind switch
        {
            RequestKind.Deliver => state.CountOf(t.targetId) >= t.count,
            RequestKind.ShowMemo => state.Companion != null && state.Companion.speciesId == t.targetId,
            _ => false,
        };

        /// <summary>Entrega el pedido: saca los objetos, paga y suma amistad.</summary>
        public static bool Complete(DailyRequest r, RequestTemplate t, GameState state, NeighborState friend)
        {
            if (r.done || !r.accepted || !CanComplete(t, state)) return false;
            if (t.kind == RequestKind.Deliver) state.RemoveItem(t.targetId, t.count);
            state.island.money += t.reward;
            NeighborFriendship.Add(friend, NeighborFriendship.RequestPoints);
            r.done = true;
            return true;
        }
    }
}
