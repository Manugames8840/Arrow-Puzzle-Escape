using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArrowOut
{
	/// <summary>
	/// Generates cube rows above the arrow grid.
	/// Supports real-time editor preview — toggle "Preview In Editor" to see
	/// exactly how cubes will look on device without entering Play Mode.
	/// All variables are exposed so you can tweak live in the Inspector.
	/// </summary>
	[ExecuteAlways]
	public class CubeRowManager : MonoBehaviour
	{
		public static CubeRowManager Instance { get; private set; }

		// ─────────────────────────────────────────────────────────────────────
		// EDITOR PREVIEW
		// ─────────────────────────────────────────────────────────────────────

		[Header("── Editor Preview ──────────────────────────────")]
		[Tooltip("Enable to see cubes in the Editor Scene view without entering Play Mode.")]
		public bool previewInEditor = false;

		[Tooltip("How many arrows to simulate in the preview.")]
		[Range(1, 20)]
		public int previewArrowCount = 5;

		[Tooltip("How many columns wide each preview arrow is (simulates arrow body width).")]
		[Range(1, 10)]
		public int previewColumnsPerArrow = 2;

		[Tooltip("Colors cycled across preview arrows. Add more entries for more variety.")]
		public Color[] previewColors = new Color[]
		{
			new Color(1f,   0.3f, 0.3f),
			new Color(0.3f, 0.8f, 0.3f),
			new Color(0.3f, 0.5f, 1f),
			new Color(1f,   0.9f, 0.2f),
			new Color(0.9f, 0.4f, 1f),
		};

		// ─────────────────────────────────────────────────────────────────────
		// PREFAB
		// ─────────────────────────────────────────────────────────────────────

		[Header("── Cube Prefab ──────────────────────────────────")]
		[Tooltip("The cube prefab to spawn. Must have a SpriteRenderer or MeshRenderer.")]
		[SerializeField] private GameObject cubePrefab;

		// ─────────────────────────────────────────────────────────────────────
		// GRID DIMENSIONS
		// ─────────────────────────────────────────────────────────────────────

		[Header("── Grid Dimensions ──────────────────────────────")]
		[Tooltip("Rows of cubes stacked vertically per arrow group.")]
		[Range(1, 10)]
		[SerializeField] private int rowCount = 3;

		[Tooltip("World-space width/height of one cube cell.")]
		[Range(0.1f, 3f)]
		[SerializeField] private float cubeSize = 0.8f;

		[Tooltip("Gap between cubes (horizontal and vertical).")]
		[Range(0f, 1f)]
		[SerializeField] private float cubeSpacing = 0.1f;

		[Tooltip("Uniform scale applied to each cube GameObject.")]
		[Range(0.1f, 3f)]
		[SerializeField] private float cubeScale = 1f;

		// ─────────────────────────────────────────────────────────────────────
		// POSITIONING
		// ─────────────────────────────────────────────────────────────────────

		[Header("── Positioning ─────────────────────────────────")]
		[Tooltip("World Y of the bottom-most cube row. Raise to push cubes higher on screen.")]
		[SerializeField] private float cubeGridBaseY = 6f;

		[Tooltip("World X of the left edge of the cube grid.")]
		[SerializeField] private float cubeGridStartX = -4f;

		// ─────────────────────────────────────────────────────────────────────
		// INTERNAL
		// ─────────────────────────────────────────────────────────────────────

		[Header("── Internal ────────────────────────────────────")]
		[SerializeField] private Transform cubeGridParent;

		private readonly Dictionary<int, List<CubeBlock>> _cubeLookup = new();
		private float _nextColumnX = 0f;

		// preview-only objects — never saved to scene
		private readonly List<GameObject> _previewObjects = new();

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
			// Fires on every Inspector change — refresh preview automatically
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
		// PUBLIC RUNTIME API
		// =====================================================================

		/// <summary>
		/// Call from GridManager.CreateArrow() — spawns cubes above this arrow.
		/// </summary>
		public void GenerateCubesForArrow(ArrowPath arrowPath)
		{
			if (!Application.isPlaying) return;
			if (cubePrefab == null) { Debug.LogWarning("[CubeRowManager] cubePrefab not assigned!"); return; }

			int cols = Mathf.Clamp(CalculateColumns(arrowPath), 1, 10);
			var blocks = SpawnCubeBlock(arrowPath.color, arrowPath.colorId, cols, _nextColumnX);

			_cubeLookup[arrowPath.colorId] = blocks;
			_nextColumnX += cols;
		}

		/// <summary>
		/// Call from GridManager.OnArrowCompleted() — triggers pig spawn + shoot.
		/// </summary>
		public void OnArrowExited(ArrowPath arrowPath)
		{
			//PigSlotManager.Instance?.SpawnPig(arrowPath.color, arrowPath.colorId);
		}

		/// <summary>
		/// Returns the lowest surviving cube matching the given colorId.
		/// </summary>
		public CubeBlock GetLowestCubeByColorId(int colorId)
		{
			if (!_cubeLookup.TryGetValue(colorId, out var list)) return null;

			CubeBlock lowest = null;
			float lowestY = float.MaxValue;

			foreach (var cube in list)
			{
				if (cube == null || cube.IsDestroyed) continue;
				if (cube.transform.position.y < lowestY) { lowestY = cube.transform.position.y; lowest = cube; }
			}
			return lowest;
		}

		/// <summary>Called by CubeBlock on self-destroy.</summary>
		public void RemoveCubeFromTracking(CubeBlock cube)
		{
			if (_cubeLookup.TryGetValue(cube.ColorId, out var list))
				list.Remove(cube);
		}

		/// <summary>Call from GridManager.ClearGrid().</summary>
		public void ResetGrid()
		{
			if (cubeGridParent != null)
				foreach (Transform child in cubeGridParent)
					Destroy(child.gameObject);

			_cubeLookup.Clear();
			_nextColumnX = 0f;
		}

		// =====================================================================
		// EDITOR PREVIEW
		// =====================================================================

		private void RefreshEditorPreview()
		{
			if (Application.isPlaying) return;

			ClearPreview();
			if (!previewInEditor || cubePrefab == null) return;

			EnsureParent();

			float startX = 0f;

			for (int a = 0; a < previewArrowCount; a++)
			{
				Color c = (previewColors != null && previewColors.Length > 0)
					? previewColors[a % previewColors.Length]
					: Color.white;

				int cols = Mathf.Clamp(previewColumnsPerArrow, 1, 10);

				var objects = SpawnPreviewBlock(c, cols, startX);
				_previewObjects.AddRange(objects);

				startX += cols;
			}
		}

		private List<GameObject> SpawnPreviewBlock(Color color, int cols, float startX)
		{
			float step = cubeSize + cubeSpacing;
			var result = new List<GameObject>();

			for (int col = 0; col < cols; col++)
			{
				for (int row = 0; row < rowCount; row++)
				{
					float wx = cubeGridStartX + (startX + col) * step;
					float wy = cubeGridBaseY + row * step;

					GameObject cubeGO;

#if UNITY_EDITOR
					cubeGO = PrefabUtility.InstantiatePrefab(cubePrefab, cubeGridParent) as GameObject;
					if (cubeGO == null) cubeGO = Instantiate(cubePrefab, cubeGridParent);
#else
                    cubeGO = Instantiate(cubePrefab, cubeGridParent);
#endif

					cubeGO.transform.position = new Vector3(wx, wy, 0f);
					cubeGO.transform.localScale = Vector3.one * cubeScale;
					cubeGO.name = $"[PREVIEW] A{col}_R{row}";
					cubeGO.hideFlags = HideFlags.DontSave; // never saved to scene file

					TintObject(cubeGO, color);
					result.Add(cubeGO);
				}
			}

			return result;
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
		// SHARED HELPERS
		// =====================================================================

		private List<CubeBlock> SpawnCubeBlock(Color color, int colorId, int cols, float startX)
		{
			float step = cubeSize + cubeSpacing;
			var blocks = new List<CubeBlock>();

			for (int col = 0; col < cols; col++)
			{
				for (int row = 0; row < rowCount; row++)
				{
					float wx = cubeGridStartX + (startX + col) * step;
					float wy = cubeGridBaseY + row * step;

					var cubeGO = Instantiate(cubePrefab, new Vector3(wx, wy, 0f), Quaternion.identity, cubeGridParent);
					cubeGO.transform.localScale = Vector3.one * cubeScale;
					cubeGO.name = $"Cube_id{colorId}_C{col}_R{row}";

					var cube = cubeGO.GetComponent<CubeBlock>() ?? cubeGO.AddComponent<CubeBlock>();
					cube.Init(color, colorId, col, row);
					blocks.Add(cube);
				}
			}

			return blocks;
		}

		private void EnsureParent()
		{
			if (cubeGridParent == null)
				cubeGridParent = new GameObject("CubeGrid").transform;
		}

		private int CalculateColumns(ArrowPath arrowPath)
		{
			if (arrowPath.body == null || arrowPath.body.Count == 0) return 1;
			int minX = arrowPath.body[0].x, maxX = arrowPath.body[0].x;
			foreach (var p in arrowPath.body)
			{
				if (p.x < minX) minX = p.x;
				if (p.x > maxX) maxX = p.x;
			}
			return (maxX - minX) + 1;
		}

		private static void TintObject(GameObject go, Color color)
		{
			var sr = go.GetComponentInChildren<SpriteRenderer>();
			if (sr != null) { sr.color = color; return; }

			var mr = go.GetComponentInChildren<MeshRenderer>();
			if (mr != null && mr.sharedMaterial != null)
				mr.material = new Material(mr.sharedMaterial) { color = color };
		}
	}

	// =========================================================================
	// CUSTOM INSPECTOR
	// =========================================================================

#if UNITY_EDITOR
	[CustomEditor(typeof(CubeRowManager))]
	public class CubeRowManagerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var mgr = (CubeRowManager)target;

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
				if (GUILayout.Button("🔄  Refresh Preview", GUILayout.Height(36)))
				{
					var m = typeof(CubeRowManager).GetMethod("RefreshEditorPreview",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					m?.Invoke(mgr, null);
				}

				GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
				if (GUILayout.Button("🗑  Clear Preview", GUILayout.Height(36)))
				{
					var m = typeof(CubeRowManager).GetMethod("ClearPreview",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					m?.Invoke(mgr, null);
				}
				GUI.backgroundColor = Color.white;
			}

			EditorGUILayout.Space(4);

			if (mgr.previewInEditor)
			{
				EditorGUILayout.HelpBox(
					"✅ Preview is ON — cubes are visible in the Scene view.\n" +
					"Preview objects use HideFlags.DontSave and will NOT be saved to the scene.\n" +
					"⚠️ Disable before entering Play Mode to avoid duplicates.",
					MessageType.Warning
				);
			}
			else
			{
				EditorGUILayout.HelpBox(
					"Preview is OFF — no editor cubes are spawned.\n" +
					"Tick 'Preview In Editor' to see the cube layout in the Scene view.",
					MessageType.Info
				);
			}
		}
	}
#endif
}