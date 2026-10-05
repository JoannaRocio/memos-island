using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Race;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MemosIsland.World
{
    /// <summary>
    /// Encuentros con Memos salvajes al caminar por el pasto alto (GDD §10). La carrera de captura
    /// se corre sobre el terreno de este lugar.
    /// </summary>
    public class WildEncounters : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string speciesId;
            public int minLevel = 3, maxLevel = 5;
            [Min(1)] public int weight = 1;
            [Tooltip("Solo aparece de noche (Noche y Atardecer).")]
            public bool nightOnly;
            [Tooltip("Aparece con el collar de Ápice: al ganarle se lo rompés.")]
            public bool collared;
        }

        [SerializeField] Tilemap ground;
        [SerializeField] TileBase encounterTile;
        [SerializeField] string terrainId = "pradera";
        [SerializeField, Range(0f, 1f)] float chancePerStep = 0.12f;
        [SerializeField] List<Entry> entries = new();

        public static WildEncounters Current { get; private set; }

        int _graceSteps = 3;
        readonly System.Random _rng = new();

        public void Setup(Tilemap groundTilemap, TileBase tile, string terrain, List<Entry> table)
        {
            ground = groundTilemap;
            encounterTile = tile;
            terrainId = terrain;
            entries = table;
        }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        /// <summary>Lo llama el MapManager en cada paso del jugador. Devuelve true si empezó un encuentro.</summary>
        public bool TryEncounter(Vector2Int cell)
        {
            if (_graceSteps > 0)
            {
                _graceSteps--;
                return false;
            }
            if (ground == null || ground.GetTile((Vector3Int)cell) != encounterTile) return false;
            if (GameRoot.Instance.State.team.Count == 0) return false; // sin Memos todavía (inicio de la historia)
            if (_rng.NextDouble() >= chancePerStep) return false;

            var entry = Pick();
            if (entry == null) return false;
            _graceSteps = 4;
            StartEncounter(entry);
            return true;
        }

        Entry Pick()
        {
            bool night = GameClock.Instance != null && GameClock.Instance.Phase is DayPhase.Night or DayPhase.Dusk;
            var valid = entries.Where(e => !e.nightOnly || night).ToList();
            int total = valid.Sum(e => e.weight);
            if (total <= 0) return null;
            int roll = _rng.Next(total);
            foreach (var e in valid)
            {
                if (roll < e.weight) return e;
                roll -= e.weight;
            }
            return valid[^1];
        }

        void StartEncounter(Entry entry)
        {
            var root = GameRoot.Instance;
            var wild = MemoInstance.CreateRandom(entry.speciesId, _rng.Next(entry.minLevel, entry.maxLevel + 1), _rng, trust: 0);
            wild.collared = entry.collared;
            var team = root.State.team;

            AudioManager.Cry(wild.Species, wild.collared ? 0.8f : 1f);
            string intro = wild.collared
                ? $"¡Un {wild.DisplayName} con collar! Sus ojos no tienen brillo…"
                : wild.shiny ? $"¡Un {wild.DisplayName} salvaje… y brilla!" : $"¡Apareció un {wild.DisplayName} salvaje!";

            var options = team.Select(m => $"{m.DisplayName} Nv.{m.level}").ToList();
            options.Add("Huir");
            root.Dialogue.Show(new[] { intro }, () =>
                root.Dialogue.ShowChoice("¿Quién corre?", options, choice =>
                {
                    if (choice >= team.Count)
                    {
                        root.Dialogue.Show(new[] { "Te alejaste despacito…" });
                        return;
                    }
                    RaceLauncher.Start(RaceSetup.Capture(wild, terrainId, team[choice]));
                }, options.Count - 1));
        }
    }
}
