#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CubeStackPuzzle
{
    /// <summary>
    /// Custom Inspector for CubeBoard.
    /// Shows preview controls and layout summary.
    /// </summary>
    [CustomEditor(typeof(CubeBoard))]
    public class CubeBoardEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CubeBoard board = (CubeBoard)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Board Preview Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
                if (GUILayout.Button("🔄  Refresh Preview", GUILayout.Height(36)))
                {
                    board.RefreshEditorPreview();
                    SceneView.RepaintAll();
                }

                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("🗑  Clear Preview", GUILayout.Height(36)))
                {
                    board.ClearBoard();
                    SceneView.RepaintAll();
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(4);

            if (board.previewInEditor)
            {
                EditorGUILayout.HelpBox(
                    "✅ Preview is ON — cubes are visible in the Scene view.\n" +
                    "Preview objects use HideFlags.DontSave and will NOT be saved.\n" +
                    "⚠️ Disable before entering Play Mode to avoid duplicates.",
                    MessageType.Warning
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Preview is OFF.\n" +
                    "Tick 'Preview In Editor' to see cubes in the Scene view.",
                    MessageType.Info
                );
            }

            // Layout summary
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Board Layout Summary", EditorStyles.boldLabel);

            if (board.CubeStacks != null && board.CubeStacks.Length > 0)
            {
                int totalCubes = 0;
                foreach (var stack in board.CubeStacks)
                {
                    if (stack == null) continue;
                    EditorGUILayout.LabelField(
                        $"  • {stack.color}: {stack.cubeCount} cubes");
                    totalCubes += stack.cubeCount;
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"  Total Cubes: {totalCubes}", EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("  No CubeStacks defined.");
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Cubes fill LEFT → RIGHT, BOTTOM → TOP within board width.\n" +
                "When a row is full, the next row starts above.\n" +
                "Cubes never overlap or exceed the board width.\n" +
                "Columns may contain cubes of different colors.",
                MessageType.Info
            );
        }
    }
}
#endif
