using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using NUnit.Framework;
using UnityEngine;

namespace MemosIsland.Tests
{
    public class FarmTests
    {
        static readonly DateTime Day1 = new(2026, 10, 3, 10, 0, 0);
        ItemData _seed;

        [SetUp]
        public void SetUp()
        {
            _seed = ScriptableObject.CreateInstance<ItemData>();
            _seed.id = "semilla_test";
            _seed.kind = ItemKind.Seed;
            _seed.growsInto = "nabo";
            _seed.growDays = 3;
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_seed);

        [Test]
        public void PlantNeedsTilledSoilAndASeed()
        {
            var state = new GameState();
            var plot = new FarmPlot();
            Assert.IsFalse(FarmLogic.Plant(plot, _seed, state), "sin arar");
            FarmLogic.Till(plot);
            Assert.IsFalse(FarmLogic.Plant(plot, _seed, state), "sin semilla en el inventario");
            state.AddItem("semilla_test");
            Assert.IsTrue(FarmLogic.Plant(plot, _seed, state));
            Assert.AreEqual(0, state.CountOf("semilla_test"));
        }

        [Test]
        public void GrowsOnlyOnWateredDays_AndNeverDies()
        {
            var plot = new FarmPlot { tilled = true, seedId = "semilla_test" };
            FarmLogic.Water(plot, Day1);
            FarmLogic.GrowOneDay(plot, IslandState.DateKey(Day1), false, false);
            FarmLogic.GrowOneDay(plot, IslandState.DateKey(Day1.AddDays(1)), false, false); // no se regó
            Assert.AreEqual(1, plot.growth);
            Assert.IsTrue(plot.HasCrop, "no se muere");
            FarmLogic.GrowOneDay(plot, "x", true, false); // un Memo de agua la regó
            FarmLogic.GrowOneDay(plot, "y", true, true);  // y uno de planta la hizo crecer de más
            Assert.AreEqual(4, plot.growth);
            Assert.AreEqual(2, FarmLogic.Stage(plot, _seed));
        }

        [Test]
        public void WaterOncePerDay_HarvestLeavesTilledSoil()
        {
            var state = new GameState();
            var plot = new FarmPlot { tilled = true, seedId = "semilla_test", growth = 3 };
            Assert.IsTrue(FarmLogic.Water(plot, Day1));
            Assert.IsFalse(FarmLogic.Water(plot, Day1.AddHours(2)));
            Assert.AreEqual("nabo", FarmLogic.Harvest(plot, _seed, state));
            Assert.AreEqual(1, state.CountOf("nabo"));
            Assert.IsTrue(plot.tilled);
            Assert.IsFalse(plot.HasCrop);
        }
    }

    public class EconomyAndCraftingTests
    {
        [Test]
        public void BuyAndSell()
        {
            var state = new GameState();
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.id = "x";
            item.price = 100;
            state.island.money = 150;
            Assert.IsTrue(Economy.Buy(state, item));
            Assert.IsFalse(Economy.Buy(state, item), "no alcanza la plata");
            Assert.AreEqual(50, state.island.money);
            Assert.IsTrue(Economy.Sell(state, item));
            Assert.AreEqual(100, state.island.money, "se vende a la mitad");
        }

        [Test]
        public void Crafting_ConsumesInputs()
        {
            var state = new GameState();
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.inputs = new List<ItemAmount> { new("nabo", 1), new("zanahoria", 1) };
            recipe.output = new ItemAmount("comida_memo", 3);
            state.AddItem("nabo", 2);
            Assert.IsFalse(Crafting.CanCraft(state, recipe));
            state.AddItem("zanahoria", 2);
            Assert.IsTrue(Crafting.Craft(state, recipe, 2));
            Assert.AreEqual(6, state.CountOf("comida_memo"));
            Assert.AreEqual(0, state.CountOf("nabo"));
        }

        [Test]
        public void Smelting_TakesRealTime_FasterWithFire()
        {
            var state = new GameState();
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.id = "lingote";
            recipe.station = CraftStation.Smelter;
            recipe.inputs = new List<ItemAmount> { new("mineral_cobre", 3) };
            recipe.output = new ItemAmount("lingote_cobre", 1);
            recipe.minutes = 30;
            var now = new DateTime(2026, 10, 3, 10, 0, 0);
            state.AddItem("mineral_cobre", 3);
            Assert.IsTrue(Crafting.StartSmelting(state, recipe, 1, now, fireHelper: true));
            Assert.IsFalse(Crafting.SmelterReady(state, now.AddMinutes(9)));
            Assert.IsTrue(Crafting.SmelterReady(state, now.AddMinutes(10)));
            Assert.IsTrue(Crafting.CollectSmelter(state, recipe, now.AddMinutes(10)));
            Assert.AreEqual(1, state.CountOf("lingote_cobre"));
        }
    }

