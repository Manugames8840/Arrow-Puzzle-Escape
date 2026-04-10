using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Framework;
using Framework.Core;



namespace Watermelon
{
    public class PURewardUIBehavior : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI amountText;
        [SerializeField] string amountFormat = "x{0}";

        public void Initialise(PUPrice price)
        {
            PUBehavior behavior = PUController.GetPowerUpBehavior(price.PowerUpType);
            iconImage.sprite = behavior.Settings.Icon;
            amountText.text = string.IsNullOrEmpty(amountFormat) ? price.Amount.ToString() : string.Format(amountFormat, price.Amount);
        }
    }
}
