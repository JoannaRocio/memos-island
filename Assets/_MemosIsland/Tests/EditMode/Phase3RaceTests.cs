using System.Collections.Generic;
using System.Linq;
using MemosIsland.Memos;
using MemosIsland.Race;
using NUnit.Framework;

namespace MemosIsland.Tests
{
    public class TrustRuleTests
    {
        [TestCase(-5, TrustLevel.Hostile)]
        [TestCase(0, TrustLevel.Fear)]
        [TestCase(100, TrustLevel.Distrust)]
        [TestCase(250, TrustLevel.Neutral)]
        [TestCase(450, TrustLevel.Trusting)]
        [TestCase(700, TrustLevel.Friend)]
        [TestCase(1000, TrustLevel.Soulmate)]
        public void LevelFor_UsesGddThresholds(int points, TrustLevel expected) =>
            Assert.AreEqual(expected, TrustRules.LevelFor(points));

        [Test]
        public void NeutralOrBetterNeverDisobeys()
        {
            Assert.AreEqual(0f, TrustRules.DisobeyChance(TrustLevel.Neutral));
            Assert.AreEqual(0f, TrustRules.DisobeyChance(TrustLevel.Soulmate));
            Assert.Greater(TrustRules.DisobeyChance(TrustLevel.Fear), 0f);
        }
    }

    public class GameStateTests
    {
        [Test]
        public void AddMemo_FillsTheTeamThenTheRefuge()
        {
            var state = new MemosIsland.Core.GameState();
            for (int i = 0; i < MemosIsland.Core.GameState.MaxTeamSize; i++)
                Assert.IsTrue(state.AddMemo(MemoInstance.Create("plumin", 3)));
            Assert.IsFalse(state.AddMemo(MemoInstance.Create("zumbi", 3)));
            Assert.AreEqual(6, state.team.Count);
            Assert.AreEqual(1, state.refuge.Count);
        }
    }

    public class RaceSimulationTests
    {
        MemoDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _db = MemoDatabase.Instance;
            Assume.That(_db, Is.Not.Null, "Falta Resources/MemoDatabase.");
        }

        RaceTrack Track(params (string terrain, float length)[] parts) =>
            new(parts.Select(p => (_db.GetTerrain(p.terrain), p.length)));

        static RacerSetup Team(string name, params string[] species) => new()
        {
            name = name,
            team = species.Select(s => MemoInstance.Create(s, 5, null, 300)).ToList(),
        };

        RaceSimulation Sim(RaceTrack track, RacerSetup player, params RacerSetup[] rivals) =>
            new(track, _db.chart, player, rivals, false, 1234);

        static void Run(RaceSimulation sim, float seconds)
        {
            for (float t = 0; t < seconds; t += RaceSimulation.FixedStep) sim.Step(RaceSimulation.FixedStep);
        }

        [Test]
        public void Terrain_ChangesTopSpeed()
        {
            var snow = Sim(Track(("nieve", 500)), Team("A", "tostin"));
            var river = Sim(Track(("rio", 500)), Team("A", "tostin"));
            Assert.Greater(snow.TargetSpeed(snow.Player), river.TargetSpeed(river.Player) * 1.5f);
        }

        [Test]
        public void ActiveMemoTiresWhileBenchRecovers()
        {
            var sim = Sim(Track(("pradera", 2000)), Team("A", "tostin", "brotito"));
            sim.Player.memos[1].energy = 10f;
            Run(sim, 5f);
            Assert.Less(sim.Player.memos[0].energy, sim.Player.memos[0].maxEnergy);
            Assert.Greater(sim.Player.memos[1].energy, 10f);
        }

        [Test]
        public void Switch_HasEightSecondCooldown()
        {
            var sim = Sim(Track(("pradera", 2000)), Team("A", "tostin", "brotito", "charquito"));
            Assert.IsTrue(sim.RequestSwitch(sim.Player, 1));
            Assert.AreEqual(1, sim.Player.activeIndex);
            Assert.IsFalse(sim.RequestSwitch(sim.Player, 2));
            Run(sim, RaceSimulation.SwitchCooldown + 0.1f);
            Assert.IsTrue(sim.RequestSwitch(sim.Player, 2));
            Assert.IsTrue(sim.Player.AllMemosRan);
        }

