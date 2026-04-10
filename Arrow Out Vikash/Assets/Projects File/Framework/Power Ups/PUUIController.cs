using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework;
using Framework.Core;



namespace Watermelon
{
	public class PUUIController : MonoBehaviour
	{
		[SerializeField] Transform containerTransform;
		[SerializeField] GameObject itemPrefab;

		[Space]
		[SerializeField] RectTransform floatingTextRectTransform;
		[SerializeField] TextMeshProUGUI floatingText;
		[SerializeField] float floatingTextDelay = 1.0f;
		[SerializeField] Ease.Type floatingTextEasing = Ease.Type.QuartOut;

		[Space]
		[SerializeField] bool showSelectionPanel;
		[SerializeField] GameObject selectionPanelObject;
		[SerializeField] Image selectionIconImage;
		[SerializeField] TextMeshProUGUI selectionDescriptionText;
		[SerializeField] Button selectionCloseButton;

		private PUController powerUpController;

		private PUUIBehavior[] uiBehaviors;
		public PUUIBehavior[] UIBehaviors => uiBehaviors;

		private float defaultFloatingTextWidth;
		private Vector2 defaultFloatingTextPosition;
		private TweenCase floatingTextTweenCase;
		private RectTransform rectTransform;

		public void Init(PUController powerUpController)
		{
			this.powerUpController = powerUpController;

			rectTransform = (RectTransform)transform;

			defaultFloatingTextPosition = floatingTextRectTransform.anchoredPosition;
			defaultFloatingTextWidth = floatingTextRectTransform.sizeDelta.x;

			// Create UI elements
			PUBehavior[] activePowerUps = PUController.ActivePowerUps;
			uiBehaviors = new PUUIBehavior[activePowerUps.Length];

			for (int i = 0; i < activePowerUps.Length; i++)
			{
				GameObject itemObject = Instantiate(itemPrefab, containerTransform);

				uiBehaviors[i] = itemObject.GetComponent<PUUIBehavior>();
				uiBehaviors[i].Initialise(activePowerUps[i]);
			}

			if (showSelectionPanel)
			{
				selectionCloseButton.onClick.AddListener(OnSelectionCloseButtonClicked);
			}
		}

		private void Update()
		{
			if (uiBehaviors != null && uiBehaviors.Length > 0)
			{
				foreach (PUUIBehavior uiBehavior in uiBehaviors)
				{
					if (uiBehavior.Behavior.IsDirty)
					{
						uiBehavior.Redraw();
					}
				}
			}
		}

		public void OnLevelLoaded(int levelNumber)
		{
			for (int i = 0; i < uiBehaviors.Length; i++)
			{
				if (uiBehaviors[i].Behavior.IsActive())
				{
					uiBehaviors[i].Activate();
					uiBehaviors[i].SetBlockState(levelNumber);
				}
				else
				{
					uiBehaviors[i].Disable();
				}

				uiBehaviors[i].OnLevelStarted(levelNumber);
			}
		}

		public void OnLevelFinished()
		{
			for (int i = 0; i < uiBehaviors.Length; i++)
			{
				if (uiBehaviors[i].Behavior.IsActive())
				{
					uiBehaviors[i].Behavior.ResetBehavior();
					uiBehaviors[i].OnLevelFinished();
					uiBehaviors[i].Redraw();
				}
			}
		}

		public void OnPowerUpUsed(PUBehavior powerUpBehavior)
		{
			RedrawPanels();

			string floatingMessageText = powerUpBehavior.GetFloatingMessage();
			if (!string.IsNullOrEmpty(floatingMessageText))
				SpawnFloatingText(floatingMessageText);
		}

		public void SpawnFloatingText(string text)
		{
			floatingTextTweenCase.KillActive();

			floatingTextRectTransform.gameObject.SetActive(true);

			floatingText.text = text;
			floatingText.ForceMeshUpdate();

			float prefferedHeight = LayoutUtility.GetPreferredHeight(floatingText.rectTransform);

			floatingTextRectTransform.sizeDelta = new Vector2(defaultFloatingTextWidth, prefferedHeight);
			floatingTextRectTransform.anchoredPosition = defaultFloatingTextPosition;

			floatingTextTweenCase = floatingTextRectTransform.DOAnchoredPosition(new Vector2(defaultFloatingTextPosition.x, defaultFloatingTextPosition.y + 50), floatingTextDelay).SetEasing(floatingTextEasing).OnComplete(() =>
			{
				floatingTextRectTransform.gameObject.SetActive(false);
			});
		}

		public void RedrawPanels()
		{
			for (int i = 0; i < uiBehaviors.Length; i++)
			{
				if (uiBehaviors[i] != null)
					uiBehaviors[i].Redraw();
			}
		}

		public void OnPowerUpUnselected(PUBehavior selectedPU)
		{
			if (showSelectionPanel)
			{
				selectionPanelObject.SetActive(false);
			}
		}

		public void OnPowerUpSelected(PUBehavior selectedPU)
		{
			if (showSelectionPanel)
			{
				PUSettings settings = selectedPU.Settings;

				selectionPanelObject.SetActive(true);
				selectionIconImage.sprite = settings.Icon;
				selectionDescriptionText.text = settings.Description;
			}
		}

		public void HidePanels()
		{
			foreach (PUUIBehavior uiBehavior in uiBehaviors)
			{
				if (uiBehavior != null)
					uiBehavior.gameObject.SetActive(false);
			}
		}

		public void HidePanel(PUType puType)
		{
			foreach (PUUIBehavior uiBehavior in uiBehaviors)
			{
				if (uiBehavior != null)
				{
					if (uiBehavior.Settings.Type == puType)
					{
						uiBehavior.gameObject.SetActive(false);

						break;
					}
				}
			}
		}

		public void ShowPanels()
		{
			foreach (PUUIBehavior uiBehavior in uiBehaviors)
			{
				if (uiBehavior != null)
				{
					if (uiBehavior.IsActive)
					{
						uiBehavior.gameObject.SetActive(true);
					}
				}
			}
		}

		public void ShowPanel(PUType puType)
		{
			foreach (PUUIBehavior uiBehavior in uiBehaviors)
			{
				if (uiBehavior != null && uiBehavior.IsActive && uiBehavior.Settings.Type == puType)
				{
					uiBehavior.gameObject.SetActive(true);

					break;
				}
			}
		}

		public PUUIBehavior GetPanel(PUType puType)
		{
			foreach (PUUIBehavior uiBehavior in uiBehaviors)
			{
				if (uiBehavior != null && uiBehavior.Settings.Type == puType)
				{
					return uiBehavior;
				}
			}

			return null;
		}

		private void OnSelectionCloseButtonClicked()
		{
			selectionPanelObject.gameObject.SetActive(false);

			PUController.UnselectPowerUp();
		}
	}
}
