using UnityEngine;
using UnityEngine.UI;

namespace ArrowOut
{
	public class GameUIManager : MonoBehaviour
	{
		// ================= BUTTON SPRITES (OPTIONAL) =================
		[Header("Button Icons (Optional)")]
		public Sprite hintIcon;
		public Sprite eraserIcon;
		public Sprite magicIcon;
		public Sprite previewIcon;

		// ================= COLORS =================
		readonly Color normalButtonColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
		readonly Color activeButtonColor = new Color(0.3f, 0.6f, 0.9f, 1f);
		readonly Color hoverButtonColor = new Color(0.35f, 0.35f, 0.4f, 0.9f);

		// ================= RUNTIME REFS =================
		Button eraserButton;
		Button magicButton;
		Button previewButton;
		Image eraserBg;
		Image magicBg;
		Image previewBg;
		bool previewEnabled = true;

		// ================= INIT =================
		void Start()
		{
			CreateUI();

			// Listen for power-up state changes
			if (PowerUpManager.Instance != null)
				PowerUpManager.Instance.onPowerUpChanged += OnPowerUpChanged;
		}

		void OnDestroy()
		{
			if (PowerUpManager.Instance != null)
				PowerUpManager.Instance.onPowerUpChanged -= OnPowerUpChanged;
		}

		// ================= CREATE UI =================
		void CreateUI()
		{
			// Canvas
			GameObject canvasGO = new GameObject("GameUI_Canvas");
			canvasGO.transform.SetParent(transform);

			Canvas canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100;

			CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080, 1920);
			scaler.matchWidthOrHeight = 0.5f;

			canvasGO.AddComponent<GraphicRaycaster>();

			// Bottom panel
			GameObject panel = CreatePanel(canvasGO.transform);

			// Buttons
			CreateButton(panel.transform, "Hint", hintIcon, OnHintClicked, out _, out _);
			CreateButton(panel.transform, "Eraser", eraserIcon, OnEraserClicked, out eraserButton, out eraserBg);
			CreateButton(panel.transform, "Magic", magicIcon, OnMagicClicked, out magicButton, out magicBg);
			CreateButton(panel.transform, "Preview", previewIcon, OnPreviewClicked, out previewButton, out previewBg);
		}

		GameObject CreatePanel(Transform parent)
		{
			GameObject panel = new GameObject("BottomBar");
			panel.transform.SetParent(parent, false);

			RectTransform rt = panel.AddComponent<RectTransform>();
			rt.anchorMin = new Vector2(0.1f, 0f);
			rt.anchorMax = new Vector2(0.9f, 0f);
			rt.pivot = new Vector2(0.5f, 0f);
			rt.anchoredPosition = new Vector2(0, 20f);
			rt.sizeDelta = new Vector2(0, 80f);

			Image bg = panel.AddComponent<Image>();
			bg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

			HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 12f;
			layout.padding = new RectOffset(16, 16, 8, 8);
			layout.childAlignment = TextAnchor.MiddleCenter;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = true;

			return panel;
		}

		void CreateButton(Transform parent, string label, Sprite icon,
			UnityEngine.Events.UnityAction onClick, out Button buttonRef, out Image bgRef)
		{
			GameObject btnGO = new GameObject($"Btn_{label}");
			btnGO.transform.SetParent(parent, false);

			RectTransform rt = btnGO.AddComponent<RectTransform>();
			rt.sizeDelta = new Vector2(120, 64);

			bgRef = btnGO.AddComponent<Image>();
			bgRef.color = normalButtonColor;

			buttonRef = btnGO.AddComponent<Button>();
			buttonRef.targetGraphic = bgRef;

			// Set button colors
			ColorBlock colors = buttonRef.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
			colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
			buttonRef.colors = colors;

			buttonRef.onClick.AddListener(onClick);

			if (icon != null)
			{
				// Icon
				GameObject iconGO = new GameObject("Icon");
				iconGO.transform.SetParent(btnGO.transform, false);

				RectTransform iconRT = iconGO.AddComponent<RectTransform>();
				iconRT.anchorMin = new Vector2(0.5f, 0.55f);
				iconRT.anchorMax = new Vector2(0.5f, 0.55f);
				iconRT.sizeDelta = new Vector2(32, 32);

				Image iconImg = iconGO.AddComponent<Image>();
				iconImg.sprite = icon;
				iconImg.preserveAspect = true;

				// Label below icon
				CreateLabel(btnGO.transform, label, new Vector2(0.5f, 0.15f), 16);
			}
			else
			{
				// Text-only button
				CreateLabel(btnGO.transform, label, new Vector2(0.5f, 0.5f), 22);
			}
		}

		void CreateLabel(Transform parent, string text, Vector2 anchorPos, int fontSize)
		{
			GameObject textGO = new GameObject("Label");
			textGO.transform.SetParent(parent, false);

			RectTransform textRT = textGO.AddComponent<RectTransform>();
			textRT.anchorMin = new Vector2(0, 0);
			textRT.anchorMax = new Vector2(1, 1);
			textRT.offsetMin = Vector2.zero;
			textRT.offsetMax = Vector2.zero;

			Text txt = textGO.AddComponent<Text>();
			txt.text = text;
			txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			txt.fontSize = fontSize;
			txt.color = Color.white;
			txt.alignment = TextAnchor.MiddleCenter;
			txt.resizeTextForBestFit = true;
			txt.resizeTextMinSize = 12;
			txt.resizeTextMaxSize = fontSize;
		}

		// ================= BUTTON CALLBACKS =================
		void OnHintClicked()
		{
			if (PowerUpManager.Instance != null)
				PowerUpManager.Instance.ActivateHint();
		}

		void OnEraserClicked()
		{
			if (PowerUpManager.Instance != null)
				PowerUpManager.Instance.ActivateEraser();
		}

		void OnMagicClicked()
		{
			if (PowerUpManager.Instance != null)
				PowerUpManager.Instance.ActivateMagic();
		}

		void OnPreviewClicked()
		{
			if (GridManager.Instance == null) return;

			previewEnabled = !previewEnabled;
			GridManager.Instance.previewLinesEnabled = previewEnabled;

			// Update button visual
			if (previewBg != null)
				previewBg.color = previewEnabled ? normalButtonColor : activeButtonColor;

			// Hide all existing previews if disabled
			var allArrows = GridManager.Instance.GetAllArrows();
			foreach (var arrow in allArrows)
			{
				if (previewEnabled)
				{
					arrow.HidePreview();
				}
				else
				{
					arrow.ShowPreview();
				}
			}
		}

		// ================= POWER-UP STATE VISUAL =================
		void OnPowerUpChanged(PowerUpManager.PowerUpType powerUp)
		{
			// Reset all buttons
			if (eraserBg != null)
				eraserBg.color = normalButtonColor;
			if (magicBg != null)
				magicBg.color = normalButtonColor;

			// Highlight active
			switch (powerUp)
			{
				case PowerUpManager.PowerUpType.Eraser:
					if (eraserBg != null) eraserBg.color = activeButtonColor;
					break;
				case PowerUpManager.PowerUpType.Magic:
					if (magicBg != null) magicBg.color = activeButtonColor;
					break;
			}
		}
	}
}
