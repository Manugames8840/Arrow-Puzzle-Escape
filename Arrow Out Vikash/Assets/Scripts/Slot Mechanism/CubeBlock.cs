using UnityEngine;

namespace ArrowOut
{
    /// <summary>
    /// Individual cube in the cube grid.
    /// Colored to match its source arrow via ArrowPath.colorId.
    /// </summary>
    public class CubeBlock : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer; // assign in prefab
        [SerializeField] private ParticleSystem destroyFX;      // optional

        public Color CubeColor  { get; private set; }
        public int   ColorId    { get; private set; }
        public int   GridColumn { get; private set; }
        public int   GridRow    { get; private set; }
        public bool  IsDestroyed { get; private set; }

        private int _health = 1;

        // ─────────────────────────────────────────────────────────────────────

        public void Init(Color color, int colorId, int column, int row)
        {
            CubeColor   = color;
            ColorId     = colorId;
            GridColumn  = column;
            GridRow     = row;
            IsDestroyed = false;

            // Auto-find renderer if not assigned in prefab
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }

        /// <summary>Called by BallProjectile on impact.</summary>
        public void TakeHit(int damage = 1)
        {
            if (IsDestroyed) return;
            _health -= damage;
            if (_health <= 0) DestroyCube();
        }

        // ─────────────────────────────────────────────────────────────────────

        private void DestroyCube()
        {
            IsDestroyed = true;

            CubeRowManager.Instance?.RemoveCubeFromTracking(this);

            if (destroyFX != null)
            {
                destroyFX.transform.SetParent(null);
                destroyFX.Play();
                Destroy(destroyFX.gameObject, 2f);
            }

            // Broadcast for score / UI
            //ArrowOutEvents.OnCubeDestroyed?.Invoke(CubeColor, ColorId);

            Destroy(gameObject, 0.05f);
        }
    }
}
