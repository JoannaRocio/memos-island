using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Algo con lo que el jugador puede interactuar apretando A de frente.</summary>
    public interface IInteractable
    {
        void Interact(PlayerController player);
    }
}
