using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Town;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemosIsland.World
{
    /// <summary>
    /// Vecinos de este mapa (Fase 7). Cada segundo mira la rutina de cada vecino: si ahora le toca estar acá,
    /// aparece (por una salida, o directamente en su lugar al cargar el mapa) y camina hasta su puesto;
    /// si le toca estar en otro lado, se va caminando por la salida más cercana.
    /// </summary>
    public class NeighborDirector : MonoBehaviour
    {
        [Tooltip("Casillas por donde entran y salen los vecinos (puertas, bordes del mapa).")]
        [SerializeField] List<Vector2Int> exits = new();

        readonly Dictionary<string, NeighborNpc> _present = new();
        string _scene;
        float _timer;
        GameObject _prefab;

        public static NeighborDirector Current { get; private set; }

        public void Setup(List<Vector2Int> exitCells) => exits = exitCells;

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            _scene = SceneManager.GetActiveScene().name;
            _prefab = Resources.Load<GameObject>("NeighborActor");
            Tick(initial: true);
        }

        void Update()
        {
            if ((_timer -= Time.deltaTime) > 0f) return;
            _timer = 1f;
            if (GameRoot.Instance == null || GameRoot.Instance.Maps.IsTransitioning) return;
            Tick(initial: false);
        }

        static DateTime Now => GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;

        /// <summary>El vecino si está en este mapa ahora (o null).</summary>
        public NeighborNpc Find(string id) => _present.TryGetValue(id, out var npc) && npc != null && !npc.Leaving ? npc : null;

        /// <summary>¿Está atendiendo su negocio ahora (llegó a su puesto de trabajo)?</summary>
        public bool IsWorking(string id)
        {
            var npc = Find(id);
            return npc != null && npc.Entry != null && npc.Entry.activity == "trabajo";
        }

        void Tick(bool initial)
        {
            var db = MemoDatabase.Instance;
            if (db == null || _prefab == null) return;
            var now = Now;
            foreach (var data in db.neighbors.Where(n => n != null))
            {
                var entry = NeighborSchedule.Resolve(data, now);
                bool wantHere = entry != null && entry.scene == _scene;
                _present.TryGetValue(data.id, out var npc);
                if (npc != null && npc.Leaving) continue; // ya se está yendo

                if (npc == null && wantHere)
                {
                    var cell = initial || exits.Count == 0 ? entry.cell : Closest(exits, entry.cell);
                    if (!initial && GridMover.IsOccupied(cell)) continue; // la salida está tapada: el próximo segundo
                    npc = Spawn(data, cell);
                    if (initial) npc.PlaceAt(entry);
                    else npc.GoTo(entry);
                    _present[data.id] = npc;
                }
                else if (npc != null && !wantHere)
                {
                    if (exits.Count == 0) Despawn(data.id);
                    else
                    {
                        var id = data.id;
                        npc.Leave(Closest(exits, npc.Mover.Cell), () => Despawn(id));
                    }
                }
                else if (npc != null && npc.Entry != entry)
                    npc.GoTo(entry);
            }
            if (!initial) Reactions();
        }

        NeighborNpc Spawn(NeighborData data, Vector2Int cell)
        {
            // Se instancia ya en su casilla: el GridMover ocupa la casilla en OnEnable.
            var go = Instantiate(_prefab, GridMover.CellToWorld(cell), Quaternion.identity, transform);
            var npc = go.GetComponent<NeighborNpc>();
            npc.Setup(data);
            return npc;
        }

        void Despawn(string id)
        {
            if (_present.TryGetValue(id, out var npc) && npc != null) Destroy(npc.gameObject);
            _present.Remove(id);
        }

        static Vector2Int Closest(List<Vector2Int> cells, Vector2Int to) =>
            cells.OrderBy(c => GridPath.Manhattan(c, to)).First();

        /// <summary>Detalles de vida: Jojo se asusta de los Memos, los demás saludan a tu compañero.</summary>
        void Reactions()
        {
            var companion = GameRoot.Instance.Companion;
            var memo = GameRoot.Instance.State.Companion;
            if (companion == null || memo == null || !companion.isActiveAndEnabled) return;
            var memoCell = GridMover.WorldToCell(companion.transform.position);
            foreach (var npc in _present.Values)
            {
                if (npc == null || npc.Talking || GridPath.Manhattan(npc.Mover.Cell, memoCell) > 2) continue;
                if (UnityEngine.Random.value > 0.12f) continue;
                npc.Bubble?.Show(npc.Data.id == "jojo" ? Emote.Scared : Emote.Love, 1.2f);
            }
        }
    }
}
