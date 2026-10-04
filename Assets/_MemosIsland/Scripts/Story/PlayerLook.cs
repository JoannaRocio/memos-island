using System;
using System.Collections.Generic;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>
    /// Apariencia del protagonista (GDD §17, versión básica): piel, peinado, color de pelo, remera y pantalón.
    /// Los sprites base usan colores "clave" (los del jugador por defecto) que se reemplazan en tiempo de juego.
    /// La bufanda roja del abuelo no cambia nunca: es parte de la historia.
    /// </summary>
    public static class PlayerLook
    {
        public readonly struct Option
        {
            public readonly string name;
            public readonly Color32 light, dark;
            public Option(string name, string light, string dark)
            {
                this.name = name;
                this.light = Hex(light);
                this.dark = Hex(dark);
            }
        }

        static Color32 Hex(string h) => new(Convert.ToByte(h.Substring(0, 2), 16), Convert.ToByte(h.Substring(2, 2), 16),
            Convert.ToByte(h.Substring(4, 2), 16), 255);

        // Colores clave del sprite base (Characters.txt): pelo i/j, piel g/h, remera a/9, pantalón 8.
        static readonly Color32 KeyHair = Hex("8f5a3c"), KeyHairDark = Hex("5a3328");
        static readonly Color32 KeySkin = Hex("f6d2b0"), KeySkinDark = Hex("d99a74");
        static readonly Color32 KeyShirt = Hex("41a6f6"), KeyShirtDark = Hex("3b5dc9");
        static readonly Color32 KeyPants = Hex("29366f");

        public static readonly string[] HairStyles = { "Corto", "Largo", "Pinchudo", "Rodete" };

        public static readonly Option[] Skins =
        {
            new("Clara", "f6d2b0", "d99a74"),
            new("Trigueña", "d99a74", "b0704a"),
            new("Morena", "8f5a3c", "5a3328"),
        };

        public static readonly Option[] HairColors =
        {
            new("Castaño", "8f5a3c", "5a3328"),
            new("Negro", "333c57", "1a1c2c"),
            new("Rubio", "ffcd75", "ef7d57"),
            new("Pelirrojo", "ef7d57", "b13e53"),
            new("Celeste", "73eff7", "41a6f6"),
        };

        public static readonly Option[] ShirtColors =
        {
            new("Celeste", "41a6f6", "3b5dc9"),
            new("Verde", "38b764", "257179"),
            new("Amarilla", "ffcd75", "ef7d57"),
            new("Blanca", "f4f4f4", "94b0c2"),
            new("Gris", "566c86", "333c57"),
            new("Naranja", "ef7d57", "b13e53"),
        };

        public static readonly Option[] PantsColors =
        {
            new("Azul", "29366f", "29366f"),
            new("Gris", "566c86", "566c86"),
            new("Marrón", "5a3328", "5a3328"),
            new("Verde", "257179", "257179"),
        };

        static PlayerStyleSet _set;

        public static PlayerStyleSet Set => _set != null ? _set : _set = Resources.Load<PlayerStyleSet>("PlayerStyles");

        static readonly Dictionary<string, Sprite[][]> Cache = new();

        /// <summary>Los 4 juegos de cuadros (abajo, arriba, izquierda, derecha) con la apariencia del perfil.</summary>
        public static Sprite[][] Frames(PlayerProfile p)
        {
            var set = Set;
            if (set == null || set.styles.Count == 0) return null;
            int style = Mathf.Clamp(p.hairStyle, 0, set.styles.Count - 1);
            string key = $"{style}-{p.skin}-{p.hairColor}-{p.shirtColor}-{p.pantsColor}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var skin = Skins[Mathf.Clamp(p.skin, 0, Skins.Length - 1)];
            var hair = HairColors[Mathf.Clamp(p.hairColor, 0, HairColors.Length - 1)];
            var shirt = ShirtColors[Mathf.Clamp(p.shirtColor, 0, ShirtColors.Length - 1)];
            var pants = PantsColors[Mathf.Clamp(p.pantsColor, 0, PantsColors.Length - 1)];
            var map = new Dictionary<Color32, Color32>(new ColorEq())
            {
                [KeyHair] = hair.light, [KeyHairDark] = hair.dark,
                [KeySkin] = skin.light, [KeySkinDark] = skin.dark,
                [KeyShirt] = shirt.light, [KeyShirtDark] = shirt.dark,
                [KeyPants] = pants.light,
            };
            var s = set.styles[style];
            var result = new[] { Recolor(s.down, map), Recolor(s.up, map), Recolor(s.left, map), Recolor(s.right, map) };
            Cache[key] = result;
            return result;
        }

        /// <summary>Viste al jugador (o a cualquier personaje con CharacterSpriteAnimator) con el perfil.</summary>
        public static void Apply(GridMover mover, PlayerProfile p)
        {
            var frames = Frames(p);
            var animator = mover != null ? mover.GetComponentInChildren<CharacterSpriteAnimator>() : null;
            if (frames == null || animator == null) return;
            animator.Setup(mover, frames[0], frames[1], frames[2], frames[3]);
        }

        static Sprite[] Recolor(Sprite[] frames, Dictionary<Color32, Color32> map)
        {
            var result = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++) result[i] = Recolor(frames[i], map);
            return result;
        }

        static Sprite Recolor(Sprite src, Dictionary<Color32, Color32> map)
        {
            if (src == null) return null;
            var r = src.textureRect;
            int w = (int)r.width, h = (int)r.height;
            var pixels = src.texture.GetPixels32();
            int texW = src.texture.width;
            var outPixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = pixels[((int)r.y + y) * texW + (int)r.x + x];
                outPixels[y * w + x] = c.a > 0 && map.TryGetValue(c, out var to) ? to : c;
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(outPixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(src.pivot.x / w, src.pivot.y / h), src.pixelsPerUnit);
            sprite.name = src.name;
            return sprite;
        }

        class ColorEq : IEqualityComparer<Color32>
        {
            public bool Equals(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;
            public int GetHashCode(Color32 c) => c.r << 16 | c.g << 8 | c.b;
        }
    }
}
