using UnityEngine;
using Framework;
using Framework.Core;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Environment Data", menuName = "Data/Environment Data")]
    public class EnvironmentData : ScriptableObject
    {
		[Header("2D Assets")]
		public Sprite arrowHeadSprite;
		public Sprite arrowBodySprite;
		public Sprite arrowBodySpriteAlt;
		public Sprite arrowCornerSprite;
		public Sprite connectorSprite;

		[Header("3D Assets")]
		public Material arrow3DMaterial;
		public GameObject blocker3D;
		public GameObject hole3D;
		public GameObject portal3D;

		[Header("Snake Head / Tail (Mesh3D only)")]
		public bool enableSnakeHeadTail = false;
		public GameObject snakeHeadPrefab;
		public GameObject snakeTailPrefab;

		[Header("2D Prefabs")]
		public GameObject blocker2D;
		public GameObject hole2D;
		public GameObject portal2D;

		[Header("Grid Visualization")]
		public GameObject gridDotPrefab;
		public Color gridDotColor = new Color(0.3f, 0.3f, 0.3f);
	}
}
