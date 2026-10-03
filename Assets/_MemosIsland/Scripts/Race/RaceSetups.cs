using System.Collections.Generic;
using System.Linq;
using MemosIsland.Memos;

namespace MemosIsland.Race
{
    /// <summary>Carreras armadas de antemano (Copa de prueba, rivales de ejemplo).</summary>
    public static class RaceSetups
    {
        public static RacerSetup Rival(string name, params (string species, int level, bool collared)[] members)
        {
            var rng = new System.Random(name.GetHashCode());
            return new RacerSetup
            {
                name = name,
                team = members.Select(m =>
                {
                    var memo = MemoInstance.CreateRandom(m.species, m.level, rng);
                    memo.shiny = false;
                    memo.collared = m.collared;
                    // Con collar obedecen sin voluntad: no desobedecen, pero tampoco tienen el bonus del vínculo.
                    memo.trust = m.collared ? 250 : 450;
                    return memo;
                }).ToList(),
            };
        }

        /// <summary>Copa de la Isla (prueba): 6 tramos contra Lalo, un agente de Ápice y la Capitana Vera.</summary>
        public static RaceSetup Cup(IEnumerable<MemoInstance> playerTeam) => new()
        {
            format = RaceFormat.Cup,
            title = "Copa de la Isla (prueba)",
            segments =
            {
                new RaceSegmentDef("pradera", 70), new RaceSegmentDef("barro", 60), new RaceSegmentDef("rio", 60),
                new RaceSegmentDef("hielo", 60), new RaceSegmentDef("montana", 60), new RaceSegmentDef("arena", 70),
            },
            player = new RacerSetup { name = "Vos", team = playerTeam.Take(6).ToList() },
            rivals =
            {
                Rival("Lalo", ("plumin", 6, false), ("chispin", 5, false), ("topin", 5, false)),
                Rival("Agente de Ápice", ("copito", 6, true), ("zumbi", 6, true), ("bostezo", 6, true)),
                Rival("Capitana Vera", ("tuerquita", 7, false), ("ferrolobo", 7, false), ("imanta", 7, false)),
            },
        };
    }
}
