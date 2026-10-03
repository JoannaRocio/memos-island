using UnityEngine;

namespace MemosIsland.World
{
    /// <summary>Animación simple por cuadros para sprites de pixel art.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(0.1f)] float framesPerSecond = 4f;

        SpriteRenderer _renderer;
        float _time;

        public void SetFrames(Sprite[] newFrames, float fps)
        {
            frames = newFrames;
            framesPerSecond = fps;
            _time = 0f;
        }

        void Awake() => _renderer = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            _time += Time.deltaTime;
            int index = (int)(_time * framesPerSecond) % frames.Length;
            _renderer.sprite = frames[index];
        }
    }
}
