using UnityEngine;
using Framework;
using Framework.Core;
using ArrowOut;

namespace Watermelon
{
	public class UIGame : UIPage
	{
		[BoxGroup("References", "References")]
		[SerializeField] RectTransform safeAreaRectTransform;

		[BoxGroup("Top Panel", "Top Panel")]
		[SerializeField] CurrencyUIPanelSimple coinsPanel;

		[BoxGroup("Top Panel")]
		[SerializeField] TimerVisualiser gameplayTimer;
		public TimerVisualiser GameplayTimer => gameplayTimer;

		[BoxGroup("Top Panel")]
		[SerializeField] LevelPanel levelPanel;

		[BoxGroup("Gameplay")]
		[SerializeField] PUUIController powerUpsUIController;
		public PUUIController PowerUpsUIController => powerUpsUIController;

		[BoxGroup("Message Box")]
		[SerializeField] MessageBox messageBox;
		public MessageBox MessageBox => messageBox;

		public override void Init()
		{
			coinsPanel.Init();
			messageBox.Init();

			NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);

			levelPanel.Init(ActiveSession.Current.DisplayLevelIndex);
			gameplayTimer.Init();
		}

		public override void PlayHideAnimation()
		{
			if (LevelController.LevelRepresentation.GameConstrain == GameConditionType.Moves_and_Time || LevelController.LevelRepresentation.GameConstrain == GameConditionType.Star_and_Time || LevelController.LevelRepresentation.GameConstrain == GameConditionType.Time)
			{
				gameplayTimer.Hide();
			}

			coinsPanel.Disable();

			UIController.OnPageClosed(this);
		}

		public override void PlayShowAnimation()
		{
			if (LevelController.LevelRepresentation.GameConstrain == GameConditionType.Moves_and_Time || LevelController.LevelRepresentation.GameConstrain == GameConditionType.Star_and_Time || LevelController.LevelRepresentation.GameConstrain == GameConditionType.Time)
			{
				gameplayTimer.Show(LevelController.GameplayTimer);
			}

			coinsPanel.Activate();

			UIController.OnPageOpened(this);
		}
	}
}
