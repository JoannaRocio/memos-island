using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.EditorTools.PixelArt
{
    /// <summary>
    /// Paleta del arte provisorio: Sweetie 16 (0-9, a-f) + tonos extra (g-p).
    /// '.' y ' ' son transparentes.
    /// </summary>
    public static class PixelPalette
    {
        public const char Transparent = '.';

        static readonly Dictionary<char, string> Hex = new()
        {
            // Sweetie 16
            { '0', "1a1c2c" }, // casi negro (contornos)
            { '1', "5d275d" }, // ciruela
            { '2', "b13e53" }, // rojo
            { '3', "ef7d57" }, // naranja
            { '4', "ffcd75" }, // amarillo
            { '5', "a7f070" }, // verde claro
            { '6', "38b764" }, // verde
            { '7', "257179" }, // verde azulado oscuro
            { '8', "29366f" }, // azul marino
            { '9', "3b5dc9" }, // azul
            { 'a', "41a6f6" }, // celeste
            { 'b', "73eff7" }, // cian
            { 'c', "f4f4f4" }, // blanco
            { 'd', "94b0c2" }, // gris claro
            { 'e', "566c86" }, // gris
            { 'f', "333c57" }, // gris oscuro
            // Extras
            { 'g', "f6d2b0" }, // piel clara
            { 'h', "d99a74" }, // piel media / sombra de piel
            { 'i', "8f5a3c" }, // marrón (madera, pelo)
            { 'j', "5a3328" }, // marrón oscuro
            { 'k', "9b3cff" }, // violeta collar
            { 'l', "d8a6ff" }, // brillo collar
            { 'm', "1e5a3c" }, // verde bosque oscuro
            { 'n', "c46a3a" }, // terracota (tejas)
            { 'o', "d9a066" }, // madera clara / arena
            { 'p', "fff1c9" }, // crema (brillos cálidos)
        };

        static Dictionary<char, Color32> _colors;

        public static bool IsTransparent(char c) => c == '.' || c == ' ';

        public static bool TryGet(char c, out Color32 color)
        {
            if (_colors == null)
            {
                _colors = new Dictionary<char, Color32>();
                foreach (var kv in Hex)
                {
                    ColorUtility.TryParseHtmlString("#" + kv.Value, out var col);
                    _colors[kv.Key] = col;
                }
            }
            return _colors.TryGetValue(c, out color);
        }
    }
}
