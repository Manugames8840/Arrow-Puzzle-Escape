using Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework.Core;
using DG.Tweening;
using ArrowOut;

namespace Watermelon
{
	public class StaticMapPanel : MonoBehaviour
	{
		private const string BUTTON_TEXT = "LEVEL {0}";
		[SerializeField] RectTransform[] mapElementPositions;
		[Space]
		[SerializeField] Button playButton;
		[SerializeField] TextMeshProUGUI playButtonText;
		[SerializeField] TextMeshProUGUI levelTypeText;

		public void Init()
		{
			playButton.onClick.AddListener(OnPlayButtonClicked);

			GameData gameData = GameData.Data;
			LevelDatabase levelDatabase = gameData.LevelDatabase;
			ActiveSession session = ActiveSession.Current;

			int currentIndex = session.Save.DisplayLevelIndex;
			int levelIndex = currentIndex;

			for (int i = 0; i < mapElementPositions.Length; i++)
			{
				LevelData levelData = levelDatabase.GetLevel(levelIndex);

				if (levelData == null)
					levelData = levelDatabase.GetRandomLevel();

				MapLevelData mapData = levelDatabase.GetMapLevelData(levelData.Type);
				RectTransform targetTransform = mapElementPositions[i];
				GameObject mapObject = Instantiate(mapData.Prefab, targetTransform);
				mapObject.transform.localPosition = Vector3.zero;
				MapLevelBehavior mapLevelBehavior = mapObject.GetComponent<MapLevelBehavior>();
				mapLevelBehavior.Init(levelIndex, currentIndex);
				levelIndex++;
			}

			LevelData currentLevelData = levelDatabase.GetLevel(session.Save.DisplayLevelIndex);

			if (currentLevelData == null)
				currentLevelData = levelDatabase.GetRandomLevel();

			MapLevelData currentMapData = levelDatabase.GetMapLevelData(currentLevelData.Type);
			playButtonText.text = string.Format(BUTTON_TEXT, session.Save.DisplayLevelIndex + 1);
			levelTypeText.text = currentLevelData.Type.ToString();
		}
		private void OnPlayButtonClicked()
		{
			//AudioController.PlaySound(AudioController.AudioClips.buttonSound);

			//GameData gameData = GameData.Data;
			//LevelDatabase levelDatabase = gameData.LevelDatabase;
			//ActiveSession session = ActiveSession.Current;
			//int currentIndex = session.Save.DisplayLevelIndex;

			//var variations = levelDatabase.GetLevelVariations(currentIndex);

			//// Always show popup if variations exist > 1 OR if it's purely a paid level.
			//if (variations != null && variations.Count > 1 && LevelVariationPopup.Instance != null)
			//{
			//	LevelVariationPopup.Instance.ShowVariations(currentIndex, variations);
			//}
			//else if (variations != null && variations.Count == 1 && variations[0].isPaid && LevelVariationPopup.Instance != null)
			//{
			//	LevelVariationPopup.Instance.ShowVariations(currentIndex, variations);
			//}
			//else
			{
				// Default skip popup if free and only 1 variation
				DOTween.KillAll();
				MenuController.OnPlayButtonClicked();
			}
		}
	}
}