using System;
using System.Collections.Generic;
using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// Pantalla de lista reutilizable (mochila, tienda, mesa de trabajo, caja de envíos):
    /// pestañas (←→), lista con íconos (↑↓), detalle a la derecha, A confirma, B cierra.
    /// </summary>
    public class ListScreen : MonoBehaviour
    {
        public class Row
        {
            public Sprite icon;
            public string label;
            public string right;
            public bool enabled = true;
        }

        /// <summary>Qué muestra la pantalla. Las funciones reciben la pestaña elegida.</summary>
        public class Content
        {
            public string title;
            public string[] tabs = { "" };
            public Func<int, List<Row>> rows;
            public Func<int, int, string> detail;
            /// <summary>A sobre una fila (pestaña, fila). Devuelve un mensaje para mostrar (o null).</summary>
            public Func<int, int, string> confirm;
            /// <summary>Texto arriba a la derecha (ej. el dinero).</summary>
            public Func<string> header;
            public string hint = "A: elegir  B: salir";
        }

        [SerializeField] PixelFont font;
        [SerializeField] Sprite boxSprite;
        [SerializeField] Sprite pixelSprite;
        [SerializeField] Material material;
        [SerializeField] int baseOrder = 650; // debajo de los diálogos

        const float Ppu = PixelFont.PixelsPerUnit;
        const int VisibleRows = 7, RowHeight = 12, ListTop = 40;
        static readonly Color Dark = new Color32(0x33, 0x3c, 0x57, 0xff);
        static readonly Color Gray = new Color32(0x94, 0xb0, 0xc2, 0xff);
        static readonly Color MidGray = new Color32(0x56, 0x6c, 0x86, 0xff);
        static readonly Color Red = new Color32(0xb1, 0x3e, 0x53, 0xff);
        static readonly Color Highlight = new Color32(0x73, 0xef, 0xf7, 0xff);

        public bool IsOpen { get; private set; }

        Content _content;
        List<Row> _rows = new();
        int _tab, _selected, _scroll, _openedFrame, _heldDir;
        float _repeat;
        Action _onClosed;

        GameObject _root;
        PixelText _title, _header, _tabs, _detail, _hint, _cursor, _empty;
        SpriteRenderer _highlight;
        readonly List<(SpriteRenderer icon, PixelText label, PixelText right)> _lines = new();

        public void Setup(PixelFont newFont, Sprite box, Sprite pixel, Material mat)
        {
            font = newFont;
            boxSprite = box;
            pixelSprite = pixel;
            material = mat;
        }

        void Awake()
        {
            Build();
            _root.SetActive(false);
        }

        public void Open(Content content, Action onClosed = null)
        {
            _content = content;
            _onClosed = onClosed;
            _tab = 0;
            _selected = 0;
            _scroll = 0;
            _openedFrame = Time.frameCount;
            IsOpen = true;
            GameRoot.InputLocks++;
            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            GameRoot.ConsumeUiInput();
            IsOpen = false;
            GameRoot.InputLocks--;
            _root.SetActive(false);
            _onClosed?.Invoke();
        }

        void Update()
        {
            if (!IsOpen || Time.frameCount == _openedFrame || GameRoot.Instance.Dialogue.IsOpen) return;
            if (GameInput.CancelPressed || GameInput.MenuPressed)
            {
                Close();
                return;
            }
            if (GameInput.ConfirmPressed && _rows.Count > 0 && _content.confirm != null)
            {
                var message = _content.confirm(_tab, _selected);
                Refresh();
                if (!string.IsNullOrEmpty(message)) GameRoot.Instance.Dialogue.Show(new[] { message });
                return;
            }

            var move = GameInput.Move;
            int v = move.y > 0.5f ? -1 : move.y < -0.5f ? 1 : 0;
            int h = move.x > 0.5f ? 1 : move.x < -0.5f ? -1 : 0;
            int dir = v != 0 ? v * 10 : h;
            if (dir == 0)
            {
                _heldDir = 0;
                return;
            }
            if (dir == _heldDir && ((_repeat -= Time.unscaledDeltaTime) > 0f || h != 0 && v == 0)) return;
            _repeat = dir == _heldDir ? 0.08f : 0.35f;
            _heldDir = dir;
            if (v != 0) _selected = Mathf.Clamp(_selected + v, 0, Mathf.Max(0, _rows.Count - 1));
            else if (_content.tabs.Length > 1)
            {
                _tab = (_tab + h + _content.tabs.Length) % _content.tabs.Length;
                _selected = 0;
                _scroll = 0;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_content == null) return;
            _rows = _content.rows(_tab) ?? new List<Row>();
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _rows.Count - 1));
            if (_selected < _scroll) _scroll = _selected;
            if (_selected >= _scroll + VisibleRows) _scroll = _selected - VisibleRows + 1;

            _title.SetText(_content.title);
            _header.SetText(_content.header?.Invoke() ?? "");
            int hw = font.MeasureWidth(_header.Text);
            _header.transform.localPosition = P(112 - hw, 62);
            _tabs.SetText(_content.tabs.Length > 1 ? $"← {_content.tabs[_tab]} →" : "");
            _hint.SetText(_content.hint);
            _empty.SetText(_rows.Count == 0 ? "No hay nada acá." : "");

            for (int i = 0; i < VisibleRows; i++)
            {
                int index = _scroll + i;
                var (icon, label, right) = _lines[i];
                if (index >= _rows.Count)
                {
                    icon.enabled = false;
                    label.SetText("");
                    right.SetText("");
                    continue;
                }
                var row = _rows[index];
                icon.enabled = row.icon != null;
                icon.sprite = row.icon;
                icon.color = row.enabled ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                label.SetColor(row.enabled ? Dark : Gray);
                right.SetColor(row.enabled ? MidGray : Gray);
                right.SetText(row.right ?? "");
                int rw = font.MeasureWidth(row.right ?? "");
                label.SetText(Fit(row.label, 81 - rw));
                right.transform.localPosition = P(-8 - rw, ListTop - i * RowHeight);
            }

            bool any = _rows.Count > 0;
            _cursor.gameObject.SetActive(any);
            _highlight.enabled = any;
            if (any)
            {
                float top = ListTop - (_selected - _scroll) * RowHeight;
                _cursor.transform.localPosition = P(-116, top);
                Place(_highlight, -108, top + 2, 102, RowHeight);
            }
            var detail = any && _content.detail != null ? _content.detail(_tab, _selected) : "";
            var lines = font.Wrap(detail ?? "", 108);
            int maxLines = Mathf.Max(1, 94 / Mathf.Max(1, font.lineHeight));
            if (lines.Count > maxLines)
            {
                lines = lines.GetRange(0, maxLines);
                lines[maxLines - 1] = Fit(lines[maxLines - 1] + "…", 108);
            }
            _detail.SetText(string.Join("\n", lines));
        }

        /// <summary>Corta el texto con "…" para que entre en el ancho dado.</summary>
        string Fit(string text, int maxWidth)
        {
            text ??= "";
            if (font.MeasureWidth(text) <= maxWidth) return text;
            while (text.Length > 1 && font.MeasureWidth(text + "…") > maxWidth) text = text.Substring(0, text.Length - 1);
            return text.TrimEnd() + "…";
        }

        // ------------------------------------------------------------------ Armado

        static Vector3 P(float x, float y) => new(x / Ppu, y / Ppu, 0f);

        void Build()
        {
            _root = new GameObject("List Screen");
            _root.transform.SetParent(transform, false);
            var bg = Renderer(boxSprite, baseOrder);
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(240f / Ppu, 135f / Ppu);

            _title = Text(-110, 62, Dark);
            _header = Text(60, 62, Dark);
            _tabs = Text(-110, 52, Red, false);
            _highlight = Renderer(pixelSprite, baseOrder + 1);
            _highlight.color = Highlight;
            for (int i = 0; i < VisibleRows; i++)
            {
                var icon = Renderer(null, baseOrder + 5);
                icon.transform.localScale = new Vector3(0.625f, 0.625f, 1f);
                icon.transform.localPosition = P(-102, ListTop - i * RowHeight - 4);
                _lines.Add((icon, Text(-92, ListTop - i * RowHeight, Dark), Text(-20, ListTop - i * RowHeight, MidGray)));
            }
            _cursor = Text(-116, ListTop, Red, false);
            _cursor.SetText("▶");
            _empty = Text(-100, ListTop, MidGray);
            var divider = Renderer(pixelSprite, baseOrder + 1);
            divider.color = Gray;
            Place(divider, 1, 50, 1, 96);
            _detail = Text(6, 46, Dark);
            _hint = Text(-110, -55, MidGray, false);
        }

        SpriteRenderer Renderer(Sprite sprite, int order)
        {
            var go = new GameObject("r");
            go.transform.SetParent(_root.transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            if (material != null) r.sharedMaterial = material;
            return r;
        }

        PixelText Text(float x, float y, Color color, bool shadow = true)
        {
            var go = new GameObject("t");
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = P(x, y);
            var t = go.AddComponent<PixelText>();
            t.Setup(font, material, baseOrder + 10, color, shadow);
            return t;
        }

        void Place(SpriteRenderer r, float x, float y, float w, float h)
        {
            float px = pixelSprite != null ? pixelSprite.rect.width : 2f;
            r.transform.localScale = new Vector3(w / px, h / px, 1f);
            r.transform.localPosition = P(x + w / 2f, y - h / 2f);
        }
    }
}
