using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;         // <-- New Input System
using UnityEngine.InputSystem.EnhancedTouch; // <-- Touch support
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using Frameork;

namespace ArrowOut
{
	public class CameraController : MonoBehaviour
	{
		// ================= SETTINGS =================
		[Header("Zoom")]
		public float zoomSpeed = 2f;
		public float zoomSmoothTime = 0.1f;
		public float minZoomMultiplier = 0.3f;

		[Header("Pan")]
		public float panSpeed = 0.5f;
		public float panSmoothTime = 0.1f;

		[Header("Boundary Margin")]
		public float boundaryMargin = 0.5f;

		[Header("UI Zoom Buttons")]
		public Button zoomInButton;
		public Button zoomOutButton;
		[Tooltip("How much each button press zooms in/out")]
		public float uiZoomStep = 1f;
		[Tooltip("How fast zoom changes while button is held")]
		public float uiZoomHoldSpeed = 3f;

		// ================= RUNTIME =================
		Camera cam;
		Bounds gridBounds;
		float initialOrthoSize;
		float targetOrthoSize;
		float zoomVelocity;

		Vector3 targetPosition;
		Vector3 panVelocity;

		bool isPanning;
		Vector3 lastPanWorldPos;
		float lastPinchDistance;

		bool isZoomInHeld;
		bool isZoomOutHeld;

		bool is3D;

		// ================= LIFECYCLE =================
		void OnEnable()
		{
			EnhancedTouchSupport.Enable();
			MyEventArgs.GameControllerEvents.OnLevelComplete.AddListener(ResetCamera);
		}

		private void ResetCamera(int arg1, int arg2)
		{
			ResetCamera();
		}

		void OnDisable()
		{
			EnhancedTouchSupport.Disable();
		}

		public void SetUp()
		{
			cam = GetComponent<Camera>();
			if (cam == null)
				cam = Camera.main;
			SetupUIButtons();
			Invoke(nameof(Setup), 0.05f);
		}

		void SetupUIButtons()
		{
			if (zoomInButton != null)
			{
				zoomInButton.onClick.AddListener(() => ZoomStep(-uiZoomStep));

				var inTrigger = zoomInButton.gameObject.AddComponent<UIButtonHoldTrigger>();
				inTrigger.OnHeld = () => isZoomInHeld = true;
				inTrigger.OnReleased = () => isZoomInHeld = false;
			}

			if (zoomOutButton != null)
			{
				zoomOutButton.onClick.AddListener(() => ZoomStep(uiZoomStep));

				var outTrigger = zoomOutButton.gameObject.AddComponent<UIButtonHoldTrigger>();
				outTrigger.OnHeld = () => isZoomOutHeld = true;
				outTrigger.OnReleased = () => isZoomOutHeld = false;
			}
		}

		void Setup()
		{
			if (GridManager.Instance == null) return;

			gridBounds = GridManager.Instance.GetGridWorldBounds();
			initialOrthoSize = GridManager.Instance.GetInitialOrthoSize();
			targetOrthoSize = cam.orthographicSize;
			targetPosition = cam.transform.position;
			is3D = GridManager.Instance.renderMode == GameRenderMode.Mesh3D;
		}

		// ================= UPDATE =================
		void Update()
		{
			if (cam == null || GridManager.Instance == null || initialOrthoSize <= 0) return;

			HandleZoom();
			HandlePan();
			ClampCamera();
		}

		// ================= ZOOM =================
		void HandleZoom()
		{
			// Mouse scroll wheel
			var mouse = Mouse.current;
			if (mouse != null)
			{
				float scrollDelta = mouse.scroll.ReadValue().y;
				if (Mathf.Abs(scrollDelta) > 0.01f)
					ApplyZoomDelta(-scrollDelta * zoomSpeed * 0.01f); // scroll values are ~120 units per notch
			}

			// Touch pinch (requires EnhancedTouch)
			var activeTouches = Touch.activeTouches;
			if (activeTouches.Count == 2)
				HandlePinchZoom(activeTouches[0], activeTouches[1]);

			// UI button hold
			if (isZoomInHeld)
				ApplyZoomDelta(-uiZoomHoldSpeed * Time.deltaTime);
			else if (isZoomOutHeld)
				ApplyZoomDelta(uiZoomHoldSpeed * Time.deltaTime);

			// Smooth zoom
			if (cam.orthographic)
				cam.orthographicSize = Mathf.SmoothDamp(
					cam.orthographicSize, targetOrthoSize, ref zoomVelocity, zoomSmoothTime);
		}

		void HandlePinchZoom(Touch t0, Touch t1)
		{
			float currentDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);

