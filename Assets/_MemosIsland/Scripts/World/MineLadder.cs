using MemosIsland.Core;
using MemosIsland.Farm;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Escalera de la cueva: baja al piso siguiente o sube (en el piso 1, sale al refugio).</summary>
    public class MineLadder : MonoBehaviour, IInteractable
    {
        [SerializeField] bool goingDown;
        [SerializeField] int floor;
        [SerializeField] string exitScene;
        [SerializeField] string exitSpawn;

        public void Setup(bool down, int currentFloor, string scene, string spawn)
        {
            goingDown = down;
            floor = currentFloor;
            exitScene = scene;
            exitSpawn = spawn;
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var island = root.State.island;
            if (goingDown)
            {
                root.Dialogue.ShowChoice($"¿Bajás al piso {floor + 1}?", new[] { "Sí", "No" }, i =>
                {
                    if (i != 0) return;
                    island.mineFloor = floor + 1;
                    island.deepestFloor = Mathf.Max(island.deepestFloor, island.mineFloor);
                    root.Maps.GoTo(MineFloor.SceneName, "arriba");
                }, 1);
                return;
            }
            string question = floor <= 1 ? "¿Salís de la cueva?" : $"¿Subís al piso {floor - 1}?";
            root.Dialogue.ShowChoice(question, new[] { "Sí", "No" }, i =>
            {
                if (i != 0) return;
                if (floor <= 1)
                {
                    island.mineFloor = 1;
                    root.Maps.GoTo(exitScene, exitSpawn);
                }
                else
                {
                    island.mineFloor = floor - 1;
                    root.Maps.GoTo(MineFloor.SceneName, "arriba");
                }
            }, 1);
        }
    }
}
