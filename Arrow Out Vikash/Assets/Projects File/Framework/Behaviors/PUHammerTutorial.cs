using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    public class PUHammerTutorial : BaseTutorial
    {
        private readonly PUType POWER_UP_TYPE = PUType.Hammer;

        [Space]
        [SerializeField] Color textHighlightColor = Color.red;

        [Space]
        [SerializeField] string firstMessage = "Use this booster to<br><color={0}>destroy</color> a block.";
        [SerializeField] string secondMessage = "<color={0}>Tap</color> on the block to destroy it!";

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

        private bool isPUUsed;

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
            isInitialised = false;

            if(puBehavior != null)
                puBehavior.SelectStateChanged -= OnSelectStateChanged;

            PUController.Used -= OnPUUsed;
        }

        public override void StartTutorial()
        {
            RaycastController.Disable();

            isPUUsed = false;

            powerUpPanel = gameUI.PowerUpsUIController.GetPanel(POWER_UP_TYPE);

            if (puSettings.Save.Amount == 0)
                puSettings.Save.Amount = 1;

            storedAmount = puSettings.Save.Amount;

            powerUpPanel.Redraw();
            powerUpPanel.Behavior.SelectStateChanged += OnSelectStateChanged;

            Tween.NextFrame(() =>
            {
                HighlightPU();
            });
        }

        private void OnSelectStateChanged(bool value)
        {
            if (isPUUsed) return;

            if (value)
            {
                RaycastController.Enable();

                TutorialCanvasController.ResetTutorialCanvas();
                TutorialCanvasController.ResetPointer();

                gameUI.MessageBox.Activate(string.Format(secondMessage, textHighlightColor.ToHex()));

                RectTransform messageRectTransform = gameUI.MessageBox.RectTransform;

                TutorialCanvasController.AlignToCorner(messageRectTransform, TutorialCanvasController.UIAnchorCorner.TopCenter, new Vector2(0, -340));

                PUController.Used += OnPUUsed;
            }
            else
            {
                RaycastController.Disable();

                HighlightPU();

                PUController.Used -= OnPUUsed;
            }
        }

        private void OnPUUsed(PUType powerUpType)
        {
            if (powerUpType != POWER_UP_TYPE) return;

            isPUUsed = true;

            powerUpPanel.Behavior.SelectStateChanged -= OnSelectStateChanged;
            PUController.Used -= OnPUUsed;

            gameUI.MessageBox.Disable();

            TutorialCanvasController.ResetPointer();

            FinishTutorial();
        }

        private void HighlightPU()
        {
            gameUI.MessageBox.Activate(string.Format(firstMessage, textHighlightColor.ToHex()));
            gameUI.MessageBox.ActivateTutorial();

            RectTransform messageRectTransform = gameUI.MessageBox.RectTransform;

            TutorialCanvasController.AlignToCorner(messageRectTransform, TutorialCanvasController.UIAnchorCorner.BottomCenter, new Vector2(0, 755));

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
