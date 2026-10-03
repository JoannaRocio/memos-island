using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemosIsland.EditorTools.PixelArt;
using MemosIsland.Farm;
using MemosIsland.Memos;
using UnityEditor;
using UnityEngine;

namespace MemosIsland.EditorTools
{
    /// <summary>
    /// Fase 6: objetos de la vida en la isla (semillas, cultivos, comidas, materiales), recetas y mapas
    /// (huerta y máquinas del refugio, puestos del pueblo, la Cueva). Corre todo lo anterior primero.
    /// </summary>
    public static class Phase6IslandBuilder
    {
        const string ItemsFolder = "Assets/_MemosIsland/Data/Items";
        const string RecipesFolder = "Assets/_MemosIsland/Data/Recipes";
        const string Icons = PixelArtGenerator.GeneratedRoot + "/Icons";

        [MenuItem("Memos Island/Fase 6/Construir vida en la isla (y todo)")]
        public static void Build()
        {
            Phase5ProgressBuilder.Build();
            BuildData(false);
            Phase6WorldBuilder.BuildCave();
            Phase6WorldBuilder.AddToBuildSettings();
            Debug.Log("[Fase 6] Vida en la isla lista: granja, mina, fundición, mesa de trabajo, tienda y caja de envíos.");
        }

        record ItemDef(string Id, string Name, string Description, ItemKind Kind, int Price,
            string GrowsInto = null, int GrowDays = 0, int FoodBonus = 0);

        static readonly ItemDef[] Items =
        {
            // Semillas
            new("semilla_nabo", "Sem. nabo", "Crece en 3 días regados.", ItemKind.Seed, 20, "nabo", 3),
            new("semilla_zanahoria", "Sem. zanahoria", "Crece en 4 días regados. A Topín le encantan.", ItemKind.Seed, 30, "zanahoria", 4),
            new("semilla_frutilla", "Sem. frutilla", "Crece en 6 días regados.", ItemKind.Seed, 50, "frutilla", 6),
            new("semilla_zapallo", "Sem. zapallo", "Crece en 8 días regados. ¡Pantuflo lo adora!", ItemKind.Seed, 70, "zapallo", 8),
            new("semilla_bayamemo", "Sem. bayamemo", "Crece en 5 días regados. La favorita de casi todos los Memos.", ItemKind.Seed, 40, "bayamemo", 5),
            // Cultivos y comidas
            new("nabo", "Nabo", "Comida básica. Se vende bien.", ItemKind.Food, 60),
            new("zanahoria", "Zanahoria", "Crocante y dulce.", ItemKind.Food, 90),
            new("frutilla", "Frutilla", "Postre favorito de Brotito y Plumín.", ItemKind.Food, 160),
            new("zapallo", "Zapallo", "Grande y nutritivo.", ItemKind.Food, 240),
            new("bayamemo", "Bayamemo", "La baya preferida de casi todos los Memos. Clave para capturar.", ItemKind.Food, 120),
            new("comida_memo", "Comida para Memos", "Alimento básico. También sirve para llenar el comedero.", ItemKind.Food, 20),
            new("pastel_frutilla", "Pastel de frutilla", "Un mimo especial: +10 de confianza extra.", ItemKind.Food, 300, FoodBonus: 10),
            new("pure_zapallo", "Puré de zapallo", "Calentito y rico: +5 de confianza extra.", ItemKind.Food, 200, FoodBonus: 5),
            new("mermelada", "Mermelada", "Hecha en la procesadora. Se vende muy bien.", ItemKind.Food, 400),
            new("fruto_silvestre", "Fruto silvestre", "Lo encontrás en el pasto. Comida simple.", ItemKind.Food, 30),
            // Materiales
            new("piedra", "Piedra", "Una piedra común.", ItemKind.Material, 4),
            new("mineral_cobre", "Mineral de cobre", "Se funde en lingotes de cobre.", ItemKind.Material, 20),
            new("mineral_hierro", "Mineral de hierro", "Se funde en lingotes de hierro.", ItemKind.Material, 40),
            new("cuarzo", "Cuarzo", "Un cristal transparente.", ItemKind.Material, 60),
            new("amatista", "Amatista", "Gema violeta.", ItemKind.Material, 200),
            new("topacio", "Topacio", "Gema dorada.", ItemKind.Material, 240),
            new("esmeralda", "Esmeralda", "Gema verde. Muy rara.", ItemKind.Material, 300),
            new("lingote_cobre", "Lingote de cobre", "Para equipo y mejoras de herramientas.", ItemKind.Material, 120),
            new("lingote_hierro", "Lingote de hierro", "Para equipo y mejoras de herramientas.", ItemKind.Material, 240),
            new("flor", "Flor", "Aparece cada día en la pradera.", ItemKind.Material, 20),
            new("pluma", "Pluma", "Una pluma suave.", ItemKind.Material, 30),
            new("alga", "Alga", "Se junta en la playa.", ItemKind.Material, 20),
            new("concha", "Concha", "Un recuerdo del mar.", ItemKind.Material, 40),
            new("hierba", "Hierba", "Hierba fresca.", ItemKind.Material, 16),
            new("tela", "Tela", "Para accesorios y equipo.", ItemKind.Material, 60),
            new("cuero", "Cuero", "Para equipo.", ItemKind.Material, 80),
            new("pelota", "Pelota", "Jugar con pelota suma más diversión.", ItemKind.Toy, 100),
        };

