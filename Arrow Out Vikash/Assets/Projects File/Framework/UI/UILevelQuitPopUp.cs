using Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Framework.Core;

namespace Framework
{
	public class UILevelQuitPopUp : UIPage, IPopupWindow
	{
		public bool IsOpened => canvas.enabled;

		[SerializeField] TextMeshProUGUI Title;
		[SerializeField] Image backgroundImage;
		[SerializeField] Button closeSmallButton;
		[SerializeField] Button confirmButton;

		private SimpleBoolCallback pageClosed;

		public override void Init()
		{
			backgroundImage.AddEvent(EventTriggerType.PointerClick, (data) => ExitPopCloseButton());
			closeSmallButton.onClick.AddListener(ExitPopCloseButton);
			confirmButton.onClick.AddListener(ExitPopUpConfirmExitButton);
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

			UIController.HidePage<UILevelQuitPopUp>();

			pageClosed?.Invoke(false);
			pageClosed = null;
		}

		public void ExitPopUpConfirmExitButton()
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);

			UIController.HidePage<UILevelQuitPopUp>();

			pageClosed?.Invoke(true);
			pageClosed = null;
		}

		public static void Show(SimpleBoolCallback onPageClosed = null)
		{
			if (LivesSystem.IsLocked || LivesSystem.InfiniteMode)
			{
				onPageClosed?.Invoke(true);
				return;
			}

			UIController.ShowPage<UILevelQuitPopUp>();
			UILevelQuitPopUp quitPopUp = UIController.GetPage<UILevelQuitPopUp>();
			quitPopUp.pageClosed += onPageClosed;
		}
	}
}
