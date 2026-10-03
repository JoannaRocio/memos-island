using System;
using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Las 4 stats de carrera. Las base van de 1 a 10 (GDD §7).</summary>
    [Serializable]
    public struct MemoStats
    {
        [Range(1, 10)] public int speed;        // VEL: velocidad máxima
        [Range(1, 10)] public int acceleration; // ACE: qué tan rápido llega a la máxima
        [Range(1, 10)] public int stamina;      // RES: cuánta energía tiene
        [Range(1, 10)] public int charge;       // CAR: qué tan rápido carga la habilidad

        public MemoStats(int speed, int acceleration, int stamina, int charge)
        {
            this.speed = speed;
            this.acceleration = acceleration;
            this.stamina = stamina;
            this.charge = charge;
        }

        public int Total => speed + acceleration + stamina + charge;

        /// <summary>Crecimiento por nivel del GDD §13: base × (1 + nivel × 0,04).</summary>
        public static float AtLevel(int baseValue, int level) => baseValue * (1f + level * 0.04f);
    }

    /// <summary>Stats ya calculadas para un nivel y un temperamento (valores con decimales).</summary>
    public readonly struct ComputedStats
    {
        public readonly float Speed, Acceleration, Stamina, Charge;

        public ComputedStats(float speed, float acceleration, float stamina, float charge)
        {
            Speed = speed;
            Acceleration = acceleration;
            Stamina = stamina;
            Charge = charge;
        }

        public static ComputedStats For(MemoStats baseStats, int level, Temperament temperament = null)
        {
            float s = MemoStats.AtLevel(baseStats.speed, level);
            float a = MemoStats.AtLevel(baseStats.acceleration, level);
            float r = MemoStats.AtLevel(baseStats.stamina, level);
            float c = MemoStats.AtLevel(baseStats.charge, level);
            if (temperament != null)
            {
                s *= temperament.speedMultiplier;
                a *= temperament.accelerationMultiplier;
                r *= temperament.staminaMultiplier;
                c *= temperament.chargeMultiplier;
            }
            return new ComputedStats(s, a, r, c);
        }
    }
}
