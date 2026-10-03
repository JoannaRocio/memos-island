using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Cartel, puerta cerrada u objeto que muestra texto.</summary>
    public class Sign : MonoBehaviour, IInteractable
    {
        [SerializeField, TextArea(2, 4)] string[] pages = { "..." };

        public void SetPages(params string[] newPages) => pages = newPages;

        public void Interact(PlayerController player) => GameRoot.Instance.Dialogue.Show(pages);
    }
}
