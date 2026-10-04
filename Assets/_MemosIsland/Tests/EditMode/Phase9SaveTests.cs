using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using MemosIsland.Story;
using MemosIsland.World;
using NUnit.Framework;

namespace MemosIsland.Tests
{
    public class SaveSystemTests
    {
        static SaveData Sample()
        {
            var state = new GameState();
            var memo = MemoInstance.Create("tostin", 7, trust: 320);
            memo.nickname = "Tosti";
            memo.rescued = true;
            state.AddMemo(memo);
            state.companionUid = memo.uid;
            state.AddItem("bayamemo", 3);
            state.island.money = 1234;
            state.island.plots.Add(new FarmPlot { x = 5, y = 6, tilled = true, seedId = "semilla_nabo", growth = 2 });
            state.town.Get("lalo").points = 450;
            state.story.active = true;
            state.story.profile = new PlayerProfile { name = "Joa", pronoun = Pronoun.Ella, hairStyle = 1 };
            state.story.Set("arrived");
            state.story.starterUid = memo.uid;
            return new SaveData { scene = "Map_PuebloPuerto", mapName = "Pueblo Puerto", x = 22, y = 3, facing = Direction.Up, playSeconds = 754f, state = state };
        }

        [Test]
        public void RoundTripKeepsTheWholeGame()
        {
            var loaded = SaveSystem.FromJson(SaveSystem.ToJson(Sample()));
            Assert.AreEqual("Map_PuebloPuerto", loaded.scene);
            Assert.AreEqual(22, loaded.x);
            Assert.AreEqual(Direction.Up, loaded.facing);
            var s = loaded.state;
            Assert.AreEqual(1, s.team.Count);
            Assert.AreEqual("Tosti", s.team[0].DisplayName);
            Assert.AreEqual(320, s.team[0].trust);
            Assert.IsTrue(s.team[0].rescued);
            Assert.AreEqual(s.team[0].uid, s.Companion.uid);
            Assert.AreEqual(3, s.CountOf("bayamemo"));
            Assert.AreEqual(1234, s.island.money);
            Assert.AreEqual("semilla_nabo", s.island.PlotAt(5, 6).seedId);
            Assert.AreEqual(450, s.town.Get("lalo").points);
            Assert.AreEqual("Joa", s.story.profile.name);
            Assert.AreEqual(Pronoun.Ella, s.story.profile.pronoun);
            Assert.IsTrue(s.story.Has("arrived"));
        }

        [Test]
        public void EmptyJobsAndDatesComeBackAsNull()
        {
            var loaded = SaveSystem.FromJson(SaveSystem.ToJson(Sample()));
            Assert.IsNull(loaded.state.island.smelter, "sin fundición en curso");
            Assert.IsNull(loaded.state.island.processor);
            Assert.IsNull(loaded.state.island.lastProcessedDate, "si no, IslandDay intentaría leer una fecha vacía");
            Assert.IsNull(loaded.state.lastDailyDate);
        }

        [Test]
        public void RunningJobsSurvive()
        {
            var data = Sample();
            data.state.island.smelter = new SmelterJob { recipeId = "fundir_cobre", count = 2, readyTicks = 123 };
            var loaded = SaveSystem.FromJson(SaveSystem.ToJson(data));
            Assert.AreEqual("fundir_cobre", loaded.state.island.smelter.recipeId);
            Assert.AreEqual(2, loaded.state.island.smelter.count);
        }

        [Test]
        public void BrokenFilesAreRejected()
        {
            Assert.IsNull(SaveSystem.FromJson("{}"));
        }
    }
}
