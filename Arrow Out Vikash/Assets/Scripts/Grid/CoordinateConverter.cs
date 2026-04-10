using UnityEngine;

namespace ArrowOut
{
	/// <summary>
	/// Handles coordinate conversion between grid space and world space
	/// Single Responsibility: Coordinate transformation
	/// </summary>
	public class CoordinateConverter : ICoordinateConverter
	{
		private readonly GameRenderMode renderMode;
		private readonly Camera mainCamera;

		public CoordinateConverter(GameRenderMode mode, Camera camera)
		{
			renderMode = mode;
			mainCamera = camera;
		}

		public Vector3 GridToWorld(Vector2Int gridPosition)
		{
			switch (renderMode)
			{
				case GameRenderMode.Mesh3D:
				case GameRenderMode.LineRenderer3D:
					return new Vector3(gridPosition.x, 0, gridPosition.y);
				default:
					return new Vector3(gridPosition.x, gridPosition.y, 0);
			}
		}

		public Vector2Int WorldToGrid(Vector3 worldPosition)
		{
			switch (renderMode)
			{
				case GameRenderMode.Mesh3D:
				case GameRenderMode.LineRenderer3D:
					return new Vector2Int(
						Mathf.RoundToInt(worldPosition.x),
						Mathf.RoundToInt(worldPosition.z)
					);
				default:
					return new Vector2Int(
						Mathf.RoundToInt(worldPosition.x),
						Mathf.RoundToInt(worldPosition.y)
					);
			}
		}

		public Vector2Int ScreenToGrid(Vector2 screenPosition)
		{
			switch (renderMode)
			{
				case GameRenderMode.Mesh3D:
				case GameRenderMode.LineRenderer3D:
					return ScreenToGrid3D(screenPosition);
				default:
					return ScreenToGrid2D(screenPosition);
			}
		}

		private Vector2Int ScreenToGrid2D(Vector2 screenPosition)
		{
			Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
			return WorldToGrid(worldPos);
		}

		private Vector2Int ScreenToGrid3D(Vector2 screenPosition)
		{
			Ray ray = mainCamera.ScreenPointToRay(screenPosition);
			Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

			if (groundPlane.Raycast(ray, out float enter))
			{
				Vector3 hitPoint = ray.GetPoint(enter);
				return WorldToGrid(hitPoint);
			}

			return new Vector2Int(-1, -1);
		}
	}
}
