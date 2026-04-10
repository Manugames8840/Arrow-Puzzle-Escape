using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework;
using Framework.Core;



namespace Watermelon
{
	public class PUUIPurchasePanel : UIPage, IPopupWindow, IPausePopup
	{
		public bool IsOpened => canvas.enabled;
		[SerializeField] RectTransform safeAreaTransform;

		[SerializeField] RectTransform panel;
		[SerializeField] Vector3 hidePos;
		[Space(5)]
		[SerializeField] Image powerUpPurchasePreview;
		[SerializeField] TMP_Text powerUpPurchaseAmountText;
		[SerializeField] TMP_Text powerVideoAmountText;
		[SerializeField] TMP_Text powerUpPurchaseDescriptionText;
		[SerializeField] TMP_Text powerUpPurchasePriceText;
		[SerializeField] Image powerUpPurchaseIcon;

		[Space(5)]
		[SerializeField] Button smallCloseButton;
		[SerializeField] Button bigCloseButton;
		[SerializeField] Button purchaseButton;
		[SerializeField] Button purchaseRVButton;

		[Space(5)]
		[SerializeField] CurrencyUIPanelSimple currencyPanel;

		private PUSettings settings;
		private Vector3 showPos;
		private WatchAdsPower _watchAdsPower;

		private void Awake()
		{
			smallCloseButton.onClick.AddListener(OnCloseButtonClicked);
			bigCloseButton.onClick.AddListener(OnCloseButtonClicked);
			purchaseButton.onClick.AddListener(OnPurchasePUButtonClicked);
			purchaseRVButton.onClick.AddListener(OnPurchaseRVButtonClicked);

			_watchAdsPower = GetComponent<WatchAdsPower>();
			if (_watchAdsPower == null)
			{
				_watchAdsPower = GetComponentInChildren<WatchAdsPower>();
			}
		}

		public override void Init()
		{
			NotchSaveArea.RegisterRectTransform(safeAreaTransform);
			showPos = panel.anchoredPosition;
		}

		public override void PlayShowAnimation()
		{
			base.PlayShowAnimation();

			if (settings == null)
			{
				Debug.LogError("PUSettings is not set for PUUIPurchasePanel!");
				return;
			}

			panel.anchoredPosition = hidePos;
			panel.DOAnchoredPosition(showPos, 0.3f, unscaledTime: true).SetEasing(Ease.Type.SineOut);

			currencyPanel.Init();

			powerUpPurchasePreview.sprite = settings.Icon;
			powerUpPurchaseDescriptionText.text = settings.Description;
			powerUpPurchasePriceText.text = settings.Price.ToString();
			powerUpPurchaseAmountText.text = string.Format("x{0}", settings.PurchaseAmount);
			powerVideoAmountText.text = $"Get x{settings.VideoPurchaseAmount}";

			Currency currency = CurrencyController.GetCurrency(settings.CurrencyType);
			powerUpPurchaseIcon.sprite = currency.Icon;

			if (settings.PurchaseOption == PUSettings.PurchaseType.Currency)
			{
				purchaseButton.gameObject.SetActive(true);
				purchaseRVButton.gameObject.SetActive(false);
			}
			else if (settings.PurchaseOption == PUSettings.PurchaseType.RewardedVideo)
			{
				purchaseButton.gameObject.SetActive(false);
				purchaseRVButton.gameObject.SetActive(true);
			}
			else
			{
				purchaseButton.gameObject.SetActive(true);
			}

			UIController.OnPageOpened(this);
		}

		public override void PlayHideAnimation()
		{
			base.PlayHideAnimation();

			panel.DOAnchoredPosition(hidePos, 0.3f, unscaledTime: true).SetEasing(Ease.Type.SineIn).OnComplete(() =>
			{
				UIController.OnPageClosed(this);
			});
		}

		public static void Show(PUSettings settings)
		{
			PUUIPurchasePanel purchasePanel = UIController.GetPage<PUUIPurchasePanel>();
			if (purchasePanel == null)
			{
				Debug.LogError("PUUIPurchasePanel is not found in the scene!");
				return;
			}

			purchasePanel.settings = settings;

			UIController.ShowPage<PUUIPurchasePanel>();
		}

		public void OnPurchasePUButtonClicked()
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);

			bool purchaseSuccessful = PUController.PurchasePowerUp(settings.Type);

			if (purchaseSuccessful)
				UIController.HidePage<PUUIPurchasePanel>();
		}

		public void OnPurchaseRVButtonClicked()
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);

#if MODULE_MONETIZATION
			AdsManager.ShowRewardBasedVideo((bool reward) =>
			{
				if (reward)
				{
					settings.Save.PurchaseUseableVideo--;
					PUController.AddPowerUp(settings.Type, settings.VideoPurchaseAmount);
					UIController.HidePage<PUUIPurchasePanel>();
					_watchAdsPower.ClaimRewardEvent?.Invoke();
				}
			}, $"PU{settings.Type}");
#else
            Debug.LogWarning("Monetization module is missing!");

            PUController.AddPowerUp(settings.Type, settings.PurchaseAmount);
            
            UIController.HidePage<PUUIPurchasePanel>();
#endif
		}

		private void OnCloseButtonClicked()
		{
			AudioController.PlaySound(AudioController.AudioClips.buttonSound);

			UIController.HidePage<PUUIPurchasePanel>();
		}
	}
}
