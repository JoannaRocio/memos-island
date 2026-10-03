using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// "Mis Memos": todos tus Memos y el diario de vínculo de cada uno (GDD §11).
    /// ↑↓ elegir · ←→ página (Vínculo, Recuerdos, Amigos) · A acciones (equipo, compañero) · B cerrar.
    /// </summary>
    public class MyMemosScreen : MonoBehaviour
    {
        [SerializeField] PixelFont font;
        [SerializeField] Sprite boxSprite;
        [SerializeField] Sprite pixelSprite;
        [SerializeField] Material material;
        // Debajo de los diálogos (1000+), así las acciones aparecen encima.
        [SerializeField] int baseOrder = 600;

        const float Ppu = PixelFont.PixelsPerUnit;
        const int VisibleRows = 9, RowHeight = 11, ListTop = 50;
        static readonly string[] PageNames = { "Vínculo", "Recuerdos", "Amigos" };
        static readonly Color Dark = new Color32(0x33, 0x3c, 0x57, 0xff);
        static readonly Color Gray = new Color32(0x56, 0x6c, 0x86, 0xff);
        static readonly Color Light = new Color32(0x94, 0xb0, 0xc2, 0xff);
        static readonly Color Red = new Color32(0xb1, 0x3e, 0x53, 0xff);
        static readonly Color Green = new Color32(0x38, 0xb7, 0x64, 0xff);
        static readonly Color Gold = new Color32(0xef, 0x7d, 0x57, 0xff);
        static readonly Color Highlight = new Color32(0x73, 0xef, 0xf7, 0xff);

        public bool IsOpen { get; private set; }

        List<MemoInstance> _memos;
        int _selected, _scroll, _page;
        float _repeat;
        int _heldDir;
        bool _actionsOpen;
        int _openedFrame;

        GameObject _root, _bondPage;
        readonly List<PixelText> _rows = new();
        readonly List<PixelText> _rowTags = new();
        PixelText _cursor, _title, _header, _subtitle, _hearts, _trustText, _pageText, _hint;
        readonly List<(PixelText label, SpriteRenderer fill)> _needBars = new();
        SpriteRenderer _highlight, _portrait;

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

        public void Open(MemoInstance focus)
        {
            var state = GameRoot.Instance.State;
            _memos = state.team.Concat(state.refuge).ToList();
            _selected = focus != null ? Mathf.Max(0, _memos.IndexOf(focus)) : 0;
            _page = 0;
            _openedFrame = Time.frameCount;
            IsOpen = true;
            GameRoot.InputLocks++;
            _root.SetActive(true);
            Refresh();
        }

        void Close()
        {
            GameRoot.ConsumeUiInput();
            IsOpen = false;
            GameRoot.InputLocks--;
            _root.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen || _actionsOpen || GameRoot.Instance.Dialogue.IsOpen || Time.frameCount == _openedFrame) return;
            if (GameInput.CancelPressed || GameInput.MenuPressed)
            {
                Close();
                return;
            }
            if (GameInput.ConfirmPressed)
            {
                ShowActions();
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
            if (v != 0) _selected = Mathf.Clamp(_selected + v, 0, _memos.Count - 1);
            else _page = (_page + h + PageNames.Length) % PageNames.Length;
            Refresh();
        }

        void ShowActions()
        {
            var root = GameRoot.Instance;
            var state = root.State;
            var memo = _memos[_selected];
            bool inTeam = state.IsInTeam(memo);
            bool isCompanion = state.companionUid == memo.uid;
            var labels = new List<string>
            {
                inTeam ? "Sacar del equipo" : "Poner en el equipo",
                isCompanion ? "Dejar en el refugio" : "Que me acompañe",
                "Volver",
            };
            _actionsOpen = true;
            root.Dialogue.ShowChoice($"¿Qué hacés con {memo.DisplayName}?", labels, i =>
            {
                _actionsOpen = false;
                if (i == 0)
                {
                    if (!state.ToggleTeam(memo))
                        root.Dialogue.Show(new[] { inTeam ? "El equipo no puede quedar vacío." : "El equipo ya tiene 6 Memos." });
                }
                else if (i == 1)
                {
                    if (!isCompanion && memo.TrustLevel < TrustLevel.Neutral)
                    {
                        root.Dialogue.Show(new[] { $"{memo.DisplayName} todavía no confía lo suficiente para acompañarte." });
                    }
                    else
                    {
                        state.companionUid = isCompanion ? null : memo.uid;
                        if (isCompanion) memo.inside = false;
                        if (World.RefugeManager.Current != null) World.RefugeManager.Current.OnCompanionChanged();
                        else root.Companion.Refresh();
                    }
                }
                _memos = state.team.Concat(state.refuge).ToList();
                _selected = Mathf.Clamp(_memos.IndexOf(memo), 0, _memos.Count - 1);
                Refresh();
            }, labels.Count - 1);
        }

        // ------------------------------------------------------------------ Contenido

        void Refresh()
        {
            var state = GameRoot.Instance.State;
            if (_selected < _scroll) _scroll = _selected;
            if (_selected >= _scroll + VisibleRows) _scroll = _selected - VisibleRows + 1;
            _title.SetText($"Mis Memos  {_memos.Count}");

            for (int i = 0; i < VisibleRows; i++)
            {
                int index = _scroll + i;
                if (index >= _memos.Count)
                {
                    _rows[i].SetText("");
                    _rowTags[i].SetText("");
                    continue;
                }
                var m = _memos[index];
                _rows[i].SetText($"{m.DisplayName} {m.level}");
                string tag = (state.IsInTeam(m) ? "E" : "") + (state.companionUid == m.uid ? "♪" : "");
                _rowTags[i].SetColor(Green);
                _rowTags[i].SetText(tag);
            }
            float rowTop = ListTop - (_selected - _scroll) * RowHeight;
            _cursor.transform.localPosition = P(-113, rowTop);
            Place(_highlight, -105, rowTop + 1, 86, RowHeight);

            var memo = _memos[_selected];
            var species = memo.Species;
            _header.SetText($"{memo.DisplayName.ToUpperInvariant()}  {PageNames[_page]}");
            _bondPage.SetActive(_page == 0);
            _pageText.gameObject.SetActive(_page != 0);
            _hint.SetText("←→ página  A: acciones");

            if (_page == 0)
            {
                _portrait.sprite = memo.RaceFrames is { Length: > 0 } f ? f[0] : null;
                var temperament = memo.Temperament != null ? memo.Temperament.displayName : "-";
                _subtitle.SetText($"{species?.displayName} Nv.{memo.level}\n{temperament}");
                var level = memo.TrustLevel;
                int filled = Mathf.Max(0, (int)level);
                _hearts.SetText(new string('♥', filled) + new string('·', 6 - filled));
                _trustText.SetColor(level <= TrustLevel.Fear ? Red : level >= TrustLevel.Friend ? Green : Dark);
                _trustText.SetText(TrustRules.Name(level));
                var needs = new[] { Need.Hunger, Need.Sleep, Need.Fun, Need.Social };
                for (int i = 0; i < needs.Length; i++)
                {
                    float v = MemoNeeds.Value(memo, needs[i]);
                    var (_, fill) = _needBars[i];
                    fill.color = v < 30f ? Red : v < 60f ? Gold : Green;
                    var o = NeedBarOrigin(i);
                    fill.enabled = v > 1f;
                    if (v > 1f) Place(fill, o.x, o.y, v / 100f * 30f, 4);
                }
            }
            else if (_page == 1)
            {
                var memories = MemoryBook.Unlocked(memo);
                memories.Reverse(); // el más nuevo primero
                var text = memories.Count > 0 ? string.Join("\n\n", memories) : "Todavía no recuerda nada con vos.";
                var lines = font.Wrap(text, 124);
                _pageText.SetText(string.Join("\n", lines.Take(8)));
            }
            else
            {
                var lines = new List<string>();
                foreach (var other in _memos.Where(o => o != memo))
                {
                    var level = Friendship.Level(state.relationships, memo.uid, other.uid);
                    if (level > FriendshipLevel.Strangers) lines.Add($"{other.DisplayName}: {Friendship.Name(level)}");
                }
                if (lines.Count == 0) lines.Add("Todavía no tiene amigos.");
                var godparent = string.IsNullOrEmpty(memo.godparentUid) ? null : state.Find(memo.godparentUid);
                if (godparent != null) lines.Add($"Padrino: {godparent.DisplayName}");
                foreach (var child in _memos.Where(o => o.godparentUid == memo.uid))
                    lines.Add($"Apadrina a {child.DisplayName}");
                _pageText.SetText(string.Join("\n", lines.Take(8)));
            }
        }

        // ------------------------------------------------------------------ Armado

        static Vector3 P(float x, float y) => new(x / Ppu, y / Ppu, 0f);
        static Vector2 NeedBarOrigin(int i) => new(i % 2 == 0 ? 6 : 70, i < 2 ? -30 : -40);

        void Build()
        {
            _root = new GameObject("My Memos");
            _root.transform.SetParent(transform, false);
            var bg = Renderer(_root.transform, boxSprite, baseOrder);
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(240f / Ppu, 135f / Ppu);

            _title = Text(-110, 62, Dark);
            _highlight = Renderer(_root.transform, pixelSprite, baseOrder + 1);
            _highlight.color = Highlight;
            for (int i = 0; i < VisibleRows; i++)
            {
                _rows.Add(Text(-103, ListTop - i * RowHeight, Dark));
                _rowTags.Add(Text(-30, ListTop - i * RowHeight, Green));
            }
            _cursor = Text(-113, ListTop, Red, false);
            _cursor.SetText("▶");
            var divider = Renderer(_root.transform, pixelSprite, baseOrder + 1);
            divider.color = Light;
            Place(divider, -16, 58, 1, 112);

            _header = Text(-10, 62, Dark);

            _bondPage = new GameObject("Bond");
            _bondPage.transform.SetParent(_root.transform, false);
            _portrait = Renderer(_bondPage.transform, null, baseOrder + 5);
            _portrait.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            _portrait.transform.localPosition = P(14, 4);
            _subtitle = Text(44, 48, Gray, parent: _bondPage.transform);
            _trustText = Text(44, 24, Dark, parent: _bondPage.transform);
            _hearts = Text(44, 13, Red, parent: _bondPage.transform);
            var needs = new[] { Need.Hunger, Need.Sleep, Need.Fun, Need.Social };
            for (int i = 0; i < needs.Length; i++)
            {
                var o = NeedBarOrigin(i);
                var label = Text(o.x - 18, o.y + 3, Dark, parent: _bondPage.transform);
                label.SetText(MemoNeeds.Name(needs[i]).Substring(0, 3));
                var back = Renderer(_bondPage.transform, pixelSprite, baseOrder + 2);
                back.color = new Color32(0xd9, 0xe4, 0xec, 0xff);
                Place(back, o.x, o.y, 30, 4);
                var fill = Renderer(_bondPage.transform, pixelSprite, baseOrder + 3);
                _needBars.Add((label, fill));
            }

            _pageText = Text(-10, 50, Dark);
            _hint = Text(-10, -55, Gray, false);
        }

        SpriteRenderer Renderer(Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject("r");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            if (material != null) r.sharedMaterial = material;
            return r;
        }

        PixelText Text(float x, float y, Color color, bool shadow = true, Transform parent = null)
        {
            var go = new GameObject("t");
            go.transform.SetParent(parent ?? _root.transform, false);
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
