using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MemosIsland.Core
{
    public enum DayPhase { Night, Dawn, Day, Dusk }

    /// <summary>
    /// Reloj del juego = reloj real del jugador (como Pokémon Oro/Plata).
    /// En el editor y en builds de desarrollo: F5/F6 atrasan/adelantan una hora, F7 vuelve a la hora real.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        [Tooltip("Horas sumadas a la hora real. Solo para pruebas.")]
        [SerializeField] int debugHourOffset;

        static readonly string[] DayNames = { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };

        public DateTime Now => DateTime.Now.AddHours(debugHourOffset);
        public float HourOfDay => ToHourOfDay(Now);
        public DayPhase Phase => PhaseFor(HourOfDay);
        public float NightFactor => NightFactorFor(HourOfDay);
        public bool IsDebugTime => debugHourOffset != 0;

        public string DayName => DayNames[(int)Now.DayOfWeek];
        public string TimeText => $"{DayName} {Now:HH:mm}";

        void Awake() => Instance = this;

        void Update()
        {
            if (!Debug.isDebugBuild || Keyboard.current == null) return;
            if (Keyboard.current.f5Key.wasPressedThisFrame) debugHourOffset--;
            if (Keyboard.current.f6Key.wasPressedThisFrame) debugHourOffset++;
            if (Keyboard.current.f7Key.wasPressedThisFrame) debugHourOffset = 0;
            // F8: clima de prueba (natural → lluvia → sol → natural).
            if (Keyboard.current.f8Key.wasPressedThisFrame)
                Weather.DebugOverride = Weather.DebugOverride switch
                {
                    null => WeatherKind.Rainy,
                    WeatherKind.Rainy => WeatherKind.Sunny,
                    _ => null,
                };
        }

        // ------------------------------------------------------------ Lógica pura (con tests)

        public static float ToHourOfDay(DateTime t) => t.Hour + t.Minute / 60f + t.Second / 3600f;

        /// <summary>Noche 20–5 · Amanecer 5–7 · Día 7–17 · Atardecer 17–20.</summary>
        public static DayPhase PhaseFor(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            if (hour >= 20f || hour < 5f) return DayPhase.Night;
            if (hour < 7f) return DayPhase.Dawn;
            if (hour < 17f) return DayPhase.Day;
            return DayPhase.Dusk;
        }

        /// <summary>0 de día, 1 de noche. Transición suave de 5 a 7 y de 18 a 20.</summary>
        public static float NightFactorFor(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            if (hour >= 20f || hour < 5f) return 1f;
            if (hour < 7f) return 1f - Mathf.InverseLerp(5f, 7f, hour);
            if (hour < 18f) return 0f;
            return Mathf.InverseLerp(18f, 20f, hour);
        }
    }
}
