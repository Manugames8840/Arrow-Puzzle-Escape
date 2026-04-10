using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    public class PUExtraTimeTutorial : BaseTutorial
    {
        private readonly PUType POWER_UP_TYPE = PUType.FreezeTimer;

        [Space]
        [SerializeField] Color textHighlightColor = Color.red;

        [Space]
        [SerializeField] string firstMessage = "Use this booster to<br>freeze the timer.";

        [Space]
        [SerializeField] Vector3 pointerOffset = new Vector3(0, 0, 0.4f);

        public override bool IsActive => saveData.isActive;
        public override bool IsFinished => saveData.isFinished;
        public override int Progress => saveData.progress;

        private TutorialBaseSave saveData;

        private UIGame gameUI;

        private PUUIBehavior powerUpPanel;
        private PUBehavior puBehavior;
        private PUSettings puSettings;

        private int storedAmount;

        public override void Init()
        {
            if (isInitialised) return;

            isInitialised = true;

            gameUI = UIController.GetPage<UIGame>();

            saveData = SaveController.GetSaveObject<TutorialBaseSave>(string.Format(ITutorial.SAVE_IDENTIFIER, tutorialId.ToString()));

            if (saveData.isFinished) return;

            puBehavior = PUController.GetPowerUpBehavior(POWER_UP_TYPE);
            puSettings = puBehavior.Settings;

            ActiveSession activeSession = ActiveSession.Current;

            if ((activeSession.MaxReachedLevelIndex + 1) == puSettings.RequiredLevel)
            {
                StartTutorial();
            }
        }

        public override void Unload()
        {
            PUController.Used -= OnPUUsed;

            isInitialised = false;
        }

        public override void StartTutorial()
        {
            RaycastController.Disable();

            powerUpPanel = gameUI.PowerUpsUIController.GetPanel(POWER_UP_TYPE);

            if(puSettings.Save.Amount == 0)
                puSettings.Save.Amount = 1;


            storedAmount = puSettings.Save.Amount;

            powerUpPanel.Redraw();

            PUController.Used += OnPUUsed;

            Tween.NextFrame(() =>
            {
                HighlightPU();
            });
        }

        private void OnPUUsed(PUType powerUpType)
        {
            if (powerUpType != POWER_UP_TYPE)
                return;

            PUController.Used -= OnPUUsed;

            gameUI.MessageBox.Disable();

            TutorialCanvasController.ResetPointer();
            TutorialCanvasController.ResetTutorialCanvas();

            RaycastController.Enable();

            FinishTutorial();
        }

        private void HighlightPU()
        {
            gameUI.MessageBox.Activate(string.Format(firstMessage, textHighlightColor.ToHex()));
            gameUI.MessageBox.ActivateTutorial();

            RectTransform messageRectTransform = gameUI.MessageBox.RectTransform;

            TutorialCanvasController.AlignToCorner(messageRectTransform, TutorialCanvasController.UIAnchorCorner.BottomCenter, new Vector2(0, 650));

            TutorialCanvasController.ActivateTutorialCanvas((RectTransform)powerUpPanel.transform, true, true);
            TutorialCanvasController.ActivatePointer(powerUpPanel.transform.position + pointerOffset, TutorialCanvasController.POINTER_SHOW_PU);
        }

        public override void FinishTutorial()
        {
            if (saveData.isFinished) return;

            puSettings.Save.Amount = storedAmount;

            saveData.isFinished = true;

            SaveController.MarkAsSaveIsRequired();
        }
    }
}
