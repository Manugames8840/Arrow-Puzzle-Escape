using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using Framework;

public class LevelEditor : EditorWindow
{
	// ================= CONFIG =================
	const float BASE_CELL_SIZE = 40f;
	const float LEFT_PANEL_WIDTH = 280f;
	const float RIGHT_PANEL_WIDTH = 260f;
	const float MIN_CELL_SIZE = 15f;
	const float MAX_GRID_HEIGHT = 600f;
	const int PAGE_SIZE = 10;

	// ================= GRID =================
	int width = 8;
	int height = 8;
	float timeLimit = 60f;
	float currentCellSize = BASE_CELL_SIZE;

	// ================= TOOLS =================
	enum PaintTool { Arrow, Blocker, Hole, Portal, Eraser }
	PaintTool currentTool = PaintTool.Arrow;

	// ================= DATA =================
	[SerializeField]
	List<Color> colorPalette = new List<Color>() {
		new Color(1f, 0.4f, 0.4f), // Red
		new Color(0.4f, 1f, 0.4f), // Green
		new Color(0.4f, 0.6f, 1f), // Blue
		new Color(1f, 1f, 0.4f),   // Yellow
		new Color(1f, 0.4f, 1f)    // Magenta
	};
	Dictionary<Vector2Int, ArrowPath> arrows = new();
	HashSet<Vector2Int> blockers = new();
	HashSet<Vector2Int> holes = new();
	HashSet<Vector2Int> portals = new();
    // Swap selection indices for level preview grid
    int swapFirstIndex = -1;
    int swapSecondIndex = -1;

	// ================= SELECTION =================
	ArrowPath selectedArrow = null;
	Vector2Int selectedArrowTail = Vector2Int.zero;

	// ================= LEVEL DATABASE =================
	LevelDatabase database;
	LevelData selectedLevel;
	int selectedLevelIndex = -1;
	Vector2 levelListScroll; // Scroll for level list
    Vector2 levelPreviewScroll; // Scroll for level preview panel
	int currentPage = 0;

	// ================= DRAW STATE =================
	bool drawingArrow;
	List<Vector2Int> currentPath = new();

	Rect lastGridRect;

	// ================= MENU =================
	[MenuItem("Tools/Arrow Out/Level Editor")]
	static void Open()
	{
		GetWindow<LevelEditor>("Level Editor");
	}

	void OnEnable()
	{
		wantsMouseMove = true; // Enable mouse move events for better drawing
		LoadDatabase();
	}

	void OnDisable()
	{
		foreach (var tex in texCache.Values)
		{
			if (tex != null) DestroyImmediate(tex);
		}
		texCache.Clear();
	}

	void LoadDatabase()
	{
		// Try to find existing database
		string[] guids = AssetDatabase.FindAssets("t:LevelDatabase");
		if (guids.Length > 0)
		{
			string path = AssetDatabase.GUIDToAssetPath(guids[0]);
			database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(path);
		}

		// Create new database if none exists
		if (database == null)
		{
			database = ScriptableObject.CreateInstance<LevelDatabase>();
			AssetDatabase.CreateAsset(database, "Assets/LevelDatabase.asset");
			AssetDatabase.SaveAssets();
		}
	}

	// ================= GUI =================
    void OnGUI()
    {
        GUILayout.BeginHorizontal();

        DrawLevelListPanel();
        DrawLevelGridPanel(); // New grid view of levels
        DrawGridPanel();
        DrawRightPanel();

		GUILayout.EndHorizontal();

		// SAFETY: cancel draw if mouse leaves window
		if (Event.current.type == EventType.MouseLeaveWindow)
			CancelArrowDraw();
	}

