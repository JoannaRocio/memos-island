using System.Collections.Generic;
using System.Linq;
using MemosIsland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 4: reconstruye todo lo anterior (mundo, datos, carreras) y crea el interior del refugio,
    /// donde viven los Memos (camitas, comedero, la cama del jugador).
    /// </summary>
    public static class Phase4RefugeBuilder
    {
        public const string InteriorScene = "Map_RefugioInterior";
        const string InteriorPath = "Assets/_MemosIsland/Scenes/Maps/" + InteriorScene + ".unity";

        [MenuItem("Memos Island/Fase 4/Construir refugio (y todo)")]
        public static void Build()
        {
            Phase3RaceBuilder.Build();
            BuildInterior();

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != InteriorPath).ToList();
            scenes.Add(new EditorBuildSettingsScene(InteriorPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.OpenScene($"Assets/_MemosIsland/Scenes/Maps/{Phase1WorldBuilder.RefugioScene}.unity");
            Debug.Log("[Fase 4] Refugio listo. Abrí Map_RefugioExterior (o Pueblo Puerto) y dale Play.");
        }

        static Sprite S(string path) => Phase1WorldBuilder.S(path);
        static UnityEngine.Tilemaps.TileBase T(string name) => Phase1WorldBuilder.T(name);

        static void BuildInterior()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new Phase1WorldBuilder.MapBuilder("Casa del refugio", 14, 10);

            // Piso, paredes (arriba con ventanas, costados y abajo) y la salida.
            m.Fill(m.Ground, T("in_floor"), 1, 1, 12, 7);
            m.Fill(m.Buildings, T("in_wall_top"), 0, 9, 13, 9);
            m.Fill(m.Buildings, T("in_wall"), 1, 8, 12, 8);
            m.Set(m.Buildings, T("in_window"), (3, 8), (10, 8));
            for (int y = 0; y <= 8; y++) m.Set(m.Buildings, T("in_wall_top"), (0, y), (13, y));
            for (int x = 1; x <= 12; x++) if (x != 7) m.Set(m.Buildings, T("in_wall_top"), (x, 0));
            m.Set(m.Ground, T("in_doormat"), (7, 0));
            m.Fill(m.Ground, T("in_rug"), 5, 2, 9, 4);

            // Muebles
            m.Prop("Bed", new Vector3(2f, 6f), S("Furniture/bed"), new Vector2(1.9f, 1.9f), new Vector2(0f, 1f));
            m.Prop("Table", new Vector3(11f, 6f), S("Furniture/table"), new Vector2(1.9f, 0.9f), new Vector2(0f, 0.5f));
            m.Prop("Plant", new Vector3(12.5f, 7f), S("Furniture/plant"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));

            var beds = new List<Vector2Int> { new(1, 2), new(3, 1), new(11, 2), new(12, 4), new(8, 6) };
            foreach (var b in beds)
            {
                var cushion = new GameObject("Memo Bed").AddComponent<SpriteRenderer>();
                cushion.transform.SetParent(m.Props, false);
                cushion.transform.position = new Vector3(b.x + 0.5f, b.y, 0f);
                cushion.sprite = S("Furniture/memo_bed");
                cushion.sortingOrder = 5; // en el piso, debajo de los Memos
            }

            var bowlGo = m.Prop("Bowl", new Vector3(6.5f, 7f), S("Furniture/bowl_empty"), new Vector2(0.9f, 0.9f), new Vector2(0f, 0.5f));
            var bowl = bowlGo.AddComponent<Bowl>();
            bowl.Setup(bowlGo.GetComponent<SpriteRenderer>(), S("Furniture/bowl_full"), S("Furniture/bowl_empty"));

            // Luz cálida de la casa (siempre prendida, para que de noche no quede a oscuras).
            var lamp = new GameObject("House Light").AddComponent<Light2D>();
            lamp.transform.position = new Vector3(7f, 5f, 0f);
            lamp.lightType = Light2D.LightType.Point;
            lamp.pointLightOuterRadius = 9f;
            lamp.pointLightInnerRadius = 3f;
            lamp.intensity = 0.55f;
            lamp.color = new Color(1f, 0.85f, 0.65f);

            m.Refuge(true, new RectInt(1, 1, 12, 7), new Vector2Int(7, 1),
                new List<Vector2Int> { new(3, 7), new(12, 1), new(1, 4), new(12, 6) },
                beds, new Vector2Int(3, 6), bowl);

            m.Spawn(7, 1, "entrada", Direction.Up);
            m.Spawn(7, 1, "default", Direction.Up);
            m.Warp(7, 0, Phase1WorldBuilder.RefugioScene, "puerta");

            Phase1WorldBuilder.SaveMap(InteriorScene);
        }
    }
}
