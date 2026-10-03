using System;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Race;
using NUnit.Framework;
using UnityEngine;

namespace MemosIsland.Tests
{
    public class LevelTests
    {
        [Test]
        public void AddXp_CanGainSeveralLevels()
        {
            var m = MemoInstance.Create("plumin", 5);
            int needed = Progression.XpToNext(5) + Progression.XpToNext(6);
            var levels = Progression.AddXp(m, needed + 3);
            CollectionAssert.AreEqual(new[] { 6, 7 }, levels);
            Assert.AreEqual(3, m.xp);
        }

        [Test]
        public void Level_IsCappedAtThirty()
        {
            var m = MemoInstance.Create("plumin", 29);
            Progression.AddXp(m, 100000);
            Assert.AreEqual(Progression.MaxLevel, m.level);
            Assert.AreEqual(0, m.xp);
            Assert.IsEmpty(Progression.AddXp(m, 50));
        }

        [Test]
        public void RaceXp_RewardsWinningAndFullTeam()
        {
            int basic = Progression.RaceXp(RaceFormat.Trainer, false, false);
            Assert.AreEqual(30, basic);
            Assert.AreEqual(45, Progression.RaceXp(RaceFormat.Trainer, true, false));
            Assert.AreEqual(135, Progression.RaceXp(RaceFormat.Cup, true, true));
        }
    }

    public class EvolutionTests
    {
        static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0);

        [SetUp]
        public void SetUp() => Assume.That(MemoDatabase.Instance, Is.Not.Null);

        [Test]
        public void StartersEvolveAtSixteen_UnlessCancelledAtThatLevel()
        {
            var m = MemoInstance.Create("charquito", 15);
            Assert.IsNull(Progression.EvolutionTarget(m));
            m.level = 16;
            Assert.AreEqual("chapuzon", Progression.EvolutionTarget(m).id);
            m.cancelledEvolutionAtLevel = 16;
            Assert.IsNull(Progression.EvolutionTarget(m));
            m.level = 17;
            Assert.IsNotNull(Progression.EvolutionTarget(m), "lo vuelve a intentar al subir de nivel");
        }

        [Test]
        public void Evolving_StartsTheRebelPhase()
        {
            var m = MemoInstance.Create("charquito", 16, null, 750); // Amigo
            Progression.Evolve(m, MemoDatabase.Instance.GetSpecies("chapuzon"), Now);
            Assert.AreEqual("chapuzon", m.speciesId);
            Assert.AreEqual(TrustLevel.Trusting, m.TrustLevel, "pierde un nivel");
            Assert.IsTrue(Progression.IsRebel(m, Now.AddDays(3)));
            Assert.IsFalse(Progression.IsRebel(m, Now.AddDays(8)));
            Assert.GreaterOrEqual(Progression.DisobeyChance(m, Now.AddDays(1)), Progression.RebelMinDisobey);
        }

        [Test]
        public void Rebel_NeverDropsBelowNeutral()
        {
            var m = MemoInstance.Create("tostin", 16, null, 300); // Neutral
            Progression.Evolve(m, MemoDatabase.Instance.GetSpecies("brason"), Now);
            Assert.AreEqual(TrustLevel.Neutral, m.TrustLevel);
        }

        [Test]
        public void RecoveringTheLostLevel_GrowsTogether()
        {
            var m = MemoInstance.Create("brotito", 16, null, 720);
            Progression.Evolve(m, MemoDatabase.Instance.GetSpecies("ramazon"), Now);
            m.trust = TrustRules.MinPointsFor(TrustLevel.Friend);
            Assert.IsTrue(Progression.CheckRebelRecovery(m, Now.AddDays(2)));
            Assert.IsTrue(m.grewTogether);
            Assert.AreEqual(0, m.rebelUntilTicks);
            StringAssert.Contains("Crecieron juntos", MemoryBook.Unlocked(m).Last());
        }

