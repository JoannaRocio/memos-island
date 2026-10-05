using MemosIsland.Core;
using MemosIsland.UI;
using UnityEngine;

namespace MemosIsland.World
{
    public enum Emote { Love, Music, Surprise, Question, Sleep, Sad, Scared, Angry, Dots }

    /// <summary>Burbuja de emoción estilo GBA sobre la cabeza de un Memo (♥ ♪ ! ? zz … !! ×).</summary>
    public class EmoteBubble : MonoBehaviour
    {
        [SerializeField] SpriteRenderer bubble;
        [SerializeField] PixelText glyph;

        float _time;

        static readonly Color Red = new Color32(0xb1, 0x3e, 0x53, 0xff);
        static readonly Color Blue = new Color32(0x3b, 0x5d, 0xc9, 0xff);
        static readonly Color Dark = new Color32(0x33, 0x3c, 0x57, 0xff);
        static readonly Color Gray = new Color32(0x56, 0x6c, 0x86, 0xff);
        static readonly Color Orange = new Color32(0xef, 0x7d, 0x57, 0xff);

        public void Setup(SpriteRenderer bubbleRenderer, PixelText text)
        {
            bubble = bubbleRenderer;
            glyph = text;
        }

        void Awake() => Hide();

        public void Show(Emote emote, float seconds = 1.8f)
        {
            AudioManager.Sfx("bubble", 0.4f, emote == Emote.Sad || emote == Emote.Scared ? 0.8f : 1f);
            var (text, color) = emote switch
            {
                Emote.Love => ("♥", Red),
                Emote.Music => ("♪", Blue),
                Emote.Surprise => ("!", Red),
                Emote.Question => ("?", Dark),
                Emote.Sleep => ("zz", Blue),
                Emote.Sad => ("…", Gray),
                Emote.Scared => ("!!", Orange),
                Emote.Angry => ("×", Red),
                _ => ("…", Dark),
            };
            glyph.SetColor(color);
            glyph.SetText(text);
            // Centrado dentro de la burbuja (14 px de ancho).
            int w = glyph.Font != null ? glyph.Font.MeasureWidth(text) : 4;
            glyph.transform.localPosition = new Vector3(Mathf.Round(-w / 2f) / 16f, 10f / 16f, 0f);
            bubble.enabled = true;
            glyph.gameObject.SetActive(true);
            _time = seconds;
        }

        public void Hide()
        {
            if (bubble != null) bubble.enabled = false;
            if (glyph != null) glyph.gameObject.SetActive(false);
            _time = 0f;
        }

        void Update()
        {
            if (_time <= 0f) return;
            _time -= Time.deltaTime;
            if (_time <= 0f) Hide();
        }
    }
}
