using System.Collections.Generic;
using UnityEngine;
using Framework;
using Framework.Core;
using ArrowOut;
using System.Linq;

namespace Watermelon
{
	public class PUMagnetBehavior : PUBehavior
	{
		private const string TIMER_UNIQUE_NAME = "magnet";

		[SerializeField] MagnetVisualsBehavior magnetVisuals;
		[SerializeField] float startDelay = 0.8f;

		[Space]
		[SerializeField] float moveSpeed = 0.6f;
		[SerializeField] Ease.Type moveEasingType = Ease.Type.Linear;
		[SerializeField] Ease.Type scaleEasingType = Ease.Type.Linear;

		[SerializeField] AudioClip magnetEffectSound;

		public override void Init()
		{
			magnetVisuals.Init(this);
		}

		public override bool Activate()
		{
			IsBusy = true;

			LevelController.GameplayTimer?.Pause(TIMER_UNIQUE_NAME);

			LevelRepresentation levelRepresentation = LevelController.LevelRepresentation;

			List<Arrow> selectedArrow = levelRepresentation.ElementForMagnet(5);

			Vector3 magnetPosition = Vector3.zero;

			Vector3 finalPosition = magnetPosition + new Vector3(0, 1f, 1.2f);
			Vector3 magnetPointPosition = finalPosition + new Vector3(0, 0, 1);

			int completedElements = 0;
			float delay = startDelay;
			foreach (Arrow arrow in selectedArrow)
			{
				arrow.OnArrowCollected();

				float distance = Vector3.Distance(arrow.transform.position, finalPosition);
				float duration = distance / moveSpeed;

				completedElements++;
				if (completedElements >= selectedArrow.Count)
				{
					magnetVisuals.StopAnimation();

					IsBusy = false;

					LevelController.GameplayTimer?.Resume(TIMER_UNIQUE_NAME);

					if (levelRepresentation.AllArrowComplete)
					{
						GameController.GameComplete();
					}
				}

				if (magnetEffectSound != null)
				{
					AudioController.PlaySound(magnetEffectSound);
				}

				delay += 0.05f;
			}

			return true;
		}

		public override bool IsSelectable() => false;

		public override bool ApplyToElement(IClickableObject clickableObject, Vector3 clickPosition)
		{
			return false;
		}
	}
}
