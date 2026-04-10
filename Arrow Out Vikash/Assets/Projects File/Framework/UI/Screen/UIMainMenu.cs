using System;
using Framework;
using UnityEngine;
using UnityEngine.UI;
using Framework.Core;

namespace Watermelon
{
	public class UIMainMenu : UIPage
	{
		public readonly float BUTTONS_RIGHT_OFFSET_X = 300F;
		public LoadingScreenUI LoadingScreen;

		[BoxGroup("References", "References")]
		[SerializeField] RectTransform safeAreaRectTransform;
		[BoxGroup("References")]
		[SerializeField] RectTransform tapToPlayRect;

		[BoxGroup("Top Panel", "Top Panel")]
		[SerializeField] CurrencyUIPanelSimple coinsPanel;

		[BoxGroup("Side Buttons", "Side Buttons")]
		[SerializeField] Button noAdsButton;

		[BoxGroup("Static Map", "Static Map")]
		[SerializeField] StaticMapPanel staticMapPanel;

		private UIScaleAnimation coinsLabelScalable;

		private void OnEnable()
		{
			AdsManager.ForcedAdDisabled += ForceAdPurchased;
		}

		private void OnDisable()
		{
			AdsManager.ForcedAdDisabled -= ForceAdPurchased;
		}

		public override void Init()
		{
			coinsLabelScalable = new UIScaleAnimation(coinsPanel);
			coinsPanel.Init();

			staticMapPanel.Init();
			noAdsButton.onClick.AddListener(NoAdButton);
			NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
		}

		#region Show/Hide

		public override void PlayShowAnimation()
		{
			coinsLabelScalable.Show();
			UIController.OnPageOpened(this);
		}

		public override void PlayHideAnimation()
		{
			UIController.OnPageClosed(this);
		}

		#endregion

		#region Side Buttons
		private void ForceAdPurchased()
		{
			noAdsButton.gameObject.SetActive(false);
		}

		#endregion

		#region Buttons

		public void NoAdButton()
		{
			UIController.ShowPage<UINoAdsPopUp>();

			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}

		public void StoreButton()
		{
			UIController.ShowPage<UIStore>();

			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}

		public void AddCoinsButton()
		{
			UIController.ShowPage<UIStore>();

			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}
		#endregion
	}
}
