using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// Texto con la fuente pixel (un SpriteRenderer por letra, con sombra estilo GBA).
    /// El origen es la esquina superior izquierda del primer renglón.
    /// </summary>
    public class PixelText : MonoBehaviour
    {
        [SerializeField] PixelFont font;
        [SerializeField, TextArea] string text = "";
        [SerializeField] Color color = new Color32(0x33, 0x3c, 0x57, 0xff);
        [SerializeField] bool shadow = true;
        [SerializeField] Color shadowColor = new Color32(0x94, 0xb0, 0xc2, 0xff);
        [SerializeField] Material material;
        [SerializeField] int sortingOrder = 1010;

        readonly List<SpriteRenderer> _pool = new();
        int _used;
        int _visible = int.MaxValue;
        bool _built;

        public PixelFont Font => font;
        public Material Material => material;
        public string Text => text;

        public void Setup(PixelFont newFont, Material newMaterial, int order, Color textColor, bool withShadow)
        {
            font = newFont;
            material = newMaterial;
            sortingOrder = order;
            color = textColor;
            shadow = withShadow;
        }

        /// <summary>Cantidad de letras visibles (para el efecto de máquina de escribir).</summary>
        public int VisibleCharacters
        {
            get => _visible;
            set
            {
                if (value == _visible) return;
                _visible = value;
                Rebuild();
            }
        }

        /// <summary>Letras que cuentan para VisibleCharacters (todo menos los saltos de línea).</summary>
        public int CharacterCount => text.Replace("\n", "").Length;

        public int WidthPixels
        {
            get
            {
                int w = 0;
                foreach (var line in text.Split('\n')) w = Mathf.Max(w, font.MeasureWidth(line));
                return w;
            }
        }

        public void SetText(string newText)
        {
            text = newText ?? "";
            _visible = int.MaxValue;
            Rebuild();
        }

        public void SetColor(Color c)
        {
            color = c;
            Rebuild();
        }

        void Start()
        {
            if (!_built) Rebuild();
        }

        void Rebuild()
        {
            _built = true;
            _used = 0;
            if (font != null)
            {
                int x = 0, y = 0, shown = 0;
                foreach (var c in text)
                {
                    if (c == '\n')
                    {
                        x = 0;
                        y += font.lineHeight;
                        continue;
                    }
                    if (shown >= _visible) break;
                    shown++;
                    var sprite = font.GetSprite(c);
                    if (sprite != null)
                    {
                        if (shadow && !IsDark(color)) Place(sprite, x + 1, y + 1, shadowColor, sortingOrder);
                        Place(sprite, x, y, color, sortingOrder + 1);
                    }
                    x += font.Advance(c);
                }
            }
            for (int i = _used; i < _pool.Count; i++) _pool[i].enabled = false;
        }

        /// <summary>
        /// Los textos oscuros van sobre cajas claras: ahí la sombra molesta para leer. Solo los textos claros
        /// (sobre fondo oscuro, como el título) llevan sombra.
        /// </summary>
        static bool IsDark(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f < 0.5f;

        void Place(Sprite sprite, int px, int py, Color c, int order)
        {
            SpriteRenderer r;
            if (_used < _pool.Count)
            {
                r = _pool[_used];
            }
            else
            {
                var go = new GameObject("glyph") { hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy };
                go.transform.SetParent(transform, false);
                r = go.AddComponent<SpriteRenderer>();
                if (material != null) r.sharedMaterial = material;
                _pool.Add(r);
            }
            _used++;
            r.enabled = true;
            r.sprite = sprite;
            r.color = c;
            r.sortingOrder = order;
            r.transform.localPosition = new Vector3(px / PixelFont.PixelsPerUnit, -py / PixelFont.PixelsPerUnit, 0f);
        }
    }
}
