using System;
using System.IO;
using System.Linq;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Core
{
    /// <summary>Una partida guardada: dónde estabas y todo el estado del juego (GDD §20 "Guardado y carga").</summary>
    [Serializable]
    public class SaveData
    {
        public int version = SaveSystem.Version;
        public string savedAt;
        public string scene;
        public string mapName;
        public int x, y;
        public Direction facing = Direction.Down;
        public float playSeconds;
        public GameState state;
    }

    /// <summary>
    /// Guardado en JSON con 3 ranuras (Application.persistentDataPath/saves/slotN.json). Se escribe primero a un archivo
    /// temporal y después se reemplaza, para no perder la partida si el juego se cierra a mitad de camino.
    /// </summary>
    public static class SaveSystem
    {
        public const int Version = 1;
        public const int Slots = 3;

        public static string Folder => Path.Combine(Application.persistentDataPath, "saves");
        static string PathFor(int slot) => Path.Combine(Folder, $"slot{slot + 1}.json");

        public static bool Exists(int slot) => File.Exists(PathFor(slot));

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        public static SaveData FromJson(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data?.state == null || string.IsNullOrEmpty(data.scene)) return null; // archivo vacío o dañado
            Normalize(data.state);
            return data;
        }

        public static bool Write(int slot, SaveData data)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                var path = PathFor(slot);
                var temp = path + ".tmp";
                File.WriteAllText(temp, ToJson(data));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Guardado] No se pudo guardar la ranura {slot + 1}: {e.Message}");
                return false;
            }
        }

        public static SaveData Read(int slot)
        {
            try
            {
                return Exists(slot) ? FromJson(File.ReadAllText(PathFor(slot))) : null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Guardado] La ranura {slot + 1} está dañada: {e.Message}");
                return null;
            }
        }

        /// <summary>La ranura guardada más recientemente (o -1 si no hay ninguna).</summary>
        public static int MostRecent()
        {
            var saved = Enumerable.Range(0, Slots).Select(i => (i, data: Read(i))).Where(s => s.data != null).ToList();
            if (saved.Count == 0) return -1;
            return saved.OrderByDescending(s => s.data.savedAt, StringComparer.Ordinal).First().i;
        }

        /// <summary>Texto de la ranura para los menús: "Joa · Refugio · 3 h 20 min".</summary>
        public static string Summary(int slot)
        {
            var d = Read(slot);
            if (d == null) return $"Ranura {slot + 1}: vacía";
            var name = string.IsNullOrEmpty(d.state.story.profile.name) ? "Partida" : d.state.story.profile.name;
            var t = TimeSpan.FromSeconds(d.playSeconds);
            var time = t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes} min" : $"{t.Minutes} min";
            return $"Ranura {slot + 1}: {name} · {d.mapName} · {time}";
        }

        /// <summary>
        /// JSON no guarda null: los objetos vuelven "vacíos" y los textos como "". Acá se devuelven a null
        /// los que el juego usa para saber si algo existe.
        /// </summary>
        public static void Normalize(GameState s)
        {
            if (string.IsNullOrEmpty(s.lastDailyDate)) s.lastDailyDate = null;
            var island = s.island;
            if (string.IsNullOrEmpty(island.lastProcessedDate)) island.lastProcessedDate = null;
            if (island.smelter != null && string.IsNullOrEmpty(island.smelter.recipeId)) island.smelter = null;
            if (island.processor != null && string.IsNullOrEmpty(island.processor.inputId)) island.processor = null;
        }
    }
}
