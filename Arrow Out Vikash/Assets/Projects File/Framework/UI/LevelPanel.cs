using System;
using Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Framework.Core;

namespace Watermelon
{
	public class LevelPanel : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI levelText;
		[SerializeField] Image leftSideIcon;
		[SerializeField] Image rightSideIcon;
		[SerializeField] TextAnim textAnimation;

		public void Init(int levelIndex)
		{
			GameData gameData = GameData.Data;
			LevelDatabase levelDatabase = gameData.LevelDatabase;
			LevelRepresentation levelRepresentation = LevelController.LevelRepresentation;

			LevelData levelData = levelRepresentation.LevelData;
			MapLevelData mapLevelData = levelDatabase.GetMapLevelData(levelData.Type);

			levelText.text = string.Format("LEVEL \n {0}", levelIndex + 1);
			levelText.color = mapLevelData.TextColor;

			Sprite customIcon = mapLevelData.Icon;
			if (customIcon != null)
			{
				// Enable icons
				leftSideIcon.sprite = customIcon;
				rightSideIcon.sprite = customIcon;

				leftSideIcon.gameObject.SetActive(true);
				rightSideIcon.gameObject.SetActive(true);

				// Scale icons
				RectTransform leftIconRT = leftSideIcon.rectTransform;
				RectTransform rightIconRT = rightSideIcon.rectTransform;

				leftIconRT.sizeDelta *= mapLevelData.IconScale;
				rightIconRT.sizeDelta *= mapLevelData.IconScale;

				// Text RectTransform
				RectTransform textRT = levelText.rectTransform;

				// Get widths
				float leftWidth = leftIconRT.sizeDelta.x;
				float rightWidth = rightIconRT.sizeDelta.x;
				float textWidth = textRT.sizeDelta.x;

				// Calculate centered offset
				float offsetX = (leftWidth - rightWidth) * 0.5f;

				// Apply
				textRT.anchoredPosition = new Vector2(
					offsetX,
					textRT.anchoredPosition.y
				);
			}
			else
			{
				leftSideIcon.gameObject.SetActive(false);

				levelText.rectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}
}
