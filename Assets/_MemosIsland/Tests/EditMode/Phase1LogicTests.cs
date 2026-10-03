using System;
using System.Collections.Generic;
using MemosIsland.Core;
using MemosIsland.UI;
using MemosIsland.World;
using NUnit.Framework;
using UnityEngine;

namespace MemosIsland.Tests
{
    public class GameClockTests
    {
        [TestCase(0f, DayPhase.Night)]
        [TestCase(4.99f, DayPhase.Night)]
        [TestCase(5f, DayPhase.Dawn)]
        [TestCase(6.5f, DayPhase.Dawn)]
        [TestCase(7f, DayPhase.Day)]
        [TestCase(16.9f, DayPhase.Day)]
        [TestCase(17f, DayPhase.Dusk)]
        [TestCase(19.9f, DayPhase.Dusk)]
        [TestCase(20f, DayPhase.Night)]
        [TestCase(23.5f, DayPhase.Night)]
        public void PhaseFor_FollowsTheGddSchedule(float hour, DayPhase expected) =>
            Assert.AreEqual(expected, GameClock.PhaseFor(hour));

        [Test]
        public void NightFactor_IsFullAtNightAndZeroAtNoon()
        {
            Assert.AreEqual(1f, GameClock.NightFactorFor(4f));
            Assert.AreEqual(0f, GameClock.NightFactorFor(12f));
            Assert.AreEqual(0.5f, GameClock.NightFactorFor(6f), 1e-4f);
            Assert.AreEqual(0.5f, GameClock.NightFactorFor(19f), 1e-4f);
        }

        [Test]
        public void ToHourOfDay_IncludesMinutes() =>
            Assert.AreEqual(4.5f, GameClock.ToHourOfDay(new DateTime(2026, 10, 3, 4, 30, 0)), 1e-4f);
    }

    public class DirectionTests
    {
        [Test]
        public void FromInput_NoInput_ReturnsNull() =>
            Assert.IsNull(DirectionExtensions.FromInput(Vector2.zero, Direction.Down));

        [Test]
        public void FromInput_SingleAxis()
        {
            Assert.AreEqual(Direction.Right, DirectionExtensions.FromInput(Vector2.right, Direction.Down));
            Assert.AreEqual(Direction.Up, DirectionExtensions.FromInput(Vector2.up, Direction.Left));
        }

        [Test]
        public void FromInput_Diagonal_KeepsCurrentWhenPossible()
        {
            var diag = new Vector2(0.707f, 0.707f);
            Assert.AreEqual(Direction.Right, DirectionExtensions.FromInput(diag, Direction.Right));
            Assert.AreEqual(Direction.Up, DirectionExtensions.FromInput(diag, Direction.Up));
            Assert.AreEqual(Direction.Up, DirectionExtensions.FromInput(diag, Direction.Left));
        }

        [Test]
        public void Opposite_IsSymmetric()
        {
            foreach (Direction d in Enum.GetValues(typeof(Direction)))
                Assert.AreEqual(d, d.Opposite().Opposite());
        }
    }

    public class CameraFollowTests
    {
        [Test]
        public void ClampAxis_CentersSmallMaps() =>
            Assert.AreEqual(10f, CameraFollow.ClampAxis(3f, 0f, 20f, 15f));

        [Test]
        public void ClampAxis_KeepsViewInsideBigMaps()
        {
            Assert.AreEqual(15f, CameraFollow.ClampAxis(2f, 0f, 40f, 15f));
            Assert.AreEqual(25f, CameraFollow.ClampAxis(39f, 0f, 40f, 15f));
            Assert.AreEqual(20f, CameraFollow.ClampAxis(20f, 0f, 40f, 15f));
        }
    }

    public class DayNightLightingTests
    {
        [Test]
        public void Evaluate_InterpolatesBetweenKeys()
        {
            var keys = new List<DayNightLighting.LightKey>
            {
                new(0f, Color.black, 0f),
                new(12f, Color.white, 1f),
                new(24f, Color.black, 0f),
            };
            DayNightLighting.Evaluate(keys, 6f, out var color, out var intensity);
            Assert.AreEqual(0.5f, intensity, 1e-4f);
            Assert.AreEqual(0.5f, color.r, 1e-4f);
            DayNightLighting.Evaluate(keys, 12f, out _, out intensity);
            Assert.AreEqual(1f, intensity, 1e-4f);
        }
    }

    public class PixelFontTests
    {
        PixelFont _font;

        [SetUp]
        public void SetUp()
        {
            // Fuente falsa: cada letra mide 4 px + 1 de espacio; el espacio, 3 + 1.
            _font = ScriptableObject.CreateInstance<PixelFont>();
            _font.spacing = 1;
            _font.spaceWidth = 3;
            foreach (var c in "abcdefghijklmnopqrstuvwxyz")
                _font.glyphs.Add(new PixelFont.Glyph { character = c, rect = new RectInt(0, 0, 4, 11) });
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_font);

        [Test]
        public void MeasureWidth_IgnoresTrailingSpacing()
        {
            Assert.AreEqual(4, _font.MeasureWidth("a"));
            Assert.AreEqual(9, _font.MeasureWidth("ab"));
            Assert.AreEqual(13, _font.MeasureWidth("a b"));
        }

        [Test]
        public void Wrap_BreaksBetweenWords()
        {
            // "abc abc" = 15 + 4 + 15 - 1 = 33 px
            var lines = _font.Wrap("abc abc abc", 33);
            CollectionAssert.AreEqual(new[] { "abc abc", "abc" }, lines);
        }

        [Test]
        public void Wrap_KeepsExplicitLineBreaks() =>
            CollectionAssert.AreEqual(new[] { "ab", "cd" }, _font.Wrap("ab\ncd", 100));

        [Test]
        public void Wrap_SplitsWordsLongerThanTheLine()
        {
            var lines = _font.Wrap("abcdefgh", 19); // entran 4 letras por renglón
            CollectionAssert.AreEqual(new[] { "abcd", "efgh" }, lines);
        }

        [Test]
        public void Paginate_GroupsLinesInPages()
        {
            var pages = DialogueBox.Paginate(_font, new[] { "abc abc abc abc abc" }, 33, 2);
            CollectionAssert.AreEqual(new[] { "abc abc\nabc abc", "abc" }, pages);
        }
    }
}
