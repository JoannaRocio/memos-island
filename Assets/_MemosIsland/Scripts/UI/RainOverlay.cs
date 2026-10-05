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

        float _y, _nextLightning = 5f;
        SpriteRenderer _flash;

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
                                            || root.Lists != null && root.Lists.IsOpen || UiKit.FullScreens > 0
                                            || root.Evolution != null && root.Evolution.IsRunning);
            bool show = clock != null && !covered && MapInfo.Current != null && MapInfo.Current.Outdoor
                        && Weather.IsRainy(clock.Now);
            sheetA.enabled = sheetB.enabled = show;
            if (!show) return;

            // Tormenta: relámpagos cada tanto (un destello blanco breve).
            if (Weather.IsStormy(clock.Now) && (_nextLightning -= Time.deltaTime) <= 0f)
            {
                _nextLightning = Random.Range(6f, 14f);
                StartCoroutine(Lightning());
                AudioManager.Sfx("thunder", 0.6f, Random.Range(0.85f, 1.1f));
            }

            float h = sheetA.sprite.bounds.size.y;
            _y -= speed * Time.deltaTime;
            if (_y <= -h) _y += h;
            sheetA.transform.localPosition = new Vector3(0f, _y, 0f);
            sheetB.transform.localPosition = new Vector3(0f, _y + h, 0f);
        }

        System.Collections.IEnumerator Lightning()
        {
            if (_flash == null)
            {
                _flash = new GameObject("Lightning").AddComponent<SpriteRenderer>();
                _flash.transform.SetParent(transform, false);
                var pixel = GameRoot.Instance.Lists.PixelSprite;
                _flash.sprite = pixel;
                _flash.sharedMaterial = sheetA.sharedMaterial;
                _flash.sortingOrder = sheetA.sortingOrder + 1;
                float px = pixel != null ? pixel.rect.width : 2f;
                _flash.transform.localScale = new Vector3(300f / px, 170f / px, 1f);
            }
            foreach (var a in new[] { 0.55f, 0f, 0.4f, 0f })
            {
                _flash.color = new Color(1f, 1f, 1f, a);
                yield return new WaitForSeconds(0.07f);
            }
        }
    }
}
