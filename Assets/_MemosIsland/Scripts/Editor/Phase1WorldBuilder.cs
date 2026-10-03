using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.EditorTools.PixelArt;
using MemosIsland.Race;
using MemosIsland.UI;
using MemosIsland.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 1: arma el prefab GameRoot (jugador, cámara, interfaz, reloj, luz) y los mapas de prueba
    /// Pueblo Puerto y Refugio (exterior). Se puede volver a correr cuando cambie el arte.
    /// </summary>
    public static class Phase1WorldBuilder
    {
        const string MapsFolder = "Assets/_MemosIsland/Scenes/Maps";
        public const string PuebloScene = "Map_PuebloPuerto";
        public const string RefugioScene = "Map_RefugioExterior";
        const string ResourcesFolder = "Assets/_MemosIsland/Resources";
        const string GameRootPath = ResourcesFolder + "/GameRoot.prefab";
        const string Gen = PixelArtGenerator.GeneratedRoot;
        internal const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        const float Ppu = 16f;

        const int OrderActors = 10;
        const int OrderUi = 1000;
        const int OrderFader = 5000;
        const float UiScale = 2f;

        [MenuItem("Memos Island/Fase 1/Construir mundo de prueba")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            PixelArtGenerator.GenerateAll();
            PixelFontGenerator.GenerateAll();
            ConfigureSpriteSorting();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildGameRootPrefab();
            BuildPuebloPuerto();
            BuildRefugio();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{MapsFolder}/{PuebloScene}.unity", true),
                new EditorBuildSettingsScene($"{MapsFolder}/{RefugioScene}.unity", true),
            };
            EditorSceneManager.OpenScene($"{MapsFolder}/{PuebloScene}.unity");
            Debug.Log("[Fase 1] Mundo de prueba listo. Abrí Map_PuebloPuerto y dale Play.");
        }

        // ------------------------------------------------------------------ Proyecto

        /// <summary>Los sprites con el mismo orden se dibujan según su altura (lo de más abajo, adelante).</summary>
        static void ConfigureSpriteSorting()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Object>("Assets/Settings/Renderer2D.asset");
            if (renderer == null) return;
            var so = new SerializedObject(renderer);
            so.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            so.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static Sprite S(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Gen}/{path}.png");
        static Sprite[] Frames(string baseName) => new[] { S($"{baseName}_0"), S($"{baseName}_1"), S($"{baseName}_2") };
        internal static TileBase T(string name) => AssetDatabase.LoadAssetAtPath<TileBase>($"{PixelArtGenerator.TilesRoot}/{name}.asset");
        static Vector3 Px(float x, float y, float z = 0f) => new(x / Ppu, y / Ppu, z);

        static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Child(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        // ------------------------------------------------------------------ GameRoot

        static void BuildGameRootPrefab()
        {
            var font = AssetDatabase.LoadAssetAtPath<PixelFont>($"{Gen}/Fonts/MemosFont.asset");
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            var boxSprite = S("UI/ui_box");

            var root = new GameObject("GameRoot");
            root.AddComponent<GameClock>();
            var maps = root.AddComponent<MapManager>();
            var gameRoot = root.AddComponent<GameRoot>();

            // Luz global que sigue la hora real
            var lightGo = Child(root.transform, "Global Light 2D", Vector3.zero);
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            lightGo.AddComponent<DayNightLighting>();

            // Jugador
            var player = Child(root.transform, "Player", Vector3.zero);
            player.AddComponent<GridMover>();
            var controller = player.AddComponent<PlayerController>();
            var body = Child(player.transform, "Sprite", Vector3.zero);
            var bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = S("Characters/player_down_0");
            bodyRenderer.sortingOrder = OrderActors;
            body.AddComponent<CharacterSpriteAnimator>().Setup(player.GetComponent<GridMover>(),
                Frames("Characters/player_down"), Frames("Characters/player_up"),
                Frames("Characters/player_left"), Frames("Characters/player_right"));

            // Cámara pixel perfect
            var camGo = Child(root.transform, "Main Camera", new Vector3(0, 0, -10));
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1a, 0x1c, 0x2c, 0xff);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 16;
            ppc.refResolutionX = 480;
            ppc.refResolutionY = 270;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.StretchFill; // siempre se ven exactamente 480x270
            var follow = camGo.AddComponent<CameraFollow>();
            follow.SetTarget(player.transform);

            // Interfaz (en el mundo, pegada a la cámara: así también es pixel art).
            // Se dibuja al doble: sus coordenadas son "pixels de interfaz" de una pantalla virtual de 240x135,
            // la misma escala que una GBA, para que la fuente se lea bien.
            var ui = Child(camGo.transform, "UI", new Vector3(0, 0, 10));
            ui.transform.localScale = new Vector3(UiScale, UiScale, 1f);

            SpriteRenderer Box(Transform parent, string name, Vector3 pos, Vector2 sizePx)
            {
                var go = Child(parent, name, pos);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = boxSprite;
                r.drawMode = SpriteDrawMode.Sliced;
                r.size = sizePx / Ppu;
                r.sharedMaterial = unlit;
                r.sortingOrder = OrderUi;
                return r;
            }

            PixelText Text(Transform parent, string name, Vector3 pos, int order, Color color, bool shadow = true)
            {
                var t = Child(parent, name, pos).AddComponent<PixelText>();
                t.Setup(font, unlit, order, color, shadow);
                return t;
            }

            var dark = new Color32(0x33, 0x3c, 0x57, 0xff);

            // Caja de diálogo: 232x44 abajo (pantalla de interfaz: x ±120, y ±67.5)
            var dialogueGo = Child(ui.transform, "Dialogue", Vector3.zero);
            var dialogue = dialogueGo.AddComponent<DialogueBox>();
            var visuals = Child(dialogueGo.transform, "Visuals", Vector3.zero);
            Box(visuals.transform, "Box", Px(0, -43.5f), new Vector2(232, 44));
            var bodyText = Text(visuals.transform, "Text", Px(-108, -26.5f), OrderUi + 10, dark);
            var arrow = Text(visuals.transform, "Arrow", Px(103, -54.5f), OrderUi + 12, new Color32(0xb1, 0x3e, 0x53, 0xff), false);
            arrow.SetText("▼");
            dialogue.Setup(visuals, bodyText, arrow);

            // Cartel con el nombre del mapa: arriba a la izquierda
            var bannerGo = Child(ui.transform, "MapBanner", Vector3.zero);
            var banner = bannerGo.AddComponent<MapNameBanner>();
            var content = Child(bannerGo.transform, "Content", Px(-118, 65.5f));
            var bannerBox = Box(content.transform, "Box", Vector3.zero, new Vector2(64, 18));
            var bannerText = Text(content.transform, "Label", Px(7, -4), OrderUi + 10, dark);
            banner.Setup(content.transform, bannerBox, bannerText);

            // Reloj: arriba a la derecha
            var clockGo = Child(ui.transform, "Clock", Px(118, 65.5f));
            var clockHud = clockGo.AddComponent<ClockHud>();
            var clockBox = Box(clockGo.transform, "Box", Vector3.zero, new Vector2(48, 16));
            var clockText = Text(clockGo.transform, "Label", Vector3.zero, OrderUi + 10, dark);
            clockHud.Setup(clockBox, clockText);

            // Fundido a negro
            var faderGo = Child(ui.transform, "Fader", Vector3.zero);
            var faderRenderer = faderGo.AddComponent<SpriteRenderer>();
            faderRenderer.sprite = S("UI/ui_pixel");
            faderRenderer.sharedMaterial = unlit;
            faderRenderer.sortingOrder = OrderFader;
            faderGo.transform.localScale = new Vector3(300f, 170f, 1f);
            var fader = faderGo.AddComponent<ScreenFader>();

            // MemoBox (Fase 2) y Mis Memos (Fase 4): se abren desde el menú de pausa (Esc/Tab)
            var memoBox = Child(ui.transform, "MemoBox", Vector3.zero).AddComponent<MemoBoxScreen>();
            memoBox.Setup(font, boxSprite, S("UI/ui_pixel"), unlit);
            var myMemos = Child(ui.transform, "My Memos", Vector3.zero).AddComponent<MyMemosScreen>();
            myMemos.Setup(font, boxSprite, S("UI/ui_pixel"), unlit);
            Child(ui.transform, "Pause Menu", Vector3.zero).AddComponent<PauseMenu>().Setup(font, boxSprite, unlit);
            var evolution = Child(ui.transform, "Evolution", Vector3.zero).AddComponent<EvolutionScreen>();
            evolution.Setup(S("UI/ui_pixel"), unlit);

            // Compañero que te sigue (Fase 4)
            var companionGo = Child(root.transform, "Companion", Vector3.zero);
            var (companionMover, companionView, companionBubble, companionCollider) = BuildMemoActor(companionGo, font, unlit, false);
            companionGo.AddComponent<CompanionFollower>().Setup(companionMover, companionView, companionBubble, companionCollider);

            SetField(gameRoot, "companion", companionGo.GetComponent<CompanionFollower>());
            SetField(gameRoot, "myMemos", myMemos);
            SetField(gameRoot, "memoBox", memoBox);
            SetField(gameRoot, "evolution", evolution);
            SetField(gameRoot, "player", controller);
            SetField(gameRoot, "cameraFollow", follow);
            SetField(gameRoot, "dialogue", dialogue);
            SetField(gameRoot, "fader", fader);
            SetField(gameRoot, "banner", banner);
            SetField(gameRoot, "maps", maps);

            Directory.CreateDirectory(ResourcesFolder);
            PrefabUtility.SaveAsPrefabAsset(root, GameRootPath);
            Object.DestroyImmediate(root);

            // Prefab de los Memos del refugio (MemoLife se agrega al crearlos).
            var actor = new GameObject("MemoActor");
            BuildMemoActor(actor, font, unlit, true);
            PrefabUtility.SaveAsPrefabAsset(actor, $"{ResourcesFolder}/MemoActor.prefab");
            Object.DestroyImmediate(actor);
        }

        /// <summary>Un Memo en el mundo: GridMover + sprite (MemoSpriteView) + burbuja de emoción + collider para interactuar.</summary>
        static (GridMover, MemoSpriteView, EmoteBubble, Collider2D) BuildMemoActor(GameObject go, PixelFont font, Material unlit, bool occupiesCell)
        {
            var mover = go.AddComponent<GridMover>();
            var so = new SerializedObject(mover);
            so.FindProperty("occupiesCell").boolValue = occupiesCell;
            so.FindProperty("walkSpeed").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);
            col.offset = new Vector2(0f, 0.5f);

            var viewGo = Child(go.transform, "View", Vector3.zero);
            var renderer = viewGo.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = OrderActors;
            var view = viewGo.AddComponent<MemoSpriteView>();
            var accessory = Child(viewGo.transform, "Accessory", Vector3.zero).AddComponent<SpriteRenderer>();
            accessory.sortingOrder = OrderActors + 1;
            view.SetAccessoryRenderer(accessory);

            var bubbleGo = Child(go.transform, "Bubble", new Vector3(0f, 2.15f, 0f));
            var bubbleRenderer = bubbleGo.AddComponent<SpriteRenderer>();
            bubbleRenderer.sprite = S("UI/ui_bubble");
            bubbleRenderer.sharedMaterial = unlit;
            bubbleRenderer.sortingOrder = OrderActors + 50;
            var glyph = Child(bubbleGo.transform, "Glyph", Vector3.zero).AddComponent<PixelText>();
            glyph.Setup(font, unlit, OrderActors + 51, Color.black, false);
            var bubble = bubbleGo.AddComponent<EmoteBubble>();
            bubble.Setup(bubbleRenderer, glyph);
            return (mover, view, bubble, col);
        }

        // ------------------------------------------------------------------ Mapas

        internal class MapBuilder
        {
            public readonly Tilemap Ground, Buildings;
            public readonly Transform Props;
            readonly int _solid = LayerMask.NameToLayer("Solid");

            public MapBuilder(string displayName, int width, int height)
            {
                var map = new GameObject("Map").AddComponent<MapInfo>();
                map.Setup(displayName, new RectInt(0, 0, width, height));

                var grid = new GameObject("Grid").AddComponent<Grid>();
                Ground = Layer(grid, "Ground", 0);
                Buildings = Layer(grid, "Buildings", 1);
                Props = new GameObject("Props").transform;
            }

            Tilemap Layer(Grid grid, string name, int order)
            {
                var go = Child(grid.transform, name, Vector3.zero);
                go.layer = _solid; // solo las tiles marcadas "solid" generan colisión
                var tm = go.AddComponent<Tilemap>();
                go.AddComponent<TilemapRenderer>().sortingOrder = order;
                go.AddComponent<TilemapCollider2D>();
                return tm;
            }

            public void Fill(Tilemap tm, TileBase tile, int x0, int y0, int x1, int y1)
            {
                for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    tm.SetTile(new Vector3Int(x, y, 0), tile);
            }

            public void Set(Tilemap tm, TileBase tile, params (int x, int y)[] cells)
            {
                foreach (var (x, y) in cells) tm.SetTile(new Vector3Int(x, y, 0), tile);
            }

            public GameObject Prop(string name, Vector3 pos, Sprite sprite, Vector2 colliderSize, Vector2 colliderOffset)
            {
                var go = Child(Props, name, pos);
                go.layer = _solid;
                if (sprite != null)
                {
                    var r = go.AddComponent<SpriteRenderer>();
                    r.sprite = sprite;
                    r.sortingOrder = OrderActors;
                }
                var col = go.AddComponent<BoxCollider2D>();
                col.size = colliderSize;
                col.offset = colliderOffset;
                return go;
            }

            /// <summary>Árbol de 2x2 casillas; bloquea las dos casillas de abajo.</summary>
            public void Tree(int x, int y) =>
                Prop("Tree", new Vector3(x + 1, y), S("Props/tree"), new Vector2(1.9f, 0.9f), new Vector2(0, 0.5f));

            public void Sign(int x, int y, params string[] pages) =>
                Prop("Sign", new Vector3(x + 0.5f, y), S("Props/sign"), new Vector2(0.9f, 0.9f), new Vector2(0, 0.5f))
                    .AddComponent<Sign>().SetPages(pages);

            /// <summary>Puerta cerrada: bloquea el paso y muestra un texto.</summary>
            public void Door(int x, int y, params string[] pages) =>
                Prop("Door", new Vector3(x + 0.5f, y), null, new Vector2(0.9f, 0.9f), new Vector2(0, 0.5f))
                    .AddComponent<Sign>().SetPages(pages);

            public void Lamp(int x, int y)
            {
                var lamp = Prop("Lamp", new Vector3(x + 0.5f, y), S("Props/lamp"), new Vector2(0.9f, 0.9f), new Vector2(0, 0.5f));
                NightLightAt(lamp.transform, new Vector3(0, 1.6f, 0), 3.5f, new Color(1f, 0.68f, 0.38f), 1.1f);
            }

            public void NightLightAt(Transform parent, Vector3 localPos, float radius, Color color, float intensity)
            {
                var go = Child(parent, "Night Light", localPos);
                var l = go.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.pointLightOuterRadius = radius;
                l.pointLightInnerRadius = radius * 0.15f;
                l.color = color;
                l.falloffIntensity = 0.6f;
                var night = go.AddComponent<NightLight>();
                var so = new SerializedObject(night);
                so.FindProperty("maxIntensity").floatValue = intensity;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            /// <summary>Casa: 2 filas de pared (puerta abajo, ventanas arriba), alero y 2 filas de techo.</summary>
            public void House(int x, int y, int width, int doorX, int[] windowXs, params string[] doorText)
            {
                for (int i = x; i < x + width; i++)
                {
                    Set(Buildings, T("house_wall"), (i, y), (i, y + 1));
                    Set(Buildings, T("house_roof_edge"), (i, y + 2));
                    Set(Buildings, T("house_roof"), (i, y + 3), (i, y + 4));
                }
                Set(Buildings, T("house_door"), (doorX, y));
                if (doorText.Length > 0) Door(doorX, y, doorText); // sin texto = puerta abierta (con Warp)
                foreach (var wx in windowXs)
                {
                    Set(Buildings, T("house_window"), (wx, y + 1));
                    NightLightAt(Props, new Vector3(wx + 0.5f, y + 1.5f), 2.2f, new Color(1f, 0.8f, 0.45f), 0.9f);
                }
            }

            /// <summary>Encuentros de Memos salvajes en el pasto alto de este mapa (Fase 3).</summary>
            public void Encounters(string terrainId, params (string species, int min, int max, int weight, bool night, bool collared)[] table)
            {
                var go = new GameObject("Wild Encounters");
                go.AddComponent<WildEncounters>().Setup(Ground, T("tall_grass"), terrainId, table.Select(e => new WildEncounters.Entry
                {
                    speciesId = e.species, minLevel = e.min, maxLevel = e.max, weight = e.weight,
                    nightOnly = e.night, collared = e.collared,
                }).ToList());
            }

            /// <summary>Cartel de desafío de carrera (Fase 3, para probar mientras no hay vecinos).</summary>
            public void Challenge(int x, int y, RaceFormat format, string title, string question, RaceSegmentDef[] track,
                string win, string lose, params (string name, (string species, int level, bool collared)[] team)[] rivals)
            {
                var go = Prop("Challenge", new Vector3(x + 0.5f, y), S("Props/sign"), new Vector2(0.9f, 0.9f), new Vector2(0, 0.5f));
                go.AddComponent<RaceChallenge>().Setup(format, title, question, track.ToList(),
                    rivals.Select(r => new RaceChallenge.Rival
                    {
                        name = r.name,
                        team = r.team.Select(m => new RaceChallenge.Member { speciesId = m.species, level = m.level, collared = m.collared }).ToList(),
                    }).ToList(), win, lose);
            }

            /// <summary>Refugio: dónde viven los Memos de este lado de la puerta (Fase 4).</summary>
            public RefugeManager Refuge(bool interior, RectInt area, Vector2Int door, List<Vector2Int> hides,
                List<Vector2Int> beds, Vector2Int? soulmateSpot, Bowl bowl)
            {
                var r = new GameObject("Refuge").AddComponent<RefugeManager>();
                r.Setup(interior, area, door, hides, beds, soulmateSpot, bowl);
                return r;
            }

            public void Warp(int x, int y, string scene, string spawn) =>
                Child(Props, $"Warp → {scene}", new Vector3(x + 0.5f, y)).AddComponent<Warp>().Setup(scene, spawn);

            public void Spawn(int x, int y, string id, Direction facing) =>
                Child(Props, $"Spawn {id}", new Vector3(x + 0.5f, y)).AddComponent<SpawnPoint>().Setup(id, facing);
        }

        internal static void SaveMap(string sceneName)
        {
            Directory.CreateDirectory(MapsFolder);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"{MapsFolder}/{sceneName}.unity");
        }

        static void BuildPuebloPuerto()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new MapBuilder("Pueblo Puerto", 32, 22);

            m.Fill(m.Ground, T("grass"), 0, 0, 31, 21);
            m.Fill(m.Ground, T("water"), 0, 0, 31, 6);
            m.Fill(m.Ground, T("sand"), 0, 7, 31, 8);
            m.Fill(m.Ground, T("dock"), 15, 2, 16, 6);
            m.Fill(m.Ground, T("path"), 2, 12, 31, 12);
            m.Fill(m.Ground, T("path"), 15, 9, 16, 11);
            m.Set(m.Ground, T("path"), (6, 13), (23, 13));
            m.Fill(m.Ground, T("tall_grass"), 3, 9, 8, 10);
            m.Set(m.Ground, T("flowers"), (11, 15), (12, 17), (18, 16), (19, 18), (27, 15), (3, 15), (10, 10), (20, 9));

            // Bosque alrededor (arriba y a los costados; a la derecha queda la salida del camino)
            for (int x = 0; x < 32; x += 2)
            {
                m.Tree(x, 21);
                m.Tree(x, 20);
            }
            for (int y = 9; y <= 19; y++)
            {
                m.Tree(0, y);
                if (y < 11 || y > 13) m.Tree(30, y);
            }
            m.Tree(13, 17);
            m.Tree(27, 16);

            m.House(4, 14, 6, 6, new[] { 5, 8 },
                "Almacén de Deny: CERRADO.",
                "\"Vuelvo en 5 minutos.\" El cartel parece tener varios días…");
            m.House(21, 14, 6, 23, new[] { 22, 25 },
                "La puerta está cerrada. Adentro se escucha a alguien roncando.");

            m.Lamp(11, 13);
            m.Lamp(20, 13);
            m.Sign(17, 13,
                "PUEBLO PUERTO",
                "¡Bienvenidos a la Isla de los Memos! Mañana hay feria de pescado en el muelle.");
            m.Sign(14, 8,
                "Muelle del puerto.",
                "El barco rompehielos está en reparación. ¡Vuelve pronto!");

            m.Encounters("pradera",
                ("plumin", 3, 5, 5, false, false), ("zumbi", 3, 5, 4, false, false),
                ("bostezo", 4, 6, 3, true, false), ("zumbi", 4, 5, 1, false, true));
            m.Challenge(9, 11, RaceFormat.Trainer, "Carrera contra Lalo",
                "DESAFÍO DE PRUEBA: carrera de 3 tramos contra Lalo. ¿Corrés?",
                new[] { new RaceSegmentDef("pradera", 70), new RaceSegmentDef("rio", 60), new RaceSegmentDef("arena", 70) },
                "Lalo: \"¡Uh! Sos más rápido de lo que pensaba.\"", "Lalo: \"¡Je! Te gané. ¿Revancha?\"",
                ("Lalo", new[] { ("plumin", 5, false), ("chispin", 5, false), ("topin", 4, false) }));
            m.Challenge(22, 11, RaceFormat.Cup, "Copa de la Isla (prueba)",
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

            m.Spawn(16, 11, "default", Direction.Down);
            m.Spawn(30, 12, "este", Direction.Left);
            m.Warp(31, 12, RefugioScene, "oeste");

            SaveMap(PuebloScene);
        }

        static void BuildRefugio()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var m = new MapBuilder("Refugio del abuelo", 32, 22);

            m.Fill(m.Ground, T("grass"), 0, 0, 31, 21);
            m.Fill(m.Ground, T("path"), 0, 10, 13, 10);
            m.Set(m.Ground, T("path"), (13, 11));
            m.Fill(m.Ground, T("water"), 20, 4, 24, 7);
            m.Fill(m.Ground, T("tall_grass"), 21, 13, 26, 16);
            m.Set(m.Ground, T("flowers"), (19, 8), (25, 5), (18, 5), (8, 13), (4, 16), (27, 9), (16, 9));

            for (int x = 0; x < 32; x += 2)
            {
                m.Tree(x, 21);
                m.Tree(x, 20);
                m.Tree(x, 1);
                m.Tree(x, 0);
            }
            for (int y = 2; y <= 19; y++)
            {
                if (y < 9 || y > 11) m.Tree(0, y);
                m.Tree(30, y);
            }
            m.Tree(5, 15);
            m.Tree(26, 10);

            for (int x = 4; x <= 10; x++) m.Set(m.Buildings, T("fence"), (x, 3), (x, 8));

            m.House(10, 12, 8, 13, new[] { 11, 15, 16 }); // puerta abierta: lleva al interior (Fase 4)
            m.Warp(13, 12, Phase4RefugeBuilder.InteriorScene, "entrada");
            m.Spawn(13, 11, "puerta", Direction.Down);
            m.Refuge(false, new RectInt(2, 2, 28, 18), new Vector2Int(13, 11),
                new List<Vector2Int> { new(4, 16), new(28, 11), new(3, 4), new(27, 17), new(19, 3) },
                new List<Vector2Int>(), null, null);

            m.Sign(11, 11,
                "REFUGIO DEL ABUELO",
                "\"Un hogar para cada Memo que lo necesite.\"");
            m.Lamp(15, 11);
            m.Lamp(6, 11);

            m.Encounters("pradera",
                ("plumin", 3, 5, 4, false, false), ("chispin", 4, 6, 2, false, false),
                ("topin", 3, 5, 3, false, false), ("bostezo", 4, 6, 3, true, false));

            m.Spawn(13, 11, "default", Direction.Down);
            m.Spawn(1, 10, "oeste", Direction.Right);
            m.Warp(0, 10, PuebloScene, "este");

            SaveMap(RefugioScene);
        }
    }
}
