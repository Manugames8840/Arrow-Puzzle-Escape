using UnityEngine;
using UnityEngine.UI;
using Watermelon;

namespace ColorBlockJam
{
	public class MapLevelBehaviorStaticExtended : MapLevelBehavior
	{
		public GameObject ParticleEffect;
		public Image Lock;
		public GameObject Green;
		public GameObject Gray;

		protected override void FixedUpdate()
		{

		}

		protected override void InitClose()
		{
			Lock.enabled = true;
			Gray?.SetActive(true);
		}

		protected override void InitCurrent()
		{
			Lock.enabled = false;
			Green?.SetActive(true);
		}
	}
}