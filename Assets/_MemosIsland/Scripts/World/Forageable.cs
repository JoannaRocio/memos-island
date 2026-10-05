using MemosIsland.Core;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Algo para juntar del suelo (lo crea ForageSpawner).</summary>
    public class Forageable : MonoBehaviour, IInteractable
    {
        [SerializeField] string itemId;
        [SerializeField] string key;

        public void Setup(string id, string spotKey)
        {
            itemId = id;
            key = spotKey;
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            root.State.AddItem(itemId);
            AudioManager.Sfx("get", 0.7f);
            root.State.island.foraged.Add(key);
            root.Dialogue.Show(new[] { $"Juntaste: {MemoDatabase.Instance.GetItem(itemId)?.displayName}." });
            Destroy(gameObject);
        }
    }
}
