using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework;
using Framework.Core;



namespace Watermelon
{
    public class MapLevelBehavior : MonoBehaviour
    {
        [SerializeField] protected TMP_Text levelNumber;

        [Space]
        [SerializeField] Image backImage;
        [SerializeField] Button button;

        [SerializeField] Sprite closedBackSprite;
        [SerializeField] Sprite currentBackSprite;

        [Space]
        [SerializeField] float scaleMin = 1.0f;
        [SerializeField] float scaleMax = 1.2f;
        [SerializeField] float scaleSpeed = 2.0f;

        public int LevelIndex { get; protected set; }

        private bool isCurrent;
        private Vector3 initialScale;

        private void Awake()
        {
            initialScale = transform.localScale;

            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            if(isCurrent)
            {
                AudioController.PlaySound(AudioController.AudioClips.buttonSound);

                MenuController.OnPlayButtonClicked();
            }
        }

        public void Init(int index, int maxReachedLevelIndex)
        {
            LevelIndex = index;
            levelNumber.text = $"{index + 1}";

            if (index == maxReachedLevelIndex)
            {
                isCurrent = true;

                initialScale *= 1.2f;

                InitCurrent();
            }
            else
            {
                InitClose();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!isCurrent) return;

            float scale = Mathf.Lerp(scaleMin, scaleMax, Mathf.PingPong(Time.time * scaleSpeed, 1.0f));
            transform.localScale = initialScale * scale;
        }

        protected virtual void InitClose()
        {
            backImage.sprite = closedBackSprite;
            transform.localScale = initialScale;
        }

        protected virtual void InitCurrent()
        {
            backImage.sprite = currentBackSprite;
            transform.localScale = initialScale;
        }
    }
}
