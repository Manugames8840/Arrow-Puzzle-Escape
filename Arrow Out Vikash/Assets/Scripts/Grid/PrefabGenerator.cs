using UnityEngine;
using UnityEditor;

namespace ArrowOut
{
#if UNITY_EDITOR
	/// <summary>
	/// Generates sample prefabs and sprites for Arrow Out
	/// Compatible with new SOLID architecture
	/// </summary>
	public class PrefabGenerator : MonoBehaviour
	{
		[MenuItem("Tools/Arrow Out/Generate Sample Assets")]
		static void GenerateSampleAssets()
		{
			string folderPath = "Assets/ArrowOut/Prefabs";

			// Create folders
			if (!AssetDatabase.IsValidFolder("Assets/ArrowOut"))
				AssetDatabase.CreateFolder("Assets", "ArrowOut");

			if (!AssetDatabase.IsValidFolder(folderPath))
				AssetDatabase.CreateFolder("Assets/ArrowOut", "Prefabs");

			Debug.Log("🎨 Generating sample assets...");

			// Generate sprites for arrows
			GenerateArrowSprites(folderPath);

			// Generate 2D prefabs
			Generate2DPrefabs(folderPath);

			// Generate 3D prefabs
			Generate3DPrefabs(folderPath);

			// Generate materials
			GenerateMaterials(folderPath);

			CreateTrainCoachSprite();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log($"✅ Sample assets generated at: {folderPath}");
			EditorUtility.DisplayDialog(
				"Assets Generated!",
				$"Sample prefabs and sprites created at:\n{folderPath}\n\n" +
				"Assets include:\n" +
				"• Arrow head & body sprites\n" +
				"• 2D prefabs (Blocker, Hole, Portal)\n" +
				"• 3D prefabs (Blocker, Hole, Portal)\n" +
				"• Sample materials",
				"OK"
			);
		}

		static void GenerateArrowSprites(string path)
		{
			Debug.Log("  Creating arrow sprites...");

			// Arrow Head Sprite
			Texture2D headTex = CreateArrowHeadTexture();
			SaveTextureAsSprite(headTex, $"{path}/ArrowHead2D.png", "Arrow Head");

			// Arrow Body Sprite
			Texture2D bodyTex = CreateArrowBodyTexture();
			SaveTextureAsSprite(bodyTex, $"{path}/ArrowBody2D.png", "Arrow Body");

			Debug.Log("  ✓ Arrow sprites created");
		}

		static void Generate2DPrefabs(string path)
		{
			Debug.Log("  Creating 2D prefabs...");

			// Blocker 2D
			GameObject blocker2D = GameObject.CreatePrimitive(PrimitiveType.Quad);
			blocker2D.name = "Blocker2D";
			blocker2D.transform.localScale = Vector3.one * 0.8f;

			MeshRenderer blockerMR = blocker2D.GetComponent<MeshRenderer>();
			Material blockerMat = new Material(Shader.Find("Sprites/Default"));
			blockerMat.color = new Color(1f, 0.3f, 0.3f);
			blockerMR.material = blockerMat;
			blockerMR.sortingOrder = 0;

			PrefabUtility.SaveAsPrefabAsset(blocker2D, $"{path}/Blocker2D.prefab");
			DestroyImmediate(blocker2D);

			// Hole 2D
			GameObject hole2D = GameObject.CreatePrimitive(PrimitiveType.Quad);
			hole2D.name = "Hole2D";
			hole2D.transform.localScale = Vector3.one * 0.7f;

			MeshRenderer holeMR = hole2D.GetComponent<MeshRenderer>();
			Material holeMat = new Material(Shader.Find("Sprites/Default"));
			holeMat.color = new Color(0.2f, 0.2f, 0.2f);
			holeMR.material = holeMat;
			holeMR.sortingOrder = 0;

			PrefabUtility.SaveAsPrefabAsset(hole2D, $"{path}/Hole2D.prefab");
			DestroyImmediate(hole2D);

			// Portal 2D
			GameObject portal2D = GameObject.CreatePrimitive(PrimitiveType.Quad);
			portal2D.name = "Portal2D";
			portal2D.transform.localScale = Vector3.one * 0.9f;

			MeshRenderer portalMR = portal2D.GetComponent<MeshRenderer>();
			Material portalMat = new Material(Shader.Find("Sprites/Default"));
			portalMat.color = new Color(0.3f, 1f, 0.5f);
			portalMR.material = portalMat;
			portalMR.sortingOrder = 0;

			PrefabUtility.SaveAsPrefabAsset(portal2D, $"{path}/Portal2D.prefab");
			DestroyImmediate(portal2D);

			Debug.Log("  ✓ 2D prefabs created");
		}

		static Texture2D CreateTrainCoachSprite()
		{
			int size = 128;
			Texture2D tex = new Texture2D(size, size);
			Color[] pixels = new Color[size * size];

			// Fill with transparent
			for (int i = 0; i < pixels.Length; i++)
				pixels[i] = Color.clear;

			// Draw train coach rectangle
			for (int y = 40; y < 88; y++)
			{
				for (int x = 20; x < 108; x++)
				{
					// Main body
					if (y > 45 && y < 83 && x > 25 && x < 103)
						pixels[y * size + x] = new Color(0.6f, 0.3f, 0.1f); // Brown

					// Windows
					if ((x > 35 && x < 50 || x > 60 && x < 75 || x > 85 && x < 100) &&
						y > 55 && y < 75)
						pixels[y * size + x] = new Color(0.7f, 0.9f, 1f); // Light blue

					// Outline
					if (y == 45 || y == 83 || x == 25 || x == 103)
						pixels[y * size + x] = Color.black;
				}
			}

			tex.SetPixels(pixels);
			tex.Apply();
			return tex;
		}

