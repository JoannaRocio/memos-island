using MemosIsland.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MemosIsland.World
{
    /// <summary>Luz de farol o ventana que se enciende al anochecer.</summary>
    [RequireComponent(typeof(Light2D))]
    public class NightLight : MonoBehaviour
    {
        [SerializeField] float maxIntensity = 1f;

        Light2D _light;

        void Awake() => _light = GetComponent<Light2D>();

        void Update()
        {
            float night = GameClock.Instance != null ? GameClock.Instance.NightFactor : 0f;
            _light.intensity = maxIntensity * night;
            _light.enabled = night > 0.01f;
        }
    }
}