	// ================= LEVEL LIST PANEL =================
	void DrawLevelListPanel()
	{
		GUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH));

		GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
		headerStyle.fontSize = 14;
		headerStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
		GUILayout.Label("Levels", headerStyle);

		GUILayout.Space(5);

		EditorGUI.BeginChangeCheck();
		database = (LevelDatabase)EditorGUILayout.ObjectField("Database", database, typeof(LevelDatabase), false);
		if (EditorGUI.EndChangeCheck())
		{
			selectedLevel = null;
			selectedLevelIndex = -1;
			selectedArrow = null;
			currentPage = 0;
		}

		if (database == null)
		{
			EditorGUILayout.HelpBox("No Level Database assigned!", MessageType.Warning);
			GUILayout.EndVertical();
			return;
		}

		GUILayout.Space(10);

		int totalLevels = database.levels.Count;
		int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalLevels / PAGE_SIZE));
		currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

		int startIndex = currentPage * PAGE_SIZE;
		int endIndex = Mathf.Min(startIndex + PAGE_SIZE, totalLevels);

		// Scrollable level list (only current page)
		levelListScroll = GUILayout.BeginScrollView(
			levelListScroll,
			false,
			true,
			GUILayout.ExpandHeight(true),
			GUILayout.MinHeight(200)
		);

		for (int i = startIndex; i < endIndex; i++)
		{
			if (database.levels[i] == null) continue;

			bool isSelected = selectedLevelIndex == i;

			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.padding = new RectOffset(10, 10, 8, 8);
			buttonStyle.fontSize = 12;
			buttonStyle.fontStyle = FontStyle.Bold;

			if (isSelected)
			{
				buttonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.8f));
				buttonStyle.normal.textColor = Color.white;
			}
			else
			{
				buttonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f));
				buttonStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
				buttonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f));
				buttonStyle.hover.textColor = Color.white;
			}

			if (GUILayout.Button($"  Level {i + 1}", buttonStyle, GUILayout.Height(35), GUILayout.ExpandWidth(true)))
			{
				SelectLevel(i);
			}

			GUILayout.Space(2);
		}

		GUILayout.EndScrollView();

		// ===== PAGINATION BAR =====
		GUILayout.Space(8);

		GUIStyle navBtn = new GUIStyle(GUI.skin.button);
		navBtn.fontSize = 12;
		navBtn.fontStyle = FontStyle.Bold;
		navBtn.padding = new RectOffset(4, 4, 4, 4);

		GUIStyle pageLabel = new GUIStyle(EditorStyles.boldLabel);
		pageLabel.alignment = TextAnchor.MiddleCenter;
		pageLabel.fontSize = 12;
		pageLabel.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

		GUILayout.BeginHorizontal();

		// << First page
		GUI.enabled = currentPage > 0;
		if (GUILayout.Button("<<", navBtn, GUILayout.Width(30), GUILayout.Height(24)))
		{
			currentPage = 0;
			levelListScroll = Vector2.zero;
			Repaint();
		}

		// < Prev page
		if (GUILayout.Button("<", navBtn, GUILayout.Width(25), GUILayout.Height(24)))
		{
			currentPage--;
			levelListScroll = Vector2.zero;
			Repaint();
		}

		GUI.enabled = true;

		// Page indicator: "1–10 / 47"
		int displayStart = totalLevels == 0 ? 0 : startIndex + 1;
		int displayEnd = endIndex;
		GUILayout.Label($"{displayStart}–{displayEnd} / {totalLevels}", pageLabel, GUILayout.ExpandWidth(true), GUILayout.Height(24));

		// > Next page
		GUI.enabled = currentPage < totalPages - 1;
		if (GUILayout.Button(">", navBtn, GUILayout.Width(25), GUILayout.Height(24)))
		{
			currentPage++;
			levelListScroll = Vector2.zero;
			Repaint();
		}

		// >> Last page
		if (GUILayout.Button(">>", navBtn, GUILayout.Width(30), GUILayout.Height(24)))
		{
			currentPage = totalPages - 1;
			levelListScroll = Vector2.zero;
			Repaint();
		}

		GUI.enabled = true;
		GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // ===== NEW LEVEL BUTTON =====
        GUILayout.Space(8);
        // ----- Swap Buttons -----
        GUI.enabled = swapFirstIndex >= 0 && swapSecondIndex >= 0;
        if (GUILayout.Button("Swap Selected Levels", GUILayout.Height(30)))
        {
            SwapSelectedLevels();
        }
        GUI.enabled = true;
        if (GUILayout.Button("Unselect All", GUILayout.Height(30)))
        {
            UnselectSwapLevels();
        }

		GUIStyle newLevelButtonStyle = new GUIStyle(GUI.skin.button);
		newLevelButtonStyle.fontStyle = FontStyle.Bold;
		newLevelButtonStyle.fontSize = 13;
		newLevelButtonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.3f));
		newLevelButtonStyle.normal.textColor = Color.white;
		newLevelButtonStyle.hover.background = MakeTex(2, 2, new Color(0.25f, 0.7f, 0.35f));

		if (GUILayout.Button("+ New Level", newLevelButtonStyle, GUILayout.Height(35)))
		{
			CreateNewLevel();
		}

		GUILayout.EndVertical();
	}

	void SelectLevel(int index)
	{
		selectedLevelIndex = index;
		selectedLevel = database.levels[index];
		selectedArrow = null;
		LoadLevelData(selectedLevel);
	}

	void LoadLevelData(LevelData level)
	{
		width = level.width;
		height = level.height;
		timeLimit = level.Duration;

		arrows.Clear();
		blockers.Clear();
		holes.Clear();
		portals.Clear();

		// Load arrows
		foreach (var arrowPath in level.arrowPaths)
		{
			if (arrowPath.body != null && arrowPath.body.Count >= 2)
			{
				Vector2Int tail = arrowPath.body[^1];
				ArrowPath newArrow = new ArrowPath
				{
					body = new List<Vector2Int>(arrowPath.body),
					color = arrowPath.color // Load the color
				};
				arrows[tail] = newArrow;
			}
		}

		// Load other elements
		foreach (var b in level.blockers) blockers.Add(b);
		foreach (var h in level.holes) holes.Add(h);
		foreach (var p in level.portals) portals.Add(p);

		CalculateCellSize();
		Repaint();
	}

	void CreateNewLevel()
	{
		LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
		newLevel.width = 8;
		newLevel.height = 8;
		newLevel.Duration = 60f;
		newLevel.arrowPaths = new List<ArrowPath>();
		newLevel.blockers = new List<Vector2Int>();
		newLevel.holes = new List<Vector2Int>();
		newLevel.portals = new List<Vector2Int>();

		string path = EditorUtility.SaveFilePanelInProject(
			"Save New Level",
			$"Level_{database.levels.Count + 1:00}",
			"asset",
			"Save Arrow Out Level"
		);

		if (!string.IsNullOrEmpty(path))
		{
			AssetDatabase.CreateAsset(newLevel, path);
			AssetDatabase.SaveAssets();

			database.levels.Add(newLevel);
			EditorUtility.SetDirty(database);
			AssetDatabase.SaveAssets();

			SelectLevel(database.levels.Count - 1);
		}
	}

	void ClearEditor()
	{
		arrows.Clear();
		blockers.Clear();
		holes.Clear();
		portals.Clear();
		selectedArrow = null;
		width = 8;
		height = 8;
		timeLimit = 60f;
		CalculateCellSize();
		Repaint();
	}

	void SaveCurrentLevel()
	{
		if (selectedLevel == null) return;

		selectedLevel.width = width;
		selectedLevel.height = height;
		selectedLevel.Duration = timeLimit;

		selectedLevel.arrowPaths = new List<ArrowPath>(arrows.Values);
		selectedLevel.blockers = new List<Vector2Int>(blockers);
		selectedLevel.holes = new List<Vector2Int>(holes);
		selectedLevel.portals = new List<Vector2Int>(portals);

		EditorUtility.SetDirty(selectedLevel);
		AssetDatabase.SaveAssets();
	}

	void ApplyPaletteToArrows()
	{
		if (colorPalette == null || colorPalette.Count == 0) return;
		if (arrows == null || arrows.Count == 0) return;

		int i = 0;
		foreach (var arrow in arrows.Values)
		{
			arrow.color = colorPalette[i % colorPalette.Count];
			i++;
		}
		SaveCurrentLevel();
		Repaint();
	}

	void ApplyPaletteToAllLevels()
	{
		if (colorPalette == null || colorPalette.Count == 0) return;
		if (database == null || database.levels == null) return;

		foreach (var level in database.levels)
		{
			if (level == null || level.arrowPaths == null) continue;

			int i = 0;
			foreach (var arrow in level.arrowPaths)
			{
				arrow.color = colorPalette[i % colorPalette.Count];
				i++;
			}
			EditorUtility.SetDirty(level);
		}
		
		AssetDatabase.SaveAssets();

		if (selectedLevel != null)
		{
			LoadLevelData(selectedLevel);
		}

		Repaint();
	}

	// ================= AUTO-SCALING =================
	void CalculateCellSize()
	{
		// Calculate available space for grid
		float availableWidth = position.width - (LEFT_PANEL_WIDTH * 2) - RIGHT_PANEL_WIDTH - 40;
		float availableHeight = position.height - 100;

		// Calculate cell size to fit both dimensions
		float cellSizeForWidth = width > 0 ? availableWidth / width : BASE_CELL_SIZE;
		float cellSizeForHeight = height > 0 ? availableHeight / height : BASE_CELL_SIZE;

		// Use the smaller value to ensure grid fits
		currentCellSize = Mathf.Min(cellSizeForWidth, cellSizeForHeight);
		currentCellSize = Mathf.Max(currentCellSize, 2f); // Lower minimum size to allow large grids to fit
		currentCellSize = Mathf.Min(currentCellSize, BASE_CELL_SIZE); // Cap at base size
	}

	// ================= GRID PANEL =================
	void DrawGridPanel()
	{
		GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

		// Header styling
		GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
		headerStyle.fontSize = 13;
		headerStyle.alignment = TextAnchor.MiddleCenter;

		if (selectedLevel != null)
		{
			headerStyle.normal.textColor = new Color(0.5f, 0.8f, 1f);
			GUILayout.Label($"Editing: {selectedLevel.name}", headerStyle);
		}
		else
		{
			headerStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
			GUILayout.Label("No level selected", headerStyle);
		}

		GUILayout.Space(5);

		// Recalculate cell size when grid dimensions change
		if (selectedLevel != null)
		{
			CalculateCellSize();
		}

		float gridWidth = width * currentCellSize;
		float gridHeight = height * currentCellSize;

		// Center the grid using flexible space
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		Rect gridRect = GUILayoutUtility.GetRect(
			gridWidth,
			gridHeight,
			GUILayout.ExpandHeight(false),
			GUILayout.ExpandWidth(false)
		);

		lastGridRect = gridRect;

		DrawGrid(gridRect);
		DrawStaticCells(gridRect);
		DrawSavedArrows(gridRect);
		DrawCurrentPreview(gridRect);

		if (selectedLevel != null)
		{
			HandleInput(gridRect);
		}

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();

		GUILayout.EndVertical();
	}

	void DrawGrid(Rect rect)
	{
		// Darker grid background
		EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

		Handles.BeginGUI();
		// Lighter grid lines for better visibility
		Handles.color = new Color(0.35f, 0.35f, 0.35f);

		for (int x = 0; x <= width; x++)
			Handles.DrawLine(
				new Vector2(rect.x + x * currentCellSize, rect.y),
				new Vector2(rect.x + x * currentCellSize, rect.y + height * currentCellSize)
			);

		for (int y = 0; y <= height; y++)
			Handles.DrawLine(
				new Vector2(rect.x, rect.y + y * currentCellSize),
				new Vector2(rect.x + width * currentCellSize, rect.y + y * currentCellSize)
			);

		Handles.EndGUI();
	}

	// ================= INPUT =================
	void HandleInput(Rect gridRect)
	{
		Event e = Event.current;

		// Check if mouse is actually within grid bounds
		if (!gridRect.Contains(e.mousePosition))
		{
			if (drawingArrow && e.type == EventType.MouseUp)
			{
				drawingArrow = false;
				currentPath.Clear();
				Repaint();
			}
			return;
		}

		Vector2Int cell = GetCellFromMouse(gridRect, e.mousePosition);

		// STRICT BOUNDS CHECK - ignore input outside grid
		if (!IsValid(cell))
		{
			if (drawingArrow && e.type == EventType.MouseUp)
			{
				drawingArrow = false;
				currentPath.Clear();
				Repaint();
			}
			return;
		}

		// ================= ARROW =================
		if (currentTool == PaintTool.Arrow)
		{
			// Check if clicking on existing arrow to select or delete
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				// Check if clicking on a tail to delete
				if (arrows.ContainsKey(cell))
				{
					// Right click or double click could delete, but let's keep it simple
					// Click on tail = delete the arrow
					arrows.Remove(cell);
					if (selectedArrow != null && selectedArrowTail == cell)
					{
						selectedArrow = null;
					}
					SaveCurrentLevel();
					Repaint();
					e.Use();
					return;
				}

				// Check if clicking on any part of an arrow to select it
				ArrowPath clickedArrow = FindArrowAtCell(cell);
				if (clickedArrow != null)
				{
					// Select this arrow
					selectedArrow = clickedArrow;
					selectedArrowTail = FindArrowTail(clickedArrow);
					Repaint();
					e.Use();
					return;
				}

				// Start drawing new arrow
				drawingArrow = true;
				currentPath.Clear();
				currentPath.Add(cell); // HEAD placeholder
				Repaint();
				e.Use();
			}

			if ((e.type == EventType.MouseDrag || e.type == EventType.MouseMove) && drawingArrow)
			{
				if (currentPath.Count > 0)
				{
					Vector2Int last = currentPath[^1];
					if (IsNeighbour(last, cell) && !currentPath.Contains(cell))
					{
						// Only add the cell to the path if the cursor is near the center of the cell
						Vector2 cellCenter = GetCellCenter(gridRect, cell);
						float distanceToCenter = Vector2.Distance(e.mousePosition, cellCenter);
						float threshold = currentCellSize * 0.35f; // Must be within inner 70% of the cell

						if (distanceToCenter <= threshold)
						{
							currentPath.Add(cell);
							Repaint();
						}
					}
				}
				e.Use();
			}

			if (e.type == EventType.MouseUp && e.button == 0 && drawingArrow)
			{
				drawingArrow = false;

				if (currentPath.Count >= 2)
				{
					SaveArrow(currentPath);
					SaveCurrentLevel();
				}

				currentPath.Clear();
				Repaint();
				e.Use();
			}
		}
		// ================= OTHER TOOLS (TOGGLE MODE) =================
		else if (e.type == EventType.MouseDown && e.button == 0)
		{
			ApplySingleCellToolToggle(cell);
			SaveCurrentLevel();
			Repaint();
			e.Use();
		}

		// Request repaint on mouse move to show hover feedback
		if (e.type == EventType.MouseMove)
		{
			Repaint();
		}
	}

	void CancelArrowDraw()
	{
		drawingArrow = false;
		currentPath.Clear();
		Repaint();
	}

	// ================= ARROW SELECTION =================
	ArrowPath FindArrowAtCell(Vector2Int cell)
	{
		foreach (var kvp in arrows)
		{
			if (kvp.Value.body.Contains(cell))
			{
				return kvp.Value;
			}
		}
		return null;
	}

	Vector2Int FindArrowTail(ArrowPath arrow)
	{
		foreach (var kvp in arrows)
		{
			if (kvp.Value == arrow)
			{
				return kvp.Key;
			}
		}
		return Vector2Int.zero;
	}

	// ================= SAVE ARROW =================
	void SaveArrow(List<Vector2Int> path)
	{
		Vector2Int tail = path[^1]; // Mouse UP = T
		ArrowPath newArrow = new ArrowPath
		{
			body = new List<Vector2Int>(path),
			color = Color.white // Default color
		};
		arrows[tail] = newArrow;

		// Auto-select the newly created arrow
		selectedArrow = newArrow;
		selectedArrowTail = tail;
	}

	// ================= SINGLE CELL TOOLS (TOGGLE MODE) =================
	void ApplySingleCellToolToggle(Vector2Int cell)
	{
		// Remove arrow if present (tail position)
		arrows.Remove(cell);
		if (selectedArrow != null && selectedArrowTail == cell)
		{
			selectedArrow = null;
		}

		switch (currentTool)
		{
			case PaintTool.Blocker:
				if (blockers.Contains(cell))
					blockers.Remove(cell);
				else
				{
					holes.Remove(cell);
					portals.Remove(cell);
					blockers.Add(cell);
				}
				break;

			case PaintTool.Hole:
				if (holes.Contains(cell))
					holes.Remove(cell);
				else
				{
					blockers.Remove(cell);
					portals.Remove(cell);
					holes.Add(cell);
				}
				break;

			case PaintTool.Portal:
				if (portals.Contains(cell))
					portals.Remove(cell);
				else
				{
					blockers.Remove(cell);
					holes.Remove(cell);
					portals.Add(cell);
				}
				break;

			case PaintTool.Eraser:
				blockers.Remove(cell);
				holes.Remove(cell);
				portals.Remove(cell);
				break;
		}
	}

	// ================= DRAW STATIC =================
	void DrawStaticCells(Rect gridRect)
	{
		foreach (var b in blockers) DrawLetter(gridRect, b, "B", new Color(1f, 0.4f, 0.4f));
		foreach (var h in holes) DrawLetter(gridRect, h, "H", new Color(0.4f, 0.6f, 1f));
		foreach (var p in portals) DrawLetter(gridRect, p, "P", new Color(0.4f, 1f, 0.5f));
	}

	// ================= DRAW ARROWS =================
	void DrawSavedArrows(Rect gridRect)
	{
		foreach (var kvp in arrows)
		{
			ArrowPath arrow = kvp.Value;
			bool isSelected = (arrow == selectedArrow);

			// Use arrow's color, with highlight if selected
			Color arrowColor = arrow.color;
			if (isSelected)
			{
				// Add a bright outline or glow effect for selected arrow
				DrawPath(gridRect, arrow.body, Color.yellow, 8f); // Thicker yellow outline
			}
			DrawPath(gridRect, arrow.body, arrowColor, 4f);
		}
	}

	void DrawCurrentPreview(Rect gridRect)
	{
		if (drawingArrow && currentPath.Count > 1)
			DrawPath(gridRect, currentPath, new Color(0.3f, 0.7f, 1f, 0.9f), 4f);
	}

	void DrawPath(Rect gridRect, List<Vector2Int> path, Color color, float thickness = 6f)
	{
		Handles.BeginGUI();
		Handles.color = color;

		for (int i = 0; i < path.Count - 1; i++)
			Handles.DrawAAPolyLine(
				thickness,
				GetCellCenter(gridRect, path[i]),
				GetCellCenter(gridRect, path[i + 1])
			);

		// HEAD (Mouse Down)
		DrawLetter(gridRect, path[0], "H", new Color(1f, 1f, 0.3f));

		// TAIL (Mouse Up) - clickable to delete
		DrawLetterWithBackground(gridRect, path[^1], "T", new Color(1f, 0.5f, 0.3f), new Color(0.3f, 0.3f, 0.3f, 0.7f));

		Handles.EndGUI();
	}

	void DrawLetterWithBackground(Rect gridRect, Vector2Int cell, string text, Color textColor, Color bgColor)
	{
		Vector2 center = GetCellCenter(gridRect, cell);
		float letterSize = Mathf.Min(24, currentCellSize * 0.6f);
		Rect letterRect = new Rect(center - Vector2.one * (letterSize / 2), new Vector2(letterSize, letterSize));

		// Draw background circle/box
		EditorGUI.DrawRect(letterRect, bgColor);

		GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
		{
			alignment = TextAnchor.MiddleCenter,
			fontSize = Mathf.Max(10, (int)(currentCellSize * 0.35f)),
			normal = { textColor = textColor }
		};

		GUI.Label(letterRect, text, style);
	}

	void DrawLetter(Rect gridRect, Vector2Int cell, string text, Color color)
	{
		float letterSize = Mathf.Min(24, currentCellSize * 0.6f);

		GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
		{
			alignment = TextAnchor.MiddleCenter,
			fontSize = Mathf.Max(10, (int)(currentCellSize * 0.35f)),
			normal = { textColor = color }
		};

		GUI.Label(
			new Rect(GetCellCenter(gridRect, cell) - Vector2.one * (letterSize / 2), new Vector2(letterSize, letterSize)),
			text,
			style
		);
	}

	    // ================= RIGHT PANEL =================
    void DrawRightPanel()
    {
		GUILayout.BeginVertical(GUILayout.Width(RIGHT_PANEL_WIDTH));

		GUI.enabled = selectedLevel != null;

		        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
		        int newWidth = EditorGUILayout.IntField("Width", width);
        int newHeight = EditorGUILayout.IntField("Height", height);
        timeLimit = EditorGUILayout.FloatField("Time Limit", timeLimit);

        if (EditorGUI.EndChangeCheck() && selectedLevel != null)
        {
			width = newWidth;
			height = newHeight;
			CalculateCellSize();
			SaveCurrentLevel();
		}

		GUILayout.Space(10);

		// ================= COLOR PALETTE =================
		GUILayout.Label("Default Color Palette", EditorStyles.boldLabel);
		
		for (int i = 0; i < colorPalette.Count; i++)
		{
			GUILayout.BeginHorizontal();
			colorPalette[i] = EditorGUILayout.ColorField(colorPalette[i]);
			if (GUILayout.Button("X", GUILayout.Width(25)))
			{
				colorPalette.RemoveAt(i);
				i--;
			}
			GUILayout.EndHorizontal();
		}
		
		if (GUILayout.Button("+ Add Color"))
		{
			colorPalette.Add(Color.white);
		}
		
		GUILayout.BeginHorizontal();
		GUI.enabled = colorPalette.Count > 0 && selectedLevel != null;
		if (GUILayout.Button("Apply to Level", GUILayout.Height(20)))
		{
			ApplyPaletteToArrows();
		}
		
		GUI.enabled = colorPalette.Count > 0 && database != null;
		if (GUILayout.Button("Apply to All Levels", GUILayout.Height(20)))
		{
			if (EditorUtility.DisplayDialog("Apply Palette to All Levels", 
				"Are you sure you want to overwrite arrow colors in all levels with this palette?", "Yes", "Cancel"))
			{
				ApplyPaletteToAllLevels();
			}
		}
		GUI.enabled = selectedLevel != null;
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		// ================= ARROW PROPERTIES =================
		if (selectedArrow != null)
		{
			GUILayout.Label("Selected Arrow", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			Color newColor = EditorGUILayout.ColorField("Arrow Color", selectedArrow.color);

			if (EditorGUI.EndChangeCheck())
			{
				selectedArrow.color = newColor;
				SaveCurrentLevel();
				Repaint();
			}

			if (GUILayout.Button("Deselect Arrow"))
			{
				selectedArrow = null;
				Repaint();
			}

			GUILayout.Space(10);
		}

		GUILayout.Label("Tools", EditorStyles.boldLabel);
		currentTool = (PaintTool)GUILayout.Toolbar(
			(int)currentTool,
			new[] { "Arrow", "Blocker", "Hole", "Portal", "Eraser" }
		);

        GUILayout.Space(10);

        // ----- Swap Buttons (already added in list panel) -----
		GUIStyle helpStyle = new GUIStyle(EditorStyles.helpBox);
		helpStyle.fontSize = 11;
		helpStyle.wordWrap = true;
		helpStyle.padding = new RectOffset(8, 8, 8, 8);

		GUILayout.Label("How to Use:", EditorStyles.boldLabel);

		if (currentTool == PaintTool.Arrow)
		{
			EditorGUILayout.HelpBox("• Draw: Click & drag to create arrow path\n• Select: Click on any cell of an arrow\n• Delete: Click on 'T' (tail) to remove arrow\n• Color: Select arrow to change its color above", MessageType.Info);
		}
		else if (currentTool == PaintTool.Eraser)
		{
			EditorGUILayout.HelpBox("• Click on any cell to clear it", MessageType.Info);
		}
		else
		{
			EditorGUILayout.HelpBox("• Click to place/remove element\n• Click again to toggle off", MessageType.Info);
		}

		GUILayout.Space(10);
		GUILayout.Label("Quick Clear", EditorStyles.boldLabel);

		if (GUILayout.Button("Clear Arrows"))
		{
			arrows.Clear();
			selectedArrow = null;
			SaveCurrentLevel();
		}
		if (GUILayout.Button("Clear Blockers"))
		{
			blockers.Clear();
			SaveCurrentLevel();
		}
		if (GUILayout.Button("Clear Holes"))
		{
			holes.Clear();
			SaveCurrentLevel();
		}
		if (GUILayout.Button("Clear Portals"))
		{
			portals.Clear();
			SaveCurrentLevel();
		}
		if (GUILayout.Button("Clear All"))
		{
			ClearEditor();
			SaveCurrentLevel();
		}

		GUI.enabled = true;

		// ================= PLAY LEVEL BUTTON =================
		GUILayout.Space(15);

		GUI.enabled = selectedLevel != null && !EditorApplication.isPlaying;

		GUIStyle playButtonStyle = new GUIStyle(GUI.skin.button);
		playButtonStyle.fontStyle = FontStyle.Bold;
		playButtonStyle.fontSize = 14;
		playButtonStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.5f, 0.9f));
		playButtonStyle.normal.textColor = Color.white;
		playButtonStyle.hover.background = MakeTex(2, 2, new Color(0.15f, 0.6f, 1f));
		playButtonStyle.hover.textColor = Color.white;
		playButtonStyle.active.background = MakeTex(2, 2, new Color(0.08f, 0.4f, 0.75f));
		playButtonStyle.active.textColor = Color.white;

		if (GUILayout.Button("▶  Play This Level", playButtonStyle, GUILayout.Height(40)))
		{
			PlaySelectedLevel();
		}

		if (EditorApplication.isPlaying)
		{
			GUIStyle stopStyle = new GUIStyle(GUI.skin.button);
			stopStyle.fontStyle = FontStyle.Bold;
			stopStyle.fontSize = 12;
			stopStyle.normal.background = MakeTex(2, 2, new Color(0.8f, 0.2f, 0.2f));
			stopStyle.normal.textColor = Color.white;
			stopStyle.hover.background = MakeTex(2, 2, new Color(0.9f, 0.25f, 0.25f));

			if (GUILayout.Button("■  Stop Playing", stopStyle, GUILayout.Height(30)))
			{
				EditorApplication.isPlaying = false;
			}
		}

		GUI.enabled = true;
		GUILayout.EndVertical();
	}

	void PlaySelectedLevel()
	{
		if (selectedLevel == null || selectedLevelIndex < 0)
		{
			EditorUtility.DisplayDialog("No Level Selected", "Please select a level to play.", "OK");
			return;
		}

		// Save any pending changes before playing
		SaveCurrentLevel();

		// Use the framework's editor-time method to persist the level index to disk.
		// This survives domain reload + scene transitions.
		// selectedLevelIndex is the index in database.levels list (what the game expects).
		ActiveSession.SetEditorLevelIndex(selectedLevelIndex);

		// Also persist the asset path for SpecificVariationToLoad as backup
		string assetPath = AssetDatabase.GetAssetPath(selectedLevel);
		SessionState.SetString(LevelEditorPlayModeHandler.PLAY_LEVEL_KEY, assetPath);

		Debug.Log($"[LevelEditor] Playing level: {selectedLevel.name} (index: {selectedLevelIndex})");

		// Enter Play Mode
		EditorApplication.isPlaying = true;
	}

	// ================= LEVEL GRID PREVIEW =================
	void DrawLevelGridPanel()
    {
        if (database == null) return;
        int totalLevelCount = database.levels.Count;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalLevelCount / PAGE_SIZE));
        int currentPageClamped = Mathf.Clamp(currentPage, 0, totalPages - 1);
        int startIndex = currentPageClamped * PAGE_SIZE;
        int endIndex = Mathf.Min(startIndex + PAGE_SIZE, totalLevelCount);
        float previewSize = 150f; // larger preview size
        GUIStyle selectedStyle = new GUIStyle(GUI.skin.box);
        selectedStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.8f));
        // Scroll view for vertical list
        levelPreviewScroll = GUILayout.BeginScrollView(levelPreviewScroll, GUILayout.Width(LEFT_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        for (int idx = startIndex; idx < endIndex; idx++)
        {
            LevelData lvl = database.levels[idx];
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // Container for preview and optional border
            Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            // Highlight if selected for swap
            if (idx == swapFirstIndex || idx == swapSecondIndex)
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.5f, 0.8f, 0.4f));
            DrawLevelPreview(lvl, previewRect);
            // Click handling for swap selection
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && previewRect.Contains(e.mousePosition))
            {
                e.Use();
                RegisterSwapSelection(idx);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
        }
        GUILayout.EndScrollView();
    }

    void DrawLevelPreview(LevelData level, Rect rect)
    {
        // Simple rendering: draw arrows onto small grid
        float cellSize = Mathf.Min(rect.width / level.width, rect.height / level.height);
        Handles.BeginGUI();
        // Background
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
        // Draw arrows
        foreach (var arrowPath in level.arrowPaths)
        {
            if (arrowPath.body == null || arrowPath.body.Count < 2) continue;
            for (int i = 0; i < arrowPath.body.Count - 1; i++)
            {
                Vector2 p1 = rect.position + new Vector2(arrowPath.body[i].x * cellSize + cellSize / 2, (level.height - 1 - arrowPath.body[i].y) * cellSize + cellSize / 2);
                Vector2 p2 = rect.position + new Vector2(arrowPath.body[i + 1].x * cellSize + cellSize / 2, (level.height - 1 - arrowPath.body[i + 1].y) * cellSize + cellSize / 2);
                Handles.color = arrowPath.color;
                Handles.DrawAAPolyLine(2f, p1, p2);
            }
        }
        Handles.EndGUI();
    }

    void RegisterSwapSelection(int idx)
    {
        if (swapFirstIndex == idx) return;
        if (swapFirstIndex < 0)
        {
            swapFirstIndex = idx;
        }
        else if (swapSecondIndex < 0 && idx != swapFirstIndex)
        {
            swapSecondIndex = idx;
        }
        else
        {
            // Reset selection to new first
            swapFirstIndex = idx;
            swapSecondIndex = -1;
        }
        Repaint();
    }

    void UnselectSwapLevels()
    {
        swapFirstIndex = -1;
        swapSecondIndex = -1;
        Repaint();
    }

    void SwapSelectedLevels()
    {
        if (swapFirstIndex < 0 || swapSecondIndex < 0) return;
        var temp = database.levels[swapFirstIndex];
        database.levels[swapFirstIndex] = database.levels[swapSecondIndex];
        database.levels[swapSecondIndex] = temp;
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        UnselectSwapLevels();
        Repaint();
    }

    // ================= HELPERS =================
	Vector2Int GetCellFromMouse(Rect grid, Vector2 mouse)
	{
		float relativeX = mouse.x - grid.x;
		float relativeY = mouse.y - grid.y;

		// Return invalid cell if outside grid bounds (no clamping!)
		if (relativeX < 0 || relativeY < 0 ||
			relativeX >= width * currentCellSize ||
			relativeY >= height * currentCellSize)
		{
			return new Vector2Int(-1, -1); // Invalid cell marker
		}

		int x = Mathf.FloorToInt(relativeX / currentCellSize);
		int y = height - 1 - Mathf.FloorToInt(relativeY / currentCellSize);

		return new Vector2Int(x, y);
	}

	Vector2 GetCellCenter(Rect grid, Vector2Int cell)
	{
		return new Vector2(
			grid.x + (cell.x + 0.5f) * currentCellSize,
			grid.y + (height - cell.y - 0.5f) * currentCellSize
		);
	}

	bool IsValid(Vector2Int c)
	{
		return c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;
	}

	bool IsNeighbour(Vector2Int a, Vector2Int b)
	{
		int dx = Mathf.Abs(a.x - b.x);
		int dy = Mathf.Abs(a.y - b.y);
		return (dx <= 1 && dy <= 1) && (dx != 0 || dy != 0);
	}

	// Helper to create colored textures for UI
	Dictionary<Color, Texture2D> texCache = new Dictionary<Color, Texture2D>();

	Texture2D MakeTex(int width, int height, Color col)
	{
		if (texCache.TryGetValue(col, out Texture2D tex) && tex != null)
			return tex;

		Color[] pix = new Color[width * height];
		for (int i = 0; i < pix.Length; i++)
			pix[i] = col;

		Texture2D result = new Texture2D(width, height);
		result.SetPixels(pix);
		result.Apply();
		
		texCache[col] = result;
		return result;
	}
}


