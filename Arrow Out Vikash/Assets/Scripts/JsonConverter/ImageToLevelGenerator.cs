using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

#if UNITY_EDITOR
public class ImageToLevelGenerator : EditorWindow
{
	private Texture2D sourceImage;
	private int gridWidth = 20;
	private int gridHeight = 20;
	private float detectionThreshold = 0.5f;
	private bool useColorSeparation = true;
	private float colorTolerance = 0.3f;
	private int minArrowLength = 3;
	private int maxArrowLength = 30;
	private string levelName = "Generated Level";
	private int levelNumber = 1;
	private string savePath = "Assets/ArrowOut/Levels";

	private Texture2D previewTexture;
	private List<List<Vector2Int>> detectedArrows = new List<List<Vector2Int>>();
	private bool hasGenerated = false;
	private Vector2 scrollPos;

	[MenuItem("Tools/Arrow Out/Image to Level Generator")]
	static void ShowWindow()
	{
		var window = GetWindow<ImageToLevelGenerator>("Image to Level");
		window.minSize = new Vector2(520, 750);
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
		DrawImageInput();
		GUILayout.Space(10);
		DrawSettings();
		GUILayout.Space(10);
		DrawPreview();
		GUILayout.Space(10);
		DrawGenerateButton();
		GUILayout.Space(10);

		if (hasGenerated)
		{
			DrawSaveSection();
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
		EditorGUILayout.LabelField("🎨 Advanced Image to Level Generator", headerStyle);
	}

	void DrawInstructions()
	{
		EditorGUILayout.HelpBox(
			"IMPROVED ALGORITHM:\n" +
			"✓ Detects multiple colors as separate arrows\n" +
			"✓ Traces line patterns (not just blobs)\n" +
			"✓ Breaks complex shapes into multiple arrows\n" +
			"✓ Better for mazes, letters, and decorative borders\n\n" +
			"TIP: Enable 'Use Color Separation' for multi-colored images!",
			MessageType.Info
		);
	}

	void DrawImageInput()
	{
		EditorGUILayout.LabelField("Image Input", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		sourceImage = (Texture2D)EditorGUILayout.ObjectField(
			"Source Image",
			sourceImage,
			typeof(Texture2D),
			false,
			GUILayout.Height(100)
		);

		if (sourceImage != null)
		{
			EditorGUILayout.LabelField($"Image Size: {sourceImage.width}x{sourceImage.height}");
		}

		EditorGUI.indentLevel--;
	}

	void DrawSettings()
	{
		EditorGUILayout.LabelField("Detection Settings", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 10, 30);
		gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 10, 30);

		GUILayout.Space(5);

		useColorSeparation = EditorGUILayout.Toggle("Use Color Separation", useColorSeparation);

		if (useColorSeparation)
		{
			EditorGUI.indentLevel++;
			colorTolerance = EditorGUILayout.Slider("Color Tolerance", colorTolerance, 0.1f, 0.8f);
			EditorGUILayout.HelpBox("Groups similar colors together. Lower = more strict", MessageType.None);
			EditorGUI.indentLevel--;
		}
		else
		{
			detectionThreshold = EditorGUILayout.Slider("Brightness Threshold", detectionThreshold, 0.1f, 0.9f);
		}

		GUILayout.Space(5);

		minArrowLength = EditorGUILayout.IntSlider("Min Arrow Length", minArrowLength, 2, 10);
		maxArrowLength = EditorGUILayout.IntSlider("Max Arrow Length", maxArrowLength, 10, 50);

		EditorGUI.indentLevel--;
	}

	void DrawPreview()
	{
		EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

		GUI.enabled = sourceImage != null;
		if (GUILayout.Button("🔍 Analyze Image", GUILayout.Height(35)))
		{
			AnalyzeImage();
		}
		GUI.enabled = true;

		if (previewTexture != null)
		{
			GUILayout.Space(10);
			GUILayout.Label("Detected Pattern:");

			float previewSize = 400f;
			Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize);
			EditorGUI.DrawPreviewTexture(previewRect, previewTexture);

			GUILayout.Space(5);
			EditorGUILayout.LabelField($"Arrows Detected: {detectedArrows.Count}");

			if (detectedArrows.Count == 0)
			{
				EditorGUILayout.HelpBox("No arrows detected! Try adjusting settings.", MessageType.Warning);
			}
			else if (detectedArrows.Count > 100)
			{
				EditorGUILayout.HelpBox("Too many arrows! Increase grid size or min length.", MessageType.Warning);
			}
		}
	}

	void DrawGenerateButton()
	{
		GUI.enabled = detectedArrows.Count > 0 && detectedArrows.Count <= 100;

		Color originalColor = GUI.backgroundColor;
		GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);

