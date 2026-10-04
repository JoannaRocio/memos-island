using System;
using MemosIsland.Core;
using MemosIsland.Farm;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>
    /// Mesa de inscripción de la Copa de la Isla (estadio). La Copa se corre los sábados de 10 a 18 con al menos
    /// 3 Memos; si perdés, se vuelve a intentar el sábado siguiente. En la partida de prueba se puede correr cualquier día.
    /// </summary>
    public class CupDesk : MonoBehaviour, IInteractable
    {
        public const int MinTeam = 3;

        /// <summary>Lógica pura (con tests): ¿se puede correr la Copa ahora? Si no, el motivo.</summary>
        public static string Blocker(DateTime now, int teamSize, string lostOn, bool storyActive, bool won)
        {
            if (won) return "¡Ya ganaste la Copa de la Isla! Tu nombre está grabado en el trofeo del estadio.";
            if (storyActive && now.DayOfWeek != DayOfWeek.Saturday)
                return "La Copa de la Isla se corre los sábados, de 10 a 18. ¡Te esperamos!";
            if (storyActive && (now.Hour < 10 || now.Hour >= 18))
                return "Las inscripciones de la Copa abren los sábados de 10 a 18.";
            if (storyActive && lostOn == IslandState.DateKey(now))
                return "Hoy ya corriste. La próxima Copa es el sábado que viene: ¡a entrenar!";
            if (teamSize < MinTeam)
                return $"Para inscribirte necesitás al menos {MinTeam} Memos en tu equipo. Una Copa se corre en relevos.";
            return null;
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var story = root.State.story;
            var now = GameClock.Instance != null ? GameClock.Instance.Now : DateTime.Now;
            var why = Blocker(now, root.State.team.Count, story.cupLostOn, story.active, story.Has("cup_won"));
            if (why != null)
            {
                root.Dialogue.SetSpeaker("Inscripciones", null);
                root.Dialogue.Show(new[] { why });
                return;
            }
            root.Dialogue.SetSpeaker("Inscripciones", null);
            root.Dialogue.ShowChoice("Copa de la Isla: tres rondas seguidas. ¿Te inscribís?", new[] { "¡Sí!", "Todavía no" }, i =>
            {
                if (i == 0) StoryDirector.Instance.StartCup();
            }, 1);
        }
    }
}
