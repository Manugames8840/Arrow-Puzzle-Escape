using System;
using Base.UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using Watermelon;

namespace Framework
{
	public class SettingButton : MonoBehaviour
	{
		private Button SettingBtm;
		private bool _gameStarted;

		private void Start()
		{
			SettingBtm = GetComponent<Button>();
			SettingBtm.onClick.AddListener(OpenSettingScreen);
			if (LevelController.GameplayTimer != null) LevelController.GameplayTimer.OnTimerStart += OnTimerStart;
		}

		private void OnTimerStart()
		{
			_gameStarted = true;
		}

		private void OpenSettingScreen()
		{
			UIPanelManager.Instance.Show(Panel.SETTING_SCREEN, true);
			if (_gameStarted && LevelController.GameplayTimer != null) LevelController.GameplayTimer.Pause();
		}
	}
}