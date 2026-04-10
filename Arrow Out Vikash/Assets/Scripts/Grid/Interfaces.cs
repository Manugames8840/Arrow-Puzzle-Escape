using UnityEngine;
using System.Collections.Generic;
using System;

namespace ArrowOut
{
	/// <summary>
	/// Interface for arrow rendering strategies
	/// </summary>
	public interface IArrowRenderer
	{
		void Initialize(Transform parent, List<Vector2Int> path, Color color);
		void UpdatePath(List<Vector2Int> path);
		void SetColor(Color color);
		void ShowHint();
		void ShowPreview(List<Vector2Int> previewPath);
		void HidePreview();
		void Destroy();
		GameObject GetRootObject();
	}

	/// <summary>
	/// Interface for arrow head visualization
	/// </summary>
	public interface IArrowHead
	{
		void Initialize(Vector2Int position, Transform parent, Vector3 rotationOffset = default);
		void UpdatePosition(Vector2Int position);
		void SetColor(Color color);
		GameObject GetGameObject();
	}

	/// <summary>
	/// Interface for input handling
	/// </summary>
	public interface IArrowInputHandler
	{
		bool IsMouseOver(Vector2 mousePosition);
		void MouseDown();
		void MouseUp();
		void OnArrowCollected();
	}

	/// <summary>
	/// Interface for movement validation
	/// </summary>
	public interface IMovementValidator
	{
		bool CanMove(List<Vector2Int> arrowBody);
		List<Vector2Int> GetValidMoves(Vector2Int fromPosition, List<Vector2Int> currentBody);
		bool IsValidPosition(Vector2Int position);
	}

	/// <summary>
	/// Interface for coordinate conversion
	/// </summary>
	public interface ICoordinateConverter
	{
		Vector3 GridToWorld(Vector2Int gridPosition);
		Vector2Int WorldToGrid(Vector3 worldPosition);
		Vector2Int ScreenToGrid(Vector2 screenPosition);
	}
}
