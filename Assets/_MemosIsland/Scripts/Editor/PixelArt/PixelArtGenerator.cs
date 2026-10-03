using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MemosIsland.EditorTools.PixelArt
{
    /// <summary>
    /// Convierte sprites definidos como grillas de texto (Art/Source/*.txt) en PNG (Art/Generated/&lt;archivo&gt;/).
    ///
    /// Formato:
    ///   # comentario
    ///   sprite nombre            empieza un sprite
    ///   size 16 16               ancho y alto en pixels
    ///   pivot 0.5 0              pivote normalizado (opcional, por defecto 0.5 0.5)
    ///   outline 0                contorno exterior automático con ese color (opcional)
    ///   border 8 8 8 8           bordes de 9 cortes en pixels: izquierda abajo derecha arriba (opcional)
    ///   frame                    las siguientes "alto" líneas son la grilla (un carácter = un color de PixelPalette)
    ///   flip 0                   nuevo cuadro = cuadro 0 espejado horizontalmente
    ///   shift 0 0 1              nuevo cuadro = cuadro 0 desplazado (dx, dy; dy positivo = hacia abajo)
    ///   copyfrom otro            copia los cuadros de un sprite anterior del mismo archivo
    ///   mirror                   espeja horizontalmente todos los cuadros actuales
    ///   variant brillante 3>9 2>8   exporta una copia con colores cambiados (sufijo _brillante)
    ///   tile [fps] [solid]       además crea un Tile (o AnimatedTile si hay varios cuadros) en Art/Tiles/;
    ///                            "solid" le da colisión de celda completa
    ///   end                      termina el sprite
    /// </summary>
    public static class PixelArtGenerator
    {
        public const string ArtRoot = "Assets/_MemosIsland/Art";
        public const string SourceRoot = ArtRoot + "/Source";
        public const string GeneratedRoot = ArtRoot + "/Generated";
        public const string TilesRoot = ArtRoot + "/Tiles";
        public const int PixelsPerUnit = 16;

        /// <summary>Pivotes a aplicar en el próximo import (los lee PixelArtImportSettings).</summary>
        internal static readonly Dictionary<string, Vector2> PendingPivots = new();

        /// <summary>Bordes de 9 cortes a aplicar en el próximo import (izquierda, abajo, derecha, arriba).</summary>
        internal static readonly Dictionary<string, Vector4> PendingBorders = new();

        class SpriteDef
        {
            public string Name;
            public int Width, Height;
            public Vector2 Pivot = new(0.5f, 0.5f);
            public char? Outline;
            public readonly List<char[,]> Frames = new();
            public readonly List<(string suffix, Dictionary<char, char> map)> Variants = new();
            public bool IsTile;
            public float TileFps = 4f;
            public bool TileSolid;
            public Vector4 Border;
        }

        [MenuItem("Memos Island/Arte/Regenerar todo el pixel art")]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder(SourceRoot))
            {
                Debug.LogWarning($"[PixelArt] No existe {SourceRoot}");
                return;
            }
            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { SourceRoot });
            int total = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    total += GenerateFile(path);
            }
            Debug.Log($"[PixelArt] Listo: {total} imágenes generadas desde {guids.Length} archivos.");
        }

        /// <summary>Genera todos los sprites de un archivo fuente. Devuelve la cantidad de PNG escritos.</summary>
        public static int GenerateFile(string sourcePath)
        {
            List<SpriteDef> defs;
            try
            {
                defs = Parse(File.ReadAllLines(sourcePath), sourcePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PixelArt] {e.Message}");
                return 0;
            }

            var group = Path.GetFileNameWithoutExtension(sourcePath);
            var outFolder = $"{GeneratedRoot}/{group}";
            Directory.CreateDirectory(outFolder);

            var written = new List<string>();
            var tileJobs = new List<(SpriteDef def, List<string> paths)>();

            foreach (var def in defs)
            {
                var finalFrames = new List<char[,]>();
                foreach (var f in def.Frames)
                    finalFrames.Add(def.Outline.HasValue ? AddOutline(f, def.Outline.Value) : f);

                var basePaths = WriteFrames(outFolder, def.Name, def, finalFrames, null);
                written.AddRange(basePaths);
                if (def.IsTile) tileJobs.Add((def, basePaths));

                foreach (var (suffix, map) in def.Variants)
                    written.AddRange(WriteFrames(outFolder, $"{def.Name}_{suffix}", def, finalFrames, map));
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var p in written)
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                foreach (var p in written)
                {
                    PendingPivots.Remove(p);
                    PendingBorders.Remove(p);
                }
            }

            foreach (var (def, paths) in tileJobs)
                CreateTile(def, paths);

            AssetDatabase.SaveAssets();
            return written.Count;
        }

        static List<string> WriteFrames(string folder, string name, SpriteDef def, List<char[,]> frames,
            Dictionary<char, char> map)
        {
            var paths = new List<string>();
            for (int i = 0; i < frames.Count; i++)
            {
                var path = frames.Count == 1 ? $"{folder}/{name}.png" : $"{folder}/{name}_{i}.png";
                var tex = ToTexture(frames[i], map);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                PendingPivots[path] = def.Pivot;
                PendingBorders[path] = def.Border;
                paths.Add(path);
            }
            return paths;
        }

        static Texture2D ToTexture(char[,] grid, Dictionary<char, char> map)
        {
            int h = grid.GetLength(0), w = grid.GetLength(1);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = grid[y, x];
                if (map != null && map.TryGetValue(c, out var mapped)) c = mapped;
                var col = new Color32(0, 0, 0, 0);
                if (!PixelPalette.IsTransparent(c)) PixelPalette.TryGet(c, out col);
                // La fila 0 del texto es la de arriba; en la textura y=0 es abajo.
                pixels[(h - 1 - y) * w + x] = col;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static char[,] AddOutline(char[,] src, char outline)
        {
            int h = src.GetLength(0), w = src.GetLength(1);
            var dst = (char[,])src.Clone();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!PixelPalette.IsTransparent(src[y, x])) continue;
                bool touches =
                    (x > 0 && !PixelPalette.IsTransparent(src[y, x - 1])) ||
                    (x < w - 1 && !PixelPalette.IsTransparent(src[y, x + 1])) ||
                    (y > 0 && !PixelPalette.IsTransparent(src[y - 1, x])) ||
                    (y < h - 1 && !PixelPalette.IsTransparent(src[y + 1, x]));
                if (touches) dst[y, x] = outline;
            }
            return dst;
        }

        static void CreateTile(SpriteDef def, List<string> spritePaths)
        {
            Directory.CreateDirectory(TilesRoot);
            var sprites = new List<Sprite>();
            foreach (var p in spritePaths)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) sprites.Add(s);
            }
            if (sprites.Count == 0) return;

            var tilePath = $"{TilesRoot}/{def.Name}.asset";
            var collider = def.TileSolid ? Tile.ColliderType.Grid : Tile.ColliderType.None;

            if (sprites.Count == 1)
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    AssetDatabase.DeleteAsset(tilePath);
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprites[0];
                tile.colliderType = collider;
                EditorUtility.SetDirty(tile);
            }
            else
            {
                var tile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(tilePath);
                if (tile == null)
                {
                    AssetDatabase.DeleteAsset(tilePath);
                    tile = ScriptableObject.CreateInstance<AnimatedTile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.m_AnimatedSprites = sprites.ToArray();
                tile.m_MinSpeed = def.TileFps;
                tile.m_MaxSpeed = def.TileFps;
                tile.m_TileColliderType = collider;
                EditorUtility.SetDirty(tile);
            }
        }

        // ---------------------------------------------------------------- Parser

        static List<SpriteDef> Parse(string[] lines, string file)
        {
            var defs = new List<SpriteDef>();
            SpriteDef cur = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i].TrimEnd();
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                string Err(string msg) => $"{file}:{i + 1}: {msg}";

                switch (parts[0])
                {
                    case "sprite":
                        if (cur != null) throw new Exception(Err($"falta 'end' antes de '{line}'"));
                        cur = new SpriteDef { Name = parts[1] };
                        break;
                    case "size":
                        Need(cur, Err);
                        cur.Width = int.Parse(parts[1]);
                        cur.Height = int.Parse(parts[2]);
                        break;
                    case "pivot":
                        Need(cur, Err);
                        cur.Pivot = new Vector2(F(parts[1]), F(parts[2]));
                        break;
                    case "border":
                        Need(cur, Err);
                        cur.Border = new Vector4(F(parts[1]), F(parts[2]), F(parts[3]), F(parts[4]));
                        break;
                    case "outline":
                        Need(cur, Err);
                        cur.Outline = parts[1][0];
                        break;
                    case "frame":
                    {
                        Need(cur, Err);
                        if (cur.Width == 0) throw new Exception(Err("'size' tiene que ir antes de 'frame'"));
                        var grid = new char[cur.Height, cur.Width];
                        for (int y = 0; y < cur.Height; y++)
                        {
                            i++;
                            if (i >= lines.Length) throw new Exception(Err("el cuadro termina antes de tiempo"));
                            var row = lines[i].Trim();
                            for (int x = 0; x < cur.Width; x++)
                            {
                                var c = x < row.Length ? row[x] : PixelPalette.Transparent;
                                if (!PixelPalette.IsTransparent(c) && !PixelPalette.TryGet(c, out _))
                                    throw new Exception($"{file}:{i + 1}: color desconocido '{c}'");
                                grid[y, x] = c;
                            }
                        }
                        cur.Frames.Add(grid);
                        break;
                    }
                    case "flip":
                    {
                        Need(cur, Err);
                        var src = cur.Frames[int.Parse(parts[1])];
                        var grid = new char[cur.Height, cur.Width];
                        for (int y = 0; y < cur.Height; y++)
                        for (int x = 0; x < cur.Width; x++)
                            grid[y, x] = src[y, cur.Width - 1 - x];
                        cur.Frames.Add(grid);
                        break;
                    }
                    case "shift":
                    {
                        Need(cur, Err);
                        var src = cur.Frames[int.Parse(parts[1])];
                        int dx = int.Parse(parts[2]), dy = int.Parse(parts[3]);
                        var grid = new char[cur.Height, cur.Width];
                        for (int y = 0; y < cur.Height; y++)
                        for (int x = 0; x < cur.Width; x++)
                        {
                            int sx = x - dx, sy = y - dy;
                            grid[y, x] = sx >= 0 && sx < cur.Width && sy >= 0 && sy < cur.Height
                                ? src[sy, sx]
                                : PixelPalette.Transparent;
                        }
                        cur.Frames.Add(grid);
                        break;
                    }
                    case "copyfrom":
                    {
                        Need(cur, Err);
                        var other = defs.Find(d => d.Name == parts[1])
                                    ?? throw new Exception(Err($"no existe el sprite '{parts[1]}' (tiene que estar antes)"));
                        foreach (var f in other.Frames) cur.Frames.Add((char[,])f.Clone());
                        break;
                    }
                    case "mirror":
                    {
                        Need(cur, Err);
                        for (int f = 0; f < cur.Frames.Count; f++)
                        {
                            var src = cur.Frames[f];
                            var grid = new char[cur.Height, cur.Width];
                            for (int y = 0; y < cur.Height; y++)
                            for (int x = 0; x < cur.Width; x++)
                                grid[y, x] = src[y, cur.Width - 1 - x];
                            cur.Frames[f] = grid;
                        }
                        break;
                    }
                    case "variant":
                    {
                        Need(cur, Err);
                        var map = new Dictionary<char, char>();
                        for (int p = 2; p < parts.Length; p++)
                        {
                            var pair = parts[p].Split('>');
                            if (pair.Length != 2 || pair[0].Length != 1 || pair[1].Length != 1)
                                throw new Exception(Err($"cambio de color inválido '{parts[p]}' (usar a>b)"));
                            map[pair[0][0]] = pair[1][0];
                        }
                        cur.Variants.Add((parts[1], map));
                        break;
                    }
                    case "tile":
                        Need(cur, Err);
                        cur.IsTile = true;
                        for (int p = 1; p < parts.Length; p++)
                        {
                            if (parts[p] == "solid") cur.TileSolid = true;
                            else cur.TileFps = F(parts[p]);
                        }
                        break;
                    case "end":
                        Need(cur, Err);
                        if (cur.Frames.Count == 0) throw new Exception(Err($"'{cur.Name}' no tiene cuadros"));
                        defs.Add(cur);
                        cur = null;
                        break;
                    default:
                        throw new Exception(Err($"instrucción desconocida '{parts[0]}'"));
                }
            }
            if (cur != null) throw new Exception($"{file}: falta 'end' en '{cur.Name}'");
            return defs;
        }

        static void Need(SpriteDef cur, Func<string, string> err)
        {
            if (cur == null) throw new Exception(err("instrucción fuera de un 'sprite'"));
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
    }
}
