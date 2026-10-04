using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MemosIsland.Story
{
    public enum Pronoun { El, Ella, Elle }

    /// <summary>El personaje que crea el jugador (GDD §17). La apariencia son índices de las opciones de PlayerLook.</summary>
    [Serializable]
    public class PlayerProfile
    {
        public string name = "Alex";
        public Pronoun pronoun = Pronoun.Elle;
        public string refugeName = "";
        public int skin, hairStyle, hairColor, shirtColor, pantsColor;

        public string RefugeDisplayName => string.IsNullOrWhiteSpace(refugeName) ? "Refugio del abuelo" : refugeName.Trim();
    }

    /// <summary>Por dónde va la historia (Fase 8): marcas, objetivo actual y el inicial elegido.</summary>
    [Serializable]
    public class StoryState
    {
        [UnityEngine.Tooltip("Partida de historia (Nueva partida). Si es falso, es una partida de prueba y no corre ninguna escena.")]
        public bool active;
        public PlayerProfile profile = new();
        public string starterId;
        public string starterUid;
        public string objective = "";
        public List<string> flags = new();
        [UnityEngine.Tooltip("Día (yyyy-MM-dd) en que perdiste en la Copa: se vuelve a intentar el sábado siguiente.")]
        public string cupLostOn = "";

        public bool Has(string flag) => flags.Contains(flag);

        public void Set(string flag)
        {
            if (!flags.Contains(flag)) flags.Add(flag);
        }
    }

    /// <summary>
    /// Etiquetas en los textos (GDD §17): {nombre}, {refugio} y opciones por pronombre como {o/a/e} o
    /// {nieto/nieta/niete} (él / ella / elle). Con dos opciones, elle usa la segunda… salvo que haya tres.
    /// </summary>
    public static class TextTags
    {
        static readonly Regex Choice = new(@"\{([^{}/]*)/([^{}/]*)(?:/([^{}/]*))?\}");

        public static string Apply(string text, PlayerProfile profile)
        {
            if (string.IsNullOrEmpty(text) || profile == null || text.IndexOf('{') < 0) return text;
            text = text.Replace("{nombre}", profile.name).Replace("{refugio}", profile.RefugeDisplayName);
            int index = (int)profile.pronoun;
            return Choice.Replace(text, m =>
            {
                var options = m.Groups[3].Success
                    ? new[] { m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value }
                    : new[] { m.Groups[1].Value, m.Groups[2].Value };
                return options[Math.Min(index, options.Length - 1)];
            });
        }
    }
}