			if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
			{
				lastPinchDistance = currentDist;
			}
			else
			{
				ApplyZoomDelta((lastPinchDistance - currentDist) * zoomSpeed * 0.01f);
				lastPinchDistance = currentDist;
			}
		}

		void ApplyZoomDelta(float delta)
		{
			targetOrthoSize = ClampZoom(targetOrthoSize + delta);
		}

		void ZoomStep(float step)
		{
			targetOrthoSize = ClampZoom(targetOrthoSize + step);
		}

		float ClampZoom(float size)
		{
			return Mathf.Clamp(size, initialOrthoSize * minZoomMultiplier, initialOrthoSize);
		}

		// ================= PAN =================
		void HandlePan()
		{
			var mouse = Mouse.current;
			var activeTouches = Touch.activeTouches;
			bool touchPan = activeTouches.Count == 1;

			// --- Begin pan ---
			if (mouse != null &&
				(mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
			{
				isPanning = true;
				lastPanWorldPos = GetMouseWorldPos(mouse);
			}
			else if (touchPan && activeTouches[0].phase == TouchPhase.Began)
			{
				isPanning = true;
				lastPanWorldPos = GetTouchWorldPos(activeTouches[0]);
			}

			// --- Continue / end pan ---
			if (isPanning)
			{
				Vector3 currentWorldPos;

				if (touchPan)
				{
					currentWorldPos = GetTouchWorldPos(activeTouches[0]);
					if (activeTouches[0].phase == TouchPhase.Ended ||
						activeTouches[0].phase == TouchPhase.Canceled)
						isPanning = false;
				}
				else if (mouse != null &&
						 (mouse.rightButton.isPressed || mouse.middleButton.isPressed))
				{
					currentWorldPos = GetMouseWorldPos(mouse);
				}
				else
				{
					isPanning = false;
					goto SmoothPan;
				}

				targetPosition += lastPanWorldPos - currentWorldPos;
				lastPanWorldPos = currentWorldPos;
			}

		SmoothPan:
			cam.transform.position = Vector3.SmoothDamp(
				cam.transform.position, targetPosition, ref panVelocity, panSmoothTime);
		}

		// ================= CLAMP =================
		void ClampCamera()
		{
			if (!cam.orthographic) return;

			float vertExtent = cam.orthographicSize;
			float horizExtent = vertExtent * cam.aspect;
			Vector3 pos = targetPosition;

			float minX = gridBounds.min.x - boundaryMargin + horizExtent;
			float maxX = gridBounds.max.x + boundaryMargin - horizExtent;

			pos.x = minX > maxX
				? (gridBounds.min.x + gridBounds.max.x) * 0.5f
				: Mathf.Clamp(pos.x, minX, maxX);

			if (is3D)
			{
				float minZ = gridBounds.min.z - boundaryMargin + vertExtent;
				float maxZ = gridBounds.max.z + boundaryMargin - vertExtent;

				pos.z = minZ > maxZ
					? (gridBounds.min.z + gridBounds.max.z) * 0.5f
					: Mathf.Clamp(pos.z, minZ, maxZ);
			}
			else
			{
				float minY = gridBounds.min.y - boundaryMargin + vertExtent;
				float maxY = gridBounds.max.y + boundaryMargin - vertExtent;

				pos.y = minY > maxY
					? (gridBounds.min.y + gridBounds.max.y) * 0.5f
					: Mathf.Clamp(pos.y, minY, maxY);
			}

			targetPosition = pos;
		}

		// ================= HELPERS =================
		Vector3 GetMouseWorldPos(Mouse mouse)
		{
			Vector3 pos = mouse.position.ReadValue();
			pos.z = Mathf.Abs(cam.transform.position.z);
			return cam.ScreenToWorldPoint(pos);
		}

		Vector3 GetTouchWorldPos(Touch touch)
		{
			Vector3 pos = touch.screenPosition;
			pos.z = Mathf.Abs(cam.transform.position.z);
			return cam.ScreenToWorldPoint(pos);
		}

		// ================= PUBLIC API =================
		public void ResetCamera()
		{
			Setup();
			targetOrthoSize = initialOrthoSize;

			Vector3 center = is3D
				? new Vector3(
					(gridBounds.min.x + gridBounds.max.x) * 0.5f,
					cam.transform.position.y,
					(gridBounds.min.z + gridBounds.max.z) * 0.5f)
				: new Vector3(
					(gridBounds.min.x + gridBounds.max.x) * 0.5f,
					(gridBounds.min.y + gridBounds.max.y) * 0.5f,
					cam.transform.position.z);

			targetPosition = center;
		}
	}
}