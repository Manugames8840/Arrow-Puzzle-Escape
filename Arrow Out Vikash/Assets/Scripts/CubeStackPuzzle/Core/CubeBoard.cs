using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CubeStackPuzzle
{
    /// <summary>
    /// Generates a board of 3D cubes from CubeStack definitions.
    /// Cubes fill the board LEFT-TO-RIGHT, BOTTOM-TO-TOP within the board width.
    /// When a row is full, a new row starts above.
    /// Each CubeStack's cubes are placed sequentially so they remain grouped.
    /// Columns can contain cubes from different stacks (different colors).
    /// </summary>
    [ExecuteAlways]
    public class CubeBoard : MonoBehaviour
    {
        public static CubeBoard Instance { get; private set; }

        // ── Editor Preview ─────────────────────────────────────────────────
        [Header("── Editor Preview ──────────────────────────────")]
        [Tooltip("Enable to see cubes in the Scene view without entering Play Mode.")]
        public bool previewInEditor = false;

        // ── CubeStack Definitions ──────────────────────────────────────────
        [Header("── Cube Stacks (Board Content) ────────────────")]
        [Tooltip("Each entry defines a group of same-colored cubes. " +
                 "All cubes fill the board row by row within the board width.")]
        [SerializeField] private CubeStack[] cubeStacks;

        // ── Board Parameters ───────────────────────────────────────────────
        [Header("── Board Parameters ───────────────────────────")]
        [Tooltip("World-space size of one cube.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float cubeSize = 1f;

        [Tooltip("Horizontal spacing between adjacent columns.")]
        [Range(0f, 3f)]
        [SerializeField] private float horizontalSpacing = 0.15f;

        [Tooltip("Vertical spacing between cubes in a column.")]
        [Range(0f, 3f)]
        [SerializeField] private float verticalSpacing = 0.15f;

        [Tooltip("Uniform scale applied to each spawned cube GameObject.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float cubeScale = 1f;

        // ── Board Boundary ─────────────────────────────────────────────────
        [Header("── Board Boundary (Horizontal) ────────────────")]
        [Tooltip("Maximum horizontal width. Cubes per row is calculated from this.")]
        [Min(0.5f)]
        [SerializeField] private float boardWidth = 12f;

        [Tooltip("Depth of the boundary gizmo (Z-axis).")]
        [Min(0.5f)]
        [SerializeField] private float boardDepth = 2f;

        [Tooltip("Color of the boundary gizmo.")]
        [SerializeField] private Color boundaryGizmoColor = new Color(0f, 1f, 0.5f, 0.35f);

        // ── Cube Prefab ────────────────────────────────────────────────────
        [Header("── Cube Prefab ────────────────────────────────")]
        [Tooltip("3D cube prefab. Must have a MeshRenderer.")]
        [SerializeField] private GameObject cubePrefab;

        // ── Internal ───────────────────────────────────────────────────────
        [Header("── Internal ───────────────────────────────────")]
        [SerializeField] private Transform cubeGridParent;

        private readonly List<CubeColumn> _columns = new();
        private readonly List<GameObject> _previewObjects = new();

        // ── Public Accessors ───────────────────────────────────────────────
        public CubeStack[]               CubeStacks  => cubeStacks;
        public IReadOnlyList<CubeColumn>  Columns     => _columns.AsReadOnly();

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance != null && Instance != this) { Destroy(this); return; }
                Instance = this;
            }
            EnsureParent();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) RefreshEditorPreview();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) ClearPreview();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                EditorApplication.delayCall += () =>
                {
                    if (this != null) RefreshEditorPreview();
                };
#endif
            }
        }

        // =====================================================================
        // PUBLIC API — RUNTIME
        // =====================================================================

        /// <summary>
        /// Generates the board. Cubes fill left-to-right, bottom-to-top
        /// within the board width. When a row is full, a new row starts above.
        /// </summary>
        public void GenerateBoard()
        {
            ClearBoard();
            EnsureParent();

            if (cubePrefab == null)
            {
                Debug.LogWarning("[CubeBoard] cubePrefab not assigned!");
                return;
            }
            if (cubeStacks == null || cubeStacks.Length == 0)
            {
                Debug.LogWarning("[CubeBoard] No CubeStacks defined!");
                return;
            }

            float hStep = cubeSize + horizontalSpacing;
            float vStep = cubeSize + verticalSpacing;

            // Calculate how many columns fit in one row
            int cubesPerRow = CalculateCubesPerRow();

            // Center columns within the board
            float totalWidth = (cubesPerRow - 1) * hStep + cubeSize;
            float startX = transform.position.x - totalWidth * 0.5f + cubeSize * 0.5f;

            // Create all columns up front
            for (int col = 0; col < cubesPerRow; col++)
            {
                float colX = startX + col * hStep;
                Vector3 basePos = new Vector3(colX, transform.position.y, transform.position.z);
                _columns.Add(new CubeColumn(col, basePos, cubeSize, verticalSpacing));
            }

            // Fill cubes sequentially from all stacks: left-to-right, bottom-to-top
            int currentCol = 0;
            int currentRow = 0;

            foreach (var stack in cubeStacks)
            {
                if (stack == null || stack.cubeCount <= 0) continue;

                for (int i = 0; i < stack.cubeCount; i++)
                {
                    CubeColumn column = _columns[currentCol];

                    float colX = startX + currentCol * hStep;
                    Vector3 cubePos = new Vector3(colX, transform.position.y + currentRow * vStep, transform.position.z);

                    GameObject cubeGO = Instantiate(cubePrefab, cubePos, Quaternion.identity, cubeGridParent);
                    cubeGO.transform.localScale = Vector3.one * cubeScale;
                    cubeGO.name = $"Cube_{stack.color}_C{currentCol}_R{currentRow}";

                    Cube cube = cubeGO.GetComponent<Cube>();
                    if (cube == null)
                        cube = cubeGO.AddComponent<Cube>();

                    // HeightLevel = the cube's index within this column
                    int heightInColumn = column.CubeCount;
                    cube.Init(stack.color, currentCol, heightInColumn);
                    column.AddCube(cube);

                    // Advance to the next grid position
                    currentCol++;
                    if (currentCol >= cubesPerRow)
                    {
                        currentCol = 0;
                        currentRow++;
                    }
                }
            }
        }

        /// <summary>
        /// Finds a front-row cube (bottom of any column) matching the color.
        /// Skips cubes that are already reserved by another character.
        /// </summary>
        public Cube FindFrontRowCubeByColor(CubeColor targetColor)
        {
            foreach (var column in _columns)
            {
                if (column.IsEmpty) continue;

                Cube front = column.GetFrontCube();
                if (front != null && !front.IsDestroyed && !front.IsReserved
                    && front.CubeColor == targetColor)
                    return front;
            }
            return null;
        }

        /// <summary>
        /// Returns the CubeColumn that owns the given cube.
        /// </summary>
        public CubeColumn GetColumnForCube(Cube cube)
        {
            if (cube == null) return null;
            if (cube.ColumnIndex >= 0 && cube.ColumnIndex < _columns.Count)
                return _columns[cube.ColumnIndex];
            return null;
        }

        /// <summary>
        /// True when every column is empty (level cleared).
        /// </summary>
        public bool IsBoardEmpty()
        {
            foreach (var column in _columns)
                if (!column.IsEmpty) return false;
            return true;
        }

        /// <summary>
        /// Destroys all cubes and resets columns.
        /// </summary>
        public void ClearBoard()
        {
            _columns.Clear();

            if (cubeGridParent != null)
            {
                for (int i = cubeGridParent.childCount - 1; i >= 0; i--)
                {
                    if (Application.isPlaying)
                        Destroy(cubeGridParent.GetChild(i).gameObject);
                    else
                        DestroyImmediate(cubeGridParent.GetChild(i).gameObject);
                }
            }
        }

        // =====================================================================
        // EDITOR PREVIEW
        // =====================================================================

        public void RefreshEditorPreview()
        {
            if (Application.isPlaying) return;

            ClearPreview();
            if (!previewInEditor || cubePrefab == null) return;
            if (cubeStacks == null || cubeStacks.Length == 0) return;

            EnsureParent();

            float hStep = cubeSize + horizontalSpacing;
            float vStep = cubeSize + verticalSpacing;

            int cubesPerRow = CalculateCubesPerRow();
            float totalWidth = (cubesPerRow - 1) * hStep + cubeSize;
            float startX = transform.position.x - totalWidth * 0.5f + cubeSize * 0.5f;

            int currentCol = 0;
            int currentRow = 0;

            foreach (var stack in cubeStacks)
            {
                if (stack == null || stack.cubeCount <= 0) continue;

                Color color = CubeColorUtility.ToUnityColor(stack.color);

                for (int i = 0; i < stack.cubeCount; i++)
                {
                    float colX = startX + currentCol * hStep;
                    Vector3 cubePos = new Vector3(
                        colX,
                        transform.position.y + currentRow * vStep,
                        transform.position.z
                    );

                    GameObject cubeGO;

#if UNITY_EDITOR
                    cubeGO = PrefabUtility.InstantiatePrefab(cubePrefab, cubeGridParent) as GameObject;
                    if (cubeGO == null) cubeGO = Instantiate(cubePrefab, cubeGridParent);
#else
                    cubeGO = Instantiate(cubePrefab, cubeGridParent);
#endif

                    cubeGO.transform.position   = cubePos;
                    cubeGO.transform.localScale  = Vector3.one * cubeScale;
                    cubeGO.name                  = $"[PREVIEW] {stack.color}_C{currentCol}_R{currentRow}";
                    cubeGO.hideFlags             = HideFlags.DontSave;

                    TintPreviewCube(cubeGO, color);
                    _previewObjects.Add(cubeGO);

                    currentCol++;
                    if (currentCol >= cubesPerRow)
                    {
                        currentCol = 0;
                        currentRow++;
                    }
                }
            }
        }

        private void ClearPreview()
        {
            foreach (var go in _previewObjects)
            {
                if (go != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(go);
#else
                    Destroy(go);
#endif
                }
            }
            _previewObjects.Clear();
        }

        // =====================================================================
        // GIZMOS — BOARD BOUNDARY
        // =====================================================================

        private void OnDrawGizmosSelected()
        {
            DrawBoundaryGizmo(1f);
        }

        private void OnDrawGizmos()
        {
            DrawBoundaryGizmo(0.3f);
        }

        private void DrawBoundaryGizmo(float alphaMultiplier)
        {
            float vStep = cubeSize + verticalSpacing;
            int totalRows = CalculateTotalRows();
            float totalHeight = Mathf.Max((totalRows - 1) * vStep + cubeSize, cubeSize);

            Vector3 center = transform.position + Vector3.up * (totalHeight * 0.5f - cubeSize * 0.5f);
            Vector3 size   = new Vector3(boardWidth, totalHeight, boardDepth);

            // Wire
            Color wire = boundaryGizmoColor;
            wire.a *= alphaMultiplier;
            Gizmos.color = wire;
            Gizmos.DrawWireCube(center, size);

            // Fill
            Color fill = boundaryGizmoColor;
            fill.a *= 0.12f * alphaMultiplier;
            Gizmos.color = fill;
            Gizmos.DrawCube(center, size);
        }

        // =====================================================================
        // INTERNAL HELPERS
        // =====================================================================

        /// <summary>
        /// How many cubes fit in a single row based on board width, cube size, and spacing.
        /// </summary>
        private int CalculateCubesPerRow()
        {
            float hStep = cubeSize + horizontalSpacing;
            // boardWidth >= cubeSize + (N-1) * hStep  →  N = floor((boardWidth - cubeSize) / hStep) + 1
            int count = Mathf.FloorToInt((boardWidth - cubeSize) / hStep) + 1;
            return Mathf.Max(count, 1);
        }

        /// <summary>
        /// Total number of rows the board will have.
        /// </summary>
        private int CalculateTotalRows()
        {
            int totalCubes = GetTotalCubeCount();
            int perRow = CalculateCubesPerRow();
            return Mathf.Max(Mathf.CeilToInt((float)totalCubes / perRow), 1);
        }

        /// <summary>
        /// Sum of all cubeCount values across valid stacks.
        /// </summary>
        private int GetTotalCubeCount()
        {
            int total = 0;
            if (cubeStacks == null) return 0;
            foreach (var s in cubeStacks)
                if (s != null && s.cubeCount > 0) total += s.cubeCount;
            return total;
        }

        private void EnsureParent()
        {
            if (cubeGridParent == null)
            {
                Transform existing = transform.Find("CubeGrid");
                if (existing != null)
                {
                    cubeGridParent = existing;
                }
                else
                {
                    GameObject go = new GameObject("CubeGrid");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = Vector3.zero;
                    cubeGridParent = go.transform;
                }
            }
        }

        private static void TintPreviewCube(GameObject go, Color color)
        {
            var mr = go.GetComponentInChildren<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                Material mat = new Material(mr.sharedMaterial);
                mat.color = color;
                mr.material = mat;
            }

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = color;
        }
    }
}
