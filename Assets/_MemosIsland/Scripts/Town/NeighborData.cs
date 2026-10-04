using System;
using System.Collections.Generic;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Town
{
    [Flags]
    public enum Days
    {
        None = 0, Sun = 1, Mon = 2, Tue = 4, Wed = 8, Thu = 16, Fri = 32, Sat = 64,
        Weekdays = Mon | Tue | Wed | Thu | Fri,
        Weekend = Sat | Sun,
        All = Weekdays | Weekend,
    }

    public enum WeatherFilter { Any, Sunny, Rainy }

    /// <summary>Desde esta hora, el vecino está en este lugar (escena + casilla). Escena vacía = en su casa (no se ve).</summary>
    [Serializable]
    public class ScheduleEntry
    {
        public float hour;
        public Days days = Days.All;
        public WeatherFilter weather;
        public string scene;
        public Vector2Int cell;
        public Direction facing = Direction.Down;
        [Tooltip("Qué está haciendo: \"trabajo\" abre su negocio; el resto es para los diálogos.")]
        public string activity;
        [Tooltip("Lo que dice si le hablás mientras hace esto (opcional).")]
        [TextArea] public string line;

        public bool AtHome => string.IsNullOrEmpty(scene);
    }

    /// <summary>Escena de amistad: se dispara al hablarle con suficientes corazones.</summary>
    [Serializable]
    public class FriendshipEvent
    {
        public string id;
        public int hearts;
        [Tooltip("Solo pasa en esta escena (vacío = en cualquier lado).")]
        public string scene;
        [TextArea] public List<string> pages = new();
        [Tooltip("Pregunta final (opcional) con respuestas que suman (o restan) amistad.")]
        public string question;
        public List<string> answers = new();
        public List<int> answerPoints = new();
        [TextArea] public List<string> replies = new();
    }

    public enum RequestKind { Deliver, ShowMemo }

    /// <summary>Pedido posible para el tablón: entregar objetos o mostrarle un Memo (de compañero).</summary>
    [Serializable]
    public class RequestTemplate
    {
        public RequestKind kind;
        [Tooltip("Objeto (Deliver) o especie (ShowMemo).")]
        public string targetId;
        public int count = 1;
        public int reward = 100;
        [TextArea] public string boardText;
        [TextArea] public string thanks;
    }

    /// <summary>Un vecino de la isla (GDD §5): datos, gustos, diálogos, rutina, eventos y pedidos.</summary>
    [CreateAssetMenu(menuName = "Memos Island/Vecino", fileName = "Neighbor")]
    public class NeighborData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string role;
        public Sprite portrait;
        [Tooltip("Cabecita de 16x16 para las listas.")]
        public Sprite icon;
        public Sprite[] down = new Sprite[3], up = new Sprite[3], left = new Sprite[3], right = new Sprite[3];

        [Header("Regalos")]
        public List<string> loved = new();
        public List<string> liked = new();
        public List<string> disliked = new();
        [TextArea] public string lovedText = "¡¿Para mí?! ¡Me encanta!";
        [TextArea] public string likedText = "Gracias, qué lindo.";
        [TextArea] public string neutralText = "Ah… gracias.";
        [TextArea] public string dislikedText = "Eh… no es lo mío.";

        [Header("Diálogos")]
        [TextArea] public string firstMeet;
        [Tooltip("0 a 2 corazones")] [TextArea] public List<string> linesLow = new();
        [Tooltip("3 a 6 corazones")] [TextArea] public List<string> linesMid = new();
        [Tooltip("7 a 10 corazones")] [TextArea] public List<string> linesHigh = new();
        [TextArea] public List<string> linesRain = new();
        [Tooltip("Si tu compañero es un Memo (opcional; {memo} = su nombre).")]
        [TextArea] public List<string> linesCompanion = new();

        [Header("Rutina, eventos y pedidos")]
        public List<ScheduleEntry> schedule = new();
        public List<FriendshipEvent> events = new();
        public List<RequestTemplate> requests = new();
    }
}
