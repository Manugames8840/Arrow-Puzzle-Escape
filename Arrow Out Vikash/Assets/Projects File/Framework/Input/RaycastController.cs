using Base.UI.Manager;
using Framework;
using Framework.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Watermelon
{
	[StaticUnload]
	public class RaycastController : MonoBehaviour
	{
		private static bool isActive;

		public static event SimpleCallback OnInputActivated;
		public static event SimpleCallback OnObjectTouched;

		// Position where the pointer was first pressed this interaction
		private static Vector2 pressStartPosition;
		// Max pixels the pointer can move and still count as a tap (not a drag)
		private const float DRAG_THRESHOLD_PX = 10f;

		public void Init()
		{
			isActive = true;
		}

		private void Update()
		{
			if (!isActive || UIController.IsPopupOpened) return;

			if (UIPanelManager.Instance.IsPanelOpened)
				return;

			if (InputController.ClickAction.WasPressedThisFrame())
			{
				// Return if clicking on any UI element
				if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
					return;

				// Record where the press started so we can detect drags on release
				pressStartPosition = InputController.MousePosition;

				Ray ray = Camera.main.ScreenPointToRay(InputController.MousePosition);
				RaycastHit hit;

				if (Physics.Raycast(ray, out hit))
				{
					IClickableObject clickableObject = hit.transform.GetComponent<IClickableObject>();
					if (clickableObject != null)
					{
						OnObjectTouched?.Invoke();

						PUBehavior selectedPU = PUController.SelectedPU;
						if (selectedPU != null)
						{
							PUController.ApplyToElement(clickableObject, hit.point);
						}
						else
						{
							PUController.DisablePowerUp(PUType.PreviewLine);
							if (clickableObject.CanBeClicked())
							{
								clickableObject.OnObjectClicked();
							}
							else
							{
								clickableObject.OnClickBlocked();
							}
						}
					}
				}
			}
			else if (InputController.ClickAction.WasReleasedThisFrame())
			{
				// Only fire OnObjectReleased if the pointer hasn't moved significantly
				// since the press — i.e. this is a tap, not a pan/drag gesture.
				float dragDistance = Vector2.Distance(InputController.MousePosition, pressStartPosition);
				if (dragDistance <= DRAG_THRESHOLD_PX)
				{
					LevelController.OnObjectReleased();
				}
				else
				{
					LevelController.OnArrowRemoved();
				}
			}
		}

		public static void Disable()
		{
			isActive = false;
		}

		public static void Enable()
		{
			isActive = true;

			OnInputActivated?.Invoke();
		}

		private static void UnloadStatic()
		{
			isActive = false;

			OnInputActivated = null;
			OnObjectTouched = null;
		}
	}
}
