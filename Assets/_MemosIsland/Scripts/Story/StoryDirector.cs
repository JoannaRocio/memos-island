using System;
using System.Collections;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.UI;
using MemosIsland.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemosIsland.Story
{
    /// <summary>
    /// La historia del Capítulo 1 (GDD §4). Fase 8A: Actos 1 y 2 — llegada con Lalo y la carta, el refugio vacío,
    /// el diario y la elección de la página, el Bosque Susurro con el minijuego de calma, el collar que se quiebra,
    /// Anni que lo cura, y el objetivo de que el inicial salga de su escondite.
    /// Solo corre en una partida de historia (Nueva partida); la partida de prueba no tiene escenas.
    /// </summary>
    public class StoryDirector : MonoBehaviour
    {
        public const string Pueblo = "Map_PuebloPuerto";
        public const string RefugioExterior = "Map_RefugioExterior";
        public const string RefugioInterior = "Map_RefugioInterior";
        public const string Bosque = "Map_BosqueSusurro";

        /// <summary>El claro del Bosque Susurro, donde está el inicial con collar.</summary>
        public static readonly RectInt Clearing = new(11, 15, 10, 6);
        public static readonly Vector2Int ClearingMemoCell = new(15, 19);

        static GameRoot Root => GameRoot.Instance;
        static StoryState S => Root.State.story;

        bool _running;
        float _checkTimer;

        void OnEnable()
        {
            MapManager.SceneEntered += OnSceneEntered;
            MapManager.PlayerStepped += OnPlayerStepped;
        }

        void OnDisable()
        {
            MapManager.SceneEntered -= OnSceneEntered;
            MapManager.PlayerStepped -= OnPlayerStepped;
        }

        void Run(IEnumerator beat)
        {
            if (_running) return;
            StartCoroutine(Wrap(beat));
        }

        IEnumerator Wrap(IEnumerator beat)
        {
            _running = true;
            Cutscene.Begin();
            yield return beat;
            Cutscene.End();
            _running = false;
        }

        void OnSceneEntered(string scene)
        {
            if (!S.active) return;
            if (scene == Pueblo && !S.Has("arrived")) Run(Arrival());
            else if (scene == RefugioExterior && S.Has("arrived") && !S.Has("refuge_seen")) Run(RefugeFirstLook());
            else if (scene == RefugioInterior && S.Has("rescue_pending")) Run(AnniHeals());
            else if (scene == RefugioInterior && S.Has("arrived") && !S.Has("journal")) Run(Journal());
            else if (scene == Bosque && S.Has("journal") && !S.Has("forest_seen")) Run(ForestFirstLook());
        }

        void OnPlayerStepped(Vector2Int cell)
        {
            if (!S.active || _running || SceneManager.GetActiveScene().name != Bosque) return;
            if (S.Has("journal") && !S.Has("rescued") && !S.Has("rescue_pending") && Clearing.Contains(cell)) Run(Rescue());
        }

        void Update()
        {
            if (!S.active || _running || !S.Has("rescued") || S.Has("starter_out")) return;
            if ((_checkTimer -= Time.deltaTime) > 0f) return;
            _checkTimer = 2f;
            var starter = Root.State.Find(S.starterUid);
            if (starter != null && starter.TrustLevel >= TrustLevel.Distrust && !GameRoot.InputLocked) Run(StarterOut(starter));
        }

        // ------------------------------------------------------------------ Objetivo

        public static void SetObjective(string text) => S.objective = TextTags.Apply(text, S.profile);

        static IEnumerator NewObjective(string text)
        {
            SetObjective(text);
            yield return Cutscene.Narrate($"Nuevo objetivo: {S.objective}");
        }

        static string StarterName => JournalScreen.PageFor(S.starterId)?.name ?? "tu Memo";

        // ------------------------------------------------------------------ Acto 1

        IEnumerator Arrival()
        {
            yield return Cutscene.Wait(1.2f);
            yield return Cutscene.Narrate("(Después de un largo viaje en barco, llegás a Pueblo Puerto. El muelle cruje bajo tus pies.)");
            var player = Root.Player.Mover;
            var lalo = Cutscene.SpawnNeighbor("lalo", new Vector2Int(22, 10), Direction.Down);
            if (lalo != null)
            {
                lalo.Bubble?.Show(Emote.Surprise, 1.2f);
                yield return Cutscene.WalkTo(lalo.Mover, player.Cell + Vector2Int.up, Direction.Down, run: true);
                player.Facing = Direction.Up;
                var n = lalo.Data;
                yield return Cutscene.Say(n,
                    "¡{nombre}! ¡Llegaste! ¡Sabía que ibas a venir!",
                    "Soy yo, Lalo. ¿Te acordás? Jugábamos a las carreras en este mismo muelle. Yo ganaba siempre. Bueno… casi siempre.",
                    "…Lo de tu abuelo. Lo siento mucho. Ya pasó un año y nadie sabe nada. Desapareció la misma noche que se llevaron a sus tres Memos.",
                    "Esto llegó al correo del pueblo con tu nombre. Es su letra.");
                yield return Cutscene.Narrate("(Lalo te da una carta. Reconocés la letra del abuelo.)");
                yield return Cutscene.Say("Carta del abuelo", null,
                    "Querid{o/a/e} {nombre}:",
                    "Si estás leyendo esto, es porque no pude volver. Te dejo el refugio: la casa, la huerta y todo lo que hay adentro.",
                    "Cuidalo. Y si podés… cuidalos a ellos. Vas a entender cuando leas mi diario.",
                    "Con todo mi cariño, el abuelo.");
                yield return Cutscene.Say(n,
                    "El refugio está al este, siguiendo la calle principal. Está medio abandonado, pero es tuyo.",
                    "¡Ah! Tomá, unas bayamemos. A los Memos les encantan. Nunca se sabe a quién te vas a cruzar.");
                Root.State.AddItem("bayamemo", 3);
                yield return Cutscene.Narrate("(Recibiste 3 bayamemos.)");
                yield return Cutscene.Say(n, "¡Nos vemos! Si necesitás algo, ando por el muelle o la plaza. ¡Bienvenid{o/a/e} a casa!");
                Root.State.town.Get("lalo").met = true;
                yield return Cutscene.WalkTo(lalo.Mover, new Vector2Int(22, 10));
                Cutscene.ReleaseNeighbor(lalo);
            }
            S.Set("arrived");
            yield return NewObjective("Ir al refugio del abuelo, al este del pueblo.");
        }

        IEnumerator RefugeFirstLook()
        {
            yield return Cutscene.Wait(0.8f);
            yield return Cutscene.Narrate(
                "(El refugio del abuelo. Los yuyos crecieron por todos lados y está todo en silencio.)",
                "(Ni un solo Memo. Ni uno.)");
            S.Set("refuge_seen");
            yield return NewObjective("Entrar a la casa del refugio.");
        }

        IEnumerator Journal()
        {
            yield return Cutscene.Wait(0.6f);
            yield return Cutscene.Narrate(
                "(Adentro todo está lleno de polvo. Sobre la mesa hay un cuaderno con tapa de cuero.)",
                "(Es el diario del abuelo. Tres páginas están arrancadas a medias: de cada una queda una silueta borroneada y unas líneas escritas a mano.)");
            string chosen = null;
            yield return JournalScreen.Run(S.profile, id => chosen = id);
            S.starterId = chosen;
            yield return Cutscene.Narrate(
                "(Al pie de las páginas, el abuelo escribió: «Cuidé a estos tres hasta la noche en que se los llevaron. Si alguna vez lees esto… encontrá al menos a uno.»)",
                $"(En el borde de la página de {StarterName} hay un dibujito: un claro en el Bosque Susurro, al norte del refugio.)");
            S.Set("journal");
            yield return NewObjective("Seguir la pista del diario: el claro del Bosque Susurro, al norte del refugio.");
        }

        // ------------------------------------------------------------------ Acto 2

        IEnumerator ForestFirstLook()
        {
            yield return Cutscene.Wait(0.8f);
            yield return Cutscene.Narrate(
                "(El Bosque Susurro. Las hojas se mueven aunque no haya viento.)",
                "(Según el dibujo del diario, el claro está al fondo, hacia el norte.)");
            S.Set("forest_seen");
        }

        IEnumerator Rescue()
        {
            var memo = MemoInstance.Create(S.starterId, 5, trust: 60);
            memo.collared = true;
            var prefab = Resources.Load<GameObject>("MemoActor");
            var go = Instantiate(prefab, GridMover.CellToWorld(ClearingMemoCell), Quaternion.identity);
            go.name = "Inicial con collar";
            var mover = go.GetComponent<GridMover>();
            var view = go.GetComponentInChildren<MemoSpriteView>();
            view.Setup(mover, memo);
            mover.Facing = Direction.Down;
            var bubble = go.GetComponentInChildren<EmoteBubble>();
            bubble.Show(Emote.Angry, 1.5f);

            yield return Cutscene.Wait(0.5f);
            yield return Cutscene.Narrate(
                $"(¡Ahí está! Es {StarterName}… pero tiene un collar violeta en el cuello y los ojos vacíos.)",
                "(Está herido. Te gruñe y lanza zarpazos al aire. No ataca por maldad: ataca por miedo.)",
                "(Si te movés rápido, se va a escapar. Quedate quiet{o/a/e}, acercate despacio y ofrecele comida.)");

            string food = null;
            bool calmed = false;
            var game = go.AddComponent<CalmMinigame>();
            game.Setup(memo, Clearing, f =>
            {
                food = f;
                calmed = true;
            });
            Cutscene.End(); // el jugador se mueve durante el minijuego
            while (!calmed) yield return null;
            Cutscene.Begin();

            var foodName = food != null ? MemoDatabase.Instance.GetItem(food)?.displayName.ToLowerInvariant() : null;
            yield return Cutscene.Narrate(
                foodName != null
                    ? $"(Le ofrecés la {foodName} con la mano abierta. {StarterName} gruñe… y de pronto se queda quieto, olfateando.)"
                    : $"(Extendés la mano, despacio. {StarterName} gruñe… y de pronto se queda quieto, olfateando.)",
                "(Huele la bufanda del abuelo. La reconoce.)",
                "(De algún lado suena una cajita de música: la melodía que el abuelo tarareaba siempre.)");
            yield return Cutscene.Flash(new Color(0.84f, 0.65f, 1f, 0.95f), 0.15f, 0.9f);
            memo.collared = false;
            view.Setup(mover, memo);
            bubble.Show(Emote.Surprise, 1.2f);
            yield return Cutscene.Narrate("(¡El collar se quiebra y cae al pasto!)");
            view.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            bubble.Show(Emote.Sleep, 2f);
            yield return Cutscene.Narrate($"(…{StarterName} se derrumba en tus brazos, agotado. Hay que llevarlo al refugio.)");

            memo.rescued = true;
            memo.inside = true;
            memo.highestTrust = TrustLevel.Fear;
            Root.State.AddMemo(memo);
            S.starterUid = memo.uid;
            S.Set("rescue_pending");
            Destroy(go);
            Root.Maps.GoTo(RefugioInterior, "default");
        }

        IEnumerator AnniHeals()
        {
            yield return Cutscene.Wait(0.4f);
            var player = Root.Player.Mover;
            var anni = Cutscene.SpawnNeighbor("anni", player.Cell + new Vector2Int(0, 3), Direction.Down);
            player.Facing = Direction.Up;
            yield return Cutscene.Narrate("(Más tarde. Anni, la veterinaria del pueblo, vino corriendo apenas le avisaron.)");
            if (anni != null)
                yield return Cutscene.Say(anni.Data,
                    "Tranqui, tranqui… Ya está. Lo revisé de pies a cabeza.",
                    "Tiene raspones y está muy cansado, pero se va a poner bien. Lo que más le duele no se ve: tiene mucho miedo.",
                    "Ese collar… Prefiero no hablar de eso ahora.",
                    "Va a esconderse un tiempo. No lo fuerces. Quedate cerca, quiet{o/a/e}, y dejale comida. Que sepa que acá nadie le va a hacer daño.",
                    "Si me necesitás, estoy en la clínica del pueblo. ¡Cuidalo mucho, {nombre}!");
            yield return Root.Fader.Fade(1f, 0.4f);
            Cutscene.ReleaseNeighbor(anni);
            Root.State.town.Get("anni").met = true;
            yield return Root.Fader.Fade(0f, 0.4f);
            S.flags.Remove("rescue_pending");
            S.Set("rescued");
            yield return NewObjective($"Lograr que {StarterName} salga de su escondite: quedate cerca y dale de comer.");
        }

        IEnumerator StarterOut(MemoInstance starter)
        {
            yield return Cutscene.Narrate(
                $"(¡{starter.DisplayName} se asomó de su escondite y te miró a los ojos por primera vez!)",
                "(Todavía desconfía, pero ya no se esconde. Es un comienzo.)");
            S.Set("starter_out");
            yield return NewObjective($"Conocer la isla: hablar con los vecinos, cuidar la huerta y a {starter.DisplayName}.");
        }
    }
}
