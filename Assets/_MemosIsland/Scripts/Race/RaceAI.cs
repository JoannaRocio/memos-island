using System.Linq;
using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.Race
{
    /// <summary>
    /// IA de rivales (GDD §9): cambia de Memo cuando el terreno le conviene a otro o cuando se cansa,
    /// y usa la habilidad cuando tiene sentido. Piensa cada medio segundo; "skill" agrega errores a propósito.
    /// </summary>
    public class RaceAI
    {
        readonly RaceSimulation _sim;
        readonly Racer _racer;
        readonly float _skill; // 0 = torpe, 1 = jefe
        float _thinkTimer;

        public RaceAI(RaceSimulation sim, Racer racer, float skill)
        {
            _sim = sim;
            _racer = racer;
            _skill = Mathf.Clamp01(skill);
        }

        public void Tick(float dt)
        {
            if (_racer.finished || _sim.IsOver) return;
            _thinkTimer -= dt;
            if (_thinkTimer > 0f) return;
            _thinkTimer = Mathf.Lerp(1.2f, 0.4f, _skill);

            // Los rivales menos hábiles a veces "se distraen".
            if (_sim.NextRandom() > 0.5f + _skill * 0.5f) return;

            ThinkSwitch();
            ThinkAbility();
        }

        void ThinkSwitch()
        {
            if (_racer.memos.Count < 2 || _racer.switchCooldown > 0f || _racer.pendingSwitch >= 0) return;
            var terrain = _sim.Track.DistanceToNextSegment(_racer.position) < 8f
                ? _sim.Track.NextTerrain(_racer.position)
                : _sim.Track.TerrainAt(_racer.position);

            float current = Score(_racer.Active, terrain);
            int best = _racer.activeIndex;
            float bestScore = current;
            for (int i = 0; i < _racer.memos.Count; i++)
            {
                var m = _racer.memos[i];
                if (i == _racer.activeIndex || m.EnergyRatio < 0.4f) continue;
                float s = Score(m, terrain);
                if (s > bestScore) { bestScore = s; best = i; }
            }

            bool tired = _racer.Active.EnergyRatio < 0.3f;
            if (best != _racer.activeIndex && (tired || bestScore > current * 1.12f))
                _sim.RequestSwitch(_racer, best);
        }

        float Score(RacerMemo m, RaceTerrain terrain) =>
            m.BaseSpeed * EffectivenessChart.Multiplier(_sim.EffectivenessFor(m, terrain)) * RaceSimulation.EnergyFactor(m);

        void ThinkAbility()
        {
            var m = _racer.Active;
            if (!m.AbilityReady || m.species.ability == null) return;
            var a = m.species.ability;

            bool useful = a.effect switch
            {
                AbilityEffect.Root or AbilityEffect.Sleep => _sim.NearestAhead(_racer, AbilityResolver.AheadRange) != null,
                AbilityEffect.Zigzag or AbilityEffect.Magnet => _sim.Nearest(_racer, AbilityResolver.AheadRange) != null,
                AbilityEffect.Stun => _sim.Nearest(_racer, AbilityResolver.NearRange) != null,
                AbilityEffect.SlowFollowers => _sim.NearestBehind(_racer, 15f) != null,
                AbilityEffect.Recover => m.EnergyRatio < 0.5f,
                AbilityEffect.ChangeTerrain => TerrainHelps(a),
                AbilityEffect.TeamBoost => _racer.switchCooldown <= 0f,
                _ => true,
            };
            if (useful) _sim.TryUseAbility(_racer);
        }

        /// <summary>¿El terreno nuevo le conviene más (o le molesta a los demás)?</summary>
        bool TerrainHelps(AbilityData a)
        {
            if (a.terrain == null) return false;
            var now = _sim.Track.TerrainAt(_racer.position);
            var mine = _sim.EffectivenessFor(_racer.Active, a.terrain);
            if ((int)mine > (int)_sim.EffectivenessFor(_racer.Active, now)) return true;
            // Igual para mí, pero peor para quien tengo cerca.
            var near = _sim.Nearest(_racer, AbilityResolver.NearRange);
            return near != null && (int)_sim.EffectivenessFor(near.Active, a.terrain) < (int)_sim.EffectivenessFor(near.Active, now);
        }
    }
}
