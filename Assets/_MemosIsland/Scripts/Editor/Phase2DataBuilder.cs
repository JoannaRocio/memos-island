using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemosIsland.EditorTools.PixelArt;
using MemosIsland.Memos;
using UnityEditor;
using UnityEngine;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 2: crea los assets de datos de Memos a partir del GDD (§7 y §8).
    /// Después se editan en el inspector. "Crear datos faltantes" no pisa lo que ya existe
    /// (solo actualiza los sprites); "Restablecer desde el GDD" vuelve todo a los valores del documento.
    /// </summary>
    public static class Phase2DataBuilder
    {
        const string DataRoot = "Assets/_MemosIsland/Data";
        const string DatabasePath = "Assets/_MemosIsland/Resources/MemoDatabase.asset";
        const string MemoSprites = PixelArtGenerator.GeneratedRoot + "/Memos";

        [MenuItem("Memos Island/Fase 2/Crear datos de Memos (solo faltantes)")]
        public static void BuildMissing() => Build(false);

        [MenuItem("Memos Island/Fase 2/Restablecer datos desde el GDD (sobrescribe)")]
        public static void BuildOverwrite() => Build(true);

        // ------------------------------------------------------------------ Datos del GDD

        static readonly (string id, string name, string color)[] Types =
        {
            ("planta", "Planta", "38b764"), ("tierra", "Tierra", "8f5a3c"), ("agua", "Agua", "3b5dc9"),
            ("hielo", "Hielo", "41a6f6"), ("fuego", "Fuego", "b13e53"), ("electrico", "Eléctrico", "c46a3a"),
            ("viento", "Viento", "257179"), ("roca", "Roca", "5a3328"), ("bicho", "Bicho", "1e5a3c"),
            ("sombra", "Sombra", "5d275d"), ("luz", "Luz", "d9a066"), ("metal", "Metal", "566c86"),
        };

        static readonly (string id, string name, string color)[] Terrains =
        {
            ("pradera", "Pradera", "a7f070"), ("bosque", "Bosque", "38b764"), ("arena", "Arena", "ffcd75"),
            ("barro", "Barro", "8f5a3c"), ("rio", "Río", "41a6f6"), ("hielo", "Hielo", "73eff7"),
            ("nieve", "Nieve", "f4f4f4"), ("ceniza", "Ceniza", "333c57"), ("montana", "Montaña", "94b0c2"),
            ("cueva", "Cueva", "566c86"), ("tormenta", "Tormenta", "29366f"), ("aire", "Corrientes de aire", "d9a066"),
        };

        // Fila = tipo (mismo orden que Types), columna = terreno (mismo orden que Terrains). + ▲ · normal - ▼
        static readonly Dictionary<string, string> Chart = new()
        {
            ["planta"] = "++-..---....",
            ["tierra"] = "..++--..+..-",
            ["agua"] = "..-++..+-...",
            ["hielo"] = "..-.+++-....",
            ["fuego"] = "...--+++..-.",
            ["electrico"] = "..--.....++.",
            ["viento"] = "+-......+-.+",
            ["roca"] = "...--..+++.-",
            ["bicho"] = "++.+.---....",
            ["sombra"] = "-+-......++.",
            ["luz"] = "+.+-.....+-.",
            ["metal"] = "..+--+..+.-.",
        };

        record AbilityDef(string Id, string Name, string Description, AbilityArchetype Archetype, AbilityEffect Effect,
            float Duration, float Strength, string Terrain = null, bool All = false);

        static readonly AbilityDef[] Abilities =
        {
            new("estela_brasas", "Estela de brasas", "Deja fuego atrás: los rivales que lo siguen se frenan 2 segundos.",
                AbilityArchetype.HinderBehind, AbilityEffect.SlowFollowers, 2f, 0.6f),
            new("estela_ardiente", "Estela ardiente", "Una estela más larga que además derrite el hielo y la nieve del tramo.",
                AbilityArchetype.HinderBehind, AbilityEffect.SlowFollowers, 3f, 0.55f),
            new("enredadera", "Enredadera", "Atrapa 2 segundos al rival que va adelante.",
                AbilityArchetype.HinderAhead, AbilityEffect.Root, 2f, 0f),
            new("raices_profundas", "Raíces profundas", "Atrapa 3 segundos al rival de adelante y recupera energía.",
                AbilityArchetype.HinderAhead, AbilityEffect.Root, 3f, 0.25f),
            new("salpicon", "Salpicón", "Convierte el tramo actual en Río durante 6 segundos.",
                AbilityArchetype.ChangeTerrain, AbilityEffect.ChangeTerrain, 6f, 0f, "rio"),
            new("ola", "Ola", "Convierte el tramo en Río y empuja hacia atrás a los rivales cercanos.",
                AbilityArchetype.ChangeTerrain, AbilityEffect.ChangeTerrain, 6f, 0.5f, "rio", true),
            new("trueno", "Trueno", "Sprint instantáneo que aturde 1,5 segundos a todos los rivales cercanos.",
                AbilityArchetype.HinderAhead, AbilityEffect.Stun, 1.5f, 1.5f, null, true),
            new("llamarada_negra", "Llamarada negra", "Convierte el tramo en Ceniza y frena a todos los que vienen atrás.",
                AbilityArchetype.HinderBehind, AbilityEffect.ChangeTerrain, 6f, 0.6f, "ceniza", true),
            new("mar_helado", "Mar helado", "Congela todo el tramo; sobre el hielo él va todavía más rápido.",
                AbilityArchetype.ChangeTerrain, AbilityEffect.ChangeTerrain, 6f, 1.2f, "hielo"),
            new("rafaga", "Ráfaga", "El próximo Memo de tu equipo que entre arranca a velocidad máxima.",
                AbilityArchetype.Self, AbilityEffect.TeamBoost, 0f, 1f),
            new("tunel", "Túnel", "Desaparece 2 segundos y reaparece más adelante.",
                AbilityArchetype.Self, AbilityEffect.Teleport, 2f, 1.5f),
            new("enjambre", "Enjambre", "El rival más cercano corre en zigzag durante 3 segundos.",
                AbilityArchetype.HinderAhead, AbilityEffect.Zigzag, 3f, 0.7f),
            new("descarga", "Descarga", "Sprint enorme durante 3 segundos; después queda agotado.",
                AbilityArchetype.Self, AbilityEffect.Sprint, 3f, 1.6f),
            new("escarcha", "Escarcha", "Convierte el tramo actual en Hielo durante 6 segundos.",
                AbilityArchetype.ChangeTerrain, AbilityEffect.ChangeTerrain, 6f, 0f, "hielo"),
            new("pantano", "Pantano", "Convierte el tramo actual en Barro durante 6 segundos.",
                AbilityArchetype.ChangeTerrain, AbilityEffect.ChangeTerrain, 6f, 0f, "barro"),
            new("siesta_contagiosa", "Siesta contagiosa", "El rival de adelante se duerme 1,5 segundos.",
                AbilityArchetype.HinderAhead, AbilityEffect.Sleep, 1.5f, 0f),
            new("destello", "Destello", "Encandila 1 segundo a todos los rivales.",
                AbilityArchetype.HinderAhead, AbilityEffect.Blind, 1f, 0.5f, null, true),
            new("remiendo", "Remiendo", "Recupera al instante el 40% de su energía.",
                AbilityArchetype.Self, AbilityEffect.Recover, 0f, 0.4f),
            new("acecho", "Acecho", "Copia la última habilidad que usó un rival.",
                AbilityArchetype.Self, AbilityEffect.Copy, 0f, 1f),
            new("magnetismo", "Magnetismo", "Atrae a los rivales cercanos y les roba velocidad durante 3 segundos.",
                AbilityArchetype.HinderAhead, AbilityEffect.Magnet, 3f, 0.8f, null, true),
        };

        record TemperamentDef(string Id, string Name, string Race, string Refuge, float Spd = 1, float Acc = 1,
            float Sta = 1, float Chg = 1, float Trust = 1, TemperamentTrait Trait = TemperamentTrait.None);

        static readonly TemperamentDef[] Temperaments =
        {
            new("impulsivo", "Impulsivo", "Arranca fuerte pero se cansa antes.", "Corre por todos lados.", Acc: 1.1f, Sta: 0.9f),
            new("constante", "Constante", "No baja el ritmo aunque tenga poca energía.", "Tiene rutinas ordenadas.",
                Trait: TemperamentTrait.SteadyPace),
            new("remontador", "Remontador", "Corre más rápido cuando va último.", "Le encanta jugar a perseguir.",
                Trait: TemperamentTrait.Comeback),
            new("timido", "Tímido", "Acelera menos pero carga su habilidad más rápido.", "Se esconde y le cuesta hacer amigos.",
                Acc: 0.9f, Chg: 1.1f),
            new("mimoso", "Mimoso", "Gana más confianza al correr con vos.", "Busca caricias y te sigue a todas partes.",
                Trust: 1.25f),
            new("dormilon", "Dormilón", "Aguanta más, pero tarda en arrancar.", "Duerme siestas larguísimas.",
                Acc: 0.9f, Sta: 1.1f),
            new("jugueton", "Juguetón", "Carga su habilidad más rápido.", "Invita a jugar a los otros Memos.", Chg: 1.1f),
            new("orgulloso", "Orgulloso", "Más veloz, pero a veces ignora tus órdenes si no confía en vos.", "Prefiere estar solo.",
                Spd: 1.1f, Trait: TemperamentTrait.Proud),
        };

        record SpeciesDef(int Number, string Id, string Name, MemoCategory Category, string Type1, string Type2,
            Mobility Mobility, int Spd, int Acc, int Sta, int Chg, string Ability, string Habitat, string Description,
            string[] Foods, string EvolvesTo = null, int EvolutionLevel = 0,
            MemoAvailability Availability = MemoAvailability.Available);

        static readonly SpeciesDef[] Species =
        {
            new(1, "tostin", "Tostín", MemoCategory.Starter, "fuego", null, Mobility.Runs, 6, 7, 5, 6, "estela_brasas",
                "Hornos y fogones abandonados",
                "Su cola es una llamita que crece cuando está contento. Se hace el duro, pero llora si lo dejan solo de noche.",
                new[] { "bayamemo" }, "brason", 16),
            new(2, "brason", "Brasón", MemoCategory.Starter, "fuego", "roca", Mobility.Runs, 8, 7, 7, 7, "estela_ardiente",
                "Hornos y fogones abandonados",
                "Sobre el lomo le crecen placas de roca caliente. Protege a los suyos con una lealtad feroz.",
                new[] { "bayamemo" }),
            new(3, "brotito", "Brotito", MemoCategory.Starter, "planta", null, Mobility.Runs, 5, 5, 7, 7, "enredadera",
                "Bosques tranquilos",
                "Mira todo con mucha atención antes de acercarse. Su hojita se cierra cuando tiene miedo.",
                new[] { "frutilla", "bayamemo" }, "ramazon", 16),
            new(4, "ramazon", "Ramazón", MemoCategory.Starter, "planta", "tierra", Mobility.Runs, 7, 5, 9, 8, "raices_profundas",
                "Bosques tranquilos",
                "Sus ramas florecen cuando está en paz. Dicen que donde duerme, al día siguiente crece pasto nuevo.",
                new[] { "frutilla", "bayamemo" }),
            new(5, "charquito", "Charquito", MemoCategory.Starter, "agua", null, Mobility.Swims, 6, 6, 5, 7, "salpicon",
                "Charcos y costas",
                "Imita los sonidos de todos para no sentirse solo. Siempre lleva una gotita de agua sobre la cabeza.",
                new[] { "bayamemo" }, "chapuzon", 16),
            new(6, "chapuzon", "Chapuzón", MemoCategory.Starter, "agua", "viento", Mobility.Swims, 8, 7, 6, 8, "ola",
                "Mar abierto",
                "Nada tan rápido que levanta olas a su paso. Con sus aletas puede planear sobre el mar.",
                new[] { "bayamemo" }),
            new(7, "karman", "Karman", MemoCategory.Legendary, "electrico", "viento", Mobility.Runs, 9, 10, 6, 8, "trueno",
                "Acantilados Tormenta (solo con tormenta)",
                "Un lobo hecho de tormenta. Nadie lo domó jamás: llega con los truenos y se va con el viento.",
                new string[0]),
            new(8, "draken", "Draken", MemoCategory.Legendary, "fuego", "sombra", Mobility.Flies, 10, 8, 8, 7, "llamarada_negra",
                "Volcán Dormido",
                "Un dragón negro de escamas encendidas. Los pescadores juran haberlo visto volar sobre el Volcán Dormido.",
                new string[0], Availability: MemoAvailability.Locked),
            new(9, "randy", "Randy", MemoCategory.Legendary, "agua", "hielo", Mobility.Swims, 9, 7, 9, 6, "mar_helado",
                "Playa Helada",
                "Un tiburón de hielo, burlón y orgulloso. Se divierte persiguiendo barcos en la Playa Helada.",
                new string[0], Availability: MemoAvailability.Locked),
            new(10, "plumin", "Plumín", MemoCategory.Wild, "viento", null, Mobility.Flies, 6, 8, 4, 7, "rafaga",
                "Ruta Pradera",
                "Una bolita de plumas tan liviana que el viento la lleva de paseo. Le encanta dormir en los techos.",
                new[] { "frutilla", "bayamemo" }),
            new(11, "topin", "Topín", MemoCategory.Wild, "tierra", null, Mobility.Digs, 5, 4, 8, 6, "tunel",
                "Cueva de Zorak",
                "Casi no ve, pero su nariz encuentra cualquier tesoro escondido bajo tierra.",
                new[] { "zanahoria", "bayamemo" }),
            new(12, "zumbi", "Zumbi", MemoCategory.Wild, "bicho", null, Mobility.Flies, 6, 7, 4, 8, "enjambre",
                "Ruta Pradera y Bosque Susurro",
                "Vuela en zigzag y nunca en línea recta. Su zumbido suena distinto según su humor.",
                new[] { "bayamemo" }),
            new(13, "chispin", "Chispín", MemoCategory.Wild, "electrico", null, Mobility.Runs, 7, 9, 3, 6, "descarga",
                "Acantilados Tormenta",
                "Junta electricidad frotándose contra el pasto. Si se emociona, se le paran todos los pelos.",
                new[] { "bayamemo" }),
            new(14, "copito", "Copito", MemoCategory.Wild, "hielo", null, Mobility.Runs, 5, 6, 6, 7, "escarcha",
                "Colina Escarcha",
                "Su cuerpo está fresquito incluso en verano. Si lo abrazás un buen rato, te deja la nariz roja.",
                new[] { "bayamemo" }),
            new(15, "pantuflo", "Pantuflo", MemoCategory.Wild, "agua", "tierra", Mobility.Swims, 4, 5, 9, 6, "pantano",
                "Pantano Pantufla",
                "Tiene forma de pantufla gigante y es igual de cómodo. Es tan lento como feliz.",
                new[] { "zapallo", "bayamemo" }),
            new(16, "bostezo", "Bostezo", MemoCategory.Wild, "sombra", null, Mobility.Runs, 5, 5, 6, 9, "siesta_contagiosa",
                "Bosque Susurro (solo de noche)",
                "Duerme todo el día y sale de noche. Si bosteza cerca tuyo, es casi imposible no bostezar también.",
                new[] { "bayamemo" }),
            new(17, "farolito", "Farolito", MemoCategory.Wild, "luz", null, Mobility.Flies, 6, 6, 5, 8, "destello",
                "Cueva de Zorak, piso 5",
                "Su luz se vuelve más cálida cuando confía en alguien. Guía a los perdidos en lo profundo de la cueva.",
                new[] { "bayamemo" }),
            new(18, "tuerquita", "Tuerquita", MemoCategory.Exclusive, "metal", null, Mobility.Runs, 6, 6, 7, 7, "remiendo",
                "Equipo de la Capitana Vera",
                "Arregla todo lo que encuentra roto. La Capitana Vera lo cuida como a un hijo.",
                new string[0], Availability: MemoAvailability.NotObtainable),
            new(19, "ferrolobo", "Ferrolobo", MemoCategory.Exclusive, "metal", "sombra", Mobility.Runs, 8, 7, 7, 6, "acecho",
                "Equipo de la Capitana Vera",
                "Estudia a sus rivales en silencio y copia sus movimientos. Es el orgullo del equipo de Vera.",
                new string[0], Availability: MemoAvailability.NotObtainable),
            new(20, "imanta", "Imanta", MemoCategory.Exclusive, "metal", "electrico", Mobility.Runs, 7, 7, 7, 7, "magnetismo",
                "Equipo de la Capitana Vera",
                "Su imán atrae todo lo metálico. Las monedas del pueblo tienen la mala costumbre de pegársele.",
                new string[0], Availability: MemoAvailability.NotObtainable),
        };

        // ------------------------------------------------------------------ Construcción

        public static void Build(bool overwrite)
        {
            PixelArtGenerator.GenerateAll();

            var types = Types.ToDictionary(t => t.id, t => Asset<MemoType>($"Types/{t.id}", overwrite, (a, fresh) =>
            {
                if (!fresh) return;
                a.id = t.id;
                a.displayName = t.name;
                a.color = Hex(t.color);
            }));

            var terrains = Terrains.ToDictionary(t => t.id, t => Asset<RaceTerrain>($"Terrains/{t.id}", overwrite, (a, fresh) =>
            {
                if (!fresh) return;
                a.id = t.id;
                a.displayName = t.name;
                a.color = Hex(t.color);
            }));

            var chart = Asset<EffectivenessChart>("EffectivenessChart", overwrite, (c, fresh) =>
            {
                c.types = Types.Select(t => types[t.id]).ToList();
                c.terrains = Terrains.Select(t => terrains[t.id]).ToList();
                c.Resize();
                if (!fresh) return;
                foreach (var (typeId, row) in Chart)
                    for (int i = 0; i < Terrains.Length; i++)
                        c.Set(types[typeId], terrains[Terrains[i].id],
                            row[i] == '+' ? Effectiveness.Strong : row[i] == '-' ? Effectiveness.Weak : Effectiveness.Normal);
            });

            var abilities = Abilities.ToDictionary(d => d.Id, d => Asset<AbilityData>($"Abilities/{d.Id}", overwrite, (a, fresh) =>
            {
                if (!fresh) return;
                a.id = d.Id;
                a.displayName = d.Name;
                a.description = d.Description;
                a.archetype = d.Archetype;
                a.effect = d.Effect;
                a.duration = d.Duration;
                a.strength = d.Strength;
                a.terrain = d.Terrain != null ? terrains[d.Terrain] : null;
                a.affectsAll = d.All;
            }));

            var temperaments = Temperaments.Select(d => Asset<Temperament>($"Temperaments/{d.Id}", overwrite, (t, fresh) =>
            {
                if (!fresh) return;
                t.id = d.Id;
                t.displayName = d.Name;
                t.raceDescription = d.Race;
                t.refugeDescription = d.Refuge;
                t.speedMultiplier = d.Spd;
                t.accelerationMultiplier = d.Acc;
                t.staminaMultiplier = d.Sta;
                t.chargeMultiplier = d.Chg;
                t.trustGainMultiplier = d.Trust;
                t.trait = d.Trait;
            })).ToList();

            var species = new Dictionary<string, MemoSpecies>();
            var freshSpecies = new HashSet<string>();
            foreach (var d in Species)
            {
                species[d.Id] = Asset<MemoSpecies>($"Species/{d.Number:00}_{d.Id}", overwrite, (s, fresh) =>
                {
                    // Los sprites se actualizan siempre (el arte se regenera seguido).
                    s.raceFrames = Frames($"{d.Id}_race");
                    s.shinyRaceFrames = Frames($"{d.Id}_race_brillante");
                    s.collarRaceFrames = Frames($"{d.Id}_race_concollar");
                    s.worldFrames = Frames($"{d.Id}_world");
                    s.shinyWorldFrames = Frames($"{d.Id}_world_brillante");
                    s.collarWorldFrames = Frames($"{d.Id}_world_concollar");
                    if (!fresh) return;
                    freshSpecies.Add(d.Id);
                    s.number = d.Number;
                    s.id = d.Id;
                    s.displayName = d.Name;
                    s.category = d.Category;
                    s.availability = d.Availability;
                    s.primaryType = types[d.Type1];
                    s.secondaryType = d.Type2 != null ? types[d.Type2] : null;
                    s.mobility = d.Mobility;
                    s.baseStats = new MemoStats(d.Spd, d.Acc, d.Sta, d.Chg);
                    s.ability = abilities[d.Ability];
                    s.habitat = d.Habitat;
                    s.description = d.Description;
                    s.favoriteFoods = d.Foods.ToList();
                    s.evolutionLevel = d.EvolutionLevel;
                });
            }
            // Las evoluciones se enlazan cuando ya existen todas las especies.
            foreach (var d in Species.Where(d => d.EvolvesTo != null && freshSpecies.Contains(d.Id)))
            {
                species[d.Id].evolvesTo = species[d.EvolvesTo];
                EditorUtility.SetDirty(species[d.Id]);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath));
            var db = AssetDatabase.LoadAssetAtPath<MemoDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<MemoDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.chart = chart;
            db.types = types.Values.ToList();
            db.terrains = terrains.Values.ToList();
            db.abilities = abilities.Values.ToList();
            db.temperaments = temperaments;
            db.species = Species.Select(d => species[d.Id]).ToList();
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fase 2] Datos de Memos listos ({species.Count} especies, {abilities.Count} habilidades)." +
                      (overwrite ? " Se restablecieron los valores del GDD." : ""));
        }

        /// <summary>Carga o crea un asset. El callback recibe "fresh" = true si hay que cargarle los valores del GDD.</summary>
        static T Asset<T>(string relativePath, bool overwrite, System.Action<T, bool> fill) where T : ScriptableObject
        {
            var path = $"{DataRoot}/{relativePath}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            bool fresh = asset == null || overwrite;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            fill(asset, fresh);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static Sprite[] Frames(string baseName)
        {
            var single = AssetDatabase.LoadAssetAtPath<Sprite>($"{MemoSprites}/{baseName}.png");
            if (single != null) return new[] { single };
            var list = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{MemoSprites}/{baseName}_{i}.png");
                if (s == null) break;
                list.Add(s);
            }
            return list.ToArray();
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
