using System;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.Race;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>
    /// Un Memo especial que se ve en el mapa (GDD §10 "algunos Memos raros se ven en el mapa"): el primer Memo con collar
    /// de la Ruta Pradera, Karman en los Acantilados con tormenta… Con A empieza la carrera de captura.
    /// Si ya lo conseguiste (marca), o todavía no toca (marca previa / clima), no aparece.
    /// </summary>
    public class StoryMemo : MonoBehaviour, IInteractable
    {
        [SerializeField] string speciesId;
        [SerializeField] int level = 5;
        [SerializeField] bool collared;
        [SerializeField] string terrainId = "pradera";
        [Tooltip("Marca que se pone al conseguirlo (y que hace que no vuelva a aparecer).")]
        [SerializeField] string capturedFlag;
        [Tooltip("Marca necesaria para que aparezca (vacía = siempre). En la partida de prueba no se pide.")]
        [SerializeField] string requiredFlag;
        [SerializeField] bool onlyInStorm;
        [TextArea] [SerializeField] string intro;

        /// <summary>Se consiguió un Memo de historia (la marca y el Memo). Lo escucha el StoryDirector.</summary>
        public static event Action<string, MemoInstance> Captured;

        MemoInstance _memo;

        public void Setup(string species, int lvl, bool withCollar, string terrain, string flag, string required, bool storm, string introText)
        {
            speciesId = species;
            level = lvl;
            collared = withCollar;
            terrainId = terrain;
            capturedFlag = flag;
            requiredFlag = required;
            onlyInStorm = storm;
            intro = introText;
        }

        void Start()
        {
            var root = GameRoot.Instance;
            var story = root.State.story;
            var now = GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;
            bool caught = story.Has(capturedFlag) || root.State.AllMemos.Any(m => m.speciesId == speciesId && IsUnique);
            bool notYet = story.active && !string.IsNullOrEmpty(requiredFlag) && !story.Has(requiredFlag);
            if (caught || notYet || onlyInStorm && !Weather.IsStormy(now))
            {
                Destroy(gameObject);
                return;
            }
            _memo = MemoInstance.Create(speciesId, level, trust: 0);
            _memo.collared = collared;
            var mover = GetComponent<GridMover>();
            GetComponentInChildren<MemoSpriteView>().Setup(mover, _memo);
            mover.Facing = Direction.Down;
        }

        bool IsUnique => _memo?.Species != null ? _memo.Species.category == MemoCategory.Legendary
            : MemoDatabase.Instance.GetSpecies(speciesId)?.category == MemoCategory.Legendary;

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var team = root.State.team;
            if (team.Count == 0) return;
            var options = team.Select(m => $"{m.DisplayName} Nv.{m.level}").ToList();
            options.Add("Ahora no");
            root.Dialogue.Show(new[] { intro }, () =>
                root.Dialogue.ShowChoice("¿Quién corre?", options, choice =>
                {
                    if (choice >= team.Count) return;
                    var flag = capturedFlag;
                    RaceLauncher.Start(RaceSetup.Capture(_memo, terrainId, team[choice]), result =>
                    {
                        if (result.captured == null) return;
                        root.State.story.Set(flag);
                        Captured?.Invoke(flag, result.captured);
                    });
                }, options.Count - 1));
        }
    }
}
