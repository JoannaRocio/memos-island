using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>
    /// Elige el sprite según la dirección y el paso del GridMover.
    /// Cada dirección tiene 3 cuadros: quieto, pie izquierdo, pie derecho.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        [SerializeField] GridMover mover;
        [SerializeField] Sprite[] down = new Sprite[3];
        [SerializeField] Sprite[] up = new Sprite[3];
        [SerializeField] Sprite[] left = new Sprite[3];
        [SerializeField] Sprite[] right = new Sprite[3];

        SpriteRenderer _renderer;

        public void Setup(GridMover target, Sprite[] downFrames, Sprite[] upFrames, Sprite[] leftFrames, Sprite[] rightFrames)
        {
            mover = target;
            down = downFrames;
            up = upFrames;
            left = leftFrames;
            right = rightFrames;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (mover == null) mover = GetComponentInParent<GridMover>();
        }

        void LateUpdate()
        {
            var frames = mover.Facing switch
            {
                Direction.Up => up,
                Direction.Left => left,
                Direction.Right => right,
                _ => down
            };
            if (frames == null || frames.Length == 0) return;

            int frame = 0;
            // Primera mitad del paso: pie adelantado (alternando); segunda mitad: quieto.
            if (mover.IsBusy && mover.StepProgress < 0.5f)
                frame = mover.StepCount % 2 == 0 ? 1 : 2;
            _renderer.sprite = frames[Mathf.Min(frame, frames.Length - 1)];
        }
    }
}
