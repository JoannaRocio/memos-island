using MemosIsland.Core;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>Menú de pausa estilo GBA (arriba a la derecha): Mis Memos · MemoBox · Mochila · Cerrar. Se abre con Esc/Tab.</summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] PixelFont font;
        [SerializeField] Sprite boxSprite;
        [SerializeField] Material material;

        const float Ppu = PixelFont.PixelsPerUnit;
        const int Order = 1200, RowHeight = 12;
        static readonly string[] Options = { "Mis Memos", "MemoBox", "Mochila", "Cerrar" };

        GameObject _root;
        PixelText _cursor;
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
            const int width = 70, height = 58, right = 118, top = 64;
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
            }
        }

        void Open()
        {
            _open = true;
            GameRoot.InputLocks++;
            _root.SetActive(true);
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
