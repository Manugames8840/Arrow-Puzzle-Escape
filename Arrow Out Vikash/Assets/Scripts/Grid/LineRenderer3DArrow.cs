using Framework.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowOut
{
	/// <summary>
	/// 3D LineRenderer arrow body
	/// </summary>
	public class LineRenderer3DArrow : MonoBehaviour, IArrowRenderer
	{
		private LineRenderer lineRenderer;
		private LineRenderer previewLine;
		private GameObject previewObject;
		private ICoordinateConverter coordinateConverter;
		private Color currentColor;
		private IArrowInputHandler parentArrow;
		private TweenCase shakeTweenCase;

		public void Initialize(Transform parent, List<Vector2Int> path, Color color)
		{
			transform.SetParent(parent);
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
			Material mat = GridManager.Instance.arrow3DMaterial;
			if (mat == null)
				mat = new Material(Shader.Find("Standard"));

			lineRenderer.material = mat;
			lineRenderer.startColor = currentColor;
			lineRenderer.endColor = currentColor;
			lineRenderer.startWidth = 0.2f;
			lineRenderer.endWidth = 0.15f;
			lineRenderer.numCapVertices = 5;
			lineRenderer.numCornerVertices = 5;
			lineRenderer.useWorldSpace = true;

			MeshCollider collider = gameObject.AddComponent<MeshCollider>();
		}

		private void CreatePreviewLine()
		{
			previewObject = new GameObject("PreviewLine");
			previewObject.transform.SetParent(transform);
			previewLine = previewObject.AddComponent<LineRenderer>();

			Material mat = new Material(Shader.Find("Standard"));
			Color previewColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.5f);
			mat.color = previewColor;

			previewLine.material = mat;
			previewLine.startColor = previewColor;
			previewLine.endColor = previewColor;
			previewLine.startWidth = 0.15f;
			previewLine.endWidth = 0.1f;
			previewLine.numCapVertices = 5;
			previewLine.numCornerVertices = 5;
			previewLine.useWorldSpace = true;

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
			if (lineRenderer == null || path == null || path.Count < 2) return;

			lineRenderer.positionCount = path.Count;

			for (int i = 0; i < path.Count; i++)
			{
				lineRenderer.SetPosition(i, coordinateConverter.GridToWorld(path[i]));
			}

			// Bake mesh for collider
			Mesh mesh = new Mesh();
			lineRenderer.BakeMesh(mesh, true);
			MeshCollider collider = GetComponent<MeshCollider>();
			if (collider != null)
				collider.sharedMesh = mesh;
		}

		public void SetColor(Color color)
		{
			currentColor = color;
			if (lineRenderer != null)
			{
				lineRenderer.startColor = color;
				lineRenderer.endColor = color;
			}
		}

		public void Destroy()
		{
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