using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MemosIsland.EditorTools.PixelArt
{
    /// <summary>
    /// Toda imagen nueva dentro de Assets/_MemosIsland/Art se importa como pixel art
    /// (Sprite, PPU 16, filtro Point, sin compresión ni mipmaps).
    /// Además, si cambia un archivo fuente de Art/Source, se regeneran sus PNG.
    /// </summary>
    public class PixelArtImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(PixelArtGenerator.ArtRoot + "/")) return;

            var importer = (TextureImporter)assetImporter;
            bool fromGenerator = PixelArtGenerator.PendingPivots.TryGetValue(assetPath, out var pivot);
            // Las imágenes que ya tienen configuración guardada (por ejemplo arte final ajustado a mano) se respetan.
            if (!fromGenerator && !importer.importSettingsMissing) return;
            if (!fromGenerator) pivot = new Vector2(0.5f, 0.5f);

            importer.textureType = TextureImporterType.Sprite;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spritePixelsPerUnit = PixelArtGenerator.PixelsPerUnit;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteBorder = fromGenerator && PixelArtGenerator.PendingBorders.TryGetValue(assetPath, out var border)
                ? border
                : Vector4.zero;
            importer.SetTextureSettings(settings);

            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            // Los peinados del jugador se recolorean en el juego (PlayerLook): necesitan poder leerse.
            importer.isReadable = assetPath.Contains("/Generated/PlayerStyles/");
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            List<string> changed = null, fonts = null;
            foreach (var path in imported)
            {
                if (!path.EndsWith(".txt")) continue;
                if (path.StartsWith(PixelArtGenerator.SourceRoot + "/"))
                    (changed ??= new List<string>()).Add(path);
                else if (path.StartsWith(PixelFontGenerator.FontSourceRoot + "/"))
                    (fonts ??= new List<string>()).Add(path);
            }
            if (changed == null && fonts == null) return;

            // Se difiere para no importar assets en medio de otro import.
            EditorApplication.delayCall += () =>
            {
                if (changed != null) foreach (var path in changed) PixelArtGenerator.GenerateFile(path);
                if (fonts != null) foreach (var path in fonts) PixelFontGenerator.Generate(path);
            };
        }
    }
}
