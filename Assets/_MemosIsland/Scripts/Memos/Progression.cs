using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>
    /// Experiencia, niveles, evolución y etapa rebelde (GDD §13).
    /// </summary>
    public static class Progression
    {
        public const int MaxLevel = 30;
        public const int RebelDays = 7;
        public const float RebelMinDisobey = 0.12f;

        /// <summary>Experiencia necesaria para pasar del nivel actual al siguiente.</summary>
        public static int XpToNext(int level) => 20 + level * 10;

        /// <summary>Experiencia de una carrera para cada Memo que corrió.</summary>
        public static int RaceXp(MemosIsland.Race.RaceFormat format, bool won, bool fullTeam)
        {
            float xp = format switch
            {
                MemosIsland.Race.RaceFormat.Capture => 20,
                MemosIsland.Race.RaceFormat.Friendly => 15,
                MemosIsland.Race.RaceFormat.Trainer => 30,
                _ => 60,
            };
            if (won) xp *= 1.5f;
            if (fullTeam) xp *= 1.5f; // bonus "Equipo completo" (GDD §9)
            return Mathf.RoundToInt(xp);
        }

        /// <summary>Suma experiencia. Devuelve los niveles que subió (vacío si ninguno).</summary>
        public static List<int> AddXp(MemoInstance m, int xp)
        {
            var levels = new List<int>();
            if (m.level >= MaxLevel || xp <= 0) return levels;
            m.xp += xp;
            while (m.level < MaxLevel && m.xp >= XpToNext(m.level))
            {
                m.xp -= XpToNext(m.level);
                m.level++;
                levels.Add(m.level);
            }
            if (m.level >= MaxLevel) m.xp = 0;
            return levels;
        }

        /// <summary>¿Le toca evolucionar? (por nivel, o por confianza al llegar a Alma gemela).</summary>
        public static MemoSpecies EvolutionTarget(MemoInstance m)
        {
            var s = m.Species;
            if (s == null || s.evolvesTo == null || m.cancelledEvolutionAtLevel == m.level) return null;
            if (s.evolutionLevel > 0 && m.level >= s.evolutionLevel) return s.evolvesTo;
            if (s.evolvesWithTrust && m.highestTrust >= TrustLevel.Soulmate) return s.evolvesTo;
            return null;
        }

        /// <summary>
        /// Evoluciona y empieza la etapa rebelde: pierde un nivel de confianza (sin bajar de Neutral)
        /// y desobedece más durante 7 días reales.
        /// </summary>
        public static void Evolve(MemoInstance m, MemoSpecies into, DateTime now)
        {
            m.speciesId = into.id;
            var level = m.TrustLevel;
            m.rebelFrom = level;
            m.grewTogether = false;
            if (level > TrustLevel.Neutral) m.trust = TrustRules.MinPointsFor(level - 1);
            m.rebelUntilTicks = now.AddDays(RebelDays).Ticks;
        }

        public static bool IsRebel(MemoInstance m, DateTime now) => m.rebelUntilTicks > now.Ticks;

        /// <summary>
        /// Si durante la etapa rebelde recupera el nivel de confianza que tenía, termina la rebeldía
        /// y gana el recuerdo "crecimos juntos". Devuelve true en ese momento.
        /// </summary>
        public static bool CheckRebelRecovery(MemoInstance m, DateTime now)
        {
            if (m.rebelUntilTicks == 0) return false;
            bool lostALevel = m.rebelFrom > TrustLevel.Neutral;
            // Perdió un nivel: termina cuando lo recupera. No perdió: termina cuando pasan los 7 días.
            bool recovered = lostALevel ? m.TrustLevel >= m.rebelFrom : now.Ticks >= m.rebelUntilTicks;
            if (recovered)
            {
                m.rebelUntilTicks = 0;
                m.grewTogether = true;
                return true;
            }
            if (now.Ticks >= m.rebelUntilTicks) m.rebelUntilTicks = 0; // se le pasó sin recuperarla
            return false;
        }

        public static float DisobeyChance(MemoInstance m, DateTime now)
        {
            float chance = TrustRules.DisobeyChance(m.TrustLevel);
            return IsRebel(m, now) ? Mathf.Max(chance, RebelMinDisobey) : chance;
        }
    }
}
