using Framework.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowOut
{
	/// <summary>
	/// Sprite segment-based 2D arrow body
	/// </summary>
	public class SpriteMesh2DArrow : MonoBehaviour, IArrowRenderer
	{
		private List<GameObject> segments = new List<GameObject>();
		private List<GameObject> connectors = new List<GameObject>(); // NEW: Connectors
		private GameObject previewObject;
		private LineRenderer previewLine;
		private ICoordinateConverter coordinateConverter;
		private Color currentColor;

		// Sprite assets
		private Sprite bodySprite;
		private Sprite bodySpriteAlt; // Alternate body sprite for variety
		private Sprite cornerSprite;  // Different sprite for corners
		private Sprite connectorSprite; // Small connector between segments
		private TweenCase shakeTweenCase;

		private IArrowInputHandler parentArrow;

		public void Initialize(Transform parent, List<Vector2Int> path, Color color)
		{
			transform.SetParent(parent);
			coordinateConverter = GridManager.Instance.GetCoordinateConverter();
			parentArrow = parent.GetComponent<IArrowInputHandler>();

			// Load sprites
			bodySprite = GridManager.Instance.arrowBodySprite;
			bodySpriteAlt = GridManager.Instance.arrowBodySpriteAlt; // Add this to GridManager
			cornerSprite = GridManager.Instance.arrowCornerSprite;   // Add this to GridManager
			connectorSprite = GridManager.Instance.connectorSprite;  // Add this to GridManager

			UpdatePath(path);
			CreatePreviewLine();
		}

		private void CreatePreviewLine()
		{
			previewObject = new GameObject("PreviewLine");
			previewObject.transform.SetParent(transform);
			previewLine = previewObject.AddComponent<LineRenderer>();

			previewLine.material = new Material(Shader.Find("Sprites/Default"));

			Color previewColor = Color.gray;
			previewLine.startColor = previewColor;
			previewLine.endColor = previewColor;
			previewLine.startWidth = 0.15f;
			previewLine.endWidth = 0.1f;
			previewLine.sortingOrder = 4;

			previewObject.SetActive(false);
		}

		void OnMouseDown()
		{
			parentArrow?.MouseDown();
		}

		void OnMouseUp()
		{
			parentArrow?.MouseUp();
		}

		public void OnClickBlocked()
		{
			CameraShake.Shake(0.3f, 0.2f);
			shakeTweenCase.KillActive();
			shakeTweenCase = transform.DOShake(0.05f, 0.15f);
		}

		public void ShowPreview(List<Vector2Int> previewPath)
		{
			if (previewLine == null || previewPath == null || previewPath.Count < 2) return;

			previewObject.SetActive(true);
			previewLine.positionCount = previewPath.Count;

			for (int i = 0; i < previewPath.Count; i++)
			{
				previewLine.SetPosition(i, coordinateConverter.GridToWorld(previewPath[i]));
			}
		}

		public void HidePreview()
		{
			if (previewObject != null)
			{
				previewObject.SetActive(false);
			}
		}

		public void UpdatePath(List<Vector2Int> path)
		{
			// Clear old segments
			ClearSegments();

			if (path == null || path.Count < 2) return;

			// Create body segments (skip first - that's the head/engine)
			for (int i = 1; i < path.Count; i++)
			{
				CreateBodySegment(path, i);

				// Add connector between this and next segment
				if (i < path.Count - 1)
				{
					CreateConnector(path, i);
				}
			}

			UpdateCollider(path);
		}

		private void ClearSegments()
		{
			for (int i = 0; i < segments.Count; i++)
			{
				if (segments[i] != null)
					Object.Destroy(segments[i]);
			}
			segments.Clear();

			for (int i = 0; i < connectors.Count; i++)
			{
				if (connectors[i] != null)
					Object.Destroy(connectors[i]);
			}
			connectors.Clear();
		}

		private void CreateBodySegment(List<Vector2Int> path, int index)
		{
			Vector2Int currentPos = path[index];
			Vector2Int prevPos = path[index - 1];

			GameObject segment = new GameObject($"BodySegment_{index}");
			segment.transform.SetParent(transform);
			segment.transform.position = coordinateConverter.GridToWorld(currentPos);

			SpriteRenderer sr = segment.AddComponent<SpriteRenderer>();
			sr.sortingOrder = 5;

			// Check if corner
			bool isCorner = false;
			if (index < path.Count - 1)
			{
				Vector2Int nextPos = path[index + 1];
				Vector2Int dir1 = currentPos - prevPos;
				Vector2Int dir2 = nextPos - currentPos;
				isCorner = (dir1 != dir2);
			}

			// Choose sprite based on segment type
			if (isCorner && cornerSprite != null)
			{
				// Use corner sprite for turns
				sr.sprite = cornerSprite;
				sr.sortingOrder = 6; // Above straight segments
				segment.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				// Alternate between body sprites for variety (train coaches, cargo types)
				sr.sprite = (index % 2 == 0) ? bodySprite :
							(bodySpriteAlt != null ? bodySpriteAlt : bodySprite);

				// Overlap to connect
				segment.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
			}

			// Rotate to face direction
			Vector2 direction = prevPos - currentPos;
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			segment.transform.rotation = Quaternion.Euler(0, 0, angle);

			// Add collider
			BoxCollider2D collider = segment.AddComponent<BoxCollider2D>();
			collider.size = new Vector2(0.9f, 0.9f);

			segments.Add(segment);
		}

		private void CreateConnector(List<Vector2Int> path, int index)
		{
			if (connectorSprite == null) return;

			Vector2Int currentPos = path[index];
			Vector2Int nextPos = path[index + 1];

			// Position connector between current and next
			Vector3 currentWorld = coordinateConverter.GridToWorld(currentPos);
			Vector3 nextWorld = coordinateConverter.GridToWorld(nextPos);
			Vector3 connectorPos = (currentWorld + nextWorld) / 2f;

			GameObject connector = new GameObject($"Connector_{index}");
			connector.transform.SetParent(transform);
			connector.transform.position = connectorPos;

			SpriteRenderer sr = connector.AddComponent<SpriteRenderer>();
			sr.sprite = connectorSprite;
			sr.sortingOrder = 3; // Above everything

			// Rotate to align with connection
			Vector2 direction = nextPos - currentPos;
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			connector.transform.rotation = Quaternion.Euler(0, 0, angle);

			// Scale smaller (connector is small)
			connector.transform.localScale = new Vector3(1f, 1f, 1f);

			connectors.Add(connector);
		}

		private void UpdateCollider(List<Vector2Int> path)
		{
			PolygonCollider2D polyCollider = GetComponent<PolygonCollider2D>();
			if (polyCollider == null)
				polyCollider = gameObject.AddComponent<PolygonCollider2D>();

			List<Vector2> points = new List<Vector2>();
			float width = 0.3f;

			for (int i = 0; i < path.Count - 1; i++)
			{
				Vector3 current = coordinateConverter.GridToWorld(path[i]);
				Vector3 next = coordinateConverter.GridToWorld(path[i + 1]);
				Vector2 dir = (next - current).normalized;
				Vector2 perpendicular = new Vector2(-dir.y, dir.x) * width;

				points.Add(new Vector2(current.x, current.y) + perpendicular);
			}

			for (int i = path.Count - 1; i >= 0; i--)
			{
				Vector3 current = coordinateConverter.GridToWorld(path[i]);
				Vector2 dir = Vector2.zero;
				if (i < path.Count - 1)
				{
					Vector3 next = coordinateConverter.GridToWorld(path[i + 1]);
					dir = (next - current).normalized;
				}
				Vector2 perpendicular = new Vector2(-dir.y, dir.x) * width;

				points.Add(new Vector2(current.x, current.y) - perpendicular);
			}

			polyCollider.points = points.ToArray();
		}

		public void SetColor(Color color)
		{
			currentColor = color;
			foreach (var seg in segments)
			{
				if (seg != null)
				{
					SpriteRenderer sr = seg.GetComponent<SpriteRenderer>();
					if (sr != null) sr.color = color;
				}
			}
		}

		public void Destroy()
		{
			foreach (var seg in segments)
			{
				if (seg != null) Object.Destroy(seg);
			}
			segments.Clear();

			if (gameObject != null)
				Object.Destroy(gameObject);
		}

		public GameObject GetRootObject() => gameObject;

		public void ShowHint()
		{
			throw new System.NotImplementedException();
		}
	}
}