        record RecipeDef(string Id, CraftStation Station, (string id, int n)[] Inputs, string Output, int Count = 1, float Minutes = 0);

        static readonly RecipeDef[] Recipes =
        {
            new("fundir_cobre", CraftStation.Smelter, new[] { ("mineral_cobre", 3) }, "lingote_cobre", 1, 20),
            new("fundir_hierro", CraftStation.Smelter, new[] { ("mineral_hierro", 3) }, "lingote_hierro", 1, 40),
            new("comida_memo", CraftStation.Workbench, new[] { ("nabo", 1), ("zanahoria", 1) }, "comida_memo", 3),
            new("comida_memo_hierba", CraftStation.Workbench, new[] { ("hierba", 2), ("nabo", 1) }, "comida_memo", 2),
            new("pastel_frutilla", CraftStation.Workbench, new[] { ("frutilla", 2), ("bayamemo", 1) }, "pastel_frutilla"),
            new("pure_zapallo", CraftStation.Workbench, new[] { ("zapallo", 1) }, "pure_zapallo", 2),
            new("herraduras", CraftStation.Workbench, new[] { ("lingote_hierro", 2) }, "herraduras"),
            new("aletas", CraftStation.Workbench, new[] { ("lingote_cobre", 2), ("alga", 1) }, "aletas"),
            new("botas_clavos", CraftStation.Workbench, new[] { ("lingote_hierro", 2), ("cuero", 1) }, "botas_clavos"),
            new("alas_planeo", CraftStation.Workbench, new[] { ("pluma", 3), ("tela", 1) }, "alas_planeo"),
            new("pesas", CraftStation.Workbench, new[] { ("lingote_hierro", 2), ("piedra", 2) }, "pesas"),
            new("mochila_agua", CraftStation.Workbench, new[] { ("cuero", 1), ("tela", 1) }, "mochila_agua"),
            new("cascabel_calma", CraftStation.Workbench, new[] { ("lingote_cobre", 1), ("cuarzo", 1) }, "cascabel_calma"),
            new("piedra_carga", CraftStation.Workbench, new[] { ("cuarzo", 2), ("topacio", 1) }, "piedra_carga"),
            new("mono_amistad", CraftStation.Workbench, new[] { ("tela", 1), ("flor", 2) }, "mono_amistad"),
            new("gema_rio", CraftStation.Workbench, new[] { ("amatista", 1), ("cuarzo", 1) }, "gema_rio"),
            new("gema_montana", CraftStation.Workbench, new[] { ("topacio", 1), ("cuarzo", 1) }, "gema_montana"),
            new("gema_pradera", CraftStation.Workbench, new[] { ("esmeralda", 1), ("cuarzo", 1) }, "gema_pradera"),
            new("acc_corona_flores", CraftStation.Workbench, new[] { ("flor", 4) }, "acc_corona_flores"),
            new("acc_gorrito_rojo", CraftStation.Workbench, new[] { ("tela", 2), ("flor", 1) }, "acc_gorrito_rojo"),
            new("pelota", CraftStation.Workbench, new[] { ("cuero", 1), ("tela", 1) }, "pelota"),
        };

        public static void BuildData(bool overwrite)
        {
            var db = AssetDatabase.LoadAssetAtPath<MemoDatabase>("Assets/_MemosIsland/Resources/MemoDatabase.asset");
            Directory.CreateDirectory(ItemsFolder);
            Directory.CreateDirectory(RecipesFolder);

            foreach (var d in Items)
            {
                var path = $"{ItemsFolder}/{d.Id}.asset";
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                bool fresh = item == null || overwrite;
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemData>();
                    AssetDatabase.CreateAsset(item, path);
                }
                if (fresh)
                {
                    item.id = d.Id;
                    item.displayName = d.Name;
                    item.description = d.Description;
                    item.kind = d.Kind;
                    item.price = d.Price;
                    item.growsInto = d.GrowsInto;
                    item.growDays = d.GrowDays;
                    item.foodTrustBonus = d.FoodBonus;
                }
                if (!db.items.Contains(item)) db.items.Add(item);
            }

            // Íconos para todos los objetos (los de la Fase 5 todavía no tenían).
            foreach (var item in db.items.Where(i => i != null))
            {
                var icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Icons}/icon_{item.id}.png")
                           ?? (item.id.StartsWith("gema_") ? AssetDatabase.LoadAssetAtPath<Sprite>($"{Icons}/icon_gema.png") : null);
                if (icon != null) item.icon = icon;
                else if (item.accessorySprite != null) item.icon = item.accessorySprite;
                EditorUtility.SetDirty(item);
            }

            var recipes = new List<RecipeData>();
            foreach (var r in Recipes)
            {
                var path = $"{RecipesFolder}/{r.Id}.asset";
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                bool fresh = recipe == null || overwrite;
                if (recipe == null)
                {
                    recipe = ScriptableObject.CreateInstance<RecipeData>();
                    AssetDatabase.CreateAsset(recipe, path);
                }
                if (fresh)
                {
                    recipe.id = r.Id;
                    recipe.station = r.Station;
                    recipe.inputs = r.Inputs.Select(i => new ItemAmount(i.id, i.n)).ToList();
                    recipe.output = new ItemAmount(r.Output, r.Count);
                    recipe.minutes = r.Minutes;
                }
                EditorUtility.SetDirty(recipe);
                recipes.Add(recipe);
            }
            db.recipes = recipes;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }
    }
}
