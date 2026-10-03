using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Comedero del refugio: lo llenás y los Memos comen solos cuando tienen hambre.</summary>
    public class Bowl : MonoBehaviour, IInteractable
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite fullSprite;
        [SerializeField] Sprite emptySprite;
        [SerializeField] int servingsPerFill = 6;

        public void Setup(SpriteRenderer r, Sprite full, Sprite empty)
        {
            spriteRenderer = r;
            fullSprite = full;
            emptySprite = empty;
        }

        public void Refresh()
        {
            var state = GameRoot.Instance != null ? GameRoot.Instance.State : null;
            spriteRenderer.sprite = state != null && state.bowlServings > 0 ? fullSprite : emptySprite;
        }

        public void Interact(PlayerController player)
        {
            var state = GameRoot.Instance.State;
            if (state.bowlServings >= servingsPerFill)
            {
                GameRoot.Instance.Dialogue.Show(new[] { "El comedero está lleno." });
                return;
            }
            state.bowlServings = servingsPerFill;
            Refresh();
            GameRoot.Instance.Dialogue.Show(new[]
            {
                "Llenaste el comedero. Los Memos van a comer cuando tengan hambre…",
                "…y los que tienen miedo, cuando no los estés mirando.",
            });
        }
    }
}
