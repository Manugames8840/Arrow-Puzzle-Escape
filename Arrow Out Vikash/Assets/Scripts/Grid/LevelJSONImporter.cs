using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace ArrowOut
{
#if UNITY_EDITOR
	public class LevelJSONImporter : EditorWindow
	{
		private TextAsset jsonFile;
		private string savePath = "Assets/ArrowOut/Levels";

		[MenuItem("Tools/Arrow Out/Import Levels from JSON")]
		static void ShowWindow()
		{
			GetWindow<LevelJSONImporter>("Level Importer");
		}

		void OnGUI()
		{
			GUILayout.Space(10);
			EditorGUILayout.LabelField("JSON Level Importer", EditorStyles.boldLabel);
			GUILayout.Space(10);

			jsonFile = (TextAsset)EditorGUILayout.ObjectField(
				"JSON File",
				jsonFile,
				typeof(TextAsset),
				false
			);

			savePath = EditorGUILayout.TextField("Save Path", savePath);

			GUILayout.Space(10);

			GUI.enabled = jsonFile != null;
			if (GUILayout.Button("Import Levels", GUILayout.Height(40)))
			{
				ImportLevels();
			}
			GUI.enabled = true;

			GUILayout.Space(10);

			if (GUILayout.Button("Create Sample JSON Template"))
			{
				CreateSampleJSON();
			}
		}

		void ImportLevels()
		{
			try
			{
				// Parse JSON
				LevelPackJSON levelPack = JsonUtility.FromJson<LevelPackJSON>(jsonFile.text);

				if (levelPack == null || levelPack.levels == null)
				{
					EditorUtility.DisplayDialog("Error", "Invalid JSON format!", "OK");
					return;
				}

				// Create save directory
				if (!Directory.Exists(savePath))
				{
					Directory.CreateDirectory(savePath);
				}

				int successCount = 0;

				// Convert each level
				foreach (LevelJSON levelJSON in levelPack.levels)
				{
					LevelData levelData = CreateLevelData(levelJSON);

					if (levelData != null)
					{
						string assetPath = $"{savePath}/Level_{levelJSON.levelNumber:D3}.asset";
						AssetDatabase.CreateAsset(levelData, assetPath);
						successCount++;
					}
				}

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				EditorUtility.DisplayDialog(
					"Import Complete",
					$"Successfully imported {successCount} levels to:\n{savePath}",
					"OK"
				);
			}
			catch (System.Exception e)
			{
				EditorUtility.DisplayDialog("Error", "Import failed:\n" + e.Message, "OK");
				Debug.LogError(e);
			}
		}

		LevelData CreateLevelData(LevelJSON levelJSON)
		{
			LevelData level = ScriptableObject.CreateInstance<LevelData>();

			level.width = levelJSON.width;
			level.height = levelJSON.height;
			level.Duration = levelJSON.timeLimit;

			// Convert arrows
			level.arrowPaths = new List<ArrowPath>();
			foreach (ArrowJSON arrowJSON in levelJSON.arrows)
			{
				ArrowPath arrow = new ArrowPath();
				arrow.body = new List<Vector2Int>();

				foreach (Vector2JSON pos in arrowJSON.body)
				{
					arrow.body.Add(new Vector2Int(pos.x, pos.y));
				}

				level.arrowPaths.Add(arrow);
			}

			// Convert blockers
			level.blockers = new List<Vector2Int>();
			foreach (Vector2JSON pos in levelJSON.blockers)
			{
				level.blockers.Add(new Vector2Int(pos.x, pos.y));
			}

			// Convert holes
			level.holes = new List<Vector2Int>();
			foreach (Vector2JSON pos in levelJSON.holes)
			{
				level.holes.Add(new Vector2Int(pos.x, pos.y));
			}

			// Convert portals
			level.portals = new List<Vector2Int>();
			foreach (Vector2JSON pos in levelJSON.portals)
			{
				level.portals.Add(new Vector2Int(pos.x, pos.y));
			}

			return level;
		}

		void CreateSampleJSON()
		{
			LevelPackJSON sample = new LevelPackJSON();
			sample.levels = new List<LevelJSON>();

			// Sample level
			LevelJSON level1 = new LevelJSON
			{
				levelNumber = 1,
				levelName = "Tutorial 1",
				width = 5,
				height = 5,
				timeLimit = 30f,
				difficulty = "Easy",
				arrows = new List<ArrowJSON>(),
				blockers = new List<Vector2JSON>(),
				holes = new List<Vector2JSON>(),
				portals = new List<Vector2JSON>()
			};

			// Add sample arrow
			ArrowJSON arrow = new ArrowJSON();
			arrow.body = new List<Vector2JSON>
		{
			new Vector2JSON { x = 1, y = 2 },
			new Vector2JSON { x = 2, y = 2 }
		};
			level1.arrows.Add(arrow);

			// Add sample hole
			level1.holes.Add(new Vector2JSON { x = 4, y = 2 });

			sample.levels.Add(level1);

			// Save to file
			string json = JsonUtility.ToJson(sample, true);
			string path = "Assets/sample_levels.json";
			File.WriteAllText(path, json);
			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog(
				"Sample Created",
				$"Sample JSON created at:\n{path}",
				"OK"
			);
		}
	}
#endif
}