using System.Linq;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.Race
{
    /// <summary>
    /// Aplica el efecto de una habilidad (datos de AbilityData) sobre la carrera.
    /// Devuelve false si no tiene sentido usarla ahora (por ejemplo, Enredadera sin nadie adelante):
    /// en ese caso la barra no se gasta.
    /// </summary>
    public static class AbilityResolver
    {
        public const float AheadRange = 30f;   // "el rival de adelante"
        public const float NearRange = 15f;    // "rivales cercanos"
        public const float TrailLength = 12f;  // largo de la estela de fuego

        public static bool Apply(RaceSimulation sim, Racer user, AbilityData a, out string failReason)
        {
            failReason = null;
            switch (a.effect)
            {
                case AbilityEffect.SlowFollowers:
                {
                    float length = a.affectsAll ? TrailLength * 1.7f : TrailLength;
                    sim.AddZone(user, user.position - length, user.position, a.duration + 2f,
                        StatusKind.Slow, a.duration, a.strength);
                    if (a.terrain != null)
                    {
                        sim.Track.Override(user.position, a.terrain, 6f);
                    }
                    else if (a.id == "estela_ardiente")
                    {
                        // Derrite el hielo y la nieve: el tramo pasa a ser Río un rato.
                        var current = sim.Track.TerrainAt(user.position);
                        if (current != null && (current.id == "hielo" || current.id == "nieve"))
                            sim.Track.Override(user.position, TerrainById(sim, "rio") ?? current, 6f);
                    }
                    return true;
                }

                case AbilityEffect.Root:
                {
                    var target = sim.NearestAhead(user, AheadRange);
                    if (target == null)
                    {
                        failReason = "¡No hay nadie adelante!";
                        return false;
                    }
                    target.AddEffect(StatusKind.Root, a.duration);
                    if (a.strength > 0f) Recover(user, a.strength);
                    return true;
                }

                case AbilityEffect.ChangeTerrain:
                {
                    if (a.terrain == null) return false;
                    sim.Track.Override(user.position, a.terrain, a.duration);
                    if (a.archetype == AbilityArchetype.HinderBehind)
                    {
                        // Llamarada negra: además frena a todos los que vienen atrás.
                        sim.AddZone(user, user.position - TrailLength * 1.7f, user.position, a.duration,
                            StatusKind.Slow, 2f, a.strength > 0f ? a.strength : 0.6f);
                    }
                    else if (a.affectsAll)
                    {
                        foreach (var r in sim.Rivals(user).Where(r => !r.IsHidden && Mathf.Abs(r.position - user.position) <= NearRange))
                        {
                            r.position = Mathf.Max(0f, r.position - 3f);
                            r.speed *= a.strength > 0f ? a.strength : 0.5f;
                        }
                    }
                    return true;
                }

                case AbilityEffect.Stun:
                {
                    user.speed += 2f;
                    user.AddEffect(StatusKind.Sprint, 1f, a.strength > 0f ? a.strength : 1.4f);
                    foreach (var r in sim.Rivals(user).Where(r => !r.IsHidden && Mathf.Abs(r.position - user.position) <= NearRange))
                        r.AddEffect(StatusKind.Stun, a.duration);
                    return true;
                }

                case AbilityEffect.Sprint:
                    user.AddEffect(StatusKind.Sprint, a.duration, a.strength);
                    user.AddEffect(StatusKind.Exhausted, 3f, 0.7f, delay: a.duration);
                    return true;

                case AbilityEffect.Teleport:
                    user.AddEffect(StatusKind.Teleport, a.duration, a.strength);
                    return true;

                case AbilityEffect.Zigzag:
                {
                    var target = sim.Nearest(user, AheadRange);
                    if (target == null)
                    {
                        failReason = "¡No hay nadie cerca!";
                        return false;
                    }
                    target.AddEffect(StatusKind.Zigzag, a.duration, a.strength);
                    return true;
                }

                case AbilityEffect.Sleep:
                {
                    var target = sim.NearestAhead(user, AheadRange);
                    if (target == null)
                    {
                        failReason = "¡No hay nadie adelante!";
                        return false;
                    }
                    target.AddEffect(StatusKind.Sleep, a.duration);
                    return true;
                }

                case AbilityEffect.Blind:
                    foreach (var r in sim.Rivals(user).Where(r => !r.IsHidden))
                        r.AddEffect(StatusKind.Blind, a.duration, a.strength);
                    return true;

                case AbilityEffect.Recover:
                    if (user.Active.EnergyRatio > 0.95f)
                    {
                        failReason = "¡Ya tiene la energía llena!";
                        return false;
                    }
                    Recover(user, a.strength);
                    return true;

                case AbilityEffect.Copy:
                {
                    var copied = sim.LastAbilityByRivals(user);
                    if (copied == null)
                    {
                        // Nada para copiar: al menos acelera un poco.
                        user.AddEffect(StatusKind.Sprint, 2f, 1.2f);
                        return true;
                    }
                    return Apply(sim, user, copied, out failReason);
                }

                case AbilityEffect.Magnet:
                {
                    var targets = sim.Rivals(user).Where(r => !r.IsHidden && Mathf.Abs(r.position - user.position) <= NearRange * 1.4f).ToList();
                    if (targets.Count == 0)
                    {
                        failReason = "¡No hay nadie cerca!";
                        return false;
                    }
                    foreach (var r in targets) r.AddEffect(StatusKind.Magnet, a.duration, a.strength);
                    user.AddEffect(StatusKind.Sprint, a.duration, 1.15f);
                    return true;
                }

                case AbilityEffect.TeamBoost:
                    if (user.memos.Count < 2)
                    {
                        user.AddEffect(StatusKind.Sprint, 2f, 1.25f);
                        return true;
                    }
                    user.teamBoostPending = true;
                    return true;
            }
            return false;
        }

        static void Recover(Racer user, float fraction)
        {
            var m = user.Active;
            m.energy = Mathf.Min(m.maxEnergy, m.energy + m.maxEnergy * fraction);
        }

        static RaceTerrain TerrainById(RaceSimulation sim, string id) =>
            sim.Chart != null ? sim.Chart.terrains.FirstOrDefault(t => t != null && t.id == id) : null;
    }
}
