using System;
using MemosIsland.Core;

namespace MemosIsland.UI
{
    /// <summary>Opciones (Fase 9A): volumen de música y efectos, velocidad del texto y controles. Se abre desde el título o la pausa.</summary>
    public static class OptionsMenu
    {
        static readonly string[] Volumes = { "Silencio", "20 %", "40 %", "60 %", "80 %", "100 %" };

        static string Pct(float v) => Volumes[(int)Math.Round(v * 5f)];

        public static void Open(Action onClosed = null)
        {
            var dialogue = GameRoot.Instance.Dialogue;
            var options = new[]
            {
                $"Música: {Pct(GameSettings.MusicVolume)}",
                $"Efectos: {Pct(GameSettings.SfxVolume)}",
                $"Texto: {GameSettings.TextSpeeds[GameSettings.TextSpeed]}",
                "Controles",
                "Volver",
            };
            dialogue.SetSpeaker("Opciones", null);
            dialogue.ShowChoice("¿Qué querés cambiar?", options, i =>
            {
                switch (i)
                {
                    case 0:
                        PickVolume("Volumen de la música", v => GameSettings.Set(v, GameSettings.SfxVolume, GameSettings.TextSpeed), onClosed);
                        break;
                    case 1:
                        PickVolume("Volumen de los efectos", v => GameSettings.Set(GameSettings.MusicVolume, v, GameSettings.TextSpeed), onClosed);
                        break;
                    case 2:
                        dialogue.ShowChoice("Velocidad del texto", GameSettings.TextSpeeds, s =>
                        {
                            GameSettings.Set(GameSettings.MusicVolume, GameSettings.SfxVolume, s);
                            Open(onClosed);
                        }, GameSettings.TextSpeed);
                        break;
                    case 3:
                        dialogue.SetSpeaker("Controles", null);
                        dialogue.Show(new[]
                        {
                            "Flechas o WASD: caminar (un toque gira). Mantené B (X o Shift) para correr.",
                            "A (Z, Enter o Espacio): hablar, interactuar y avanzar los textos. B: volver.",
                            "Esc o Tab: menú de pausa. En carrera: ←→ elegir, B cambiar de corredor, A habilidad.",
                            "También funciona con joystick: cruceta o palanca, A/B y Start.",
                        }, () => Open(onClosed));
                        break;
                    default:
                        onClosed?.Invoke();
                        break;
                }
            }, 4);
        }

        static void PickVolume(string question, Action<float> apply, Action onClosed)
        {
            var dialogue = GameRoot.Instance.Dialogue;
            dialogue.SetSpeaker("Opciones", null);
            dialogue.ShowChoice(question, Volumes, i =>
            {
                apply(i / 5f);
                Open(onClosed);
            });
        }
    }
}
