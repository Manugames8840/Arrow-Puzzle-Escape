using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR

// ============= JSON 1 FORMAT CLASSES =============
[System.Serializable]
public class OldFormatLevel
{
	public SizeData size;
	public float timeLimit;
	public string name;
	public List<OldArrowData> arrows;
	public List<Vector2Data> wayBlockers;
	public List<Vector2Data> blackHoles;
	public List<Vector2Data> portals;
}

[System.Serializable]
public class SizeData
{
	public int x;
	public int y;
}

[System.Serializable]
public class OldArrowData
{
	public List<Vector2Data> nodes;
	public int color;
}

[System.Serializable]
public class Vector2Data
{
	public int x;
	public int y;
}

// ============= EDITOR WINDOW =============
public class JSONFormatConverter : EditorWindow
{
	private List<TextAsset> jsonFiles = new List<TextAsset>();
	private string savePath = "Assets/ArrowOut/Levels/Converted";
	private int startingLevelNumber = 1;
	private string levelNamePrefix = "Level";
	private Vector2 scrollPos;

	private List<ConversionResult> results = new List<ConversionResult>();
	private bool showResults = false;

	[System.Serializable]
	private class ConversionResult
	{
		public string fileName;
		public bool success;
		public string message;
		public LevelData levelData;
	}

	[MenuItem("Tools/Arrow Out/JSON Format Converter")]
	static void ShowWindow()
	{
		var window = GetWindow<JSONFormatConverter>("JSON Converter");
		window.minSize = new Vector2(500, 600);
		window.Show();
	}

	void OnGUI()
	{
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

		GUILayout.Space(10);
		DrawHeader();
		GUILayout.Space(10);
		DrawInstructions();
		GUILayout.Space(10);
		DrawFileSelection();
		GUILayout.Space(10);
		DrawSettings();
		GUILayout.Space(10);
		DrawConvertButton();
		GUILayout.Space(10);

		if (showResults)
		{
			DrawResults();
		}

		EditorGUILayout.EndScrollView();
	}

	void DrawHeader()
	{
		GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
		{
			fontSize = 16,
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = new Color(0.3f, 0.7f, 1f) }
		};
		EditorGUILayout.LabelField("🔄 JSON Format Converter", headerStyle);

