using Base.UI.Manager;
using Framework;
using Framework.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Watermelon;

namespace Framework
{
	public class UILevelQuitPopUpGameOver : UIPage, IPopupWindow
	{
		public bool IsOpened => canvas.enabled;

		[SerializeField] TextMeshProUGUI Title;
		[SerializeField] Image backgroundImage;
		[SerializeField] Button closeSmallButton;
		[SerializeField] Button replayButton;

		private SimpleBoolCallback pageClosed;
		private SimpleBoolCallback replayPressed;

		public override void Init()
		{
			backgroundImage.AddEvent(EventTriggerType.PointerClick, (data) => ExitPopCloseButton());
			closeSmallButton.onClick.AddListener(ExitPopCloseButton);
			replayButton.onClick.AddListener(ReplayGame);
			Title.SetText(string.Format("LEVEL {0}", GetCurrentLevelAbstract.Instance.GetLevel() + 1));
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

		public void ExitPopCloseButton()
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
			UIController.HidePage<UILevelQuitPopUpGameOver>();
			pageClosed?.Invoke(false);
			pageClosed = null;
			replayPressed = null;
		}

		public void ReplayGame()
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);
			replayPressed?.Invoke(true);
			pageClosed = null;
			replayPressed = null;
		}

		public static void Show(SimpleBoolCallback onPageClosed = null, SimpleBoolCallback onReplayButtonClicked = null, bool isGameOverScreen = false)
		{
			if (LivesSystem.IsLocked || LivesSystem.InfiniteMode)
			{
				onPageClosed?.Invoke(true);
				return;
			}

			UIController.ShowPage<UILevelQuitPopUpGameOver>();
			UILevelQuitPopUpGameOver quitPopUp = UIController.GetPage<UILevelQuitPopUpGameOver>();
			quitPopUp.pageClosed += onPageClosed;
			quitPopUp.replayPressed += onReplayButtonClicked;
		}
	}
}
