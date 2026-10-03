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
        [SerializeField] float charactersPerSecond = 45f;

        public bool IsOpen { get; private set; }

        public void Setup(GameObject visualsRoot, PixelText body, PixelText nextArrow)
        {
            visuals = visualsRoot;
            text = body;
            arrow = nextArrow;
        }

        void Awake() => visuals.SetActive(false);

        public void Show(IEnumerable<string> pages, Action onClosed = null)
        {
            if (IsOpen) return;
            StartCoroutine(Run(pages, onClosed));
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

        IEnumerator Run(IEnumerable<string> pages, Action onClosed)
        {
            IsOpen = true;
            GameRoot.InputLocks++;
            visuals.SetActive(true);

            foreach (var page in Paginate(text.Font, pages, maxLineWidth, linesPerPage))
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
                    shown += charactersPerSecond * Time.deltaTime;
                    text.VisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                    yield return null;
                }

                float blink = 0f;
                while (!GameInput.ConfirmPressed && !GameInput.CancelPressed)
                {
                    blink += Time.deltaTime;
                    arrow.gameObject.SetActive(blink % 0.6f < 0.4f);
                    yield return null;
                }
            }

            visuals.SetActive(false);
            GameRoot.InputLocks--;
            IsOpen = false;
            onClosed?.Invoke();
        }
    }
}
