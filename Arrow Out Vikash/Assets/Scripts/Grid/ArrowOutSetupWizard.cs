using UnityEngine;
using UnityEditor;

namespace ArrowOut
{
#if UNITY_EDITOR
	/// <summary>
	/// Setup wizard for Arrow Out - SOLID Architecture
	/// Creates and configures GridManager with selected render mode
	/// </summary>
	public class ArrowOutSetupWizard : EditorWindow
	{
		GameRenderMode renderMode = GameRenderMode.LineRenderer2D;
		bool generatePrefabs = true;
		bool setupCamera = true;

		[MenuItem("Tools/Arrow Out/Setup Wizard")]
		static void ShowWindow()
		{
			var window = GetWindow<ArrowOutSetupWizard>("Arrow Out Setup");
			window.minSize = new Vector2(450, 550);
			window.Show();
		}

		void OnGUI()
		{
			GUILayout.Space(10);

			// Header
			DrawHeader();

			GUILayout.Space(20);

			// Info box
			EditorGUILayout.HelpBox(
				"This wizard will create a GridManager with the new SOLID architecture.\n" +
				"Features: Separate head & body, mesh colliders, 4 render modes!",
				MessageType.Info
			);

			GUILayout.Space(10);

			// Step 1: Render Mode
			DrawRenderModeSection();

			GUILayout.Space(10);

			// Step 2: Options
			DrawOptionsSection();

			GUILayout.Space(20);

			// Setup Button
			DrawSetupButton();

			GUILayout.Space(10);

			// Instructions
			DrawInstructions();

			GUILayout.Space(10);

			// Documentation buttons
			DrawDocumentationButtons();
		}

		void DrawHeader()
		{
			GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 18,
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = new Color(0.3f, 0.7f, 1f) }
			};
			EditorGUILayout.LabelField("⚡ Arrow Out Setup Wizard", headerStyle);

