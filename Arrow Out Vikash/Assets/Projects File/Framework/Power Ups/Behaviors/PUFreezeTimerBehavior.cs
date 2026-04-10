using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    public class PUFreezeTimerBehavior : PUBehavior
    {
        private const string TIMER_UNIQUE_NAME = "freezeTime";

        private PUTimer timer;

        private PUFreezeTimerSettings timerSettings;

        private TimerVisualiser timerVisualiser;

        public override void Init()
        {
            timerSettings = (PUFreezeTimerSettings)settings;

            timer = null;

            UIGame uiGame = UIController.GetPage<UIGame>();
            timerVisualiser = uiGame.GameplayTimer;
        }

        public override bool IsActive()
        {
            return true;
        }

        public override bool Activate()
        {
            IsBusy = true;

            LevelController.GameplayTimer.Pause(TIMER_UNIQUE_NAME);

            timer = new PUTimer(timerSettings.TimeFreezeDuration, () =>
            {
                timer = null;
                IsBusy = false;

                LevelController.GameplayTimer.Resume(TIMER_UNIQUE_NAME);
            });

            return true;
        }

        public override void OnTimerTick()
        {
            if (timer != null)
            {
                timerVisualiser.SetFreezeFillAmount(1 - timer.State);
            }
            else
            {
                timerVisualiser.SetFreezeFillAmount(0);
            }
        }

        public override void OnLevelLoaded()
        {
            timerVisualiser.SetFreezeFillAmount(0);
        }

        public override void OnLevelEnded()
        {
            timerVisualiser.SetFreezeFillAmount(0);
        }

        public override void ResetBehavior()
        {
            if (timer != null)
            {
                timer.Disable();
                timer = null;
            }

            IsBusy = false;
        }

        public override PUTimer GetTimer()
        {
            return timer;
        }

        public override bool ApplyToElement(IClickableObject clickableObject, Vector3 clickPosition) => false;
        public override bool IsSelectable() => false;
    }
}
