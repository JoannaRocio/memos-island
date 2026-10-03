using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemosIsland.World
{
    /// <summary>
    /// Recolección (GDD §15): cada día real aparecen cosas para juntar en lugares del mapa
    /// (flores, plumas, algas, conchas…). Siempre las mismas en el día; mañana, otras.
    /// </summary>
    public class ForageSpawner : MonoBehaviour
    {
        [SerializeField] List<Vector2Int> spots = new();
        [SerializeField] List<string> items = new();
        [SerializeField] int perDay = 4;

        public void Setup(List<Vector2Int> candidateSpots, List<string> itemPool, int countPerDay)
        {
            spots = candidateSpots;
            items = itemPool;
            perDay = countPerDay;
        }

        void Start()
        {
            var now = GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;
            var island = GameRoot.Instance.State.island;
            island.ResetDailyIfNeeded(now);
            var scene = SceneManager.GetActiveScene().name;
            var rng = new System.Random(Mining.Seed(IslandState.DateKey(now) + scene, 0));
            foreach (var cell in spots.OrderBy(_ => rng.Next()).Take(perDay))
            {
                var key = $"{scene}:{cell.x}:{cell.y}";
                var itemId = items[rng.Next(items.Count)];
                if (island.foraged.Contains(key)) continue;
                var item = MemoDatabase.Instance.GetItem(itemId);
                if (item == null) continue;

                var go = new GameObject($"Forage {itemId}");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.4f, 0f);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = item.icon;
                r.sortingOrder = 9;
                go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.2f, 1.2f);
                go.AddComponent<Forageable>().Setup(itemId, key);
            }
        }
    }
}
