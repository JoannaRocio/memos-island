using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MemosIsland.Farm
{
    public enum RockKind { Stone, Copper, Iron, Quartz, Gem }

    /// <summary>Cueva de Zorak (GDD §15): 5 pisos con rocas que se renuevan cada día real.</summary>
    public static class Mining
    {
        public const int Floors = 5;

        static readonly Dictionary<int, (RockKind kind, int weight)[]> Table = new()
        {
            [1] = new[] { (RockKind.Stone, 70), (RockKind.Copper, 30) },
            [2] = new[] { (RockKind.Stone, 50), (RockKind.Copper, 35), (RockKind.Quartz, 15) },
            [3] = new[] { (RockKind.Stone, 40), (RockKind.Copper, 25), (RockKind.Iron, 25), (RockKind.Quartz, 10) },
            [4] = new[] { (RockKind.Stone, 30), (RockKind.Copper, 15), (RockKind.Iron, 35), (RockKind.Quartz, 15), (RockKind.Gem, 5) },
            [5] = new[] { (RockKind.Stone, 25), (RockKind.Iron, 35), (RockKind.Quartz, 20), (RockKind.Gem, 20) },
        };

        /// <summary>Nivel de pico necesario: hierro y cuarzo piden pico de cobre; las gemas, de hierro.</summary>
        public static int RequiredPick(RockKind kind) => kind switch
        {
            RockKind.Iron or RockKind.Quartz => 1,
            RockKind.Gem => 2,
            _ => 0,
        };

        public static string KindName(RockKind kind) => kind switch
        {
            RockKind.Copper => "una roca con cobre",
            RockKind.Iron => "una roca con hierro",
            RockKind.Quartz => "una roca con cuarzo",
            RockKind.Gem => "una roca con algo brillante",
            _ => "una roca",
        };

        public static int Seed(string date, int floor)
        {
            int h = 7;
            foreach (var c in date + "#" + floor) h = (h * 31 + c) & 0x7fffffff;
            return h;
        }

        /// <summary>Rocas del día: siempre las mismas para esa fecha y ese piso.</summary>
        public static List<(Vector2Int cell, RockKind kind)> GenerateRocks(string date, int floor, IList<Vector2Int> candidates, int count)
        {
            var rng = new System.Random(Seed(date, floor));
            var shuffled = candidates.OrderBy(_ => rng.Next()).Take(count).ToList();
            var table = Table[Mathf.Clamp(floor, 1, Floors)];
            int total = table.Sum(t => t.weight);
            var result = new List<(Vector2Int, RockKind)>();
            foreach (var cell in shuffled)
            {
                int roll = rng.Next(total);
                var kind = table[0].kind;
                foreach (var (k, w) in table)
                {
                    if (roll < w) { kind = k; break; }
                    roll -= w;
                }
                result.Add((cell, kind));
            }
            return result;
        }

        /// <summary>Lo que da una roca. Con un Memo excavador en el equipo o de compañero, da uno más.</summary>
        public static List<(string id, int count)> Drops(RockKind kind, bool digger, System.Random rng)
        {
            var drops = new List<(string, int)>();
            int bonus = digger ? 1 : 0;
            switch (kind)
            {
                case RockKind.Stone: drops.Add(("piedra", 1 + rng.Next(2) + bonus)); break;
                case RockKind.Copper: drops.Add(("mineral_cobre", 1 + rng.Next(2) + bonus)); break;
                case RockKind.Iron: drops.Add(("mineral_hierro", 1 + rng.Next(2) + bonus)); break;
                case RockKind.Quartz: drops.Add(("cuarzo", 1 + bonus)); break;
                default:
                    string[] gems = { "amatista", "topacio", "esmeralda" };
                    drops.Add((gems[rng.Next(gems.Length)], 1 + bonus));
                    break;
            }
            if (kind != RockKind.Stone && rng.Next(3) == 0) drops.Add(("piedra", 1));
            return drops;
        }

        public static string RockKey(int floor, Vector2Int cell) => $"{floor}:{cell.x}:{cell.y}";
    }
}
