using System.Collections.Generic;
using System.Linq;
using MemosIsland.EditorTools.PixelArt;
using MemosIsland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 6 (mapas): la Cueva de Zorak, y lo que se suma al refugio (huerta, máquinas, entrada a la cueva)
    /// y al pueblo (puestos de Deny y Fer). Se aplica sobre los mapas recién armados por la Fase 1.
    /// </summary>
    public static class Phase6WorldBuilder
    {
        const string MapsFolder = "Assets/_MemosIsland/Scenes/Maps";
        const string CavePath = MapsFolder + "/" + MineFloor.SceneName + ".unity";

        static Sprite S(string path) => Phase1WorldBuilder.S(path);
        static TileBase T(string name) => Phase1WorldBuilder.T(name);

        // ------------------------------------------------------------------ Cueva

        public static void BuildCave()
        {
            AddToRefugio();
            AddToPueblo();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Cueva de Zorak", 20, 14);
            m.Indoor();

            m.Fill(m.Ground, T("cave_floor"), 1, 1, 18, 12);
            for (int x = 0; x < 20; x++) m.Set(m.Buildings, T("cave_wall"), (x, 0), (x, 13));
            for (int y = 0; y < 14; y++) m.Set(m.Buildings, T("cave_wall"), (0, y), (19, y));
            // Columnas sueltas para que no sea una sala vacía.
            m.Set(m.Buildings, T("cave_wall"), (7, 7), (7, 8), (12, 4), (13, 4), (15, 10));
            // Piso áspero: ahí aparecen Memos salvajes.
            m.Fill(m.Ground, T("cave_rough"), 9, 9, 13, 11);
            m.Fill(m.Ground, T("cave_rough"), 2, 2, 5, 4);
            m.Fill(m.Ground, T("cave_rough"), 15, 2, 17, 6);

            // Luz tenue de la cueva (siempre igual, de día o de noche).
            var light = new GameObject("Cave Light").AddComponent<Light2D>();
            light.transform.position = new Vector3(10f, 7f, 0f);
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 14f;
            light.pointLightInnerRadius = 4f;
            light.intensity = 0.5f;
            light.color = new Color(0.75f, 0.8f, 1f);

            var floor = new GameObject("Mine Floor").AddComponent<MineFloor>();
            floor.Setup(new RectInt(1, 1, 18, 12), new Vector2Int(2, 11), m.Ground, T("cave_rough"),
                new[] { S("Cave/rock_stone"), S("Cave/rock_copper"), S("Cave/rock_iron"), S("Cave/rock_quartz"), S("Cave/rock_gem") },
                S("Cave/ladder_down"), S("Cave/ladder_up"), Phase1WorldBuilder.RefugioScene, "cueva");

            m.Spawn(3, 11, "arriba", Direction.Down);
            m.Spawn(3, 11, "default", Direction.Down);

            Phase1WorldBuilder.SaveMap(MineFloor.SceneName);
        }

        public static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != CavePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(CavePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorSceneManager.OpenScene($"{MapsFolder}/{Phase1WorldBuilder.RefugioScene}.unity");
        }

        // ------------------------------------------------------------------ Refugio

        static Station StationProp(Transform props, string name, Vector3 pos, Sprite sprite, Vector2 size, StationKind kind)
        {
            var go = new GameObject(name);
            go.transform.SetParent(props, false);
            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Solid");
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = 10;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.offset = new Vector2(0f, size.y / 2f);
            var station = go.AddComponent<Station>();
            station.Setup(kind);
            return station;
        }

        static void AddToRefugio()
        {
            var scene = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase1WorldBuilder.RefugioScene}.unity");
            var props = GameObject.Find("Props").transform;
            var grid = Object.FindAnyObjectByType<Grid>();

            // Huerta: tilemap propio para la tierra arada, encima del pasto y debajo de los cultivos.
            var soilGo = new GameObject("Farm Soil");
            soilGo.transform.SetParent(grid.transform, false);
            var soil = soilGo.AddComponent<Tilemap>();
            soilGo.AddComponent<TilemapRenderer>().sortingOrder = 1;
            var crops = AssetDatabase.FindAssets("crop_ t:Sprite", new[] { PixelArtGenerator.GeneratedRoot + "/Farm" })
                .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null).ToList();
            var field = new GameObject("Farm Field").AddComponent<FarmField>();
            field.Setup(new RectInt(5, 4, 5, 4), soil, T("soil_dry"), T("soil_wet"), crops);

            // Máquinas
            StationProp(props, "Workbench", new Vector3(3f, 12f), S("Machines/workbench"), new Vector2(1.9f, 0.9f), StationKind.Workbench);
            StationProp(props, "Smelter", new Vector3(20f, 12f), S("Machines/smelter"), new Vector2(1.9f, 0.9f), StationKind.Smelter);
            StationProp(props, "Processor", new Vector3(18.5f, 12f), S("Machines/processor"), new Vector2(0.9f, 0.9f), StationKind.Processor);
            StationProp(props, "Shipping Box", new Vector3(14.5f, 9f), S("Machines/shipping_box"), new Vector2(0.9f, 0.9f), StationKind.ShippingBox);

            // Entrada a la Cueva de Zorak (abajo a la derecha): se entra caminando hacia la boca.
            var entrance = new GameObject("Cave Entrance");
            entrance.transform.SetParent(props, false);
            entrance.transform.position = new Vector3(28f, 5f);
            entrance.layer = LayerMask.NameToLayer("Solid");
            var er = entrance.AddComponent<SpriteRenderer>();
            er.sprite = S("Cave/cave_entrance");
            er.sortingOrder = 10;
            var ec = entrance.AddComponent<BoxCollider2D>();
            ec.size = new Vector2(1.9f, 0.9f);
            ec.offset = new Vector2(0f, 1.5f);
            foreach (var x in new[] { 27, 28 })
            {
                var w = new GameObject($"Warp → {MineFloor.SceneName}");
                w.transform.SetParent(props, false);
                w.transform.position = new Vector3(x + 0.5f, 5f);
                w.AddComponent<Warp>().Setup(MineFloor.SceneName, "arriba");
            }
            var spawn = new GameObject("Spawn cueva");
            spawn.transform.SetParent(props, false);
            spawn.transform.position = new Vector3(27.5f, 4f);
            spawn.AddComponent<SpawnPoint>().Setup("cueva", Direction.Down);

            var sign = new GameObject("Cave Sign");
            sign.transform.SetParent(props, false);
            sign.transform.position = new Vector3(26.5f, 4f);
            sign.layer = LayerMask.NameToLayer("Solid");
            sign.AddComponent<SpriteRenderer>().sprite = S("Props/sign");
            sign.GetComponent<SpriteRenderer>().sortingOrder = 10;
            var sc = sign.AddComponent<BoxCollider2D>();
            sc.size = new Vector2(0.9f, 0.9f);
            sc.offset = new Vector2(0f, 0.5f);
            sign.AddComponent<Sign>().SetPages("CUEVA DE ZORAK",
                "\"Cinco pisos. Las rocas vuelven a crecer cada día.\" — Z.",
                "Con el pico picás rocas (A). Para hierro y cuarzo necesitás el pico de cobre.");

            // Lugares donde trabajan los Memos (Fase 6).
            Object.FindAnyObjectByType<RefugeManager>().SetWorkSpots(new Vector2Int(7, 9), new Vector2Int(21, 11), new Vector2Int(18, 11));

            // Recolección diaria
            Forage(props, "Forage Pradera", new List<Vector2Int>
            {
                new(3, 6), new(12, 5), new(15, 4), new(17, 7), new(22, 10), new(8, 17), new(12, 17),
                new(17, 16), new(24, 18), new(3, 13), new(28, 15), new(19, 17),
            }, new List<string> { "flor", "flor", "hierba", "hierba", "pluma", "fruto_silvestre" }, 5);

            EditorSceneManager.SaveScene(scene);
        }

        internal static void Forage(Transform props, string name, List<Vector2Int> spots, List<string> items, int perDay)
        {
            var go = new GameObject(name);
            go.transform.SetParent(props, false);
            go.AddComponent<ForageSpawner>().Setup(spots, items, perDay);
        }

        // ------------------------------------------------------------------ Pueblo

        static void AddToPueblo()
        {
            var scene = EditorSceneManager.OpenScene($"{MapsFolder}/{Phase1WorldBuilder.PuebloScene}.unity");
            var props = GameObject.Find("Props").transform;

            StationProp(props, "Stall Deny", new Vector3(12f, 15f), S("Machines/stall_deny"), new Vector2(1.9f, 0.9f), StationKind.DenyShop);
            StationProp(props, "Stall Fer", new Vector3(19f, 15f), S("Machines/stall_fer"), new Vector2(1.9f, 0.9f), StationKind.FerForge);

            Forage(props, "Forage Playa", new List<Vector2Int>
            {
                new(3, 7), new(7, 8), new(10, 7), new(20, 8), new(25, 7), new(28, 8), new(12, 8), new(23, 8),
            }, new List<string> { "alga", "alga", "concha" }, 3);
            Forage(props, "Forage Pradera", new List<Vector2Int>
            {
                new(3, 17), new(8, 18), new(16, 18), new(24, 18), new(28, 14), new(10, 16), new(26, 19),
            }, new List<string> { "flor", "hierba", "fruto_silvestre", "pluma" }, 3);

            EditorSceneManager.SaveScene(scene);
        }
    }
}
