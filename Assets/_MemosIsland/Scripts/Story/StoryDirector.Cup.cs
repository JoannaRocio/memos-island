using System.Collections;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using MemosIsland.Race;
using MemosIsland.Town;
using MemosIsland.UI;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>
    /// Fase 8C — Clímax y cierre (GDD §4): la Copa de la Isla en el estadio (agente de Ápice, Lalo y Pipo con collar,
    /// final contra Vera), la confesión de Anni y el plano final en la torre de Ápice. "Continuará…"
    /// </summary>
    public partial class StoryDirector
    {
        public const string Estadio = "Map_Estadio";
        public const int CupPrize = 5000;
        static readonly RectInt Track = new(2, 2, 20, 12);

        public void StartCup() => Run(Cup());

        static IEnumerator Race(RaceSetup setup, System.Action<RaceResult> got)
        {
            RaceResult result = null;
            Cutscene.End(); // durante la carrera, los controles son de la carrera
            RaceLauncher.Start(setup, r => result = r);
            while (result == null) yield return null;
            Cutscene.Begin();
            got(result);
        }

        static RaceSetup Trainer(string title, RacerSetup rival, params RaceSegmentDef[] track) => new()
        {
            format = RaceFormat.Trainer,
            title = title,
            segments = track.ToList(),
            player = new RacerSetup { name = "Vos", team = Root.State.team.Take(GameState.MaxTeamSize).ToList() },
            rivals = { rival },
        };

        static IEnumerator Announcer(params string[] pages) => Cutscene.Say("Locutor", null, pages);

        IEnumerator LoseCup(string who)
        {
            S.cupLostOn = IslandState.DateKey(GameClock.Instance != null ? GameClock.Instance.Now : System.DateTime.Now);
            yield return Announcer($"¡Y gana {who}! Una gran carrera, pero hoy no alcanzó.");
            yield return Cutscene.Narrate("(La próxima Copa es el sábado que viene. Hay tiempo para entrenar y cuidar a tus Memos.)");
        }

        IEnumerator Cup()
        {
            var player = Root.Player.Mover;
            player.Teleport(new Vector2Int(12, 5), Direction.Up);
            Root.Camera.SnapNow();
            yield return Announcer(
                "¡Bienvenidos a la Copa de la Isla, patrocinada por Ápice: el futuro de las carreras!",
                "¡Hoy se corre por el trofeo y por un premio de 5000 monedas!");

            if (!S.Has("cup_semi_done"))
            {
                // Ronda 1: agente de Ápice.
                yield return Announcer("Primera ronda: ¡{nombre} contra un agente de Ápice! Tres tramos.");
                RaceResult r1 = null;
                yield return Race(Trainer("Copa · Primera ronda",
                    RaceSetups.Rival("Agente de Ápice", ("copito", 12, true), ("zumbi", 12, true), ("bostezo", 13, true)),
                    new RaceSegmentDef("pradera", 70), new RaceSegmentDef("rio", 60), new RaceSegmentDef("arena", 70)), r => r1 = r);
                if (!r1.playerWon)
                {
                    yield return LoseCup("el agente de Ápice");
                    yield break;
                }
                yield return Announcer("¡{nombre} pasa a la semifinal!");
                yield return Semifinal();
            }

            yield return Final();
        }

        // ------------------------------------------------------------------ Semifinal: Lalo y Pipo

        IEnumerator Semifinal()
        {
            var player = Root.Player.Mover;
            player.Teleport(new Vector2Int(12, 5), Direction.Left);
            var lalo = Cutscene.SpawnNeighbor("lalo", new Vector2Int(9, 5), Direction.Right);
            var l = lalo.Data;
            yield return Announcer("Semifinal: ¡{nombre} contra Lalo, el chico del puerto!");
            yield return Cutscene.Say(l,
                "{nombre}… Tengo que ganar. Tengo que.",
                "El barco de mi papá se rompió. Sin barco no hay pesca, y sin pesca… El premio de la Copa nos salvaría.",
                "Un señor de Ápice me dio esto. Me dijo que con esto Pipo iba a correr como nunca.");
            yield return Cutscene.Narrate("(Lalo saca un collar violeta. Pipo, su Plumín, lo mira con confianza… como siempre.)");
            int answer = -1;
            yield return Cutscene.Choice("¿Qué hacés?", new[] { "¡Lalo, no!", "Quedarte callad{o/a/e}" }, i => answer = i);
            if (answer == 0)
                yield return Cutscene.Say(l, "¡No me mires así! …Es solo por hoy. Después se lo saco. Te lo prometo.");
            yield return Cutscene.Flash(new Color(0.61f, 0.24f, 1f, 0.7f), 0.1f, 0.6f);
            yield return Cutscene.Narrate("(Lalo le pone el collar. Los ojos de Pipo se apagan.)");

            var rival = RaceSetups.Rival("Lalo", ("plumin", 13, true), ("chispin", 12, false), ("topin", 12, false));
            rival.team[0].nickname = "Pipo";
            yield return Race(Trainer("Copa · Semifinal", rival,
                new RaceSegmentDef("pradera", 70), new RaceSegmentDef("montana", 60), new RaceSegmentDef("arena", 70)), _ => { });

            // Pipo colapsó en plena pista (GDD §4).
            player = Root.Player.Mover;
            player.Teleport(new Vector2Int(12, 5), Direction.Left);
            Root.Camera.SnapNow();
            Cutscene.ReleaseNeighbor(lalo);
            lalo = Cutscene.SpawnNeighbor("lalo", new Vector2Int(9, 5), Direction.Down);
            var pipo = MemoInstance.Create("plumin", 13, trust: 0);
            pipo.nickname = "Pipo";
            pipo.collared = true;
            var go = Instantiate(Resources.Load<GameObject>("MemoActor"), GridMover.CellToWorld(new Vector2Int(9, 9)), Quaternion.identity);
            go.name = "Pipo";
            var mover = go.GetComponent<GridMover>();
            var view = go.GetComponentInChildren<MemoSpriteView>();
            view.Setup(mover, pipo);
            view.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            var bubble = go.GetComponentInChildren<EmoteBubble>();

            yield return Cutscene.Narrate(
                "(En el primer tramo, Pipo salió disparado como un rayo. Nadie lo podía alcanzar.)",
                "(Y en plena pista… se desplomó.)");
            yield return Announcer("¡Atención! ¡El Plumín de Lalo cayó! ¡La carrera se detiene!");
            yield return Cutscene.WalkTo(lalo.Mover, new Vector2Int(9, 8), Direction.Down, run: true);
            view.transform.localRotation = Quaternion.identity;
            yield return Cutscene.Say(l,
                "¡Pipo! ¡Pipo, soy yo! Mirá… tu pelotita. La que te regalé cuando eras chiquito…");
            bubble.Show(Emote.Dots, 1.6f);
            yield return Cutscene.Narrate("(Pipo mira la pelota. Y la ignora. Mira a Lalo como si fuera un desconocido.)");
            yield return Cutscene.Say(l, "No… no, no, no. Pipo… ¿no me conocés?");
            yield return Cutscene.WalkTo(lalo.Mover, new Vector2Int(6, 6), Direction.Right);
            yield return Cutscene.Narrate(
                "(Saltás a la pista. Como con tu primer Memo: quiet{o/a/e}, despacio, sin apurarte.)");

            bool calmed = false;
            var game = go.AddComponent<CalmMinigame>();
            game.Setup(pipo, Track, _ => calmed = true);
            Cutscene.End();
            while (!calmed) yield return null;
            Cutscene.Begin();

            yield return Cutscene.Narrate("(Pipo huele la bufanda del abuelo. Se queda quieto. Tiembla.)");
            yield return Cutscene.Flash(new Color(0.84f, 0.65f, 1f, 0.95f), 0.15f, 0.9f);
            pipo.collared = false;
            view.Setup(mover, pipo);
            bubble.Show(Emote.Surprise, 1.2f);
            yield return Cutscene.Narrate("(¡El collar se rompe!)");
            yield return Cutscene.Wait(0.6f);
            bubble.Show(Emote.Love, 2f);
            yield return Cutscene.WalkTo(mover, lalo.Mover.Cell + Vector2Int.right, Direction.Left, run: true);
            yield return Cutscene.Narrate("(Pipo corre hacia Lalo. Lo reconoce.)");
            yield return Cutscene.Say(l,
                "Pipo… Pipo, perdoname. Perdoname.",
                "Solo quería ser suficiente.");
            answer = -1;
            yield return Cutscene.Choice("¿Qué le decís?", new[] { "Siempre lo fuiste", "Para Pipo, sí" }, i => answer = i);
            yield return Cutscene.Say(l, answer == 0
                ? "…Vos siempre fuiste así. Desde el muelle."
                : "…Sí. Para él sí. Gracias, {nombre}.");
            yield return Cutscene.Say(l,
                "Me bajo de la Copa. No me la merezco. Ganala vos. Por los dos.",
                "Tomá… el collar. No lo quiero ver nunca más.");
            Root.State.AddItem("collar_roto");
            yield return Cutscene.Narrate("(Recibiste el collar de Pipo.)");
            NeighborFriendship.Add(Root.State.town.Get("lalo"), 100);
            Destroy(go);
            Cutscene.ReleaseNeighbor(lalo);
            S.Set("cup_semi_done");
            yield return Announcer("Lalo se retira… ¡{nombre} pasa a la gran final!");
        }

        // ------------------------------------------------------------------ Final: Vera

        IEnumerator Final()
        {
            var player = Root.Player.Mover;
            player.Teleport(new Vector2Int(12, 5), Direction.Right);
            Root.Camera.SnapNow();
            var vera = Cutscene.SpawnNeighbor("vera", new Vector2Int(14, 5), Direction.Left);
            yield return Announcer("¡La gran final! Seis tramos. ¡{nombre} contra la Capitana Vera y lo mejor de la isla!");
            yield return Cutscene.Say(vera.Data,
                "Así que llegaste. Bien.",
                "No te voy a regalar nada. Pero vi lo que hiciste en la pista con ese Plumín.",
                "Corramos limpio.");
            Cutscene.ReleaseNeighbor(vera);

            RaceResult result = null;
            yield return Race(new RaceSetup
            {
                format = RaceFormat.Cup,
                title = "Copa de la Isla · Final",
                segments =
                {
                    new RaceSegmentDef("pradera", 70), new RaceSegmentDef("barro", 60), new RaceSegmentDef("rio", 60),
                    new RaceSegmentDef("hielo", 60), new RaceSegmentDef("montana", 60), new RaceSegmentDef("arena", 70),
                },
                player = new RacerSetup { name = "Vos", team = Root.State.team.Take(GameState.MaxTeamSize).ToList() },
                rivals =
                {
                    RaceSetups.Rival("Capitana Vera", ("tuerquita", 15, false), ("ferrolobo", 15, false), ("imanta", 16, false)),
                    RaceSetups.Rival("Agente de Ápice", ("copito", 14, true), ("zumbi", 14, true), ("chispin", 14, true)),
                    RaceSetups.Rival("Corredor del puerto", ("topin", 13, false), ("pantuflo", 13, false), ("plumin", 13, false)),
                },
            }, r => result = r);
            if (!result.playerWon)
            {
                yield return LoseCup("la Capitana Vera");
                yield break;
            }

            player = Root.Player.Mover;
            player.Teleport(new Vector2Int(12, 5), Direction.Right);
            Root.Camera.SnapNow();
            vera = Cutscene.SpawnNeighbor("vera", new Vector2Int(14, 5), Direction.Left);
            yield return Announcer("¡¡{nombre} gana la Copa de la Isla!! ¡Increíble!");
            yield return Cutscene.Say(vera.Data,
                "Ganaste limpio. Y con Memos que confían en vos. Eso no lo compra ningún patrocinador.",
                "Hace tiempo que desconfío de Ápice. Sus corredores con collar no corren: obedecen.",
                "Desde hoy, contá conmigo. Para lo que sea.");
            NeighborFriendship.Add(Root.State.town.Get("vera"), 100);
            Root.State.island.money += CupPrize;
            yield return Cutscene.Narrate($"(¡Ganaste el trofeo de la Copa y {CupPrize} monedas!)");
            Cutscene.ReleaseNeighbor(vera);
            S.Set("cup_won");
            yield return Ending();
        }

        // ------------------------------------------------------------------ Cierre de la demo

        IEnumerator Ending()
        {
            var player = Root.Player.Mover;
            var anni = Cutscene.SpawnNeighbor("anni", player.Cell + new Vector2Int(2, 0), Direction.Left);
            var a = anni.Data;
            yield return Cutscene.Say(a,
                "{nombre}… felicitaciones. De verdad.",
                "¿Me prestás un segundo el collar de Pipo? Hay algo que tengo que ver.");
            yield return Cutscene.Narrate("(Anni lo mira de cerca, dándolo vuelta. Se le llenan los ojos de lágrimas.)");
            yield return Cutscene.Say(a, "Mirá. Adentro del metal. El grabado.");
            yield return Cutscene.Narrate("(Es una firma. La conocés de la carta y del diario: es la firma del abuelo.)");
            yield return Cutscene.Say(a,
                "Él lo inventó para calmar a los Memos heridos. A los que tenían recuerdos horribles. Borraba solo el dolor.",
                "…No para esto.",
                "Lo hicimos juntos. Yo lo ayudé. Y Ápice se lo llevó y lo convirtió en esto: un collar que borra todo.",
                "Perdoname por no habértelo dicho antes. Tenía miedo. Ya no.");
            Cutscene.ReleaseNeighbor(anni);
            yield return Root.Fader.Fade(1f, 0.8f);
            yield return Tower();
            S.Set("chapter1_done");
            SetObjective("Fin del Capítulo 1. Seguí cuidando la isla y a tus Memos… el viaje continúa.");
            yield return Root.Fader.Fade(0f, 0.8f);
            yield return Cutscene.Narrate($"Nuevo objetivo: {S.objective}");
        }

        /// <summary>Último plano (GDD §4): la torre de Ápice, el Director Sílex, la figura con la bufanda y el inicial no elegido.</summary>
        IEnumerator Tower()
        {
            var ui = new UiKit("Tower");
            UiKit.FullScreens++;
            ui.Rect(0, 0, 260, 150, new Color32(0x1a, 0x1c, 0x2c, 0xff), 950);
            // Ventanal con la tormenta del continente.
            ui.Rect(0, 20, 220, 80, new Color32(0x29, 0x36, 0x6f, 0xff), 951);
            for (int i = 0; i < 5; i++) ui.Rect(-88 + i * 44, 20, 2, 80, new Color32(0x1a, 0x1c, 0x2c, 0xff), 952);
            // Pantalla con tu foto.
            ui.Rect(-62, 36, 60, 46, new Color32(0x33, 0x3c, 0x57, 0xff), 953);
            ui.Rect(-62, 36, 54, 40, new Color32(0x41, 0xa6, 0xf6, 0xff), 954);
            var frames = PlayerLook.Frames(S.profile);
            var photo = ui.Sprite(frames != null ? frames[0][0] : null, -62, 18, 955);
            photo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            // Sílex de espaldas y la figura con bufanda en las sombras.
            var silex = ui.Sprite(StoryArtSet.Instance != null ? StoryArtSet.Instance.silex : null, 20, -22, 956);
            silex.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            var figure = ui.Sprite(StoryArtSet.Instance != null ? StoryArtSet.Instance.scarfFigure : null, 82, -22, 956);
            figure.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            figure.color = new Color(1f, 1f, 1f, 0f);

            yield return Root.Fader.Fade(0f, 0.8f);
            yield return Cutscene.Narrate("(Mientras tanto, en el continente. Una torre de Ápice, en lo más alto.)");
            yield return Cutscene.Say("Director Sílex", null,
                "Así que esta es {el nieto/la nieta/le niete} del viejo. Rompió tres collares. Y ganó nuestra Copa.",
                "Interesante. Muy interesante.");
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                figure.color = new Color(1f, 1f, 1f, t / 1.5f);
                yield return null;
            }
            yield return Cutscene.Narrate("(Detrás de él, en las sombras, alguien lleva una bufanda roja. La misma que la del abuelo.)");

            var unchosen = JournalScreen.Pages.Select(p => p.speciesId).FirstOrDefault(id => id != S.starterId);
            var species = MemoDatabase.Instance.GetSpecies(unchosen);
            if (species != null && species.collarWorldFrames is { Length: > 0 })
            {
                var caged = ui.Sprite(species.collarWorldFrames[0], -100, -20, 956);
                caged.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                yield return Cutscene.Narrate($"(Y en un rincón, con un collar violeta y la mirada vacía… {species.displayName}.)");
            }

            yield return Root.Fader.Fade(1f, 1f);
            ui.Destroy();
            var end = new UiKit("Continuara");
            end.Rect(0, 0, 260, 150, new Color32(0x1a, 0x1c, 0x2c, 0xff), 950);
            var text = end.Text(0, 8, UiKit.Gold, 960);
            text.transform.localScale = new Vector3(2f, 2f, 1f);
            text.SetText("Continuará…");
            end.Center(text, 0, 10);
            yield return Root.Fader.Fade(0f, 1f);
            yield return Cutscene.Wait(2.5f);
            yield return Cutscene.Narrate("(En la MemoBox, dos fichas siguen borrosas: «Zona inaccesible: próximamente.»)");
            Root.MemoBox.Open("draken");
            Cutscene.End();
            while (Root.MemoBox.IsOpen) yield return null;
            Cutscene.Begin();
            yield return Root.Fader.Fade(1f, 0.6f);
            end.Destroy();
            UiKit.FullScreens--;
        }
    }
}
