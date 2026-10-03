using System;
using System.Collections;
using System.Collections.Generic;
using MemosIsland.Core;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// Escena de evolución estilo Pokémon (GDD §13): el sprite parpadea entre el Memo y su evolución,
    /// cada vez más rápido, hasta un destello. Con B se cancela. Al evolucionar empieza la etapa rebelde.
    /// </summary>
    public class EvolutionScreen : MonoBehaviour
    {
        [SerializeField] Sprite pixelSprite;
        [SerializeField] Material material;
        // Encima del HUD de carrera (900) y debajo de los diálogos (1000).
        [SerializeField] int baseOrder = 950;

        const float Ppu = PixelFont.PixelsPerUnit;

        GameObject _root;
        SpriteRenderer _background, _sprite, _flash;

        public bool IsRunning { get; private set; }

        public void Setup(Sprite pixel, Material mat)
        {
            pixelSprite = pixel;
            material = mat;
        }

        void Awake()
        {
            _root = new GameObject("Evolution");
            _root.transform.SetParent(transform, false);
            _background = NewRenderer("Background", pixelSprite, baseOrder);
            _background.color = new Color32(0x1a, 0x1c, 0x2c, 0xff);
            Stretch(_background, 260, 150);
            _sprite = NewRenderer("Memo", null, baseOrder + 1);
            _sprite.transform.localPosition = new Vector3(0f, -12f / Ppu, 0f);
            _flash = NewRenderer("Flash", pixelSprite, baseOrder + 2);
            Stretch(_flash, 260, 150);
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _root.SetActive(false);
        }

        SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            if (material != null) r.sharedMaterial = material;
            return r;
        }

        void Stretch(SpriteRenderer r, float w, float h)
        {
            float px = pixelSprite != null ? pixelSprite.rect.width : 2f;
            r.transform.localScale = new Vector3(w / px, h / px, 1f);
        }

        /// <summary>Corre la escena completa. onDone(true) si evolucionó.</summary>
        public IEnumerator Run(MemoInstance memo, MemoSpecies into, Action<bool> onDone = null)
        {
            IsRunning = true;
            GameRoot.InputLocks++;
            var from = memo.Species;
            string oldName = memo.DisplayName;
            var oldSprite = memo.RaceFrames is { Length: > 0 } a ? a[0] : null;
            var newSprite = into.raceFrames is { Length: > 0 } b ? b[0] : null;
            _sprite.sprite = oldSprite;
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _root.SetActive(true);

            yield return Say(new[] { $"¿Qué? ¡{oldName} está evolucionando!" });

            bool cancelled = false;
            float duration = 4.5f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (GameInput.CancelPressed)
                {
                    cancelled = true;
                    break;
                }
                // Parpadeo cada vez más rápido entre las dos formas (siluetas claras).
                float speed = Mathf.Lerp(2f, 14f, t / duration);
                bool showNew = Mathf.Sin(t * speed * Mathf.PI) > 0f;
                _sprite.sprite = showNew ? newSprite : oldSprite;
                _sprite.color = Color.Lerp(Color.white, new Color(0.75f, 0.85f, 1f), t / duration);
                yield return null;
            }

            if (cancelled)
            {
                _sprite.sprite = oldSprite;
                _sprite.color = Color.white;
                memo.cancelledEvolutionAtLevel = memo.level;
                yield return Say(new[] { $"¿Eh? {oldName} dejó de evolucionar." });
            }
            else
            {
                for (float t = 0f; t < 0.35f; t += Time.deltaTime)
                {
                    _flash.color = new Color(1f, 1f, 1f, t / 0.35f);
                    yield return null;
                }
                Progression.Evolve(memo, into, GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now);
                _sprite.sprite = newSprite;
                _sprite.color = Color.white;
                for (float t = 0f; t < 0.6f; t += Time.deltaTime)
                {
                    _flash.color = new Color(1f, 1f, 1f, 1f - t / 0.6f);
                    yield return null;
                }
                _flash.color = new Color(1f, 1f, 1f, 0f);
                var lines = new List<string> { $"¡Felicitaciones! ¡{oldName} evolucionó en {into.displayName}!" };
                lines.Add(from != null && memo.rebelFrom > TrustLevel.Neutral
                    ? $"{into.displayName} te mira distinto… Está en una etapa rebelde. Con cariño y paciencia, va a volver a confiar como antes."
                    : $"{into.displayName} anda un poco rebelde estos días. Ya se le va a pasar.");
                yield return Say(lines);
            }

            _root.SetActive(false);
            GameRoot.InputLocks--;
            IsRunning = false;
            onDone?.Invoke(!cancelled);
        }

        static IEnumerator Say(IEnumerable<string> pages)
        {
            bool done = false;
            GameRoot.Instance.Dialogue.Show(pages, () => done = true);
            while (!done) yield return null;
        }
    }
}
