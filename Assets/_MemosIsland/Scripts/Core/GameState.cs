using System;
using System.Collections.Generic;
using MemosIsland.Memos;

namespace MemosIsland.Core
{
    /// <summary>Estado de la partida (lo que se va a guardar). Por ahora: el equipo y los Memos del refugio.</summary>
    [Serializable]
    public class GameState
    {
        public const int MaxTeamSize = 6;

        public List<MemoInstance> team = new();
        public List<MemoInstance> refuge = new();

        /// <summary>Suma un Memo: al equipo si hay lugar, si no, al refugio. Devuelve true si fue al equipo.</summary>
        public bool AddMemo(MemoInstance memo)
        {
            if (team.Count < MaxTeamSize)
            {
                team.Add(memo);
                return true;
            }
            refuge.Add(memo);
            return false;
        }

        /// <summary>Equipo de prueba hasta que exista la elección del inicial (Fase 8).</summary>
        public void GiveDebugTeam()
        {
            team.Clear();
            team.Add(MemoInstance.Create("tostin", 6, "impulsivo", 500));
            team.Add(MemoInstance.Create("brotito", 5, "timido", 450));
            team.Add(MemoInstance.Create("charquito", 5, "jugueton", 450));
            team.Add(MemoInstance.Create("plumin", 5, "mimoso", 300));
            team.Add(MemoInstance.Create("topin", 5, "dormilon", 300));
            team.Add(MemoInstance.Create("copito", 5, "constante", 300));
        }
    }
}
