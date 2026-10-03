using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Town;

namespace MemosIsland.UI
{
    /// <summary>Menús del pueblo (Fase 7): regalar, tablón de pedidos y revisión en la clínica.</summary>
    public static class TownMenus
    {
        static GameState State => GameRoot.Instance.State;
        static MemoDatabase Db => MemoDatabase.Instance;
        static DateTime Now => GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;

        // ------------------------------------------------------------------ Regalar

        /// <summary>Elegís un objeto de la mochila para regalar; onChosen recibe el id (o nada si salís con B).</summary>
        public static void OpenGift(NeighborData neighbor, Action<string> onChosen)
        {
            List<ItemData> items = null;
            string chosen = null;
            GameRoot.Instance.Lists.Open(new ListScreen.Content
            {
                title = $"Regalo para {neighbor.displayName}",
                rows = _ =>
                {
                    items = State.inventory.Select(s => Db.GetItem(s.id)).Where(i => i != null).ToList();
                    return items.Select(i => new ListScreen.Row
                    {
                        icon = i.icon, label = i.displayName, right = $"×{State.CountOf(i.id)}",
                    }).ToList();
                },
                detail = (_, row) => items[row].description,
                confirm = (_, row) =>
                {
                    chosen = items[row].id;
                    GameRoot.Instance.Lists.Close();
                    return null;
                },
                hint = "A: regalar  B: volver",
            }, () => onChosen?.Invoke(chosen));
        }

        // ------------------------------------------------------------------ Tablón de pedidos

        public static void OpenBoard()
        {
            var town = State.town;
            RequestBoard.Refresh(town, Db.neighbors, Now);
            GameRoot.Instance.Lists.Open(new ListScreen.Content
            {
                title = "Tablón de pedidos",
                header = () => $"${State.island.money}",
                rows = _ => town.board.Select(r =>
                {
                    var n = Db.GetNeighbor(r.neighborId);
                    var t = RequestBoard.TemplateOf(r, n);
                    return new ListScreen.Row
                    {
                        icon = n?.icon,
                        label = n?.displayName ?? r.neighborId,
                        right = r.done ? "Hecho" : r.accepted ? "Aceptado" : $"${t?.reward}",
                        enabled = !r.done,
                    };
                }).ToList(),
                detail = (_, row) =>
                {
                    var r = town.board[row];
                    var n = Db.GetNeighbor(r.neighborId);
                    var t = RequestBoard.TemplateOf(r, n);
                    if (t == null) return "";
                    string status = r.done ? "¡Ya lo cumpliste!"
                        : r.accepted ? $"Aceptado. Hablá con {n.displayName} para entregarlo."
                        : "A: aceptar el pedido.";
                    string have = t.kind == RequestKind.Deliver ? $"\nTenés {State.CountOf(t.targetId)}/{t.count}." : "";
                    return $"{t.boardText}{have}\n\nPremio: ${t.reward} y amistad.\n{status}";
                },
                confirm = (_, row) =>
                {
                    var r = town.board[row];
                    if (r.done || r.accepted) return null;
                    r.accepted = true;
                    return $"Aceptaste el pedido de {Db.GetNeighbor(r.neighborId)?.displayName}. Vence hoy a la noche.";
                },
                hint = "A: aceptar  B: salir",
            });
        }

        // ------------------------------------------------------------------ Clínica de Anni

        /// <summary>Anni revisa a tu equipo y te da un consejo por Memo (GDD §15 "consejos de cuidado").</summary>
        public static List<string> ClinicCheckup()
        {
            var memos = State.team.ToList();
            var companion = State.Companion;
            if (companion != null && !memos.Contains(companion)) memos.Add(companion);
            var pages = new List<string>();
            if (memos.Count == 0)
            {
                pages.Add("No trajiste ningún Memo. Cuando quieras, traelos y los reviso.");
                return pages;
            }
            pages.Add("A ver, vengan… Les reviso las orejitas, las patitas y el corazón.");
            foreach (var m in memos) pages.Add($"{m.DisplayName}: {Advice(m)}");
            pages.Add("Y acordate: un Memo contento corre mejor. ¡Pero sobre todo, vive mejor!");
            return pages;
        }

        static string Advice(MemoInstance m)
        {
            if (m.TrustLevel <= TrustLevel.Fear) return "todavía tiene mucho miedo. No lo apures: quedate cerca, quieto, y dejale comida.";
            var needs = new (float value, string text)[]
            {
                (m.hunger, "tiene hambre. Una comida favorita lo pone contentísimo."),
                (m.sleep, "está cansado. Dejalo dormir en su camita del refugio."),
                (m.fun, "se aburre. Jugá con él o dejalo con otros Memos."),
                (m.social, "se siente solo. Pasá más tiempo con él."),
            };
            var worst = needs.OrderBy(n => n.value).First();
            if (worst.value < 50f) return worst.text;
            if (m.mood >= 80) return "¡está radiante! Se nota que lo cuidás mucho.";
            return "está sano y tranquilo. Seguí así.";
        }
    }
}
