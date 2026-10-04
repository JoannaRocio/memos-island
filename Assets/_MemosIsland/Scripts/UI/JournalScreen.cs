using System;
using System.Collections;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Story;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>
    /// El diario del abuelo (GDD §4, Acto 1): tres páginas arrancadas a medias, cada una con la silueta borroneada
    /// de un Memo y lo que el abuelo escribió de él. La página que sigas es tu Memo inicial (nunca lo ves antes de encontrarlo).
    /// </summary>
    public static class JournalScreen
    {
        const int Order = 900;

        public class Page
        {
            public string speciesId, name, text;
        }

        public static readonly Page[] Pages =
        {
            new()
            {
                speciesId = "tostin", name = "Tostín",
                text = "\"Lo encontré dormido entre las brasas de un horno de pan abandonado. Se hace el duro, pero llora si lo dejás solo de noche.\"",
            },
            new()
            {
                speciesId = "brotito", name = "Brotito",
                text = "\"Nació de una semilla que planté el día que nació mi {nieto/nieta/niete}. Es tímido, mira todo con mucha atención. Crecieron juntos sin conocerse.\"",
            },
            new()
            {
                speciesId = "charquito", name = "Charquito",
                text = "\"Perdió a su manada en una tormenta. Imita a todos para no sentirse solo. Todavía mira el mar esperando.\"",
            },
        };

        public static IEnumerator Run(PlayerProfile profile, Action<string> onChosen)
        {
            var ui = new UiKit("Journal");
            UiKit.FullScreens++;
            ui.Rect(0, 0, 260, 150, new Color32(0x1a, 0x1c, 0x2c, 0xff), Order);
            // Cuaderno abierto: tapa de cuero y dos hojas.
            ui.Rect(0, 4, 228, 118, new Color32(0x5a, 0x33, 0x28, 0xff), Order + 1);
            ui.Rect(-56, 4, 108, 110, new Color32(0xff, 0xf1, 0xc9, 0xff), Order + 2);
            ui.Rect(56, 4, 108, 110, new Color32(0xff, 0xf1, 0xc9, 0xff), Order + 2);
            ui.Rect(0, 4, 2, 110, new Color32(0xd9, 0xa0, 0x66, 0xff), Order + 3);
            // Borde arrancado de la hoja derecha.
            for (int i = 0; i < 11; i++)
                ui.Rect(108, 54 - i * 10, 4, 5, new Color32(0x5a, 0x33, 0x28, 0xff), Order + 3);

            var header = ui.Text(-104, 52, UiKit.MidGray, Order + 5, false);
            // Silueta borroneada: el Memo en negro con una sombra corrida (no se le ven los rasgos).
            var smudge = ui.Sprite(null, -54, -34, Order + 4);
            smudge.color = new Color32(0x56, 0x6c, 0x86, 0xff);
            var silhouette = ui.Sprite(null, -56, -33, Order + 5);
            silhouette.color = new Color32(0x1a, 0x1c, 0x2c, 0xff);
            var nameText = ui.Text(-104, -40, UiKit.Dark, Order + 5, false);
            var body = ui.Text(6, 50, UiKit.Dark, Order + 5, false);
            var hint = ui.Text(-110, -60, UiKit.Gray, Order + 5, false);
            hint.SetText("←→: otra página · A: seguir esta página");

            int page = 0, heldDir = 0;
            string chosen = null;
            yield return null;
            while (chosen == null)
            {
                var p = Pages[page];
                var species = MemoDatabase.Instance.GetSpecies(p.speciesId);
                header.SetText($"Página {page + 1} de {Pages.Length}");
                silhouette.sprite = smudge.sprite = species != null ? species.Portrait : null;
                nameText.SetText(p.name);
                body.SetText(string.Join("\n", ui.Font.Wrap(TextTags.Apply(p.text, profile), 98)));

                if (!GameRoot.Instance.Dialogue.IsOpen)
                {
                    if (GameInput.ConfirmPressed)
                    {
                        int? answer = null;
                        GameRoot.Instance.Dialogue.ShowChoice($"¿Seguís la página de {p.name}?", new[] { "Sí", "Leer otra" },
                            i => answer = i, 1);
                        while (answer == null) yield return null;
                        if (answer == 0) chosen = p.speciesId;
                        yield return null;
                        continue;
                    }
                    var move = GameInput.Move;
                    int h = move.x > 0.5f ? 1 : move.x < -0.5f ? -1 : 0;
                    if (h != 0 && h != heldDir) page = (page + h + Pages.Length) % Pages.Length;
                    heldDir = h;
                }
                yield return null;
            }
            ui.Destroy();
            UiKit.FullScreens--;
            onChosen?.Invoke(chosen);
        }

        public static Page PageFor(string speciesId) => Array.Find(Pages, p => p.speciesId == speciesId);
    }
}
