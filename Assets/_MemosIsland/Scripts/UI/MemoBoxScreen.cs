using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// La MemoBox: enciclopedia de Memos (se abre desde el menú de pausa). B o Esc cierra · ↑↓ elige · A pasa de página (stats, info, habilidad) ·
    /// ←→ muestra la versión normal, brillante o con collar · B cierra.
    /// Arma su propia interfaz al iniciar (coordenadas en pixels de interfaz: x ±120, y ±67.5).
    /// </summary>
    public class MemoBoxScreen : MonoBehaviour
    {
        [SerializeField] PixelFont font;
        [SerializeField] Sprite boxSprite;
        [SerializeField] Sprite pixelSprite;
        [SerializeField] Material material;
        [SerializeField] int baseOrder = 1100;

        const float Ppu = PixelFont.PixelsPerUnit;
        const int VisibleRows = 10, RowHeight = 10, ListTop = 50;
        const float RepeatDelay = 0.35f, RepeatRate = 0.07f;

        static readonly Color Dark = new Color32(0x33, 0x3c, 0x57, 0xff);
        static readonly Color Gray = new Color32(0x56, 0x6c, 0x86, 0xff);
        static readonly Color LineGray = new Color32(0x94, 0xb0, 0xc2, 0xff);
        static readonly Color Red = new Color32(0xb1, 0x3e, 0x53, 0xff);
        static readonly Color Highlight = new Color32(0x73, 0xef, 0xf7, 0xff);
        static readonly Color BarBack = new Color32(0xd9, 0xe4, 0xec, 0xff);
        static readonly Color Silhouette = new(0.16f, 0.2f, 0.36f);
        static readonly Color[] StatColors =
        {
            new Color32(0xb1, 0x3e, 0x53, 0xff), new Color32(0xef, 0x7d, 0x57, 0xff),
            new Color32(0x38, 0xb7, 0x64, 0xff), new Color32(0x3b, 0x5d, 0xc9, 0xff),
        };
        static readonly string[] StatNames = { "VEL", "ACE", "RES", "CAR" };
        static readonly string[] VariantNames = { "Normal", "Brillante", "Con collar" };

        public bool IsOpen { get; private set; }
        int _openedFrame;

        List<MemoSpecies> _species;
        int _selected, _scroll, _variant;
        int _page; // 0 = stats, 1 = descripción, 2 = habilidad
        float _repeatTimer;
        int _heldDir;
        float _animTime;

        GameObject _root, _statsPage, _infoPageRoot;
        readonly List<PixelText> _rows = new();
        PixelText _cursor, _title, _header, _variantLabel, _hint, _infoText, _abilityLabel, _abilityName;
        readonly List<PixelText> _facts = new();
        SpriteRenderer _highlight, _portrait;
        readonly List<(SpriteRenderer fill, PixelText value)> _bars = new();

        public void Setup(PixelFont newFont, Sprite box, Sprite pixel, Material mat)
        {
            font = newFont;
            boxSprite = box;
            pixelSprite = pixel;
            material = mat;
        }

        void Awake()
        {
            BuildUi();
            _root.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen || Time.frameCount == _openedFrame) return;

            if (GameInput.CancelPressed || GameInput.MenuPressed)
            {
                Close();
                return;
            }
            if (GameInput.ConfirmPressed)
            {
                _page = (_page + 1) % 3;
                Refresh();
            }
            HandleNavigation();
            AnimatePortrait();
        }

        public void Open()
        {
            var db = MemoDatabase.Instance;
            if (db == null)
            {
                Debug.LogError("[MemoBox] Falta Resources/MemoDatabase. Usá Memos Island ▸ Fase 2 ▸ Crear datos de Memos.");
                return;
            }
            _species = db.SpeciesByNumber.ToList();
            _openedFrame = Time.frameCount;
            IsOpen = true;
            GameRoot.InputLocks++;
            _root.SetActive(true);
            _variant = 0;
            Refresh();
        }

        public void Close()
        {
            GameRoot.ConsumeUiInput();
            IsOpen = false;
            GameRoot.InputLocks--;
            _root.SetActive(false);
        }

        // ------------------------------------------------------------------ Entrada

        void HandleNavigation()
        {
            var move = GameInput.Move;
            int vertical = move.y > 0.5f ? -1 : move.y < -0.5f ? 1 : 0;
            int horizontal = move.x > 0.5f ? 1 : move.x < -0.5f ? -1 : 0;
            int dir = vertical != 0 ? vertical * 10 : horizontal; // códigos distintos para ↑↓ y ←→

            if (dir == 0)
            {
                _heldDir = 0;
                return;
            }
            if (dir == _heldDir)
            {
                _repeatTimer -= Time.unscaledDeltaTime;
                if (_repeatTimer > 0f || horizontal != 0 && vertical == 0) return; // ←→ no se repite
                _repeatTimer = RepeatRate;
            }
            else
            {
                _heldDir = dir;
                _repeatTimer = RepeatDelay;
            }

            if (vertical != 0)
            {
                _selected = Mathf.Clamp(_selected + vertical, 0, _species.Count - 1);
                _variant = 0;
            }
            else
            {
                _variant = (_variant + horizontal + VariantNames.Length) % VariantNames.Length;
            }
            Refresh();
        }

        void AnimatePortrait()
        {
            var frames = CurrentFrames();
            if (frames == null || frames.Length == 0) return;
            _animTime += Time.unscaledDeltaTime;
            _portrait.sprite = frames[(int)(_animTime * 6f) % frames.Length];
        }

        Sprite[] CurrentFrames()
        {
            if (_species == null || _species.Count == 0) return null;
            var s = _species[_selected];
            var frames = _variant switch
            {
                1 => s.shinyRaceFrames,
                2 => s.collarRaceFrames,
                _ => s.raceFrames
            };
            return frames != null && frames.Length > 0 ? frames : s.raceFrames;
        }

        // ------------------------------------------------------------------ Contenido

        void Refresh()
        {
            if (_selected < _scroll) _scroll = _selected;
            if (_selected >= _scroll + VisibleRows) _scroll = _selected - VisibleRows + 1;

            _title.SetText($"MemoBox  {_species.Count}");
            for (int i = 0; i < VisibleRows; i++)
            {
                int index = _scroll + i;
                var row = _rows[i];
                if (index >= _species.Count)
                {
                    row.SetText("");
                    continue;
                }
                var s = _species[index];
                row.SetColor(s.availability == MemoAvailability.Locked ? Gray : Dark);
                row.SetText($"{s.number:000} {s.displayName}");
            }
            int cursorRow = _selected - _scroll;
            float rowTop = ListTop - cursorRow * RowHeight;
            _cursor.transform.localPosition = P(-111, rowTop);
            Place(_highlight, -105, rowTop + 1, 80, RowHeight);

            var sp = _species[_selected];
            bool locked = sp.availability == MemoAvailability.Locked;
            _header.SetText($"{sp.number:000} {sp.displayName.ToUpperInvariant()}");

            _statsPage.SetActive(_page == 0);
            _infoPageRoot.SetActive(_page != 0);
            _portrait.color = locked ? Silhouette : Color.white;
            _animTime = 0f;
            AnimatePortrait();
            _variantLabel.SetText(locked ? "" : $"← {VariantNames[_variant]} →");

            if (_page == 0)
            {
                FillFacts(sp, locked);
                var stats = new[] { sp.baseStats.speed, sp.baseStats.acceleration, sp.baseStats.stamina, sp.baseStats.charge };
                for (int i = 0; i < 4; i++)
                {
                    var (fill, value) = _bars[i];
                    int w = locked ? 0 : stats[i] * 3;
                    var origin = BarOrigin(i);
                    fill.enabled = w > 0;
                    if (w > 0) Place(fill, origin.x, origin.y, w, 5);
                    value.SetText(locked ? "?" : stats[i].ToString());
                }
                if (locked)
                {
                    _abilityLabel.SetColor(Red);
                    _abilityLabel.SetText("ZONA INACCESIBLE");
                    _abilityName.SetText("Todavía no se puede llegar…");
                }
                else
                {
                    _abilityLabel.SetColor(Gray);
                    _abilityLabel.SetText("Habilidad");
                    _abilityName.SetText(sp.ability != null ? sp.ability.displayName : "-");
                }
            }
            else
            {
                string text;
                if (_page == 1)
                {
                    text = $"Hábitat: {sp.habitat}\n\n{sp.description}";
                }
                else
                {
                    text = locked || sp.ability == null
                        ? "Habilidad: ???"
                        : $"Habilidad: {sp.ability.displayName}\n\n{sp.ability.description}";
                    if (sp.availability == MemoAvailability.NotObtainable) text += "\n\nNo se puede conseguir.";
                    if (sp.evolvesTo != null && !locked)
                        text += $"\n\nEvoluciona en {sp.evolvesTo.displayName} (nivel {sp.evolutionLevel}).";
                }
                _infoText.SetText(string.Join("\n", font.Wrap(text, 124)));
            }
            _hint.SetText($"A: página {_page + 1}/3  B: salir");
        }

        void FillFacts(MemoSpecies s, bool locked)
        {
            var lines = new List<(string text, Color color)>();
            if (locked)
            {
                lines.Add(("???", Dark));
            }
            else
            {
                lines.Add((s.primaryType.displayName, s.primaryType.color));
                if (s.secondaryType != null) lines.Add((s.secondaryType.displayName, s.secondaryType.color));
                lines.Add((MemoSpecies.MobilityName(s.mobility), Dark));
            }
            lines.Add((MemoSpecies.CategoryName(s.category), Gray));
            for (int i = 0; i < _facts.Count; i++)
            {
                if (i < lines.Count)
                {
                    _facts[i].SetColor(lines[i].color);
                    _facts[i].SetText(lines[i].text);
                }
                else
                {
                    _facts[i].SetText("");
                }
            }
        }

        // ------------------------------------------------------------------ Armado de la interfaz

        static Vector3 P(float x, float y) => new(x / Ppu, y / Ppu, 0f);

        static Vector2 BarOrigin(int i) => new(i % 2 == 0 ? 2 : 66, i < 2 ? -13 : -23); // barras de 30 px (3 por punto)

        void BuildUi()
        {
            _root = new GameObject("MemoBox");
            _root.transform.SetParent(transform, false);

            var bg = NewRenderer(_root.transform, "Box", boxSprite, baseOrder);
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(240f / Ppu, 135f / Ppu);

            _title = NewText(_root.transform, "Title", -110, 62, Dark);
            _highlight = NewRenderer(_root.transform, "Highlight", pixelSprite, baseOrder + 1);
            _highlight.color = Highlight;
            for (int i = 0; i < VisibleRows; i++)
                _rows.Add(NewText(_root.transform, $"Row{i}", -103, ListTop - i * RowHeight, Dark));
            _cursor = NewText(_root.transform, "Cursor", -111, ListTop, Red, false);
            _cursor.SetText("▶");

            var divider = NewRenderer(_root.transform, "Divider", pixelSprite, baseOrder + 1);
            divider.color = LineGray;
            Place(divider, -21, 58, 1, 118);

            _header = NewText(_root.transform, "Header", -14, 62, Dark);

            // Página de stats
            _statsPage = new GameObject("Stats");
            _statsPage.transform.SetParent(_root.transform, false);
            _portrait = NewRenderer(_statsPage.transform, "Portrait", null, baseOrder + 5);
            _portrait.transform.localPosition = P(20, -6);
            _variantLabel = NewText(_statsPage.transform, "Variant", 56, 2, Gray);
            for (int i = 0; i < 5; i++)
                _facts.Add(NewText(_statsPage.transform, $"Fact{i}", 58, 44 - i * 11, Dark));
            for (int i = 0; i < 4; i++)
            {
                var o = BarOrigin(i);
                var label = NewText(_statsPage.transform, $"{StatNames[i]}Label", o.x - 16, o.y + 2, Dark);
                label.SetText(StatNames[i]);
                var back = NewRenderer(_statsPage.transform, $"{StatNames[i]}Back", pixelSprite, baseOrder + 2);
                back.color = BarBack;
                Place(back, o.x, o.y, 30, 5);
                var fill = NewRenderer(_statsPage.transform, $"{StatNames[i]}Fill", pixelSprite, baseOrder + 3);
                fill.color = StatColors[i];
                var value = NewText(_statsPage.transform, $"{StatNames[i]}Value", o.x + 33, o.y + 2, Dark);
                _bars.Add((fill, value));
            }
            _abilityLabel = NewText(_statsPage.transform, "AbilityLabel", -14, -33, Gray);
            _abilityName = NewText(_statsPage.transform, "AbilityName", -14, -44, Dark);

            // Página de información
            _infoPageRoot = new GameObject("Info");
            _infoPageRoot.transform.SetParent(_root.transform, false);
            _infoText = NewText(_infoPageRoot.transform, "Text", -14, 50, Dark);

            _hint = NewText(_root.transform, "Hint", 20, -53, Gray, false);
        }

        SpriteRenderer NewRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            if (material != null) r.sharedMaterial = material;
            return r;
        }

        PixelText NewText(Transform parent, string name, float x, float y, Color color, bool shadow = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = P(x, y);
            var t = go.AddComponent<PixelText>();
            t.Setup(font, material, baseOrder + 10, color, shadow);
            return t;
        }

        /// <summary>Estira el pixel blanco a un rectángulo (x, y = esquina superior izquierda, en pixels de interfaz).</summary>
        void Place(SpriteRenderer r, float x, float y, float w, float h)
        {
            float spritePx = pixelSprite != null ? pixelSprite.rect.width : 2f;
            r.transform.localScale = new Vector3(w / spritePx, h / spritePx, 1f);
            r.transform.localPosition = P(x + w / 2f, y - h / 2f);
        }
    }
}
