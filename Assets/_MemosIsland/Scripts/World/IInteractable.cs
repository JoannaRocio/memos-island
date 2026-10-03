using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Algo con lo que el jugador puede interactuar apretando A de frente.</summary>
    public interface IInteractable
    {
        void Interact(PlayerController player);
    }

    /// <summary>Algo que vive en casillas sin colisión (ej. las parcelas de la huerta).</summary>
    public interface ICellInteractable
    {
        bool Handles(Vector2Int cell);
        void InteractCell(PlayerController player, Vector2Int cell);
    }

    public static class CellInteractables
    {
        static readonly System.Collections.Generic.List<ICellInteractable> All = new();
        public static void Register(ICellInteractable c) { if (!All.Contains(c)) All.Add(c); }
        public static void Unregister(ICellInteractable c) => All.Remove(c);
        public static ICellInteractable At(Vector2Int cell) => All.Find(c => c.Handles(cell));
    }
}
