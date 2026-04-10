using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

#if UNITY_EDITOR

// ============= JSON FORMAT 3 CLASSES =============
[System.Serializable]
public class PrefabFormatLevel
{
	public int XSize;
	public int YSize;
	public List<PrefabArrowData> Arrows;
}

[System.Serializable]
public class PrefabArrowData
{
	public int Dx;
	public int Dy;
	public int X;
	public int Y;
	public List<int> Indices;
	public string color;
}

// ============= EDITOR WINDOW =============
public class PrefabJSONConverter : EditorWindow
{
	private List<Object> prefabFiles = new List<Object>();
	private string savePath = "Assets/ArrowOut/Levels/FromPrefabs";
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
		public string jsonPreview;
		public LevelData levelData;
	}

	[MenuItem("Tools/Arrow Out/Prefab JSON Converter (YAML)")]
	static void ShowWindow()
	{
		var window = GetWindow<PrefabJSONConverter>("Prefab Converter");
		window.minSize = new Vector2(550, 700);
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
			normal = { textColor = new Color(0.9f, 0.5f, 0.2f) }
		};
		EditorGUILayout.LabelField("🎯 Prefab JSON Converter (YAML Reader)", headerStyle);

		GUIStyle subStyle = new GUIStyle(EditorStyles.label)
		{
			fontSize = 11,
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = Color.gray }
		};
		EditorGUILayout.LabelField("Reads JSON directly from prefab YAML (no script needed!)", subStyle);
	}

	void DrawInstructions()
	{
		EditorGUILayout.HelpBox(
			"✅ NO SCRIPT REQUIRED!\n" +
			"This tool reads the prefab file directly as text.\n" +
			"Even if MonoBehaviour is missing, it will extract the JSON!\n\n" +
			"WHAT IT DOES:\n" +
			"• Reads prefab .prefab file as YAML text\n" +
			"• Extracts 'jsonString:' value using regex\n" +
			"• Converts index-based arrows to coordinate-based\n" +
			"• Auto-generates holes at arrow endpoints\n\n" +
			"SUPPORTED:\n" +
			"✓ Prefabs with missing scripts\n" +
			"✓ Any prefab with jsonString field in YAML",
			MessageType.Info
		);
	}

	void DrawFileSelection()
	{
		EditorGUILayout.LabelField("Prefab Files to Convert", EditorStyles.boldLabel);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("+ Add File", GUILayout.Width(100)))
		{
			prefabFiles.Add(null);
		}
		if (GUILayout.Button("Clear All", GUILayout.Width(100)))
		{
			prefabFiles.Clear();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		EditorGUI.indentLevel++;
		for (int i = 0; i < prefabFiles.Count; i++)
		{
			GUILayout.BeginHorizontal();

			prefabFiles[i] = EditorGUILayout.ObjectField(
				$"File {i + 1}",
				prefabFiles[i],
				typeof(Object),
				false
			);

			if (GUILayout.Button("×", GUILayout.Width(25)))
			{
				prefabFiles.RemoveAt(i);
				i--;
				continue;
			}

			GUILayout.EndHorizontal();
		}
		EditorGUI.indentLevel--;

		GUILayout.Space(5);

		// Drag & Drop area
		Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
		GUI.Box(dropArea, "Drag & Drop Prefab Files Here", EditorStyles.helpBox);

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
						if (!prefabFiles.Contains(draggedObject))
						{
							prefabFiles.Add(draggedObject);
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
		GUI.enabled = prefabFiles.Count > 0 && prefabFiles.FindAll(f => f != null).Count > 0;

		Color originalColor = GUI.backgroundColor;
		GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);

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

				if (!string.IsNullOrEmpty(result.jsonPreview))
				{
					string preview = result.jsonPreview.Length > 100
						? result.jsonPreview.Substring(0, 100) + "..."
						: result.jsonPreview;
					EditorGUILayout.LabelField($"JSON: {preview}", EditorStyles.wordWrappedMiniLabel);
				}

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

		foreach (Object file in prefabFiles)
		{
			if (file == null) continue;

			string fileName = file.name;
			ConversionResult result = new ConversionResult
			{
				fileName = fileName
			};

			try
			{
				// Get file path
				string assetPath = AssetDatabase.GetAssetPath(file);

				if (string.IsNullOrEmpty(assetPath))
				{
					result.success = false;
					result.message = "Could not find file path";
					results.Add(result);
					continue;
				}

				// Read prefab file as text (YAML)
				string yamlContent = File.ReadAllText(assetPath);

				// Extract JSON string using regex
				string jsonString = ExtractJSONFromYAML(yamlContent);

				if (string.IsNullOrEmpty(jsonString))
				{
					result.success = false;
					result.message = "No jsonString found in YAML";
					results.Add(result);
					continue;
				}

				result.jsonPreview = jsonString;

				// Parse JSON
				PrefabFormatLevel prefabLevel = JsonUtility.FromJson<PrefabFormatLevel>(jsonString);

				if (prefabLevel == null || prefabLevel.Arrows == null)
				{
					result.success = false;
					result.message = "Failed to parse JSON";
					results.Add(result);
					continue;
				}

				// Convert to LevelData
				LevelData newLevel = ConvertToNewFormat(prefabLevel, levelNumber, fileName);

				// Save
				string outputFileName = $"Level_{levelNumber:D3}_{fileName.Replace(" ", "_")}.asset";
				string outputPath = $"{savePath}/{outputFileName}";

				AssetDatabase.CreateAsset(newLevel, outputPath);

				result.success = true;
				//result.message = $"✓ {prefabLevel.Arrows.Count} arrows, {newLevel.holes.Count} holes";
				result.levelData = newLevel;

				levelNumber++;
			}
			catch (System.Exception e)
			{
				result.success = false;
				result.message = e.Message;
				Debug.LogError($"Error converting {fileName}: {e.Message}\n{e.StackTrace}");
			}

			results.Add(result);
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		showResults = true;

		int successCount = results.FindAll(r => r.success).Count;

		EditorUtility.DisplayDialog(
			"Conversion Complete",
			$"Converted {successCount} out of {results.Count} files!\n\nSaved to: {savePath}",
			"OK"
		);
	}

	string ExtractJSONFromYAML(string yaml)
	{
		int keyIndex = yaml.IndexOf("jsonString:");
		if (keyIndex == -1)
			return null;

		// Move cursor after "jsonString:"
		int i = keyIndex + "jsonString:".Length;

		// Skip whitespace
		while (i < yaml.Length && char.IsWhiteSpace(yaml[i]))
			i++;

		// Remove optional quote
		if (yaml[i] == '\'' || yaml[i] == '"')
			i++;

		// Find first '{'
		while (i < yaml.Length && yaml[i] != '{')
			i++;

		if (i >= yaml.Length)
			return null;

		int braceCount = 0;
		int start = i;

		for (; i < yaml.Length; i++)
		{
			if (yaml[i] == '{') braceCount++;
			else if (yaml[i] == '}') braceCount--;

			if (braceCount == 0)
			{
				int length = i - start + 1;
				return yaml.Substring(start, length);
			}
		}

		return null;
	}

	LevelData ConvertToNewFormat(PrefabFormatLevel prefabLevel, int levelNumber, string fileName)
	{
		LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();

		// Basic info
		newLevel.levelNumber = levelNumber;
		newLevel.levelName = $"{levelNamePrefix} {levelNumber}";
		newLevel.width = prefabLevel.XSize;
		newLevel.height = prefabLevel.YSize;
		newLevel.Duration = 60f + (prefabLevel.Arrows.Count * 30f);

		// Convert arrows and collect endpoint info
		newLevel.arrowPaths = new List<ArrowPath>();
		List<Vector2Int> potentialHoles = new List<Vector2Int>();

		foreach (PrefabArrowData prefabArrow in prefabLevel.Arrows)
		{
			if (prefabArrow.Indices == null || prefabArrow.Indices.Count == 0)
				continue;

			ArrowPath newArrow = new ArrowPath();
			newArrow.body = ConvertIndicesToPositions(
				prefabArrow.Indices,
				prefabLevel.XSize,
				prefabLevel.YSize
			);

			newArrow.color = ArrowColorPalette.HexToColor(prefabArrow.color);
			if (newArrow.body.Count > 0)
			{
				newLevel.arrowPaths.Add(newArrow);

				// Calculate hole position
				Vector2Int head = newArrow.body[0];
				Vector2Int direction = new Vector2Int(prefabArrow.Dx, prefabArrow.Dy);

				// Find position outside grid in arrow's direction
				Vector2Int holePos = CalculateHolePosition(
					head,
					direction,
					prefabLevel.XSize,
					prefabLevel.YSize
				);

				if (!potentialHoles.Contains(holePos))
				{
					potentialHoles.Add(holePos);
				}
			}
		}

		// Set holes
		//newLevel.holes = potentialHoles;
		//newLevel.blockers = new List<Vector2Int>();
		//newLevel.portals = new List<Vector2Int>();

		return newLevel;
	}

	List<Vector2Int> ConvertIndicesToPositions(List<int> indices, int gridWidth, int gridHeight)
	{
		List<Vector2Int> positions = new List<Vector2Int>();

		foreach (int index in indices)
		{
			// Convert 1D index to 2D coordinates
			// Formula: index = y * width + x
			int x = index % gridWidth;
			int y = index / gridWidth;

			// Validate
			if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
			{
				positions.Add(new Vector2Int(x, y));
			}
			else
			{
				Debug.LogWarning($"Index {index} out of bounds for grid {gridWidth}x{gridHeight}");
			}
		}

		return positions;
	}

	Vector2Int CalculateHolePosition(Vector2Int head, Vector2Int direction, int width, int height)
	{
		// Start from head
		Vector2Int pos = head;

		// Move in direction until outside grid
		while (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
		{
			pos += direction;
		}

		// Return position just outside grid
		return pos;
	}
}

// ============= BATCH CONVERTER =============
public class BatchPrefabFolderConverter : EditorWindow
{
	private DefaultAsset sourceFolder;
	private string savePath = "Assets/ArrowOut/Levels/FromPrefabs";
	private int startingLevelNumber = 1;

	[MenuItem("Tools/Arrow Out/Batch Convert Prefab Folder")]
	static void ShowWindow()
	{
		GetWindow<BatchPrefabFolderConverter>("Batch Converter").Show();
	}

	void OnGUI()
	{
		GUILayout.Space(10);

		EditorGUILayout.LabelField("Batch Prefab Folder Converter", EditorStyles.boldLabel);

		GUILayout.Space(10);

		EditorGUILayout.HelpBox(
			"Select a folder containing .prefab files.\n" +
			"All prefabs will be converted automatically!",
			MessageType.Info
		);

		GUILayout.Space(10);

		sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
			"Source Folder",
			sourceFolder,
			typeof(DefaultAsset),
			false
		);

		savePath = EditorGUILayout.TextField("Save Path", savePath);
		startingLevelNumber = EditorGUILayout.IntField("Starting Level Number", startingLevelNumber);

		GUILayout.Space(10);

		GUI.enabled = sourceFolder != null;
		if (GUILayout.Button("Convert All Prefabs in Folder", GUILayout.Height(40)))
		{
			ConvertFolder();
		}
		GUI.enabled = true;
	}

	void ConvertFolder()
	{
		string folderPath = AssetDatabase.GetAssetPath(sourceFolder);

		if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
		{
			EditorUtility.DisplayDialog("Error", "Please select a valid folder!", "OK");
			return;
		}

		// Find all files in folder
		string[] allFiles = Directory.GetFiles(folderPath, "*.prefab", SearchOption.AllDirectories);

		if (allFiles.Length == 0)
		{
			EditorUtility.DisplayDialog("Error", "No .prefab files found in folder!", "OK");
			return;
		}

		// Create save directory
		if (!Directory.Exists(savePath))
		{
			Directory.CreateDirectory(savePath);
		}

		int successCount = 0;
		int levelNumber = startingLevelNumber;

		foreach (string filePath in allFiles)
		{
			try
			{
				string assetPath = filePath.Replace("\\", "/");
				Object file = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

				if (file != null)
				{
					// Read YAML
					string yamlContent = File.ReadAllText(filePath);
					string jsonString = ExtractJSON(yamlContent);

					if (!string.IsNullOrEmpty(jsonString))
					{
						PrefabFormatLevel prefabLevel = JsonUtility.FromJson<PrefabFormatLevel>(jsonString);

						if (prefabLevel != null && prefabLevel.Arrows != null)
						{
							// Convert (using simplified version)
							LevelData level = CreateLevel(prefabLevel, levelNumber, file.name);

							string fileName = $"Level_{levelNumber:D3}_{file.name.Replace(" ", "_")}.asset";
							AssetDatabase.CreateAsset(level, $"{savePath}/{fileName}");

							successCount++;
							levelNumber++;
						}
					}
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Failed to convert {Path.GetFileName(filePath)}: {e.Message}");
			}
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		EditorUtility.DisplayDialog(
			"Batch Conversion Complete",
			$"Converted {successCount} out of {allFiles.Length} prefabs!\n\nSaved to: {savePath}",
			"OK"
		);
	}

	string ExtractJSON(string yamlContent)
	{
		Match match = Regex.Match(yamlContent, @"jsonString:\s*'([^']+)'");
		if (match.Success) return match.Groups[1].Value;

		match = Regex.Match(yamlContent, @"jsonString:\s*({[\s\S]*?})\s*\n", RegexOptions.Multiline);
		if (match.Success) return match.Groups[1].Value.Trim();

		match = Regex.Match(yamlContent, @"({""XSize""[^}]+})");
		if (match.Success) return match.Groups[1].Value;

		return null;
	}

	LevelData CreateLevel(PrefabFormatLevel prefabLevel, int levelNum, string name)
	{
		LevelData level = ScriptableObject.CreateInstance<LevelData>();
		level.levelNumber = levelNum;
		level.levelName = $"Level {levelNum}";
		level.width = prefabLevel.XSize;
		level.height = prefabLevel.YSize;
		level.Duration = 60f + (prefabLevel.Arrows.Count * 30f);
		level.arrowPaths = new List<ArrowPath>();
		level.holes = new List<Vector2Int>();
		level.blockers = new List<Vector2Int>();
		level.portals = new List<Vector2Int>();

		foreach (var arrow in prefabLevel.Arrows)
		{
			if (arrow.Indices != null && arrow.Indices.Count > 0)
			{
				ArrowPath path = new ArrowPath();
				path.body = new List<Vector2Int>();

				foreach (int idx in arrow.Indices)
				{
					int x = idx % prefabLevel.XSize;
					int y = idx / prefabLevel.XSize;
					path.body.Add(new Vector2Int(x, y));
				}

				level.arrowPaths.Add(path);

				// Calculate hole
				Vector2Int head = path.body[0];
				Vector2Int dir = new Vector2Int(arrow.Dx, arrow.Dy);
				Vector2Int holePos = head;

				while (holePos.x >= 0 && holePos.x < prefabLevel.XSize &&
					   holePos.y >= 0 && holePos.y < prefabLevel.YSize)
				{
					holePos += dir;
				}

				if (!level.holes.Contains(holePos))
				{
					level.holes.Add(holePos);
				}
			}
		}

		return level;
	}
}
#endif