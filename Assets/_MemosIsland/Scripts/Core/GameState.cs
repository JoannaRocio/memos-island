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
    public class ItemStack
    {
        public string id;
        public int count;
    }

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
        public List<ItemStack> inventory = new();
        public Farm.IslandState island = new();

        // ------------------------------------------------------------------ Inventario

        public int CountOf(string itemId) => inventory.Find(i => i.id == itemId)?.count ?? 0;

        public void AddItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;
            var stack = inventory.Find(i => i.id == itemId);
            if (stack == null) inventory.Add(new ItemStack { id = itemId, count = count });
            else stack.count += count;
        }

        public bool RemoveItem(string itemId, int count = 1)
        {
            var stack = inventory.Find(i => i.id == itemId);
            if (stack == null || stack.count < count) return false;
            stack.count -= count;
            if (stack.count == 0) inventory.Remove(stack);
            return true;
        }

        /// <summary>
        /// Equipa un objeto del inventario en un espacio del Memo (equipo, amuleto o accesorio).
        /// El que tenía puesto vuelve al inventario. itemId null = quitar.
        /// </summary>
        public bool Equip(MemoInstance memo, ItemKind slot, string itemId)
        {
            if (itemId != null && !RemoveItem(itemId)) return false;
            string previous = slot switch
            {
                ItemKind.Equipment => memo.equipmentId,
                ItemKind.Amulet => memo.amuletId,
                _ => memo.accessoryId,
            };
            if (!string.IsNullOrEmpty(previous)) AddItem(previous);
            switch (slot)
            {
                case ItemKind.Equipment: memo.equipmentId = itemId; break;
                case ItemKind.Amulet: memo.amuletId = itemId; break;
                default: memo.accessoryId = itemId; break;
            }
            return true;
        }

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
            // Charquito está por llegar al nivel 16: una carrera más y evoluciona (para probar la Fase 5).
            var charquito = MemoInstance.Create("charquito", 15, "jugueton", 460);
            charquito.xp = Progression.XpToNext(15) - 10;
            team.Add(charquito);
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
            // Legendario hostil: gruñe, no se deja tocar y de día se va del refugio (GDD §11).
            refuge.Add(MemoInstance.Create("karman", 12, "orgulloso", -40));

            inventory.Clear();
            foreach (var id in new[]
                     {
                         "herraduras", "aletas", "botas_clavos", "alas_planeo", "pesas", "mochila_agua",
                         "amuleto_relevo", "piedra_carga", "cascabel_calma", "mono_amistad", "gema_rio", "gema_hielo",
                         "acc_gorrito_rojo", "acc_gorrito_azul", "acc_mono_rosa", "acc_corona_flores", "acc_sombrero_paja",
                         "acc_estrellita", "acc_bufanda_roja", "acc_bufanda_verde", "acc_panuelo_azul", "acc_cascabel",
                     })
                AddItem(id);
            // Comida y semillas para empezar (Fase 6).
            foreach (var (id, n) in new[]
                     {
                         ("comida_memo", 10), ("bayamemo", 4), ("frutilla", 3), ("zanahoria", 3),
                         ("semilla_nabo", 6), ("semilla_frutilla", 3), ("semilla_bayamemo", 2), ("tela", 2), ("pluma", 1),
                     })
                AddItem(id, n);
            island = new Farm.IslandState();
            tostin.accessoryId = "acc_panuelo_azul"; // (una bufanda roja casi no se vería sobre un Memo de fuego)

            // Tostín y Brotito ya son mejores amigos (impulso de amistad en los relevos).
            Friendship.Add(relationships, tostin, brotito, Friendship.BestFriendsAt + 10f);
            companionUid = tostin.uid;
        }
    }
}