			GUIStyle subHeaderStyle = new GUIStyle(EditorStyles.label)
			{
				fontSize = 11,
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = Color.gray }
			};
			EditorGUILayout.LabelField("SOLID Architecture | Design Patterns | Professional Code", subHeaderStyle);
		}

		void DrawRenderModeSection()
		{
			GUIStyle sectionStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 13
			};
			EditorGUILayout.LabelField("Step 1: Choose Rendering Mode", sectionStyle);

			EditorGUI.indentLevel++;
			renderMode = (GameRenderMode)EditorGUILayout.EnumPopup("Render Mode", renderMode);

			GUILayout.Space(5);

			string description = GetRenderModeDescription(renderMode);
			EditorGUILayout.HelpBox(description, MessageType.None);

			GUILayout.Space(5);

			// Technical details
			DrawTechnicalDetails(renderMode);

			EditorGUI.indentLevel--;
		}

		void DrawTechnicalDetails(GameRenderMode mode)
		{
			GUIStyle techStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				wordWrap = true,
				fontSize = 10
			};

			string techDetails = GetTechnicalDetails(mode);
			EditorGUILayout.LabelField(techDetails, techStyle);
		}

		void DrawOptionsSection()
		{
			GUIStyle sectionStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 13
			};
			EditorGUILayout.LabelField("Step 2: Setup Options", sectionStyle);

			EditorGUI.indentLevel++;
			generatePrefabs = EditorGUILayout.Toggle("Generate Sample Prefabs", generatePrefabs);
			setupCamera = EditorGUILayout.Toggle("Auto-Setup Camera", setupCamera);
			EditorGUI.indentLevel--;
		}

		void DrawSetupButton()
		{
			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
			{
				fontSize = 14,
				fontStyle = FontStyle.Bold,
				fixedHeight = 45
			};

			Color originalColor = GUI.backgroundColor;
			GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);

			if (GUILayout.Button("🚀 Create GridManager", buttonStyle))
			{
				SetupScene();
			}

			GUI.backgroundColor = originalColor;
		}

		void DrawInstructions()
		{
			EditorGUILayout.HelpBox(
				"After setup:\n" +
				"1. Assign your LevelData in the inspector\n" +
				"2. Assign sprites/materials based on render mode\n" +
				"3. Press Play to test!\n\n" +
				"💡 Tip: Start with LineRenderer2D for testing!",
				MessageType.Info
			);
		}

		void DrawDocumentationButtons()
		{
			EditorGUILayout.LabelField("Documentation", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("📖 Quick Start Guide"))
			{
				OpenDocumentation("QUICK_START");
			}

			if (GUILayout.Button("🏗️ Architecture Guide"))
			{
				OpenDocumentation("ARCHITECTURE");
			}

			GUILayout.EndHorizontal();
		}

		void SetupScene()
		{
			// Step 1: Generate prefabs if needed
			if (generatePrefabs)
			{
				GenerateSamplePrefabs();
			}

			// Step 2: Create GameManager
			GameObject gameManager = CreateGameManager();

			// Step 3: Setup GridManager component
			GridManager gridManager = gameManager.AddComponent<GridManager>();

			// Step 4: Create GridRoot
			GameObject gridRoot = new GameObject("GridRoot");
			gridRoot.transform.SetParent(gameManager.transform);

			// Step 5: Configure GridManager
			ConfigureGridManager(gridManager, gridRoot);

			// Step 6: Setup Camera
			if (setupCamera)
			{
				SetupCameraForMode(gridManager);
			}

			// Step 7: Load and assign prefabs
			AssignPrefabs(gridManager);

			// Step 8: Select and ping
			Selection.activeGameObject = gameManager;
			EditorGUIUtility.PingObject(gameManager);

			// Step 9: Show completion dialog
			ShowCompletionDialog();

			Close();
		}

		GameObject CreateGameManager()
		{
			// Check if one already exists
			GameObject existing = GameObject.Find("GameManager");
			if (existing != null)
			{
				if (EditorUtility.DisplayDialog(
					"GameManager Exists",
					"A GameManager already exists. Replace it?",
					"Yes", "Cancel"))
				{
					DestroyImmediate(existing);
				}
				else
				{
					return existing;
				}
			}

			return new GameObject("GameManager");
		}

		void ConfigureGridManager(GridManager gridManager, GameObject gridRoot)
		{
			gridManager.renderMode = renderMode;
			gridManager.cameraMode = CameraMode.Orthographic2D; // Always orthographic
			gridManager.gridRoot = gridRoot.transform;
			gridManager.cameraPadding = 1f;
		}

		void SetupCameraForMode(GridManager gridManager)
		{
			Camera cam = Camera.main;
			if (cam == null)
			{
				GameObject camObj = new GameObject("Main Camera");
				cam = camObj.AddComponent<Camera>();
				camObj.tag = "MainCamera";
			}

			gridManager.mainCamera = cam;

			// Configure camera based on mode
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
		}

		void AssignPrefabs(GridManager gridManager)
		{
			string prefabPath = "Assets/ArrowOut/Prefabs";

			switch (renderMode)
			{
				case GameRenderMode.LineRenderer2D:
					AssignLineRenderer2DAssets(gridManager, prefabPath);
					break;

				case GameRenderMode.SpriteMesh2D:
					AssignSpriteMesh2DAssets(gridManager, prefabPath);
					break;

				case GameRenderMode.Mesh3D:
					AssignMesh3DAssets(gridManager, prefabPath);
					break;

				case GameRenderMode.LineRenderer3D:
					AssignLineRenderer3DAssets(gridManager, prefabPath);
					break;
			}
		}

		void AssignLineRenderer2DAssets(GridManager gridManager, string path)
		{
			// Arrow Head Sprite
			Sprite headSprite = LoadSprite(path, "ArrowHead2D");
			gridManager.arrowHeadSprite = headSprite;

			// Grid Elements
			gridManager.blocker2D = LoadPrefab(path, "Blocker2D");
			gridManager.hole2D = LoadPrefab(path, "Hole2D");
			gridManager.portal2D = LoadPrefab(path, "Portal2D");
		}

		void AssignSpriteMesh2DAssets(GridManager gridManager, string path)
		{
			// Arrow Sprites
			gridManager.arrowHeadSprite = LoadSprite(path, "ArrowHead2D");
			gridManager.arrowBodySprite = LoadSprite(path, "ArrowBody2D");

			// Grid Elements
			gridManager.blocker2D = LoadPrefab(path, "Blocker2D");
			gridManager.hole2D = LoadPrefab(path, "Hole2D");
			gridManager.portal2D = LoadPrefab(path, "Portal2D");
		}

		void AssignMesh3DAssets(GridManager gridManager, string path)
		{
			// Arrow Material
			Material arrowMat = new Material(Shader.Find("Standard"));
			arrowMat.color = new Color(0.3f, 0.7f, 1f);
			arrowMat.SetFloat("_Metallic", 0.2f);
			arrowMat.SetFloat("_Glossiness", 0.6f);
			gridManager.arrow3DMaterial = arrowMat;

			// Grid Elements
			gridManager.blocker3D = LoadPrefab(path, "Blocker3D");
			gridManager.hole3D = LoadPrefab(path, "Hole3D");
			gridManager.portal3D = LoadPrefab(path, "Portal3D");
		}

		void AssignLineRenderer3DAssets(GridManager gridManager, string path)
		{
			// Arrow Material (similar to Mesh3D)
			Material arrowMat = new Material(Shader.Find("Standard"));
			arrowMat.color = new Color(0.3f, 0.7f, 1f);
			arrowMat.SetFloat("_Metallic", 0.3f);
			arrowMat.SetFloat("_Glossiness", 0.8f);
			gridManager.arrow3DMaterial = arrowMat;

			// Grid Elements
			gridManager.blocker3D = LoadPrefab(path, "Blocker3D");
			gridManager.hole3D = LoadPrefab(path, "Hole3D");
			gridManager.portal3D = LoadPrefab(path, "Portal3D");
		}

		void GenerateSamplePrefabs()
		{
			string folderPath = "Assets/ArrowOut/Prefabs";

			// Create folders
			if (!AssetDatabase.IsValidFolder("Assets/ArrowOut"))
				AssetDatabase.CreateFolder("Assets", "ArrowOut");

			if (!AssetDatabase.IsValidFolder(folderPath))
				AssetDatabase.CreateFolder("Assets/ArrowOut", "Prefabs");

			// Generate 2D Assets
			Generate2DAssets(folderPath);

			// Generate 3D Assets
			Generate3DAssets(folderPath);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("✅ Sample prefabs and sprites generated at: " + folderPath);
		}

		void Generate2DAssets(string path)
		{
			// Create arrow head sprite
			Texture2D headTex = CreateArrowHeadTexture();
			SaveTextureAsSprite(headTex, $"{path}/ArrowHead2D.png");

			// Create arrow body sprite
			Texture2D bodyTex = CreateArrowBodyTexture();
			SaveTextureAsSprite(bodyTex, $"{path}/ArrowBody2D.png");

			// Create blocker
			GameObject blocker2D = GameObject.CreatePrimitive(PrimitiveType.Quad);
			blocker2D.transform.localScale = Vector3.one * 0.8f;
			MeshRenderer blockerMR = blocker2D.GetComponent<MeshRenderer>();
			blockerMR.material = new Material(Shader.Find("Sprites/Default"));
			blockerMR.material.color = new Color(1f, 0.3f, 0.3f);
			PrefabUtility.SaveAsPrefabAsset(blocker2D, $"{path}/Blocker2D.prefab");
			DestroyImmediate(blocker2D);

			// Create hole
			GameObject hole2D = GameObject.CreatePrimitive(PrimitiveType.Quad);
			hole2D.transform.localScale = Vector3.one * 0.7f;
			MeshRenderer holeMR = hole2D.GetComponent<MeshRenderer>();
			holeMR.material = new Material(Shader.Find("Sprites/Default"));
			holeMR.material.color = new Color(0.2f, 0.2f, 0.2f);
			PrefabUtility.SaveAsPrefabAsset(hole2D, $"{path}/Hole2D.prefab");
			DestroyImmediate(hole2D);

			// Create portal
			GameObject portal2D = GameObject.CreatePrimitive(PrimitiveType.Quad);
			portal2D.transform.localScale = Vector3.one * 0.9f;
			MeshRenderer portalMR = portal2D.GetComponent<MeshRenderer>();
			portalMR.material = new Material(Shader.Find("Sprites/Default"));
			portalMR.material.color = new Color(0.3f, 1f, 0.5f);
			PrefabUtility.SaveAsPrefabAsset(portal2D, $"{path}/Portal2D.prefab");
			DestroyImmediate(portal2D);
		}

		void Generate3DAssets(string path)
		{
			// Blocker 3D
			GameObject blocker3D = GameObject.CreatePrimitive(PrimitiveType.Cube);
			blocker3D.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
			MeshRenderer blockerMR = blocker3D.GetComponent<MeshRenderer>();
			blockerMR.material = new Material(Shader.Find("Standard"));
			blockerMR.material.color = new Color(1f, 0.3f, 0.3f);
			PrefabUtility.SaveAsPrefabAsset(blocker3D, $"{path}/Blocker3D.prefab");
			DestroyImmediate(blocker3D);

			// Hole 3D
			GameObject hole3D = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			hole3D.transform.localScale = new Vector3(0.7f, 0.05f, 0.7f);
			MeshRenderer holeMR = hole3D.GetComponent<MeshRenderer>();
			holeMR.material = new Material(Shader.Find("Standard"));
			holeMR.material.color = new Color(0.1f, 0.1f, 0.1f);
			PrefabUtility.SaveAsPrefabAsset(hole3D, $"{path}/Hole3D.prefab");
			DestroyImmediate(hole3D);

			// Portal 3D
			GameObject portal3D = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			portal3D.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
			MeshRenderer portalMR = portal3D.GetComponent<MeshRenderer>();
			Material portalMat = new Material(Shader.Find("Standard"));
			portalMat.color = new Color(0.3f, 1f, 0.5f);
			portalMat.EnableKeyword("_EMISSION");
			portalMat.SetColor("_EmissionColor", new Color(0.3f, 1f, 0.5f) * 0.5f);
			portalMR.material = portalMat;
			PrefabUtility.SaveAsPrefabAsset(portal3D, $"{path}/Portal3D.prefab");
			DestroyImmediate(portal3D);
		}

		Texture2D CreateArrowHeadTexture()
		{
			int size = 128;
			Texture2D tex = new Texture2D(size, size);
			Color clear = new Color(0, 0, 0, 0);
			Color white = Color.white;

			// Fill with transparent
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					tex.SetPixel(x, y, clear);
				}
			}

			// Draw arrow pointing right (triangle)
			int centerY = size / 2;
			for (int x = 40; x < 88; x++)
			{
				int height = (int)((x - 40) * 0.7f);
				for (int y = centerY - height; y <= centerY + height; y++)
				{
					if (y >= 0 && y < size)
						tex.SetPixel(x, y, white);
				}
			}

			tex.Apply();
			return tex;
		}

		Texture2D CreateArrowBodyTexture()
		{
			int size = 128;
			Texture2D tex = new Texture2D(size, size);
			Color clear = new Color(0, 0, 0, 0);
			Color white = Color.white;

			// Fill with transparent
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					tex.SetPixel(x, y, clear);
				}
			}

			// Draw horizontal line
			int centerY = size / 2;
			int thickness = 20;
			for (int x = 20; x < 108; x++)
			{
				for (int y = centerY - thickness / 2; y <= centerY + thickness / 2; y++)
				{
					tex.SetPixel(x, y, white);
				}
			}

			tex.Apply();
			return tex;
		}

		void SaveTextureAsSprite(Texture2D texture, string path)
		{
			byte[] bytes = texture.EncodeToPNG();
			System.IO.File.WriteAllBytes(path, bytes);
			AssetDatabase.Refresh();

			// Configure as sprite
			TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer != null)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Single;
				importer.spritePixelsPerUnit = 100;
				importer.filterMode = FilterMode.Bilinear;
				importer.SaveAndReimport();
			}
		}

		GameObject LoadPrefab(string path, string name)
		{
			return AssetDatabase.LoadAssetAtPath<GameObject>($"{path}/{name}.prefab");
		}

		Sprite LoadSprite(string path, string name)
		{
			return AssetDatabase.LoadAssetAtPath<Sprite>($"{path}/{name}.png");
		}

		void ShowCompletionDialog()
		{
			string modeDescription = renderMode.ToString();
			string message = $"GridManager created with {modeDescription} mode!\n\n" +
							 "✅ SOLID architecture applied\n" +
							 "✅ Design patterns implemented\n" +
							 "✅ Mesh colliders configured\n\n" +
							 "Next steps:\n" +
							 "1. Assign your LevelData\n" +
							 "2. Customize sprites/materials (optional)\n" +
							 "3. Press Play!\n\n" +
							 "💡 Check QUICK_START.md for details!";

			EditorUtility.DisplayDialog("✨ Setup Complete!", message, "Let's Go!");
		}

		void OpenDocumentation(string docName)
		{
			string[] guids = AssetDatabase.FindAssets($"{docName} t:TextAsset");
			if (guids.Length > 0)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[0]);
				string fullPath = Application.dataPath + "/../" + path;
				Application.OpenURL("file://" + fullPath);
			}
			else
			{
				EditorUtility.DisplayDialog(
					"Documentation Not Found",
					$"Could not find {docName}.md\n\nMake sure you've imported all documentation files.",
					"OK"
				);
			}
		}

		string GetRenderModeDescription(GameRenderMode mode)
		{
			switch (mode)
			{
				case GameRenderMode.LineRenderer2D:
					return "⚡ BEST PERFORMANCE\n" +
						   "✓ Unity's 2D LineRenderer\n" +
						   "✓ EdgeCollider2D for clicking\n" +
						   "✓ Perfect for mobile & prototypes\n" +
						   "✓ Separate arrow head sprite";

				case GameRenderMode.SpriteMesh2D:
					return "🎨 CUSTOM ART\n" +
						   "✓ Sprite segments with rotation\n" +
						   "✓ PolygonCollider2D for clicking\n" +
						   "✓ Support for custom sprites\n" +
						   "✓ Great for polished 2D games\n" +
						   "📏 Sprite size: 128x128px";

				case GameRenderMode.Mesh3D:
					return "💎 PREMIUM 3D\n" +
						   "✓ Procedural tube mesh\n" +
						   "✓ Full lighting & shadows\n" +
						   "✓ MeshCollider for clicking\n" +
						   "✓ Material/shader support\n" +
						   "✓ Best visual quality";

				case GameRenderMode.LineRenderer3D:
					return "⭐ 3D LINE RENDERER (NEW!)\n" +
						   "✓ Unity's 3D LineRenderer\n" +
						   "✓ MeshCollider (baked)\n" +
						   "✓ Better performance than Mesh3D\n" +
						   "✓ Material/shader effects\n" +
						   "✓ Same as 2D but in 3D space";

				default:
					return "";
			}
		}

		string GetTechnicalDetails(GameRenderMode mode)
		{
			switch (mode)
			{
				case GameRenderMode.LineRenderer2D:
					return "Technical: LineRenderer + EdgeCollider2D + SpriteArrowHead";

				case GameRenderMode.SpriteMesh2D:
					return "Technical: Sprite segments + PolygonCollider2D + SpriteArrowHead";

				case GameRenderMode.Mesh3D:
					return "Technical: Procedural tube mesh + MeshCollider + MeshArrowHead";

				case GameRenderMode.LineRenderer3D:
					return "Technical: 3D LineRenderer + Baked MeshCollider + MeshArrowHead";

				default:
					return "";
			}
		}
	}
#endif
}
