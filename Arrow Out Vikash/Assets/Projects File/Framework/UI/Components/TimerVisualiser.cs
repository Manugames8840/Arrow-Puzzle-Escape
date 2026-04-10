using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework;
using Framework.Core;

namespace Watermelon
{
	public class TimerVisualiser : MonoBehaviour
	{
		[SerializeField] Button ReplyButton;

		[SerializeField] TMP_Text timerText;
		private GameplayTimer timer;

		[SerializeField] SlicedFilledImage fillImage;
		[SerializeField] RectTransform _timeContainer;

		public void Init()
		{
			if (ReplyButton != null)
			{
				ReplyButton.onClick.AddListener(OnReplayButtonClick);
				ReplyButton.interactable = false;
			}

			if (LevelController.GameplayTimer != null)
			{
				LevelController.GameplayTimer.OnTimerStart += OnTimerStart;
			}
			else
			{
				LevelController.GameplayMove.OnTimerStart += OnTimerStart;
			}
		}

		public void SetTimeContainer(bool isActive)
		{
			_timeContainer.gameObject.SetActive(isActive);
		}

		private void OnTimerStart()
		{
			if (LevelController.GameplayTimer != null) LevelController.GameplayTimer.OnTimerStart -= OnTimerStart;
			if (LevelController.GameplayMove != null) LevelController.GameplayMove.OnTimerStart -= OnTimerStart;

			if (ReplyButton == null)
				return;

			if (!ReplyButton.interactable) ReplyButton.interactable = true;
		}

		private void OnReplayButtonClick()
		{
			LevelController.GameplayTimer?.Pause();
			LivesSystem.LockLife();
			UILevelQuitPopUpGameOver.Show((confirmed) =>
			{
				if (confirmed)
				{
					LoadMenu();
				}
				else
				{
					LevelController.GameplayTimer?.Resume();
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
				GameController.LoadGameGameScene();
			});
		}

		public void Show(GameplayTimer timer)
		{
			this.timer = timer;

			gameObject.SetActive(true);

			timer.OnTimeSpanChanged += OnTimeChanged;

			OnTimeChanged(timer.CurrentTimeSpan);
		}

		private void OnDestroy()
		{
			if (timer != null)
				timer.OnTimeSpanChanged -= OnTimeChanged;
		}

		public void Hide()
		{
			gameObject.SetActive(false);

			if (timer != null)
				timer.OnTimeSpanChanged -= OnTimeChanged;
		}

		public void SetFreezeFillAmount(float t)
		{
			fillImage.fillAmount = t;
		}

		public void OnTimeChanged(TimeSpan timeSpan)
		{
			timerText.text = string.Format("{0:mm\\:ss}", timeSpan);
		}
	}
}
