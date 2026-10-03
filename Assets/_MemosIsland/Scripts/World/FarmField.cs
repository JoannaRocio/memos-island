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
    /// La huerta del refugio (GDD §15). A sobre una parcela hace lo que corresponde:
    /// arar → plantar → regar → cosechar. Las herramientas mejoradas actúan en línea (1, 3 o 5 parcelas).
    /// </summary>
    public class FarmField : MonoBehaviour, ICellInteractable
    {
        [SerializeField] RectInt area = new(5, 4, 5, 4);
        [SerializeField] Tilemap soilMap;
        [SerializeField] TileBase drySoil;
        [SerializeField] TileBase wetSoil;
        [SerializeField] List<Sprite> cropSprites = new(); // "crop_<cultivo>_<etapa>"

        readonly Dictionary<Vector2Int, SpriteRenderer> _crops = new();
        Dictionary<string, Sprite> _spriteByName;

        public RectInt Area => area;

        public void Setup(RectInt field, Tilemap soil, TileBase dry, TileBase wet, List<Sprite> crops)
        {
            area = field;
            soilMap = soil;
            drySoil = dry;
            wetSoil = wet;
            cropSprites = crops;
        }

        void OnEnable() => CellInteractables.Register(this);
        void OnDisable() => CellInteractables.Unregister(this);

        void Start()
        {
            _spriteByName = cropSprites.Where(s => s != null).ToDictionary(s => s.name);
            var island = GameRoot.Instance.State.island;
            foreach (var cell in Cells())
                if (island.PlotAt(cell.x, cell.y) == null)
                    island.plots.Add(new FarmPlot { x = cell.x, y = cell.y });
            Refresh();
        }

        IEnumerable<Vector2Int> Cells()
        {
            for (int x = area.xMin; x < area.xMax; x++)
            for (int y = area.yMin; y < area.yMax; y++)
                yield return new Vector2Int(x, y);
        }

        public bool Handles(Vector2Int cell) => area.Contains(cell);

        static DateTime Now => GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;

        public void InteractCell(PlayerController player, Vector2Int cell)
        {
            var root = GameRoot.Instance;
            var state = root.State;
            var island = state.island;
            var plot = island.PlotAt(cell.x, cell.y);
            if (plot == null) return;
            var seed = MemoDatabase.Instance.GetItem(plot.seedId);

            if (!plot.tilled)
            {
                foreach (var p in Line(cell, player.Mover.Facing, island.hoeLevel)) FarmLogic.Till(p);
                Refresh();
                return;
            }
            if (!plot.HasCrop)
            {
                ChooseSeed(plot);
                return;
            }
            if (FarmLogic.IsReady(plot, seed))
            {
                var cropId = FarmLogic.Harvest(plot, seed, state);
                Refresh();
                root.Dialogue.Show(new[] { $"¡Cosechaste: {MemoDatabase.Instance.GetItem(cropId)?.displayName}!" });
                return;
            }
            if (plot.wateredOn != IslandState.DateKey(Now))
            {
                foreach (var p in Line(cell, player.Mover.Facing, island.canLevel)) FarmLogic.Water(p, Now);
                Refresh();
                return;
            }
            root.Dialogue.Show(new[]
            {
                $"{MemoDatabase.Instance.GetItem(seed.growsInto)?.displayName}: {plot.growth}/{seed.growDays} días regados. " +
                "Hoy ya está regado. Mañana va a estar un poquito más grande.",
            });
        }

        /// <summary>Parcelas en línea hacia donde mira el jugador, según el nivel de la herramienta.</summary>
        IEnumerable<FarmPlot> Line(Vector2Int start, Direction facing, int toolLevel)
        {
            var island = GameRoot.Instance.State.island;
            var step = facing.ToVector();
            for (int i = 0; i < ShopCatalog.AreaFor(toolLevel); i++)
            {
                var c = start + step * i;
                if (!area.Contains(c)) yield break;
                var p = island.PlotAt(c.x, c.y);
                if (p != null) yield return p;
            }
        }

        void ChooseSeed(FarmPlot plot)
        {
            var root = GameRoot.Instance;
            var state = root.State;
            var seeds = state.inventory.Select(s => MemoDatabase.Instance.GetItem(s.id))
                .Where(i => i != null && i.kind == ItemKind.Seed).ToList();
            if (seeds.Count == 0)
            {
                root.Dialogue.Show(new[] { "No tenés semillas. Deny las vende en el pueblo." });
                return;
            }
            var labels = seeds.Select(s => $"{s.displayName} ×{state.CountOf(s.id)}").ToList();
            labels.Add("Nada");
            root.Dialogue.ShowChoice("¿Qué plantás?", labels, i =>
            {
                if (i >= seeds.Count) return;
                FarmLogic.Plant(plot, seeds[i], state);
                Refresh();
            }, labels.Count - 1);
        }

        /// <summary>Si hay un Memo de agua trabajando, hoy la huerta amanece regada.</summary>
        public void WaterAllByMemos()
        {
            var island = GameRoot.Instance.State.island;
            foreach (var p in island.plots.Where(p => area.Contains(new Vector2Int(p.x, p.y)) && p.HasCrop))
                FarmLogic.Water(p, Now);
            Refresh();
        }

        public void Refresh()
        {
            var island = GameRoot.Instance.State.island;
            var today = IslandState.DateKey(Now);
            foreach (var cell in Cells())
            {
                var plot = island.PlotAt(cell.x, cell.y);
                var tile = plot == null || !plot.tilled ? null : plot.wateredOn == today ? wetSoil : drySoil;
                soilMap.SetTile((Vector3Int)cell, tile);

                var seed = plot != null && plot.HasCrop ? MemoDatabase.Instance.GetItem(plot.seedId) : null;
                if (!_crops.TryGetValue(cell, out var r))
                {
                    r = new GameObject($"Crop {cell}").AddComponent<SpriteRenderer>();
                    r.transform.SetParent(transform, false);
                    r.transform.position = new Vector3(cell.x + 0.5f, cell.y, 0f);
                    r.sortingOrder = 10;
                    _crops[cell] = r;
                }
                if (seed == null)
                {
                    r.enabled = false;
                    continue;
                }
                _spriteByName.TryGetValue($"crop_{seed.growsInto}_{FarmLogic.Stage(plot, seed)}", out var sprite);
                r.sprite = sprite;
                r.enabled = sprite != null;
            }
        }
    }
}
