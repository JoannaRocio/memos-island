using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;

namespace MemosIsland.UI
{
    /// <summary>Mochila, tienda de Deny, herrería de Fer, mesa de trabajo y caja de envíos (GDD §15).</summary>
    public static class IslandMenus
    {
        static GameState State => GameRoot.Instance.State;
        static MemoDatabase Db => MemoDatabase.Instance;
        static DateTime Now => GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;
        static string Money() => $"${State.island.money}";

        static List<ItemData> Owned(Func<ItemData, bool> filter) =>
            State.inventory.Select(s => Db.GetItem(s.id)).Where(i => i != null && filter(i)).ToList();

        static ListScreen.Row RowFor(ItemData item, string right, bool enabled = true) =>
            new() { icon = item.icon, label = item.displayName, right = right, enabled = enabled };

        // ------------------------------------------------------------------ Mochila

        static readonly string[] BagTabs = { "Todo", "Comida", "Materiales", "Equipo" };

        static bool InBagTab(ItemData i, int tab) => tab switch
        {
            1 => i.kind == ItemKind.Food,
            2 => i.kind is ItemKind.Material or ItemKind.Seed or ItemKind.Toy,
            3 => i.kind is ItemKind.Equipment or ItemKind.Amulet or ItemKind.Accessory,
            _ => true,
        };

        public static void OpenBag()
        {
            List<ItemData> items = null;
            GameRoot.Instance.Lists.Open(new ListScreen.Content
            {
                title = "Mochila",
                tabs = BagTabs,
                header = Money,
                rows = tab =>
                {
                    items = Owned(i => InBagTab(i, tab));
                    return items.Select(i => RowFor(i, $"×{State.CountOf(i.id)}")).ToList();
                },
                detail = (tab, row) =>
                {
                    var i = items[row];
                    var tools = $"\n\n{ShopCatalog.ToolName(ToolKind.Hoe, State.island.hoeLevel)}, " +
                                $"{ShopCatalog.ToolName(ToolKind.Can, State.island.canLevel)}, " +
                                $"{ShopCatalog.ToolName(ToolKind.Pick, State.island.pickLevel)}.";
                    return $"{i.description}\n\nSe vende a ${i.SellPrice}." + (row == 0 ? tools : "");
                },
                hint = "←→ pestaña  B: salir",
            });
        }

        // ------------------------------------------------------------------ Tienda de Deny

        public static void OpenDenyShop()
        {
            List<ItemData> stock = null, sellable = null;
            GameRoot.Instance.Lists.Open(new ListScreen.Content
            {
                title = "Almacén de Deny",
                tabs = new[] { "Comprar", "Vender" },
                header = Money,
                rows = tab =>
                {
                    if (tab == 0)
                    {
                        stock = ShopCatalog.Deny(Now).Select(Db.GetItem).Where(i => i != null).ToList();
                        return stock.Select(i => RowFor(i, $"${i.price}", State.island.money >= i.price)).ToList();
                    }
                    sellable = Owned(i => i.price > 0);
                    return sellable.Select(i => RowFor(i, $"${i.SellPrice} ×{State.CountOf(i.id)}")).ToList();
                },
                detail = (tab, row) =>
                {
                    var i = tab == 0 ? stock[row] : sellable[row];
                    return tab == 0
                        ? $"{i.description}\n\nTenés {State.CountOf(i.id)}." +
                          (i.kind == ItemKind.Seed ? "\n\nLas semillas cambian según el día de la semana." : "")
                        : $"{i.description}\n\nDeny te da ${i.SellPrice} por cada uno.";
                },
                confirm = (tab, row) =>
                {
                    if (tab == 0)
                    {
                        var i = stock[row];
                        return Economy.Buy(State, i) ? null : "No te alcanza la plata.";
                    }
                    var s = sellable[row];
                    Economy.Sell(State, s);
                    return null;
                },
                hint = "A: comprar/vender uno  B: salir",
            });
        }

        // ------------------------------------------------------------------ Herrería de Fer

        static readonly ToolKind[] Tools = { ToolKind.Pick, ToolKind.Can, ToolKind.Hoe };

