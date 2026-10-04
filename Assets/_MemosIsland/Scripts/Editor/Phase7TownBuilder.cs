using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemosIsland.EditorTools.PixelArt;
using MemosIsland.Memos;
using MemosIsland.Race;
using MemosIsland.Town;
using MemosIsland.UI;
using MemosIsland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 7: vecinos. Corre todo lo anterior, crea los datos de los 9 vecinos (Data/Neighbors), el prefab que camina,
    /// el Pueblo Puerto completo (almacén, herrería, clínica, taberna, plaza con tablón, municipio, estadio, oficina de Ápice,
    /// muelle) con sus 4 interiores, y suma los vecinos que visitan el refugio.
    /// </summary>
    public static class Phase7TownBuilder
    {
        const string MapsFolder = "Assets/_MemosIsland/Scenes/Maps";
        const string NeighborsFolder = "Assets/_MemosIsland/Data/Neighbors";
        const string Gen = PixelArtGenerator.GeneratedRoot;
        const string P = Phase7NeighborContent.Pueblo;

        static Sprite S(string path) => Phase1WorldBuilder.S(path);
        static TileBase T(string name) => Phase1WorldBuilder.T(name);

        [MenuItem("Memos Island/Fase 7/Construir vecinos (y todo)")]
        public static void Build()
        {
            Phase6IslandBuilder.Build();
            BuildNeighbors(false);
            BuildNeighborActor();
            BuildPueblo();
            BuildInteriors();
            AddToRefugio();
            AddToBuildSettings();
            EditorSceneManager.OpenScene($"{MapsFolder}/{P}.unity");
            Debug.Log("[Fase 7] Vecinos listos. Abrí Map_PuebloPuerto y dale Play (F5/F6 cambian la hora, F8 el clima).");
        }

        [MenuItem("Memos Island/Fase 7/Restablecer vecinos (sobrescribe textos y rutinas)")]
        public static void ResetNeighbors() => BuildNeighbors(true);

        // ------------------------------------------------------------------ Datos

        static Sprite[] Frames(string id, string dir) =>
            Enumerable.Range(0, 3).Select(i => S($"Neighbors/{id}_{dir}_{i}")).ToArray();

        public static void BuildNeighbors(bool overwrite)
        {
            var db = AssetDatabase.LoadAssetAtPath<MemoDatabase>("Assets/_MemosIsland/Resources/MemoDatabase.asset");
            Directory.CreateDirectory(NeighborsFolder);
            var list = new List<NeighborData>();
            foreach (var d in Phase7NeighborContent.All)
            {
                var path = $"{NeighborsFolder}/{d.Id}.asset";
                var n = AssetDatabase.LoadAssetAtPath<NeighborData>(path);
                bool fresh = n == null || overwrite;
                if (n == null)
                {
                    n = ScriptableObject.CreateInstance<NeighborData>();
                    AssetDatabase.CreateAsset(n, path);
                }
                if (fresh)
                {
                    n.id = d.Id;
                    n.displayName = d.Name;
                    n.role = d.Role;
                    n.firstMeet = d.FirstMeet;
                    n.loved = d.Loved.ToList();
                    n.liked = d.Liked.ToList();
                    n.disliked = d.Disliked.ToList();
                    n.lovedText = d.LovedText;
                    n.likedText = d.LikedText;
                    n.neutralText = d.NeutralText;
                    n.dislikedText = d.DislikedText;
                    n.linesLow = d.Low.ToList();
                    n.linesMid = d.Mid.ToList();
                    n.linesHigh = d.High.ToList();
                    n.linesRain = d.Rain.ToList();
                    n.linesCompanion = d.Companion.ToList();
                    n.schedule = d.Schedule.ToList();
                    n.events = d.Events.ToList();
                    n.requests = d.Requests.ToList();
                }
                // El arte se actualiza siempre.
                n.portrait = S($"Portraits/portrait_{d.Id}");
                n.icon = S($"Portraits/head_{d.Id}");
                n.down = Frames(d.Id, "down");
                n.up = Frames(d.Id, "up");
                n.left = Frames(d.Id, "left");
                n.right = Frames(d.Id, "right");
                EditorUtility.SetDirty(n);
                list.Add(n);
            }
            db.neighbors = list;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Prefab del vecino

        static void BuildNeighborActor()
        {
            var font = AssetDatabase.LoadAssetAtPath<PixelFont>($"{Gen}/Fonts/MemosFont.asset");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(Phase1WorldBuilder.UnlitMaterialPath);
            var go = new GameObject("NeighborActor");
            var mover = go.AddComponent<GridMover>();
            var so = new SerializedObject(mover);
            so.FindProperty("occupiesCell").boolValue = true;
            so.FindProperty("walkSpeed").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);
            col.offset = new Vector2(0f, 0.5f);

            var body = Phase1WorldBuilder.Child(go.transform, "Sprite", Vector3.zero);
            var r = body.AddComponent<SpriteRenderer>();
            r.sortingOrder = 10;
            r.sprite = S("Neighbors/deny_down_0");
            body.AddComponent<CharacterSpriteAnimator>();

            var bubbleGo = Phase1WorldBuilder.Child(go.transform, "Bubble", new Vector3(0f, 2.6f, 0f));
            var bubbleRenderer = bubbleGo.AddComponent<SpriteRenderer>();
            bubbleRenderer.sprite = S("UI/ui_bubble");
            bubbleRenderer.sharedMaterial = unlit;
            bubbleRenderer.sortingOrder = 60;
            var glyph = Phase1WorldBuilder.Child(bubbleGo.transform, "Glyph", Vector3.zero).AddComponent<PixelText>();
            glyph.Setup(font, unlit, 61, Color.black, false);
            bubbleGo.AddComponent<EmoteBubble>().Setup(bubbleRenderer, glyph);

            go.AddComponent<NeighborNpc>();
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/_MemosIsland/Resources/NeighborActor.prefab");
            Object.DestroyImmediate(go);
        }

        // ------------------------------------------------------------------ Ayudas de mapa

        static void HangingSign(Phase1WorldBuilder.MapBuilder m, int doorX, int y, string sprite)
        {
            var go = Phase1WorldBuilder.Child(m.Props, $"Sign {sprite}", new Vector3(doorX + 0.5f, y + 1.5f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S($"Town/{sprite}");
            sr.sortingOrder = 2; // sobre la pared, detrás de la gente
        }

        static Station StationAt(Phase1WorldBuilder.MapBuilder m, string name, Vector3 pos, Sprite sprite, Vector2 size,
            StationKind kind, string keeper = null)
        {
            var go = m.Prop(name, pos, sprite, size, new Vector2(0f, size.y / 2f));
            var st = go.AddComponent<Station>();
            st.Setup(kind, keeper);
            return st;
        }

        static void Director(List<Vector2Int> exits) =>
            new GameObject("Neighbors").AddComponent<NeighborDirector>().Setup(exits);

        // ------------------------------------------------------------------ Pueblo Puerto

        static void BuildPueblo()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Pueblo Puerto", 48, 32);

            // Suelo: mar, playa, muelle, calle principal, plaza y caminitos a cada puerta.
            m.Fill(m.Ground, T("grass"), 0, 0, 47, 31);
            m.Fill(m.Ground, T("water"), 0, 0, 47, 4);
            m.Fill(m.Ground, T("sand"), 0, 5, 47, 6);
            m.Fill(m.Ground, T("dock"), 22, 0, 23, 4);
            m.Fill(m.Ground, T("path"), 0, 12, 47, 12);
            m.Fill(m.Ground, T("path"), 22, 7, 23, 11);
            m.Fill(m.Ground, T("path"), 19, 13, 28, 18);
            foreach (var x in new[] { 5, 13, 33, 42 }) m.Fill(m.Ground, T("path"), x, 13, x, 19);
            m.Fill(m.Ground, T("tall_grass"), 19, 24, 28, 28);
            m.Set(m.Ground, T("flowers"), (2, 14), (9, 15), (16, 17), (30, 16), (37, 15), (45, 17), (11, 26), (35, 27),
                (18, 19), (29, 19), (3, 9), (20, 10), (34, 10), (45, 9), (16, 28), (40, 29));

            // Bosque alrededor (el mar abajo; la salida al refugio, a la derecha por la calle).
            for (int x = 0; x < 48; x += 2)
            {
                m.Tree(x, 30);
                if (x < 18 || x > 28) m.Tree(x, 28);
            }
            for (int y = 7; y <= 27; y++)
            {
                if (y != 12) m.Tree(0, y);
                if (y < 11 || y > 13) m.Tree(46, y);
            }

            // Fila de arriba: locales con interior.
            m.House(3, 20, 6, 5, new[] { 4, 7 });
            m.Warp(5, 20, Phase7NeighborContent.Almacen, "entrada");
            HangingSign(m, 5, 20, "sign_shop");
            m.House(11, 20, 6, 13, new[] { 12, 15 });
            m.Warp(13, 20, Phase7NeighborContent.Herreria, "entrada");
            HangingSign(m, 13, 20, "sign_forge");
            m.House(31, 20, 6, 33, new[] { 32, 35 });
            m.Warp(33, 20, Phase7NeighborContent.Clinica, "entrada");
            HangingSign(m, 33, 20, "sign_clinic");
            m.House(39, 20, 7, 42, new[] { 40, 44 });
            m.Warp(42, 20, Phase7NeighborContent.Taberna, "entrada");
            HangingSign(m, 42, 20, "sign_tavern");

            // Fila de abajo, mirando al mar: cerrados por ahora (Fase 8).
            m.House(3, 7, 7, 6, new[] { 4, 8 }, "Municipio de Pueblo Puerto. Atención con turno, de 9 a 17.");
            HangingSign(m, 6, 7, "sign_town");
            m.House(12, 7, 6, 14, new[] { 13, 16 }, "Casa de la familia de Lalo. Adentro alguien arregla una red de pesca.");
            m.House(26, 7, 6, 28, new[] { 27, 30 }, "Oficina de Ápice. \"Solo personal autorizado.\"");
            HangingSign(m, 28, 7, "sign_apice");
            m.House(36, 7, 9, 40, new[] { 37, 43 }, "Estadio de la Copa de la Isla. La Copa todavía no empezó.");
            HangingSign(m, 40, 7, "sign_stadium");

            // Plaza: tablón de pedidos, faroles y carteles.
            StationAt(m, "Tablón de pedidos", new Vector3(24f, 18f), S("Town/board"), new Vector2(1.9f, 0.9f), StationKind.Board);
            foreach (var (x, y) in new[] { (10, 13), (17, 13), (30, 13), (37, 13), (18, 16), (29, 16) }) m.Lamp(x, y);
            m.Sign(44, 13, "PUEBLO PUERTO", "← Plaza y locales · Refugio del abuelo →");
            m.Sign(21, 6, "Muelle del puerto.", "El barco rompehielos está en reparación. ¡Vuelve pronto!");

            // Desafíos de prueba (Fase 3) hasta que la Copa tenga su historia.
            m.Challenge(18, 6, RaceFormat.Trainer, "Carrera contra Lalo",
                "DESAFÍO DE PRUEBA: carrera de 3 tramos contra Lalo. ¿Corrés?",
                new[] { new RaceSegmentDef("pradera", 70), new RaceSegmentDef("rio", 60), new RaceSegmentDef("arena", 70) },
                "Lalo: \"¡Uh! Sos más rápido de lo que pensaba.\"", "Lalo: \"¡Je! Te gané. ¿Revancha?\"",
                ("Lalo", new[] { ("plumin", 5, false), ("chispin", 5, false), ("topin", 4, false) }));
            m.Challenge(38, 6, RaceFormat.Cup, "Copa de la Isla (prueba)",
                "DESAFÍO DE PRUEBA: Copa de 6 tramos contra 3 rivales. ¿Corrés?",
                new[]
                {
                    new RaceSegmentDef("pradera", 70), new RaceSegmentDef("barro", 60), new RaceSegmentDef("rio", 60),
                    new RaceSegmentDef("hielo", 60), new RaceSegmentDef("montana", 60), new RaceSegmentDef("arena", 70),
                },
                "¡Ganaste la Copa de prueba!", "La Copa se escapó esta vez…",
                ("Lalo", new[] { ("plumin", 6, false), ("chispin", 5, false), ("topin", 5, false) }),
                ("Agente de Ápice", new[] { ("copito", 6, true), ("zumbi", 6, true), ("bostezo", 6, true) }),
                ("Capitana Vera", new[] { ("tuerquita", 7, false), ("ferrolobo", 7, false), ("imanta", 7, false) }));

            m.Encounters("pradera",
                ("plumin", 3, 5, 5, false, false), ("zumbi", 3, 5, 4, false, false),
                ("bostezo", 4, 6, 3, true, false), ("zumbi", 4, 5, 1, false, true));

            Phase6WorldBuilder.Forage(m.Props, "Forage Playa", new List<Vector2Int>
            {
                new(3, 5), new(7, 6), new(10, 5), new(16, 5), new(26, 5), new(31, 6), new(35, 5), new(44, 6),
            }, new List<string> { "alga", "alga", "concha" }, 3);
            Phase6WorldBuilder.Forage(m.Props, "Forage Pradera", new List<Vector2Int>
            {
                new(4, 26), new(9, 27), new(14, 25), new(33, 26), new(38, 25), new(43, 25), new(10, 25), new(30, 25),
            }, new List<string> { "flor", "hierba", "fruto_silvestre", "pluma" }, 3);

            m.Spawn(24, 13, "default", Direction.Down);
            m.Spawn(46, 12, "este", Direction.Left);
            m.Spawn(5, 19, "almacen", Direction.Down);
            m.Spawn(13, 19, "herreria", Direction.Down);
            m.Spawn(33, 19, "clinica", Direction.Down);
            m.Spawn(42, 19, "taberna", Direction.Down);
            m.Warp(47, 12, Phase1WorldBuilder.RefugioScene, "oeste");

            Director(new List<Vector2Int>
            {
                new(47, 12), new(5, 20), new(13, 20), new(33, 20), new(42, 20),
                new(6, 6), new(14, 6), new(28, 6), new(40, 6),
            });
            Phase1WorldBuilder.SaveMap(P);
        }

        // ------------------------------------------------------------------ Interiores

        static void BuildInteriors()
        {
            Interior(Phase7NeighborContent.Almacen, "Almacén de Deny", "almacen", m =>
            {
                m.Fill(m.Buildings, T("counter"), 2, 4, 8, 4);
                StationAt(m, "Mostrador", new Vector3(5.5f, 4f), null, new Vector2(0.9f, 0.9f), StationKind.DenyShop, "deny");
                m.Prop("Shelf", new Vector3(3f, 6f), S("Town/shelf"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Shelf", new Vector3(8f, 6f), S("Town/shelf"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Barrel", new Vector3(10.5f, 1f), S("Town/barrel"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Plant", new Vector3(1.5f, 1f), S("Furniture/plant"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
            });
            Interior(Phase7NeighborContent.Herreria, "Herrería de Fer", "herreria", m =>
            {
                m.Fill(m.Buildings, T("counter"), 2, 4, 8, 4);
                StationAt(m, "Mostrador", new Vector3(5.5f, 4f), null, new Vector2(0.9f, 0.9f), StationKind.FerForge, "fer");
                m.Prop("Forge", new Vector3(2f, 5f), S("Machines/smelter"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Anvil", new Vector3(2.5f, 2f), S("Town/anvil"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Barrel", new Vector3(10.5f, 6f), S("Town/barrel"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Barrel", new Vector3(10.5f, 1f), S("Town/barrel"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
            });
            Interior(Phase7NeighborContent.Clinica, "Clínica de Anni", "clinica", m =>
            {
                m.Fill(m.Buildings, T("counter"), 2, 4, 7, 4);
                StationAt(m, "Mostrador", new Vector3(4.5f, 4f), null, new Vector2(0.9f, 0.9f), StationKind.Clinic, "anni");
                m.Prop("Bed", new Vector3(9f, 2f), S("Town/clinic_bed"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Shelf", new Vector3(3f, 6f), S("Town/shelf"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Plant", new Vector3(10.5f, 6f), S("Furniture/plant"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Plant", new Vector3(1.5f, 1f), S("Furniture/plant"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
            });
            Interior(Phase7NeighborContent.Taberna, "Taberna del Puerto", "taberna", m =>
            {
                m.Fill(m.Buildings, T("counter"), 4, 5, 9, 5);
                m.Prop("Table", new Vector3(3f, 3f), S("Furniture/table"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Prop("Table", new Vector3(8f, 3f), S("Furniture/table"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
                foreach (var x in new[] { 5.5f, 7.5f, 9.5f })
                    m.Prop("Barrel", new Vector3(x, 6f), S("Town/barrel"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
                m.Sign(1, 6, "Pizarra de la taberna:", "\"Jugo de bayamemo del día. Historias de pescadores: gratis.\"");
            });
        }

        /// <summary>Local de 12x9: piso, paredes con ventanas, la puerta abajo al medio y la luz cálida.</summary>
        static void Interior(string scene, string displayName, string backSpawn, Action<Phase1WorldBuilder.MapBuilder> furnish)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder(displayName, 12, 9);
            m.Indoor();
            m.Fill(m.Ground, T("in_floor"), 1, 1, 10, 6);
            m.Fill(m.Buildings, T("in_wall_top"), 0, 8, 11, 8);
            m.Fill(m.Buildings, T("in_wall"), 1, 7, 10, 7);
            m.Set(m.Buildings, T("in_window"), (3, 7), (8, 7));
            for (int y = 0; y <= 7; y++) m.Set(m.Buildings, T("in_wall_top"), (0, y), (11, y));
            for (int x = 1; x <= 10; x++) if (x != 6) m.Set(m.Buildings, T("in_wall_top"), (x, 0));
            m.Set(m.Ground, T("in_doormat"), (6, 0));

            furnish(m);

            var lamp = new GameObject("Light").AddComponent<Light2D>();
            lamp.transform.position = new Vector3(6f, 4.5f, 0f);
            lamp.lightType = Light2D.LightType.Point;
            lamp.pointLightOuterRadius = 9f;
            lamp.pointLightInnerRadius = 3f;
            lamp.intensity = 0.6f;
            lamp.color = new Color(1f, 0.85f, 0.65f);

            m.Spawn(6, 1, "entrada", Direction.Up);
            m.Spawn(6, 1, "default", Direction.Up);
            m.Warp(6, 0, P, backSpawn);
            Director(new List<Vector2Int> { new(6, 1) });
            Phase1WorldBuilder.SaveMap(scene);
        }

        // ------------------------------------------------------------------ Refugio y Build Settings

        static void AddToRefugio()
        {
            var scene = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase1WorldBuilder.RefugioScene}.unity");
            Director(new List<Vector2Int> { new(1, 10), new(28, 4) });
            EditorSceneManager.SaveScene(scene);
        }

        static void AddToBuildSettings()
        {
            var paths = new[] { Phase7NeighborContent.Almacen, Phase7NeighborContent.Herreria, Phase7NeighborContent.Clinica,
                Phase7NeighborContent.Taberna }.Select(s => $"{MapsFolder}/{s}.unity").ToList();
            var scenes = EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path)).ToList();
            scenes.AddRange(paths.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