    public class MiningTests
    {
        [Test]
        public void Rocks_AreTheSameAllDay_AndChangeTomorrow()
        {
            var cells = Enumerable.Range(0, 40).Select(i => new Vector2Int(i % 10, i / 10)).ToList();
            var a = Mining.GenerateRocks("2026-10-03", 2, cells, 12);
            var b = Mining.GenerateRocks("2026-10-03", 2, cells, 12);
            var c = Mining.GenerateRocks("2026-10-04", 2, cells, 12);
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a, c);
            Assert.AreEqual(12, a.Count);
        }

        [Test]
        public void HardRocksNeedBetterPicks()
        {
            Assert.AreEqual(0, Mining.RequiredPick(RockKind.Copper));
            Assert.AreEqual(1, Mining.RequiredPick(RockKind.Iron));
            Assert.AreEqual(2, Mining.RequiredPick(RockKind.Gem));
        }

        [Test]
        public void Diggers_FindOneMore()
        {
            int normal = Mining.Drops(RockKind.Quartz, false, new System.Random(1))[0].count;
            int digger = Mining.Drops(RockKind.Quartz, true, new System.Random(1))[0].count;
            Assert.AreEqual(normal + 1, digger);
        }
    }

    public class IslandDayTests
    {
        [Test]
        public void NewDay_PaysShipping_AndMemosWork()
        {
            var db = MemoDatabase.Instance;
            Assume.That(db, Is.Not.Null);
            Assume.That(db.GetItem("nabo"), Is.Not.Null, "Falta construir los objetos de la Fase 6.");
            var state = new GameState();
            var waterMemo = MemoInstance.Create("charquito", 5, null, 300);
            waterMemo.mood = 80;
            state.refuge.Add(waterMemo);
            var plot = new FarmPlot { x = 1, y = 1, tilled = true, seedId = "semilla_nabo" };
            state.island.plots.Add(plot);
            state.island.shippingBin.Add(new ItemStack { id = "nabo", count = 2 });

            var day1 = new DateTime(2026, 10, 3, 9, 0, 0);
            Assert.AreEqual(0, IslandDay.Process(state, db, day1).daysPassed, "el primer día solo se anota");
            var report = IslandDay.Process(state, db, day1.AddDays(2));
            Assert.AreEqual(2, report.daysPassed);
            Assert.AreEqual(db.GetItem("nabo").SellPrice * 2, report.shippingIncome);
            Assert.AreEqual(2, plot.growth, "Charquito regó los dos días");
            Assert.AreEqual(IslandDay.WorkXpPerDay * 2, waterMemo.xp);
            Assert.AreEqual(0, IslandDay.Process(state, db, day1.AddDays(2).AddHours(3)).daysPassed);
        }

        [Test]
        public void UnhappyOrScaredMemos_DoNotWork()
        {
            Assume.That(MemoDatabase.Instance, Is.Not.Null);
            var state = new GameState();
            var sad = MemoInstance.Create("charquito", 5, null, 300);
            sad.mood = 20;
            var scared = MemoInstance.Create("pantuflo", 5, null, 30);
            scared.mood = 90;
            state.refuge.Add(sad);
            state.refuge.Add(scared);
            Assert.IsEmpty(IslandDay.Workers(state, DateTime.Now));
        }

        [Test]
        public void Roles_ByType()
        {
            Assume.That(MemoDatabase.Instance, Is.Not.Null);
            Assert.AreEqual(WorkRole.Water, IslandDay.RoleOf(MemoInstance.Create("charquito", 5)));
            Assert.AreEqual(WorkRole.Grow, IslandDay.RoleOf(MemoInstance.Create("brotito", 5)));
            Assert.AreEqual(WorkRole.Smelt, IslandDay.RoleOf(MemoInstance.Create("tostin", 5)));
            Assert.AreEqual(WorkRole.Process, IslandDay.RoleOf(MemoInstance.Create("chispin", 5)));
            Assert.AreEqual(WorkRole.Dig, IslandDay.RoleOf(MemoInstance.Create("topin", 5)));
            Assert.AreEqual(WorkRole.None, IslandDay.RoleOf(MemoInstance.Create("plumin", 5)));
        }
    }
}
