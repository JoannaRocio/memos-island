using System.Linq;
using MemosIsland.Memos;
using NUnit.Framework;
using UnityEngine;

namespace MemosIsland.Tests
{
    public class EffectivenessRuleTests
    {
        [TestCase(Effectiveness.Strong, Effectiveness.Normal, Effectiveness.Strong)]
        [TestCase(Effectiveness.Strong, Effectiveness.Strong, Effectiveness.Strong)]
        [TestCase(Effectiveness.Weak, Effectiveness.Normal, Effectiveness.Weak)]
        [TestCase(Effectiveness.Weak, Effectiveness.Weak, Effectiveness.Weak)]
        [TestCase(Effectiveness.Strong, Effectiveness.Weak, Effectiveness.Normal)]
        [TestCase(Effectiveness.Normal, Effectiveness.Normal, Effectiveness.Normal)]
        public void Combine_FollowsTheDualTypeRule(Effectiveness a, Effectiveness b, Effectiveness expected)
        {
            Assert.AreEqual(expected, EffectivenessChart.Combine(a, b));
            Assert.AreEqual(expected, EffectivenessChart.Combine(b, a));
        }

        [Test]
        public void Multipliers_MatchTheGdd()
        {
            Assert.AreEqual(1.30f, EffectivenessChart.Multiplier(Effectiveness.Strong), 1e-5f);
            Assert.AreEqual(1.00f, EffectivenessChart.Multiplier(Effectiveness.Normal), 1e-5f);
            Assert.AreEqual(0.75f, EffectivenessChart.Multiplier(Effectiveness.Weak), 1e-5f);
        }
    }

    public class StatsTests
    {
        [Test]
        public void AtLevel_GrowsFourPercentPerLevel()
        {
            Assert.AreEqual(5f, MemoStats.AtLevel(5, 0), 1e-4f);
            Assert.AreEqual(7f, MemoStats.AtLevel(5, 10), 1e-4f);
        }

        [Test]
        public void Temperament_ModifiesStats()
        {
            var t = ScriptableObject.CreateInstance<Temperament>();
            t.accelerationMultiplier = 1.1f;
            t.staminaMultiplier = 0.9f;
            var s = ComputedStats.For(new MemoStats(5, 10, 10, 5), 0, t);
            Assert.AreEqual(5f, s.Speed, 1e-4f);
            Assert.AreEqual(11f, s.Acceleration, 1e-4f);
            Assert.AreEqual(9f, s.Stamina, 1e-4f);
            Object.DestroyImmediate(t);
        }
    }

    /// <summary>Revisa los datos reales generados desde el GDD (Resources/MemoDatabase).</summary>
    public class MemoDatabaseTests
    {
        MemoDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _db = MemoDatabase.Instance;
            Assume.That(_db, Is.Not.Null, "Falta Resources/MemoDatabase: correr Memos Island ▸ Fase 2 ▸ Crear datos de Memos.");
        }

        [Test]
        public void Has20SpeciesNumberedOneToTwenty() =>
            CollectionAssert.AreEqual(Enumerable.Range(1, 20), _db.SpeciesByNumber.Select(s => s.number));

        [Test]
        public void EverySpeciesIsComplete()
        {
            foreach (var s in _db.species)
            {
                Assert.IsNotNull(s.primaryType, s.id);
                Assert.IsNotNull(s.ability, s.id);
                Assert.IsFalse(string.IsNullOrEmpty(s.displayName), s.id);
                Assert.IsFalse(string.IsNullOrEmpty(s.description), s.id);
                Assert.That(s.raceFrames, Has.Length.GreaterThan(0), $"{s.id} sin sprite");
                Assert.That(s.shinyRaceFrames, Has.Length.GreaterThan(0), $"{s.id} sin sprite brillante");
                Assert.That(s.collarRaceFrames, Has.Length.GreaterThan(0), $"{s.id} sin sprite con collar");
                foreach (var v in new[] { s.baseStats.speed, s.baseStats.acceleration, s.baseStats.stamina, s.baseStats.charge })
                    Assert.That(v, Is.InRange(1, 10), s.id);
            }
        }

        [Test]
        public void ChartIsTwelveByTwelve()
        {
            Assert.AreEqual(12, _db.chart.types.Count);
            Assert.AreEqual(12, _db.chart.terrains.Count);
        }

        [Test]
        public void ChartMatchesSomeGddCells()
        {
            var fuego = _db.GetMemoType("fuego");
            var agua = _db.GetMemoType("agua");
            var planta = _db.GetMemoType("planta");
            Assert.AreEqual(Effectiveness.Strong, _db.chart.Get(fuego, _db.GetTerrain("nieve")));
            Assert.AreEqual(Effectiveness.Weak, _db.chart.Get(fuego, _db.GetTerrain("rio")));
            Assert.AreEqual(Effectiveness.Strong, _db.chart.Get(agua, _db.GetTerrain("rio")));
            Assert.AreEqual(Effectiveness.Normal, _db.chart.Get(planta, _db.GetTerrain("cueva")));
        }

        [Test]
        public void DualTypeExamples()
        {
            // Brasón (Fuego/Roca) en Ceniza: ▲ + ▲ = ▲ · en Río: ▼ + ▼ = ▼ · en Hielo: ▲ + · = ▲
            var brason = _db.GetSpecies("brason");
            Assert.AreEqual(Effectiveness.Strong, _db.EffectivenessFor(brason, _db.GetTerrain("ceniza")));
            Assert.AreEqual(Effectiveness.Weak, _db.EffectivenessFor(brason, _db.GetTerrain("rio")));
            Assert.AreEqual(Effectiveness.Strong, _db.EffectivenessFor(brason, _db.GetTerrain("hielo")));
            // Pantuflo (Agua/Tierra) en Arena: ▼ + ▲ = ·
            Assert.AreEqual(Effectiveness.Normal, _db.EffectivenessFor(_db.GetSpecies("pantuflo"), _db.GetTerrain("arena")));
        }

        [Test]
        public void StartersEvolveAtLevel16()
        {
            foreach (var (from, to) in new[] { ("tostin", "brason"), ("brotito", "ramazon"), ("charquito", "chapuzon") })
            {
                var s = _db.GetSpecies(from);
                Assert.AreEqual(_db.GetSpecies(to), s.evolvesTo, from);
                Assert.AreEqual(16, s.evolutionLevel, from);
            }
        }

        [Test]
        public void LockedAndExclusiveSpecies()
        {
            Assert.AreEqual(MemoAvailability.Locked, _db.GetSpecies("draken").availability);
            Assert.AreEqual(MemoAvailability.Locked, _db.GetSpecies("randy").availability);
            Assert.AreEqual(MemoAvailability.Available, _db.GetSpecies("karman").availability);
            foreach (var id in new[] { "tuerquita", "ferrolobo", "imanta" })
                Assert.AreEqual(MemoAvailability.NotObtainable, _db.GetSpecies(id).availability, id);
        }

        [Test]
        public void HasEightTemperaments() => Assert.AreEqual(8, _db.temperaments.Count);
    }
}
