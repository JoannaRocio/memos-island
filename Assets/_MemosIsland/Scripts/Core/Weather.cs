using System;

namespace MemosIsland.Core
{
    public enum WeatherKind { Sunny, Rainy, Stormy }

    /// <summary>
    /// Clima del día (GDD §5 y §16): uno por día real, igual todo el día y para todos.
    /// Llueve más o menos uno de cada cuatro días (con lluvia la huerta se riega sola); uno de cada
    /// doce, más o menos, hay tormenta: llueve igual y en los Acantilados aparece Karman (Fase 8B).
    /// </summary>
    public static class Weather
    {
        public const float RainChance = 0.25f;
        public const float StormChance = 0.08f; // parte de los días de lluvia

        /// <summary>Para probar (F8 en el juego): fuerza el clima de hoy.</summary>
        public static WeatherKind? DebugOverride;

        public static WeatherKind For(DateTime date)
        {
            var today = GameClock.Instance != null ? GameClock.Instance.Now.Date : DateTime.Now.Date;
            if (DebugOverride.HasValue && date.Date == today) return DebugOverride.Value;
            return Roll(date);
        }

        /// <summary>El clima "natural" de una fecha (determinista: no depende de cuándo se pregunta).</summary>
        public static WeatherKind Roll(DateTime date)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (var c in date.ToString("yyyy-MM-dd")) h = (h ^ c) * 16777619;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                float roll = (h % 1000) / 1000f;
                return roll < StormChance ? WeatherKind.Stormy : roll < RainChance ? WeatherKind.Rainy : WeatherKind.Sunny;
            }
        }

        /// <summary>Llueve (también con tormenta).</summary>
        public static bool IsRainy(DateTime date) => For(date) != WeatherKind.Sunny;

        public static bool IsStormy(DateTime date) => For(date) == WeatherKind.Stormy;

        public static string Name(WeatherKind w) => w switch { WeatherKind.Rainy => "Lluvia", WeatherKind.Stormy => "Tormenta", _ => "Sol" };

        public static string Icon(WeatherKind w) => w switch { WeatherKind.Rainy => "☂", WeatherKind.Stormy => "ϟ", _ => "☀" };
    }
}
