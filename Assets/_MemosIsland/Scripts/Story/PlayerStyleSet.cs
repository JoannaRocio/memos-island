using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>Sprites base del jugador por peinado (los arma el constructor de la Fase 8 en Resources).</summary>
    public class PlayerStyleSet : ScriptableObject
    {
        [Serializable]
        public class Style
        {
            public string name;
            public Sprite[] down = new Sprite[3], up = new Sprite[3], left = new Sprite[3], right = new Sprite[3];
        }

        public List<Style> styles = new();
    }
}