		static void Generate3DPrefabs(string path)
		{
			Debug.Log("  Creating 3D prefabs...");

			// Blocker 3D
			GameObject blocker3D = GameObject.CreatePrimitive(PrimitiveType.Cube);
			blocker3D.name = "Blocker3D";
			blocker3D.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

			MeshRenderer blockerMR = blocker3D.GetComponent<MeshRenderer>();
			Material blockerMat = new Material(Shader.Find("Standard"));
			blockerMat.color = new Color(1f, 0.3f, 0.3f);
			blockerMat.SetFloat("_Metallic", 0.2f);
			blockerMat.SetFloat("_Glossiness", 0.5f);
			blockerMR.material = blockerMat;

			PrefabUtility.SaveAsPrefabAsset(blocker3D, $"{path}/Blocker3D.prefab");
			DestroyImmediate(blocker3D);

			// Hole 3D
			GameObject hole3D = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			hole3D.name = "Hole3D";
			hole3D.transform.localScale = new Vector3(0.7f, 0.05f, 0.7f);

			MeshRenderer holeMR = hole3D.GetComponent<MeshRenderer>();
			Material holeMat = new Material(Shader.Find("Standard"));
			holeMat.color = new Color(0.1f, 0.1f, 0.1f);
			holeMat.SetFloat("_Metallic", 0f);
			holeMat.SetFloat("_Glossiness", 0.3f);
			holeMR.material = holeMat;

			PrefabUtility.SaveAsPrefabAsset(hole3D, $"{path}/Hole3D.prefab");
			DestroyImmediate(hole3D);

			// Portal 3D with emission
			GameObject portal3D = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			portal3D.name = "Portal3D";
			portal3D.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);

			MeshRenderer portalMR = portal3D.GetComponent<MeshRenderer>();
			Material portalMat = new Material(Shader.Find("Standard"));
			portalMat.color = new Color(0.3f, 1f, 0.5f);
			portalMat.SetFloat("_Metallic", 0.5f);
			portalMat.SetFloat("_Glossiness", 0.8f);
			portalMat.EnableKeyword("_EMISSION");
			portalMat.SetColor("_EmissionColor", new Color(0.3f, 1f, 0.5f) * 0.5f);
			portalMR.material = portalMat;

			PrefabUtility.SaveAsPrefabAsset(portal3D, $"{path}/Portal3D.prefab");
			DestroyImmediate(portal3D);

			Debug.Log("  ✓ 3D prefabs created");
		}

		static void GenerateMaterials(string path)
		{
			Debug.Log("  Creating materials...");

			// Arrow 3D Material
			Material arrowMat = new Material(Shader.Find("Standard"));
			arrowMat.name = "Arrow3DMaterial";
			arrowMat.color = new Color(0.3f, 0.7f, 1f);
			arrowMat.SetFloat("_Metallic", 0.3f);
			arrowMat.SetFloat("_Glossiness", 0.6f);
			AssetDatabase.CreateAsset(arrowMat, $"{path}/Arrow3DMaterial.mat");

			Debug.Log("  ✓ Materials created");
		}

		static Texture2D CreateArrowHeadTexture()
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

			// Draw arrow head pointing right (triangle/chevron)
			int centerY = size / 2;

			// Main triangle
			for (int x = 30; x < 98; x++)
			{
				float progress = (x - 30) / 68f;
				int height = (int)(progress * 40);

				for (int y = centerY - height; y <= centerY + height; y++)
				{
					if (y >= 0 && y < size)
						tex.SetPixel(x, y, white);
				}
			}

			// Make it more arrow-like with sharper point
			for (int x = 98; x < 105; x++)
			{
				int height = 40 - (x - 98) * 6;
				for (int y = centerY - height; y <= centerY + height; y++)
				{
					if (y >= 0 && y < size)
						tex.SetPixel(x, y, white);
				}
			}

			tex.Apply();
			return tex;
		}

		static Texture2D CreateArrowBodyTexture()
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

			// Draw horizontal rectangle (body segment)
			int centerY = size / 2;
			int thickness = 20;
			int startX = 15;
			int endX = 113;

			for (int x = startX; x < endX; x++)
			{
				for (int y = centerY - thickness / 2; y <= centerY + thickness / 2; y++)
				{
					tex.SetPixel(x, y, white);
				}
			}

			// Add slight taper on both ends for better connection
			for (int i = 0; i < 5; i++)
			{
				int reduceHeight = i / 2;

				// Left taper
				for (int y = centerY - thickness / 2 + reduceHeight;
					 y <= centerY + thickness / 2 - reduceHeight; y++)
				{
					tex.SetPixel(startX + i, y, white);
				}

				// Right taper
				for (int y = centerY - thickness / 2 + reduceHeight;
					 y <= centerY + thickness / 2 - reduceHeight; y++)
				{
					tex.SetPixel(endX - i - 1, y, white);
				}
			}

			tex.Apply();
			return tex;
		}

		static void SaveTextureAsSprite(Texture2D texture, string path, string assetName)
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
				importer.alphaIsTransparency = true;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.SaveAndReimport();

				Debug.Log($"    → {assetName} sprite");
			}
		}
	}
#endif
}
