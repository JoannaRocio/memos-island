using System;
using MemosIsland.Story;
using NUnit.Framework;

namespace MemosIsland.Tests
{
    public class CupDeskTests
    {
        static readonly DateTime Saturday = new(2026, 10, 3, 12, 0, 0);

        [Test]
        public void OnlySaturdaysDuringTheDay()
        {
            Assert.IsNull(CupDesk.Blocker(Saturday, 3, "", true, false));
            Assert.IsNotNull(CupDesk.Blocker(Saturday.AddDays(1), 3, "", true, false), "domingo no");
            Assert.IsNotNull(CupDesk.Blocker(Saturday.AddHours(8), 3, "", true, false), "a las 20 ya cerró");
        }

        [Test]
        public void NeedsAFullRelayTeamAndOneTryPerSaturday()
        {
            Assert.IsNotNull(CupDesk.Blocker(Saturday, 2, "", true, false));
            Assert.IsNotNull(CupDesk.Blocker(Saturday, 3, "2026-10-03", true, false), "si perdiste hoy, el sábado que viene");
            Assert.IsNull(CupDesk.Blocker(Saturday.AddDays(7), 3, "2026-10-03", true, false));
        }

        [Test]
        public void TestGameCanRaceAnyDayAndWinnersAreDone()
        {
            Assert.IsNull(CupDesk.Blocker(Saturday.AddDays(2), 3, "", false, false));
            Assert.IsNotNull(CupDesk.Blocker(Saturday, 3, "", true, true));
        }
    }
}
