using System;
using System.Collections;
using MemosIsland.Core;
using MemosIsland.Story;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MemosIsland.UI
{
    /// <summary>
    /// Creación de personaje (GDD §17, versión básica): nombre, pronombre (independiente de la apariencia),
    /// piel, peinado, color de pelo, remera, pantalón y nombre del refugio, con vista previa que gira.
    /// ↑↓ elige la fila, ←→ cambia, A en un nombre lo edita con el teclado (Enter termina).
    /// </summary>
    public static class CharacterCreator
    {
        const int Order = 900; // debajo de la caja de diálogo (para la pregunta final)
        const int MaxName = 12;

        static readonly string[] Pronouns = { "Él", "Ella", "Elle" };

        public static IEnumerator Run(PlayerProfile profile, Action onDone)
        {
            var ui = new UiKit("Character Creator");
            UiKit.FullScreens++;
            ui.Rect(0, 0, 260, 150, new Color32(0x1a, 0x1c, 0x2c, 0xff), Order);
            ui.Box(-38, 0, 160, 130, Order + 1);
            ui.Box(80, 10, 70, 110, Order + 1);
            var title = ui.Text(-110, 60, UiKit.Dark, Order + 5);
            title.SetText("¿Cómo sos?");
            var hint = ui.Text(-110, -52, UiKit.MidGray, Order + 5, false);
            var preview = ui.Sprite(null, 80, -8, Order + 5);
            preview.transform.localScale = new Vector3(2f, 2f, 1f);
            var label = ui.Text(66, -30, UiKit.MidGray, Order + 5, false);

            string[] names = { "Nombre", "Pronombre", "Piel", "Peinado", "Pelo", "Remera", "Pantalón", "Refugio", "¡Listo!" };
            var rows = new PixelText[names.Length];
            var values = new PixelText[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                rows[i] = ui.Text(-104, 46 - i * 11, UiKit.Dark, Order + 5);
                rows[i].SetText(names[i]);
                values[i] = ui.Text(-50, 46 - i * 11, UiKit.Dark, Order + 5);
            }
            var cursor = ui.Text(-112, 46, UiKit.Red, Order + 6, false);
            cursor.SetText("▶");

            int row = 0, heldDir = 0;
            float repeat = 0f, spin = 0f;
            int facing = 0;
            bool editing = false;

            void OnText(char c)
            {
                if (!editing || char.IsControl(c)) return;
                if (row == 0 && profile.name.Length < MaxName) profile.name += c;
                else if (row == 7 && profile.refugeName.Length < 18) profile.refugeName += c;
            }

            string Value(int i) => i switch
            {
                0 => profile.name + (editing && row == 0 ? "_" : ""),
                1 => Pronouns[(int)profile.pronoun],
                2 => PlayerLook.Skins[profile.skin].name,
                3 => PlayerLook.HairStyles[profile.hairStyle],
                4 => PlayerLook.HairColors[profile.hairColor].name,
                5 => PlayerLook.ShirtColors[profile.shirtColor].name,
                6 => PlayerLook.PantsColors[profile.pantsColor].name,
                7 => (string.IsNullOrEmpty(profile.refugeName) ? "(del abuelo)" : profile.refugeName) + (editing && row == 7 ? "_" : ""),
                _ => "",
            };

            void Change(int d)
            {
                static int Wrap(int v, int n) => (v % n + n) % n;
                switch (row)
                {
                    case 1: profile.pronoun = (Pronoun)Wrap((int)profile.pronoun + d, 3); break;
                    case 2: profile.skin = Wrap(profile.skin + d, PlayerLook.Skins.Length); break;
                    case 3: profile.hairStyle = Wrap(profile.hairStyle + d, PlayerLook.HairStyles.Length); break;
                    case 4: profile.hairColor = Wrap(profile.hairColor + d, PlayerLook.HairColors.Length); break;
                    case 5: profile.shirtColor = Wrap(profile.shirtColor + d, PlayerLook.ShirtColors.Length); break;
                    case 6: profile.pantsColor = Wrap(profile.pantsColor + d, PlayerLook.PantsColors.Length); break;
                }
            }

            if (Keyboard.current != null) Keyboard.current.onTextInput += OnText;
            bool done = false;
            yield return null;
            while (!done)
            {
                // Vista previa: gira cada segundo (abajo, derecha, arriba, izquierda).
                spin += Time.unscaledDeltaTime;
                if (spin > 1f)
                {
                    spin = 0f;
                    facing = (facing + 1) % 4;
                }
                var frames = PlayerLook.Frames(profile);
                if (frames != null) preview.sprite = frames[new[] { 0, 3, 1, 2 }[facing]][0];
                label.SetText(profile.pronoun switch { Pronoun.El => "(él)", Pronoun.Ella => "(ella)", _ => "(elle)" });

                for (int i = 0; i < names.Length; i++)
                {
                    values[i].SetText(Value(i));
                    values[i].SetColor(i == row ? UiKit.Red : UiKit.Dark);
                }
                cursor.transform.localPosition = UiKit.P(-112, 46 - row * 11);
                hint.SetText(editing ? "Escribí con el teclado · Enter: listo"
                    : row is 0 or 7 ? "A: escribir · ↑↓: moverse"
                    : row == 8 ? "A: empezar" : "←→: cambiar · ↑↓: moverse");

                if (editing)
                {
                    var kb = Keyboard.current;
                    if (kb != null && kb.backspaceKey.wasPressedThisFrame)
                    {
                        if (row == 0 && profile.name.Length > 0) profile.name = profile.name.Substring(0, profile.name.Length - 1);
                        else if (row == 7 && profile.refugeName.Length > 0)
                            profile.refugeName = profile.refugeName.Substring(0, profile.refugeName.Length - 1);
                    }
                    if (kb == null || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame
                        || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                    {
                        editing = false;
                        if (string.IsNullOrWhiteSpace(profile.name)) profile.name = "Alex";
                        profile.name = profile.name.Trim();
                        GameRoot.ConsumeUiInput();
                        yield return null; // que el Enter no cuente también como A
                    }
                    yield return null;
                    continue;
                }

                if (GameInput.ConfirmPressed)
                {
                    if (row is 0 or 7)
                    {
                        editing = true;
                        if (row == 0) profile.name = "";
                    }
                    else if (row == 8)
                    {
                        bool? ok = null;
                        var p = profile;
                        GameRoot.Instance.Dialogue.ShowChoice(
                            $"Te llamás {p.name} ({Pronouns[(int)p.pronoun].ToLowerInvariant()}). ¿Empezamos?",
                            new[] { "¡Sí!", "Cambiar algo" }, i => ok = i == 0, 1);
                        while (ok == null) yield return null;
                        done = ok.Value;
                        yield return null;
                    }
                    yield return null;
                    continue;
                }

                var move = GameInput.Move;
                int v = move.y > 0.5f ? -1 : move.y < -0.5f ? 1 : 0;
                int h = move.x > 0.5f ? 1 : move.x < -0.5f ? -1 : 0;
                int dir = v != 0 ? v * 10 : h;
                if (dir == 0) heldDir = 0;
                else if (dir != heldDir || (repeat -= Time.unscaledDeltaTime) <= 0f)
                {
                    repeat = dir == heldDir ? 0.12f : 0.35f;
                    heldDir = dir;
                    if (v != 0) row = Mathf.Clamp(row + v, 0, names.Length - 1);
                    else Change(h);
                }
                yield return null;
            }
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText;
            ui.Destroy();
            UiKit.FullScreens--;
            onDone?.Invoke();
        }
    }
}
