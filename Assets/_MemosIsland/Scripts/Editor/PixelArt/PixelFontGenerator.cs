using System;
using System.Collections.Generic;
using System.IO;
using MemosIsland.UI;
using UnityEditor;
using UnityEngine;

namespace MemosIsland.EditorTools.PixelArt
{
    /// <summary>Convierte Art/FontSource/*.txt en un atlas PNG + un asset PixelFont (en Art/Generated/Fonts).</summary>
    public static class PixelFontGenerator
    {
        public const string FontSourceRoot = PixelArtGenerator.ArtRoot + "/FontSource";
        const string OutFolder = PixelArtGenerator.GeneratedRoot + "/Fonts";
        const int CellW = 8, CellPad = 1, Columns = 16;

        [MenuItem("Memos Island/Arte/Regenerar fuente")]
        public static void GenerateAll()
        {
            foreach (var file in Directory.GetFiles(FontSourceRoot, "*.txt"))
                Generate(file.Replace('\\', '/'));
        }

        public static PixelFont Generate(string sourcePath)
        {
            int height = 11, lineHeight = 14, spacing = 1, space = 3;
            var glyphs = new List<(char c, bool[,] px, int w)>();

            var lines = File.ReadAllLines(sourcePath);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string Err(string m) => $"[PixelFont] {sourcePath}:{i + 1}: {m}";

                switch (p[0])
                {
                    case "height": height = int.Parse(p[1]); break;
                    case "lineheight": lineHeight = int.Parse(p[1]); break;
                    case "spacing": spacing = int.Parse(p[1]); break;
                    case "space": space = int.Parse(p[1]); break;
                    case "glyph":
                    {
                        if (p[1].Length != 1) { Debug.LogError(Err($"carácter inválido '{p[1]}'")); continue; }
                        int start = p.Length >= 4 ? int.Parse(p[2]) : 2;
                        var rows = p[p.Length - 1].Split('/');
                        int w = 0;
                        foreach (var r in rows) w = Mathf.Max(w, r.Length);
                        if (start + rows.Length > height) { Debug.LogError(Err($"'{p[1]}' es más alto que la celda")); continue; }
                        var px = new bool[height, w];
                        for (int r = 0; r < rows.Length; r++)
                        for (int x = 0; x < rows[r].Length; x++)
                            px[start + r, x] = rows[r][x] == '#';
                        glyphs.Add((p[1][0], px, w));
                        break;
                    }
                    default: Debug.LogError(Err($"instrucción desconocida '{p[0]}'")); break;
                }
            }

            int cellH = height + CellPad;
            int rowsCount = Mathf.CeilToInt(glyphs.Count / (float)Columns);
            int texW = Columns * CellW, texH = Mathf.Max(1, rowsCount * cellH);
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            var clear = new Color32[texW * texH];
            tex.SetPixels32(clear);

            var font = LoadOrCreateFont(sourcePath);
            font.glyphs.Clear();
            for (int g = 0; g < glyphs.Count; g++)
            {
                var (c, px, w) = glyphs[g];
                int cx = (g % Columns) * CellW;
                int top = texH - 1 - (g / Columns) * cellH; // fila 0 del glifo
                for (int r = 0; r < height; r++)
                for (int x = 0; x < w; x++)
                    if (px[r, x]) tex.SetPixel(cx + x, top - r, Color.white);
                font.glyphs.Add(new PixelFont.Glyph
                {
                    character = c,
                    rect = new RectInt(cx, top - height + 1, Mathf.Max(1, w), height)
                });
            }
            tex.Apply();

            Directory.CreateDirectory(OutFolder);
            var name = Path.GetFileNameWithoutExtension(sourcePath);
            var pngPath = $"{OutFolder}/{name}.png";
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            PixelArtGenerator.PendingPivots[pngPath] = new Vector2(0.5f, 0.5f);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
            PixelArtGenerator.PendingPivots.Remove(pngPath);

            font.atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            font.glyphHeight = height;
            font.lineHeight = lineHeight;
            font.spacing = spacing;
            font.spaceWidth = space;
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFont] {name}: {glyphs.Count} glifos.");
            return font;
        }

        static PixelFont LoadOrCreateFont(string sourcePath)
        {
            Directory.CreateDirectory(OutFolder);
            var path = $"{OutFolder}/{Path.GetFileNameWithoutExtension(sourcePath)}.asset";
            var font = AssetDatabase.LoadAssetAtPath<PixelFont>(path);
            if (font != null) return font;
            font = ScriptableObject.CreateInstance<PixelFont>();
            AssetDatabase.CreateAsset(font, path);
            return font;
        }
    }
}
