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
    /// Fase 8C (clímax y cierre): corre todo lo anterior y suma el Estadio de la Copa (mapa con pista e inscripciones),
    /// abre su puerta en el pueblo cuando termina el Acto 3 y prepara los sprites del plano final.
    /// </summary>
    public static class Phase8CupBuilder
    {
        const string MapsFolder = "Assets/_MemosIsland/Scenes/Maps";

        static Sprite S(string path) => Phase1WorldBuilder.S(path);
        static TileBase T(string name) => Phase1WorldBuilder.T(name);

        [MenuItem("Memos Island/Fase 8/Construir Copa y cierre (y todo)")]
        public static void Build()
        {
            Phase8ZonesBuilder.Build();
            BuildStoryArt();
            BuildEstadio();
            AddToPueblo();
            AddToBuildSettings();
            EditorSceneManager.OpenScene($"Assets/_MemosIsland/Scenes/{TitleScreen.SceneName}.unity");
            Debug.Log("[Fase 8C] Copa de la Isla y cierre del Capítulo 1 listos.");
        }

        static void BuildStoryArt()
        {
            const string path = "Assets/_MemosIsland/Resources/StoryArt.asset";
            var art = AssetDatabase.LoadAssetAtPath<StoryArtSet>(path);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<StoryArtSet>();
                AssetDatabase.CreateAsset(art, path);
            }
            art.silex = S("Zones/silex");
            art.scarfFigure = S("Zones/scarf_figure");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Estadio

        static void BuildEstadio()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Estadio de la Copa", 24, 16);
            m.Fill(m.Ground, T("grass"), 0, 0, 23, 15);
            m.Fill(m.Ground, T("sand"), 2, 2, 21, 13);
            m.Fill(m.Ground, T("grass"), 4, 4, 19, 11);
            m.Fill(m.Ground, T("path"), 11, 0, 12, 1);
            m.Set(m.Ground, T("flowers"), (6, 6), (17, 9), (9, 10), (14, 5));

            // Tribunas arriba y muros alrededor (la entrada, abajo al medio).
            m.Fill(m.Buildings, T("house_roof"), 0, 15, 23, 15);
            m.Fill(m.Buildings, T("house_wall"), 0, 14, 23, 14);
            for (int y = 0; y < 14; y++) m.Set(m.Buildings, T("house_wall"), (0, y), (23, y));
            for (int x = 1; x < 23; x++) if (x != 11 && x != 12) m.Set(m.Buildings, T("house_wall"), (x, 0));

            foreach (var (x, sprite) in new[] { (6, "sign_apice"), (12, "sign_stadium"), (17, "sign_apice") })
            {
                var go = Phase1WorldBuilder.Child(m.Props, $"Banner {sprite}", new Vector3(x + 0.5f, 14.5f));
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = S($"Town/{sprite}");
                r.sortingOrder = 2;
            }
            foreach (var (x, y) in new[] { (1, 13), (22, 13), (1, 1), (22, 1) }) m.Lamp(x, y);

            // Mesa de inscripciones (mostrador) y su cartel.
            m.Set(m.Buildings, T("counter"), (16, 1), (17, 1));
            var desk = m.Prop("Inscripciones", new Vector3(16.5f, 1f), null, new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
            desk.AddComponent<CupDesk>();
            m.Sign(15, 1, "COPA DE LA ISLA", "Los sábados de 10 a 18. Tres rondas. Premio: 5000 monedas.",
                "\"Patrocina ÁPICE: el futuro de las carreras.\"");

            m.Spawn(12, 1, "entrada", Direction.Up);
            m.Spawn(12, 1, "default", Direction.Up);
            m.Warp(11, 0, Phase7NeighborContent.Pueblo, "estadio");
            m.Warp(12, 0, Phase7NeighborContent.Pueblo, "estadio");
            Phase1WorldBuilder.SaveMap(StoryDirector.Estadio);
        }

        /// <summary>La puerta del estadio en el pueblo: lleva al estadio, cerrada hasta que termina el Acto 3.</summary>
        static void AddToPueblo()
        {
            var scene = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase7NeighborContent.Pueblo}.unity");
            var props = GameObject.Find("Props").transform;
            // La Copa de verdad reemplaza al cartel de la "Copa de prueba" (Fase 3).
            foreach (var sign in props.Cast<Transform>().Where(t => t.name == "Challenge").ToList())
                if (Mathf.Approximately(sign.position.x, 38.5f)) Object.DestroyImmediate(sign.gameObject);
            foreach (var door in props.Cast<Transform>().Where(t => t.name == "Door").ToList())
                if (Mathf.Approximately(door.position.x, 40.5f) && Mathf.Approximately(door.position.y, 7f))
                    Object.DestroyImmediate(door.gameObject);
            Phase8StoryBuilder.AddWarp(props, 40, 7, StoryDirector.Estadio, "entrada");
            Phase8StoryBuilder.AddSpawn(props, 40, 6, "estadio", Direction.Down);
            var gate = new GameObject("Puerta del estadio (cerrada)");
            gate.transform.SetParent(props, false);
            gate.transform.position = new Vector3(40.5f, 7f);
            gate.layer = LayerMask.NameToLayer("Solid");
            var col = gate.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);
            col.offset = new Vector2(0f, 0.5f);
            gate.AddComponent<StoryBlocker>().Setup("act3_done",
                "Estadio de la Copa de la Isla. Las puertas están cerradas.",
                "Un cartel dice: \"Inscripciones próximamente\".");
            EditorSceneManager.SaveScene(scene);
        }

        static void AddToBuildSettings()
        {
            var path = $"{MapsFolder}/{StoryDirector.Estadio}.unity";
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
