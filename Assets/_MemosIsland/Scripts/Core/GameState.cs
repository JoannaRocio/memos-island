using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Memos;

namespace MemosIsland.Core
{
    /// <summary>
    /// Estado de la partida (lo que se va a guardar). Todos los Memos viven en el refugio;
    /// "team" son los (hasta) 6 que corren carreras y "companion" el que te sigue por la isla.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int MaxTeamSize = 6;

        public List<MemoInstance> team = new();
        public List<MemoInstance> refuge = new();
        public List<MemoRelationship> relationships = new();
        public string companionUid;
        public string lastDailyDate;
        public int bowlServings;

        public IEnumerable<MemoInstance> AllMemos => team.Concat(refuge);
        public MemoInstance Find(string uid) => AllMemos.FirstOrDefault(m => m.uid == uid);
        public MemoInstance Companion => string.IsNullOrEmpty(companionUid) ? null : Find(companionUid);
        public bool IsInTeam(MemoInstance m) => team.Contains(m);

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

        /// <summary>Mueve un Memo entre el equipo y el refugio. Devuelve false si el equipo está lleno o quedaría vacío.</summary>
        public bool ToggleTeam(MemoInstance m)
        {
            if (team.Contains(m))
            {
                if (team.Count <= 1) return false;
                team.Remove(m);
                refuge.Add(m);
                return true;
            }
            if (team.Count >= MaxTeamSize) return false;
            refuge.Remove(m);
            team.Add(m);
            return true;
        }

        /// <summary>Una vez por día real: +1 de confianza por vivir en el refugio (+3 si tiene padrino).</summary>
        public List<(MemoInstance memo, CareResult result)> RunDailyVisit(DateTime now)
        {
            var results = new List<(MemoInstance, CareResult)>();
            var today = now.Date.ToString("yyyy-MM-dd");
            if (lastDailyDate == today) return results;
            bool firstEver = lastDailyDate == null;
            lastDailyDate = today;
            if (firstEver) return results;
            foreach (var m in AllMemos)
            {
                bool hasGodparent = !string.IsNullOrEmpty(m.godparentUid) && Find(m.godparentUid) != null;
                results.Add((m, MemoCare.DailyVisit(m, hasGodparent)));
            }
            return results;
        }

        /// <summary>Equipo de prueba hasta que exista la elección del inicial (Fase 8).</summary>
        public void GiveDebugTeam()
        {
            team.Clear();
            refuge.Clear();
            relationships.Clear();
            var tostin = MemoInstance.Create("tostin", 6, "impulsivo", 520);
            var brotito = MemoInstance.Create("brotito", 5, "timido", 470);
            team.Add(tostin);
            team.Add(brotito);
            team.Add(MemoInstance.Create("charquito", 5, "jugueton", 460));
            team.Add(MemoInstance.Create("plumin", 5, "mimoso", 300));
            team.Add(MemoInstance.Create("topin", 5, "dormilon", 300));
            team.Add(MemoInstance.Create("copito", 5, "constante", 300));

            // En el refugio: uno de cada nivel de confianza, para ver todos los comportamientos.
            var zumbi = MemoInstance.Create("zumbi", 4, "timido", 30);
            zumbi.rescued = true;
            zumbi.highestTrust = TrustLevel.Fear;
            zumbi.godparentUid = tostin.uid;
            refuge.Add(zumbi);
            refuge.Add(MemoInstance.Create("bostezo", 5, "dormilon", 160));
            refuge.Add(MemoInstance.Create("chispin", 6, "jugueton", 760));
            refuge.Add(MemoInstance.Create("pantuflo", 6, "mimoso", 1050));

            // Tostín y Brotito ya son mejores amigos (impulso de amistad en los relevos).
            Friendship.Add(relationships, tostin, brotito, Friendship.BestFriendsAt + 10f);
            companionUid = tostin.uid;
        }
    }
}
