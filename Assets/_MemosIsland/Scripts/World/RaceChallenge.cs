using System;
using System.Collections.Generic;
using System.Linq;
using MemosIsland.Core;
using MemosIsland.Race;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Cartel o personaje que te desafía a una carrera (entrenador, amistosa o Copa).
    /// Los rivales y la pista se definen en el inspector.
    /// </summary>
    public class RaceChallenge : MonoBehaviour, IInteractable
    {
        [Serializable]
        public class Member
        {
            public string speciesId;
            public int level = 5;
            public bool collared;
        }

        [Serializable]
        public class Rival
        {
            public string name;
            public List<Member> team = new();
        }

        [SerializeField] RaceFormat format = RaceFormat.Trainer;
        [SerializeField] string title = "Carrera";
        [SerializeField, TextArea] string question = "¿Corremos una carrera?";
        [SerializeField] List<RaceSegmentDef> segments = new();
        [SerializeField] List<Rival> rivals = new();
        [SerializeField, TextArea] string winText = "¡Ganaste!";
        [SerializeField, TextArea] string loseText = "¡Casi! La próxima seguro.";

        public void Setup(RaceFormat raceFormat, string raceTitle, string ask, List<RaceSegmentDef> track,
            List<Rival> rivalList, string win, string lose)
        {
            format = raceFormat;
            title = raceTitle;
            question = ask;
            segments = track;
            rivals = rivalList;
            winText = win;
            loseText = lose;
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            if (root.State.team.Count == 0)
            {
                root.Dialogue.Show(new[] { "Para correr necesitás al menos un Memo en tu equipo." });
                return;
            }
            root.Dialogue.ShowChoice(question, new[] { "¡Sí!", "Ahora no" }, choice =>
            {
                if (choice != 0) return;
                RaceLauncher.Start(BuildSetup(root.State), result =>
                    root.Dialogue.Show(new[] { result.playerWon ? winText : loseText }));
            }, 1);
        }

        RaceSetup BuildSetup(GameState state) => new()
        {
            format = format,
            title = title,
            segments = segments.Select(s => new RaceSegmentDef(s.terrainId, s.length)).ToList(),
            player = new RacerSetup { name = "Vos", team = state.team.Take(GameState.MaxTeamSize).ToList() },
            rivals = rivals.Select(r => RaceSetups.Rival(r.name,
                r.team.Select(m => (m.speciesId, m.level, m.collared)).ToArray())).ToList(),
        };
    }
}
