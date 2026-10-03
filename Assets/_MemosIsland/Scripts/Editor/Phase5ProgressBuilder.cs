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
    /// Fase 5: reconstruye todo y crea los objetos (equipo, amuletos, gemas de terreno y accesorios estéticos)
    /// y los puntos de cabeza/cuello de cada especie para dibujar los accesorios.
    /// Igual que en la Fase 2, "solo faltantes" respeta lo editado a mano.
    /// </summary>
    public static class Phase5ProgressBuilder
    {
        const string ItemsFolder = "Assets/_MemosIsland/Data/Items";
        const string AccessorySprites = PixelArtGenerator.GeneratedRoot + "/Accessories";

        [MenuItem("Memos Island/Fase 5/Construir progreso (y todo)")]
        public static void Build()
        {
            Phase4RefugeBuilder.Build();
            BuildItems(false);
            Debug.Log("[Fase 5] Progreso listo: objetos, equipamiento y accesorios.");
        }

        [MenuItem("Memos Island/Fase 5/Restablecer objetos desde el GDD (sobrescribe)")]
        public static void ResetItems() => BuildItems(true);

        // Puntos de cabeza y cuello en el sprite de mundo (32x32, mirando a la derecha, y medida desde arriba).
        static readonly Dictionary<string, (Vector2Int head, Vector2Int neck)> Anchors = new()
        {
            ["tostin"] = (new(22, 7), new(18, 18)), ["brason"] = (new(23, 6), new(19, 17)),
            ["brotito"] = (new(21, 9), new(17, 21)), ["ramazon"] = (new(23, 7), new(19, 18)),
            ["charquito"] = (new(22, 9), new(18, 20)), ["chapuzon"] = (new(24, 8), new(20, 18)),
            ["karman"] = (new(23, 9), new(20, 16)), ["draken"] = (new(24, 9), new(20, 16)),
            ["randy"] = (new(25, 17), new(22, 23)), ["plumin"] = (new(16, 15), new(18, 24)),
            ["topin"] = (new(17, 18), new(21, 24)), ["zumbi"] = (new(20, 15), new(17, 22)),
            ["chispin"] = (new(21, 11), new(18, 21)), ["copito"] = (new(15, 16), new(18, 25)),
            ["pantuflo"] = (new(23, 19), new(21, 24)), ["bostezo"] = (new(20, 13), new(17, 22)),
            ["farolito"] = (new(19, 13), new(16, 21)), ["tuerquita"] = (new(21, 12), new(17, 22)),
            ["ferrolobo"] = (new(23, 9), new(20, 16)), ["imanta"] = (new(23, 8), new(19, 18)),
        };

        record EquipDef(string Id, string Name, string Description, int Price, TerrainRule Rule = TerrainRule.None,
            string[] Terrains = null, int Speed = 0, int Stamina = 0);

        static readonly EquipDef[] Equipment =
        {
            new("herraduras", "Herraduras", "El barro y la montaña le resultan más fáciles.", 350,
                TerrainRule.UpgradeOneStep, new[] { "barro", "montana" }),
            new("aletas", "Aletas", "En el río corre como si fuera de tipo Agua.", 400, TerrainRule.ForceStrong, new[] { "rio" }),
            new("botas_clavos", "Botas de clavos", "El hielo y la nieve ya no lo frenan.", 400,
                TerrainRule.RemoveWeakness, new[] { "hielo", "nieve" }),
            new("alas_planeo", "Alas de planeo", "Planea sobre las corrientes de aire.", 500, TerrainRule.ForceStrong, new[] { "aire" }),
            new("pesas", "Pesas livianas", "+1 de velocidad, −1 de resistencia.", 300, Speed: 1, Stamina: -1),
            new("mochila_agua", "Mochila de agua", "+2 de resistencia: aguanta más.", 300, Stamina: 2),
        };

        static readonly (string id, string name, string description, AmuletEffect effect)[] Amulets =
        {
            ("amuleto_relevo", "Amuleto de relevo", "Entra a toda velocidad cuando cambiás a este Memo.", AmuletEffect.Relay),
            ("piedra_carga", "Piedra de carga", "Empieza la carrera con la habilidad lista.", AmuletEffect.ChargeStart),
            ("cascabel_calma", "Cascabel de calma", "Lo protege de una habilidad molesta por carrera.", AmuletEffect.CalmBell),
            ("mono_amistad", "Moño de amistad", "Gana 50% más de confianza en las carreras.", AmuletEffect.FriendshipBow),
        };

        static readonly (string id, string name, AccessorySlot slot)[] Accessories =
        {
            ("acc_gorrito_rojo", "Gorrito rojo", AccessorySlot.Head), ("acc_gorrito_azul", "Gorrito azul", AccessorySlot.Head),
            ("acc_mono_rosa", "Moño rosa", AccessorySlot.Head), ("acc_corona_flores", "Corona de flores", AccessorySlot.Head),
            ("acc_sombrero_paja", "Sombrero de paja", AccessorySlot.Head), ("acc_estrellita", "Estrellita", AccessorySlot.Head),
            ("acc_bufanda_roja", "Bufanda roja", AccessorySlot.Neck), ("acc_bufanda_verde", "Bufanda verde", AccessorySlot.Neck),
            ("acc_panuelo_azul", "Pañuelo azul", AccessorySlot.Neck), ("acc_cascabel", "Cascabel", AccessorySlot.Neck),
        };

        public static void BuildItems(bool overwrite)
        {
            var db = MemoDatabase.Instance ?? AssetDatabase.LoadAssetAtPath<MemoDatabase>("Assets/_MemosIsland/Resources/MemoDatabase.asset");
            if (db == null)
            {
                Debug.LogError("[Fase 5] Falta la base de datos de Memos (correr la Fase 2).");
                return;
            }
            Directory.CreateDirectory(ItemsFolder);
            var items = new List<ItemData>();

            foreach (var e in Equipment)
                items.Add(Item(e.Id, overwrite, it =>
                {
                    Basics(it, e.Id, e.Name, e.Description, ItemKind.Equipment, e.Price);
                    it.terrainRule = e.Rule;
                    it.terrains = (e.Terrains ?? new string[0]).Select(db.GetTerrain).Where(t => t != null).ToList();
                    it.speedBonus = e.Speed;
                    it.staminaBonus = e.Stamina;
                }));

            foreach (var (id, name, description, effect) in Amulets)
                items.Add(Item(id, overwrite, it =>
                {
                    Basics(it, id, name, description, ItemKind.Amulet, 600);
                    it.amuletEffect = effect;
                }));

            // Gemas de terreno: una por terreno (GDD §14).
            foreach (var terrain in db.terrains)
            {
                var id = $"gema_{terrain.id}";
                items.Add(Item(id, overwrite, it =>
                {
                    Basics(it, id, $"Gema de {terrain.displayName.ToLowerInvariant()}",
                        $"En {terrain.displayName.ToLowerInvariant()} siempre corre como si fuera muy eficaz.", ItemKind.Amulet, 450);
                    it.amuletEffect = AmuletEffect.TerrainGem;
                    it.gemTerrain = terrain;
                }));
            }

            foreach (var (id, name, slot) in Accessories)
            {
                var item = Item(id, overwrite, it =>
                {
                    Basics(it, id, name, "Un adorno para lucir. Si le gusta, se pone contento.", ItemKind.Accessory, 150);
                    it.accessorySlot = slot;
                });
                // El sprite se actualiza siempre (el arte se regenera seguido).
                item.accessorySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AccessorySprites}/{id}.png");
                item.icon = item.accessorySprite;
                EditorUtility.SetDirty(item);
                items.Add(item);
            }

            db.items.RemoveAll(i => i == null);
            foreach (var it in items)
                if (!db.items.Contains(it)) db.items.Add(it);
            EditorUtility.SetDirty(db);

            foreach (var s in db.species)
            {
                if (s == null || !Anchors.TryGetValue(s.id, out var a)) continue;
                s.headAnchor = a.head;
                s.neckAnchor = a.neck;
                EditorUtility.SetDirty(s);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fase 5] {items.Count} objetos listos.");
        }

        static void Basics(ItemData it, string id, string name, string description, ItemKind kind, int price)
        {
            it.id = id;
            it.displayName = name;
            it.description = description;
            it.kind = kind;
            it.price = price;
        }

        static ItemData Item(string id, bool overwrite, System.Action<ItemData> fill)
        {
            var path = $"{ItemsFolder}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            bool fresh = item == null || overwrite;
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }
            if (fresh) fill(item);
            EditorUtility.SetDirty(item);
            return item;
        }
    }
}
