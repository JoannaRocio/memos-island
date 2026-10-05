using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>Menú de pausa estilo GBA (arriba a la derecha): Mis Memos · MemoBox · Mochila · Guardar · Opciones · Cerrar. Se abre con Esc/Tab.</summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] PixelFont font;
        [SerializeField] Sprite boxSprite;
        [SerializeField] Material material;

        const float Ppu = PixelFont.PixelsPerUnit;
        const int Order = 1200, RowHeight = 12;
        static readonly string[] Options = { "Mis Memos", "MemoBox", "Mochila", "Guardar", "Opciones", "Cerrar" };

        GameObject _root;
        PixelText _cursor;
        SpriteRenderer _objectiveBox;
        PixelText _objective;
        int _selected;
        float _repeat;
        bool _open;

        public void Setup(PixelFont newFont, Sprite box, Material mat)
        {
            font = newFont;
            boxSprite = box;
            material = mat;
        }

        void Awake()
        {
            _root = new GameObject("Pause Menu");
            _root.transform.SetParent(transform, false);
            const int width = 70, right = 118, top = 64;
            int height = Options.Length * RowHeight + 10;
            var box = new GameObject("Box").AddComponent<SpriteRenderer>();
            box.transform.SetParent(_root.transform, false);
            box.sprite = boxSprite;
            box.drawMode = SpriteDrawMode.Sliced;
            box.size = new Vector2(width / Ppu, height / Ppu);
            box.sharedMaterial = material;
            box.sortingOrder = Order;
            box.transform.localPosition = new Vector3((right - width / 2f) / Ppu, (top - height / 2f) / Ppu, 0f);
            for (int i = 0; i < Options.Length; i++)
            {
                var t = NewText(right - width + 16, top - 6 - i * RowHeight, new Color32(0x33, 0x3c, 0x57, 0xff), true);
                t.SetText(Options[i]);
            }
            _cursor = NewText(right - width + 7, top - 6, new Color32(0xb1, 0x3e, 0x53, 0xff), false);
            _cursor.SetText("▶");

            // Objetivo actual de la historia (Fase 8), abajo a la izquierda.
            _objectiveBox = new GameObject("Objective Box").AddComponent<SpriteRenderer>();
            _objectiveBox.transform.SetParent(_root.transform, false);
            _objectiveBox.sprite = boxSprite;
            _objectiveBox.drawMode = SpriteDrawMode.Sliced;
            _objectiveBox.sharedMaterial = material;
            _objectiveBox.sortingOrder = Order;
            _objective = NewText(-110, -30, new Color32(0x33, 0x3c, 0x57, 0xff), true);
            _root.SetActive(false);
        }

        PixelText NewText(float x, float y, Color color, bool shadow)
        {
            var go = new GameObject("t");
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = new Vector3(x / Ppu, y / Ppu, 0f);
            var t = go.AddComponent<PixelText>();
            t.Setup(font, material, Order + 10, color, shadow);
            return t;
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null) return;
            if (!_open)
            {
                if (GameInput.MenuPressed && !GameRoot.InputLocked && !GameRoot.UiInputConsumed && !root.Player.Mover.IsBusy
                    && root.Player.gameObject.activeInHierarchy && !root.Maps.IsTransitioning)
                    Open();
                return;
            }

            int dir = GameInput.Move.y > 0.5f ? -1 : GameInput.Move.y < -0.5f ? 1 : 0;
            if (dir == 0) _repeat = 0f;
            else if ((_repeat -= Time.unscaledDeltaTime) <= 0f)
            {
                _selected = (_selected + dir + Options.Length) % Options.Length;
                _repeat = 0.2f;
            }
            _cursor.transform.localPosition = new Vector3((118 - 70 + 7) / Ppu, (64 - 6 - _selected * RowHeight) / Ppu, 0f);

            if (GameInput.CancelPressed || GameInput.MenuPressed)
            {
                Close();
            }
            else if (GameInput.ConfirmPressed)
            {
                int choice = _selected;
                Close();
                if (choice == 0) root.MyMemos.Open(null);
                else if (choice == 1) root.MemoBox.Open();
                else if (choice == 2) IslandMenus.OpenBag();
                else if (choice == 3) Save(root);
                else if (choice == 4) OptionsMenu.Open();
            }
        }

        static void Save(GameRoot root)
        {
            if (root.CurrentSlot >= 0) AudioManager.Sfx("save");
            string text = root.CurrentSlot < 0
                ? "La partida de prueba no se guarda. Empezá una Nueva partida desde el título para guardar."
                : root.SaveNow() ? $"Partida guardada en la ranura {root.CurrentSlot + 1}." : "No se pudo guardar. Probá de nuevo.";
            root.Dialogue.Show(new[] { text });
        }

        void Open()
        {
            _open = true;
            GameRoot.InputLocks++;
            _root.SetActive(true);
            RefreshObjective();
        }

        void RefreshObjective()
        {
            var text = GameRoot.Instance.State.story.objective;
            bool show = !string.IsNullOrEmpty(text);
            _objectiveBox.gameObject.SetActive(show);
            _objective.gameObject.SetActive(show);
            if (!show) return;
            var lines = font.Wrap($"Objetivo: {text}", 210);
            _objective.SetText(string.Join("\n", lines));
            float h = lines.Count * font.lineHeight + 8;
            const float bottom = -66f;
            _objectiveBox.size = new Vector2(224f / Ppu, h / Ppu);
            _objectiveBox.transform.localPosition = new Vector3(0f, (bottom + h / 2f) / Ppu, 0f);
            _objective.transform.localPosition = new Vector3(-106f / Ppu, (bottom + h - 4f) / Ppu, 0f);
        }

        void Close()
        {
            GameRoot.ConsumeUiInput();
            _open = false;
            GameRoot.InputLocks--;
            _root.SetActive(false);
        }
    }
}
