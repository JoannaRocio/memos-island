using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>Reloj arriba a la derecha: día de la semana y hora real (en modo prueba se ve en naranja).</summary>
    public class ClockHud : MonoBehaviour
    {
        [SerializeField] SpriteRenderer box;
        [SerializeField] PixelText label;

        const float Ppu = PixelFont.PixelsPerUnit;
        const int Padding = 5, Height = 16;
        static readonly Color NormalColor = new Color32(0x33, 0x3c, 0x57, 0xff);
        static readonly Color DebugColor = new Color32(0xb1, 0x3e, 0x53, 0xff);

        string _shown;

        public void Setup(SpriteRenderer background, PixelText text)
        {
            box = background;
            label = text;
        }

        void Update()
        {
            var clock = GameClock.Instance;
            if (clock == null) return;
            // Se oculta detrás de las pantallas completas (Mis Memos, MemoBox, evolución).
            var root = GameRoot.Instance;
            bool covered = root != null && (root.MyMemos != null && root.MyMemos.IsOpen || root.MemoBox != null && root.MemoBox.IsOpen
                                            || root.Lists != null && root.Lists.IsOpen
                                            || root.Evolution != null && root.Evolution.IsRunning);
            box.enabled = !covered;
            label.gameObject.SetActive(!covered);
            var text = (Weather.IsRainy(clock.Now) ? "☂ " : "☀ ") + clock.TimeText;
            if (text == _shown) return;
            _shown = text;

            label.SetColor(clock.IsDebugTime ? DebugColor : NormalColor);
            label.SetText(text);
            // Anclado a la derecha: la caja crece hacia la izquierda.
            int width = label.WidthPixels + Padding * 2;
            box.size = new Vector2(width / Ppu, Height / Ppu);
            box.transform.localPosition = new Vector3(-width / 2f / Ppu, -Height / 2f / Ppu, 0f);
            label.transform.localPosition = new Vector3((-width + Padding) / Ppu, -3f / Ppu, 0f);
        }
    }
}
