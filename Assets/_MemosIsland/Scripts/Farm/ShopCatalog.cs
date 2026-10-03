using System;
using System.Collections.Generic;
using MemosIsland.Core;

namespace MemosIsland.Farm
{
    public enum ToolKind { Hoe, Can, Pick }

    /// <summary>Qué vende cada puesto (GDD §15). Las semillas de Deny cambian según el día de la semana.</summary>
    public static class ShopCatalog
    {
        static readonly Dictionary<DayOfWeek, string[]> SeedsByDay = new()
        {
            [DayOfWeek.Monday] = new[] { "semilla_nabo", "semilla_zanahoria" },
            [DayOfWeek.Tuesday] = new[] { "semilla_nabo", "semilla_frutilla" },
            [DayOfWeek.Wednesday] = new[] { "semilla_zanahoria", "semilla_zapallo" },
            [DayOfWeek.Thursday] = new[] { "semilla_nabo", "semilla_bayamemo" },
            [DayOfWeek.Friday] = new[] { "semilla_frutilla", "semilla_zapallo" },
            [DayOfWeek.Saturday] = new[] { "semilla_nabo", "semilla_zanahoria", "semilla_frutilla", "semilla_zapallo", "semilla_bayamemo" },
            [DayOfWeek.Sunday] = new[] { "semilla_zanahoria", "semilla_bayamemo" },
        };

        static readonly string[] Accessories =
        {
            "acc_gorrito_rojo", "acc_gorrito_azul", "acc_mono_rosa", "acc_sombrero_paja", "acc_estrellita",
            "acc_bufanda_roja", "acc_bufanda_verde", "acc_panuelo_azul", "acc_cascabel",
        };

        public static List<string> Deny(DateTime now)
        {
            var list = new List<string>(SeedsByDay[now.DayOfWeek]);
            list.AddRange(new[] { "comida_memo", "tela", "cuero", "pelota" });
            // Tres accesorios que rotan cada día.
            int start = now.DayOfYear % Accessories.Length;
            for (int i = 0; i < 3; i++) list.Add(Accessories[(start + i * 3) % Accessories.Length]);
            return list;
        }

        public static readonly string[] Fer = { "herraduras", "aletas", "botas_clavos", "pesas", "mochila_agua", "lingote_cobre" };

        // ------------------------------------------------------------------ Mejoras de herramientas (herrería de Fer)

        public static string ToolName(ToolKind tool, int level)
        {
            string material = level switch { 1 => " de cobre", 2 => " de hierro", _ => "" };
            return tool switch
            {
                ToolKind.Hoe => "Azada" + material,
                ToolKind.Can => "Regadera" + material,
                _ => "Pico" + material,
            };
        }

        public static string ToolEffect(ToolKind tool, int level) => tool switch
        {
            ToolKind.Pick => level switch
            {
                0 => "Rompe piedra y cobre.",
                1 => "También rompe hierro y cuarzo.",
                _ => "Rompe todo, hasta las rocas con gemas.",
            },
            _ => $"{(tool == ToolKind.Hoe ? "Ara" : "Riega")} {AreaFor(level)} parcela(s) en línea de un golpe.",
        };

        /// <summary>Cuántas parcelas en línea ara o riega la herramienta según su nivel.</summary>
        public static int AreaFor(int level) => level switch { 0 => 1, 1 => 3, _ => 5 };

        public static (int money, string bar, int bars) UpgradeCost(int nextLevel) => nextLevel switch
        {
            1 => (500, "lingote_cobre", 3),
            _ => (1500, "lingote_hierro", 3),
        };

        public static int LevelOf(IslandState island, ToolKind tool) => tool switch
        {
            ToolKind.Hoe => island.hoeLevel,
            ToolKind.Can => island.canLevel,
            _ => island.pickLevel,
        };

        public static bool Upgrade(GameState state, ToolKind tool)
        {
            int next = LevelOf(state.island, tool) + 1;
            if (next > 2) return false;
            var (money, bar, bars) = UpgradeCost(next);
            if (state.island.money < money || state.CountOf(bar) < bars) return false;
            state.island.money -= money;
            state.RemoveItem(bar, bars);
            switch (tool)
            {
                case ToolKind.Hoe: state.island.hoeLevel = next; break;
                case ToolKind.Can: state.island.canLevel = next; break;
                default: state.island.pickLevel = next; break;
            }
            return true;
        }
    }
}
