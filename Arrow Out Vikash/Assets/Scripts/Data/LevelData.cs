using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ArrowOut/Level")]
public class LevelData : ScriptableObject
{
	public int width;
	public int height;
	public Vector2Int Size => new Vector2Int(width, height);
	public float Duration;
	public int levelNumber;
	public string levelName;
	public LevelType Type;

	public List<ArrowPath> arrowPaths;

	public List<Vector2Int> blockers;
	public List<Vector2Int> holes;
	public List<Vector2Int> portals;
	public bool UseInRandomizer;

	[Header("Variation Settings")]
	public bool isPaid = false;
	public int coinCost = 100;
}

public enum LevelType
{
	Easy,
	Hard
}
