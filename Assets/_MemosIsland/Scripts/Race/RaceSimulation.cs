using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.Race
{
    /// <summary>
    /// Simulación de una carrera (GDD §9), sin nada visual: se puede testear y es determinista con una semilla.
    /// La vista (RaceController/RaceView) solo la lee y le manda órdenes del jugador.
    /// </summary>
    public class RaceSimulation
    {
        public const float SwitchCooldown = 8f;
        public const float SwitchTransition = 0.5f;
        public const float FixedStep = 1f / 60f;
        const float DrainPerSecond = 1f, WildDrainPerSecond = 1.6f, BenchRegenPerSecond = 2f;
        const float GiveUpAfterSeconds = 2f;

        /// <summary>Una franja de pista que molesta a quien pasa (estela de fuego, llamarada…).</summary>
        public class Zone
        {
            public Racer owner;
            public float start, end, life;
            public StatusKind effect;
            public float effectSeconds, strength;
        }

        public readonly RaceTrack Track;
        public readonly List<Racer> Racers = new();
        public readonly List<Zone> Zones = new();
        public readonly EffectivenessChart Chart;
        public readonly bool IsCapture;
        public float Time { get; private set; }
        public bool IsOver { get; private set; }
        public Racer Player => Racers[0];

        /// <summary>Se usó una habilidad (corredor, habilidad, mensaje para mostrar).</summary>
        public event Action<Racer, AbilityData> AbilityUsed;
        public event Action<Racer> Switched;
        /// <summary>Relevo entre mejores amigos: el que entra arranca a toda velocidad (GDD §12).</summary>
        public event Action<Racer> FriendshipBoost;

        /// <summary>Dice si dos Memos del mismo equipo son mejores amigos (lo arma quien crea la carrera).</summary>
        public Func<RacerMemo, RacerMemo, bool> AreBestFriends;
        /// <summary>Mensajes para el jugador ("¡Nadie adelante!", "¡No te hace caso!").</summary>
        public event Action<Racer, string> Message;

        readonly System.Random _rng;
        readonly Dictionary<Racer, AbilityData> _lastAbility = new();
        float _accumulator;
        int _finishedCount;

        public RaceSimulation(RaceTrack track, EffectivenessChart chart, RacerSetup player, IEnumerable<RacerSetup> rivals,
            bool isCapture, int seed)
        {
            Track = track;
            Chart = chart;
            IsCapture = isCapture;
            _rng = new System.Random(seed);
            Racers.Add(new Racer(player.name, player.team, 0, true, false));
            int lane = 1;
            foreach (var r in rivals)
                Racers.Add(new Racer(r.name, r.team, lane++, false, r.isWild));
            foreach (var r in Racers)
                r.Blocked += x => Message?.Invoke(x, $"¡El Cascabel de calma protegió a {x.Active.instance.DisplayName}!");
        }

        public float NextRandom() => (float)_rng.NextDouble();

        // ------------------------------------------------------------------ Paso de simulación

        /// <summary>Avanza la simulación usando pasos fijos (resultado igual sin importar los FPS).</summary>
        public void Advance(float dt)
        {
            _accumulator += dt;
            while (_accumulator >= FixedStep && !IsOver)
            {
                Step(FixedStep);
                _accumulator -= FixedStep;
            }
        }

        public void Step(float dt)
        {
            if (IsOver) return;
            Time += dt;
            Track.Tick(dt);

            foreach (var z in Zones) z.life -= dt;
            Zones.RemoveAll(z => z.life <= 0f);

            Racer last = Racers.Where(r => !r.finished).OrderBy(r => r.position).FirstOrDefault();

            foreach (var r in Racers)
            {
                if (r.finished) continue;
                r.TickEffects(dt);
                if (r.switchCooldown > 0f) r.switchCooldown -= dt;

                if (r.pendingSwitch >= 0)
                {
                    r.pendingSwitchDelay -= dt;
                    if (r.pendingSwitchDelay <= 0f) DoSwitch(r, r.pendingSwitch);
                }

                ApplyZones(r);
                UpdateMovement(r, dt, r == last && Racers.Count > 1);
                UpdateEnergyAndCharge(r, dt);
                MaybeMisbehave(r, dt);

                if (r.position >= Track.Length) Finish(r);
            }

            CheckEnd();
        }

        public float TargetSpeed(Racer r, bool isLast = false)
        {
            if (r.IsStopped) return 0f;
            var m = r.Active;
            float speed = m.BaseSpeed;
            speed *= EffectivenessChart.Multiplier(EffectivenessFor(m, Track.TerrainAt(r.position)));
            speed *= EnergyFactor(m);
            speed *= TrustRules.SpeedFactor(m.instance.TrustLevel);
            speed *= TrustRules.MoodFactor(m.instance.mood);
            if (isLast && m.Trait == TemperamentTrait.Comeback) speed *= 1.1f;

            foreach (var e in r.effects)
            {
                if (!e.Active) continue;
                switch (e.kind)
                {
                    case StatusKind.Slow:
                    case StatusKind.Zigzag:
                    case StatusKind.Blind:
                    case StatusKind.Magnet:
                    case StatusKind.Sprint:
                    case StatusKind.Teleport:
                        speed *= e.strength;
                        break;
                    case StatusKind.Exhausted:
                        speed *= 0.7f;
                        break;
                    case StatusKind.Switching:
                        speed *= 0.5f;
                        break;
                }
            }
            return speed;
        }

        public Effectiveness EffectivenessFor(RacerMemo m, RaceTerrain terrain)
        {
            var e = Chart != null && m.species != null
                ? Chart.Get(m.species.primaryType, m.species.secondaryType, terrain)
                : Effectiveness.Normal;
            return EquipmentRules.Adjust(e, m.equipment, m.amulet, terrain);
        }

        public static float EnergyFactor(RacerMemo m)
        {
            if (m.Trait == TemperamentTrait.SteadyPace) return 1f;
            if (m.energy <= 0f) return 0.6f;
            return m.EnergyRatio < 0.25f ? 0.8f : 1f;
        }

        void UpdateMovement(Racer r, float dt, bool isLast)
        {
            float target = TargetSpeed(r, isLast);
            if (r.speed < target) r.speed = Mathf.Min(target, r.speed + r.Active.Acceleration * dt);
            else r.speed = Mathf.Max(target, r.speed - (r.IsStopped ? 25f : 10f) * dt);
            r.position += r.speed * dt;
        }

        void UpdateEnergyAndCharge(Racer r, float dt)
        {
            for (int i = 0; i < r.memos.Count; i++)
            {
                var m = r.memos[i];
                if (i == r.activeIndex)
                {
                    float drain = r.isWild && IsCapture ? WildDrainPerSecond : DrainPerSecond;
                    if (r.Has(StatusKind.Sprint)) drain += 1.5f;
                    m.energy = Mathf.Max(0f, m.energy - drain * dt);
                    if (!r.IsStopped) m.charge = Mathf.Min(1f, m.charge + m.ChargePerSecond * dt);
                }
                else
                {
                    m.energy = Mathf.Min(m.maxEnergy, m.energy + BenchRegenPerSecond * dt);
                }
            }

            // Captura: el salvaje se rinde si se queda sin energía un rato.
            if (r.isWild && IsCapture)
            {
                r.zeroEnergyTime = r.Active.energy <= 0f ? r.zeroEnergyTime + dt : 0f;
                if (r.zeroEnergyTime >= GiveUpAfterSeconds)
                {
                    r.gaveUp = true;
                    r.finished = true;
                    r.finishTime = float.MaxValue;
                    Message?.Invoke(r, $"¡{r.name} está agotado y se rinde!");
                }
            }
        }

        /// <summary>Un Memo con poca confianza a veces hace lo que quiere (GDD §9).</summary>
        void MaybeMisbehave(Racer r, float dt)
        {
            float chance = r.Active.disobeyChance;
            if (chance <= 0f) return;
            if (NextRandom() < chance * 0.1f * dt)
            {
                r.AddEffect(StatusKind.Slow, 1f, 0.6f);
                if (r.isPlayer) Message?.Invoke(r, $"¡{r.Active.instance.DisplayName} se frena a propósito!");
            }
            if (r.Active.AbilityReady && NextRandom() < chance * 0.3f * dt)
                TryUseAbility(r, ignoreDisobedience: true);
        }

        void ApplyZones(Racer r)
        {
            if (r.IsHidden) return;
            foreach (var z in Zones)
                if (z.owner != r && r.position >= z.start && r.position <= z.end)
                    r.AddEffect(z.effect, z.effectSeconds, z.strength);
        }

        void Finish(Racer r)
        {
            r.position = Track.Length;
            r.finished = true;
            r.finishTime = Time;
            r.rank = ++_finishedCount;
        }

        void CheckEnd()
        {
            if (IsCapture)
            {
                if (Racers.Any(r => r.finished)) EndRace();
                return;
            }
            if (Player.finished || Racers.All(r => r.finished)) EndRace();
        }

        void EndRace()
        {
            IsOver = true;
            // Los que no llegaron se ordenan por distancia recorrida.
            var order = Racers.OrderBy(r => r.gaveUp ? 1 : 0)
                .ThenBy(r => r.finished && !r.gaveUp ? r.finishTime : float.MaxValue)
                .ThenByDescending(r => r.position).ToList();
            for (int i = 0; i < order.Count; i++) order[i].rank = i + 1;
        }

        /// <summary>En captura: ¿ganó el jugador (llegó primero o el salvaje se rindió)?</summary>
        public bool PlayerWon => IsOver && Player.rank == 1;

        // ------------------------------------------------------------------ Órdenes

        public bool CanSwitch(Racer r, int index) =>
            !IsOver && !r.finished && index >= 0 && index < r.memos.Count && index != r.activeIndex
            && r.switchCooldown <= 0f && r.pendingSwitch < 0;

        /// <summary>Pide cambiar de Memo. Si el que corre no obedece, el cambio se demora 2 segundos.</summary>
        public bool RequestSwitch(Racer r, int index)
        {
            if (!CanSwitch(r, index)) return false;
            if (NextRandom() < r.Active.disobeyChance)
            {
                r.pendingSwitch = index;
                r.pendingSwitchDelay = 2f;
                r.switchCooldown = SwitchCooldown;
                Message?.Invoke(r, $"¡{r.Active.instance.DisplayName} no quiere salir!");
                return true;
            }
            DoSwitch(r, index);
            return true;
        }

        void DoSwitch(Racer r, int index)
        {
            r.pendingSwitch = -1;
            var previous = r.Active;
            r.activeIndex = index;
            r.Active.hasRun = true;
            r.switchCooldown = SwitchCooldown;
            bool friends = AreBestFriends != null && AreBestFriends(previous, r.Active);
            if (friends)
            {
                r.teamBoostPending = false;
                r.speed = TargetSpeed(r) * 1.1f;
                r.AddEffect(StatusKind.Sprint, 1.5f, 1.15f);
                FriendshipBoost?.Invoke(r);
            }
            else if (r.teamBoostPending || EquipmentRules.Has(r.Active.amulet, AmuletEffect.Relay))
            {
                // Ráfaga o Amuleto de relevo: entra a toda velocidad.
                r.teamBoostPending = false;
                r.speed = TargetSpeed(r) * 1.1f;
            }
            else
            {
                r.speed *= 0.6f;
                r.AddEffect(StatusKind.Switching, SwitchTransition);
            }
            Switched?.Invoke(r);
        }

        public bool TryUseAbility(Racer r, bool ignoreDisobedience = false)
        {
            if (IsOver || r.finished || !r.Active.AbilityReady || r.IsStopped) return false;
            var ability = r.Active.species.ability;
            if (ability == null) return false;

            if (!ignoreDisobedience && NextRandom() < r.Active.disobeyChance)
            {
                Message?.Invoke(r, $"¡{r.Active.instance.DisplayName} no te hace caso!");
                return false;
            }

            if (!AbilityResolver.Apply(this, r, ability, out var failReason))
            {
                if (failReason != null) Message?.Invoke(r, failReason);
                return false;
            }
            r.Active.charge = 0f;
            _lastAbility[r] = ability;
            AbilityUsed?.Invoke(r, ability);
            return true;
        }

        // ------------------------------------------------------------------ Ayudas para habilidades e IA

        public IEnumerable<Racer> Rivals(Racer of) => Racers.Where(r => r != of && !r.finished);

        public Racer NearestAhead(Racer of, float range) => Rivals(of)
            .Where(r => !r.IsHidden && r.position > of.position && r.position - of.position <= range)
            .OrderBy(r => r.position - of.position).FirstOrDefault();

        public Racer NearestBehind(Racer of, float range) => Rivals(of)
            .Where(r => !r.IsHidden && r.position <= of.position && of.position - r.position <= range)
            .OrderBy(r => of.position - r.position).FirstOrDefault();

        public Racer Nearest(Racer of, float range) => Rivals(of)
            .Where(r => !r.IsHidden && Mathf.Abs(r.position - of.position) <= range)
            .OrderBy(r => Mathf.Abs(r.position - of.position)).FirstOrDefault();

        public AbilityData LastAbilityByRivals(Racer of) =>
            _lastAbility.Where(kv => kv.Key != of).Select(kv => kv.Value)
                .FirstOrDefault(a => a.effect != AbilityEffect.Copy);

        public void AddZone(Racer owner, float start, float end, float life, StatusKind effect, float seconds, float strength)
        {
            Zones.Add(new Zone
            {
                owner = owner, start = start, end = end, life = life,
                effect = effect, effectSeconds = seconds, strength = strength,
            });
        }
    }
}
