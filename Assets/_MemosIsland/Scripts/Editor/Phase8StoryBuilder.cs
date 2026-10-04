using System.Collections.Generic;
using System.Linq;
using MemosIsland.Story;
using MemosIsland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 8A (historia, Actos 1 y 2): corre todo lo anterior y suma la pantalla de título, los peinados del jugador,
    /// el Bosque Susurro (con el claro del inicial), la salida norte del refugio y el muelle de llegada.
    /// </summary>
    public static class Phase8StoryBuilder
    {
        const string MapsFolder = "Assets/_MemosIsland/Scenes/Maps";
        const string ScenesFolder = "Assets/_MemosIsland/Scenes";

        static Sprite S(string path) => Phase1WorldBuilder.S(path);
        static TileBase T(string name) => Phase1WorldBuilder.T(name);

        [MenuItem("Memos Island/Fase 8/Construir historia (y todo)")]
        public static void Build()
        {
            Phase7TownBuilder.Build();
            BuildPlayerStyles();
            BuildBosque();
            AddToRefugio();
            AddToPueblo();
            BuildTitle();
            AddToBuildSettings();
            EditorSceneManager.OpenScene($"{ScenesFolder}/{TitleScreen.SceneName}.unity");
            Debug.Log("[Fase 8] Historia (Actos 1 y 2) lista. Abrí la escena Title y dale Play.");
        }

        // ------------------------------------------------------------------ Peinados del jugador

        static Sprite[] Frames(string style, string dir) =>
            Enumerable.Range(0, 3).Select(i => S($"PlayerStyles/player_{style}_{dir}_{i}")).ToArray();

        static void BuildPlayerStyles()
        {
            const string path = "Assets/_MemosIsland/Resources/PlayerStyles.asset";
            var set = AssetDatabase.LoadAssetAtPath<PlayerStyleSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<PlayerStyleSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            set.styles = new List<PlayerStyleSet.Style>();
            for (int i = 0; i < 4; i++)
            {
                var id = $"s{i}";
                set.styles.Add(new PlayerStyleSet.Style
                {
                    name = PlayerLook.HairStyles[i],
                    down = Frames(id, "down"), up = Frames(id, "up"), left = Frames(id, "left"), right = Frames(id, "right"),
                });
            }
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Título

        static void BuildTitle()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("", 1, 1);
            m.Indoor();
            m.Spawn(0, 0, "default", Direction.Down);
            new GameObject("Title Screen").AddComponent<TitleScreen>().Setup(S("Story/eyes"));
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
                $"{ScenesFolder}/{TitleScreen.SceneName}.unity");
        }

        // ------------------------------------------------------------------ Bosque Susurro

        static void BuildBosque()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Bosque Susurro", 32, 24);
            m.Fill(m.Ground, T("forest_floor"), 0, 0, 31, 23);

            // Senda serpenteante de la entrada (abajo) al claro (arriba).
            m.Fill(m.Ground, T("path"), 15, 0, 16, 7);
            m.Fill(m.Ground, T("path"), 8, 6, 16, 7);
            m.Fill(m.Ground, T("path"), 8, 6, 9, 13);
            m.Fill(m.Ground, T("path"), 8, 12, 20, 13);
            m.Fill(m.Ground, T("path"), 19, 12, 20, 15);

            // Helechos (pasto alto del bosque): encuentros.
            m.Fill(m.Ground, T("forest_grass"), 2, 2, 6, 5);
            m.Fill(m.Ground, T("forest_grass"), 20, 2, 27, 7);
            m.Fill(m.Ground, T("forest_grass"), 2, 14, 6, 19);
            m.Fill(m.Ground, T("forest_grass"), 23, 11, 28, 18);
            m.Fill(m.Ground, T("forest_grass"), 11, 8, 14, 10);

            // Árboles: borde completo (con la entrada abajo al medio) y algunos sueltos.
            for (int x = 0; x < 32; x += 2)
            {
                m.Tree(x, 22);
                if (x != 14 && x != 16) m.Tree(x, 0);
            }
            for (int y = 2; y <= 21; y++)
            {
                m.Tree(0, y);
                m.Tree(30, y);
            }
            foreach (var (x, y) in new[]
                     {
                         (3, 7), (5, 10), (11, 1), (19, 1), (22, 9), (26, 1), (12, 4), (17, 9), (2, 12), (6, 20),
                         (25, 20), (27, 9), (22, 18), (8, 16), (9, 20), (4, 0), (21, 15),
                     })
                m.Tree(x, y);

            m.Encounters("bosque",
                ("zumbi", 4, 6, 5, false, false), ("plumin", 3, 5, 2, false, false),
                ("bostezo", 4, 6, 4, true, false), ("zumbi", 5, 6, 1, false, true));
            Phase6WorldBuilder.Forage(m.Props, "Forage Bosque", new List<Vector2Int>
            {
                new(3, 8), new(10, 5), new(18, 10), new(25, 10), new(13, 15), new(6, 13), new(27, 19), new(4, 21),
            }, new List<string> { "fruto_silvestre", "hierba", "flor", "pluma" }, 3);

            m.Spawn(15, 1, "sur", Direction.Up);
            m.Spawn(15, 1, "default", Direction.Up);
            m.Warp(15, 0, Phase1WorldBuilder.RefugioScene, "norte");
            m.Warp(16, 0, Phase1WorldBuilder.RefugioScene, "norte");
            Phase1WorldBuilder.SaveMap(StoryDirector.Bosque);
        }

        // ------------------------------------------------------------------ Refugio y pueblo

        /// <summary>Abre la salida norte del refugio (hacia el Bosque Susurro) sacando dos árboles del borde.</summary>
        static void AddToRefugio()
        {
            var scene = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase1WorldBuilder.RefugioScene}.unity");
            var props = GameObject.Find("Props").transform;
            foreach (var tree in props.Cast<Transform>().Where(t => t.name == "Tree").ToList())
            {
                var p = tree.position;
                if ((Mathf.Approximately(p.x, 15f) || Mathf.Approximately(p.x, 17f)) && p.y >= 20f) Object.DestroyImmediate(tree.gameObject);
            }
            AddWarp(props, 15, 21, StoryDirector.Bosque, "sur");
            AddWarp(props, 16, 21, StoryDirector.Bosque, "sur");
            AddSpawn(props, 15, 20, "norte", Direction.Down);
            var sign = new GameObject("Sign Bosque");
            sign.transform.SetParent(props, false);
            sign.transform.position = new Vector3(13.5f, 19f);
            sign.layer = LayerMask.NameToLayer("Solid");
            var sr = sign.AddComponent<SpriteRenderer>();
            sr.sprite = S("Props/sign");
            sr.sortingOrder = 10;
            var col = sign.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);
            col.offset = new Vector2(0f, 0.5f);
            sign.AddComponent<Sign>().SetPages("↑ BOSQUE SUSURRO", "\"No hagas ruido: el bosque escucha.\"");
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>El jugador llega en barco: aparece en la punta del muelle.</summary>
        static void AddToPueblo()
        {
            var scene = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase7NeighborContent.Pueblo}.unity");
            AddSpawn(GameObject.Find("Props").transform, 22, 2, "muelle", Direction.Up);
            EditorSceneManager.SaveScene(scene);
        }

        static void AddWarp(Transform props, int x, int y, string scene, string spawn)
        {
            var go = new GameObject($"Warp → {scene}");
            go.transform.SetParent(props, false);
            go.transform.position = new Vector3(x + 0.5f, y);
            go.AddComponent<Warp>().Setup(scene, spawn);
        }

        static void AddSpawn(Transform props, int x, int y, string id, Direction facing)
        {
            var go = new GameObject($"Spawn {id}");
            go.transform.SetParent(props, false);
            go.transform.position = new Vector3(x + 0.5f, y);
            go.AddComponent<SpawnPoint>().Setup(id, facing);
        }

        static void AddToBuildSettings()
        {
            var title = $"{ScenesFolder}/{TitleScreen.SceneName}.unity";
            var bosque = $"{MapsFolder}/{StoryDirector.Bosque}.unity";
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != title && s.path != bosque).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(title, true));
            scenes.Add(new EditorBuildSettingsScene(bosque, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
