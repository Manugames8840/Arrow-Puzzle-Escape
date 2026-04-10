using Framework;
using Framework.Core;
using UnityEngine;

namespace BlockGame
{
	public class MenuManager : MonoBehaviour
	{
		public delegate void ButtonAction();

		public bool IsPlayGlobalButtonSound = true;

		public static ButtonAction HomeButtonAction;
		public static ButtonAction StoreButtonAction;
		public static ButtonAction SkinButtonAction;

		public AnimatedButton homeButton;
		public AnimatedButton storeButton;
		public AnimatedButton profileButton;

		void Start()
		{
			homeButton.SetAction(() => OpenHome());
			storeButton.SetAction(() => OpenStore());
			profileButton.SetAction(() => OpenSkin());

			HomeButtonAction += OpenHome;
			StoreButtonAction += OpenStore;
			SkinButtonAction += OpenSkin;
		}

		public virtual void OpenHome()
		{
			Debug.Log("Home opened");

			UIController.HidePage<UIStore>();
			if (IsPlayGlobalButtonSound) AudioController.PlaySound(AudioController.AudioClips.buttonSound);
			homeButton.InvokeAnimation();
		}

		public virtual void OpenStore()
		{
			Debug.Log("Store opened");
			UIController.ShowPage<UIStore>();
			if (IsPlayGlobalButtonSound) AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}

		public virtual void OpenSkin()
		{
			Debug.Log("skin opened");
			ToastMessage.Instance.Show("COMMING SOON");
			if (IsPlayGlobalButtonSound) AudioController.PlaySound(AudioController.AudioClips.buttonSound);
		}
	}
}