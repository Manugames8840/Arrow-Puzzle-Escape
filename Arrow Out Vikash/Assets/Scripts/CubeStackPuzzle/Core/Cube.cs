using UnityEngine;

namespace CubeStackPuzzle
{
    /// <summary>
    /// Individual 3D cube on the puzzle board.
    /// Uses MeshRenderer for visuals. Stores its CubeColor, column, and height.
    /// </summary>
    public class Cube : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private ParticleSystem destroyFX;

        // ── Public Properties ──────────────────────────────────────────────
        public CubeColor CubeColor   { get; private set; }
        public int       ColumnIndex  { get; private set; }
        public int       HeightLevel  { get; private set; }
        public bool      IsDestroyed  { get; private set; }
        public bool      IsReserved   { get; private set; }

        private Material _runtimeMaterial;

        // ── Initialization ─────────────────────────────────────────────────

        /// <summary>
        /// Called by the board after instantiation.
        /// </summary>
        public void Init(CubeColor color, int columnIndex, int heightLevel)
        {
            CubeColor   = color;
            ColumnIndex  = columnIndex;
            HeightLevel  = heightLevel;
            IsDestroyed  = false;
            IsReserved   = false;

            ApplyVisuals(color);
        }

        /// <summary>
        /// Update the stored height level (called after gravity shift).
        /// </summary>
        public void SetHeightLevel(int newLevel)
        {
            HeightLevel = newLevel;
        }

        // ── Reservation ────────────────────────────────────────────────────

        /// <summary>
        /// Reserve this cube so only one character can target it.
        /// </summary>
        public bool Reserve()
        {
            if (IsReserved || IsDestroyed) return false;
            IsReserved = true;
            return true;
        }

        /// <summary>
        /// Release the reservation (e.g. if the character cancelled targeting).
        /// </summary>
        public void ReleaseReservation()
        {
            IsReserved = false;
        }

        // ── Destruction ────────────────────────────────────────────────────

        /// <summary>
        /// Destroy this cube with optional particle FX.
        /// </summary>
        public void DestroyCube()
        {
            if (IsDestroyed) return;
            IsDestroyed  = true;
            IsReserved   = false;

            if (destroyFX != null)
            {
                destroyFX.transform.SetParent(null);
                destroyFX.Play();
                Object.Destroy(destroyFX.gameObject, destroyFX.main.duration + 1f);
            }

            Object.Destroy(gameObject, 0.05f);
        }

        // ── Visuals ────────────────────────────────────────────────────────

        private void ApplyVisuals(CubeColor color)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();

            if (meshRenderer == null) return;

            Color unityColor = CubeColorUtility.ToUnityColor(color);

            if (meshRenderer.sharedMaterial != null)
                _runtimeMaterial = new Material(meshRenderer.sharedMaterial);
            else
                _runtimeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            _runtimeMaterial.color = unityColor;
            meshRenderer.material = _runtimeMaterial;
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
                Destroy(_runtimeMaterial);
        }
    }
}
