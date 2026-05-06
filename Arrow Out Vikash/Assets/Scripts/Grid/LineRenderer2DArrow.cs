using System.Collections.Generic;
using Framework.Core;
using UnityEngine;
using Watermelon;

namespace ArrowOut
{
	public class LineRenderer2DArrow : MonoBehaviour, IArrowRenderer, IClickableObjectRenderer
	{
		private readonly List<BoxCollider> segmentColliders = new List<BoxCollider>();
		[SerializeField] private int cornerResolution = 10;
		[SerializeField] private float cornerRadius = 0.1f;
		[SerializeField] private float colliderWidth = 1f; // Should match lineRenderer width

		private LineRenderer lineRenderer;
		private LineRenderer previewLineRenderer;
		private LineRendererBlink blinkLineRenderer;
		private ICoordinateConverter coordinateConverter;
		private Color currentColor;
		private IArrowInputHandler parentArrow;
		private TweenCase shakeTweenCase;

		// Replaces single MeshCollider
		private readonly List<GameObject> segmentColliderObjects = new List<GameObject>();

		public void Initialize(Transform parent, List<Vector2Int> path, Color color)
		{
			transform.SetParent(parent);
			blinkLineRenderer = gameObject.AddComponent<LineRendererBlink>();
			currentColor = color;
			coordinateConverter = GridManager.Instance.GetCoordinateConverter();
			parentArrow = parent.GetComponent<IArrowInputHandler>();

			lineRenderer = gameObject.AddComponent<LineRenderer>();

			ConfigureLineRenderer();
			UpdatePath(path);
			CreatePreviewLine();
		}

		private void ConfigureLineRenderer()
		{
			lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
			lineRenderer.startColor = currentColor;
			lineRenderer.endColor = currentColor;
			lineRenderer.startWidth = 0.2f;
			lineRenderer.endWidth = 0.2f;
			lineRenderer.sortingOrder = -1;
			lineRenderer.useWorldSpace = false;
		}

		private void CreatePreviewLine()
		{
			GameObject previewObj = new GameObject("PreviewLine");
			previewObj.transform.SetParent(transform);
			previewObj.transform.localPosition = Vector3.zero;

			previewLineRenderer = previewObj.AddComponent<LineRenderer>();
			previewLineRenderer.material = new Material(Shader.Find("Sprites/Default"));

			Color previewColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.5f);
			previewLineRenderer.startColor = previewColor;
			previewLineRenderer.endColor = previewColor;
			previewLineRenderer.startWidth = 0.15f;
			previewLineRenderer.endWidth = 0.15f;
			previewLineRenderer.sortingOrder = -1;
			previewLineRenderer.useWorldSpace = false;
			previewLineRenderer.enabled = false;
		}

		public void UpdatePath(List<Vector2Int> path)
		{
			if (lineRenderer == null || path == null || path.Count < 2) return;

			List<Vector3> finalPoints = BuildVisualPoints(path);

			lineRenderer.positionCount = finalPoints.Count;
			lineRenderer.SetPositions(finalPoints.ToArray());

			RebuildSegmentColliders(path);
		}

		/// <summary>
		/// Builds the bezier-curved visual points list (same logic as before).
		/// </summary>
		private List<Vector3> BuildVisualPoints(List<Vector2Int> path)
		{
			List<Vector3> finalPoints = new List<Vector3>();

			for (int i = 0; i < path.Count; i++)
			{
				Vector3 curr = coordinateConverter.GridToWorld(path[i]);

				if (i == 0 || i == path.Count - 1)
				{
					finalPoints.Add(transform.InverseTransformPoint(curr));
					continue;
				}

				Vector3 prev = coordinateConverter.GridToWorld(path[i - 1]);
				Vector3 next = coordinateConverter.GridToWorld(path[i + 1]);

				Vector3 dirA = (curr - prev).normalized;
				Vector3 dirB = (next - curr).normalized;

				if (Vector3.Dot(dirA, dirB) > 0.99f)
				{
					finalPoints.Add(transform.InverseTransformPoint(curr));
					continue;
				}

				Vector3 start = curr - dirA * cornerRadius;
				Vector3 end = curr + dirB * cornerRadius;

				for (int j = 0; j <= cornerResolution; j++)
				{
					float t = j / (float)cornerResolution;
					finalPoints.Add(transform.InverseTransformPoint(QuadraticBezier(start, curr, end, t)));
				}
			}

			// Extend tip
			Vector3 last = coordinateConverter.GridToWorld(path[path.Count - 1]);
			Vector3 beforeLast = coordinateConverter.GridToWorld(path[path.Count - 2]);
			Vector3 endDir = (last - beforeLast).normalized;
			finalPoints.Add(transform.InverseTransformPoint(last + endDir * 0.3f));

			return finalPoints;
		}


