using System;
using System.Collections;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Town;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>
    /// Fase 8B — Acto 3 "La isla y Ápice" (GDD §4): Zorak enseña a correr a la forma antigua, el primer Memo con collar,
    /// Fer odia a Ápice y ayuda en secreto, Luca empieza a dudar, el momento duro en el Pantano, Anni esconde algo,
    /// Marga mira para otro lado, Zorak y la foto de Draken, y Karman en las tormentas de los Acantilados.
    /// </summary>
    public partial class StoryDirector
    {
        public const string Ruta = "Map_RutaPradera";
        public const string Pantano = "Map_PantanoPantufla";
        public const string Colina = "Map_ColinaEscarcha";
        public const string Acantilados = "Map_AcantiladosTormenta";

        static NeighborData Neighbor(string id) => MemoDatabase.Instance.GetNeighbor(id);

        /// <summary>
        /// Al hablarle a un vecino: si le toca una escena de la historia, la corre y devuelve true
        /// (NeighborTalk no sigue con la charla normal).
        /// </summary>
        public bool TryNeighborBeat(NeighborNpc npc, Action done)
        {
            if (!S.active || _running) return false;
            IEnumerator beat = npc.Data.id switch
            {
                "zorak" when S.Has("starter_out") && !S.Has("zorak_lesson") => ZorakLesson(npc),
                "zorak" when S.Has("marga_away") && !S.Has("zorak_photo") => ZorakPhoto(npc),
                "fer" when S.Has("first_collar") && !S.Has("fer_reveal") => FerReveal(npc),
                "luca" when S.Has("fer_reveal") && !S.Has("luca_doubt") => LucaDoubt(npc),
                "anni" when S.Has("hard_moment") && !S.Has("anni_hides") => AnniHides(npc),
                "marga" when S.Has("anni_hides") && !S.Has("marga_away") => MargaAway(npc),
                _ => null,
            };
            if (beat == null) return false;
            Run(Then(beat, done));
            return true;
        }

        static IEnumerator Then(IEnumerator beat, Action done)
        {
            yield return beat;
            done?.Invoke();
        }

        void OnSceneEnteredAct3(string scene)
        {
            if (scene == Ruta && S.Has("zorak_lesson") && !S.Has("ruta_seen")) Run(RutaFirstLook());
            else if (scene == Pantano && S.Has("luca_doubt") && !S.Has("hard_moment")) Run(HardMoment());
        }

        void OnStoryMemoCaptured(string flag, MemoInstance memo)
        {
            if (!S.active) return;
            if (flag == "first_collar") Run(FirstCollar(memo));
            else if (flag == "karman") Run(KarmanJoins(memo));
        }

        // ------------------------------------------------------------------ Zorak y la Ruta Pradera

        IEnumerator ZorakLesson(NeighborNpc npc)
        {
            var z = npc.Data;
            var starter = Root.State.Find(S.starterUid);
            string name = starter != null ? starter.DisplayName : "tu Memo";
            yield return Cutscene.Say(z,
                "…Así que vos sos {el nieto/la nieta/le niete} del viejo. Tenés su misma cara de terco.",
                $"Y ese es {name}. Lo conozco. Lo vi crecer en ese refugio.",
                "Escuchá, porque lo digo una sola vez. Hoy todos corren con collares y con plata. Yo te voy a enseñar a correr a la forma antigua.");
            yield return Cutscene.Say(z,
                "Uno: cada Memo tiene su terreno. En el suyo vuela; en el que no le gusta, se arrastra. Mirá las flechitas de la pista.",
                "Dos: cuando cambia el terreno, cambiá de corredor con B. Una carrera de verdad se corre en equipo, no con uno solo.",
                "Tres, y es lo único que importa: un Memo no corre por la comida. Corre porque confía en que vas a estar en la meta.");
            int answer = -1;
            yield return Cutscene.Choice("¿Qué le decís?", new[] { "Voy a estar", "¿Y si pierdo?" }, i => answer = i, z.displayName, z.portrait);
            yield return Cutscene.Say(z, answer == 0
                ? "…Eso mismo decía tu abuelo. Bien."
                : "Vas a perder. Muchas veces. Y vas a volver a estar ahí igual. Eso es correr.");
            yield return Cutscene.Say(z,
                "Al este del refugio está la Ruta Pradera. Saqué el tronco que tapaba el paso.",
                "Andá. Conseguí un segundo Memo. Y si ves uno con collar… no lo dejes ahí.");
            NeighborFriendship.Add(Root.State.town.Get("zorak"), 40);
            S.Set("zorak_lesson");
            S.Set("route_open");
            yield return NewObjective("Explorar la Ruta Pradera, al este del refugio, y conseguir un segundo Memo.");
        }

        IEnumerator RutaFirstLook()
        {
            yield return Cutscene.Wait(0.8f);
            yield return Cutscene.Narrate(
                "(La Ruta Pradera. El pasto alto se mueve con el viento… y algo violeta brilla más adelante, en el camino.)");
            S.Set("ruta_seen");
        }

        IEnumerator FirstCollar(MemoInstance memo)
        {
            yield return Cutscene.Wait(0.4f);
            Root.State.AddItem("collar_roto");
            yield return Cutscene.Narrate(
                $"(Del collar de {memo.DisplayName} quedaron dos pedazos de metal violeta. Los guardás en la mochila.)",
                "(Tiene una marca grabada: un triángulo. ¿Quién fabrica algo así?)");
            yield return NewObjective("Mostrarle el collar roto a alguien que sepa de metales: Fer, en la herrería del pueblo.");
        }

        // ------------------------------------------------------------------ Fer, Luca y el Pantano

        IEnumerator FerReveal(NeighborNpc npc)
        {
            var f = npc.Data;
            yield return Cutscene.Narrate("(Le mostrás a Fer los pedazos del collar.)");
            yield return Cutscene.Say(f,
                "…Cerrá la puerta.",
                "Este triángulo es la marca de Ápice. Los de la oficina del muelle. Los que patrocinan la Copa.",
                "Hace años le cerraron el taller a mi viejo porque no quiso fabricarles piezas. Piezas como esta.",
                "No sé qué le hacen a los Memos con esto. Pero si se lo sacaste a uno, hiciste bien.");
            yield return Cutscene.Say(f,
                "Tomá. Herraduras. Para que tus Memos corran mejor. No me debés nada.",
                "Y si encontrás más collares, traelos. Pero no le digas a nadie que te lo dije. A nadie.");
            Root.State.AddItem("herraduras");
            yield return Cutscene.Narrate("(Recibiste herraduras.)");
            yield return Cutscene.Say(f, "Una cosa más: Luca, el pibe de Ápice, anda raro. Pasa las tardes en el muelle mirando el agua. Hablá con él.");
            NeighborFriendship.Add(Root.State.town.Get("fer"), 60);
            S.Set("fer_reveal");
            yield return NewObjective("Hablar con Luca, el empleado de Ápice (de día en la oficina del muelle, a la tarde en la punta del muelle).");
        }

        IEnumerator LucaDoubt(NeighborNpc npc)
        {
            var l = npc.Data;
            yield return Cutscene.Say(l,
                "Ah… hola. Perdón, estaba pensando.",
                "¿Te puedo contar algo? Pero no acá, que nos ven. Bueno, sí, acá. No hay nadie.",
                "En la oficina hay cajas. Muchas. Con collares violetas adentro. Dicen que son \"calmantes\", para los Memos nerviosos.",
                "Pero ayer vi a un Memo con uno puesto. Y no estaba calmado. Estaba… vacío. Como apagado.");
            int answer = -1;
            yield return Cutscene.Choice("¿Qué le decís?", new[] { "Yo le saqué uno a un Memo", "Capaz son calmantes" }, i => answer = i, l.displayName, l.portrait);
            if (answer == 0)
                yield return Cutscene.Say(l, "¿Se lo sacaste? ¿Y… cómo está? …Volvió a ser él. ¿No? Lo sabía. Lo sabía.");
            else
                yield return Cutscene.Say(l, "…Ojalá. Ojalá tengas razón.");
            yield return Cutscene.Say(l,
                "Hay camionetas de Ápice que van al Pantano Pantufla, al sur de la Ruta Pradera. Vuelven vacías.",
                "Yo no puedo ir. Si alguien me ve… Pero vos sí.");
            NeighborFriendship.Add(Root.State.town.Get("luca"), 40);
            S.Set("luca_doubt");
            S.Set("pantano_open");
            yield return NewObjective("Investigar el Pantano Pantufla, al sur de la Ruta Pradera.");
        }

        /// <summary>GDD §4: un Memo con collar da un paso hacia el protagonista, recuerda… y el collar lo obliga a irse.</summary>
        IEnumerator HardMoment()
        {
            yield return Cutscene.Wait(0.8f);
            yield return Cutscene.Narrate("(El Pantano Pantufla. Huele a barro y a lluvia vieja. Hay huellas de ruedas en el camino.)");
            var player = Root.Player.Mover;
            var memo = MemoInstance.Create("pantuflo", 9, trust: 0);
            memo.collared = true;
            var prefab = Resources.Load<GameObject>("MemoActor");
            var start = player.Cell + new Vector2Int(0, -4);
            var go = Instantiate(prefab, GridMover.CellToWorld(start), Quaternion.identity);
            var mover = go.GetComponent<GridMover>();
            go.GetComponentInChildren<MemoSpriteView>().Setup(mover, memo);
            var bubble = go.GetComponentInChildren<EmoteBubble>();
            mover.Facing = Direction.Up;
            player.Facing = Direction.Down;

            yield return Cutscene.Narrate("(Un Pantuflo con collar aparece entre los juncos. Te mira sin brillo en los ojos.)");
            mover.TryStep(Direction.Up, false);
            while (mover.IsMoving) yield return null;
            yield return Cutscene.Wait(0.6f);
            bubble.Show(Emote.Question, 1.6f);
            yield return Cutscene.Narrate("(Da un paso hacia vos. Despacio. Olfatea el aire… como si reconociera algo. ¿La bufanda?)");
            mover.TryStep(Direction.Up, false);
            while (mover.IsMoving) yield return null;
            bubble.Show(Emote.Love, 1.4f);
            yield return Cutscene.Wait(1.2f);
            yield return Cutscene.Flash(new Color(0.61f, 0.24f, 1f, 0.6f), 0.1f, 0.6f);
            bubble.Show(Emote.Sad, 1.6f);
            yield return Cutscene.Narrate("(El collar brilla. Pantuflo se encoge, suelta un quejido… y retrocede.)");
            yield return Cutscene.WalkTo(mover, start + new Vector2Int(0, -3), null, run: true);
            Destroy(go);
            yield return Cutscene.Narrate(
                "(Se fue. Por un segundo te recordó a alguien. Y el collar no lo dejó quedarse.)",
                "(Necesitás entender qué es ese collar. Anni sabe de Memos más que nadie en la isla.)");
            S.Set("hard_moment");
            yield return NewObjective("Contarle a Anni, en la clínica, lo que viste en el Pantano.");
        }

        // ------------------------------------------------------------------ Anni, Marga y Zorak

        IEnumerator AnniHides(NeighborNpc npc)
        {
            var a = npc.Data;
            yield return Cutscene.Narrate("(Le contás a Anni lo del Pantuflo y el collar que brillaba.)");
            yield return Cutscene.Say(a,
                "¿Un collar que… brilla? ¿Que no lo dejó acercarse?",
                "…",
                "No. No sé nada de eso, tesoro. Debe ser algo nuevo de Ápice. Yo… yo curo Memos, nada más.");
            npc.Bubble?.Show(Emote.Dots, 1.6f);
            yield return Cutscene.Narrate("(Anni esquiva tu mirada. Sus manos tiemblan un poco al guardar las vendas.)");
            yield return Cutscene.Say(a,
                "Perdón, tengo un paciente esperando. ¿Por qué no hablás con la alcaldesa? Si alguien puede hacer algo, es Marga.");
            S.Set("anni_hides");
            yield return NewObjective("Hablar con la alcaldesa Marga (frente al municipio o en la plaza).");
        }

        IEnumerator MargaAway(NeighborNpc npc)
        {
            var m = npc.Data;
            yield return Cutscene.Narrate("(Le contás a Marga lo de los collares de Ápice.)");
            yield return Cutscene.Say(m,
                "Mirá. Te voy a hablar como adulta, porque te lo merecés.",
                "Ápice trajo la Copa, trabajo, el arreglo del muelle. Hay familias en este pueblo que comen gracias a eso.",
                "Un collar raro en un Memo salvaje no es una prueba de nada.");
            int answer = -1;
            yield return Cutscene.Choice("¿Qué le decís?", new[] { "Los Memos sufren", "¿Y si fuera tu Memo?" }, i => answer = i, m.displayName, m.portrait);
            yield return Cutscene.Say(m, answer == 0
                ? "…No me lo hagas más difícil. No hagamos olas, ¿sí? Hay cosas que es mejor no mirar."
                : "…Eso fue un golpe bajo. Y efectivo. Pero mi respuesta es la misma: no hagamos olas.");
            yield return Cutscene.Narrate("(Marga mira para otro lado. Pero no te dice que te detengas.)");
            S.Set("marga_away");
            yield return NewObjective("Volver a hablar con Zorak. Alguien que no le tenga miedo a Ápice.");
        }

        IEnumerator ZorakPhoto(NeighborNpc npc)
        {
            var z = npc.Data;
            yield return Cutscene.Say(z,
                "Ya sé. Marga te dijo que no hagas olas. Siempre dice eso.",
                "Vení. Te quiero mostrar algo.");
            yield return Cutscene.Narrate(
                "(Zorak saca de la campera una foto vieja y doblada. Es él, joven, sonriendo… al lado de un Memo enorme, negro, con ojos de fuego.)");
            yield return Cutscene.Say(z,
                "Draken. Mi compañero. Corrimos juntos diez años. Ganamos todo lo que había para ganar.",
                "Una noche de tormenta, hace mucho, se fue volando hacia el Volcán Dormido. Nunca volvió.",
                "Ahora el camino está tapado por un derrumbe. Dicen que hace falta una herramienta que todavía no existe en la isla.");
            yield return Cutscene.Say(z,
                "Pero escuchá esto: en los Acantilados Tormenta, cuando hay tormenta de verdad, aparece un Memo que nadie pudo alcanzar. Karman.",
                "Si querés ganarle a Ápice en su propia Copa, vas a necesitar Memos fuertes. Y Memos que confíen en vos.",
                "La Colina Escarcha queda al norte de la ruta; los Acantilados, al este. Ya pueden pasar. Esperá un día de tormenta.");
            NeighborFriendship.Add(Root.State.town.Get("zorak"), 40);
            S.Set("zorak_photo");
            S.Set("colina_open");
            S.Set("acantilados_open");
            yield return NewObjective("Explorar la Colina Escarcha y los Acantilados Tormenta. En un día de tormenta, buscar a Karman en los Acantilados.");
        }

        IEnumerator KarmanJoins(MemoInstance memo)
        {
            yield return Cutscene.Wait(0.4f);
            yield return Cutscene.Narrate(
                $"({memo.DisplayName} te sigue de lejos, con el pelaje lleno de chispas. No confía en nadie… todavía.)",
                "(Zorak tenía razón: con Memos que confíen en vos, se puede enfrentar cualquier cosa. Incluso la Copa de Ápice.)");
            S.Set("act3_done");
            yield return NewObjective("Prepararse para la Copa de la Isla: se corre los sábados en el estadio del pueblo.");
        }
    }
}