		GUIStyle subStyle = new GUIStyle(EditorStyles.label)
		{
			fontSize = 11,
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = Color.gray }
		};
		EditorGUILayout.LabelField("Convert old JSON format to LevelData ScriptableObjects", subStyle);
	}

	void DrawInstructions()
	{
		EditorGUILayout.HelpBox(
			"CONVERSION MAPPING:\n" +
			"• size.x, size.y → width, height\n" +
			"• arrows.nodes → arrows.body\n" +
			"• arrows.color → ignored (can use for visual customization)\n" +
			"• wayBlockers → blockers\n" +
			"• blackHoles → holes\n" +
			"• name → levelName\n" +
			"• Auto-generates levelNumber based on order\n" +
			"• Auto-sets difficulty based on grid size and arrow count",
			MessageType.Info
		);
	}

	void DrawFileSelection()
	{
		EditorGUILayout.LabelField("JSON Files to Convert", EditorStyles.boldLabel);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("+ Add File", GUILayout.Width(100)))
		{
			jsonFiles.Add(null);
		}
		if (GUILayout.Button("Clear All", GUILayout.Width(100)))
		{
			jsonFiles.Clear();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		EditorGUI.indentLevel++;
		for (int i = 0; i < jsonFiles.Count; i++)
		{
			GUILayout.BeginHorizontal();

			jsonFiles[i] = (TextAsset)EditorGUILayout.ObjectField(
				$"File {i + 1}",
				jsonFiles[i],
				typeof(TextAsset),
				false
			);

			if (GUILayout.Button("×", GUILayout.Width(25)))
			{
				jsonFiles.RemoveAt(i);
				i--;
				continue;
			}

			GUILayout.EndHorizontal();
		}
		EditorGUI.indentLevel--;

		GUILayout.Space(5);

		// Drag & Drop area
		Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
		GUI.Box(dropArea, "Drag & Drop JSON Files Here", EditorStyles.helpBox);

		Event evt = Event.current;
		if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
		{
			if (dropArea.Contains(evt.mousePosition))
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				if (evt.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();

					foreach (Object draggedObject in DragAndDrop.objectReferences)
					{
						if (draggedObject is TextAsset textAsset)
						{
							if (!jsonFiles.Contains(textAsset))
							{
								jsonFiles.Add(textAsset);
							}
						}
					}
				}
				evt.Use();
			}
		}
	}

	void DrawSettings()
	{
		EditorGUILayout.LabelField("Conversion Settings", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		savePath = EditorGUILayout.TextField("Save Path", savePath);
		startingLevelNumber = EditorGUILayout.IntField("Starting Level Number", startingLevelNumber);
		levelNamePrefix = EditorGUILayout.TextField("Level Name Prefix", levelNamePrefix);

		EditorGUI.indentLevel--;
	}

	void DrawConvertButton()
	{
		GUI.enabled = jsonFiles.Count > 0 && jsonFiles.FindAll(f => f != null).Count > 0;

		Color originalColor = GUI.backgroundColor;
		GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);

		if (GUILayout.Button("🔄 Convert All Files", GUILayout.Height(40)))
		{
			ConvertFiles();
		}

		GUI.backgroundColor = originalColor;
		GUI.enabled = true;
	}

	void DrawResults()
	{
		GUILayout.Space(10);
		EditorGUILayout.LabelField("Conversion Results", EditorStyles.boldLabel);

		EditorGUI.indentLevel++;

		int successCount = results.FindAll(r => r.success).Count;
		int failCount = results.Count - successCount;

		EditorGUILayout.LabelField($"Total: {results.Count} | Success: {successCount} | Failed: {failCount}");

		GUILayout.Space(5);

		foreach (var result in results)
		{
			GUILayout.BeginVertical(EditorStyles.helpBox);

			GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
			{
				fontStyle = FontStyle.Bold
			};

			if (result.success)
			{
				nameStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
				EditorGUILayout.LabelField($"✓ {result.fileName}", nameStyle);
				EditorGUILayout.LabelField(result.message, EditorStyles.miniLabel);

				if (result.levelData != null && GUILayout.Button("Show in Project", GUILayout.Width(120)))
				{
					EditorGUIUtility.PingObject(result.levelData);
					Selection.activeObject = result.levelData;
				}
			}
			else
			{
				nameStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
				EditorGUILayout.LabelField($"✗ {result.fileName}", nameStyle);
				EditorGUILayout.LabelField($"Error: {result.message}", EditorStyles.miniLabel);
			}

			GUILayout.EndVertical();
			GUILayout.Space(3);
		}

		EditorGUI.indentLevel--;
	}

	void ConvertFiles()
	{
		results.Clear();

		// Create save directory
		if (!Directory.Exists(savePath))
		{
			Directory.CreateDirectory(savePath);
		}

		int levelNumber = startingLevelNumber;

		foreach (TextAsset jsonFile in jsonFiles)
		{
			if (jsonFile == null) continue;

			ConversionResult result = new ConversionResult
			{
				fileName = jsonFile.name
			};

			try
			{
				// Parse old format JSON
				OldFormatLevel oldLevel = JsonUtility.FromJson<OldFormatLevel>(jsonFile.text);

				if (oldLevel == null)
				{
					result.success = false;
					result.message = "Failed to parse JSON";
					results.Add(result);
					continue;
				}

				// Convert to new format
				LevelData newLevel = ConvertToNewFormat(oldLevel, levelNumber);

				// Save as ScriptableObject
				string fileName = string.IsNullOrEmpty(newLevel.levelName)
					? $"Level_{levelNumber:D3}.asset"
					: $"Level_{levelNumber:D3}_{newLevel.levelName.Replace(" ", "_")}.asset";

				string assetPath = $"{savePath}/{fileName}";

				AssetDatabase.CreateAsset(newLevel, assetPath);

				result.success = true;
				result.message = $"Converted to {fileName}";
				result.levelData = newLevel;

				levelNumber++;
			}
			catch (System.Exception e)
			{
				result.success = false;
				result.message = e.Message;
			}

			results.Add(result);
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		showResults = true;

		int successCount = results.FindAll(r => r.success).Count;

		EditorUtility.DisplayDialog(
			"Conversion Complete",
			$"Converted {successCount} out of {results.Count} files successfully!\n\nSaved to: {savePath}",
			"OK"
		);
	}

	LevelData ConvertToNewFormat(OldFormatLevel oldLevel, int levelNumber)
	{
		LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();

		// Basic info
		newLevel.levelNumber = levelNumber;
		newLevel.levelName = string.IsNullOrEmpty(oldLevel.name)
			? $"{levelNamePrefix} {levelNumber}"
			: oldLevel.name;

		// Grid size
		newLevel.width = oldLevel.size.x;
		newLevel.height = oldLevel.size.y;
		newLevel.Duration = oldLevel.timeLimit;

		// Convert arrows
		newLevel.arrowPaths = new List<ArrowPath>();
		foreach (OldArrowData oldArrow in oldLevel.arrows)
		{
			ArrowPath newArrow = new ArrowPath();
			newArrow.body = new List<Vector2Int>();
			oldArrow.nodes.Reverse();

			foreach (Vector2Data node in oldArrow.nodes)
			{
				newArrow.body.Add(new Vector2Int(node.x, node.y));
			}
			newArrow.color = ArrowColorPalette.GetColor(oldArrow.color);
			newLevel.arrowPaths.Add(newArrow);
		}

		// Convert blockers (wayBlockers → blockers)
		newLevel.blockers = new List<Vector2Int>();
		foreach (Vector2Data blocker in oldLevel.wayBlockers)
		{
			newLevel.blockers.Add(new Vector2Int(blocker.x, blocker.y));
		}

		// Convert holes (blackHoles → holes)
		newLevel.holes = new List<Vector2Int>();
		foreach (Vector2Data hole in oldLevel.blackHoles)
		{
			newLevel.holes.Add(new Vector2Int(hole.x, hole.y));
		}

		// Convert portals
		newLevel.portals = new List<Vector2Int>();
		foreach (Vector2Data portal in oldLevel.portals)
		{
			newLevel.portals.Add(new Vector2Int(portal.x, portal.y));
		}

		return newLevel;
	}
}
#endif