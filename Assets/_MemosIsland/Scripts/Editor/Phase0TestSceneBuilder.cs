using System.Linq;
using MemosIsland.EditorTools.PixelArt;
using MemosIsland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MemosIsland.EditorTools
{
    /// <summary>Arma la escena de prueba de la Fase 0 con el arte provisorio generado.</summary>
    public static class Phase0TestSceneBuilder
    {
        const string ScenePath = "Assets/_MemosIsland/Scenes/Fase0_Prueba.unity";
        const string Gen = PixelArtGenerator.GeneratedRoot;

        [MenuItem("Memos Island/Fase 0/Crear escena de prueba")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            PixelArtGenerator.GenerateAll();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            BuildMap();
            PlaceSprites();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Fase 0] Escena creada: {ScenePath}");
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1a, 0x1c, 0x2c, 0xff);
            go.AddComponent<UniversalAdditionalCameraData>();

            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = PixelArtGenerator.PixelsPerUnit;
            ppc.refResolutionX = 480;
            ppc.refResolutionY = 270;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        }

        // ------------------------------------------------------------------ Mapa

        static void BuildMap()
        {
            var grid = new GameObject("Grid").AddComponent<Grid>();
            var ground = CreateTilemap(grid, "Ground", 0);
            var buildings = CreateTilemap(grid, "Buildings", 1);

            TileBase T(string name) =>
                AssetDatabase.LoadAssetAtPath<TileBase>($"{PixelArtGenerator.TilesRoot}/{name}.asset");

            var grass = T("grass");
            var tall = T("tall_grass");
            var flowers = T("flowers");
            var path = T("path");
            var water = T("water");

            // Vista de 30x17 tiles centrada en el origen.
            for (int x = -15; x < 15; x++)
            for (int y = -9; y < 8; y++)
                ground.SetTile(new Vector3Int(x, y, 0), grass);

            for (int x = -15; x < 15; x++) ground.SetTile(new Vector3Int(x, -2, 0), path);
            for (int y = -1; y < 2; y++) ground.SetTile(new Vector3Int(-7, y, 0), path);

            for (int x = 7; x < 12; x++)
            for (int y = 1; y < 5; y++)
                ground.SetTile(new Vector3Int(x, y, 0), water);

            for (int x = -14; x < -8; x++)
            for (int y = -8; y < -4; y++)
                ground.SetTile(new Vector3Int(x, y, 0), tall);

            foreach (var p in new[] { new Vector2Int(-3, 3), new Vector2Int(-2, 4), new Vector2Int(2, 0),
                         new Vector2Int(4, 5), new Vector2Int(-4, -6), new Vector2Int(1, -7) })
                ground.SetTile(new Vector3Int(p.x, p.y, 0), flowers);

            // Casa de 6x5
            for (int x = -10; x < -4; x++)
            {
                buildings.SetTile(new Vector3Int(x, 6, 0), T("house_roof"));
                buildings.SetTile(new Vector3Int(x, 5, 0), T("house_roof"));
                buildings.SetTile(new Vector3Int(x, 4, 0), T("house_roof_edge"));
                buildings.SetTile(new Vector3Int(x, 3, 0), T("house_wall"));
                buildings.SetTile(new Vector3Int(x, 2, 0), T("house_wall"));
            }
            buildings.SetTile(new Vector3Int(-9, 3, 0), T("house_window"));
            buildings.SetTile(new Vector3Int(-6, 3, 0), T("house_window"));
            buildings.SetTile(new Vector3Int(-7, 2, 0), T("house_door"));

            for (int x = 1; x < 6; x++) buildings.SetTile(new Vector3Int(x, -5, 0), T("fence"));
        }

        static Tilemap CreateTilemap(Grid grid, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var map = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;
            return map;
        }

        // ------------------------------------------------------------------ Sprites

        static void PlaceSprites()
        {
            var root = new GameObject("Sprites").transform;

            foreach (var pos in new[] { new Vector2(-12.5f, 3f), new Vector2(13f, 2f), new Vector2(-1.5f, 5f),
                         new Vector2(-3f, -8f) })
                Place(root, "Tree", pos, new[] { $"{Gen}/Props/tree.png" }, 1f);

            Place(root, "Player", new Vector2(0.5f, -2f), Frames("Characters/player_down", 0, 1, 0, 2), 4f);
            Place(root, "Player (derecha)", new Vector2(-2.5f, -2f), Frames("Characters/player_right", 0, 1, 0, 2), 4f);
            Place(root, "Player (arriba)", new Vector2(-4.5f, -2f), Frames("Characters/player_up", 0, 1, 0, 2), 4f);
            Place(root, "Player (izquierda)", new Vector2(-6.5f, -2f), Frames("Characters/player_left", 0, 1, 0, 2), 4f);

            Place(root, "Tostin", new Vector2(1.5f, -2f), Frames("Memos/tostin_world", 0, 1), 3f);
            Place(root, "Tostin brillante", new Vector2(2.5f, -2f), Frames("Memos/tostin_world_brillante", 0, 1), 3f);
            Place(root, "Tostin con collar", new Vector2(3.5f, -2f), Frames("Memos/tostin_world_concollar", 0, 1), 3f);

            Place(root, "Tostin (carrera)", new Vector2(5f, -8.5f), Frames("Memos/tostin_race", 0, 1), 8f);
            Place(root, "Tostin brillante (carrera)", new Vector2(9f, -8.5f), Frames("Memos/tostin_race_brillante", 0, 1), 8f);
            Place(root, "Tostin con collar (carrera)", new Vector2(13f, -8.5f), Frames("Memos/tostin_race_concollar", 0, 1), 8f);
        }

        static string[] Frames(string baseName, params int[] order) =>
            order.Select(i => $"{Gen}/{baseName}_{i}.png").ToArray();

        static void Place(Transform parent, string name, Vector2 pos, string[] spritePaths, float fps)
        {
            var sprites = spritePaths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();
            if (sprites.Any(s => s == null))
            {
                Debug.LogWarning($"[Fase 0] Faltan sprites para '{name}'");
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            // Lo que está más abajo en pantalla se dibuja adelante.
            sr.sortingOrder = 1000 + Mathf.RoundToInt(-pos.y * 10);

            if (sprites.Length > 1)
                go.AddComponent<SpriteFrameAnimator>().SetFrames(sprites, fps);
        }
    }
}
