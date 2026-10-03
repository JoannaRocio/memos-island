using MemosIsland.Core;
using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.UI
{
    /// <summary>Lluvia en pantalla (días de lluvia, solo al aire libre): dos láminas que caen y se repiten.</summary>
    public class RainOverlay : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sheetA;
        [SerializeField] SpriteRenderer sheetB;
        [SerializeField] float speed = 9f; // unidades de interfaz por segundo

        float _y;

        public void Setup(SpriteRenderer a, SpriteRenderer b)
        {
            sheetA = a;
            sheetB = b;
        }

        void Update()
        {
            var root = GameRoot.Instance;
            var clock = GameClock.Instance;
            bool covered = root != null && (root.MyMemos != null && root.MyMemos.IsOpen || root.MemoBox != null && root.MemoBox.IsOpen
                                            || root.Lists != null && root.Lists.IsOpen
                                            || root.Evolution != null && root.Evolution.IsRunning);
            bool show = clock != null && !covered && MapInfo.Current != null && MapInfo.Current.Outdoor
                        && Weather.IsRainy(clock.Now);
            sheetA.enabled = sheetB.enabled = show;
            if (!show) return;

            float h = sheetA.sprite.bounds.size.y;
            _y -= speed * Time.deltaTime;
            if (_y <= -h) _y += h;
            sheetA.transform.localPosition = new Vector3(0f, _y, 0f);
            sheetB.transform.localPosition = new Vector3(0f, _y + h, 0f);
        }
    }
}
