using System;
using UnityEngine;

namespace MemosIsland.Memos
{
    public enum CareAction { Feed, Pet, Play, Bath, StayClose }

    public struct CareResult
    {
        public bool success;
        public int trustGained;
        public string message;
        /// <summary>Si subió a un nivel de confianza nunca alcanzado: recupera un recuerdo.</summary>
        public bool newMemory;
        public TrustLevel level;
    }

    /// <summary>
    /// Cuidados y confianza (GDD §11). Los límites diarios usan el día real (el reloj del juego es el real).
    /// Los Memos con miedo (u hostiles) solo aceptan comida y que te quedes cerca.
    /// </summary>
    public static class MemoCare
    {
        public const int FeedTrust = 5, FavoriteFeedTrust = 15, FeedsPerDay = 2;
        public const int PetTrust = 5, PetTrustDistrust = 3;
        public const int PlayTrust = 6, BathTrust = 8, BathEveryDays = 3;
        public const int StayCloseTrust = 2, StayCloseMaxPerDay = 20;
        public const int RefugeDayTrust = 1, GodparentDayTrust = 3;
        public const int RaceTrust = 3, RaceWinTrust = 6;
        public const int FollowTrustPerHour = 2;

        public static CareResult Apply(MemoInstance m, CareAction action, DateTime now, bool favoriteFood = false)
        {
            ResetDayIfNeeded(m, now);
            var level = m.TrustLevel;
            bool scared = level <= TrustLevel.Fear;
            string name = m.DisplayName;

            switch (action)
            {
                case CareAction.Feed:
                    MemoNeeds.Satisfy(m, Need.Hunger, 40f);
                    if (m.feedsToday >= FeedsPerDay)
                        return Fail($"{name} ya comió suficiente por hoy.");
                    m.feedsToday++;
                    return Gain(m, favoriteFood ? FavoriteFeedTrust : FeedTrust,
                        scared ? $"{name} esperó a que te alejaras… y comió." :
                        favoriteFood ? $"¡A {name} le encantó! ♥" : $"{name} comió con ganas.");

                case CareAction.Pet:
                    if (scared) return Fail($"{name} retrocede asustado…");
                    if (m.pettedToday) return Fail($"{name} ya recibió mimos hoy. Igual se ve contento.");
                    m.pettedToday = true;
                    MemoNeeds.Satisfy(m, Need.Social, 25f);
                    return level == TrustLevel.Distrust
                        ? Gain(m, PetTrustDistrust, $"{name} se dejó tocar un poquito.")
                        : Gain(m, PetTrust, $"{name} cierra los ojos mientras lo acariciás. ♥");

                case CareAction.Play:
                    if (scared) return Fail($"{name} no quiere jugar. Te mira desde lejos.");
                    MemoNeeds.Satisfy(m, Need.Fun, 40f);
                    if (m.playedToday) return Fail($"{name} jugó un rato, pero ya está cansado de la pelota.");
                    m.playedToday = true;
                    return Gain(m, PlayTrust, $"¡{name} corre detrás de la pelota! ♪");

                case CareAction.Bath:
                    if (scared) return Fail($"{name} se escapa del agua.");
                    if (!string.IsNullOrEmpty(m.lastBathDate) &&
                        (now.Date - DateTime.Parse(m.lastBathDate)).TotalDays < BathEveryDays)
                        return Fail($"{name} todavía está limpito.");
                    m.lastBathDate = now.Date.ToString("yyyy-MM-dd");
                    return Gain(m, BathTrust, $"{name} quedó brillante y perfumado. ♪");

                case CareAction.StayClose:
                    // Solo sirve con los que todavía no confían.
                    if (level > TrustLevel.Distrust || m.nearPointsToday >= StayCloseMaxPerDay)
                        return Fail(null);
                    m.nearPointsToday += StayCloseTrust;
                    return Gain(m, StayCloseTrust, null);
            }
            return Fail(null);
        }

        /// <summary>Lo que se gana por cada día en el refugio (y por tener padrino).</summary>
        public static CareResult DailyVisit(MemoInstance m, bool hasGodparent) =>
            Gain(m, RefugeDayTrust + (hasGodparent ? GodparentDayTrust : 0), null);

        public static CareResult AfterRace(MemoInstance m, bool won) => Gain(m, won ? RaceWinTrust : RaceTrust, null);

        /// <summary>El compañero gana confianza mientras te sigue (+2 por hora).</summary>
        public static CareResult AddFollowTime(MemoInstance m, float seconds)
        {
            m.followSeconds += seconds;
            int hours = Mathf.FloorToInt(m.followSeconds / 3600f);
            if (hours <= 0) return Fail(null);
            m.followSeconds -= hours * 3600f;
            return Gain(m, hours * FollowTrustPerHour, null);
        }

        public static void ResetDayIfNeeded(MemoInstance m, DateTime now)
        {
            var today = now.Date.ToString("yyyy-MM-dd");
            if (m.careDate == today) return;
            m.careDate = today;
            m.feedsToday = 0;
            m.pettedToday = false;
            m.playedToday = false;
            m.nearPointsToday = 0;
        }

        static CareResult Gain(MemoInstance m, int points, string message)
        {
            float multiplier = m.Temperament != null ? m.Temperament.trustGainMultiplier : 1f;
            int gained = Mathf.RoundToInt(points * multiplier);
            m.trust += gained;
            var level = m.TrustLevel;
            bool newMemory = level > m.highestTrust;
            if (newMemory) m.highestTrust = level;
            return new CareResult { success = true, trustGained = gained, message = message, newMemory = newMemory, level = level };
        }

        static CareResult Fail(string message) => new() { success = false, message = message };
    }
}
