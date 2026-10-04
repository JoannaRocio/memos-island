using System;
using System.Collections;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Town;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>Piezas para armar escenas de historia como corrutinas: hablar, narrar, elegir, caminar y vecinos "actores".</summary>
    public static class Cutscene
    {
        static GameRoot Root => GameRoot.Instance;

        /// <summary>Bloquea al jugador durante la escena (los diálogos ya bloquean, esto cubre las pausas entre ellos).</summary>
        public static void Begin() => GameRoot.InputLocks++;
        public static void End() => GameRoot.InputLocks = Mathf.Max(0, GameRoot.InputLocks - 1);

        public static IEnumerator Say(string speaker, Sprite portrait, params string[] pages)
        {
            while (Root.Dialogue.IsOpen) yield return null;
            bool closed = false;
            Root.Dialogue.SetSpeaker(speaker, portrait);
            Root.Dialogue.Show(pages, () => closed = true);
            while (!closed) yield return null;
        }

        public static IEnumerator Say(NeighborData n, params string[] pages) => Say(n.displayName, n.portrait, pages);

        /// <summary>Texto sin retrato (lo que ve o piensa el protagonista).</summary>
        public static IEnumerator Narrate(params string[] pages) => Say(null, null, pages);

        public static IEnumerator Choice(string question, string[] options, Action<int> onChosen, string speaker = null, Sprite portrait = null)
        {
            while (Root.Dialogue.IsOpen) yield return null;
            int chosen = -1;
            Root.Dialogue.SetSpeaker(speaker, portrait);
            Root.Dialogue.ShowChoice(question, options, i => chosen = i);
            while (chosen < 0) yield return null;
            onChosen?.Invoke(chosen);
        }

        public static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        /// <summary>Camina por casillas hasta el destino (si algo tapa el camino, espera y reintenta un rato).</summary>
        public static IEnumerator WalkTo(GridMover mover, Vector2Int target, Direction? face = null, bool run = false)
        {
            float waited = 0f;
            while (mover.Cell != target && waited < 6f)
            {
                var path = GridPath.Find(mover, mover.Cell, target, 4000);
                if (path.Count == 0)
                {
                    waited += 0.3f;
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }
                foreach (var dir in path)
                {
                    while (mover.IsBusy) yield return null;
                    if (!mover.TryStep(dir, run)) break;
                    while (mover.IsMoving) yield return null;
                }
            }
            if (face.HasValue) mover.Facing = face.Value;
        }

        /// <summary>Un vecino como actor de la escena (el NeighborDirector no lo maneja mientras tanto).</summary>
        public static NeighborNpc SpawnNeighbor(string id, Vector2Int cell, Direction facing)
        {
            var data = MemoDatabase.Instance.GetNeighbor(id);
            var prefab = Resources.Load<GameObject>("NeighborActor");
            if (data == null || prefab == null) return null;
            NeighborDirector.Reserved.Add(id);
            var go = UnityEngine.Object.Instantiate(prefab, GridMover.CellToWorld(cell), Quaternion.identity);
            var npc = go.GetComponent<NeighborNpc>();
            npc.Setup(data);
            npc.Mover.Facing = facing;
            npc.enabled = false; // en la escena no se charla con él
            return npc;
        }

        public static void ReleaseNeighbor(NeighborNpc npc)
        {
            if (npc == null) return;
            NeighborDirector.Reserved.Remove(npc.Data.id);
            UnityEngine.Object.Destroy(npc.gameObject);
        }

        /// <summary>Pantalla de un color que aparece y se va (relámpago, destello violeta del collar).</summary>
        public static IEnumerator Flash(Color color, float hold, float fade)
        {
            var ui = new UI.UiKit("Flash");
            var r = ui.Rect(0, 0, 260, 150, color, 2000);
            yield return new WaitForSeconds(hold);
            for (float t = 0; t < fade; t += Time.deltaTime)
            {
                r.color = new Color(color.r, color.g, color.b, color.a * (1f - t / fade));
                yield return null;
            }
            ui.Destroy();
        }
    }
}
