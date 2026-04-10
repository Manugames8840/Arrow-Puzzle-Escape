using System.Collections.Generic;
using System.Linq;
using Framework.Core;
using UnityEngine;
using Watermelon;

[CreateAssetMenu(menuName = "Level/Level Database")]
public class LevelDatabase : ScriptableObject
{
	public List<LevelData> levels = new List<LevelData>();
	public int AmountOfLevels => levels.Count;

	[Space]
	[SerializeField] MapLevelData[] mapLevelDatas;
	public MapLevelData[] MapLevelDatas => mapLevelDatas;

	/// <summary>
	/// Is called when LevelController is initialized
	/// </summary>
	public void Init()
	{

	}

	public int GetRandomLevelIndex(int displayLevelNumber, int lastPlayedLevelNumber, bool replayingLevel)
	{
		if (levels.IsInRange(displayLevelNumber))
		{
			return displayLevelNumber;
		}

		if (replayingLevel)
		{
			return lastPlayedLevelNumber;
		}

		int randomLevelIndex;
		int attempts = 0;

		do
		{
			randomLevelIndex = Random.Range(0, levels.Count);

			attempts++;
			if (attempts > 100)
				return randomLevelIndex;
		}
		while (!levels[randomLevelIndex].UseInRandomizer && randomLevelIndex != lastPlayedLevelNumber);

		return randomLevelIndex;
	}

	public LevelData GetRandomLevel()
	{
		int randomLevelIndex;

		int attempts = 0;

		do
		{
			randomLevelIndex = Random.Range(0, levels.Count);

			attempts++;
			if (attempts > 100)
			{
				return levels[randomLevelIndex];
			}
		}
		while (!levels[randomLevelIndex].UseInRandomizer);

		return levels[randomLevelIndex];
	}

	public LevelData GetLevel(int index)
	{
		if (index < AmountOfLevels && index >= 0)
			return levels[index];

		return null;
	}

	public List<LevelData> GetLevelVariations(int displayLevelNumber)
	{
		// Map index to human-readable levelNumber (typically displayLevelNumber + 1)
		int targetLevelNumber = displayLevelNumber + 1;
		
		List<LevelData> variations = levels.Where(l => l.levelNumber == targetLevelNumber).ToList();
		
		// Fallback for older configurations where levelNumber might not be set perfectly
		if (variations.Count == 0)
		{
			LevelData fallback = GetLevel(displayLevelNumber);
			if (fallback != null)
				variations.Add(fallback);
		}
		
		return variations;
	}
	public MapLevelData GetMapLevelData(LevelType levelType)
	{
		MapLevelData mapLevelData = mapLevelDatas.First(data => data.LevelType == levelType);
		if (mapLevelData == null)
		{
			Debug.LogError($"Map level data for {levelType} not found in level database. Please check the LevelDatabase asset.", this);
		}

		return mapLevelData;
	}

}