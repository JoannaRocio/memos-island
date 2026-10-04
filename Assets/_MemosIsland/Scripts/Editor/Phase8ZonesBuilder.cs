using System.Collections.Generic;
using System.Linq;
using MemosIsland.Memos;
using MemosIsland.Story;
using MemosIsland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 8B (Acto 3): corre todo lo anterior y suma las zonas Ruta Pradera, Pantano Pantufla, Colina Escarcha y
    /// Acantilados Tormenta (con sus pasos cerrados por la historia), el primer Memo con collar, Karman,
    /// el collar roto y las pistas de Draken y Randy.
    /// </summary>
    public static class Phase8ZonesBuilder
    {
        const string MapsFolder = "Assets/_MemosIsland/Scenes/Maps";
        const string Refugio = Phase1WorldBuilder.RefugioScene;

        static Sprite S(string path) => Phase1WorldBuilder.S(path);
        static TileBase T(string name) => Phase1WorldBuilder.T(name);

        [MenuItem("Memos Island/Fase 8/Construir Acto 3 (y todo)")]
        public static void Build()
        {
            Phase8StoryBuilder.Build();
            BuildItems();
            BuildRuta();
            BuildPantano();
            BuildColina();
            BuildAcantilados();
            AddToRefugio();
            AddHints();
            AddToBuildSettings();
            EditorSceneManager.OpenScene($"Assets/_MemosIsland/Scenes/{TitleScreen.SceneName}.unity");
            Debug.Log("[Fase 8B] Acto 3 listo: Ruta Pradera, Pantano, Colina, Acantilados, collares y Karman.");
        }

        // ------------------------------------------------------------------ Objetos

        static void BuildItems()
        {
            const string path = "Assets/_MemosIsland/Data/Items/collar_roto.asset";
            var db = AssetDatabase.LoadAssetAtPath<MemoDatabase>("Assets/_MemosIsland/Resources/MemoDatabase.asset");
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.id = "collar_roto";
            item.displayName = "Collar roto";
            item.description = "Dos pedazos de metal violeta con un triángulo grabado. No se vende.";
            item.kind = ItemKind.Material;
            item.price = 0;
            item.icon = S("Zones/icon_collar_roto");
            if (!db.items.Contains(item)) db.items.Add(item);
            EditorUtility.SetDirty(item);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Ayudas

        static GameObject Solid(Transform props, string name, Vector3 pos, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(props, false);
            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Solid");
            if (sprite != null)
            {
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = sprite;
                r.sortingOrder = 10;
            }
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.offset = new Vector2(0f, size.y / 2f);
            return go;
        }

        /// <summary>Paso cerrado hasta que la historia ponga la marca (un cartel con el motivo).</summary>
        static void Gate(Transform props, int x, int y, string flag, params string[] text)
        {
            var go = Solid(props, $"Paso cerrado ({flag})", new Vector3(x + 0.5f, y), S("Props/sign"), new Vector2(0.9f, 0.9f));
            go.AddComponent<StoryBlocker>().Setup(flag, text);
        }

        static void Barrier(Transform props, int x, int y, string flag)
        {
            var go = Solid(props, $"Barrera ({flag})", new Vector3(x + 0.5f, y), null, new Vector2(0.9f, 0.9f));
            go.AddComponent<StoryBlocker>().Setup(flag, "Por acá todavía no se puede pasar.");
        }

        static void StoryMemoAt(Transform props, int x, int y, string species, int level, bool collared, string terrain,
            string flag, string required, bool storm, string intro)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_MemosIsland/Resources/MemoActor.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(props, false);
            go.transform.position = GridMover.CellToWorld(new Vector2Int(x, y));
            go.name = $"Memo de historia ({species})";
            go.AddComponent<StoryMemo>().Setup(species, level, collared, terrain, flag, required, storm, intro);
        }

        static void Border(Phase1WorldBuilder.MapBuilder m, int w, int h, HashSet<(int, int)> skip)
        {
            for (int x = 0; x < w; x += 2)
            {
                if (!skip.Contains((x, h - 2))) m.Tree(x, h - 2);
                if (!skip.Contains((x, 0))) m.Tree(x, 0);
            }
            for (int y = 1; y < h - 2; y++)
            {
                if (!skip.Contains((0, y))) m.Tree(0, y);
                if (!skip.Contains((w - 2, y))) m.Tree(w - 2, y);
            }
        }

        // ------------------------------------------------------------------ Ruta Pradera

        static void BuildRuta()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Ruta Pradera", 40, 20);
            m.Fill(m.Ground, T("grass"), 0, 0, 39, 19);
            m.Fill(m.Ground, T("path"), 0, 9, 39, 10);
            m.Fill(m.Ground, T("path"), 19, 0, 20, 8);
            m.Fill(m.Ground, T("path"), 19, 11, 20, 19);
            m.Fill(m.Ground, T("tall_grass"), 4, 2, 9, 6);
            m.Fill(m.Ground, T("tall_grass"), 13, 13, 17, 16);
            m.Fill(m.Ground, T("tall_grass"), 25, 2, 31, 6);
            m.Fill(m.Ground, T("tall_grass"), 30, 13, 35, 16);
            m.Set(m.Ground, T("flowers"), (3, 12), (11, 4), (23, 14), (27, 9), (36, 4), (8, 15), (15, 6), (32, 11));

            Border(m, 40, 20, new HashSet<(int, int)> { (18, 18), (20, 18), (18, 0), (20, 0), (0, 9), (0, 10), (38, 9), (38, 10) });
            foreach (var (x, y) in new[] { (11, 14), (23, 2), (34, 8), (6, 8) }) m.Tree(x, y);

            m.Encounters("pradera",
                ("plumin", 4, 6, 5, false, false), ("zumbi", 4, 6, 4, false, false), ("topin", 4, 6, 2, false, false),
                ("chispin", 5, 7, 1, false, false), ("bostezo", 5, 7, 2, true, false), ("zumbi", 5, 6, 1, false, true));
            StoryMemoAt(m.Props, 14, 10, "zumbi", 5, true, "pradera", "first_collar", "zorak_lesson", false,
                "(Un Zumbi con collar violeta bloquea el camino. Zumba bajito, sin ganas, con los ojos apagados.)");
            Phase6WorldBuilder.Forage(m.Props, "Forage Ruta", new List<Vector2Int>
            {
                new(3, 13), new(12, 7), new(22, 6), new(28, 12), new(35, 7), new(16, 3), new(24, 16), new(5, 16),
            }, new List<string> { "flor", "hierba", "fruto_silvestre", "pluma" }, 4);
            m.Sign(3, 11, "RUTA PRADERA", "← Refugio · Acantilados →", "↑ Colina Escarcha · ↓ Pantano Pantufla");

            Gate(m.Props, 21, 17, "colina_open", "Hay nieve y hielo en el camino de la Colina Escarcha. Mejor volver con más experiencia.");
            Barrier(m.Props, 19, 17, "colina_open");
            Barrier(m.Props, 20, 17, "colina_open");
            Gate(m.Props, 21, 2, "pantano_open", "El camino al Pantano Pantufla está embarrado y oscuro. Todavía no tenés motivo para meterte ahí.");
            Barrier(m.Props, 19, 2, "pantano_open");
            Barrier(m.Props, 20, 2, "pantano_open");
            Gate(m.Props, 37, 11, "acantilados_open", "Los Acantilados Tormenta. El viento sopla fuerte: Zorak dijo que todavía no.");
            Barrier(m.Props, 37, 9, "acantilados_open");
            Barrier(m.Props, 37, 10, "acantilados_open");

            m.Spawn(1, 10, "oeste", Direction.Right);
            m.Spawn(1, 10, "default", Direction.Right);
            m.Spawn(38, 10, "este", Direction.Left);
            m.Spawn(19, 18, "norte", Direction.Down);
            m.Spawn(19, 1, "sur", Direction.Up);
            foreach (var y in new[] { 9, 10 })
            {
                m.Warp(0, y, Refugio, "este");
                m.Warp(39, y, StoryDirector.Acantilados, "oeste");
            }
            foreach (var x in new[] { 19, 20 })
            {
                m.Warp(x, 19, StoryDirector.Colina, "sur");
                m.Warp(x, 0, StoryDirector.Pantano, "norte");
            }
            Phase1WorldBuilder.SaveMap(StoryDirector.Ruta);
        }

        // ------------------------------------------------------------------ Pantano Pantufla

        static void BuildPantano()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Pantano Pantufla", 32, 20);
            m.Fill(m.Ground, T("mud"), 0, 0, 31, 19);
            m.Fill(m.Ground, T("water"), 6, 4, 10, 7);
            m.Fill(m.Ground, T("water"), 21, 10, 25, 13);
            m.Fill(m.Ground, T("mud_reeds"), 11, 2, 13, 6);
            m.Fill(m.Ground, T("mud_reeds"), 3, 11, 8, 15);
            m.Fill(m.Ground, T("mud_reeds"), 24, 3, 28, 7);
            m.Fill(m.Ground, T("mud_reeds"), 18, 14, 21, 16);
            m.Fill(m.Ground, T("path"), 15, 8, 16, 19);

            Border(m, 32, 20, new HashSet<(int, int)> { (14, 18), (16, 18) });
            foreach (var (x, y) in new[] { (3, 6), (27, 15), (12, 12), (18, 4) }) m.Tree(x, y);

            m.Encounters("barro",
                ("pantuflo", 7, 9, 5, false, false), ("topin", 7, 8, 2, false, false),
                ("bostezo", 7, 9, 2, true, false), ("pantuflo", 8, 9, 1, false, true));
            Phase6WorldBuilder.Forage(m.Props, "Forage Pantano", new List<Vector2Int>
            {
                new(4, 9), new(13, 9), new(19, 7), new(27, 9), new(9, 17), new(23, 17),
            }, new List<string> { "hierba", "hierba", "fruto_silvestre" }, 3);
            m.Sign(18, 9, "Un cartel tirado en el barro:", "\"ÁPICE — Propiedad privada. Prohibido el paso.\"",
                "Al lado, huellas de ruedas que se pierden entre los juncos.");

            m.Spawn(15, 18, "norte", Direction.Down);
            m.Spawn(15, 18, "default", Direction.Down);
            m.Warp(15, 19, StoryDirector.Ruta, "sur");
            m.Warp(16, 19, StoryDirector.Ruta, "sur");
            Phase1WorldBuilder.SaveMap(StoryDirector.Pantano);
        }

        // ------------------------------------------------------------------ Colina Escarcha

        static void BuildColina()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Colina Escarcha", 32, 20);
            m.Fill(m.Ground, T("snow"), 0, 0, 31, 19);
            m.Fill(m.Ground, T("ice"), 10, 8, 20, 11);
            m.Fill(m.Ground, T("snow_grass"), 3, 3, 8, 7);
            m.Fill(m.Ground, T("snow_grass"), 22, 3, 27, 8);
            m.Fill(m.Ground, T("snow_grass"), 4, 12, 9, 16);
            m.Fill(m.Ground, T("snow_grass"), 22, 13, 27, 16);
            m.Fill(m.Ground, T("ice"), 15, 17, 16, 19);

            Border(m, 32, 20, new HashSet<(int, int)> { (14, 0), (16, 0), (14, 18), (16, 18) });
            foreach (var (x, y) in new[] { (12, 4), (18, 14), (11, 15) }) m.Tree(x, y);

            m.Encounters("nieve",
                ("copito", 9, 11, 5, false, false), ("plumin", 9, 10, 2, false, false), ("copito", 10, 11, 1, false, true));
            Phase6WorldBuilder.Forage(m.Props, "Forage Colina", new List<Vector2Int>
            {
                new(6, 9), new(14, 4), new(24, 10), new(20, 6), new(13, 14), new(26, 17),
            }, new List<string> { "pluma", "cuarzo", "flor" }, 3);

            // Playa Helada (próxima actualización): muro de hielo.
            foreach (var x in new[] { 15, 16 })
            {
                var go = Solid(m.Props, "Muro de hielo", new Vector3(x + 0.5f, 18f), null, new Vector2(0.9f, 0.9f));
                go.AddComponent<StoryBlocker>().Setup("",
                    "Un muro de hielo gigante tapa el camino a la Playa Helada.",
                    "Del otro lado se escucha algo enorme respirando. El hielo cruje con cada respiro.",
                    "Para pasar haría falta el barco rompehielos del puerto… que está \"en reparación, vuelve pronto\".");
            }
            m.Sign(17, 16, "↑ PLAYA HELADA", "\"Cerrado por hielo.\"");

            m.Spawn(15, 1, "sur", Direction.Up);
            m.Spawn(15, 1, "default", Direction.Up);
            m.Warp(15, 0, StoryDirector.Ruta, "norte");
            m.Warp(16, 0, StoryDirector.Ruta, "norte");
            Phase1WorldBuilder.SaveMap(StoryDirector.Colina);
        }

        // ------------------------------------------------------------------ Acantilados Tormenta

        static void BuildAcantilados()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Acantilados Tormenta", 32, 22);
            m.Fill(m.Ground, T("cliff"), 0, 0, 31, 21);
            for (int x = 0; x < 32; x++) m.Set(m.Buildings, T("cliff_wall"), (x, 0), (x, 21));
            for (int y = 0; y < 22; y++)
            {
                m.Set(m.Buildings, T("cliff_wall"), (31, y));
                if (y < 9 || y > 12) m.Set(m.Buildings, T("cliff_wall"), (0, y));
            }
            m.Fill(m.Buildings, T("cliff_wall"), 8, 5, 10, 7);
            m.Fill(m.Buildings, T("cliff_wall"), 17, 9, 19, 12);
            m.Fill(m.Buildings, T("cliff_wall"), 13, 15, 15, 17);
            m.Fill(m.Ground, T("cliff_grass"), 3, 2, 7, 6);
            m.Fill(m.Ground, T("cliff_grass"), 21, 3, 27, 7);
            m.Fill(m.Ground, T("cliff_grass"), 3, 14, 9, 18);
            m.Fill(m.Ground, T("cliff_grass"), 20, 13, 23, 17);
            m.Fill(m.Ground, T("path"), 1, 10, 12, 11);

            m.Encounters("montana",
                ("chispin", 11, 13, 5, false, false), ("plumin", 11, 12, 2, false, false), ("chispin", 12, 13, 1, false, true));
            StoryMemoAt(m.Props, 25, 17, "karman", 15, false, "tormenta", "karman", "zorak_photo", true,
                "(¡Entre los relámpagos, un Memo enorme y anguloso te mira desde las rocas! El aire se carga de electricidad: es Karman.)");
            m.Sign(24, 12, "Una piedra grabada:", "\"Cuando el cielo ruge, el que no puede ser alcanzado baja a mirar.\"");

            // Volcán Dormido (próxima actualización): derrumbe.
            var rubble = Solid(m.Props, "Derrumbe", new Vector3(28f, 18f), S("Zones/rubble"), new Vector2(1.9f, 1.5f));
            rubble.AddComponent<StoryBlocker>().Setup("",
                "Un derrumbe enorme tapa el camino al Volcán Dormido.",
                "Fer dijo que haría falta una herramienta que todavía no existe en la isla.");
            m.Sign(25, 19, "↗ VOLCÁN DORMIDO", "\"Peligro: derrumbes.\"");

            m.Spawn(1, 10, "oeste", Direction.Right);
            m.Spawn(1, 10, "default", Direction.Right);
            m.Warp(0, 10, StoryDirector.Ruta, "este");
            m.Warp(0, 11, StoryDirector.Ruta, "este");
            Phase1WorldBuilder.SaveMap(StoryDirector.Acantilados);
        }

        // ------------------------------------------------------------------ Refugio y pistas

        /// <summary>Salida este del refugio hacia la Ruta Pradera (cerrada hasta la lección de Zorak).</summary>
        static void AddToRefugio()
        {
            var scene = EditorSceneManager.OpenScene($"{MapsFolder}/{Refugio}.unity");
            var props = GameObject.Find("Props").transform;
            foreach (var tree in props.Cast<Transform>().Where(t => t.name == "Tree").ToList())
                if (Mathf.Approximately(tree.position.x, 31f) && Mathf.Approximately(tree.position.y, 10f))
                    Object.DestroyImmediate(tree.gameObject);
            Phase8StoryBuilder.AddWarp(props, 31, 10, StoryDirector.Ruta, "oeste");
            Phase8StoryBuilder.AddSpawn(props, 30, 10, "este", Direction.Left);
            var go = Solid(props, "Tronco caído", new Vector3(30.5f, 10f), S("Props/sign"), new Vector2(0.9f, 0.9f));
            go.AddComponent<StoryBlocker>().Setup("route_open",
                "Un tronco caído tapa el camino hacia el este.",
                "Alguien escribió en la corteza: \"No pasen sin saber correr. — Z.\"");
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Pistas de Draken y Randy en el pueblo (GDD §6).</summary>
        static void AddHints()
        {
            var tavern = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase7NeighborContent.Taberna}.unity");
            var props = GameObject.Find("Props").transform;
            var sign = Solid(props, "Recorte de diario", new Vector3(10.5f, 6f), S("Props/sign"), new Vector2(0.9f, 0.9f));
            sign.AddComponent<Sign>().SetPages("Un recorte de diario amarillento, pegado en la pared:",
                "\"AVISTAN DRAGÓN NEGRO SOBRE EL VOLCÁN DORMIDO\"",
                "Abajo, un pescador anotó con lápiz: «Y en la Playa Helada, el mar se congela solo. Algo vive debajo del hielo.»");
            EditorSceneManager.SaveScene(tavern);

            var pueblo = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase7NeighborContent.Pueblo}.unity");
            props = GameObject.Find("Props").transform;
            var poster = Solid(props, "Cartel de recompensa", new Vector3(26.5f, 6f), S("Props/sign"), new Vector2(0.9f, 0.9f));
            poster.AddComponent<Sign>().SetPages("CARTEL DE RECOMPENSA",
                "Se busca: Memo de hielo gigante visto cerca de la Playa Helada. Los pescadores lo llaman \"Randy\".",
                "El dibujo muestra algo enorme y blanco, con ojos celestes que brillan.",
                "Debajo, alguien escribió: «No lo busquen. El mar se congela a su paso.»");
            EditorSceneManager.SaveScene(pueblo);
        }

        static void AddToBuildSettings()
        {
            var paths = new[] { StoryDirector.Ruta, StoryDirector.Pantano, StoryDirector.Colina, StoryDirector.Acantilados }
                .Select(s => $"{MapsFolder}/{s}.unity").ToList();
            var scenes = EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path)).ToList();
            scenes.AddRange(paths.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
