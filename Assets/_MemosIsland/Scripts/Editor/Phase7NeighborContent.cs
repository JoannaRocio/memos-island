using System.Collections.Generic;
using MemosIsland.Town;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Contenido de los vecinos (GDD §5): gustos, diálogos, rutinas, eventos de amistad (personales, sin trama:
    /// las revelaciones de la historia llegan en la Fase 8) y pedidos del tablón.
    /// El constructor de la Fase 7 lo convierte en ScriptableObjects editables en Data/Neighbors.
    /// </summary>
    internal static class Phase7NeighborContent
    {
        internal const string Pueblo = "Map_PuebloPuerto";
        internal const string Almacen = "Map_Almacen";
        internal const string Herreria = "Map_Herreria";
        internal const string Clinica = "Map_Clinica";
        internal const string Taberna = "Map_Taberna";
        internal const string Refugio = "Map_RefugioExterior";
        const string Work = "trabajo";

        internal class Def
        {
            public string Id, Name, Role, FirstMeet;
            public string[] Loved = { }, Liked = { }, Disliked = { };
            public string LovedText, LikedText, NeutralText, DislikedText;
            public string[] Low = { }, Mid = { }, High = { }, Rain = { }, Companion = { };
            public ScheduleEntry[] Schedule = { };
            public FriendshipEvent[] Events = { };
            public RequestTemplate[] Requests = { };
        }

        static ScheduleEntry At(float hour, string scene, int x, int y, Direction facing, string activity,
            string line = null, Days days = Days.All, WeatherFilter weather = WeatherFilter.Any) =>
            new()
            {
                hour = hour, scene = scene, cell = new Vector2Int(x, y), facing = facing, activity = activity,
                line = line, days = days, weather = weather,
            };

        static ScheduleEntry Home(float hour, Days days = Days.All, WeatherFilter weather = WeatherFilter.Any) =>
            new() { hour = hour, scene = "", activity = "casa", days = days, weather = weather };

        static FriendshipEvent Event(string id, int hearts, string[] pages, string question = null,
            (string answer, int points, string reply)[] answers = null, string scene = null)
        {
            var e = new FriendshipEvent { id = id, hearts = hearts, pages = new List<string>(pages), question = question, scene = scene };
            if (answers != null)
                foreach (var (a, p, r) in answers)
                {
                    e.answers.Add(a);
                    e.answerPoints.Add(p);
                    e.replies.Add(r);
                }
            return e;
        }

        static RequestTemplate Deliver(string item, int count, int reward, string text, string thanks) =>
            new() { kind = RequestKind.Deliver, targetId = item, count = count, reward = reward, boardText = text, thanks = thanks };

        static RequestTemplate Show(string species, int reward, string text, string thanks) =>
            new() { kind = RequestKind.ShowMemo, targetId = species, reward = reward, boardText = text, thanks = thanks };

        const Direction D = Direction.Down, U = Direction.Up, L = Direction.Left, R = Direction.Right;

        internal static readonly Def[] All =
        {
            // ------------------------------------------------------------------ Deny
            new()
            {
                Id = "deny", Name = "Deny", Role = "Dueño del almacén",
                FirstMeet = "¡Uh, cara nueva! Soy Deny, el del almacén. Semillas, comida, cositas lindas… si no lo tengo, te lo consigo. Bueno, casi siempre.",
                Loved = new[] { "mermelada", "pastel_frutilla" }, Liked = new[] { "frutilla", "zapallo", "concha" },
                Disliked = new[] { "piedra", "hierba" },
                LovedText = "¡¿Mermelada casera?! Esto lo pongo en la vidriera… no, mentira, me la como hoy.",
                LikedText = "¡Mirá vos, qué detalle! Gracias, de verdad.",
                NeutralText = "Ah, mirá. Gracias, eh. Algo le voy a encontrar.",
                DislikedText = "¿Una… piedra? Bueno… sirve de pisapapeles. Supongo.",
                Low = new[]
                {
                    "Las semillas cambian según el día. Los domingos traigo zapallo, ¡no te lo pierdas!",
                    "¿Sabías que si vendés lo que cosechás en la caja de envíos, te lo pago al otro día? Negocio redondo.",
                    "El cartel de la puerta dice \"vuelvo en 5 minutos\". Lo puse hace tres años. Nunca lo saqué.",
                },
                Mid = new[]
                {
                    "Vos sí que sabés elegir verduras. Se nota que les hablás. Yo también le hablo a las mías.",
                    "Si un día ves que me olvido de cobrarte, no me avises. …No, mentira, avisame.",
                    "Me encanta cuando entrás. Es como cuando suena la campanita y es alguien que me cae bien.",
                },
                High = new[]
                {
                    "Te guardé lo mejor del pedido de la semana. No le digas a nadie.",
                    "Antes el almacén era más silencioso. Ahora entra gente que me pregunta cómo estoy. Bah, entrás vos.",
                },
                Rain = new[] { "Con lluvia no viene nadie… ¡por eso me encanta que vengas vos!", "La lluvia riega tus cultivos solita. ¡Día libre para la regadera!" },
                Companion = new[] { "¡Hola, {memo}! ¿Querés un caramelito? …No tengo. Pero la intención está." },
                Schedule = new[]
                {
                    At(8, Pueblo, 5, 18, D, "barrer", "¡Buen día! Barro la vereda antes de abrir. Bah, la miro."),
                    At(9, Almacen, 5, 5, D, Work),
                    At(17, Pueblo, 22, 15, R, "paseo", "Después de cerrar me gusta mirar quién anda por la plaza. Es mi novela."),
                    At(17, Taberna, 2, 2, U, "taberna", weather: WeatherFilter.Rainy),
                    At(19, Taberna, 2, 2, U, "taberna", "¡Acá estoy! Charla, jugo de bayamemo y chismes. En ese orden."),
                    Home(22),
                },
                Events = new[]
                {
                    Event("deny_3", 3, new[]
                    {
                        "¡Llegaste justo! Estoy ordenando el depósito y encontré esto: ¡la primera moneda que gané con el almacén!",
                        "Me la dio una nena por un caramelo. Yo tenía tu edad, más o menos… y cero idea de cómo llevar un negocio.",
                        "Todavía no tengo idea, eh. Pero la gente vuelve. Algo bien debo hacer.",
                    }, "¿Por qué creés que vuelven?", new[]
                    {
                        ("Porque sos buena onda", 40, "¡Ja! Igual sí, ¿no? Gracias. Me lo voy a escribir en un papelito."),
                        ("Porque no hay otro almacén", -10, "…Eso dolió un poquito. Pero es verdad. ¡Igual!"),
                    }, Almacen),
                    Event("deny_6", 6, new[]
                    {
                        "Te quiero mostrar algo. Mirá la vidriera.",
                        "Hice un cartelito: \"Productos de la huerta del refugio\". ¡Son tus verduras! Las vendo con nombre y todo.",
                        "La gente pregunta por vos. Dicen \"¿las de la huerta del refugio?\" y yo digo \"¡sí, de alguien muy especial!\".",
                        "…Te dije especial. Bueno. Ya está dicho.",
                    }, scene: Almacen),
                },
                Requests = new[]
                {
                    Deliver("nabo", 5, 220, "Deny: \"¡Se me terminaron los nabos! Necesito 5 para hoy.\"", "¡Salvaste el día! Bah, la tarde. Gracias."),
                    Deliver("frutilla", 3, 380, "Deny: \"Una clienta quiere 3 frutillas para una torta. ¿Tenés?\"", "¡Perfectas! Van directo a la torta. Bah, una me la como yo."),
                    Deliver("concha", 2, 160, "Deny: \"Quiero decorar la vidriera con 2 conchas de la playa.\"", "¡Quedan hermosas! La vidriera más linda del puerto."),
                },
            },

            // ------------------------------------------------------------------ Fer
            new()
            {
                Id = "fer", Name = "Fer", Role = "Herrera",
                FirstMeet = "…Fer. Herrería. Si querés mejorar herramientas, traé lingotes y plata. Si querés charlar… también se puede. Poco.",
                Loved = new[] { "lingote_hierro", "topacio" }, Liked = new[] { "lingote_cobre", "cuarzo", "mineral_hierro" },
                Disliked = new[] { "flor", "pastel_frutilla" },
                LovedText = "…Hierro bien fundido. Esto es un regalo de verdad. Gracias.",
                LikedText = "Sirve. Gracias.",
                NeutralText = "Mmh. Gracias.",
                DislikedText = "¿Para qué quiero esto? …No importa. La intención cuenta. Eso dicen.",
                Low = new[]
                {
                    "Una herramienta buena dura toda la vida. Una mala, también, pero te la hace imposible.",
                    "El pico de cobre rompe hierro y cuarzo. El de hierro, las gemas. No hay atajos.",
                    "Fundir se hace en la fundición del refugio. Tres minerales, un lingote. Paciencia.",
                },
                Mid = new[]
                {
                    "Mi papá decía que el metal escucha. Si le pegás con bronca, sale torcido.",
                    "Me gusta cuando venís. Hablás menos que el resto del pueblo.",
                    "Tu regadera estaba hecha un desastre. La próxima, traela antes.",
                },
                High = new[]
                {
                    "…Te hice un lugar en el banco del taller. Para cuando quieras mirar cómo trabajo.",
                    "No tengo muchos amigos. Tengo un yunque. Y ahora, vos.",
                },
                Rain = new[] { "Con lluvia el fuego de la forja cuesta más. Pero sale. Siempre sale." },
                Companion = new[] { "Tu {memo} tiene buenas patas. Con unas herraduras correría todavía mejor." },
                Schedule = new[]
                {
                    At(8, Herreria, 5, 5, D, Work),
                    At(13, Pueblo, 20, 17, D, "almuerzo", "Almuerzo. No molestes. …Bueno, un ratito.", weather: WeatherFilter.Sunny),
                    At(14, Herreria, 5, 5, D, Work),
                    At(18, Pueblo, 24, 5, D, "muelle", "Después de la forja, el mar. Enfría la cabeza."),
                    At(19, Taberna, 4, 3, L, "taberna", "Un vaso de jugo y silencio. Bueno, casi silencio. Deny está acá."),
                    Home(22),
                },
                Events = new[]
                {
                    Event("fer_3", 3, new[]
                    {
                        "Pasá. Estoy terminando algo.",
                        "(Fer martilla una pieza chiquita con mucho cuidado. Es una hebilla con forma de hoja.)",
                        "Era de mi papá. Se rompió hace años. La estoy rehaciendo de memoria.",
                        "No te la muestro para que digas nada. Solo… quería que alguien la viera terminada.",
                    }, "¿Qué le decís?", new[]
                    {
                        ("Le quedó igual, seguro", 40, "…No sé si igual. Pero parecida. Gracias."),
                        ("¿Me enseñás a hacer una?", 50, "…Algún día. Primero aprendé a no martillarte el dedo."),
                    }, Herreria),
                    Event("fer_6", 6, new[]
                    {
                        "Tomá. No preguntes.",
                        "(Te da un llavero de hierro con forma de Memo. Está hecho con mucho detalle.)",
                        "Me sobraba hierro. …Mentira. Lo compré para esto.",
                        "La gente que viene al taller me pide cosas. Vos me trajiste cosas. Es distinto.",
                    }, scene: Herreria),
                },
                Requests = new[]
                {
                    Deliver("mineral_cobre", 6, 260, "Fer: \"Necesito 6 minerales de cobre. De la cueva.\"", "Bien. Esto me sirve. Gracias."),
                    Deliver("piedra", 10, 120, "Fer: \"10 piedras para el horno. No pregunten para qué.\"", "Piedras. Bien. …Era para arreglar el horno, ya que preguntás."),
                    Deliver("lingote_cobre", 2, 420, "Fer: \"2 lingotes de cobre para un encargo urgente.\"", "Justo a tiempo. Buen trabajo."),
                },
            },

            // ------------------------------------------------------------------ Anni
            new()
            {
                Id = "anni", Name = "Anni", Role = "Veterinaria",
                FirstMeet = "¡Hola, tesoro! Soy Anni, la veterinaria. Si alguno de tus Memos se siente mal, o vos tenés dudas, la clínica siempre está abierta para ustedes.",
                Loved = new[] { "pastel_frutilla", "flor" }, Liked = new[] { "frutilla", "bayamemo", "pure_zapallo" },
                Disliked = new[] { "mineral_hierro", "piedra" },
                LovedText = "¡Ay, qué hermoso! Me hiciste el día, ¿sabías? Gracias, tesoro.",
                LikedText = "¡Qué rico! Gracias por pensar en mí.",
                NeutralText = "Gracias, corazón. Muy amable.",
                DislikedText = "Ay… gracias igual. Lo voy a usar para algo, seguro.",
                Low = new[]
                {
                    "Un Memo con miedo no necesita que lo apures. Necesita que te quedes. Quieto, cerca, tranquilo.",
                    "La comida favorita no es solo un premio: les dice \"te conozco\".",
                    "Si querés, traé a tu equipo y les hago una revisión. Es gratis para los vecinos.",
                },
                Mid = new[]
                {
                    "Me gusta cómo los mirás. Se nota cuando alguien quiere a los Memos de verdad.",
                    "Cuando era chica quería ser marinera. Después curé a un Plumín con el ala lastimada… y ya no hubo vuelta.",
                    "¿Comiste algo hoy? Vos también tenés que cuidarte, ¿eh?",
                },
                High = new[]
                {
                    "Sos de las personas que hacen que esta isla valga la pena. Te lo digo en serio.",
                    "Cuando estoy cansada pienso en tus Memos jugando en el refugio. Se me pasa todo.",
                },
                Rain = new[] { "Los días de lluvia los Memos duermen más. ¡Igual que yo!", "Con lluvia vienen menos pacientes. Aprovecho y ordeno las vendas." },
                Companion = new[] { "¡Hola, {memo}! Qué brillo tenés en los ojos hoy. Se nota que te cuidan bien.", "{memo} tiene el pelaje (o las plumas, o lo que sea) precioso. ¡Bien hecho!" },
                Schedule = new[]
                {
                    At(7, Pueblo, 26, 16, D, "paseo", "Antes de abrir doy una vuelta. Saludo a los Memos de la plaza… aunque se escondan."),
                    At(7, Clinica, 4, 5, D, Work, weather: WeatherFilter.Rainy),
                    At(9, Clinica, 4, 5, D, Work),
                    At(18, Pueblo, 23, 6, D, "muelle", "El atardecer desde el muelle es mi momento favorito del día."),
                    At(18, Clinica, 4, 5, D, "ordenar", "Con esta lluvia me quedo un rato más ordenando.", weather: WeatherFilter.Rainy),
                    Home(20),
                },
                Events = new[]
                {
                    Event("anni_3", 3, new[]
                    {
                        "¿Tenés un minuto? Mirá este cuaderno.",
                        "Anoto cada Memo que atendí. Nombre, qué tenía, cómo se curó. Ya van… novecientos y pico.",
                        "Cuando un día sale mal, lo abro en cualquier página. Siempre hay uno que salió bien.",
                    }, "¿Qué le decís?", new[]
                    {
                        ("Es un cuaderno precioso", 40, "¿Viste? Algún día te muestro la página de tus Memos."),
                        ("Deberías descansar más", 30, "Ja, eso me dice todo el mundo. Pero gracias por preocuparte."),
                    }),
                    Event("anni_6", 6, new[]
                    {
                        "Tesoro, vení que te quiero dar algo.",
                        "Es una cajita de vendas, gasas y un ungüento que hago yo. Para el refugio.",
                        "No es que no confíe en tus cuidados, ¡al contrario! Es que… me quedo más tranquila sabiendo que lo tenés.",
                        "Y si un día pasa algo, de noche, lo que sea: golpeás la puerta de la clínica. ¿Prometido?",
                    }, "¿Prometés?", new[]
                    {
                        ("Prometido", 50, "Así me gusta. Ahora andá, que tus Memos te esperan."),
                    }, Clinica),
                },
                Requests = new[]
                {
                    Deliver("hierba", 5, 150, "Anni: \"Necesito 5 hierbas frescas para un ungüento.\"", "¡Gracias, tesoro! Con esto alcanzo para todo el mes."),
                    Deliver("bayamemo", 3, 330, "Anni: \"3 bayamemos para un paciente que no quiere comer.\"", "Ya le ofrecí una… ¡y se la comió! Gracias, de corazón."),
                    Show("plumin", 180, "Anni: \"Quiero revisar a un Plumín sano para comparar. ¿Me traés uno?\"", "¡Qué alas fuertes! Gracias por traerlo. Está perfecto."),
                },
            },

            // ------------------------------------------------------------------ Lalo
            new()
            {
                Id = "lalo", Name = "Lalo", Role = "Amigo de la infancia",
                FirstMeet = "¡Eeeh, volviste! ¿Te acordás de mí? ¡Lalo! Jugábamos a las carreras en el muelle. Yo ganaba siempre. Bueno, casi siempre.",
                Loved = new[] { "zanahoria", "pelota" }, Liked = new[] { "bayamemo", "fruto_silvestre", "pluma" },
                Disliked = new[] { "cuarzo", "mineral_cobre" },
                LovedText = "¡¿En serio?! ¡Sos lo más! Esto lo comparto con Pipo. …Bueno, la mitad.",
                LikedText = "¡Buenísimo! Gracias, crack.",
                NeutralText = "¡Ah, gracias! Algo le voy a hacer.",
                DislikedText = "Eh… ¿esto se come? ¿No? Ah. Bueno. Gracias igual.",
                Low = new[]
                {
                    "¡Algún día te gano una carrera de verdad! Hoy no, eh. Hoy estoy cansado. Mañana.",
                    "Mi papá pesca desde que sale el sol. Yo lo ayudo con las redes… cuando me despierto.",
                    "¿Viste la Copa de la Isla? Este año la gano. O el que viene. Pero la gano.",
                },
                Mid = new[]
                {
                    "Te extrañé, ¿sabés? El pueblo estaba más aburrido sin vos.",
                    "Pipo, mi Plumín, me despierta todos los días picoteándome la oreja. Es mi despertador con plumas.",
                    "Cuando éramos chicos dijiste que ibas a volver. Y volviste. Eso no lo hace cualquiera.",
                },
                High = new[]
                {
                    "Sos como de mi familia. ¡Familia elegida, ponele!",
                    "A veces pienso que soy medio desastre. Pero cuando estás vos, me sale ser mejor.",
                },
                Rain = new[] { "¡Lluvia! Ni las redes ni las carreras. Día de jugo y de perder al truco con Deny." },
                Companion = new[] { "¡Che, {memo} está re en forma! ¿Le das algo especial? ¡Pasame el secreto!", "¡Hola, {memo}! Pipo te manda saludos. Bah, te los mandaría si supiera hablar." },
                Schedule = new[]
                {
                    At(7, Pueblo, 22, 3, U, "muelle", "¡Ayudando a mi papá con las redes! …Bueno, mirando cómo las arregla."),
                    At(12, Pueblo, 20, 15, R, "plaza", "En la plaza a esta hora pasa de todo. Hoy, nada. Pero podría."),
                    At(15, Refugio, 16, 9, D, "entrenar", "¡Vine a entrenar en tu refugio! Hay más lugar para correr que en el pueblo.", weather: WeatherFilter.Sunny),
                    At(15, Taberna, 9, 2, D, "taberna", "Con lluvia no se entrena. Se toma jugo. Son las reglas.", weather: WeatherFilter.Rainy),
                    At(18, Pueblo, 15, 6, D, "casa", "Esta es mi casa. Es chiquita, pero entramos todos. Más o menos."),
                    Home(21),
                },
                Events = new[]
                {
                    Event("lalo_3", 3, new[]
                    {
                        "¡Ey! Mirá lo que encontré limpiando mi pieza.",
                        "¡Nuestra medalla de cartón! La que hicimos para la \"Gran Copa del Muelle\". Dice \"CAMPEONES\" con faltas de ortografía.",
                        "Ganamos los dos, ¿te acordás? Porque nos peleamos tanto por quién ganaba que hicimos empate.",
                    }, "¿Quién ganó en realidad?", new[]
                    {
                        ("Vos, obvio", 30, "¡JA! ¡Lo sabía! …Igual empate. Me gusta más empate."),
                        ("Yo, obvio", 40, "¡Mentira! …Bueno, puede ser. Revancha cuando quieras."),
                    }),
                    Event("lalo_6", 6, new[]
                    {
                        "¿Puedo contarte algo? Pero no te rías.",
                        "A veces siento que todos en la isla saben qué hacer con su vida. Anni cura, Fer forja, vos… vos rescatás Memos.",
                        "Y yo… corro. Y pierdo. Y vuelvo a correr.",
                        "Pero Pipo me mira como si yo fuera el mejor corredor del mundo. Y por él, quiero serlo.",
                    }, "¿Qué le decís?", new[]
                    {
                        ("Para Pipo, ya lo sos", 60, "…Sí, ¿no? Gracias. En serio. Sos lo más."),
                        ("Entrenemos juntos", 50, "¡Trato hecho! Y la próxima te gano. Prometido."),
                    }),
                },
                Requests = new[]
                {
                    Show("zumbi", 200, "Lalo: \"¡Quiero ver un Zumbi de cerca para estudiar cómo vuela!\"", "¡Mirá cómo zumba! ¡Ya entendí el secreto! …Creo."),
                    Deliver("zanahoria", 3, 270, "Lalo: \"3 zanahorias para el entrenamiento de Pipo. ¡Por favor!\"", "¡Gracias! Pipo va a correr como nunca. Bah, como siempre, pero contento."),
                    Deliver("pluma", 2, 140, "Lalo: \"2 plumas para hacerle un adorno a Pipo.\"", "¡Le va a quedar hermoso! Gracias, crack."),
                },
            },

            // ------------------------------------------------------------------ Luca
            new()
            {
                Id = "luca", Name = "Luca", Role = "Empleado de Ápice",
                FirstMeet = "¡Hola! Luca, de la oficina de Ápice. Vinimos a la isla para traer progreso: trabajo, la Copa, tecnología. ¡Es un momento emocionante!",
                Loved = new[] { "cuarzo", "pure_zapallo" }, Liked = new[] { "amatista", "zapallo", "concha" },
                Disliked = new[] { "hierba", "alga" },
                LovedText = "¡Wow, un cuarzo perfecto! ¿Sabías que se usa en los relojes? ¡Gracias!",
                LikedText = "¡Qué amable! Muchas gracias.",
                NeutralText = "Ah, gracias. Muy considerado.",
                DislikedText = "Oh… gracias. Es… interesante. Muy… natural.",
                Low = new[]
                {
                    "En el continente todo es más rápido. Acá la gente saluda por la calle. Me cuesta acostumbrarme… pero me gusta.",
                    "Trabajo en la oficina del muelle. Mucho papeleo. Mucho, mucho papeleo.",
                    "Creo que la tecnología puede hacer la vida de todos más fácil. Hasta la de los Memos.",
                },
                Mid = new[]
                {
                    "Ayer comí pescado del papá de Lalo. Nunca había comido algo tan fresco. En la ciudad viene en cajitas.",
                    "Me gusta hablar con vos. No me mirás raro por el uniforme.",
                    "A veces me quedo mirando el mar después del trabajo. No sé bien qué busco.",
                },
                High = new[]
                {
                    "Vine pensando que iba a enseñarle cosas a la isla. Creo que es al revés.",
                    "Sos la primera persona de acá que me hizo sentir que pertenezco. Gracias.",
                },
                Rain = new[] { "En el continente la lluvia es gris. Acá huele a pasto. Es raro lo distinto que se siente." },
                Companion = new[] { "{memo} es… muy expresivo. No sabía que los Memos podían ser tan distintos entre sí." },
                Schedule = new[]
                {
                    At(8, Pueblo, 29, 6, D, "oficina", "Entrando a la oficina. Tengo una pila de formularios así de alta."),
                    At(13, Pueblo, 27, 17, L, "almuerzo", "Almuerzo en la plaza. En el continente comía frente a la computadora."),
                    At(13, Taberna, 6, 3, R, "almuerzo", weather: WeatherFilter.Rainy),
                    At(14, Pueblo, 29, 6, D, "oficina"),
                    At(18, Pueblo, 23, 1, D, "muelle", "Me gusta la punta del muelle. Desde acá no se ve la oficina."),
                    At(20, Taberna, 6, 3, R, "taberna", "Deny me está enseñando a jugar al truco. Pierdo siempre."),
                    Home(23),
                },
                Events = new[]
                {
                    Event("luca_3", 3, new[]
                    {
                        "¡Hola! Perdón, estoy medio distraído. Me llegó una carta de mi abuela.",
                        "Dice que la ciudad está igual que siempre, que el colectivo sigue tarde, y que le mande una foto del mar.",
                        "Nunca le saqué fotos al mar. Siempre me pareció que no salía igual.",
                    }, "¿Qué le decís?", new[]
                    {
                        ("Mandale una igual", 40, "Tenés razón. No sale igual, pero algo llega. Gracias."),
                        ("Invitala a la isla", 50, "¿Te imaginás? Mi abuela en el muelle, retando a Lalo… Lo voy a pensar."),
                    }),
                    Event("luca_6", 6, new[]
                    {
                        "¿Te puedo preguntar algo?",
                        "Cuando un Memo te elige… ¿cómo sabés que te quiere? ¿Que no es solo que le das de comer?",
                        "En la oficina hablamos de Memos con números. Velocidad, rendimiento. Pero vos hablás de ellos como si fueran… amigos.",
                    }, "¿Qué le decís?", new[]
                    {
                        ("Porque vuelven a vos", 50, "Porque vuelven… Sí. Eso no lo mide ninguna planilla. Gracias."),
                        ("Porque te esperan", 50, "Te esperan… Me quedé pensando. Gracias por contestarme en serio."),
                    }),
                },
                Requests = new[]
                {
                    Deliver("cuarzo", 2, 300, "Luca: \"Necesito 2 cuarzos para… un experimento personal.\"", "¡Perfectos! Gracias. Te cuento si funciona."),
                    Deliver("concha", 3, 200, "Luca: \"Quiero mandarle 3 conchas a mi abuela.\"", "¡Le van a encantar! Gracias, de verdad."),
                    Show("chispin", 220, "Luca: \"¿Me mostrás un Chispín? Quiero ver cómo hace chispas.\"", "¡Increíble! Ninguna batería hace eso. Gracias."),
                },
            },

            // ------------------------------------------------------------------ Zorak
            new()
            {
                Id = "zorak", Name = "Zorak", Role = "Ermitaño de la cueva",
                FirstMeet = "…¿Qué mirás? Esta es mi cueva. Bueno, no es mía. Pero yo llegué primero. Picá las rocas si querés. Y no hagas ruido.",
                Loved = new[] { "zapallo", "esmeralda" }, Liked = new[] { "pure_zapallo", "topacio", "lingote_hierro" },
                Disliked = new[] { "flor", "pelota" },
                LovedText = "…Zapallo. Como el que hacía mi madre. …No me mires así. Gracias.",
                LikedText = "Mmh. No está mal. Gracias.",
                NeutralText = "¿Y esto? …Bueno. Lo dejo por ahí.",
                DislikedText = "No necesito esto. Pero… gracias por acordarte del viejo.",
                Low = new[]
                {
                    "Las rocas vuelven a crecer cada día. Como los problemas.",
                    "Cinco pisos tiene la cueva. En el último hay Farolitos. Llevá luz… o un Memo que brille.",
                    "¿Carreras? Antes corría. Ahora miro. Mirar también es un arte.",
                },
                Mid = new[]
                {
                    "Tu forma de tratar a los Memos… me recuerda a alguien. No preguntes a quién.",
                    "Un Memo no corre por la comida. Corre porque confía en que vas a estar en la meta.",
                    "Hoy te traje té. Está frío. Igual tomalo.",
                },
                High = new[]
                {
                    "Hace mucho que nadie venía a visitar al viejo de la cueva. Gracias. De verdad.",
                    "Algún día te voy a enseñar a correr a la forma antigua. Cuando sea el momento. Ya casi.",
                },
                Rain = new[] { "La lluvia no entra en la cueva. Por eso vivo acá. Bueno, por eso y porque no me gusta la gente." },
                Companion = new[] { "Ese {memo}… tiene buena mirada. No lo descuides." },
                Schedule = new[]
                {
                    At(6, Refugio, 25, 6, D, "cueva", "Vigilo la entrada. Nadie me lo pidió. Igual lo hago."),
                    At(19, Taberna, 2, 4, D, "taberna", "Los sábados bajo al pueblo. Una vez por semana alcanza.", days: Days.Sat),
                    Home(19, Days.All & ~Days.Sat),
                    Home(23, Days.Sat),
                },
                Events = new[]
                {
                    Event("zorak_3", 3, new[]
                    {
                        "Vení. Sentate. No, ahí no. Ahí. Bien.",
                        "(Zorak saca de su bolsillo una pelota de trapo vieja, muy remendada.)",
                        "Era de un Memo que tuve. Le encantaba que se la tirara lejos, cerca del agua.",
                        "No te voy a contar más. Solo… quería que alguien supiera que existió.",
                    }, "¿Qué hacés?", new[]
                    {
                        ("Quedarte en silencio", 60, "…Gracias por no decir nada. Es lo mejor que se puede decir."),
                        ("¿Cómo se llamaba?", 20, "…Otro día. Hoy no."),
                    }),
                    Event("zorak_6", 6, new[]
                    {
                        "Te estuve mirando correr con tus Memos. No está mal.",
                        "Pero corrés como si la carrera fuera tuya. No es tuya. Es de ellos.",
                        "Vos solo tenés que estar ahí. Que te vean. Que sepan que pase lo que pase, la meta sos vos.",
                        "…Eso era todo. Andá, que se hace tarde.",
                    }),
                },
                Requests = new[]
                {
                    Deliver("zapallo", 1, 350, "Zorak: \"Un zapallo. Uno solo. No pregunten.\"", "…Bien. Esta noche hay sopa. Gracias."),
                    Deliver("lingote_hierro", 1, 380, "Zorak: \"Necesito un lingote de hierro para arreglar algo viejo.\"", "Buen hierro. Va a quedar como nuevo. Bueno, como viejo, pero sano."),
                    Show("topin", 200, "Zorak: \"Traeme un Topín. Hace años que no veo uno de cerca.\"", "…Igual de cabezón que siempre. Gracias."),
                },
            },

            // ------------------------------------------------------------------ Jojo
            new()
            {
                Id = "jojo", Name = "Jojo", Role = "Nene del pueblo",
                FirstMeet = "¡Hola! Soy Jojo. Vos tenés Memos, ¿no? Yo… les tengo un poquito de miedo. Un poquito nomás. ¡No le digas a nadie!",
                Loved = new[] { "frutilla", "pelota" }, Liked = new[] { "concha", "pluma", "fruto_silvestre" },
                Disliked = new[] { "mineral_hierro", "hierba" },
                LovedText = "¡¡¿Para mí?!! ¡Gracias, gracias, gracias!",
                LikedText = "¡Qué lindo! Lo voy a guardar en mi caja de tesoros.",
                NeutralText = "Ah… gracias. ¿Esto para qué sirve?",
                DislikedText = "¡Puaj! …Perdón. Gracias igual.",
                Low = new[]
                {
                    "Mi mamá dice que los Memos no muerden. Pero tienen dientes. ¿Para qué tienen dientes si no muerden?",
                    "En la escuela aprendimos que hay veinte Memos distintos. ¡Veinte! Yo conozco tres de vista. De lejos.",
                    "Cuando sea grande voy a ser marinero. O pirata. O los dos.",
                },
                Mid = new[]
                {
                    "Ayer un Plumín se me acercó en la plaza ¡y no salí corriendo! Bueno, caminé rápido.",
                    "¿Cómo hacés para que tus Memos te quieran? ¿Les cantás?",
                    "Anni me dijo que el miedo se va de a poquito. Como cuando te sacás una curita.",
                },
                High = new[]
                {
                    "¡Ya casi no les tengo miedo! Es gracias a vos, ¿sabías?",
                    "Cuando tenga mi Memo, vos vas a ser su tío. O tía. O lo que quieras.",
                },
                Rain = new[] { "¡Con lluvia me dejan ir a la clínica a ver a los Memos de Anni! Desde atrás del mostrador. Por las dudas." },
                Companion = new[] { "¡Ay! ¿{memo} muerde? ¿Seguro que no? …¿Seguro, seguro?", "{memo} me está mirando. ¿Por qué me mira? ¿Le caigo bien?" },
                Schedule = new[]
                {
                    Home(8, Days.Weekdays),
                    At(15, Pueblo, 21, 14, U, "plaza", "¡Salí de la escuela! Hoy aprendimos fracciones. No entendí nada.", Days.Weekdays),
                    At(15, Clinica, 9, 4, L, "clinica", "Miro a los Memos de Anni desde acá. Es más seguro.", Days.Weekdays, WeatherFilter.Rainy),
                    Home(18, Days.Weekdays),
                    At(9, Pueblo, 25, 15, D, "plaza", "¡Es fin de semana! No hay escuela. ¡Hay plaza!", Days.Weekend),
                    At(9, Clinica, 9, 4, L, "clinica", days: Days.Weekend, weather: WeatherFilter.Rainy),
                    At(12, Pueblo, 24, 5, D, "playa", "Junto conchas. Tengo cuarenta y dos. Bueno, cuarenta y una, una se rompió.", Days.Weekend),
                    At(12, Clinica, 9, 4, L, "clinica", days: Days.Weekend, weather: WeatherFilter.Rainy),
                    Home(17, Days.Weekend),
                },
                Events = new[]
                {
                    Event("jojo_3", 3, new[]
                    {
                        "¡Psst! ¡Vení! Mirá lo que dibujé.",
                        "(Es un dibujo con crayones de vos y tus Memos. Todos tienen sonrisas enormes. Jojo está en una esquina, un poco lejos.)",
                        "Me dibujé lejos porque todavía me da miedo. Pero el año que viene me dibujo más cerca.",
                    }, "¿Qué le decís?", new[]
                    {
                        ("¡Dibujate más cerca ya!", 40, "¿Ya? ¿Ahora? …Bueno. Un pasito más cerca. ¡Listo!"),
                        ("Está hermoso así", 40, "¿Sí? ¡Lo voy a colgar en mi pieza!"),
                    }),
                    Event("jojo_6", 6, new[]
                    {
                        "¡¡Adiviná qué!! ¡Ayer acaricié un Memo! ¡Un Plumín! ¡En la cabeza!",
                        "Estaba temblando. Yo, no el Plumín. Bueno, los dos.",
                        "Pero me acordé de vos, de cómo te quedás quieto cerca de tus Memos, y lo hice igual.",
                        "Cuando sea grande quiero ser como vos.",
                    }),
                },
                Requests = new[]
                {
                    Show("plumin", 150, "Jojo: \"¿Me mostrás un Plumín? ¡Pero que no me pique!\"", "¡No me picó! ¡No me picó! ¡Gracias!"),
                    Show("zumbi", 150, "Jojo: \"Quiero ver un Zumbi de cerca. Bueno, de medio cerca.\"", "¡Zumba re fuerte! ¡Qué miedo! ¡Qué lindo! ¡Gracias!"),
                    Deliver("concha", 1, 90, "Jojo: \"Me falta una concha para mi colección. ¿Me das una?\"", "¡Cuarenta y dos de nuevo! ¡Gracias!"),
                },
            },

            // ------------------------------------------------------------------ Marga
            new()
            {
                Id = "marga", Name = "Marga", Role = "Alcaldesa",
                FirstMeet = "Te damos la bienvenida a Pueblo Puerto. Soy Marga, la alcaldesa. Si necesitás algo del municipio, pedí turno. Si necesitás un consejo, me encontrás en la plaza.",
                Loved = new[] { "mermelada", "amatista" }, Liked = new[] { "flor", "zanahoria", "pastel_frutilla" },
                Disliked = new[] { "alga", "piedra" },
                LovedText = "Qué detalle exquisito. Te lo agradezco mucho.",
                LikedText = "Muy amable de tu parte.",
                NeutralText = "Gracias. Lo tendré en cuenta.",
                DislikedText = "Ah. Bueno. Gracias por el… gesto.",
                Low = new[]
                {
                    "Un pueblo es como un barco: todos tienen que remar para el mismo lado.",
                    "La Copa de la Isla trae visitantes y trabajo. Hay que cuidar lo que funciona.",
                    "Si tu refugio necesita permisos, el municipio abre de nueve a cinco. Con turno.",
                },
                Mid = new[]
                {
                    "Tu refugio le hace bien al pueblo. La gente habla de los Memos con otra cara.",
                    "Ser alcaldesa es decidir todo el día entre dos cosas que no te gustan.",
                    "Cuando era joven corría en la Copa. Llegaba última. Siempre. Pero llegaba.",
                },
                High = new[]
                {
                    "Me hacés acordar por qué acepté este cargo. Para que la isla sea un buen lugar para gente como vos.",
                    "Si algún día necesitás ayuda de verdad, venís a mí. Sin turno.",
                },
                Rain = new[] { "La lluvia no detiene al municipio. Detiene a los vecinos que vienen a quejarse. Es casi lo mismo." },
                Companion = new[] { "Tu {memo} se ve sano y bien cuidado. Así deberían estar todos los Memos de la isla." },
                Schedule = new[]
                {
                    At(9, Pueblo, 7, 6, D, "municipio", "Entrando al municipio. Hoy tengo cuatro reuniones. Cuatro."),
                    At(12, Pueblo, 22, 17, D, "plaza", "Almuerzo en la plaza para escuchar a los vecinos. Así me entero de todo.", weather: WeatherFilter.Sunny),
                    At(13, Pueblo, 7, 6, D, "municipio"),
                    At(17, Pueblo, 26, 14, D, "plaza", "Último paseo del día. Miro qué hay que arreglar. Siempre hay algo.", weather: WeatherFilter.Sunny),
                    Home(19),
                },
                Events = new[]
                {
                    Event("marga_3", 3, new[]
                    {
                        "Tenés un momento. Acompañame.",
                        "Esta plaza la hicieron los vecinos hace cuarenta años. Cada uno trajo una baldosa.",
                        "Esa, la torcida, la puse yo. Tenía ocho años y estaba muy orgullosa.",
                        "Cada vez que tengo que tomar una decisión difícil, vengo a mirarla.",
                    }, "¿Qué le decís?", new[]
                    {
                        ("Es la más linda", 40, "Es la más torcida. Pero gracias. Me alegra que la veas."),
                        ("¿Qué decisión tenés que tomar?", 20, "Muchas. Ninguna fácil. Otro día te cuento."),
                    }, Pueblo),
                    Event("marga_6", 6, new[]
                    {
                        "Quiero agradecerte algo, formalmente.",
                        "Desde que reabriste el refugio, los chicos de la escuela preguntan por los Memos. Jojo no habla de otra cosa.",
                        "La isla se estaba olvidando de por qué los Memos importan. Vos se lo estás recordando.",
                        "Eso vale más que cualquier Copa.",
                    }),
                },
                Requests = new[]
                {
                    Deliver("flor", 6, 200, "Marga: \"6 flores para decorar el municipio antes de una visita.\"", "Quedó precioso. El pueblo te lo agradece."),
                    Deliver("zanahoria", 4, 300, "Marga: \"4 zanahorias para el almuerzo comunitario.\"", "Gracias. Hoy comemos todos juntos gracias a vos."),
                },
            },

            // ------------------------------------------------------------------ Vera
            new()
            {
                Id = "vera", Name = "Vera", Role = "Capitana de la Copa",
                FirstMeet = "Capitana Vera. Lidero la Copa de la Isla, equipo Metal. Si querés correr contra mí, primero ganate el lugar. No regalo nada.",
                Loved = new[] { "lingote_hierro", "esmeralda" }, Liked = new[] { "mineral_hierro", "comida_memo", "pure_zapallo" },
                Disliked = new[] { "flor", "pastel_frutilla" },
                LovedText = "Hierro de calidad. Sabés lo que valoro. Gracias.",
                LikedText = "Útil. Gracias.",
                NeutralText = "Gracias.",
                DislikedText = "No soy de estas cosas. Pero valoro el gesto.",
                Low = new[]
                {
                    "La disciplina le gana al talento cuando el talento no entrena.",
                    "Mis Memos son de tipo Metal. Lentos al arrancar, imparables en la montaña.",
                    "Una carrera se gana en los relevos. Elegí bien quién corre cada tramo.",
                },
                Mid = new[]
                {
                    "Vi una de tus carreras. Tenés buen ojo para los tramos. Te falta frialdad.",
                    "Entreno a mis Memos todos los días. Ellos me entrenan a mí también: paciencia.",
                    "No confío fácil. Pero vos jugás limpio. Eso se nota.",
                },
                High = new[]
                {
                    "Si alguna vez llegás a la final, espero que seas vos. Me gustaría perder contra alguien que lo merece. …Si es que pierdo.",
                    "Sos de las pocas personas en esta isla con las que bajo la guardia.",
                },
                Rain = new[] { "Se entrena igual con lluvia. La pista mojada enseña a no confiarse." },
                Companion = new[] { "Tu {memo}: buena postura, buena respiración. Bien entrenado." },
                Schedule = new[]
                {
                    At(7, Pueblo, 41, 6, D, "estadio", "Entrenamiento matutino. Diez vueltas al estadio. Sin quejas."),
                    At(12, Taberna, 9, 3, L, "almuerzo", "Almuerzo rápido. Proteína y vuelta a entrenar."),
                    At(14, Pueblo, 41, 6, D, "estadio"),
                    At(18, Pueblo, 20, 5, R, "muelle", "Después de entrenar, estiro mirando el mar. El mar no tiene apuro."),
                    Home(20),
                },
                Events = new[]
                {
                    Event("vera_3", 3, new[]
                    {
                        "Estás acá. Bien. Necesito una opinión.",
                        "Tengo dos estrategias para la Copa. Una segura, una arriesgada. Mis Memos rinden más con la arriesgada… pero si sale mal, se cansan de más.",
                        "¿Qué harías vos?",
                    }, "¿Qué harías?", new[]
                    {
                        ("La segura: los cuida", 50, "Cuidarlos primero. …Sí. Por eso te pregunté a vos."),
                        ("La arriesgada: para ganar", 10, "Ganar no es todo. Pensé que dirías otra cosa."),
                    }),
                    Event("vera_6", 6, new[]
                    {
                        "Te voy a decir algo que no le digo a nadie.",
                        "Mi primer Memo fue un Tuerquita que encontré roto en un taller. Literalmente: le faltaba un tornillo.",
                        "Lo arreglé. Me siguió desde ese día. Ahora es el más rápido de mi equipo.",
                        "Por eso desconfío de los atajos. Las cosas buenas se arreglan despacio.",
                    }),
                },
                Requests = new[]
                {
                    Deliver("comida_memo", 6, 240, "Vera: \"6 comidas para Memos para el equipo. Calidad, por favor.\"", "Bien. Mis Memos te lo agradecen. Yo también."),
                    Deliver("mineral_hierro", 3, 280, "Vera: \"3 minerales de hierro para el equipo de entrenamiento.\"", "Buen material. Gracias."),
                    Show("topin", 180, "Vera: \"Quiero ver la técnica de excavación de un Topín.\"", "Interesante. Muy eficiente. Gracias."),
                },
            },
        };
    }
}
