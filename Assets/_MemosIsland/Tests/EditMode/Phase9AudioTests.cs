using MemosIsland.Core;
using NUnit.Framework;

namespace MemosIsland.Tests
{
    public class AudioManagerTests
    {
        [Test]
        public void EachPlaceHasItsMusic()
        {
            Assert.AreEqual("cajita", AudioManager.TrackFor("Title", false));
            Assert.AreEqual("pueblo", AudioManager.TrackFor("Map_PuebloPuerto", false));
            Assert.AreEqual("pueblo", AudioManager.TrackFor("Map_Taberna", false), "los locales del pueblo usan su tema");
            Assert.AreEqual("refugio", AudioManager.TrackFor("Map_RefugioInterior", false));
            Assert.AreEqual("copa", AudioManager.TrackFor("Map_Estadio", false));
            Assert.AreEqual("ruta", AudioManager.TrackFor("Map_BosqueSusurro", false), "las zonas salvajes usan el de las rutas");
        }

        [Test]
        public void RacesUseTheCupThemeOnlyInTheCup()
        {
            Assert.AreEqual("carrera", AudioManager.TrackFor("Race", false));
            Assert.AreEqual("copa", AudioManager.TrackFor("Race", true));
        }
    }
}
