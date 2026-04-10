using Framework;
using Framework.Core;
using UnityEngine;

namespace ArrowOut
{
	public class GetBlockSlideLevel : GetCurrentLevelAbstract
	{
		public override void SetCurrenTLevel()
		{
			Level = SaveController.GetSaveObject<LevelSave>().DisplayLevelIndex + 1;
		}
	}
}