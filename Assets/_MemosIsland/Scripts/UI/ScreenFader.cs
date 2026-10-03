using System.Collections;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>Fundido a negro de pantalla completa.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ScreenFader : MonoBehaviour
    {
        SpriteRenderer _renderer;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            SetAlpha(0f);
        }

        public IEnumerator Fade(float targetAlpha, float duration)
        {
            float start = _renderer.color.a;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(start, targetAlpha, t / duration));
                yield return null;
            }
            SetAlpha(targetAlpha);
        }

        void SetAlpha(float a)
        {
            _renderer.color = new Color(0f, 0f, 0f, a);
            _renderer.enabled = a > 0f;
        }
    }
}
