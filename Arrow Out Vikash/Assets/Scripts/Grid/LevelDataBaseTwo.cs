using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Level/Level Database Two")]
public class LevelDataBaseTwo : ScriptableObject
{
	public List<LevelData> levels = new();
}
