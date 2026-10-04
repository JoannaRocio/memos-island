using System;
using UnityEngine;

namespace MemosIsland.Core
{
    /// <summary>Opciones del jugador (se guardan aparte de la partida, en PlayerPrefs): volumen y velocidad del texto.</summary>
    public static class GameSettings
    {
        public static readonly string[] TextSpeeds = { "Lenta", "Normal", "Rápida" };
        static readonly float[] CharsPerSecond = { 25f, 45f, 90f };

        public static float MusicVolume { get; private set; } = 0.8f;
        public static float SfxVolume { get; private set; } = 0.8f;
        public static int TextSpeed { get; private set; } = 1;

        public static float CharactersPerSecond => CharsPerSecond[Mathf.Clamp(TextSpeed, 0, CharsPerSecond.Length - 1)];

        /// <summary>Avisa cuando cambia el volumen (lo usa el audio, Fase 9B).</summary>
        public static event Action Changed;

        static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                MusicVolume = PlayerPrefs.GetFloat("music", 0.8f);
                SfxVolume = PlayerPrefs.GetFloat("sfx", 0.8f);
                TextSpeed = PlayerPrefs.GetInt("textSpeed", 1);
            }
            catch (Exception) { /* sin preferencias guardadas: valores por defecto */ }
        }

        public static void Set(float music, float sfx, int textSpeed)
        {
            MusicVolume = Mathf.Clamp01(music);
            SfxVolume = Mathf.Clamp01(sfx);
            TextSpeed = Mathf.Clamp(textSpeed, 0, TextSpeeds.Length - 1);
            try
            {
                PlayerPrefs.SetFloat("music", MusicVolume);
                PlayerPrefs.SetFloat("sfx", SfxVolume);
                PlayerPrefs.SetInt("textSpeed", TextSpeed);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
            Changed?.Invoke();
        }
    }
}
