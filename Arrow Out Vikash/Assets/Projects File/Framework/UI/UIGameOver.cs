using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework;
using Framework.Core;

namespace Framework
{
	public class UIGameOver : UIPage
	{
		[SerializeField] GameConditionType gameConditionType = GameConditionType.Time;

		[BoxGroup("References", "References")]
		[SerializeField] CanvasGroup backgroundFade;

		[SerializeField] RectTransform SpecialOffer;

		[BoxGroup("Revive")]
		[SerializeField] Button reviveButton;
		[SerializeField] Button reviveCoinButton;
		[SerializeField] Button closeButton;

		private TweenCase _floatTweenCase;
		private WatchAdsPower _watchAdsPower;

		public override void Init()
		{
			reviveButton.onClick.AddListener(OnReviveButtonClicked);
			reviveCoinButton.onClick.AddListener(OnUsingCoinReviveButtonClicked);
			closeButton.onClick.AddListener(OnCloseButtonClicked);
			_watchAdsPower = GetComponent<WatchAdsPower>();

			if (_watchAdsPower == null)
			{
				_watchAdsPower = GetComponentInChildren<WatchAdsPower>();
			}
		}

		public override void PlayShowAnimation()
		{
			base.PlayShowAnimation();
			UIController.OnPageOpened(this);
		}

		public override void PlayHideAnimation()
		{
			base.PlayHideAnimation();
			UIController.OnPageClosed(this);
		}

		private void ShowLevelFailUI()
		{
#if MODULE_MONETIZATION
			AdsManager.ShowInterstitial((result) =>
			{
				LivesSystem.TakeLife();
				backgroundFade.DOFade(1f, 0.3f);
			});
#else
			LivesSystem.TakeLife();

			backgroundFade.DOFade(1f, 0.3f);
#endif
		}

		private void OnDestroy()
		{
			_floatTweenCase.KillActive();
		}

		public static void Show(bool showRevive)
		{
			UIController.ShowPage<UIGameOver>();
		}

		#region Buttons 
		public void OnReplayButtonClicked()
		{
			GameController.Replay();

			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}

		private void OnReviveButtonClicked()
		{
			AdsManager.ShowRewardBasedVideo((reward) =>
			{
				if (reward)
				{
					if (gameConditionType == GameConditionType.Time)
					{
						GameController.Revive(GameData.Data.ReviveExtraSeconds);
					}
					else
					{
						GameController.ReviveMoves(GameData.Data.ReviveExtraMoves);
					}
					_watchAdsPower.ClaimRewardEvent?.Invoke();
				}
				else
				{
					_floatTweenCase.CompleteActive();
				}
			}, "gameover_rewarded_ads");

			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}

		private void OnUsingCoinReviveButtonClicked()
		{
			var coinAmount = CurrencyController.Get(CurrencyType.Coins);
			if (coinAmount >= 100)
			{
				CurrencyController.Substract(CurrencyType.Coins, 100);
				if (gameConditionType == GameConditionType.Time)
				{
					GameController.Revive(GameData.Data.ReviveDuration);
				}
				else
				{
					GameController.ReviveMoves(GameData.Data.ReviveMoves);
				}
				SaveController.MarkAsSaveIsRequired();
			}
			else
			{
				UIController.ShowPage<UIStore>();
			}
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}

		private void OnCloseButtonClicked()
		{
			LivesSystem.LockLife();
			UILevelQuitPopUpGameOver.Show((confirmed) =>
			{
				if (confirmed)
				{
					LoadMenu();
				}
			});
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}

		private void LoadMenu()
		{
			// Show fullscreen black overlay
			Overlay.Show(0.3f, () =>
			{
				LivesSystem.UnlockLife(true);

				// Save the current state of the game
				SaveController.Save(true);

				// Unload the current level and all the dependencies
				GameController.LoadMenu();
			});
		}
		#endregion
	}
}
