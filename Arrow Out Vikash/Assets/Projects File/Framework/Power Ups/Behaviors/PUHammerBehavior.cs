using UnityEngine;
using Framework;
using Framework.Core;
using ArrowOut;

namespace Watermelon
{
	public class PUHammerBehavior : PUBehavior
	{
		private const string TIMER_UNIQUE_NAME = "hammer";

		[SerializeField] ParticleSystem hitParticle;
		[SerializeField] HammerAnimationBehavior hammerAnimation;
		[SerializeField] AudioClip hammerHitSound;

		public override void Init()
		{
			hammerAnimation.Init(this);
		}

		public override bool Activate()
		{
			return true;
		}

		public override bool ApplyToElement(IClickableObject clickableObject, Vector3 clickPosition)
		{
			if (clickableObject is LineRenderer2DArrow arrow)
			{
				if (arrow != null)
				{
					IsBusy = true;

					LevelController.GameplayTimer?.Pause(TIMER_UNIQUE_NAME);

					Vector3 centerPosition = arrow.transform.position + arrow.GetLineRendererBounds().center;

					arrow.OnArrowCollected();

					bool isAllArrowCollected = LevelController.LevelRepresentation.AllArrowComplete;

					hammerAnimation.PlayHitAnimation(centerPosition + new Vector3(0, 1.1f, 0), arrow.GetComponentInParent<Arrow>(), () =>
					{
						hitParticle.gameObject.SetActive(true);
						hitParticle.transform.position = centerPosition + new Vector3(0, 1.1f, 0);

						hitParticle.Play();

						arrow.GetComponentInParent<Arrow>().CompletePathOutside();

						LevelController.GameplayTimer?.Resume(TIMER_UNIQUE_NAME);

						if (isAllArrowCollected)
						{
							GameController.GameComplete();
						}

						AudioController.PlaySound(AudioController.AudioClips.blockDestroy);

						if (hammerHitSound != null)
						{
							AudioController.PlaySound(hammerHitSound);
						}

						IsBusy = false;
					});

					return true;
				}
			}

			return false;
		}

		public override bool IsSelectable() => true;
	}
}
