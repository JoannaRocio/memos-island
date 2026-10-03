using System;
using UnityEngine;

namespace MemosIsland.Memos
{
    /// <summary>Niveles de confianza del GDD §11.</summary>
    public enum TrustLevel { Hostile, Fear, Distrust, Neutral, Trusting, Friend, Soulmate }

    /// <summary>Reglas de confianza que afectan las carreras (GDD §9 y §11).</summary>
    public static class TrustRules
    {
        public static TrustLevel LevelFor(int points) => points switch
        {
            < 0 => TrustLevel.Hostile,
            < 100 => TrustLevel.Fear,
            < 250 => TrustLevel.Distrust,
            < 450 => TrustLevel.Neutral,
            < 700 => TrustLevel.Trusting,
            < 1000 => TrustLevel.Friend,
            _ => TrustLevel.Soulmate,
        };

        /// <summary>Multiplicador de velocidad en carrera: 0,90 (Hostil/Miedo) … 1,10 (Alma gemela).</summary>
        public static float SpeedFactor(TrustLevel level) => level switch
        {
            TrustLevel.Hostile => 0.90f,
            TrustLevel.Fear => 0.90f,
            TrustLevel.Distrust => 0.95f,
            TrustLevel.Neutral => 1.00f,
            TrustLevel.Trusting => 1.03f,
            TrustLevel.Friend => 1.06f,
            _ => 1.10f,
        };

        /// <summary>Probabilidad de que desobedezca una orden en carrera.</summary>
        public static float DisobeyChance(TrustLevel level) => level switch
        {
            TrustLevel.Hostile => 0.35f,
            TrustLevel.Fear => 0.20f,
            TrustLevel.Distrust => 0.08f,
            _ => 0f,
        };

        public static string Name(TrustLevel level) => level switch
        {
            TrustLevel.Hostile => "Hostil",
            TrustLevel.Fear => "Miedo",
            TrustLevel.Distrust => "Desconfianza",
            TrustLevel.Neutral => "Neutral",
            TrustLevel.Trusting => "Confía",
            TrustLevel.Friend => "Amigo",
            _ => "Alma gemela",
        };

        /// <summary>Ánimo de 0 (triste) a 100 (feliz) → 0,95 … 1,05.</summary>
        public static float MoodFactor(int mood) => Mathf.Lerp(0.95f, 1.05f, Mathf.Clamp01(mood / 100f));

        /// <summary>Puntos mínimos para estar en ese nivel.</summary>
        public static int MinPointsFor(TrustLevel level) => level switch
        {
            TrustLevel.Hostile => -100,
            TrustLevel.Fear => 0,
            TrustLevel.Distrust => 100,
            TrustLevel.Neutral => 250,
            TrustLevel.Trusting => 450,
            TrustLevel.Friend => 700,
            _ => 1000,
        };
    }

    /// <summary>Un Memo concreto del jugador (o de un rival): lo que se guarda en la partida.</summary>
    [Serializable]
    public class MemoInstance
    {
        public string uid = Guid.NewGuid().ToString("N");
        public string speciesId;
        public string nickname;
        public int level = 5;
        public string temperamentId;
        public bool shiny;
        public bool collared;
        public int trust = 250;
        [Range(0, 100)] public int mood = 50;

        [Header("Necesidades (0 = urgente, 100 = satisfecha)")]
        public float hunger = 80f;
        public float sleep = 80f;
        public float fun = 80f;
        public float social = 80f;
        public long needsUpdatedTicks;

        [Header("Cuidados del día (día real)")]
        public string careDate;
        public int feedsToday;
        public bool pettedToday;
        public bool playedToday;
        public int nearPointsToday;
        public string lastBathDate;
        public float followSeconds;

        [Header("Vínculo")]
        [Tooltip("Nivel de confianza más alto alcanzado: define qué recuerdos ya recuperó.")]
        public TrustLevel highestTrust = TrustLevel.Fear;
        [Tooltip("uid del Memo que lo apadrina (para los recién rescatados).")]
        public string godparentUid;
        [Tooltip("Dónde está en el refugio: adentro de la casa o afuera.")]
        public bool inside;
        [Tooltip("Lo liberaste de un collar de Ápice.")]
        public bool rescued;

        [Header("Progreso (Fase 5)")]
        public int xp;
        [Tooltip("Si canceló la evolución en este nivel, no vuelve a intentarlo hasta el próximo.")]
        public int cancelledEvolutionAtLevel;
        public long rebelUntilTicks;
        public TrustLevel rebelFrom;
        public bool grewTogether;

        [Header("Equipamiento (Fase 5)")]
        public string equipmentId;
        public string amuletId;
        public string accessoryId;
        public System.Collections.Generic.List<string> accessoriesTried = new();

        [Tooltip("Legendarios hostiles: de día se van del refugio hasta esta hora.")]
        public long awayUntilTicks;

        public MemoSpecies Species => MemoDatabase.Instance != null ? MemoDatabase.Instance.GetSpecies(speciesId) : null;
        public Temperament Temperament => MemoDatabase.Instance != null ? MemoDatabase.Instance.GetTemperament(temperamentId) : null;
        public string DisplayName => string.IsNullOrEmpty(nickname) ? Species?.displayName ?? speciesId : nickname;
        public TrustLevel TrustLevel => TrustRules.LevelFor(trust);
        public ItemData Equipment => MemoDatabase.Instance?.GetItem(equipmentId);
        public ItemData Amulet => MemoDatabase.Instance?.GetItem(amuletId);
        public ItemData Accessory => MemoDatabase.Instance?.GetItem(accessoryId);
        public ComputedStats Stats =>
            ComputedStats.For(EquipmentRules.WithEquipment(Species.baseStats, Equipment), level, Temperament);
        public bool IsRebel => Progression.IsRebel(this, DateTime.Now);

        public Sprite[] WorldFrames
        {
            get
            {
                var s = Species;
                if (s == null) return null;
                if (collared && s.collarWorldFrames is { Length: > 0 }) return s.collarWorldFrames;
                if (shiny && s.shinyWorldFrames is { Length: > 0 }) return s.shinyWorldFrames;
                return s.worldFrames;
            }
        }

        public Sprite[] RaceFrames
        {
            get
            {
                var s = Species;
                if (s == null) return null;
                if (collared && s.collarRaceFrames is { Length: > 0 }) return s.collarRaceFrames;
                if (shiny && s.shinyRaceFrames is { Length: > 0 }) return s.shinyRaceFrames;
                return s.raceFrames;
            }
        }

        public static MemoInstance Create(string speciesId, int level, string temperamentId = null, int trust = 250)
        {
            return new MemoInstance
            {
                speciesId = speciesId, level = level, temperamentId = temperamentId, trust = trust,
                highestTrust = TrustRules.LevelFor(trust),
            };
        }

        /// <summary>Crea un Memo con temperamento al azar (y 1 en 200 de ser brillante, GDD §7).</summary>
        public static MemoInstance CreateRandom(string speciesId, int level, System.Random rng, int trust = 250)
        {
            var db = MemoDatabase.Instance;
            string temperament = db != null && db.temperaments.Count > 0
                ? db.temperaments[rng.Next(db.temperaments.Count)].id
                : null;
            return new MemoInstance
            {
                speciesId = speciesId, level = level, temperamentId = temperament, trust = trust,
                highestTrust = TrustRules.LevelFor(trust),
                shiny = rng.Next(200) == 0,
            };
        }
    }
}
