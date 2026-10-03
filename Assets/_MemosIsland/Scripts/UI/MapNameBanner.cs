using System.Collections;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>Cartel con el nombre del lugar que baja desde arriba al entrar a un mapa (como en Pokémon).</summary>
    public class MapNameBanner : MonoBehaviour
    {
        [SerializeField] Transform content;
        [SerializeField] SpriteRenderer box;
        [SerializeField] PixelText label;
        [SerializeField] float slideDuration = 0.25f;
        [SerializeField] float holdDuration = 2f;

        const float Ppu = PixelFont.PixelsPerUnit;
        const int Padding = 7, Height = 18;

        Coroutine _running;
        Vector3 _shownPosition;

        public void Setup(Transform contentRoot, SpriteRenderer background, PixelText text)
        {
            content = contentRoot;
            box = background;
            label = text;
        }

        void Awake()
        {
            _shownPosition = content.localPosition;
            content.gameObject.SetActive(false);
        }

        public void Show(string mapName)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Run(mapName));
        }

        IEnumerator Run(string mapName)
        {
            label.SetText(mapName);
            int width = label.WidthPixels + Padding * 2;
            box.size = new Vector2(width / Ppu, Height / Ppu);
            box.transform.localPosition = new Vector3(width / 2f / Ppu, -Height / 2f / Ppu, 0f);

            var hidden = _shownPosition + new Vector3(0f, (Height + 8) / Ppu, 0f);
            content.gameObject.SetActive(true);
            yield return Slide(hidden, _shownPosition);
            yield return new WaitForSeconds(holdDuration);
            yield return Slide(_shownPosition, hidden);
            content.gameObject.SetActive(false);
            _running = null;
        }

        IEnumerator Slide(Vector3 from, Vector3 to)
        {
            for (float t = 0f; t < slideDuration; t += Time.deltaTime)
            {
                var p = Vector3.Lerp(from, to, t / slideDuration);
                p.y = Mathf.Round(p.y * Ppu) / Ppu;
                content.localPosition = p;
                yield return null;
            }
            content.localPosition = to;
        }
    }
}