		if (GUILayout.Button("✨ Create Level", GUILayout.Height(40)))
		{
			CreateLevel();
		}

		GUI.backgroundColor = originalColor;
		GUI.enabled = true;
	}

	void DrawSaveSection()
	{
		GUILayout.Space(10);
		EditorGUILayout.LabelField("Level Details", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		levelName = EditorGUILayout.TextField("Level Name", levelName);
		levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
		savePath = EditorGUILayout.TextField("Save Path", savePath);

		EditorGUI.indentLevel--;
	}

	void AnalyzeImage()
	{
		if (sourceImage == null)
		{
			EditorUtility.DisplayDialog("Error", "Please assign a source image first!", "OK");
			return;
		}

		Texture2D readableTexture = MakeTextureReadable(sourceImage);
		if (readableTexture == null)
		{
			EditorUtility.DisplayDialog(
				"Error",
				"Could not read texture. Enable 'Read/Write' in import settings!",
				"OK"
			);
			return;
		}

		// Detect patterns using improved algorithm
		if (useColorSeparation)
		{
			detectedArrows = DetectArrowsByColor(readableTexture);
		}
		else
		{
			detectedArrows = DetectArrowsByBrightness(readableTexture);
		}

		// Filter by length
		detectedArrows = detectedArrows.Where(a =>
			a.Count >= minArrowLength && a.Count <= maxArrowLength
		).ToList();

		previewTexture = GeneratePreviewTexture();
		hasGenerated = false;

		Debug.Log($"Analysis complete. Found {detectedArrows.Count} arrows.");
	}

	Texture2D MakeTextureReadable(Texture2D texture)
	{
		try
		{
			texture.GetPixel(0, 0);
			return texture;
		}
		catch
		{
			RenderTexture tmp = RenderTexture.GetTemporary(
				texture.width, texture.height, 0,
				RenderTextureFormat.Default, RenderTextureReadWrite.Linear
			);

			Graphics.Blit(texture, tmp);
			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = tmp;

			Texture2D readable = new Texture2D(texture.width, texture.height);
			readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
			readable.Apply();

			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(tmp);

			return readable;
		}
	}

	// NEW: Detect arrows by color groups
	List<List<Vector2Int>> DetectArrowsByColor(Texture2D texture)
	{
		// Sample image and group by color
		Dictionary<Color, List<Vector2Int>> colorGroups = new Dictionary<Color, List<Vector2Int>>();

		for (int y = 0; y < gridHeight; y++)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				float sampleX = (x + 0.5f) / gridWidth;
				float sampleY = (y + 0.5f) / gridHeight;
				Color pixelColor = texture.GetPixelBilinear(sampleX, sampleY);

				// Skip background (very bright colors)
				float brightness = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;
				if (brightness > 0.9f) continue;

				// Find or create color group
				Color groupColor = FindColorGroup(pixelColor, colorGroups.Keys.ToList());
				if (groupColor == Color.clear)
				{
					groupColor = pixelColor;
					colorGroups[groupColor] = new List<Vector2Int>();
				}

				colorGroups[groupColor].Add(new Vector2Int(x, y));
			}
		}

		Debug.Log($"Found {colorGroups.Count} color groups");

		// Convert each color group to arrows
		List<List<Vector2Int>> allArrows = new List<List<Vector2Int>>();

		foreach (var group in colorGroups.Values)
		{
			if (group.Count < minArrowLength) continue;

			// Break group into linear paths
			List<List<Vector2Int>> groupArrows = BreakIntoLinearPaths(group);
			allArrows.AddRange(groupArrows);
		}

		return allArrows;
	}

	Color FindColorGroup(Color color, List<Color> existingGroups)
	{
		foreach (Color group in existingGroups)
		{
			float distance = Mathf.Abs(color.r - group.r) +
						   Mathf.Abs(color.g - group.g) +
						   Mathf.Abs(color.b - group.b);

			if (distance < colorTolerance)
			{
				return group;
			}
		}
		return Color.clear;
	}

	// NEW: Break pixel groups into linear paths
	List<List<Vector2Int>> BreakIntoLinearPaths(List<Vector2Int> pixels)
	{
		List<List<Vector2Int>> paths = new List<List<Vector2Int>>();
		HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(pixels);

		while (remaining.Count >= minArrowLength)
		{
			// Find an endpoint (pixel with only 1 neighbor)
			Vector2Int start = FindEndpoint(remaining);
			if (start == new Vector2Int(-1, -1))
			{
				// No endpoint, just pick any pixel
				start = remaining.First();
			}

			// Trace path from this start point
			List<Vector2Int> path = TracePath(start, remaining);

			if (path.Count >= minArrowLength && path.Count <= maxArrowLength)
			{
				paths.Add(path);
			}

			// Remove used pixels
			foreach (var pixel in path)
			{
				remaining.Remove(pixel);
			}
		}

		return paths;
	}

	Vector2Int FindEndpoint(HashSet<Vector2Int> pixels)
	{
		foreach (Vector2Int pixel in pixels)
		{
			int neighborCount = CountNeighbors(pixel, pixels);
			if (neighborCount == 1) return pixel; // Endpoint
		}
		return new Vector2Int(-1, -1);
	}

	int CountNeighbors(Vector2Int pixel, HashSet<Vector2Int> pixels)
	{
		int count = 0;
		Vector2Int[] neighbors = new Vector2Int[]
		{
			new Vector2Int(pixel.x + 1, pixel.y),
			new Vector2Int(pixel.x - 1, pixel.y),
			new Vector2Int(pixel.x, pixel.y + 1),
			new Vector2Int(pixel.x, pixel.y - 1)
		};

		foreach (Vector2Int neighbor in neighbors)
		{
			if (pixels.Contains(neighbor)) count++;
		}
		return count;
	}

	List<Vector2Int> TracePath(Vector2Int start, HashSet<Vector2Int> pixels)
	{
		List<Vector2Int> path = new List<Vector2Int>();
		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

		Vector2Int current = start;
		path.Add(current);
		visited.Add(current);

		while (path.Count < maxArrowLength)
		{
			// Find next unvisited neighbor
			Vector2Int next = FindNextNeighbor(current, pixels, visited);

			if (next == new Vector2Int(-1, -1))
				break; // No more neighbors

			path.Add(next);
			visited.Add(next);
			current = next;
		}

		// Extend to exit grid
		ExtendPathToExit(path);

		return path;
	}

	Vector2Int FindNextNeighbor(Vector2Int current, HashSet<Vector2Int> pixels, HashSet<Vector2Int> visited)
	{
		Vector2Int[] neighbors = new Vector2Int[]
		{
			new Vector2Int(current.x + 1, current.y),
			new Vector2Int(current.x - 1, current.y),
			new Vector2Int(current.x, current.y + 1),
			new Vector2Int(current.x, current.y - 1)
		};

		foreach (Vector2Int neighbor in neighbors)
		{
			if (pixels.Contains(neighbor) && !visited.Contains(neighbor))
			{
				return neighbor;
			}
		}

		return new Vector2Int(-1, -1);
	}

	void ExtendPathToExit(List<Vector2Int> path)
	{
		if (path.Count < 2) return;

		Vector2Int head = path[0];
		Vector2Int next = path[1];
		Vector2Int direction = new Vector2Int(
			Mathf.Clamp(head.x - next.x, -1, 1),
			Mathf.Clamp(head.y - next.y, -1, 1)
		);

		// Extend until outside grid
		Vector2Int current = head;
		for (int i = 0; i < 5; i++) // Max 5 extensions
		{
			current += direction;
			if (current.x < 0 || current.x >= gridWidth ||
				current.y < 0 || current.y >= gridHeight)
			{
				break;
			}
			path.Insert(0, current);
		}
	}

	// Fallback: brightness-based detection
	List<List<Vector2Int>> DetectArrowsByBrightness(Texture2D texture)
	{
		bool[,] grid = new bool[gridWidth, gridHeight];

		for (int y = 0; y < gridHeight; y++)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				float sampleX = (x + 0.5f) / gridWidth;
				float sampleY = (y + 0.5f) / gridHeight;
				Color pixelColor = texture.GetPixelBilinear(sampleX, sampleY);

				float brightness = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;
				grid[x, y] = brightness < (1f - detectionThreshold);
			}
		}

		List<Vector2Int> allPixels = new List<Vector2Int>();
		for (int y = 0; y < gridHeight; y++)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				if (grid[x, y])
				{
					allPixels.Add(new Vector2Int(x, y));
				}
			}
		}

		return BreakIntoLinearPaths(allPixels);
	}

	Texture2D GeneratePreviewTexture()
	{
		int texSize = 512;
		Texture2D preview = new Texture2D(texSize, texSize);

		Color[] pixels = new Color[texSize * texSize];
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = new Color(0.95f, 0.95f, 0.95f);
		}
		preview.SetPixels(pixels);

		DrawGrid(preview, texSize);

		Color[] arrowColors = new Color[]
		{
			new Color(1f, 0.3f, 0.3f),    // Red
            new Color(0.3f, 0.7f, 1f),    // Blue
            new Color(0.3f, 1f, 0.5f),    // Green
            new Color(1f, 0.8f, 0.2f),    // Yellow
            new Color(1f, 0.4f, 1f),      // Magenta
            new Color(0.4f, 1f, 1f),      // Cyan
            new Color(1f, 0.6f, 0.2f),    // Orange
            new Color(0.8f, 0.3f, 1f)     // Purple
        };

		for (int i = 0; i < detectedArrows.Count; i++)
		{
			Color arrowColor = arrowColors[i % arrowColors.Length];
			DrawArrow(preview, detectedArrows[i], arrowColor, texSize);
		}

		preview.Apply();
		return preview;
	}

	void DrawGrid(Texture2D texture, int texSize)
	{
		int cellWidth = texSize / gridWidth;
		int cellHeight = texSize / gridHeight;
		Color gridColor = new Color(0.8f, 0.8f, 0.8f);

		for (int x = 0; x <= gridWidth; x++)
		{
			int pixelX = x * cellWidth;
			for (int y = 0; y < texSize; y++)
			{
				if (pixelX < texSize)
					texture.SetPixel(pixelX, y, gridColor);
			}
		}

		for (int y = 0; y <= gridHeight; y++)
		{
			int pixelY = y * cellHeight;
			for (int x = 0; x < texSize; x++)
			{
				if (pixelY < texSize)
					texture.SetPixel(x, pixelY, gridColor);
			}
		}
	}

	void DrawArrow(Texture2D texture, List<Vector2Int> path, Color color, int texSize)
	{
		int cellWidth = texSize / gridWidth;
		int cellHeight = texSize / gridHeight;

		for (int i = 0; i < path.Count; i++)
		{
			Vector2Int gridPos = path[i];

			int centerX = gridPos.x * cellWidth + cellWidth / 2;
			int centerY = gridPos.y * cellHeight + cellHeight / 2;
			int radius = Mathf.Min(cellWidth, cellHeight) / 3;

			// Draw circle
			for (int dy = -radius; dy <= radius; dy++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					if (dx * dx + dy * dy <= radius * radius)
					{
						int px = centerX + dx;
						int py = centerY + dy;
						if (px >= 0 && px < texSize && py >= 0 && py < texSize)
						{
							texture.SetPixel(px, py, color);
						}
					}
				}
			}

			// Draw line to next
			if (i < path.Count - 1)
			{
				Vector2Int next = path[i + 1];
				DrawLine(
					texture,
					centerX, centerY,
					next.x * cellWidth + cellWidth / 2,
					next.y * cellHeight + cellHeight / 2,
					color,
					3
				);
			}
		}
	}

	void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
	{
		int dx = Mathf.Abs(x1 - x0);
		int dy = Mathf.Abs(y1 - y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx - dy;

		while (true)
		{
			for (int ty = -thickness; ty <= thickness; ty++)
			{
				for (int tx = -thickness; tx <= thickness; tx++)
				{
					int px = x0 + tx;
					int py = y0 + ty;
					if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
					{
						texture.SetPixel(px, py, color);
					}
				}
			}

			if (x0 == x1 && y0 == y1) break;

			int e2 = 2 * err;
			if (e2 > -dy)
			{
				err -= dy;
				x0 += sx;
			}
			if (e2 < dx)
			{
				err += dx;
				y0 += sy;
			}
		}
	}

	void CreateLevel()
	{
		if (detectedArrows.Count == 0)
		{
			EditorUtility.DisplayDialog("Error", "No arrows detected!", "OK");
			return;
		}

		LevelData level = ScriptableObject.CreateInstance<LevelData>();

		level.width = gridWidth;
		level.height = gridHeight;
		level.Duration = 60f + (detectedArrows.Count * 20f);

		level.arrowPaths = new List<ArrowPath>();
		foreach (var arrowPath in detectedArrows)
		{
			ArrowPath arrow = new ArrowPath();
			arrow.body = new List<Vector2Int>(arrowPath);
			level.arrowPaths.Add(arrow);
		}

		level.blockers = new List<Vector2Int>();
		level.holes = new List<Vector2Int>();
		level.portals = new List<Vector2Int>();

		if (!System.IO.Directory.Exists(savePath))
		{
			System.IO.Directory.CreateDirectory(savePath);
		}

		string assetPath = $"{savePath}/Level_{levelNumber:D3}_{levelName.Replace(" ", "_")}.asset";
		AssetDatabase.CreateAsset(level, assetPath);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		hasGenerated = true;

		EditorUtility.DisplayDialog(
			"Success!",
			$"Level created!\n\nArrows: {detectedArrows.Count}\nSaved to: {assetPath}",
			"OK"
		);

		EditorGUIUtility.PingObject(level);
	}
}
#endif