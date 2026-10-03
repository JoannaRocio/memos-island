using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>Fuente bitmap de pixel art. La genera PixelFontGenerator desde Art/FontSource.</summary>
    [CreateAssetMenu(menuName = "Memos Island/UI/Pixel Font")]
    public class PixelFont : ScriptableObject
    {
        [Serializable]
        public struct Glyph
        {
            public char character;
            public RectInt rect; // en pixels del atlas
        }

        public Texture2D atlas;
        public int glyphHeight = 11;
        public int lineHeight = 14;
        public int spacing = 1;
        public int spaceWidth = 3;
        public List<Glyph> glyphs = new();

        public const float PixelsPerUnit = 16f;

        Dictionary<char, Glyph> _lookup;
        readonly Dictionary<char, Sprite> _sprites = new();

        void BuildLookup()
        {
            _lookup = new Dictionary<char, Glyph>();
            foreach (var g in glyphs) _lookup[g.character] = g;
        }

        public bool Has(char c)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.ContainsKey(c);
        }

        /// <summary>Ancho en pixels que avanza el cursor después de este carácter.</summary>
        public int Advance(char c)
        {
            if (c == ' ') return spaceWidth + spacing;
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(c, out var g) ? g.rect.width + spacing : spaceWidth + spacing;
        }

        public int MeasureWidth(string text, int start = 0, int length = -1)
        {
            if (length < 0) length = text.Length - start;
            int w = 0;
            for (int i = start; i < start + length; i++) w += Advance(text[i]);
            return Mathf.Max(0, w - spacing);
        }

        /// <summary>Corta el texto en renglones de hasta maxWidth pixels, sin partir palabras (salvo que no entren).</summary>
        public List<string> Wrap(string text, int maxWidth)
        {
            var lines = new List<string>();
            foreach (var paragraph in (text ?? "").Split('\n'))
            {
                var line = "";
                foreach (var word in paragraph.Split(' '))
                {
                    var candidate = line.Length == 0 ? word : line + " " + word;
                    if (line.Length == 0 || MeasureWidth(candidate) <= maxWidth)
                    {
                        line = candidate;
                        continue;
                    }
                    lines.Add(line);
                    line = word;
                }
                // Palabras más largas que el renglón: se cortan por caracteres.
                while (line.Length > 1 && MeasureWidth(line) > maxWidth)
                {
                    int cut = line.Length - 1;
                    while (cut > 1 && MeasureWidth(line.Substring(0, cut)) > maxWidth) cut--;
                    lines.Add(line.Substring(0, cut));
                    line = line.Substring(cut);
                }
                lines.Add(line);
            }
            return lines;
        }

        /// <summary>Sprite del glifo con pivote arriba a la izquierda (null si no existe o es espacio).</summary>
        public Sprite GetSprite(char c)
        {
            if (_sprites.TryGetValue(c, out var s)) return s;
            if (_lookup == null) BuildLookup();
            if (!_lookup.TryGetValue(c, out var g)) return null;
            s = Sprite.Create(atlas, new Rect(g.rect.x, g.rect.y, g.rect.width, g.rect.height),
                new Vector2(0f, 1f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.name = $"glyph_{c}";
            _sprites[c] = s;
            return s;
        }

        void OnDisable() => _sprites.Clear();
    }
}