/// <summary>
/// Survives domain reload: injects the selected level into LevelController
/// before any MonoBehaviour Awake/Start runs.
/// [InitializeOnLoad] static constructor runs during domain reload,
/// which is BEFORE scene load and BEFORE any MonoBehaviour.
/// </summary>
[InitializeOnLoad]
public static class LevelEditorPlayModeHandler
{
	public const string PLAY_LEVEL_KEY = "LevelEditor_PlayLevelPath";

	static LevelEditorPlayModeHandler()
	{
		// Inject the level immediately during domain reload (before scene load).
		// This ensures LevelController.SpecificVariationToLoad is set
		// BEFORE LevelController.Init() runs in Awake/Start.
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
			InjectLevelIfPending();
		}

		// Register callback to clean up when exiting play mode
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	private static void InjectLevelIfPending()
	{
		string levelPath = SessionState.GetString(PLAY_LEVEL_KEY, "");
		if (string.IsNullOrEmpty(levelPath)) return;

		LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(levelPath);
		if (level != null)
		{
			LevelController.SpecificVariationToLoad = level;
			Debug.Log($"[LevelEditor] Injected level for play: {level.name}");
		}
		else
		{
			Debug.LogWarning($"[LevelEditor] Could not load level at path: {levelPath}");
		}

		// Clear after consuming so normal play works next time
		SessionState.EraseString(PLAY_LEVEL_KEY);
	}

	private static void OnPlayModeStateChanged(PlayModeStateChange state)
	{
		// Fallback: also try injecting when entering play mode
		if (state == PlayModeStateChange.EnteredPlayMode)
		{
			InjectLevelIfPending();
		}

		// Clean up when exiting play mode
		if (state == PlayModeStateChange.ExitingPlayMode)
		{
			SessionState.EraseString(PLAY_LEVEL_KEY);
		}
	}
}