        public static void OpenFerForge()
        {
            List<ItemData> stock = null;
            GameRoot.Instance.Lists.Open(new ListScreen.Content
            {
                title = "Herrería de Fer",
                tabs = new[] { "Herramientas", "Equipo" },
                header = Money,
                rows = tab =>
                {
                    if (tab == 0)
                    {
                        return Tools.Select(t =>
                        {
                            int level = ShopCatalog.LevelOf(State.island, t);
                            if (level >= 2) return new ListScreen.Row { label = ShopCatalog.ToolName(t, level), right = "Listo", enabled = false };
                            var (money, bar, bars) = ShopCatalog.UpgradeCost(level + 1);
                            bool can = State.island.money >= money && State.CountOf(bar) >= bars;
                            return new ListScreen.Row { label = ShopCatalog.ToolName(t, level + 1), right = $"${money}", enabled = can };
                        }).ToList();
                    }
                    stock = ShopCatalog.Fer.Select(Db.GetItem).Where(i => i != null).ToList();
                    return stock.Select(i => RowFor(i, $"${i.price}", State.island.money >= i.price)).ToList();
                },
                detail = (tab, row) =>
                {
                    if (tab == 1) return stock[row].description;
                    var t = Tools[row];
                    int level = ShopCatalog.LevelOf(State.island, t);
                    if (level >= 2) return $"Tu {ShopCatalog.ToolName(t, level).ToLowerInvariant()} ya es la mejor.";
                    var (money, bar, bars) = ShopCatalog.UpgradeCost(level + 1);
                    return $"{ShopCatalog.ToolEffect(t, level + 1)}\n\nCuesta ${money} y {bars} {Db.GetItem(bar)?.displayName.ToLowerInvariant()}(s). " +
                           $"Tenés {State.CountOf(bar)}.";
                },
                confirm = (tab, row) =>
                {
                    if (tab == 1) return Economy.Buy(State, stock[row]) ? null : "No te alcanza la plata.";
                    var t = Tools[row];
                    return ShopCatalog.Upgrade(State, t)
                        ? $"Fer: \"Listo. Tu {ShopCatalog.ToolName(t, ShopCatalog.LevelOf(State.island, t)).ToLowerInvariant()} quedó como nueva.\""
                        : "Fer: \"Te faltan materiales o plata.\"";
                },
                hint = "A: comprar/mejorar  B: salir",
            });
        }

        // ------------------------------------------------------------------ Mesa de trabajo

        public static void OpenWorkbench()
        {
            var recipes = Db.recipes.Where(r => r != null && r.station == CraftStation.Workbench).ToList();
            GameRoot.Instance.Lists.Open(new ListScreen.Content
            {
                title = "Mesa de trabajo",
                header = Money,
                rows = _ => recipes.Select(r =>
                {
                    var output = Db.GetItem(r.output.id);
                    return new ListScreen.Row
                    {
                        icon = output?.icon,
                        label = output?.displayName ?? r.output.id,
                        right = r.output.count > 1 ? $"×{r.output.count}" : "",
                        enabled = Crafting.CanCraft(State, r),
                    };
                }).ToList(),
                detail = (_, row) =>
                {
                    var r = recipes[row];
                    var lines = r.inputs.Select(i => $"{Db.GetItem(i.id)?.displayName ?? i.id}: {State.CountOf(i.id)}/{i.count}");
                    return $"Necesitás:\n{string.Join("\n", lines)}\n\n{Db.GetItem(r.output.id)?.description}";
                },
                confirm = (_, row) =>
                {
                    var r = recipes[row];
                    if (!Crafting.Craft(State, r)) return "Te faltan materiales.";
                    return $"¡Hiciste {Db.GetItem(r.output.id)?.displayName}!";
                },
                hint = "A: fabricar  B: salir",
            });
        }

        // ------------------------------------------------------------------ Caja de envíos

        public static void OpenShipping()
        {
            List<ItemData> sellable = null;
            GameRoot.Instance.Lists.Open(new ListScreen.Content
            {
                title = "Caja de envíos",
                header = () => $"Mañana: ${State.island.shippingBin.Sum(s => (Db.GetItem(s.id)?.SellPrice ?? 0) * s.count)}",
                rows = _ =>
                {
                    sellable = Owned(i => i.price > 0 && i.kind is not ItemKind.Equipment and not ItemKind.Amulet and not ItemKind.Accessory);
                    return sellable.Select(i => RowFor(i, $"${i.SellPrice} ×{State.CountOf(i.id)}")).ToList();
                },
                detail = (_, row) => $"{sellable[row].description}\n\nLo que dejes en la caja se cobra al día siguiente.",
                confirm = (_, row) =>
                {
                    Economy.Ship(State, sellable[row], 1);
                    return null;
                },
                hint = "A: poner uno  B: salir",
            });
        }
    }
}
