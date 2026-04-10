using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Framework.Core;
using Framework;
using Watermelon;

namespace ArrowOut
{
	public class LevelVariationPopup : MonoBehaviour
	{
		public static LevelVariationPopup Instance { get; private set; }

		[Header("References")]
		public GameObject popupPanel;
		public Transform variationsContainer;
		public GameObject variationButtonPrefab;
		public Button closeButton;
		public Sprite arrowHeadSprite;
		public float arrowHeadRotationOffset = 0f;

		private int currentDisplayLevelIndex;

		private void Awake()
		{
			Instance = this;
			if (popupPanel != null) popupPanel.SetActive(false);
			if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);
		}

		public void ShowVariations(int displayLevelIndex, List<LevelData> variations)
		{
			currentDisplayLevelIndex = displayLevelIndex;

			if (popupPanel != null) popupPanel.SetActive(true);

			// Clear old buttons
			foreach (Transform child in variationsContainer)
			{
				Destroy(child.gameObject);
			}

			foreach (var variation in variations)
			{
				CreateVariationButton(variation);
			}
		}

		private void CreateVariationButton(LevelData variation)
		{
			if (variationButtonPrefab == null)
			{
				Debug.LogError("Variation Button Prefab is missing!");
				return;
			}

			GameObject btnObj = Instantiate(variationButtonPrefab, variationsContainer);
			Button btn = btnObj.GetComponent<Button>();

			TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
			TextMeshProUGUI titleText = texts.Length > 0 ? texts[0] : null;
			if (titleText != null)
			{
				string statusText = "";
				if (variation.isPaid && !IsVariationUnlocked(variation))
				{
					statusText = $"<color=yellow>[Cost: {variation.coinCost} Coins]</color>";
				}
				else
				{
					statusText = "<color=green>Free / Unlocked</color>";
				}
				titleText.text = $"{variation.levelName}\n{statusText}";
			}

			// Draw mini-level visually inside the button
			DrawMiniLevel(variation, btnObj.transform);

			btn.onClick.AddListener(() => OnVariationClicked(variation));
		}

		private void DrawMiniLevel(LevelData variation, Transform parent)
		{
			// Container to hold the mini level elements
			GameObject miniLevelContainer = new GameObject("MiniLevelRender");
			miniLevelContainer.transform.SetParent(parent, false);

			RectTransform rect = miniLevelContainer.AddComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = Vector2.zero;
			rect.localScale = Vector3.one;

			// Allocate exactly 300x300 space safely inside the user's 400x400 panel bounds
			float maxDimension = Mathf.Max((float)variation.width, (float)variation.height);
			float cellSize = 300f / Mathf.Max(1f, maxDimension); 
			Vector2 centerOffset = new Vector2((variation.width - 1) * 0.5f, (variation.height - 1) * 0.5f);

			foreach (var path in variation.arrowPaths)
			{
				if (path.body == null || path.body.Count < 2) continue;

				GameObject pathContainer = new GameObject("MiniPath");
				pathContainer.transform.SetParent(miniLevelContainer.transform, false);

				// [CRITICAL FIX]: Using UI RectTransform Images instead of LineRenderer!
				// LineRenderer width uses 3D World space meters. When used inside a Canvas that scales 
				// with screen resolution (CanvasScaler), LineRenderers explode to massive sizes.
				// UI Images natively use Canvas space and prevent all exploding geometry glitches.
				for (int i = 0; i < path.body.Count - 1; i++)
				{
					Vector2 posA = path.body[i];
					Vector2 posB = path.body[i + 1];

					Vector2 localA = new Vector2(posA.x - centerOffset.x, posA.y - centerOffset.y) * cellSize;
					Vector2 localB = new Vector2(posB.x - centerOffset.x, posB.y - centerOffset.y) * cellSize;

					Vector2 midPoint = (localA + localB) * 0.5f;
					Vector2 dir = localB - localA;
					float length = dir.magnitude;
					float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

					GameObject lineObj = new GameObject($"Segment_{i}");
					lineObj.transform.SetParent(pathContainer.transform, false);

					Image img = lineObj.AddComponent<Image>();
					img.color = path.color;

					RectTransform lineRect = img.GetComponent<RectTransform>();
					lineRect.anchorMin = new Vector2(0.5f, 0.5f);
					lineRect.anchorMax = new Vector2(0.5f, 0.5f);
					lineRect.anchoredPosition = midPoint;
					// Line thickness is dynamically pegged to exactly 20% of the cell size seamlessly
					lineRect.sizeDelta = new Vector2(length + (cellSize * 0.1f), cellSize * 0.2f); 
					lineRect.localRotation = Quaternion.Euler(0, 0, angle);
				}

				// Draw Arrow Head Image
				if (arrowHeadSprite != null)
				{
					GameObject headObj = new GameObject("MiniHead");
					headObj.transform.SetParent(pathContainer.transform, false);
					
					Image headImg = headObj.AddComponent<Image>();
					headImg.sprite = arrowHeadSprite;
					headImg.color = path.color;

					Vector2 headGridPos = path.body[0];
					Vector2 headLocalPos = new Vector2(headGridPos.x - centerOffset.x, headGridPos.y - centerOffset.y) * cellSize;

					RectTransform headRect = headImg.GetComponent<RectTransform>();
					headRect.anchorMin = new Vector2(0.5f, 0.5f);
					headRect.anchorMax = new Vector2(0.5f, 0.5f);
					headRect.anchoredPosition = headLocalPos;
					
					// Head size perfectly proportioned to 45% of the cell size natively
					headRect.sizeDelta = new Vector2(cellSize * 0.45f, cellSize * 0.45f); 

					Vector2Int direction = path.body[0] - path.body[1];
					float headAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
					
					headRect.localRotation = Quaternion.Euler(0, 0, headAngle + arrowHeadRotationOffset);
				}
			}
		}

		private void OnVariationClicked(LevelData variation)
		{
			if (variation.isPaid && !IsVariationUnlocked(variation))
			{
				if (CurrencyController.HasAmount(CurrencyType.Coins, variation.coinCost))
				{
					CurrencyController.Substract(CurrencyType.Coins, variation.coinCost, "Bought Variation");
					UnlockVariation(variation);
				}
				else
				{
					Debug.Log("Not enough coins!");
					AudioController.PlaySound(AudioController.AudioClips.actionError);
					return; // Cannot afford, ignore click
				}
			}

			ClosePopup();

			// Pass explicitly chosen variation to the Level Controller via static property override
			LevelController.SpecificVariationToLoad = variation;

			DG.Tweening.DOTween.KillAll();
			MenuController.OnPlayButtonClicked();
		}

		private bool IsVariationUnlocked(LevelData variation)
		{
			// Basic save mechanism for purchases
			string key = $"Unlocked_Variation_{variation.levelNumber}_{variation.name}";
			return PlayerPrefs.GetInt(key, 0) == 1; // 0 = locked, 1 = unlocked
		}

		private void UnlockVariation(LevelData variation)
		{
			string key = $"Unlocked_Variation_{variation.levelNumber}_{variation.name}";
			PlayerPrefs.SetInt(key, 1);
			PlayerPrefs.Save();
		}

		public void ClosePopup()
		{
			if (popupPanel != null) popupPanel.SetActive(false);
		}
	}
}
