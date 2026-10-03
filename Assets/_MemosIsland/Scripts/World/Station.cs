using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.Memos;
using MemosIsland.UI;
using UnityEngine;

namespace MemosIsland.World
{
    public enum StationKind { Workbench, Smelter, Processor, ShippingBox, DenyShop, FerForge, Clinic, Board }

    /// <summary>Máquinas del refugio y puestos del pueblo (GDD §15).</summary>
    public class Station : MonoBehaviour, IInteractable
    {
        [SerializeField] StationKind kind;
        [Tooltip("Vecino que atiende (Fase 7). Vacío = siempre abierto.")]
        [SerializeField] string keeperId;

        public StationKind Kind => kind;

        public void Setup(StationKind stationKind, string keeper = null)
        {
            kind = stationKind;
            keeperId = keeper;
        }

        static DateTime Now => GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;

        public void Interact(PlayerController player)
        {
            if (!string.IsNullOrEmpty(keeperId) && KeeperHere())
            {
                // Del otro lado del mostrador: se charla con quien atiende (y desde ahí se compra).
                NeighborDirector.Current.Find(keeperId).Interact(player);
                return;
            }
            if (!string.IsNullOrEmpty(keeperId))
            {
                var keeper = MemoDatabase.Instance.GetNeighbor(keeperId);
                var hours = keeper != null ? Town.NeighborSchedule.HoursText(keeper, "trabajo", Now) : "";
                GameRoot.Instance.Dialogue.Show(new[]
                {
                    $"No hay nadie atendiendo." + (hours.Length > 0 ? $" Horario de {keeper.displayName}: {hours}." : ""),
                });
                return;
            }
            switch (kind)
            {
                case StationKind.Workbench: IslandMenus.OpenWorkbench(); break;
                case StationKind.ShippingBox: IslandMenus.OpenShipping(); break;
                case StationKind.DenyShop: IslandMenus.OpenDenyShop(); break;
                case StationKind.FerForge: IslandMenus.OpenFerForge(); break;
                case StationKind.Smelter: UseSmelter(); break;
                case StationKind.Processor: UseProcessor(); break;
                case StationKind.Board: TownMenus.OpenBoard(); break;
                case StationKind.Clinic:
                {
                    var anni = MemoDatabase.Instance.GetNeighbor(keeperId);
                    if (anni != null) GameRoot.Instance.Dialogue.SetSpeaker(anni.displayName, anni.portrait);
                    GameRoot.Instance.Dialogue.Show(TownMenus.ClinicCheckup());
                    break;
                }
            }
        }

        bool KeeperHere()
        {
            var director = NeighborDirector.Current;
            return director != null && director.IsWorking(keeperId);
        }

        static void UseSmelter()
        {
            var root = GameRoot.Instance;
            var state = root.State;
            var db = MemoDatabase.Instance;
            var job = state.island.smelter;
            if (job != null)
            {
                var recipe = db.GetRecipe(job.recipeId);
                if (Crafting.CollectSmelter(state, recipe, Now))
                {
                    root.Dialogue.Show(new[] { $"Sacaste {job.count} {db.GetItem(recipe.output.id)?.displayName.ToLowerInvariant()}(s) de la fundición." });
                    return;
                }
                var minutes = Mathf.CeilToInt((float)(new DateTime(job.readyTicks) - Now).TotalMinutes);
                root.Dialogue.Show(new[] { $"La fundición está trabajando. Faltan unos {minutes} minutos." });
                return;
            }

            var recipes = db.recipes.Where(r => r != null && r.station == CraftStation.Smelter && Crafting.CanCraft(state, r)).ToList();
            if (recipes.Count == 0)
            {
                root.Dialogue.Show(new[] { "Para fundir necesitás 3 minerales del mismo tipo. Se consiguen en la Cueva." });
                return;
            }
            bool fire = IslandDay.HasHelper(state, Now, WorkRole.Smelt);
            var labels = recipes.Select(r =>
            {
                int times = MaxTimes(state, r);
                return $"{db.GetItem(r.output.id)?.displayName} ×{times}";
            }).ToList();
            labels.Add("Nada");
            root.Dialogue.ShowChoice(fire ? "Un Memo de fuego aviva la fundición. ¿Qué fundís?" : "¿Qué fundís?", labels, i =>
            {
                if (i >= recipes.Count) return;
                var r = recipes[i];
                int times = MaxTimes(state, r);
                Crafting.StartSmelting(state, r, times, Now, fire);
                var ready = new DateTime(state.island.smelter.readyTicks) - Now;
                root.Dialogue.Show(new[] { $"La fundición está lista en unos {Mathf.CeilToInt((float)ready.TotalMinutes)} minutos." });
            }, labels.Count - 1);
        }

        static int MaxTimes(GameState state, RecipeData r) => r.inputs.Min(i => state.CountOf(i.id) / i.count);

        static readonly string[] Fruits = { "frutilla", "fruto_silvestre" };

        static void UseProcessor()
        {
            var root = GameRoot.Instance;
            var state = root.State;
            var job = state.island.processor;
            if (job != null)
            {
                root.Dialogue.Show(new[] { $"Hay {job.count} fruta(s) adentro. Mañana van a ser mermelada (si hay un Memo eléctrico que la haga andar)." });
                return;
            }
            if (!IslandDay.HasHelper(state, Now, WorkRole.Process))
            {
                root.Dialogue.Show(new[] { "La procesadora necesita un Memo eléctrico contento para funcionar." });
                return;
            }
            var owned = Fruits.Where(f => state.CountOf(f) > 0).ToList();
            if (owned.Count == 0)
            {
                root.Dialogue.Show(new[] { "La procesadora convierte frutillas o frutos silvestres en mermelada." });
                return;
            }
            var labels = owned.Select(f => $"{MemoDatabase.Instance.GetItem(f)?.displayName} ×{state.CountOf(f)}").ToList();
            labels.Add("Nada");
            root.Dialogue.ShowChoice("¿Qué ponés en la procesadora?", labels, i =>
            {
                if (i >= owned.Count) return;
                int n = state.CountOf(owned[i]);
                state.RemoveItem(owned[i], n);
                state.island.processor = new ProcessorJob
                {
                    inputId = owned[i], count = n, readyDate = IslandState.DateKey(Now.AddDays(1)),
                };
                root.Dialogue.Show(new[] { $"Pusiste {n} fruta(s). Mañana vas a tener mermelada." });
            }, labels.Count - 1);
        }
    }
}
