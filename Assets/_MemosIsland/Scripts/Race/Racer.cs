using System.Collections.Generic;
using System.Linq;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.Race
{
    public enum StatusKind { Slow, Root, Stun, Sleep, Blind, Zigzag, Magnet, Sprint, Exhausted, Teleport, Switching }

    /// <summary>Un efecto activo sobre un corredor (frenado, dormido, sprint…).</summary>
    public class StatusEffect
    {
        public StatusKind kind;
        public float time;      // segundos que quedan
        public float delay;     // segundos antes de empezar (ej.: el agotamiento después de Descarga)
        public float strength;  // multiplicador de velocidad cuando corresponde
        public bool Active => delay <= 0f && time > 0f;
    }

    /// <summary>Un Memo dentro de la carrera, con su energía y su barra de habilidad.</summary>
    public class RacerMemo
    {
        public readonly MemoInstance instance;
        public readonly MemoSpecies species;
        public readonly ComputedStats stats;
        public readonly float maxEnergy;
        public float energy;
        public float charge; // 0..1
        public bool hasRun;

        public RacerMemo(MemoInstance instance)
        {
            this.instance = instance;
            species = instance.Species;
            stats = instance.Stats;
            maxEnergy = Mathf.Max(10f, stats.Stamina * 10f);
            energy = maxEnergy;
        }

        public bool AbilityReady => charge >= 1f;
        public float EnergyRatio => energy / maxEnergy;
        public TemperamentTrait Trait => instance.Temperament != null ? instance.Temperament.trait : TemperamentTrait.None;

        /// <summary>Velocidad base en unidades por segundo (sin terreno ni efectos).</summary>
        public float BaseSpeed => 3f + stats.Speed * 0.6f;
        public float Acceleration => 2f + stats.Acceleration * 0.8f;
        public float ChargePerSecond => (4f + stats.Charge) / 100f;
    }

    /// <summary>Un corredor de la carrera (equipo + posición en la pista).</summary>
    public class Racer
    {
        public readonly string name;
        public readonly bool isPlayer, isWild;
        public readonly int lane;
        public readonly List<RacerMemo> memos;
        public int activeIndex;
        public float position, speed;
        public float switchCooldown;
        public readonly List<StatusEffect> effects = new();
        public bool finished, gaveUp;
        public float finishTime;
        public int rank;
        public bool teamBoostPending;
        public int pendingSwitch = -1;
        public float pendingSwitchDelay;
        public float zeroEnergyTime;

        public Racer(string name, IEnumerable<MemoInstance> team, int lane, bool isPlayer, bool isWild)
        {
            this.name = name;
            this.lane = lane;
            this.isPlayer = isPlayer;
            this.isWild = isWild;
            memos = team.Select(m => new RacerMemo(m)).ToList();
            memos[0].hasRun = true;
        }

        public RacerMemo Active => memos[activeIndex];
        public bool Has(StatusKind kind) => effects.Any(e => e.kind == kind && e.Active);
        public bool IsStopped => Has(StatusKind.Root) || Has(StatusKind.Stun) || Has(StatusKind.Sleep);
        public bool IsHidden => Has(StatusKind.Teleport);
        public bool AllMemosRan => memos.All(m => m.hasRun);

        public void AddEffect(StatusKind kind, float seconds, float strength = 1f, float delay = 0f)
        {
            if (seconds <= 0f) return;
            var existing = effects.Find(e => e.kind == kind && e.delay <= 0f);
            if (existing != null && delay <= 0f)
            {
                existing.time = Mathf.Max(existing.time, seconds);
                existing.strength = strength;
                return;
            }
            effects.Add(new StatusEffect { kind = kind, time = seconds, strength = strength, delay = delay });
        }

        public void TickEffects(float dt)
        {
            foreach (var e in effects)
            {
                if (e.delay > 0f) e.delay -= dt;
                else e.time -= dt;
            }
            effects.RemoveAll(e => e.delay <= 0f && e.time <= 0f);
        }
    }
}
