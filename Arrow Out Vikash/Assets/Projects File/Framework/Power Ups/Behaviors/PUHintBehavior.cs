using System.Collections.Generic;
using UnityEngine;
using Framework;
using Framework.Core;
using ArrowOut;
using System.Linq;

namespace Watermelon
{
	public class PUHintBehavior : PUBehavior
	{
		private const string TIMER_UNIQUE_NAME = "magnet";

		public override void Init()
		{
		}

		public override bool Activate()
		{
			IsBusy = true;

			LevelRepresentation levelRepresentation = LevelController.LevelRepresentation;

			Arrow hintArrow = levelRepresentation.FindHintArrow();

			hintArrow.ShowHint();

			if (levelRepresentation.AllArrowComplete)
			{
				GameController.GameComplete();
			}
			IsBusy = false;

			return true;
		}

		public override bool IsSelectable() => false;

		public override bool ApplyToElement(IClickableObject clickableObject, Vector3 clickPosition)
		{
			return false;
		}
	}
}
