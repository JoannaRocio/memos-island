using System;
using System.Collections.Generic;

namespace MemosIsland.Town
{
    /// <summary>Amistad con un vecino (0 a 1000 puntos = 0 a 10 corazones).</summary>
    [Serializable]
    public class NeighborState
    {
        public string id;
        public int points;
        public bool met;
        public string talkedOn = "";
        public string giftedOn = "";
        public List<string> seenEvents = new();
    }

    /// <summary>Un pedido del tablón de hoy.</summary>
    [Serializable]
    public class DailyRequest
    {
        public string neighborId;
        public int template;
        public bool accepted;
        public bool done;
    }

    /// <summary>Todo lo del pueblo que se guarda: amistades y el tablón del día.</summary>
    [Serializable]
    public class TownState
    {
        public List<NeighborState> neighbors = new();
        public string boardDate = "";
        public List<DailyRequest> board = new();

        public NeighborState Get(string id)
        {
            var s = neighbors.Find(n => n.id == id);
            if (s == null)
            {
                s = new NeighborState { id = id };
                neighbors.Add(s);
            }
            return s;
        }
    }
}
