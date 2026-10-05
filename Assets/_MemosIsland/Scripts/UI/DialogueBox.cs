using System;
using System.Collections;
using System.Collections.Generic;
using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// Caja de texto estilo GBA: escribe letra por letra, A acelera o avanza, flechita roja al terminar cada página.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        [SerializeField] GameObject visuals;
        [SerializeField] PixelText text;
        [SerializeField] PixelText arrow;
        [SerializeField] int maxLineWidth = 206; // en pixels de interfaz
        [SerializeField] int linesPerPage = 2;

        public bool IsOpen { get; private set; }

        public void Setup(GameObject visualsRoot, PixelText body, PixelText nextArrow)
        {
            visuals = visualsRoot;
            text = body;
            arrow = nextArrow;
        }

        void Awake() => visuals.SetActive(false);

        // ------------------------------------------------------------------ Quién habla (retrato)

        string _speakerName;
        Sprite _speakerPortrait;
        GameObject _portraitRoot;
        SpriteRenderer _portraitImage;
        SpriteRenderer _nameBox;
        PixelText _nameText;

        /// <summary>Retrato y nombre para el próximo diálogo (se borran al cerrarlo).</summary>
        public void SetSpeaker(string speakerName, Sprite portrait)
        {
            _speakerName = speakerName;
            _speakerPortrait = portrait;
        }

        void ShowSpeaker()
        {
            bool show = _speakerPortrait != null || !string.IsNullOrEmpty(_speakerName);
            if (!show)
            {
                if (_portraitRoot != null) _portraitRoot.SetActive(false);
                return;
            }
            if (_portraitRoot == null) BuildSpeaker();
            _portraitRoot.SetActive(true);
            _portraitImage.transform.parent.gameObject.SetActive(_speakerPortrait != null);
            _portraitImage.sprite = _speakerPortrait;
            _nameText.SetText(_speakerName ?? "");
            int w = text.Font.MeasureWidth(_speakerName ?? "") + 16;
            float left = _speakerPortrait != null ? -58f : -116f;
            // Caja de 18 px de alto apoyada sobre la caja de diálogo, con margen para que el texto no toque el borde.
            _nameBox.size = new Vector2(w / Ppu, 18f / Ppu);
            _nameBox.transform.localPosition = new Vector3((left + w / 2f) / Ppu, -12.5f / Ppu, 0f);
            _nameText.transform.localPosition = new Vector3((left + 8f) / Ppu, -8.5f / Ppu, 0f);
        }

        void BuildSpeaker()
        {
            var source = visuals.transform.Find("Box").GetComponent<SpriteRenderer>();
            _portraitRoot = new GameObject("Speaker");
            _portraitRoot.transform.SetParent(transform, false);

            SpriteRenderer Box(string name, int order)
            {
                var r = new GameObject(name).AddComponent<SpriteRenderer>();
                r.transform.SetParent(_portraitRoot.transform, false);
                r.sprite = source.sprite;
                r.drawMode = SpriteDrawMode.Sliced;
                r.sharedMaterial = source.sharedMaterial;
                r.sortingOrder = order;
                return r;
            }

            // Marco de 56x56 a la izquierda, apoyado sobre la caja de diálogo; el retrato es de 48x48.
            var frame = Box("Portrait Frame", source.sortingOrder);
            frame.size = new Vector2(56f / Ppu, 56f / Ppu);
            frame.transform.localPosition = new Vector3(-88f / Ppu, 6.5f / Ppu, 0f);
            _portraitImage = new GameObject("Portrait").AddComponent<SpriteRenderer>();
            _portraitImage.transform.SetParent(frame.transform, false);
            _portraitImage.sharedMaterial = source.sharedMaterial;
            _portraitImage.sortingOrder = source.sortingOrder + 5;

            _nameBox = Box("Name Box", source.sortingOrder);
            _nameText = new GameObject("Name").AddComponent<PixelText>();
            _nameText.transform.SetParent(_portraitRoot.transform, false);
            _nameText.Setup(text.Font, text.Material, source.sortingOrder + 10, new Color32(0xb1, 0x3e, 0x53, 0xff), true);
            _portraitRoot.SetActive(false);
        }

        /// <summary>Reemplaza las etiquetas de nombre y pronombre ({nombre}, {o/a/e}…) según el personaje.</summary>
        static string Tags(string s)
        {
            var root = GameRoot.Instance;
            return root != null ? Story.TextTags.Apply(s, root.State.story.profile) : s;
        }

        public void Show(IEnumerable<string> pages, Action onClosed = null)
        {
            if (IsOpen) return;
            var tagged = new List<string>();
            foreach (var p in pages) tagged.Add(Tags(p));
            StartCoroutine(Run(tagged, onClosed));
        }

        /// <summary>Corta los textos en páginas de pocos renglones que entran en la caja.</summary>
        public static List<string> Paginate(PixelFont font, IEnumerable<string> pages, int maxWidth, int linesPerPage)
        {
            var result = new List<string>();
            foreach (var page in pages)
            {
                var lines = font.Wrap(page, maxWidth);
                for (int i = 0; i < lines.Count; i += linesPerPage)
                    result.Add(string.Join("\n", lines.GetRange(i, Mathf.Min(linesPerPage, lines.Count - i))));
            }
            return result;
        }

        /// <summary>
        /// Pregunta con opciones (↑↓ elige, A confirma). B elige cancelIndex si es ≥ 0.
        /// </summary>
        public void ShowChoice(string question, IList<string> options, Action<int> onChosen, int cancelIndex = -1)
        {
            if (IsOpen) return;
            var tagged = new List<string>();
            foreach (var o in options) tagged.Add(Tags(o));
            StartCoroutine(RunChoice(Tags(question), tagged, onChosen, cancelIndex));
        }

        IEnumerator Run(IEnumerable<string> pages, Action onClosed)
        {
            Open();
            foreach (var page in Paginate(text.Font, pages, maxLineWidth, linesPerPage))
            {
                yield return TypePage(page);
                float blink = 0f;
                while (!GameInput.ConfirmPressed && !GameInput.CancelPressed)
                {
                    blink += Time.unscaledDeltaTime;
                    arrow.gameObject.SetActive(blink % 0.6f < 0.4f);
                    yield return null;
                }
            }
            Close();
            onClosed?.Invoke();
        }

        IEnumerator RunChoice(string question, IList<string> options, Action<int> onChosen, int cancelIndex)
        {
            Open();
            var pages = Paginate(text.Font, new[] { question }, maxLineWidth, linesPerPage);
            yield return TypePage(pages[pages.Count - 1]);

            BuildChoiceBox(options);
            int selected = 0;
            int chosen = -1;
            yield return null;
            while (chosen < 0)
            {
                var move = GameInput.Move;
                if (_choiceRepeat > 0f) _choiceRepeat -= Time.unscaledDeltaTime;
                int dir = move.y > 0.5f ? -1 : move.y < -0.5f ? 1 : 0;
                if (dir == 0) _choiceRepeat = 0f;
                else if (_choiceRepeat <= 0f)
                {
                    selected = (selected + dir + options.Count) % options.Count;
                    _choiceRepeat = 0.2f;
                    AudioManager.Sfx("cursor", 0.6f);
                }
                _choiceCursor.transform.localPosition = _choiceRowPositions[selected];

                if (GameInput.ConfirmPressed)
                {
                    chosen = selected;
                    AudioManager.Sfx("confirm", 0.7f);
                }
                else if (GameInput.CancelPressed && cancelIndex >= 0)
                {
                    chosen = cancelIndex;
                    AudioManager.Sfx("cancel", 0.7f);
                }
                yield return null;
            }
            _choiceRoot.SetActive(false);
            Close();
            onChosen?.Invoke(chosen);
        }

        IEnumerator TypePage(string page)
        {
            arrow.gameObject.SetActive(false);
            text.SetText(page);
            text.VisibleCharacters = 0;
            int total = text.CharacterCount;
            float shown = 0f;
            yield return null;
            while (text.VisibleCharacters < total)
            {
                if (GameInput.ConfirmPressed || GameInput.CancelPressed)
                {
                    text.VisibleCharacters = total;
                    yield return null;
                    break;
                }
                shown += GameSettings.CharactersPerSecond * Time.unscaledDeltaTime;
                int before = text.VisibleCharacters;
                text.VisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                // Blip de texto, una letra de cada tres (como en los juegos de GBA).
                if (text.VisibleCharacters / 3 != before / 3) AudioManager.Sfx("blip", 0.35f);
                yield return null;
            }
        }

        void Open()
        {
            IsOpen = true;
            GameRoot.InputLocks++;
            visuals.SetActive(true);
            ShowSpeaker();
        }

        void Close()
        {
            visuals.SetActive(false);
            if (_portraitRoot != null) _portraitRoot.SetActive(false);
            _speakerName = null;
            _speakerPortrait = null;
            GameRoot.InputLocks--;
            IsOpen = false;
        }

        // ------------------------------------------------------------------ Caja de opciones

        GameObject _choiceRoot;
        SpriteRenderer _choiceBox;
        PixelText _choiceCursor;
        readonly List<PixelText> _choiceRows = new();
        readonly List<Vector3> _choiceRowPositions = new();
        float _choiceRepeat;

        const float Ppu = PixelFont.PixelsPerUnit;
        const int ChoiceRowHeight = 10;

        /// <summary>Caja con las opciones, a la derecha y apoyada sobre la caja de diálogo (pixels de interfaz).</summary>
        void BuildChoiceBox(IList<string> options)
        {
            if (_choiceRoot == null)
            {
                _choiceRoot = new GameObject("Choice");
                _choiceRoot.transform.SetParent(transform, false);
                var source = visuals.transform.Find("Box").GetComponent<SpriteRenderer>();
                _choiceBox = new GameObject("Box").AddComponent<SpriteRenderer>();
                _choiceBox.transform.SetParent(_choiceRoot.transform, false);
                _choiceBox.sprite = source.sprite;
                _choiceBox.drawMode = SpriteDrawMode.Sliced;
                _choiceBox.sharedMaterial = source.sharedMaterial;
                _choiceBox.sortingOrder = source.sortingOrder + 20;
                _choiceCursor = NewText("Cursor", new Color32(0xb1, 0x3e, 0x53, 0xff), false);
                _choiceCursor.SetText("▶");
            }

            while (_choiceRows.Count < options.Count)
                _choiceRows.Add(NewText($"Row{_choiceRows.Count}", new Color32(0x33, 0x3c, 0x57, 0xff), true));
            int width = 0;
            foreach (var o in options) width = Mathf.Max(width, text.Font.MeasureWidth(o));
            width += 22;
            int height = options.Count * ChoiceRowHeight + 10;
            float right = 116f, bottom = -19f;
            float left = right - width, top = bottom + height;

            _choiceBox.size = new Vector2(width / Ppu, height / Ppu);
            _choiceBox.transform.localPosition = new Vector3((left + width / 2f) / Ppu, (bottom + height / 2f) / Ppu, 0f);
            _choiceRowPositions.Clear();
            for (int i = 0; i < _choiceRows.Count; i++)
            {
                bool used = i < options.Count;
                _choiceRows[i].gameObject.SetActive(used);
                if (!used) continue;
                _choiceRows[i].SetText(options[i]);
                float y = top - 5 - i * ChoiceRowHeight + 1;
                _choiceRows[i].transform.localPosition = new Vector3((left + 14) / Ppu, y / Ppu, 0f);
                _choiceRowPositions.Add(new Vector3((left + 6) / Ppu, y / Ppu, 0f));
            }
            _choiceRoot.SetActive(true);
        }

        PixelText NewText(string name, Color color, bool shadow)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_choiceRoot.transform, false);
            var t = go.AddComponent<PixelText>();
            t.Setup(text.Font, text.Material, _choiceBox.sortingOrder + 10, color, shadow);
            return t;
        }
    }
}
