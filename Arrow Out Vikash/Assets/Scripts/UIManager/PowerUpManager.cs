using UnityEngine;
using System.Collections.Generic;

namespace ArrowOut
{
	public class PowerUpManager : MonoBehaviour
	{
		// ================= SINGLETON =================
		public static PowerUpManager Instance { get; private set; }

		// ================= POWER-UP STATE =================
		public enum PowerUpType { None, Hint, Eraser, Magic }
		public PowerUpType activePowerUp = PowerUpType.None;

		// ================= EVENTS =================
		/// <summary>
		/// Fired when the active power-up changes. UI can listen to update button highlights.
		/// </summary>
		public System.Action<PowerUpType> onPowerUpChanged;

		// ================= UNITY =================
		void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
		}

		// ================= HINT =================
		/// <summary>
		/// Finds an arrow that can reach a hole and highlights it.
		/// </summary>
		public void ActivateHint()
		{
			// Cancel any active power-up mode
			CancelActivePowerUp();

			Arrow hintArrow = FindHintArrow();
			if (hintArrow != null)
			{
				hintArrow.ShowHint();
				Debug.Log($"Hint: Arrow at head {hintArrow.Body[0]} can move!");
			}
			else
			{
				Debug.Log("Hint: No movable arrow found.");
			}
		}

		/// <summary>
		/// Finds an arrow that can move (has valid moves).
		/// Prioritizes arrows that can directly reach a hole.
		/// </summary>
		Arrow FindHintArrow()
		{
			List<Arrow> allArrows = GridManager.Instance.GetAllArrows();
			Arrow bestCandidate = null;

			foreach (var arrow in allArrows)
			{
				if (!arrow.CanMove())
					continue;
				else
				{
					bestCandidate = arrow;
					break;
				}
			}
			return bestCandidate;
		}

		// ================= ERASER =================
		/// <summary>
		/// Activates Eraser mode. Next arrow tap will remove that arrow.
		/// </summary>
		public void ActivateEraser()
		{
			//if (activePowerUp == PowerUpType.Eraser)
			//{
			//	CancelActivePowerUp();
			//	return;
			//}

			activePowerUp = PowerUpType.Eraser;
			onPowerUpChanged?.Invoke(activePowerUp);
			Debug.Log("Eraser mode activated. Tap an arrow to remove it.");
		}

		// ================= MAGIC =================
		/// <summary>
		/// Activates Magic mode. Next arrow tap will remove all arrows with the same colorId.
		/// </summary>
		public void ActivateMagic()
		{
			//if (activePowerUp == PowerUpType.Magic)
			//{
			//	CancelActivePowerUp();
			//	return;
			//}

			activePowerUp = PowerUpType.Magic;
			onPowerUpChanged?.Invoke(activePowerUp);
			Debug.Log("Magic mode activated. Tap an arrow to remove all arrows of the same color.");
		}

		// ================= TAP HANDLER =================
		/// <summary>
		/// Called by GridDot when an arrow is tapped during Eraser or Magic mode.
		/// </summary>
		public void OnArrowTapped(Arrow arrow)
		{
			if (arrow == null) return;

			switch (activePowerUp)
			{
				case PowerUpType.Eraser:
					GridManager.Instance.RemoveArrow(arrow);
					Debug.Log($"Eraser: Removed arrow at head {arrow.Body[0]}");
					break;

				case PowerUpType.Magic:
					RemoveAllArrowsOfColor(arrow.ColorId);
					Debug.Log($"Magic: Removed all arrows with colorId {arrow.ColorId}");
					break;
			}

			CancelActivePowerUp();
		}

		/// <summary>
		/// Removes all arrows that share the given colorId.
		/// </summary>
		void RemoveAllArrowsOfColor(int targetColorId)
		{
			List<Arrow> allArrows = GridManager.Instance.GetAllArrows();
			List<Arrow> toRemove = new List<Arrow>();

			foreach (var arrow in allArrows)
			{
				if (arrow.ColorId == targetColorId)
					toRemove.Add(arrow);
			}

			foreach (var arrow in toRemove)
			{
				//arrow.RemoveArrow();
			}
		}

		// ================= CANCEL =================
		/// <summary>
		/// Cancels any active power-up mode.
		/// </summary>
		public void CancelActivePowerUp()
		{
			activePowerUp = PowerUpType.None;
			onPowerUpChanged?.Invoke(activePowerUp);
		}
	}
}
