using System.Collections.Generic;
using UnityEngine;
using Framework;
using Framework.Core;

namespace Watermelon
{
	[StaticUnload]
	public class PUController : MonoBehaviour
	{
		private static PUController instance;

		[DrawReference]
		[SerializeField] PUDatabase database;

		[LineSpacer("Sounds")]
		[SerializeField] AudioClip activateSound;

		public static PUBehavior[] ActivePowerUps { get; private set; }
		public static PUUIController PowerUpsUIController { get; private set; }
		public static PUBehavior SelectedPU { get; private set; }

		private static Dictionary<PUType, PUBehavior> powerUpsLink;

		public static event PowerUpCallback Used;

		private Transform behaviorsContainer;

		public void Init()
		{
#if MODULE_POWERUPS
			instance = this;

			behaviorsContainer = new GameObject("[POWER UPS]").transform;
			behaviorsContainer.gameObject.isStatic = true;

			PUSettings[] powerUpSettings = database.PowerUps;
			ActivePowerUps = new PUBehavior[powerUpSettings.Length];
			powerUpsLink = new Dictionary<PUType, PUBehavior>();

			for (int i = 0; i < ActivePowerUps.Length; i++)
			{
				PUSettings settings = powerUpSettings[i];

				// Initialise power ups
				settings.InitialiseSave();
				settings.Init();

				// Spawn behavior object 
				GameObject powerUpBehaviorObject = Instantiate(settings.BehaviorPrefab, behaviorsContainer);
				powerUpBehaviorObject.transform.ResetLocal();

				PUBehavior powerUpBehavior = powerUpBehaviorObject.GetComponent<PUBehavior>();
				powerUpBehavior.InitialiseSettings(settings);

				ActivePowerUps[i] = powerUpBehavior;

				// Add power up to dictionary
				powerUpsLink.Add(settings.Type, ActivePowerUps[i]);

				powerUpBehaviorObject.gameObject.SetActive(false);
			}
#else
            Debug.LogError("[PU Controller]: Module Define isn't active!");
#endif
		}

		public void InitBehaviors()
		{
			foreach (PUBehavior powerUp in ActivePowerUps)
			{
				powerUp.gameObject.SetActive(true);
				powerUp.Init();
			}

			UIGame gameUI = UIController.GetPage<UIGame>();

			PowerUpsUIController = gameUI.PowerUpsUIController;
			PowerUpsUIController.Init(this);
		}

		public static void OnLevelLoaded(int levelIndex)
		{
			PowerUpsUIController?.OnLevelLoaded(levelIndex + 1);

			for (int i = 0; i < ActivePowerUps.Length; i++)
			{
				ActivePowerUps[i].OnLevelLoaded();
			}
		}

		public static void OnLevelEnded()
		{
			UnselectPowerUp();

			for (int i = 0; i < ActivePowerUps.Length; i++)
			{
				ActivePowerUps[i].OnLevelEnded();
			}

			PowerUpsUIController?.OnLevelFinished();
		}

		public static bool PurchasePowerUp(PUType powerUpType)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				PUBehavior powerUpBehavior = powerUpsLink[powerUpType];
				if (powerUpBehavior.Settings.HasEnoughCurrency())
				{
					CurrencyController.Substract(powerUpBehavior.Settings.CurrencyType, powerUpBehavior.Settings.Price, "PUPurchase");

					powerUpBehavior.Settings.Save.Amount += powerUpBehavior.Settings.PurchaseAmount;

					SaveController.MarkAsSaveIsRequired();

					PowerUpsUIController?.RedrawPanels();

					return true;
				}
				else
				{
					if (UIController.HasPage<UIStore>())
					{
						UIController.ShowPage<UIStore>();

						return false;
					}
				}
			}
			else
			{
				Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
			}

