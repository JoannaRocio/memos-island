using MemosIsland.Memos;
using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Dibuja un Memo en el mundo con su sprite de perfil: mira a izquierda/derecha según hacia dónde camina,
    /// alterna los 2 cuadros al moverse, respira quieto, tiembla si tiene miedo y puede desvanecerse.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MemoSpriteView : MonoBehaviour
    {
        [SerializeField] GridMover mover;
        [Tooltip("Accesorio estético (gorrito, moño, bufanda…) dibujado encima del Memo.")]
        [SerializeField] SpriteRenderer accessory;

        SpriteRenderer _renderer;
        Sprite[] _frames;
        MemoInstance _memo;
        string _shownAccessory;
        bool _facingLeft;
        float _anim;
        float _alpha = 1f, _targetAlpha = 1f;

        public bool Trembling { get; set; }
        public bool Asleep { get; set; }
        public bool Hopping { get; set; }
        public SpriteRenderer Renderer => _renderer;

        public void Setup(GridMover target, MemoInstance memo)
        {
            mover = target;
            SetMemo(memo);
        }

        public void SetAccessoryRenderer(SpriteRenderer r) => accessory = r;

        public void SetMemo(MemoInstance memo)
        {
            _memo = memo;
            _shownAccessory = null;
            _frames = memo?.WorldFrames;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = _frames is { Length: > 0 } ? _frames[0] : null;
        }

        public void FadeTo(float alpha) => _targetAlpha = alpha;
        public bool FadeDone => Mathf.Approximately(_alpha, _targetAlpha);

        void Awake() => _renderer = GetComponent<SpriteRenderer>();

        void LateUpdate()
        {
            if (mover == null || _frames == null || _frames.Length == 0) return;

            if (mover.Facing == Direction.Left) _facingLeft = true;
            else if (mover.Facing == Direction.Right) _facingLeft = false;
            _renderer.flipX = _facingLeft;

            if (mover.IsMoving)
            {
                _anim += Time.deltaTime * 8f;
                _renderer.sprite = _frames[(int)_anim % _frames.Length];
            }
            else
            {
                _renderer.sprite = _frames[0];
            }

            // Pequeños movimientos para que se sienta vivo.
            var offset = Vector3.zero;
            if (Trembling) offset.x = (Time.time * 30f % 2f < 1f ? 1f : -1f) / 16f;
            else if (Hopping) offset.y = Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 4f / 16f;
            else if (!mover.IsMoving && !Asleep && Time.time % 2.4f < 0.3f) offset.y = 1f / 16f;
            transform.localPosition = new Vector3(Mathf.Round(offset.x * 16f) / 16f, Mathf.Round(offset.y * 16f) / 16f, 0f);

            _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, Time.deltaTime * 2.5f);
            var c = Asleep ? new Color(0.85f, 0.88f, 1f) : Color.white;
            c.a = _alpha;
            _renderer.color = c;
            UpdateAccessory(c);
        }

        /// <summary>Dibuja el accesorio en el punto de cabeza o cuello de la especie (en pixels del sprite de 32x32).</summary>
        void UpdateAccessory(Color tint)
        {
            if (accessory == null) return;
            var item = _memo?.Accessory;
            if (item == null || item.accessorySprite == null || _memo.Species == null)
            {
                accessory.enabled = false;
                return;
            }
            if (_shownAccessory != item.id)
            {
                _shownAccessory = item.id;
                accessory.sprite = item.accessorySprite;
            }
            var anchor = item.accessorySlot == AccessorySlot.Head ? _memo.Species.headAnchor : _memo.Species.neckAnchor;
            float x = (_facingLeft ? 16 - anchor.x : anchor.x - 16) / 16f;
            float y = (32 - anchor.y) / 16f;
            accessory.transform.localPosition = new Vector3(x, y, 0f);
            accessory.flipX = _facingLeft;
            accessory.sortingOrder = _renderer.sortingOrder + 1;
            accessory.color = tint;
            accessory.enabled = true;
        }
    }
}
