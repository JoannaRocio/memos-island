using System;
using System.Collections.Generic;
using MemosIsland.Core;
using MemosIsland.Memos;
using NUnit.Framework;

namespace MemosIsland.Tests
{
    public class NeedsTests
    {
        [Test]
        public void Decay_LowersNeedsAndMood()
        {
            var m = MemoInstance.Create("plumin", 5);
            MemoNeeds.Decay(m, 2f);
            Assert.AreEqual(80f - 2f * MemoNeeds.HungerPerHour, m.hunger, 1e-3f);
            Assert.Less(m.mood, 80);
        }

        [Test]
        public void Sleeping_RecoversSleep()
        {
            var m = MemoInstance.Create("plumin", 5);
            m.sleep = 20f;
            MemoNeeds.Decay(m, 1f, asleep: true);
            Assert.AreEqual(50f, m.sleep, 1e-3f);
        }

        [Test]
        public void CatchUp_IsCappedAtTwelveHours()
        {
            var m = MemoInstance.Create("plumin", 5);
            var now = new DateTime(2026, 10, 3, 12, 0, 0);
            m.needsUpdatedTicks = now.AddDays(-5).Ticks;
            MemoNeeds.CatchUp(m, now);
            Assert.AreEqual(Math.Max(0f, 80f - 12f * MemoNeeds.HungerPerHour), m.hunger, 1e-3f);
            Assert.AreEqual(now.Ticks, m.needsUpdatedTicks);
        }

        [Test]
        public void MostUrgent_IsTheLowest()
        {
            var m = MemoInstance.Create("plumin", 5);
            m.fun = 5f;
            Assert.AreEqual(Need.Fun, MemoNeeds.MostUrgent(m));
        }
    }

    public class CareTests
    {
        static readonly DateTime Day1 = new(2026, 10, 3, 10, 0, 0);

        [Test]
        public void Feed_TwicePerDay_FavoriteGivesMore()
        {
            var m = MemoInstance.Create("plumin", 5, null, 300);
            Assert.AreEqual(MemoCare.FavoriteFeedTrust, MemoCare.Apply(m, CareAction.Feed, Day1, favoriteFood: true).trustGained);
            Assert.AreEqual(MemoCare.FeedTrust, MemoCare.Apply(m, CareAction.Feed, Day1).trustGained);
            Assert.IsFalse(MemoCare.Apply(m, CareAction.Feed, Day1).success);
            Assert.IsTrue(MemoCare.Apply(m, CareAction.Feed, Day1.AddDays(1)).success, "al día siguiente se puede de nuevo");
        }

        [Test]
        public void ScaredMemos_OnlyAcceptFoodAndCloseness()
        {
            var m = MemoInstance.Create("zumbi", 4, null, 30);
            Assert.IsFalse(MemoCare.Apply(m, CareAction.Pet, Day1).success);
            Assert.IsFalse(MemoCare.Apply(m, CareAction.Play, Day1).success);
            Assert.IsFalse(MemoCare.Apply(m, CareAction.Bath, Day1).success);
            Assert.IsTrue(MemoCare.Apply(m, CareAction.Feed, Day1).success);
            Assert.IsTrue(MemoCare.Apply(m, CareAction.StayClose, Day1).success);
        }

        [Test]
        public void StayClose_IsCappedAndOnlyForWaryMemos()
        {
            var wary = MemoInstance.Create("zumbi", 4, null, 30);
            int total = 0;
            for (int i = 0; i < 30; i++) total += MemoCare.Apply(wary, CareAction.StayClose, Day1).trustGained;
            Assert.AreEqual(MemoCare.StayCloseMaxPerDay, total);

            var friend = MemoInstance.Create("plumin", 5, null, 800);
            Assert.IsFalse(MemoCare.Apply(friend, CareAction.StayClose, Day1).success);
        }

        [Test]
        public void Pet_GivesLessWhenDistrustful()
        {
            var m = MemoInstance.Create("bostezo", 5, null, 150);
            Assert.AreEqual(MemoCare.PetTrustDistrust, MemoCare.Apply(m, CareAction.Pet, Day1).trustGained);
            Assert.IsFalse(MemoCare.Apply(m, CareAction.Pet, Day1).success, "una vez por día");
        }

        [Test]
        public void Bath_EveryThreeDays()
        {
            var m = MemoInstance.Create("plumin", 5, null, 300);
            Assert.IsTrue(MemoCare.Apply(m, CareAction.Bath, Day1).success);
            Assert.IsFalse(MemoCare.Apply(m, CareAction.Bath, Day1.AddDays(2)).success);
            Assert.IsTrue(MemoCare.Apply(m, CareAction.Bath, Day1.AddDays(3)).success);
        }

        [Test]
        public void ReachingANewLevel_UnlocksAMemory()
        {
            var m = MemoInstance.Create("plumin", 5, null, 445);
            Assert.AreEqual(TrustLevel.Neutral, m.highestTrust);
            var r = MemoCare.Apply(m, CareAction.Feed, Day1);
            Assert.IsTrue(r.newMemory);
            Assert.AreEqual(TrustLevel.Trusting, m.highestTrust);
        }

