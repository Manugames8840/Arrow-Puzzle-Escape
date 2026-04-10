using UnityEngine;
using System.Collections.Generic;

namespace ArrowOut
{
	/// <summary>
	/// Validates arrow movement based on game rules
	/// Single Responsibility: Movement validation logic
	/// </summary>
	public class MovementValidator : IMovementValidator
	{
		private readonly GridManager gridManager;

		public MovementValidator(GridManager manager)
		{
			gridManager = manager;
		}

		public bool CanMove(List<Vector2Int> arrowBody)
		{
			if (arrowBody == null || arrowBody.Count < 2) return false;

			// Get arrow direction
			Vector2Int head = arrowBody[0];
			Vector2Int rawDirection = head - arrowBody[1];
			Vector2Int direction = new Vector2Int(System.Math.Sign(rawDirection.x), System.Math.Sign(rawDirection.y));

			// Check ENTIRE path ahead
			Vector2Int currPos = head;
			Vector2Int checkPos = head + direction;

			while (gridManager.IsInsideGrid(checkPos))
			{
				// BLOCKER blocks movement
				if (gridManager.IsBlocked(checkPos))
					return false;

				// ANOTHER ARROW blocks movement
				if (gridManager.HasArrowAt(checkPos))
					return false;

				if (direction.x != 0 && direction.y != 0)
				{
					if (gridManager.DoesArrowCrossDiagonal(currPos, checkPos))
						return false;
				}

				// HOLE allows movement (arrow will stop there)
				if (gridManager.IsHole(checkPos))
					return true;

				// Continue checking
				currPos = checkPos;
				checkPos += direction;
			}

			// Reached grid edge - can move (will exit)
			return true;
		}

		public List<Vector2Int> GetValidMoves(Vector2Int fromPosition, List<Vector2Int> currentBody)
		{
			List<Vector2Int> validMoves = new List<Vector2Int>();

			if (currentBody == null || currentBody.Count < 2)
				return validMoves;

			// Get arrow direction
			Vector2Int head = currentBody[0];
			Vector2Int rawDirection = head - currentBody[1];
			Vector2Int direction = new Vector2Int(System.Math.Sign(rawDirection.x), System.Math.Sign(rawDirection.y));

			// Check all positions in the arrow's direction until blocked or edge
			Vector2Int currPos = head;
			Vector2Int checkPos = head + direction;

			while (gridManager.IsInsideGrid(checkPos) && !gridManager.IsBlocked(checkPos))
			{
				if (direction.x != 0 && direction.y != 0)
				{
					if (gridManager.DoesArrowCrossDiagonal(currPos, checkPos))
						break;
				}

				validMoves.Add(checkPos);
				currPos = checkPos;
				checkPos += direction;
			}

			return validMoves;
		}

		public bool IsValidPosition(Vector2Int position)
		{
			if (!gridManager.IsInsideGrid(position))
				return false;

			if (gridManager.IsBlocked(position))
				return false;

			return true;
		}
	}
}
