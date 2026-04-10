using System.Collections.Generic;
using System.Linq;
using ArrowOut;
using Framework;
using Framework.Core;
using Google.Play.Common;
using UnityEngine;

namespace Watermelon
{
	public class PUPreviewBehavior : PUBehavior
	{
		public bool _previewEnabled = false;

		public override void Init()
		{
		}

		public override bool Activate()
		{
			return false;
		}

		public override bool IsEnableDisable()
		{
			IsActivated = !IsActivated;
			OnPreviewClicked();
			return true;
		}

		public override void DisablePowerUp()
		{
			base.DisablePowerUp();
			HidePreviewLine();
		}

		public override bool IsSelectable() => false;

		public override bool ApplyToElement(IClickableObject clickableObject, Vector3 clickPosition)
		{
			return false;
		}

		private void OnPreviewClicked()
		{

			// Hide all existing previews if disabled
			var allArrows = GridManager.Instance.GetAllArrows();
			foreach (var arrow in allArrows)
			{
				if (_previewEnabled)
				{
					HidePreviewLine(arrow);
				}
				else
				{
					ShowPreviewLine(arrow);
				}
			}
			_previewEnabled = !_previewEnabled;
		}

		private static void HidePreviewLine()
		{
			// Hide all existing previews if disabled
			var allArrows = GridManager.Instance.GetAllArrows();
			foreach (var arrow in allArrows)
			{
				HidePreviewLine(arrow);
			}

		}

		private static void ShowPreviewLine(Arrow arrow)
		{
			arrow.ShowPreview();
		}

		private static void HidePreviewLine(Arrow arrow)
		{
			arrow.HidePreview();
		}
	}
}
