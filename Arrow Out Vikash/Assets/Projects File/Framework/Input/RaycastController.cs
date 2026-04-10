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
			else if (InputController.ClickAction.WasReleasedThisFrame() && LevelController._currentSelectedArrow != null)
			{
				LevelController.OnObjectReleased();
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
