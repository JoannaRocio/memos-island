using MemosIsland.World;
using UnityEngine;

namespace MemosIsland.Core
{
    /// <summary>Sigue al jugador sin mostrar fuera del mapa, siempre alineada a la grilla de pixels.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] int viewWidthPixels = 480;
        [SerializeField] int viewHeightPixels = 270;

        const float Ppu = GridMover.PixelsPerUnit;

        public void SetTarget(Transform t) => target = t;

        public void SnapNow() => LateUpdate();

        void LateUpdate()
        {
            if (target == null) return;
            // El jugador se mide desde sus pies: centramos en el medio de su casilla.
            var p = (Vector2)target.position + new Vector2(0f, 0.5f);

            var map = MapInfo.Current;
            if (map != null)
            {
                var b = map.Bounds;
                p.x = ClampAxis(p.x, b.xMin, b.xMax, viewWidthPixels / Ppu / 2f);
                p.y = ClampAxis(p.y, b.yMin, b.yMax, viewHeightPixels / Ppu / 2f);
            }

            transform.position = new Vector3(Mathf.Round(p.x * Ppu) / Ppu, Mathf.Round(p.y * Ppu) / Ppu, -10f);
        }

        /// <summary>Si el mapa es más chico que la pantalla, lo centra; si no, no deja ver afuera.</summary>
        public static float ClampAxis(float value, float min, float max, float halfView)
        {
            if (max - min <= halfView * 2f) return (min + max) / 2f;
            return Mathf.Clamp(value, min + halfView, max - halfView);
        }
    }
}
