using MemosIsland.Core;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>
    /// Cierra un paso (salida de un mapa) hasta que la historia ponga una marca. Con A se lee por qué está cerrado.
    /// Sin marca (vacía) queda cerrado siempre: los bloqueos de la próxima actualización (Volcán, Playa Helada).
    /// En la partida de prueba se abre todo lo que tenga marca.
    /// </summary>
    public class StoryBlocker : MonoBehaviour, IInteractable
    {
        [SerializeField] string openFlag;
        [TextArea] [SerializeField] string[] lockedText = { "Por ahora no se puede pasar." };

        public void Setup(string flag, params string[] text)
        {
            openFlag = flag;
            lockedText = text;
        }

        void Start()
        {
            var story = GameRoot.Instance != null ? GameRoot.Instance.State.story : null;
            if (story == null || string.IsNullOrEmpty(openFlag)) return;
            if (!story.active || story.Has(openFlag)) Destroy(gameObject);
        }

        public void Interact(PlayerController player) => GameRoot.Instance.Dialogue.Show(lockedText);
    }
}
