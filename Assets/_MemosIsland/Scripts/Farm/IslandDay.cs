using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;

namespace MemosIsland.Farm
{
    public enum WorkRole { None, Water, Grow, Till, Smelt, Process, Dig }

    public class DayReport
    {
        public int daysPassed;
        public int shippingIncome;
        public readonly List<string> messages = new();
    }

    /// <summary>
    /// Lo que pasa cada día real (GDD §15–16): cobra la caja de envíos, crecen los cultivos regados
    /// y los Memos del refugio trabajan según su tipo (si están contentos y te tienen al menos confianza neutral).
    /// </summary>
    public static class IslandDay
    {
        public const int MaxCatchUpDays = 14;
        public const int WorkXpPerDay = 10;

        public static WorkRole RoleOf(MemoInstance m)
        {
            var s = m.Species;
            if (s == null) return WorkRole.None;
            bool Is(string type) => s.primaryType != null && s.primaryType.id == type || s.secondaryType != null && s.secondaryType.id == type;
            if (Is("agua")) return WorkRole.Water;
            if (Is("planta")) return WorkRole.Grow;
            if (Is("fuego")) return WorkRole.Smelt;
            if (Is("electrico")) return WorkRole.Process;
            if (s.mobility == Mobility.Digs) return WorkRole.Dig;
            if (Is("tierra")) return WorkRole.Till;
            return WorkRole.None;
        }

        public static string RoleName(WorkRole role) => role switch
        {
            WorkRole.Water => "riega la huerta",
            WorkRole.Grow => "hace crecer los cultivos",
            WorkRole.Till => "ara la tierra",
            WorkRole.Smelt => "aviva la fundición",
            WorkRole.Process => "hace andar la procesadora",
            WorkRole.Dig => "cava buscando cosas",
            _ => "no tiene un trabajo",
        };

        /// <summary>Memos que trabajan hoy: viven en el refugio (no el compañero), contentos y con confianza.</summary>
        public static List<MemoInstance> Workers(GameState state, DateTime now) =>
            state.AllMemos.Where(m => m.uid != state.companionUid && m.TrustLevel >= TrustLevel.Neutral
                                      && m.mood >= 50 && m.awayUntilTicks <= now.Ticks && RoleOf(m) != WorkRole.None).ToList();

        public static bool HasHelper(GameState state, DateTime now, WorkRole role) =>
            Workers(state, now).Any(m => RoleOf(m) == role);

        public static DayReport Process(GameState state, MemoDatabase db, DateTime now)
        {
            var report = new DayReport();
            var island = state.island;
            var today = IslandState.DateKey(now);
            if (island.lastProcessedDate == null)
            {
                island.lastProcessedDate = today;
                return report;
            }
            if (island.lastProcessedDate == today) return report;

            var last = DateTime.Parse(island.lastProcessedDate);
            int days = Math.Min(MaxCatchUpDays, (now.Date - last.Date).Days);
            island.lastProcessedDate = today;
            if (days <= 0) return report;
            report.daysPassed = days;

            report.shippingIncome = Economy.SettleShipping(state, db);
            if (report.shippingIncome > 0)
                report.messages.Add($"Se vendió lo de la caja de envíos: ¡ganaste ${report.shippingIncome}!");

            var workers = Workers(state, now);
            var byRole = workers.GroupBy(RoleOf).ToDictionary(g => g.Key, g => g.ToList());
            bool Has(WorkRole r) => byRole.ContainsKey(r);
            var rng = new System.Random(Mining.Seed(today, 0));
            var found = new Dictionary<string, int>();

            for (int d = 0; d < days; d++)
            {
                var day = IslandState.DateKey(last.AddDays(d));
                foreach (var plot in island.plots)
                    FarmLogic.GrowOneDay(plot, day, Has(WorkRole.Water) || Weather.IsRainy(last.AddDays(d)), Has(WorkRole.Grow) && rng.Next(10) < 3);

                if (Has(WorkRole.Till))
                    foreach (var plot in island.plots.Where(p => !p.tilled).Take(2)) plot.tilled = true;

                if (Has(WorkRole.Dig))
                {
                    string[] finds = { "piedra", "mineral_cobre", "cuarzo", "fruto_silvestre" };
                    foreach (var _ in byRole[WorkRole.Dig])
                    {
                        var id = finds[rng.Next(finds.Length)];
                        state.AddItem(id);
                        found[id] = found.TryGetValue(id, out var n) ? n + 1 : 1;
                    }
                }

                if (island.processor != null && string.CompareOrdinal(island.processor.readyDate, IslandState.DateKey(last.AddDays(d + 1))) <= 0
                    && Has(WorkRole.Process))
                {
                    state.AddItem("mermelada", island.processor.count);
                    report.messages.Add($"La procesadora terminó: {island.processor.count} mermelada(s).");
                    island.processor = null;
                }

                foreach (var w in workers) Progression.AddXp(w, WorkXpPerDay);
            }

            foreach (var (role, list) in byRole)
            {
                var names = string.Join(" y ", list.Select(m => m.DisplayName));
                report.messages.Add(days == 1
                    ? $"Mientras no estabas, {names} {RoleName(role)}."
                    : $"Estos {days} días, {names} {RoleName(role)}.");
            }
            if (found.Count > 0)
                report.messages.Add("Cavando encontraron: " + string.Join(", ",
                    found.Select(f => $"{db.GetItem(f.Key)?.displayName ?? f.Key} ×{f.Value}")) + ".");
            return report;
        }
    }
}