        [Test]
        public void Mimoso_GainsMoreTrust()
        {
            Assume.That(MemoDatabase.Instance, Is.Not.Null);
            var m = MemoInstance.Create("plumin", 5, "mimoso", 300);
            Assert.AreEqual(19, MemoCare.Apply(m, CareAction.Feed, Day1, favoriteFood: true).trustGained); // 15 × 1,25
        }

        [Test]
        public void FollowTime_GivesTwoPerHour()
        {
            var m = MemoInstance.Create("plumin", 5, null, 300);
            Assert.IsFalse(MemoCare.AddFollowTime(m, 1800f).success);
            Assert.AreEqual(2, MemoCare.AddFollowTime(m, 1800f).trustGained);
        }
    }

    public class FriendshipTests
    {
        [Test]
        public void Levels_AndOrderIndependence()
        {
            var all = new List<MemoRelationship>();
            var a = MemoInstance.Create("plumin", 5);
            var b = MemoInstance.Create("zumbi", 5);
            Assert.AreEqual(FriendshipLevel.Strangers, Friendship.Level(all, a.uid, b.uid));
            Assert.IsTrue(Friendship.Add(all, a, b, 25f));
            Assert.AreEqual(FriendshipLevel.Acquaintances, Friendship.Level(all, b.uid, a.uid));
            Friendship.Add(all, b, a, 200f);
            Assert.IsTrue(Friendship.AreBestFriends(all, a.uid, b.uid));
            Assert.AreEqual(1, all.Count);
        }

        [Test]
        public void Compatibility_FollowsTheGdd()
        {
            Assert.Greater(Friendship.Compatibility("jugueton", "mimoso"), 1f);
            Assert.Less(Friendship.Compatibility("orgulloso", "timido"), 1f);
            Assert.AreEqual(Friendship.Compatibility("timido", "orgulloso"), Friendship.Compatibility("orgulloso", "timido"));
        }
    }

    public class FriendshipBoostTests
    {
        [Test]
        public void SwitchingBetweenBestFriends_EntersAtFullSpeed()
        {
            var db = MemoDatabase.Instance;
            Assume.That(db, Is.Not.Null);
            var track = new MemosIsland.Race.RaceTrack(new[] { (db.GetTerrain("pradera"), 2000f) });
            var team = new MemosIsland.Race.RacerSetup
            {
                name = "Vos",
                team = { MemoInstance.Create("tostin", 5, null, 300), MemoInstance.Create("brotito", 5, null, 300) },
            };
            var sim = new MemosIsland.Race.RaceSimulation(track, db.chart, team, new MemosIsland.Race.RacerSetup[0], false, 1);
            bool boosted = false;
            sim.AreBestFriends = (a, b) => true;
            sim.FriendshipBoost += _ => boosted = true;
            for (int i = 0; i < 120; i++) sim.Step(MemosIsland.Race.RaceSimulation.FixedStep);
            sim.RequestSwitch(sim.Player, 1);
            Assert.IsTrue(boosted);
            // Entra a toda velocidad (sin el sprint extra del impulso) y sin la transición lenta del relevo normal.
            Assert.GreaterOrEqual(sim.Player.speed, sim.TargetSpeed(sim.Player) / 1.15f * 0.99f);
            Assert.IsFalse(sim.Player.Has(MemosIsland.Race.StatusKind.Switching));
        }
    }

    public class MemoryAndStateTests
    {
        [Test]
        public void OnlyRescuedMemosRememberFear()
        {
            Assume.That(MemoDatabase.Instance, Is.Not.Null);
            var wild = MemoInstance.Create("plumin", 5, null, 300);
            var rescued = MemoInstance.Create("zumbi", 5, null, 300);
            rescued.rescued = true;
            Assert.AreEqual(2, MemoryBook.Unlocked(wild).Count);      // Desconfianza y Neutral
            Assert.AreEqual(3, MemoryBook.Unlocked(rescued).Count);   // + el del collar
            StringAssert.Contains("collar", MemoryBook.Unlocked(rescued)[0]);
        }

        [Test]
        public void ToggleTeam_RespectsLimits()
        {
            var state = new GameState();
            var only = MemoInstance.Create("plumin", 5);
            state.team.Add(only);
            Assert.IsFalse(state.ToggleTeam(only), "el equipo no puede quedar vacío");
            for (int i = 0; i < 5; i++) state.team.Add(MemoInstance.Create("zumbi", 3));
            var extra = MemoInstance.Create("copito", 3);
            state.refuge.Add(extra);
            Assert.IsFalse(state.ToggleTeam(extra), "el equipo está lleno");
            Assert.IsTrue(state.ToggleTeam(only));
            Assert.IsTrue(state.ToggleTeam(extra));
            Assert.IsTrue(state.IsInTeam(extra));
        }

        [Test]
        public void DailyVisit_OncePerRealDay()
        {
            var state = new GameState();
            var m = MemoInstance.Create("plumin", 5, null, 300);
            state.refuge.Add(m);
            var day = new DateTime(2026, 10, 3);
            Assert.AreEqual(0, state.RunDailyVisit(day).Count, "el primer día solo se anota");
            Assert.AreEqual(1, state.RunDailyVisit(day.AddDays(1)).Count);
            Assert.AreEqual(0, state.RunDailyVisit(day.AddDays(1).AddHours(5)).Count);
            Assert.AreEqual(301, m.trust);
        }
    }
}
