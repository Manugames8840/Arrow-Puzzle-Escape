using System;
using System.Collections.Generic;
using UnityEngine;
using Framework;
using Framework.Core;

namespace Watermelon
{
	[Serializable]
	[RegisterReward(typeof(PURewardView))]
	public sealed class PUReward : Reward
	{
		private const int PREVIEW_SORTING_ORDER = 0;

		[SerializeField] PUData[] powerUps;
		public PUData[] PowerUps => powerUps;

		public PUReward() { }
		public PUReward(PUData[] powerUps)
		{
			this.powerUps = powerUps;
		}

		public override void ApplyReward()
		{
			foreach (PUData powerUp in powerUps)
			{
				PUController.AddPowerUp(powerUp.PowerUpType, powerUp.Amount);
			}
		}

		public override List<IRewardPreview> GetRewardPreviews()
		{
			List<IRewardPreview> rewards = new List<IRewardPreview>();
			foreach (PUData powerUp in powerUps)
			{
				PUBehavior powerUpBehavior = PUController.GetPowerUpBehavior(powerUp.PowerUpType);
				if (powerUpBehavior != null)
				{
					PUSettings settings = powerUpBehavior.Settings;
					if (settings != null)
					{
						var type = settings.Type == PUType.FreezeTimer ?
							  RewardType.Power1 : settings.Type == PUType.Hammer ?
							  RewardType.Power2 : RewardType.Power3;

						rewards.Add(new RewardPreview(settings.Icon, $"+{powerUp.Amount}", PREVIEW_SORTING_ORDER, null, type));
					}
				}
			}

			return rewards;
		}

		[System.Serializable]
		public class PUData
		{
			[SerializeField] PUType powerUpType;
			public PUType PowerUpType => powerUpType;

			[SerializeField] int amount;
			public int Amount => amount;

			public PUData(PUType type, int amount)
			{
				this.powerUpType = type;
				this.amount = amount;
			}
		}
	}
}