			return false;
		}

		public static void AddPowerUp(PUType powerUpType, int amount)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				PUBehavior powerUpBehavior = powerUpsLink[powerUpType];

				powerUpBehavior.Settings.Save.Amount += amount;

				SaveController.MarkAsSaveIsRequired();

				PowerUpsUIController?.RedrawPanels();
			}
			else
			{
				Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
			}
		}

		public static void SetPowerUpAmount(PUType powerUpType, int amount)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				PUBehavior powerUpBehavior = powerUpsLink[powerUpType];

				powerUpBehavior.Settings.Save.Amount = amount;

				SaveController.MarkAsSaveIsRequired();

				PowerUpsUIController?.RedrawPanels();
			}
			else
			{
				Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
			}
		}

		public static bool SelectPowerUp(PUType powerUpType)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				PUBehavior powerUpBehavior = powerUpsLink[powerUpType];

				if (SelectedPU == powerUpBehavior)
				{
					SelectedPU.OnDeselected();

					PowerUpsUIController?.OnPowerUpUnselected(SelectedPU);

					SelectedPU = null;

					return false;
				}

				if (!powerUpBehavior.IsBusy)
				{
					if (SelectedPU != null)
					{
						SelectedPU.OnDeselected();

						PowerUpsUIController?.OnPowerUpUnselected(SelectedPU);

						SelectedPU = null;
					}

					powerUpBehavior.OnSelected();

					PowerUpsUIController?.OnPowerUpSelected(powerUpBehavior);

					SelectedPU = powerUpBehavior;

					return true;
				}
			}
			else
			{
				Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
			}

			return false;
		}

		public static void ApplyToElement(IClickableObject clickableObject, Vector3 clickPosition)
		{
			if (SelectedPU != null)
			{
				if (SelectedPU.ApplyToElement(clickableObject, clickPosition))
				{
					PUSettings settings = SelectedPU.Settings;

					Haptic.Play(Haptic.HAPTIC_HARD);

					if (instance.activateSound != null)
						AudioController.PlaySound(settings.CustomAudioClip.Handle(instance.activateSound));

					settings.Save.Amount--;

					SaveController.MarkAsSaveIsRequired();

					PowerUpsUIController?.OnPowerUpUsed(SelectedPU);

					Used?.Invoke(settings.Type);
				}

				PowerUpsUIController?.OnPowerUpUnselected(SelectedPU);

				SelectedPU.OnDeselected();
				SelectedPU = null;
			}
		}

		public static void UnselectPowerUp()
		{
			if (SelectedPU != null)
			{
				SelectedPU.OnDeselected();

				PowerUpsUIController?.OnPowerUpUnselected(SelectedPU);

				SelectedPU = null;
			}
		}

		public static bool UsePowerUp(PUType powerUpType)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				PUBehavior powerUpBehavior = powerUpsLink[powerUpType];
				if (!powerUpBehavior.IsBusy)
				{
					if (powerUpBehavior.Activate())
					{
						PUSettings settings = powerUpBehavior.Settings;

						if (instance.activateSound != null)
							AudioController.PlaySound(settings.CustomAudioClip.Handle(instance.activateSound));

						settings.Save.Amount--;

						SaveController.MarkAsSaveIsRequired();

						PowerUpsUIController?.OnPowerUpUsed(powerUpBehavior);

						Used?.Invoke(powerUpType);

						return true;
					}
					else if (powerUpBehavior.IsEnableDisable())
					{
						if (powerUpBehavior.IsActivated)
						{
							PUSettings settings = powerUpBehavior.Settings;

							if (instance.activateSound != null)
								AudioController.PlaySound(settings.CustomAudioClip.Handle(instance.activateSound));

							settings.Save.Amount--;

							SaveController.MarkAsSaveIsRequired();

							PowerUpsUIController?.OnPowerUpUsed(powerUpBehavior);

							Used?.Invoke(powerUpType);
						}
					}
				}
			}
			else
			{
				Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
			}

			return false;
		}

		public static bool DisablePowerUp(PUType powerUpType)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				PUBehavior powerUpBehavior = powerUpsLink[powerUpType];
				if (powerUpBehavior.IsActivated)
				{
					powerUpBehavior.DisablePowerUp();
				}
			}
			else
			{
				Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
			}

			return false;
		}

		public static void ResetPowerUp(PUType powerUpType)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				PUBehavior powerUpBehavior = powerUpsLink[powerUpType];

				powerUpBehavior.Settings.Save.Amount = 0;

				SaveController.MarkAsSaveIsRequired();

				PowerUpsUIController?.RedrawPanels();
			}
			else
			{
				Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
			}
		}

		public static void ResetPowerUps()
		{
			foreach (PUBehavior powerUp in ActivePowerUps)
			{
				powerUp.Settings.Save.Amount = 0;

				SaveController.MarkAsSaveIsRequired();
			}

			PowerUpsUIController?.RedrawPanels();
		}

		public static int GetPUAmount(PUType powerUpType)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				return powerUpsLink[powerUpType].Settings.Save.Amount;
			}

			return 0;
		}

		public static PUBehavior GetPowerUpBehavior(PUType powerUpType)
		{
			if (powerUpsLink.ContainsKey(powerUpType))
			{
				return powerUpsLink[powerUpType];
			}

			Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));

			return null;
		}

		public static void ResetBehaviors()
		{
			for (int i = 0; i < ActivePowerUps.Length; i++)
			{
				ActivePowerUps[i].ResetBehavior();
			}
		}

		[Button("Give Test Amount")]
		public void GiveDebugAmount()
		{
			if (!Application.isPlaying)
				return;

			for (int i = 0; i < ActivePowerUps.Length; i++)
			{
				ActivePowerUps[i].Settings.Save.Amount = 999;
			}

			PowerUpsUIController?.RedrawPanels();
		}

		[Button("Reset Amount")]
		public void ResetDebugAmount()
		{
			if (!Application.isPlaying)
				return;

			for (int i = 0; i < ActivePowerUps.Length; i++)
			{
				ActivePowerUps[i].Settings.Save.Amount = 0;
			}

			PowerUpsUIController?.RedrawPanels();
		}

		private static void UnloadStatic()
		{
			Used = null;

			powerUpsLink = null;

			ActivePowerUps = null;
			PowerUpsUIController = null;
			SelectedPU = null;
		}

		public delegate void PowerUpCallback(PUType powerUpType);
	}
}

// -----------------
// PU Controller v1.2.1
// -----------------

// Changelog
// v 1.2.1
// • Added notch offset on mobile devices
// • Added Show, Hide methods to PUUIController
// v 1.2
// • Added isDirty state for UI panels (redraws automatically in Update)
// • Added visuals for busy state
// v 1.1
// • Added ResetPowerUp, ResetPowerUps, SetPowerUpAmount, GetPowerUpBehavior methods
// v 1.0
// • Basic PU logic