		private void RebuildSegmentColliders(List<Vector2Int> path)
		{
			// Clean up child objects and old colliders
			foreach (GameObject go in segmentColliderObjects)
				if (go != null) Destroy(go);
			segmentColliderObjects.Clear();

			foreach (BoxCollider box in segmentColliders)
				if (box != null) Destroy(box);
			segmentColliders.Clear();

			for (int i = 0; i < path.Count - 1; i++)
			{
				Vector3 worldStart = coordinateConverter.GridToWorld(path[i]);
				Vector3 worldEnd = coordinateConverter.GridToWorld(path[i + 1]);

				if (i == path.Count - 2)
					worldEnd += (worldEnd - worldStart).normalized * 0.3f;

				Vector2 localStart = transform.InverseTransformPoint(worldStart);
				Vector2 localEnd = transform.InverseTransformPoint(worldEnd);

				Vector2 localMid = (localStart + localEnd) * 0.5f;
				float length = Vector2.Distance(localStart, localEnd);
				Vector2 direction = localEnd - localStart;
				float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

				// Create child GameObject for the segment
				GameObject segmentGO = new GameObject($"SegmentCollider_{i}");
				segmentGO.transform.SetParent(transform);
				segmentGO.transform.localPosition = new Vector3(localMid.x, localMid.y, 0f);
				segmentGO.transform.localRotation = Quaternion.Euler(0, 0, angle);
				segmentGO.layer = gameObject.layer;

				// Add BoxCollider
				BoxCollider box = segmentGO.AddComponent<BoxCollider>();
				box.center = Vector3.zero;
				box.size = new Vector3(length, colliderWidth, 0.1f);

				// Forward clicks to this LineRenderer2DArrow
				ColliderClickForwarder forwarder = segmentGO.AddComponent<ColliderClickForwarder>();
				forwarder.Initialize(this);

				segmentColliderObjects.Add(segmentGO);
				segmentColliders.Add(box);
			}
		}

		private Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
		{
			return Mathf.Pow(1 - t, 2) * a +
				   2 * (1 - t) * t * b +
				   Mathf.Pow(t, 2) * c;
		}

		// ── IClickableObject ───────────────────────────────────────────────────

		public void OnObjectClicked() => parentArrow?.MouseDown();
		public bool CanBeClicked() => true;

		public void OnClickBlocked()
		{
			CameraShake.Shake(0.3f, 0.2f);
			shakeTweenCase.KillActive();
			shakeTweenCase = transform.DOShake(0.05f, 0.15f);
		}

		// ── Preview / Hint ─────────────────────────────────────────────────────

		public void ShowPreview(List<Vector2Int> previewPath)
		{
			if (previewLineRenderer == null || previewPath == null || previewPath.Count < 2) return;

			previewLineRenderer.enabled = true;
			previewLineRenderer.positionCount = previewPath.Count;

			for (int i = 0; i < previewPath.Count; i++)
			{
				Vector3 localPos = transform.InverseTransformPoint(
					coordinateConverter.GridToWorld(previewPath[i]));
				previewLineRenderer.SetPosition(i, localPos);
			}
		}

		public void HidePreview()
		{
			if (previewLineRenderer != null)
				previewLineRenderer.enabled = false;
		}

		public void ShowHint() => blinkLineRenderer.StartBlink();

		// ── Color ──────────────────────────────────────────────────────────────

		public void SetColor(Color color)
		{
			currentColor = color;

			if (lineRenderer != null)
			{
				lineRenderer.startColor = color;
				lineRenderer.endColor = color;
			}

			if (previewLineRenderer != null)
			{
				Color pc = new Color(color.r, color.g, color.b, 0.5f);
				previewLineRenderer.startColor = pc;
				previewLineRenderer.endColor = pc;
			}
		}

		// Add this method to your LineRenderer2DArrow script
		public Vector2 GetArrowCenterBound()
		{
			// Get the bounds of the line renderer
			Bounds bounds = GetLineRendererBounds();

			// Return the center of the bounds
			return bounds.center;
		}

		public Bounds GetLineRendererBounds()
		{
			if (lineRenderer == null || lineRenderer.positionCount == 0)
			{
				Debug.LogWarning("LineRenderer missing or has no positions.");
				return new Bounds(Vector3.zero, Vector3.zero);
			}

			// Correct usage: allocate first, then fill
			Vector3[] positions = new Vector3[lineRenderer.positionCount];
			lineRenderer.GetPositions(positions);

			Bounds bounds = new Bounds(positions[0], Vector3.zero);
			for (int i = 1; i < positions.Length; i++)
			{
				bounds.Encapsulate(positions[i]);
			}

			return bounds;
		}

		public void Destroy()
		{
			if (gameObject == null) return;

			shakeTweenCase.KillActive();

			foreach (GameObject go in segmentColliderObjects)
				if (go != null) Object.Destroy(go);
			segmentColliderObjects.Clear();

			Object.Destroy(gameObject);
		}

		public GameObject GetRootObject() => gameObject;

		public void OnArrowCollected() { }
	}
}