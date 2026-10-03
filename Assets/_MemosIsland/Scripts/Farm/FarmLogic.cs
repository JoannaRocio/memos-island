using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;

namespace MemosIsland.Farm
{
    /// <summary>Granja simple (GDD §15): arar, plantar, regar y cosechar. Crece por días reales regados.</summary>
    public static class FarmLogic
    {
        public static bool Till(FarmPlot plot)
        {
            if (plot.tilled) return false;
            plot.tilled = true;
            return true;
        }

        public static bool Plant(FarmPlot plot, ItemData seed, GameState state)
        {
            if (!plot.tilled || plot.HasCrop || seed == null || seed.kind != ItemKind.Seed) return false;
            if (!state.RemoveItem(seed.id)) return false;
            plot.seedId = seed.id;
            plot.growth = 0;
            return true;
        }

        public static bool Water(FarmPlot plot, DateTime now)
        {
            var today = IslandState.DateKey(now);
            if (!plot.tilled || plot.wateredOn == today) return false;
            plot.wateredOn = today;
            return true;
        }

        public static bool IsReady(FarmPlot plot, ItemData seed) => plot.HasCrop && seed != null && plot.growth >= seed.growDays;

        /// <summary>0 = brote, 1 = creciendo, 2 = listo.</summary>
        public static int Stage(FarmPlot plot, ItemData seed)
        {
            if (!plot.HasCrop || seed == null) return 0;
            if (plot.growth >= seed.growDays) return 2;
            return plot.growth * 2 >= seed.growDays ? 1 : 0;
        }

        /// <summary>Cosecha (la tierra queda arada para volver a plantar). Devuelve el id del cultivo.</summary>
        public static string Harvest(FarmPlot plot, ItemData seed, GameState state, int amount = 1)
        {
            if (!IsReady(plot, seed)) return null;
            state.AddItem(seed.growsInto, amount);
            plot.seedId = null;
            plot.growth = 0;
            return seed.growsInto;
        }

        /// <summary>Cuenta un día que pasó: si estuvo regado ese día (o hay Memos de agua), crece.</summary>
        public static void GrowOneDay(FarmPlot plot, string day, bool watered, bool extraGrowth)
        {
            if (!plot.HasCrop) return;
            if (plot.wateredOn == day || watered)
            {
                plot.growth++;
                if (extraGrowth) plot.growth++;
            }
        }
    }

    /// <summary>Comprar, vender y la caja de envíos.</summary>
    public static class Economy
    {
        public static bool Buy(GameState state, ItemData item, int count = 1)
        {
            int cost = item.price * count;
            if (item.price <= 0 || state.island.money < cost) return false;
            state.island.money -= cost;
            state.AddItem(item.id, count);
            return true;
        }

        public static bool Sell(GameState state, ItemData item, int count = 1)
        {
            if (item.price <= 0 || !state.RemoveItem(item.id, count)) return false;
            state.island.money += item.SellPrice * count;
            return true;
        }

        public static bool Ship(GameState state, ItemData item, int count)
        {
            if (item.price <= 0 || !state.RemoveItem(item.id, count)) return false;
            var stack = state.island.shippingBin.Find(s => s.id == item.id);
            if (stack == null) state.island.shippingBin.Add(new ItemStack { id = item.id, count = count });
            else stack.count += count;
            return true;
        }

        /// <summary>Cobra lo que había en la caja de envíos. Devuelve lo ganado.</summary>
        public static int SettleShipping(GameState state, MemoDatabase db)
        {
            int total = 0;
            foreach (var s in state.island.shippingBin)
            {
                var item = db.GetItem(s.id);
                if (item != null) total += item.SellPrice * s.count;
            }
            state.island.shippingBin.Clear();
            state.island.money += total;
            return total;
        }
    }

    /// <summary>Mesa de trabajo y fundición.</summary>
    public static class Crafting
    {
        public static bool CanCraft(GameState state, RecipeData recipe, int times = 1) =>
            recipe != null && recipe.inputs.All(i => state.CountOf(i.id) >= i.count * times);

        public static bool Craft(GameState state, RecipeData recipe, int times = 1)
        {
            if (!CanCraft(state, recipe, times)) return false;
            foreach (var i in recipe.inputs) state.RemoveItem(i.id, i.count * times);
            state.AddItem(recipe.output.id, recipe.output.count * times);
            return true;
        }

        /// <summary>La fundición tarda minutos reales; un Memo de fuego contento la hace 3 veces más rápida.</summary>
        public static bool StartSmelting(GameState state, RecipeData recipe, int times, DateTime now, bool fireHelper)
        {
            if (state.island.smelter != null || recipe.station != CraftStation.Smelter || !CanCraft(state, recipe, times)) return false;
            foreach (var i in recipe.inputs) state.RemoveItem(i.id, i.count * times);
            float minutes = recipe.minutes * times / (fireHelper ? 3f : 1f);
            state.island.smelter = new SmelterJob { recipeId = recipe.id, count = times, readyTicks = now.AddMinutes(minutes).Ticks };
            return true;
        }

        public static bool SmelterReady(GameState state, DateTime now) =>
            state.island.smelter != null && now.Ticks >= state.island.smelter.readyTicks;

        public static bool CollectSmelter(GameState state, RecipeData recipe, DateTime now)
        {
            if (!SmelterReady(state, now) || recipe == null) return false;
            state.AddItem(recipe.output.id, recipe.output.count * state.island.smelter.count);
            state.island.smelter = null;
            return true;
        }
    }
}
