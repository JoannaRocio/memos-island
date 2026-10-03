using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MemosIsland.Core
{
    /// <summary>Colorea la luz global según la hora real: noche azul, amanecer rosado, día blanco, atardecer naranja.</summary>
    [RequireComponent(typeof(Light2D))]
    public class DayNightLighting : MonoBehaviour
    {
        [Serializable]
        public struct LightKey
        {
            [Range(0f, 24f)] public float hour;
            public Color color;
            [Range(0f, 1.5f)] public float intensity;

            public LightKey(float hour, Color color, float intensity)
            {
                this.hour = hour;
                this.color = color;
                this.intensity = intensity;
            }
        }

        [SerializeField] List<LightKey> keys = new()
        {
            new(0f, new Color(0.42f, 0.47f, 0.85f), 0.55f),
            new(4.5f, new Color(0.42f, 0.47f, 0.85f), 0.55f),
            new(6f, new Color(0.98f, 0.74f, 0.78f), 0.8f),
            new(7.5f, Color.white, 1f),
            new(17f, new Color(1f, 0.97f, 0.9f), 1f),
            new(18.5f, new Color(1f, 0.72f, 0.5f), 0.88f),
            new(20f, new Color(0.42f, 0.47f, 0.85f), 0.55f),
            new(24f, new Color(0.42f, 0.47f, 0.85f), 0.55f),
        };

        Light2D _light;

        void Awake() => _light = GetComponent<Light2D>();

        void Update()
        {
            if (GameClock.Instance == null) return;
            Evaluate(keys, GameClock.Instance.HourOfDay, out var color, out var intensity);
            // Día de lluvia al aire libre: más gris y un poco más oscuro.
            if (Weather.IsRainy(GameClock.Instance.Now) && (World.MapInfo.Current == null || World.MapInfo.Current.Outdoor))
            {
                color = Color.Lerp(color, new Color(0.62f, 0.68f, 0.8f), 0.45f);
                intensity *= 0.8f;
            }
            _light.color = color;
            _light.intensity = intensity;
        }

        public static void Evaluate(IReadOnlyList<LightKey> keys, float hour, out Color color, out float intensity)
        {
            hour = Mathf.Repeat(hour, 24f);
            for (int i = 0; i < keys.Count - 1; i++)
            {
                var a = keys[i];
                var b = keys[i + 1];
                if (hour < a.hour || hour > b.hour) continue;
                float t = Mathf.InverseLerp(a.hour, b.hour, hour);
                color = Color.Lerp(a.color, b.color, t);
                intensity = Mathf.Lerp(a.intensity, b.intensity, t);
                return;
            }
            color = keys[0].color;
            intensity = keys[0].intensity;
        }
    }
}
