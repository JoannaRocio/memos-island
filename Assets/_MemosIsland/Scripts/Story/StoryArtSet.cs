using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>Sprites que usan las escenas de historia en tiempo de juego (los asigna el constructor; vive en Resources).</summary>
    public class StoryArtSet : ScriptableObject
    {
        public Sprite silex;
        public Sprite scarfFigure;

        static StoryArtSet _instance;
        public static StoryArtSet Instance => _instance != null ? _instance : _instance = Resources.Load<StoryArtSet>("StoryArt");
    }
}
