using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// Ayudas para armar pantallas por código sobre la interfaz del GameRoot (pixels de una pantalla de 240x135).
    /// Reusa la fuente, la caja y el material de la ListScreen.
    /// </summary>
    public class UiKit
    {
        public static readonly Color Dark = new Color32(0x33, 0x3c, 0x57, 0xff);
        public static readonly Color Gray = new Color32(0x94, 0xb0, 0xc2, 0xff);
        public static readonly Color MidGray = new Color32(0x56, 0x6c, 0x86, 0xff);
        public static readonly Color Red = new Color32(0xb1, 0x3e, 0x53, 0xff);
        public static readonly Color White = new Color32(0xf4, 0xf4, 0xf4, 0xff);
        public static readonly Color Gold = new Color32(0xff, 0xcd, 0x75, 0xff);
        public static readonly Color Violet = new Color32(0x9b, 0x3c, 0xff, 0xff);

        const float Ppu = PixelFont.PixelsPerUnit;

        /// <summary>Pantallas completas abiertas (título, creación, diario): ocultan el reloj y la lluvia.</summary>
        public static int FullScreens;

        public readonly Transform Root;
        readonly PixelFont _font;
        readonly Sprite _box, _pixel;
        readonly Material _material;

        /// <summary>Crea la raíz de una pantalla colgada de la interfaz (inactiva hasta que la actives).</summary>
        public UiKit(string name)
        {
            var lists = GameRoot.Instance.Lists;
            _font = lists.Font;
            _box = lists.BoxSprite;
            _pixel = lists.PixelSprite;
            _material = lists.Material;
            Root = new GameObject(name).transform;
            Root.SetParent(lists.transform.parent, false);
        }

        public static Vector3 P(float x, float y) => new(x / Ppu, y / Ppu, 0f);

        public PixelFont Font => _font;

        public PixelText Text(float x, float y, Color color, int order, bool shadow = true, Transform parent = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent ? parent : Root, false);
            go.transform.localPosition = P(x, y);
            var t = go.AddComponent<PixelText>();
            t.Setup(_font, _material, order, color, shadow);
            return t;
        }

        public SpriteRenderer Sprite(Sprite sprite, float x, float y, int order, Transform parent = null)
        {
            var go = new GameObject("Sprite");
            go.transform.SetParent(parent ? parent : Root, false);
            go.transform.localPosition = P(x, y);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            r.sharedMaterial = _material;
            return r;
        }

        /// <summary>Caja con borde (9-slice) centrada en (x, y), en pixels de interfaz.</summary>
        public SpriteRenderer Box(float x, float y, float w, float h, int order, Transform parent = null)
        {
            var r = Sprite(_box, x, y, order, parent);
            r.drawMode = SpriteDrawMode.Sliced;
            r.size = new Vector2(w / Ppu, h / Ppu);
            return r;
        }

        /// <summary>Rectángulo de color liso centrado en (x, y).</summary>
        public SpriteRenderer Rect(float x, float y, float w, float h, Color color, int order, Transform parent = null)
        {
            var r = Sprite(_pixel, x, y, order, parent);
            float px = _pixel != null ? _pixel.rect.width : 2f;
            r.transform.localScale = new Vector3(w / px, h / px, 1f);
            r.color = color;
            return r;
        }

        /// <summary>Centra un texto de una línea en x.</summary>
        public void Center(PixelText t, float x, float y)
        {
            t.transform.localPosition = P(x - _font.MeasureWidth(t.Text) * t.transform.localScale.x / 2f, y);
        }

        public void Destroy() => Object.Destroy(Root.gameObject);
    }
}
