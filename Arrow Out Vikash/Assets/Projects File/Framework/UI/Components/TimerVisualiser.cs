using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework;
using Framework.Core;
using Base.UI.Manager;

namespace Watermelon
{
	public class TimerVisualiser : MonoBehaviour
	{
		[SerializeField] TMP_Text timerText;
		private GameplayTimer timer;

		[SerializeField] SlicedFilledImage fillImage;
		[SerializeField] RectTransform _timeContainer;

		public void Init()
		{
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
			if (fillImage != null) fillImage.fillAmount = t;
		}

		public void OnTimeChanged(TimeSpan timeSpan)
		{
			if (timerText != null) timerText.text = string.Format("{0:mm\\:ss}", timeSpan);
		}
	}
}
