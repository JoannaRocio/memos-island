using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Roca de la cueva: con A se pica (si el pico alcanza) y da materiales.</summary>
    public class Rock : MonoBehaviour, IInteractable
    {
        [SerializeField] RockKind kind;
        [SerializeField] int floor;
        [SerializeField] Vector2Int cell;

        public void Setup(RockKind rockKind, int mineFloor, Vector2Int rockCell)
        {
            kind = rockKind;
            floor = mineFloor;
            cell = rockCell;
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var state = root.State;
            int need = Mining.RequiredPick(kind);
            if (state.island.pickLevel < need)
            {
                root.Dialogue.Show(new[]
                {
                    $"Es {Mining.KindName(kind)}. Tu pico no alcanza: necesitás {ShopCatalog.ToolName(ToolKind.Pick, need).ToLowerInvariant()}. Fer lo mejora.",
                });
                return;
            }

            // Un Memo excavador en el equipo o de compañero encuentra un poco más (GDD §15).
            bool digger = state.team.Append(state.Companion).Any(m => m?.Species != null && m.Species.mobility == Mobility.Digs);
            var drops = Mining.Drops(kind, digger, new System.Random(Mining.Seed(state.island.mineDate, cell.x * 100 + cell.y + floor)));
            foreach (var (id, count) in drops) state.AddItem(id, count);
            state.island.brokenRocks.Add(Mining.RockKey(floor, cell));
            var text = string.Join(", ", drops.Select(d => $"{MemoDatabase.Instance.GetItem(d.id)?.displayName} ×{d.count}"));
            root.Dialogue.Show(new[] { (digger ? "¡Tu Memo excavador ayudó! " : "") + $"Conseguiste: {text}." });
            Destroy(gameObject);
        }
    }
}
