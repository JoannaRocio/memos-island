using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Town;
using NUnit.Framework;
using UnityEngine;

namespace MemosIsland.Tests
{
    public class ScheduleTests
    {
        // 2026-10-05 es lunes; 2026-10-03, sábado.
        static readonly DateTime Monday = new(2026, 10, 5);
        static readonly DateTime Saturday = new(2026, 10, 3);

        static readonly List<ScheduleEntry> Fer = new()
        {
            new() { hour = 8, scene = "Herreria", activity = "trabajo" },
            new() { hour = 13, scene = "Pueblo", activity = "almuerzo", weather = WeatherFilter.Sunny },
            new() { hour = 13, scene = "Herreria", activity = "almuerzo", weather = WeatherFilter.Rainy },
            new() { hour = 14, scene = "Herreria", activity = "trabajo" },
            new() { hour = 18, scene = "Taberna", activity = "taberna", days = Days.Weekend },
            new() { hour = 18, scene = "", activity = "casa", days = Days.Weekdays },
        };

        [Test]
        public void BeforeFirstEntryIsAtHome() =>
            Assert.IsNull(NeighborSchedule.Resolve(Fer, Monday.AddHours(6), WeatherKind.Sunny));

        [Test]
        public void PicksLatestStartedEntry()
        {
            Assert.AreEqual("Herreria", NeighborSchedule.Resolve(Fer, Monday.AddHours(9.5), WeatherKind.Sunny).scene);
            Assert.AreEqual("trabajo", NeighborSchedule.Resolve(Fer, Monday.AddHours(15), WeatherKind.Sunny).activity);
        }

        [Test]
        public void WeatherChangesWhereTheyLunch()
        {
            Assert.AreEqual("Pueblo", NeighborSchedule.Resolve(Fer, Monday.AddHours(13.2), WeatherKind.Sunny).scene);
            Assert.AreEqual("Herreria", NeighborSchedule.Resolve(Fer, Monday.AddHours(13.2), WeatherKind.Rainy).scene);
        }

        [Test]
        public void DaysFilterAndHomeEntries()
        {
            Assert.IsNull(NeighborSchedule.Resolve(Fer, Monday.AddHours(19), WeatherKind.Sunny), "entre semana se va a su casa");
            Assert.AreEqual("Taberna", NeighborSchedule.Resolve(Fer, Saturday.AddHours(19), WeatherKind.Sunny).scene);
        }

        [Test]
        public void HoursTextForSigns()
        {
            var n = ScriptableObject.CreateInstance<NeighborData>();
            n.schedule = new List<ScheduleEntry>
            {
                new() { hour = 9, scene = "Almacen", activity = "trabajo" },
                new() { hour = 17.5f, scene = "Pueblo", activity = "paseo" },
            };
            Assert.AreEqual("de 9:00 a 17:30", NeighborSchedule.HoursText(n, "trabajo", Monday));
            UnityEngine.Object.DestroyImmediate(n);
        }
    }

    public class NeighborFriendshipTests
    {
        static readonly DateTime Day = new(2026, 10, 5, 10, 0, 0);
        NeighborData _deny;

        [SetUp]
        public void SetUp()
        {
            _deny = ScriptableObject.CreateInstance<NeighborData>();
            _deny.id = "deny";
            _deny.loved = new List<string> { "mermelada" };
            _deny.liked = new List<string> { "nabo" };
            _deny.disliked = new List<string> { "piedra" };
            _deny.events = new List<FriendshipEvent>
            {
                new() { id = "deny_3", hearts = 3 },
                new() { id = "deny_6", hearts = 6 },
            };
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_deny);

        [Test]
        public void TalkCountsOncePerDay()
        {
            var s = new NeighborState { id = "deny" };
            Assert.IsTrue(NeighborFriendship.Talk(s, Day));
            Assert.IsFalse(NeighborFriendship.Talk(s, Day.AddHours(3)));
            Assert.IsTrue(NeighborFriendship.Talk(s, Day.AddDays(1)));
            Assert.AreEqual(NeighborFriendship.TalkPoints * 2, s.points);
            Assert.IsTrue(s.met);
        }

        [Test]
        public void GiftsByTasteAndOncePerDay()
        {
            var s = new NeighborState { id = "deny" };
            Assert.AreEqual(GiftReaction.Loved, NeighborFriendship.Gift(_deny, s, "mermelada", Day));
            Assert.AreEqual(GiftReaction.AlreadyToday, NeighborFriendship.Gift(_deny, s, "nabo", Day));
            Assert.AreEqual(NeighborFriendship.LovedPoints, s.points);
            Assert.AreEqual(GiftReaction.Disliked, NeighborFriendship.Gift(_deny, s, "piedra", Day.AddDays(1)));
            Assert.AreEqual(GiftReaction.Neutral, NeighborFriendship.ReactionTo(_deny, "flor"));
            Assert.AreEqual(NeighborFriendship.LovedPoints + NeighborFriendship.DislikedPoints, s.points);
        }

        [Test]
        public void HeartsAreClampedFromZeroToTen()
        {
            var s = new NeighborState();
            NeighborFriendship.Add(s, -50);
            Assert.AreEqual(0, s.points);
            NeighborFriendship.Add(s, 5000);
            Assert.AreEqual(NeighborFriendship.MaxPoints, s.points);
            Assert.AreEqual(10, NeighborFriendship.Hearts(s));
        }

