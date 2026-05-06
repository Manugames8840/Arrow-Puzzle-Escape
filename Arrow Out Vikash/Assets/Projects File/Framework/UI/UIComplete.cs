using System;
using Frameork;
using Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework.Core;


namespace Framework
{
	public class UIComplete : UIPage
	{
		public int LuckyWheelOpeningLevel;
		public LuckyWheelManager LuckyWheel;

		public LoadingScreenUI LoadingScreenUI;

		[BoxGroup("References", "References")]
		[SerializeField] UIFadeAnimation backgroundFade;
		[BoxGroup("References")]
		[SerializeField] RectTransform safeAreaRectTransform;

		[BoxGroup("Top Panel", "Top Panel")]
		[SerializeField] CurrencyUIPanelSimple coinsPanelUI;

		[Space]
		[BoxGroup("Content", "Content")]
		[SerializeField] TextMeshProUGUI rewardAmountText;
		[BoxGroup("Content")]
		[SerializeField] Image rewardCurrencyIconImage;
		[BoxGroup("Content")]
		[SerializeField] TextMeshProUGUI LevelText;

		[BoxGroup("Buttons", "Buttons")]
		[SerializeField] Button nextLevelButton;
		[BoxGroup("Buttons")]
		[SerializeField] Button extraRewardButton;

		private UIScaleAnimation coinsPanelScalable;

		private CanvasGroup nextLevelCanvasGroup;
		private CanvasGroup extraRewardCanvasGroup;
		private CanvasGroup luckyWheelCanvasGroup;

		private CurrencyAmount currentReward;

		public override void Init()
		{
			NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);

			nextLevelButton.onClick.AddListener(() => { OnNextLevelButtonClicked(); });
			extraRewardButton.onClick.AddListener(OnExtraRewardButtonClicked);

			nextLevelCanvasGroup = nextLevelButton.GetComponent<CanvasGroup>();
			extraRewardCanvasGroup = extraRewardButton.GetComponent<CanvasGroup>();
			luckyWheelCanvasGroup = LuckyWheel.GetComponent<CanvasGroup>();

			coinsPanelScalable = new UIScaleAnimation(coinsPanelUI);
		}

		#region Show/Hide
		public override void PlayShowAnimation()
		{
			base.PlayShowAnimation();

			LevelText.text = "Level " + (GetCurrentLevelAbstract.Instance.GetLevel() - 1).ToString();

			extraRewardCanvasGroup.alpha = 0;
			LuckyWheel.gameObject.SetActive(false);
			extraRewardCanvasGroup.interactable = false;

			extraRewardCanvasGroup.alpha = 0;
			extraRewardCanvasGroup.gameObject.SetActive(false);
			extraRewardCanvasGroup.interactable = false;

			nextLevelCanvasGroup.alpha = 0;
			nextLevelCanvasGroup.gameObject.SetActive(false);
			nextLevelCanvasGroup.interactable = false;

			currentReward = GameData.Data.DefaultReward;

			coinsPanelUI.Init(currentReward.CurrencyType);
			rewardCurrencyIconImage.sprite = currentReward.Currency.Icon;
			rewardAmountText.text = currentReward.FormattedPrice;

			coinsPanelScalable.Hide(immediately: true);

			backgroundFade.Show(duration: 0.3f);

			coinsPanelScalable.Show();

			Tween.DoFloat(0, currentReward.Amount, 0.6f, (value) =>
			{
				rewardAmountText.text = value.ToString("F0");
			}).OnComplete(() =>
			{
				AdsSettings adsSettings = Monetization.AdsSettings;
				if (adsSettings.RewardedVideoType != AdProvider.Disable && Monetization.IsActive)
				{
					if (LuckyWheelOpeningLevel <= GetCurrentLevelAbstract.Instance.GetLevel())
					{
						LuckyWheel.IsMoving = true;
						luckyWheelCanvasGroup.gameObject.SetActive(true);
						luckyWheelCanvasGroup.interactable = true;
						luckyWheelCanvasGroup.DOFade(1.0f, 3f);

						extraRewardCanvasGroup.gameObject.SetActive(true);
						extraRewardCanvasGroup.interactable = true;
						extraRewardCanvasGroup.DOFade(1.0f, 0.3f);
					}
				}

				nextLevelCanvasGroup.gameObject.SetActive(true);
				nextLevelCanvasGroup.DOFade(1.0f, 0.3f, 0.3f).OnComplete(() =>
				{
					nextLevelCanvasGroup.interactable = true;
				});
			});

			UIController.OnPageOpened(this);
		}

		public override void PlayHideAnimation()
		{
			base.PlayHideAnimation();

			UIController.OnPageClosed(this);
		}
		#endregion

		#region Buttons
		private void OnExtraRewardButtonClicked()
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);

			nextLevelButton.interactable = false;
			extraRewardButton.interactable = false;

			AdsManager.ShowRewardBasedVideo((reward) =>
			{
				if (reward)
				{
					int tempReward = currentReward.Amount;
					int newReward = tempReward * LuckyWheel.InitialReward;

					if (LuckyWheel != null)
					{
						newReward = tempReward * LuckyWheel.InitialReward;
					}
					else
					{
						newReward = tempReward * 2;
					}

					Tween.DoFloat(tempReward, tempReward + newReward, 0.6f, (value) =>
					{
						rewardAmountText.text = value.ToString("F0");
					}).OnComplete(() =>
					{
						OnNextLevelButtonClicked(LuckyWheel.InitialReward);
					});
				}
				else
				{
					OnNextLevelButtonClicked();
				}
			}, "level_complete_rewarded_ads");

			extraRewardCanvasGroup.gameObject.SetActive(false);
			extraRewardCanvasGroup.interactable = false;
		}

		private void OnNextLevelButtonClicked(int doubleAmount = 1)
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);

			nextLevelButton.interactable = false;
			extraRewardButton.interactable = false;

			MyEventArgs.GameControllerEvents.OnLevelComplete?.Dispatch(currentReward.Amount * doubleAmount, 1);
			//LivesSystem.LockLife();

			//if (Monetization.Settings.LevelRequire < GetCurrentLevelAbstract.Instance.GetLevel())
			//{
			//	CurrencyController.Add(currentReward.CurrencyType, currentReward.Amount * doubleAmount);
			//	GameController.Unload((b) =>
			//	{
			//		if (b)
			//		{
			//			GameController.LoadMainScene();
			//		}
			//		else
			//		{
			//			GameController.LoadGameGameScene();
			//		}
			//	});
			//}
			//else
			{
				FloatingCloud.SpawnCurrency(currentReward.CurrencyType.ToString(), rewardAmountText.rectTransform, coinsPanelUI.Image.rectTransform, 10, "", () =>
				{
					CurrencyController.Add(currentReward.CurrencyType, currentReward.Amount * doubleAmount);
					GameController.Unload((b) =>
					{
						//if (b)
						//{
						//	GameController.LoadMainScene();
						//}
						//else
						{
							GameController.LoadGameGameScene();
						}
					});
				});
			}
		}
		#endregion
	}
}