        [Test]
        public void WithoutLostLevel_GrowsTogetherAfterSevenDays()
        {
            var m = MemoInstance.Create("tostin", 16, null, 300);
            Progression.Evolve(m, MemoDatabase.Instance.GetSpecies("brason"), Now);
            Assert.IsFalse(Progression.CheckRebelRecovery(m, Now.AddDays(3)));
            Assert.IsTrue(Progression.CheckRebelRecovery(m, Now.AddDays(7)));
        }
    }

    public class EquipmentTests
    {
        static ItemData Item(TerrainRule rule, params RaceTerrain[] terrains)
        {
            var it = ScriptableObject.CreateInstance<ItemData>();
            it.terrainRule = rule;
            it.terrains = terrains.ToList();
            return it;
        }

        [Test]
        public void TerrainRules()
        {
            var mud = ScriptableObject.CreateInstance<RaceTerrain>();
            var ice = ScriptableObject.CreateInstance<RaceTerrain>();
            Assert.AreEqual(Effectiveness.Normal, EquipmentRules.Adjust(Effectiveness.Weak, Item(TerrainRule.UpgradeOneStep, mud), null, mud));
            Assert.AreEqual(Effectiveness.Strong, EquipmentRules.Adjust(Effectiveness.Normal, Item(TerrainRule.UpgradeOneStep, mud), null, mud));
            Assert.AreEqual(Effectiveness.Strong, EquipmentRules.Adjust(Effectiveness.Weak, Item(TerrainRule.ForceStrong, mud), null, mud));
            Assert.AreEqual(Effectiveness.Normal, EquipmentRules.Adjust(Effectiveness.Weak, Item(TerrainRule.RemoveWeakness, ice), null, ice));
            Assert.AreEqual(Effectiveness.Weak, EquipmentRules.Adjust(Effectiveness.Weak, Item(TerrainRule.RemoveWeakness, ice), null, mud),
                "solo en sus terrenos");

            var gem = ScriptableObject.CreateInstance<ItemData>();
            gem.amuletEffect = AmuletEffect.TerrainGem;
            gem.gemTerrain = ice;
            Assert.AreEqual(Effectiveness.Strong, EquipmentRules.Adjust(Effectiveness.Weak, null, gem, ice));
        }

        [Test]
        public void StatBonuses_AreClamped()
        {
            var weights = ScriptableObject.CreateInstance<ItemData>();
            weights.speedBonus = 1;
            weights.staminaBonus = -1;
            var s = EquipmentRules.WithEquipment(new MemoStats(10, 5, 1, 5), weights);
            Assert.AreEqual(10, s.speed);
            Assert.AreEqual(1, s.stamina);
        }

        [Test]
        public void Equip_SwapsWithTheInventory()
        {
            var state = new GameState();
            var m = MemoInstance.Create("plumin", 5);
            state.AddItem("aletas");
            state.AddItem("herraduras");
            Assert.IsTrue(state.Equip(m, ItemKind.Equipment, "aletas"));
            Assert.AreEqual(0, state.CountOf("aletas"));
            Assert.IsTrue(state.Equip(m, ItemKind.Equipment, "herraduras"));
            Assert.AreEqual("herraduras", m.equipmentId);
            Assert.AreEqual(1, state.CountOf("aletas"), "el anterior vuelve al inventario");
            Assert.IsFalse(state.Equip(m, ItemKind.Equipment, "botas_clavos"), "no se puede equipar lo que no tenés");
            Assert.IsTrue(state.Equip(m, ItemKind.Equipment, null));
            Assert.IsNull(m.equipmentId);
            Assert.AreEqual(1, state.CountOf("herraduras"));
        }

        [Test]
        public void Accessory_CountsOnlyTheFirstTime()
        {
            var accessory = ScriptableObject.CreateInstance<ItemData>();
            accessory.id = "acc_test";
            accessory.displayName = "Test";
            var m = MemoInstance.Create("plumin", 5, null, 300);
            var first = MemoCare.TryAccessory(m, accessory);
            Assert.AreEqual(MemoCare.LikesAccessory(m, "acc_test"), first.success);
            Assert.IsFalse(MemoCare.TryAccessory(m, accessory).success, "la segunda vez no suma");
        }

        [Test]
        public void HostileMemos_Growl()
        {
            var karman = MemoInstance.Create("karman", 12, null, -40);
            var r = MemoCare.Apply(karman, CareAction.Pet, DateTime.Now);
            Assert.IsFalse(r.success);
            StringAssert.Contains("gruñe", r.message);
            Assert.IsTrue(MemoCare.Apply(karman, CareAction.Feed, DateTime.Now).success);
        }
    }

    public class AmuletRaceTests
    {
        MemoDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _db = MemoDatabase.Instance;
            Assume.That(_db, Is.Not.Null);
            Assume.That(_db.GetItem("piedra_carga"), Is.Not.Null, "Falta construir los objetos (Fase 5).");
        }

        RaceSimulation Sim(MemoInstance a, MemoInstance b = null, MemoInstance rival = null)
        {
            var track = new RaceTrack(new[] { (_db.GetTerrain("pradera"), 2000f) });
            var team = new RacerSetup { name = "Vos", team = { a } };
            if (b != null) team.team.Add(b);
            var rivals = rival != null ? new[] { new RacerSetup { name = "R", team = { rival } } } : new RacerSetup[0];
            return new RaceSimulation(track, _db.chart, team, rivals, false, 3);
        }

        [Test]
        public void ChargeStone_StartsWithAbilityReady()
        {
            var m = MemoInstance.Create("tostin", 5, null, 300);
            m.amuletId = "piedra_carga";
            Assert.IsTrue(Sim(m).Player.Active.AbilityReady);
        }

        [Test]
        public void CalmBell_BlocksOneHinderingEffect()
        {
            var bell = MemoInstance.Create("topin", 5, null, 300);
            bell.amuletId = "cascabel_calma";
            var sim = Sim(MemoInstance.Create("brotito", 5, null, 300), rival: bell);
            var rival = sim.Racers[1];
            rival.AddEffect(StatusKind.Root, 2f);
            Assert.IsFalse(rival.Has(StatusKind.Root), "el primero lo bloquea");
            rival.AddEffect(StatusKind.Root, 2f);
            Assert.IsTrue(rival.Has(StatusKind.Root), "el segundo ya no");
        }

        [Test]
        public void RelayAmulet_EntersAtFullSpeed()
        {
            var second = MemoInstance.Create("brotito", 5, null, 300);
            second.amuletId = "amuleto_relevo";
            var sim = Sim(MemoInstance.Create("tostin", 5, null, 300), second);
            for (int i = 0; i < 120; i++) sim.Step(RaceSimulation.FixedStep);
            sim.RequestSwitch(sim.Player, 1);
            Assert.IsFalse(sim.Player.Has(StatusKind.Switching));
            Assert.GreaterOrEqual(sim.Player.speed, sim.TargetSpeed(sim.Player) * 0.99f);
        }
    }
}
