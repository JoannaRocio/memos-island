using System.IO;
using System.Linq;
using MemosIsland.EditorTools.PixelArt;
using MemosIsland.Race;
using MemosIsland.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 3: reconstruye el mundo (mapas con pasto alto y carteles de desafío) y crea la escena de carrera.
    /// </summary>
    public static class Phase3RaceBuilder
    {
        const string RaceScenePath = "Assets/_MemosIsland/Scenes/" + RaceLauncher.SceneName + ".unity";
        const string Gen = PixelArtGenerator.GeneratedRoot;
        const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [MenuItem("Memos Island/Fase 3/Construir carreras (y mundo)")]
        public static void Build()
        {
            Phase1WorldBuilder.Build(); // mapas + GameRoot (incluye encuentros y desafíos)
            Phase2DataBuilder.BuildMissing();
            BuildRaceScene();

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != RaceScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(RaceScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.OpenScene($"Assets/_MemosIsland/Scenes/Maps/{Phase1WorldBuilder.PuebloScene}.unity");
            Debug.Log("[Fase 3] Carreras listas. En Pueblo Puerto: pasto alto (captura) y dos carteles de desafío.");
        }

        static Sprite S(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Gen}/{path}.png");

        static void BuildRaceScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var font = AssetDatabase.LoadAssetAtPath<PixelFont>($"{Gen}/Fonts/MemosFont.asset");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);

            var root = new GameObject("Race");
            var viewGo = new GameObject("Race View");
            viewGo.transform.SetParent(root.transform, false);
            var view = viewGo.AddComponent<RaceView>();
            var terrains = AssetDatabase.FindAssets("track_ t:Sprite", new[] { $"{Gen}/Track" })
                .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null && s.name != "track_flag" && s.name != "track_finish")
                .ToList();
            view.Setup(font, S("UI/ui_pixel"), S("Track/track_flag"), S("Track/track_finish"), terrains);

            root.AddComponent<RaceController>().Setup(view, font, S("UI/ui_box"), S("UI/ui_pixel"), unlit);

            Directory.CreateDirectory(Path.GetDirectoryName(RaceScenePath));
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), RaceScenePath);
        }
    }
}
