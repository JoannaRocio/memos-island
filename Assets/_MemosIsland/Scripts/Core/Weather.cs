using System;

namespace MemosIsland.Core
{
    public enum WeatherKind { Sunny, Rainy }

    /// <summary>
    /// Clima del día (GDD §5 y §16): uno por día real, igual todo el día y para todos.
    /// Llueve más o menos uno de cada cuatro días. Con lluvia la huerta se riega sola.
    /// </summary>
    public static class Weather
    {
        public const float RainChance = 0.25f;

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
                return (h % 1000) / 1000f < RainChance ? WeatherKind.Rainy : WeatherKind.Sunny;
            }
        }

        public static bool IsRainy(DateTime date) => For(date) == WeatherKind.Rainy;

        public static string Name(WeatherKind w) => w == WeatherKind.Rainy ? "Lluvia" : "Sol";
    }
}
