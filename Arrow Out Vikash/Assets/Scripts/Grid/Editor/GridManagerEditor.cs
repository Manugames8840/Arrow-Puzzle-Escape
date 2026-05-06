using Framework.Core;
using UnityEditor;
using UnityEngine;

namespace ArrowOut
{
	[CustomEditor(typeof(GridManager))]
	public class GridManagerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GridManager manager = (GridManager)target;

			// Header
			EditorGUILayout.Space(10);
			GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 14,
				normal = { textColor = new Color(0.3f, 0.7f, 1f) }
			};
			EditorGUILayout.LabelField("GRID MANAGER", headerStyle);
			EditorGUILayout.Space(10);

			EditorGUILayout.Space(10);

			// Rendering Mode
			DrawSection("Rendering", () =>
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("renderMode"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMode"));

				GameRenderMode mode = (GameRenderMode)serializedObject.FindProperty("renderMode").enumValueIndex;
				EditorGUILayout.HelpBox(GetModeDescription(mode), MessageType.Info);
			});

			// Assets
			DrawSection("Assets", () =>
			{
				GameRenderMode mode = (GameRenderMode)serializedObject.FindProperty("renderMode").enumValueIndex;

				if (mode == GameRenderMode.LineRenderer2D || mode == GameRenderMode.SpriteMesh2D)
				{
					EditorGUILayout.LabelField("2D Assets", EditorStyles.miniBoldLabel);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("arrowHeadSprite"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("SpriteHeadMat"));

					if (mode == GameRenderMode.SpriteMesh2D)
					{
						EditorGUILayout.PropertyField(serializedObject.FindProperty("arrowBodySprite"));
						EditorGUILayout.PropertyField(serializedObject.FindProperty("arrowBodySpriteAlt"));
						EditorGUILayout.PropertyField(serializedObject.FindProperty("arrowCornerSprite"));
						EditorGUILayout.PropertyField(serializedObject.FindProperty("connectorSprite"));
					}

					EditorGUILayout.PropertyField(serializedObject.FindProperty("StarSprite"));
					EditorGUILayout.Space(5);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("blocker2D"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("hole2D"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("portal2D"));
				}
				else
				{
					EditorGUILayout.LabelField("3D Assets", EditorStyles.miniBoldLabel);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("arrow3DMaterial"));

					EditorGUILayout.Space(5);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("blocker3D"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("hole3D"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("portal3D"));

					EditorGUILayout.Space(8);
					EditorGUILayout.LabelField("Snake Head / Tail", EditorStyles.miniBoldLabel);
					SerializedProperty enableProp = serializedObject.FindProperty("enableSnakeHeadTail");
					EditorGUILayout.PropertyField(enableProp, new GUIContent("Enable Head & Tail"));
					if (enableProp.boolValue)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField(serializedObject.FindProperty("snakeHeadPrefab"), new GUIContent("Head Prefab"));
						EditorGUILayout.PropertyField(serializedObject.FindProperty("snakeTailPrefab"), new GUIContent("Tail Prefab"));
						EditorGUI.indentLevel--;
					}
				}

				EditorGUILayout.Space(10);
				EditorGUILayout.LabelField("Grid Visualization", EditorStyles.miniBoldLabel);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("gridDotPrefab"), new GUIContent("Grid Dot Prefab"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("gridDotColor"), new GUIContent("Grid Dot Color"));
			});

			EditorGUILayout.PropertyField(serializedObject.FindProperty("_cameraController"));

			EditorGUILayout.Space(10);

			// Settings
			DrawSection("Settings", () =>
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("mainCamera"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraPadding"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("gridRoot"));
			});

			serializedObject.ApplyModifiedProperties();
		}

		void DrawSection(string title, System.Action content)
		{
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			content();
			EditorGUI.indentLevel--;
		}

		string GetModeDescription(GameRenderMode mode)
		{
			switch (mode)
			{
				case GameRenderMode.LineRenderer2D:
					return "✓ 2D LineRenderer\n✓ Best Performance\n✓ Mobile Friendly\n✓ Click on arrow body to move";

				case GameRenderMode.SpriteMesh2D:
					return "✓ 2D Sprite Segments\n✓ Custom Art Support\n✓ Good Performance\n✓ Click on arrow body to move";

				case GameRenderMode.Mesh3D:
					return "✓ 3D Tube Meshes\n✓ Lighting & Shadows\n✓ Premium Look\n✓ Click on arrow to move";

				case GameRenderMode.LineRenderer3D:
					return "✓ 3D LineRenderer\n✓ Same as 2D but in 3D space\n✓ Good Performance\n✓ Click on arrow to move";

				default:
					return "";
			}
		}
	}
}
