using UnityEngine;
using UnityEngine.UI;
using Framework;
using Framework.Core;



namespace Watermelon
{
    public class PUFreezeVisualsBehavior : MonoBehaviour
    {
        [SerializeField] Image overlayImage;
        [SerializeField] float fadeInDuration = 0.5f;
        [SerializeField] Ease.Type fadeInEasingType = Ease.Type.Linear;
        [SerializeField] float fadeOutDuration = 0.5f;
        [SerializeField] Ease.Type fadeOutEasingType = Ease.Type.Linear;

        [Space]
        [SerializeField] AudioClip freezeSound;

        private Color defaultOverlayColor;

        private TweenCase fadeTweenCase;

        private void Awake()
        {
            defaultOverlayColor = overlayImage.color;
        }

        private void OnDestroy()
        {
            fadeTweenCase.KillActive();
        }

        private void OnEnable()
        {
            PUController.Used += OnPUUsed;
        }

        private void OnDisable()
        {
            PUController.Used -= OnPUUsed;
        }

        private void OnPUUsed(PUType powerUpType)
        {
            if(powerUpType == PUType.FreezeTimer)
            {
                PUBehavior behavior = PUController.GetPowerUpBehavior(powerUpType);
                if(behavior != null)
                {
                    if(freezeSound != null)
                        AudioController.PlaySound(freezeSound);

                    EnableVisuals();

                    PUTimer timer = behavior.GetTimer();
                    timer.OnCompleted(() => DisableVisuals());
                }
            }
        }

        public void EnableVisuals()
        {
            overlayImage.gameObject.SetActive(true);
            overlayImage.color = defaultOverlayColor.SetAlpha(0);

            fadeTweenCase = overlayImage.DOColor(defaultOverlayColor, fadeInDuration).SetEasing(fadeInEasingType);
        }

        public void DisableVisuals()
        {
            fadeTweenCase = overlayImage.DOColor(defaultOverlayColor.SetAlpha(0), fadeOutDuration).SetEasing(fadeOutEasingType).OnComplete(() => 
            {   
                overlayImage.gameObject.SetActive(false); 
            });
        }
    }
}
