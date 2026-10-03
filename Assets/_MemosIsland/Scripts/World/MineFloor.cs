using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MemosIsland.World
{
    /// <summary>
    /// Un piso de la Cueva de Zorak (GDD §15). La misma escena sirve para los 5 pisos: al cargar,
    /// arma las rocas del día (siempre las mismas hoy, otras mañana), la escalera para bajar
    /// y los Memos salvajes del piso.
    /// </summary>
    public class MineFloor : MonoBehaviour
    {
        public const string SceneName = "Map_Cueva";

        [SerializeField] RectInt area = new(1, 1, 18, 12);
        [SerializeField] Vector2Int upLadder = new(2, 11);
        [SerializeField] Tilemap ground;
        [SerializeField] TileBase roughTile;
        [SerializeField] Sprite stone, copper, iron, quartz, gem, ladderDown, ladderUp;
        [SerializeField] string returnScene = "Map_RefugioExterior";
        [SerializeField] string returnSpawn = "cueva";

        int _floor;

        public void Setup(RectInt walkArea, Vector2Int up, Tilemap groundMap, TileBase rough,
            Sprite[] rocks, Sprite down, Sprite upSprite, string backScene, string backSpawn)
        {
            area = walkArea;
            upLadder = up;
            ground = groundMap;
            roughTile = rough;
            (stone, copper, iron, quartz, gem) = (rocks[0], rocks[1], rocks[2], rocks[3], rocks[4]);
            ladderDown = down;
            ladderUp = upSprite;
            returnScene = backScene;
            returnSpawn = backSpawn;
        }

        void Awake()
        {
            // La GameRoot puede no existir todavía si se le da Play directo a la cueva.
            var state = GameRoot.Instance != null ? GameRoot.Instance.State : null;
            _floor = Mathf.Clamp(state?.island.mineFloor ?? 1, 1, Mining.Floors);
            var map = FindAnyObjectByType<MapInfo>();
            if (map != null) map.Setup($"Cueva de Zorak · Piso {_floor}", map.Bounds);
        }

        void Start()
        {
            var state = GameRoot.Instance.State;
            var now = GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;
            state.island.ResetDailyIfNeeded(now);
            var date = IslandState.DateKey(now);

            var candidates = new List<Vector2Int>();
            for (int x = area.xMin; x < area.xMax; x++)
            for (int y = area.yMin; y < area.yMax; y++)
            {
                var c = new Vector2Int(x, y);
                if (GridPath.Manhattan(c, upLadder) > 2) candidates.Add(c);
            }

            // Escalera para bajar (menos en el último piso), siempre en el mismo lugar hoy.
            Vector2Int? down = null;
            if (_floor < Mining.Floors)
            {
                var rng = new System.Random(Mining.Seed(date, _floor + 100));
                var far = candidates.Where(c => GridPath.Manhattan(c, upLadder) > 8).ToList();
                down = far[rng.Next(far.Count)];
                candidates.RemoveAll(c => GridPath.Manhattan(c, down.Value) <= 1);
                MakeLadder(down.Value, ladderDown, goingDown: true);
            }
            MakeLadder(upLadder, ladderUp, goingDown: false);

            foreach (var (cell, kind) in Mining.GenerateRocks(date, _floor, candidates, 10 + _floor * 2))
            {
                if (state.island.brokenRocks.Contains(Mining.RockKey(_floor, cell))) continue;
                MakeRock(cell, kind);
            }

            SetupEncounters();
        }

        void MakeLadder(Vector2Int cell, Sprite sprite, bool goingDown)
        {
            var go = new GameObject(goingDown ? "Ladder Down" : "Ladder Up");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            go.layer = LayerMask.NameToLayer("Solid");
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = 2;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);
            go.AddComponent<MineLadder>().Setup(goingDown, _floor, returnScene, returnSpawn);
        }

        void MakeRock(Vector2Int cell, RockKind kind)
        {
            var go = new GameObject($"Rock {kind}");
            go.transform.SetParent(transform, false);
            go.transform.position = GridMover.CellToWorld(cell);
            go.layer = LayerMask.NameToLayer("Solid");
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = kind switch
            {
                RockKind.Copper => copper,
                RockKind.Iron => iron,
                RockKind.Quartz => quartz,
                RockKind.Gem => gem,
                _ => stone,
            };
            r.sortingOrder = 10;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);
            col.offset = new Vector2(0f, 0.5f);
            go.AddComponent<Rock>().Setup(kind, _floor, cell);
        }

        void SetupEncounters()
        {
            var table = new List<WildEncounters.Entry>
            {
                new() { speciesId = "topin", minLevel = 3 + _floor, maxLevel = 5 + _floor, weight = 5 },
            };
            if (_floor >= 3) table.Add(new WildEncounters.Entry { speciesId = "farolito", minLevel = 6, maxLevel = 8, weight = _floor == 5 ? 4 : 1 });
            var encounters = gameObject.AddComponent<WildEncounters>();
            encounters.Setup(ground, roughTile, "cueva", table);
        }
    }
}
