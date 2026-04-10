using System.Collections.Generic;
using Frameork;
using Framework.Core;
using Watermelon;

namespace Framework
{
	public class RewardHandle : RewardBaseHandle
	{

		public override void SetReward(RewardStepData rewardStepData, bool isDouble = false)
		{
			UpdateReward?.Invoke(rewardStepData);
			SetReward(rewardStepData.items, isDouble);
		}

		public override void SetReward(List<RewardItem> reardItem, bool isDouble = false)
		{
			foreach (var item in reardItem)
			{
				SetReward(item, isDouble);
			}

			FlowRewards.Clear();
			foreach (var item in reardItem)
			{
				FlowRewards.Add(new FlowReward(item.type));
			}

			UIRewardsConfirmation.Display(reardItem);
		}

		public override void SetReward(RewardItem reardItem, bool isDouble = false)
		{
			GiveReward(reardItem, isDouble);
			UIRewardsConfirmation.Display(new List<RewardItem>() { reardItem });
		}

		public void GiveReward(RewardItem item, bool isDouble = false)
		{
			item.quantity = isDouble ? item.quantity * 2 : item.quantity;

			switch (item.type)
			{
				case RewardType.Coins:
					CurrencyController.Add(CurrencyType.Coins, item.quantity);
					break;
				//FreezeTimer
				case RewardType.Power1:
					PUReward.PUData[] Power1 =
					   {
							   new PUReward.PUData(PUType.Hint, item.quantity),
						   };
					PUReward reward1 = new PUReward(Power1);
					reward1.ApplyReward();
					break;
				//Hammer
				case RewardType.Power2:
					PUReward.PUData[] Power2 =
					   {
							   new PUReward.PUData(PUType.Hammer, item.quantity),
						   };
					PUReward reward2 = new PUReward(Power2);
					reward2.ApplyReward();
					break;
				//Magnet
				case RewardType.Power3:
					PUReward.PUData[] Power3 =
					   {
							   new PUReward.PUData(PUType.Magnet, item.quantity),
						   };
					PUReward reward3 = new PUReward(Power3);
					reward3.ApplyReward();
					break;
				//LivesInFinite
				case RewardType.Power4:
					LivesInfiniteModeReward livesInfiniteModeReward = new LivesInfiniteModeReward(item.quantity);
					livesInfiniteModeReward.ApplyReward();
					break;
				//LivesInAmount
				case RewardType.Power5:
					LivesReward livesReward = new LivesReward(item.quantity);
					livesReward.ApplyReward();
					break;
			}
		}
	}
}