        [Test]
        public void EventsUnlockByHeartsInOrder()
        {
            var s = new NeighborState();
            Assert.IsNull(NeighborFriendship.PendingEvent(_deny, s));
            s.points = 650;
            Assert.AreEqual("deny_3", NeighborFriendship.PendingEvent(_deny, s).id);
            s.seenEvents.Add("deny_3");
            Assert.AreEqual("deny_6", NeighborFriendship.PendingEvent(_deny, s).id);
            s.seenEvents.Add("deny_6");
            Assert.IsNull(NeighborFriendship.PendingEvent(_deny, s));
        }
    }

    public class FriendshipEventSceneTests
    {
        [Test]
        public void EventsWithSceneOnlyHappenThere()
        {
            var fer = ScriptableObject.CreateInstance<NeighborData>();
            fer.events = new List<FriendshipEvent> { new() { id = "fer_3", hearts = 3, scene = "Map_Herreria" } };
            var s = new NeighborState { points = 300 };
            Assert.IsNull(NeighborFriendship.PendingEvent(fer, s, "Map_Taberna"));
            Assert.AreEqual("fer_3", NeighborFriendship.PendingEvent(fer, s, "Map_Herreria").id);
            UnityEngine.Object.DestroyImmediate(fer);
        }
    }

    public class RequestBoardTests
    {
        static readonly DateTime Day = new(2026, 10, 5, 10, 0, 0);
        readonly List<NeighborData> _neighbors = new();

        [SetUp]
        public void SetUp()
        {
            foreach (var id in new[] { "deny", "fer", "jojo", "anni" })
            {
                var n = ScriptableObject.CreateInstance<NeighborData>();
                n.id = id;
                n.requests = new List<RequestTemplate>
                {
                    new() { kind = RequestKind.Deliver, targetId = "nabo", count = 3, reward = 150 },
                    new() { kind = RequestKind.ShowMemo, targetId = "zumbi", reward = 100 },
                };
                _neighbors.Add(n);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var n in _neighbors) UnityEngine.Object.DestroyImmediate(n);
            _neighbors.Clear();
        }

        [Test]
        public void ThreeDistinctRequestsStableDuringTheDay()
        {
            var town = new TownState();
            RequestBoard.Refresh(town, _neighbors, Day);
            Assert.AreEqual(RequestBoard.PerDay, town.board.Count);
            Assert.AreEqual(town.board.Count, town.board.Select(r => r.neighborId).Distinct().Count());
            var first = town.board.Select(r => $"{r.neighborId}{r.template}").ToList();
            town.board[0].accepted = true;
            RequestBoard.Refresh(town, _neighbors, Day.AddHours(5));
            Assert.IsTrue(town.board[0].accepted, "el mismo día no se regenera");

            var again = new TownState();
            RequestBoard.Refresh(again, _neighbors, Day.AddHours(2));
            CollectionAssert.AreEqual(first, again.board.Select(r => $"{r.neighborId}{r.template}").ToList(), "determinista por fecha");
        }

        [Test]
        public void DeliverPaysAndRemovesItems()
        {
            var state = new GameState();
            int money = state.island.money;
            var friend = new NeighborState();
            var req = new DailyRequest { neighborId = "deny", template = 0 };
            var t = RequestBoard.TemplateOf(req, _neighbors[0]);
            state.AddItem("nabo", 2);
            Assert.IsFalse(RequestBoard.Complete(req, t, state, friend), "sin aceptar no se entrega");
            req.accepted = true;
            Assert.IsFalse(RequestBoard.Complete(req, t, state, friend), "faltan nabos");
            state.AddItem("nabo", 2);
            Assert.IsTrue(RequestBoard.Complete(req, t, state, friend));
            Assert.AreEqual(1, state.CountOf("nabo"));
            Assert.AreEqual(money + 150, state.island.money);
            Assert.AreEqual(NeighborFriendship.RequestPoints, friend.points);
            Assert.IsFalse(RequestBoard.Complete(req, t, state, friend), "no se cobra dos veces");
        }

        [Test]
        public void ShowMemoNeedsTheRightCompanion()
        {
            var state = new GameState();
            var memo = MemoInstance.Create("plumin", 5);
            state.refuge.Add(memo);
            state.companionUid = memo.uid;
            var t = new RequestTemplate { kind = RequestKind.ShowMemo, targetId = "zumbi" };
            Assert.IsFalse(RequestBoard.CanComplete(t, state));
            memo.speciesId = "zumbi";
            Assert.IsTrue(RequestBoard.CanComplete(t, state));
        }
    }

    public class WeatherTests
    {
        [Test]
        public void SameDateSameWeatherAndRoughlyOneInFourRainy()
        {
            Weather.DebugOverride = null;
            var start = new DateTime(2026, 1, 1);
            Assert.AreEqual(Weather.Roll(start.AddHours(3)), Weather.Roll(start.AddHours(20)));
            int rainy = Enumerable.Range(0, 365).Count(d => Weather.Roll(start.AddDays(d)) != WeatherKind.Sunny);
            Assert.That(rainy, Is.InRange(55, 130));
        }

        [Test]
        public void StormsAreRareAndCountAsRain()
        {
            Weather.DebugOverride = null;
            var start = new DateTime(2026, 1, 1);
            int storms = Enumerable.Range(0, 365).Count(d => Weather.Roll(start.AddDays(d)) == WeatherKind.Stormy);
            Assert.That(storms, Is.InRange(12, 50), "más o menos uno de cada doce días");
            var stormDay = Enumerable.Range(0, 365).Select(d => start.AddDays(d)).First(d => Weather.Roll(d) == WeatherKind.Stormy);
            Assert.IsTrue(Weather.IsRainy(stormDay), "con tormenta también llueve (la huerta se riega)");
        }
    }
}
