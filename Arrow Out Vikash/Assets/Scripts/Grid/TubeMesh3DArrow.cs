using System.Collections.Generic;
using Dreamteck.Splines;
using Framework.Core;
using UnityEngine;
using Watermelon;

namespace ArrowOut
{
	// <summary>
	/// 3D Tube mesh arrow body — mesh generation delegated to Dreamteck TubeGenerator + SplineComputer
	/// </summary>
	public class TubeMesh3DArrow : MonoBehaviour, IArrowRenderer, IClickableObject
	{
		// Dreamteck Splines components
		private SplineComputer splineComputer;
		private TubeGenerator tubeGenerator;

		// Click detection
		private MeshCollider meshCollider;

		// Preview line
		private LineRenderer previewLine;
		private GameObject previewObject;

		// Core references
		private ICoordinateConverter coordinateConverter;
		private Color currentColor;
		private IArrowInputHandler parentArrow;

		// Optional snake head / tail decorators
		private GameObject headInstance;
		private GameObject tailInstance;
		private TweenCase shakeTweenCase;

		public void Initialize(Transform parent, List<Vector2Int> path, Color color)
		{
			transform.SetParent(parent);
			currentColor = color;
			coordinateConverter = GridManager.Instance.GetCoordinateConverter();
			parentArrow = parent.GetComponent<IArrowInputHandler>();

			// Add SplineComputer first.
			// AddComponent<TubeGenerator>() triggers Unity's [RequireComponent] resolution,
			// which auto-adds MeshFilter + MeshRenderer. Do NOT add them manually first.
			splineComputer = gameObject.AddComponent<SplineComputer>();
			splineComputer.type = Spline.Type.CatmullRom;
			splineComputer.space = SplineComputer.Space.World;

			tubeGenerator = gameObject.AddComponent<TubeGenerator>();
			tubeGenerator.spline = splineComputer;  // subscribe TubeGenerator to the computer
			tubeGenerator.sides = 12;
			tubeGenerator.size = 0.60f;          // diameter = 2 × 0.15f radius
			tubeGenerator.capMode = TubeGenerator.CapMethod.Round;
			tubeGenerator.roundCapLatitude = 3;              // 8 latitude rings → smooth hemisphere at each end
			tubeGenerator.color = color;

			// Material — create a per-instance copy so each arrow has its own color
			MeshRenderer mr = GetComponent<MeshRenderer>(); // auto-added by TubeGenerator RequireComponent
			Material mat = GridManager.Instance.arrow3DMaterial;
			if (mat == null)
				mat = new Material(Shader.Find("Toony Colors Pro 2/Examples URP/Cat Demo/Cat/Style 1\""));
			mat = new Material(mat); // per-instance copy to avoid mutating the shared asset
			mat.SetColor("_BaseColor", color); ;
			mr.material = mat;

			// MeshCollider for OnMouseDown click detection
			meshCollider = gameObject.AddComponent<MeshCollider>();

			// Optional head / tail prefab decorators
			SpawnHeadTail();

			UpdatePath(path);
			CreatePreviewLine();
		}

		private void SpawnHeadTail()
		{
			if (!GridManager.Instance.enableSnakeHeadTail) return;

			if (GridManager.Instance.snakeHeadPrefab != null)
			{
				headInstance = Object.Instantiate(GridManager.Instance.snakeHeadPrefab);
				headInstance.transform.SetParent(transform);
			}
			if (GridManager.Instance.snakeTailPrefab != null)
			{
				tailInstance = Object.Instantiate(GridManager.Instance.snakeTailPrefab);
				tailInstance.transform.SetParent(transform);
			}
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

			previewObject.SetActive(false);
		}

		public void OnObjectClicked()
		{
			parentArrow?.MouseDown();
		}

		public bool CanBeClicked()
		{
			return true;
		}

		public void OnClickBlocked()
		{
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
			if (path == null || path.Count < 2) return;

			// Build world-space SplinePoints.
			// CatmullRom derives its own tangents from positions — SplinePoint(Vector3)
			// constructor is sufficient (sets tangent=tangent2=pos, size=1, normal=up).
			SplinePoint[] pts = new SplinePoint[path.Count];
			for (int i = 0; i < path.Count; i++)
				pts[i] = new SplinePoint(coordinateConverter.GridToWorld(path[i]));

			splineComputer.SetPoints(pts, SplineComputer.Space.World);

			// Force a synchronous mesh rebuild so the MeshCollider can be assigned
			// in the same frame. SetPoints only queues a rebuild for the next Update tick.
			splineComputer.RebuildImmediate();

			// Sync MeshCollider to the freshly generated mesh
			MeshFilter mf = GetComponent<MeshFilter>();
			if (mf != null && mf.sharedMesh != null)
				meshCollider.sharedMesh = mf.sharedMesh;

			UpdateHeadTailTransforms(path);
		}

		private void UpdateHeadTailTransforms(List<Vector2Int> path)
		{
			if (headInstance != null)
			{
				Vector3 headPos = coordinateConverter.GridToWorld(path[0]);
				headInstance.transform.position = headPos;
				if (path.Count >= 2)
				{
					Vector3 dir = (coordinateConverter.GridToWorld(path[1]) - headPos).normalized;
					if (dir != Vector3.zero)
						headInstance.transform.rotation = Quaternion.LookRotation(dir);
				}
			}

			if (tailInstance != null)
			{
				int last = path.Count - 1;
				Vector3 tailPos = coordinateConverter.GridToWorld(path[last]);
				tailInstance.transform.position = tailPos;
				if (path.Count >= 2)
				{
					Vector3 dir = (tailPos - coordinateConverter.GridToWorld(path[last - 1])).normalized;
					if (dir != Vector3.zero)
						tailInstance.transform.rotation = Quaternion.LookRotation(dir);
				}
			}
		}

		public void SetColor(Color color)
		{
			currentColor = color;
			MeshRenderer mr = GetComponent<MeshRenderer>();
			if (mr != null && mr.material != null)
				mr.material.color = color;
			// Note: setting tubeGenerator.color would trigger an extra Rebuild().
			// Material color change alone is sufficient for visual feedback.
		}

		public void Destroy()
		{
			// headInstance / tailInstance are children and would cascade anyway,
			// but explicit destroy is a safety net in case parenting hasn't settled.
			if (headInstance != null) Object.Destroy(headInstance);
			if (tailInstance != null) Object.Destroy(tailInstance);
			if (gameObject != null) Object.Destroy(gameObject);
		}

		public GameObject GetRootObject() => gameObject;

		public void ShowHint()
		{
			// Add To show Hint
		}
	}
}
