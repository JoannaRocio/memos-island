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
        /// <summary>Terminó la etapa rebelde recuperando la confianza: recuerdo "crecimos juntos".</summary>
        public bool grewTogether;
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
            bool hostile = level == TrustLevel.Hostile;
            string name = m.DisplayName;

            switch (action)
            {
                case CareAction.Feed:
                    MemoNeeds.Satisfy(m, Need.Hunger, 40f);
                    if (m.feedsToday >= FeedsPerDay)
                        return Fail($"{name} ya comió suficiente por hoy.");
                    m.feedsToday++;
                    return Gain(m, favoriteFood ? FavoriteFeedTrust : FeedTrust,
                        hostile ? $"{name} agarró la comida de un tirón y se fue a comer solo." :
                        scared ? $"{name} esperó a que te alejaras… y comió." :
                        favoriteFood ? $"¡A {name} le encantó! ♥" : $"{name} comió con ganas.");

                case CareAction.Pet:
                    if (hostile) return Fail($"{name} te gruñe. Mejor no tocarlo… todavía.");
                    if (scared) return Fail($"{name} retrocede asustado…");
                    if (m.pettedToday) return Fail($"{name} ya recibió mimos hoy. Igual se ve contento.");
                    m.pettedToday = true;
                    MemoNeeds.Satisfy(m, Need.Social, 25f);
                    return level == TrustLevel.Distrust
                        ? Gain(m, PetTrustDistrust, $"{name} se dejó tocar un poquito.")
                        : Gain(m, PetTrust, $"{name} cierra los ojos mientras lo acariciás. ♥");

                case CareAction.Play:
                    if (hostile) return Fail($"{name} te da la espalda.");
                    if (scared) return Fail($"{name} no quiere jugar. Te mira desde lejos.");
                    MemoNeeds.Satisfy(m, Need.Fun, 40f);
                    if (m.playedToday) return Fail($"{name} jugó un rato, pero ya está cansado de la pelota.");
                    m.playedToday = true;
                    return Gain(m, PlayTrust, $"¡{name} corre detrás de la pelota! ♪");

                case CareAction.Bath:
                    if (hostile) return Fail($"{name} sacude la cola y te salpica. No quiere saber nada.");
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

        /// <summary>Dar de comer un objeto del inventario: favorita = más confianza; algunas comidas suman extra.</summary>
        public static CareResult ApplyFood(MemoInstance m, ItemData food, DateTime now)
        {
            bool favorite = food != null && m.Species != null && m.Species.favoriteFoods.Contains(food.id);
            var result = Apply(m, CareAction.Feed, now, favorite);
            if (result.success && food != null && food.foodTrustBonus > 0)
            {
                var extra = Gain(m, food.foodTrustBonus, null);
                result.trustGained += extra.trustGained;
                result.newMemory |= extra.newMemory;
                result.level = extra.level;
            }
            return result;
        }

        public static bool IsFavorite(MemoInstance m, ItemData food) =>
            food != null && m.Species != null && m.Species.favoriteFoods.Contains(food.id);

        /// <summary>Lo que se gana por cada día en el refugio (y por tener padrino).</summary>
        public static CareResult DailyVisit(MemoInstance m, bool hasGodparent) =>
            Gain(m, RefugeDayTrust + (hasGodparent ? GodparentDayTrust : 0), null);

        /// <summary>Correr juntos (+3, +6 si ganan; ×1,5 con el Moño de amistad).</summary>
        public static CareResult AfterRace(MemoInstance m, bool won)
        {
            int points = won ? RaceWinTrust : RaceTrust;
            if (EquipmentRules.Has(m.Amulet, AmuletEffect.FriendshipBow)) points = Mathf.RoundToInt(points * 1.5f);
            return Gain(m, points, null);
        }

        public const int AccessoryLikedTrust = 10;

        /// <summary>
        /// La primera vez que se pone un accesorio: si le gusta, +10 de confianza (GDD §14).
        /// A cada Memo le gustan unos y otros no (siempre los mismos).
        /// </summary>
        public static CareResult TryAccessory(MemoInstance m, ItemData accessory)
        {
            if (accessory == null || m.accessoriesTried.Contains(accessory.id)) return Fail(null);
            m.accessoriesTried.Add(accessory.id);
            if (!LikesAccessory(m, accessory.id))
                return Fail($"{m.DisplayName} no parece muy convencido con {accessory.displayName}…");
            return Gain(m, AccessoryLikedTrust, $"¡A {m.DisplayName} le encanta {accessory.displayName}! ♥");
        }

        public static bool LikesAccessory(MemoInstance m, string accessoryId)
        {
            int h = 17;
            foreach (var c in (m.uid ?? "") + accessoryId) h = (h * 31 + c) & 0x7fffffff;
            return h % 3 != 0;
        }

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
            bool grewTogether = Progression.CheckRebelRecovery(m, DateTime.Now);
            return new CareResult
            {
                success = true, trustGained = gained, message = message, newMemory = newMemory, level = level,
                grewTogether = grewTogether,
            };
        }

        static CareResult Fail(string message) => new() { success = false, message = message };
    }
}