        [Test]
        public void Enredadera_StopsTheRivalAhead()
        {
            var sim = Sim(Track(("pradera", 2000)), Team("A", "brotito"), Team("B", "topin"));
            var rival = sim.Racers[1];
            rival.position = 10f;
            Run(sim, 1f);
            sim.Player.Active.charge = 1f;
            Assert.IsTrue(sim.TryUseAbility(sim.Player));
            Assert.AreEqual(0f, sim.Player.Active.charge, "la barra se gasta al usarla");
            Run(sim, 0.5f);
            Assert.AreEqual(0f, rival.speed, 0.01f);
        }

        [Test]
        public void Enredadera_WithNobodyAhead_KeepsTheCharge()
        {
            var sim = Sim(Track(("pradera", 2000)), Team("A", "brotito"), Team("B", "topin"));
            sim.Player.position = 50f;
            sim.Player.Active.charge = 1f;
            Assert.IsFalse(sim.TryUseAbility(sim.Player));
            Assert.AreEqual(1f, sim.Player.Active.charge);
        }

        [Test]
        public void Salpicon_TurnsTheSegmentIntoRiver()
        {
            var sim = Sim(Track(("arena", 2000)), Team("A", "charquito"));
            sim.Player.Active.charge = 1f;
            Assert.IsTrue(sim.TryUseAbility(sim.Player));
            Assert.AreEqual("rio", sim.Track.TerrainAt(sim.Player.position).id);
            Run(sim, 6.5f);
            Assert.AreEqual("arena", sim.Track.TerrainAt(sim.Player.position).id);
        }

        [Test]
        public void Descarga_SprintsThenExhausts()
        {
            var sim = Sim(Track(("pradera", 2000)), Team("A", "chispin"));
            Run(sim, 3f);
            float normal = sim.TargetSpeed(sim.Player);
            sim.Player.Active.charge = 1f;
            sim.TryUseAbility(sim.Player);
            Assert.Greater(sim.TargetSpeed(sim.Player), normal * 1.4f);
            Run(sim, 3.5f);
            Assert.Less(sim.TargetSpeed(sim.Player), normal);
        }

        [Test]
        public void Race_FinishesWithRanks_AndIsDeterministic()
        {
            List<string> RunRace()
            {
                var sim = Sim(Track(("pradera", 60), ("rio", 60), ("nieve", 60)),
                    Team("Vos", "tostin", "charquito"), Team("R1", "brotito", "copito"), Team("R2", "chispin", "topin"));
                var ais = sim.Racers.Skip(1).Select(r => new RaceAI(sim, r, 1f)).ToList();
                var playerAi = new RaceAI(sim, sim.Player, 1f);
                for (int i = 0; i < 60 * 300 && !sim.IsOver; i++)
                {
                    playerAi.Tick(RaceSimulation.FixedStep);
                    foreach (var ai in ais) ai.Tick(RaceSimulation.FixedStep);
                    sim.Step(RaceSimulation.FixedStep);
                }
                Assert.IsTrue(sim.IsOver, "la carrera no terminó");
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, sim.Racers.Select(r => r.rank));
                return sim.Racers.OrderBy(r => r.rank).Select(r => $"{r.name}:{r.position:F2}").ToList();
            }
            CollectionAssert.AreEqual(RunRace(), RunRace());
        }

        [Test]
        public void Capture_WildGivesUpWhenExhausted()
        {
            var wild = new RacerSetup { name = "Plumín", isWild = true, team = { MemoInstance.Create("plumin", 3, null, 300) } };
            var sim = new RaceSimulation(Track(("pradera", 5000)), _db.chart, Team("Vos", "tostin"), new[] { wild }, true, 7);
            for (int i = 0; i < 60 * 200 && !sim.IsOver; i++) sim.Step(RaceSimulation.FixedStep);
            Assert.IsTrue(sim.IsOver);
            Assert.IsTrue(sim.Racers[1].gaveUp);
            Assert.IsTrue(sim.PlayerWon);
        }
    }
}
