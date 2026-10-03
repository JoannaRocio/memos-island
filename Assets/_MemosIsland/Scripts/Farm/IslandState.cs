using System;
using System.Collections.Generic;
using MemosIsland.Core;

namespace MemosIsland.Farm
{
    [Serializable]
    public class FarmPlot
    {
        public int x, y;
        public bool tilled;
        public string seedId;
        public int growth;          // días regados acumulados
        public string wateredOn;    // fecha (yyyy-MM-dd) del último riego
        public bool HasCrop => !string.IsNullOrEmpty(seedId);
    }

    [Serializable]
    public class SmelterJob
    {
        public string recipeId;
        public int count;
        public long readyTicks;
    }

    [Serializable]
    public class ProcessorJob
    {
        public string inputId;
        public int count;
        public string readyDate;
    }

    /// <summary>Todo lo de la vida en la isla (Fase 6). Vive dentro de GameState.</summary>
    [Serializable]
    public class IslandState
    {
        public int money = 500;

        // Herramientas: 0 = básica, 1 = cobre, 2 = hierro.
        public int hoeLevel, canLevel, pickLevel;

        public List<FarmPlot> plots = new();
        public List<ItemStack> shippingBin = new();
        public SmelterJob smelter;
        public ProcessorJob processor;

        public string lastProcessedDate;

        // Mina: rocas rotas hoy ("piso:x:y") y piso actual/máximo.
        public string mineDate;
        public List<string> brokenRocks = new();
        public int mineFloor = 1;
        public int deepestFloor = 1;

        // Recolección: lo juntado hoy ("escena:x:y").
        public string forageDate;
        public List<string> foraged = new();

        public FarmPlot PlotAt(int x, int y) => plots.Find(p => p.x == x && p.y == y);

        public static string DateKey(DateTime d) => d.Date.ToString("yyyy-MM-dd");

        /// <summary>Rocas y recolección se renuevan cada día real.</summary>
        public void ResetDailyIfNeeded(DateTime now)
        {
            var today = DateKey(now);
            if (mineDate != today)
            {
                mineDate = today;
                brokenRocks.Clear();
            }
            if (forageDate != today)
            {
                forageDate = today;
                foraged.Clear();
            }
        }
    }
}
