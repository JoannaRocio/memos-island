using System;
using UnityEngine;

namespace MemosIsland.Memos
{
    public enum Need { Hunger, Sleep, Fun, Social }

    /// <summary>
    /// Necesidades de un Memo (GDD §12): bajan con el tiempo real y el ánimo es su promedio.
    /// 0 = urgente, 100 = satisfecha.
    /// </summary>
    public static class MemoNeeds
    {
        public const float HungerPerHour = 6f;
        public const float SleepPerHour = 4f;
        public const float FunPerHour = 5f;
        public const float SocialPerHour = 3f;
        public const float SleepRecoveryPerHour = 30f;
        public const float MaxOfflineHours = 12f;

        /// <summary>Pasa el tiempo. Durmiendo recupera sueño en vez de perderlo.</summary>
        public static void Decay(MemoInstance m, float hours, bool asleep = false)
        {
            if (hours <= 0f) return;
            m.hunger = Clamp(m.hunger - HungerPerHour * hours);
            m.sleep = Clamp(asleep ? m.sleep + SleepRecoveryPerHour * hours : m.sleep - SleepPerHour * hours);
            m.fun = Clamp(m.fun - FunPerHour * hours);
            m.social = Clamp(m.social - SocialPerHour * hours);
            RefreshMood(m);
        }

        /// <summary>Aplica el tiempo que pasó desde la última vez (hasta 12 horas, para no castigar).</summary>
        public static void CatchUp(MemoInstance m, DateTime now)
        {
            if (m.needsUpdatedTicks > 0)
            {
                float hours = (float)(now - new DateTime(m.needsUpdatedTicks)).TotalHours;
                Decay(m, Mathf.Min(hours, MaxOfflineHours));
            }
            m.needsUpdatedTicks = now.Ticks;
        }

        public static void Satisfy(MemoInstance m, Need need, float amount)
        {
            switch (need)
            {
                case Need.Hunger: m.hunger = Clamp(m.hunger + amount); break;
                case Need.Sleep: m.sleep = Clamp(m.sleep + amount); break;
                case Need.Fun: m.fun = Clamp(m.fun + amount); break;
                default: m.social = Clamp(m.social + amount); break;
            }
            RefreshMood(m);
        }

        public static void RefreshMood(MemoInstance m) =>
            m.mood = Mathf.RoundToInt((m.hunger + m.sleep + m.fun + m.social) / 4f);

        /// <summary>La necesidad más urgente (la de valor más bajo).</summary>
        public static Need MostUrgent(MemoInstance m)
        {
            var need = Need.Hunger;
            float min = m.hunger;
            if (m.sleep < min) { min = m.sleep; need = Need.Sleep; }
            if (m.fun < min) { min = m.fun; need = Need.Fun; }
            if (m.social < min) need = Need.Social;
            return need;
        }

        public static float Value(MemoInstance m, Need need) => need switch
        {
            Need.Hunger => m.hunger,
            Need.Sleep => m.sleep,
            Need.Fun => m.fun,
            _ => m.social,
        };

        public static string Name(Need need) => need switch
        {
            Need.Hunger => "Hambre",
            Need.Sleep => "Sueño",
            Need.Fun => "Juego",
            _ => "Compañía",
        };

        static float Clamp(float v) => Mathf.Clamp(v, 0f, 100f);
    }
}
