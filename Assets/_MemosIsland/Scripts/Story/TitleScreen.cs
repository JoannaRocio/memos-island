using System.Collections;
using System.Collections.Generic;
using MemosIsland.Core;
using MemosIsland.Memos;
using MemosIsland.UI;
using UnityEngine;

namespace MemosIsland.Story
{
    /// <summary>
    /// Pantalla de título (escena "Title"): Nueva partida (prólogo → creación de personaje → llegada al pueblo)
    /// y, en el editor o en builds de desarrollo, Partida de prueba (el equipo y los objetos de prueba).
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        public const string SceneName = "Title";
        const int Order = 900;

        [SerializeField] Sprite eyes;

        public void Setup(Sprite glowingEyes) => eyes = glowingEyes;

        UiKit _ui;
        readonly List<GameObject> _titleItems = new();

        IEnumerator Start()
        {
            while (GameRoot.Instance == null) yield return null;
            yield return null;
            var root = GameRoot.Instance;
            Cutscene.Begin();
            UiKit.FullScreens++;
            SetPlayerVisible(false);

            _ui = new UiKit("Title Screen");
            _ui.Rect(0, 0, 260, 150, new Color32(0x1a, 0x1c, 0x2c, 0xff), Order);

            // Estrellitas, el logo, el subtítulo y Tostín corriendo en el lugar.
            var rnd = new System.Random(5);
            for (int i = 0; i < 40; i++)
            {
                var star = _ui.Rect(rnd.Next(-118, 118), rnd.Next(-64, 66), 1, 1, i % 4 == 0 ? UiKit.Gold : UiKit.Gray, Order + 1);
                _titleItems.Add(star.gameObject);
            }
            var logo = _ui.Text(0, 52, UiKit.Gold, Order + 5);
            logo.transform.localScale = new Vector3(2f, 2f, 1f);
            logo.SetText("Memos Island");
            _ui.Center(logo, 0, 62);
            var subtitle = _ui.Text(0, 40, UiKit.White, Order + 5, false);
            subtitle.SetText("La Isla de los Recuerdos");
            _ui.Center(subtitle, 0, 40);
            var species = MemoDatabase.Instance.GetSpecies("tostin");
            var mascot = _ui.Sprite(species != null ? species.Portrait : null, 0, -34, Order + 5);
            _titleItems.AddRange(new[] { logo.gameObject, subtitle.gameObject, mascot.gameObject });

            var options = new List<string> { "Nueva partida" };
            if (Debug.isDebugBuild) options.Add("Partida de prueba (sin historia)");
            var rows = new List<PixelText>();
            for (int i = 0; i < options.Count; i++)
            {
                var t = _ui.Text(0, -46 - i * 11, UiKit.White, Order + 5);
                t.SetText(options[i]);
                _ui.Center(t, 0, -46 - i * 11);
                rows.Add(t);
                _titleItems.Add(t.gameObject);
            }
            var cursor = _ui.Text(0, -40, UiKit.Red, Order + 6, false);
            cursor.SetText("▶");
            _titleItems.Add(cursor.gameObject);

            int selected = 0, held = 0;
            float anim = 0f;
            while (true)
            {
                anim += Time.deltaTime;
                if (species != null && species.raceFrames is { Length: > 1 })
                    mascot.sprite = species.raceFrames[(int)(anim * 4f) % species.raceFrames.Length];
                for (int i = 0; i < rows.Count; i++) rows[i].SetColor(i == selected ? UiKit.Gold : UiKit.White);
                cursor.transform.localPosition = UiKit.P(rows[selected].transform.localPosition.x * 16f - 9f, -46 - selected * 11);

                var move = GameInput.Move;
                int v = move.y > 0.5f ? -1 : move.y < -0.5f ? 1 : 0;
                if (v != 0 && v != held) selected = (selected + v + rows.Count) % rows.Count;
                held = v;
                if (GameInput.ConfirmPressed) break;
                yield return null;
            }

            if (selected == 1) yield return DebugGame(root);
            else yield return NewGame(root);
        }

        void SetPlayerVisible(bool visible)
        {
            foreach (var r in GameRoot.Instance.Player.GetComponentsInChildren<SpriteRenderer>()) r.enabled = visible;
        }

        IEnumerator Leave(string scene, string spawn)
        {
            var root = GameRoot.Instance;
            root.Maps.GoTo(scene, spawn);
            yield return new WaitForSeconds(0.3f); // ya está todo negro
            _ui.Destroy();
            UiKit.FullScreens--;
            SetPlayerVisible(true);
            Cutscene.End();
        }

        IEnumerator DebugGame(GameRoot root)
        {
            root.StartDebugGame();
            yield return Leave(StoryDirector.RefugioExterior, "default");
        }

        IEnumerator NewGame(GameRoot root)
        {
            root.StartNewGame();
            foreach (var go in _titleItems) go.SetActive(false);
            yield return Prologue();

            var profile = root.State.story.profile;
            yield return CharacterCreator.Run(profile, null);
            PlayerLook.Apply(root.Player.Mover, profile);
            yield return Cutscene.Narrate("(Un año después de aquella noche…)");
            yield return Leave(StoryDirector.Pueblo, "muelle");
        }

        /// <summary>Prólogo (GDD §4): pantalla negra, tormenta, cadenas, dos ojos, la voz, el destello violeta y el título.</summary>
        IEnumerator Prologue()
        {
            yield return new WaitForSeconds(0.8f);
            yield return Cutscene.Narrate("(Una tormenta. Truenos que hacen temblar el piso.)");
            yield return Cutscene.Flash(new Color(1f, 1f, 1f, 0.75f), 0.05f, 0.5f);
            yield return Cutscene.Narrate("(Voces que gritan. Cadenas que se arrastran sobre la piedra.)");

            var eyesRenderer = _ui.Sprite(eyes, 0, 18, Order + 5);
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                eyesRenderer.color = new Color(1f, 1f, 1f, t / 1.2f);
                yield return null;
            }
            yield return Cutscene.Narrate("(En la oscuridad, dos ojos brillan. Algo enorme respira.)");
            yield return Cutscene.Say("???", null, "Este es fuerte. Pónganle el collar.");
            yield return Cutscene.Narrate("(Un grito.)");
            Destroy(eyesRenderer.gameObject);
            yield return Cutscene.Flash(new Color(0.61f, 0.24f, 1f, 1f), 0.25f, 1.6f);
            yield return new WaitForSeconds(0.6f);
            yield return Cutscene.Narrate("(…Después, silencio.)");

            var title = _ui.Text(0, 12, UiKit.Gold, Order + 5);
            title.transform.localScale = new Vector3(2f, 2f, 1f);
            title.SetText("Memos Island");
            _ui.Center(title, 0, 14);
            var chapter = _ui.Text(0, -10, UiKit.White, Order + 5, false);
            chapter.SetText("Capítulo 1: El Collar");
            _ui.Center(chapter, 0, -10);
            yield return new WaitForSeconds(2.5f);
            Destroy(title.gameObject);
            Destroy(chapter.gameObject);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
