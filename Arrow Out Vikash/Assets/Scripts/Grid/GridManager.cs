using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Frameork;
using Framework;
using UnityEngine;

namespace ArrowOut
{
	public class GridManager : MonoBehaviour
	{
		public static GridManager Instance { get; private set; }

		[Header("Rendering")]
		public GameRenderMode renderMode = GameRenderMode.LineRenderer2D;
		public CameraMode cameraMode = CameraMode.Orthographic2D;
		private GameConditionType GameConstrain = GameConditionType.Star;

		[Header("2D Assets")]
		public Material SpriteHeadMat;
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

		[Header("Camera")]
		public Camera mainCamera;
		public float cameraPadding = 1f;

		[Header("Root")]
		public Transform gridRoot;

		public bool AllArrowCleared => _levelRepresentation.AllArrowComplete;

		public Sprite StarSprite;

		[SerializeField] private TransformWaveAnimation _transformWaveAnimation;

		[Header("Level")]
		private LevelData CurrentLevel;

		private ArrowFactory arrowFactory;
		private ICoordinateConverter coordinateConverter;
		private IMovementValidator movementValidator;

		private Dictionary<Vector2Int, Arrow> arrows = new Dictionary<Vector2Int, Arrow>();
		private Dictionary<Vector2Int, GameObject> blockers = new Dictionary<Vector2Int, GameObject>();
		private Dictionary<Vector2Int, GameObject> holes = new Dictionary<Vector2Int, GameObject>();
		private Dictionary<Vector2Int, GameObject> portals = new Dictionary<Vector2Int, GameObject>();

		private int totalArrows;
		private int completedArrows;

		private float timer;
		private int _moves = 3;
		private bool levelFinished;
		public bool previewLinesEnabled;
		private List<Arrow> _wrongArrow = new List<Arrow>();
		private List<Transform> _gridDots = new List<Transform>();
		private Dictionary<Vector2Int, GameObject> _gridDotMap = new Dictionary<Vector2Int, GameObject>();
		public CubeRowManager cubeRowManager;
		private Coroutine _waitCall;

		private LevelRepresentation _levelRepresentation => LevelController.LevelRepresentation;

		public void Init()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
		}

		public void StartLevel(LevelData levelData, GameConditionType gameConstrain = GameConditionType.Star)
		{
			CurrentLevel = levelData;
			GameConstrain = gameConstrain;

			InitializeFactories();
			SetupCamera();
			InitLevel();
			MyEventArgs.GameControllerEvents.OnScreenOpen.AddListener(OnScreenClose);
		}

		private void OnScreenClose(bool active)
		{
			gridRoot.gameObject.SetActive(!active);
		}

		private void InitializeFactories()
		{
			arrowFactory = new ArrowFactory(renderMode, this);
			coordinateConverter = arrowFactory.CreateCoordinateConverter(mainCamera);
			movementValidator = arrowFactory.CreateMovementValidator();
		}

		void InitLevel()
		{
			if (CurrentLevel == null)
			{
				Debug.LogError("No LevelData assigned!");
				return;
			}

			totalArrows = CurrentLevel.arrowPaths.Count;
			completedArrows = 0;
			timer = CurrentLevel.Duration;

			ClearGrid();

			GenerateGridDots();
			GenerateBlockers();
			GenerateHoles();
			GeneratePortals();
			GenerateArrows();

			totalArrows = CurrentLevel.arrowPaths.Count;
			completedArrows = 0;
			timer = CurrentLevel.Duration;

			_levelRepresentation.SetTotalArrow(arrows.Count);
		}

		void ClearGrid()
		{
			foreach (Transform t in gridRoot)
				Destroy(t.gameObject);

			arrows.Clear();
			blockers.Clear();
			holes.Clear();
			portals.Clear();
			_gridDots.Clear();
			_gridDotMap.Clear();
		}

		void SetupCamera()
		{
			if (mainCamera == null)
				mainCamera = Camera.main;

			if (CurrentLevel == null) return;

			Vector3 gridCenter = GetGridCenter();

			switch (cameraMode)
			{
				case CameraMode.Orthographic2D:
					SetupOrthographicCamera(gridCenter);
					break;

				case CameraMode.Perspective3D:
					SetupPerspectiveCamera(gridCenter);
					break;
			}
		}

		private Vector3 GetGridCenter()
		{
			float centerX = (CurrentLevel.width - 1) * 0.5f;
			float centerY = (CurrentLevel.height - 1) * 0.5f;

			if (renderMode == GameRenderMode.Mesh3D || renderMode == GameRenderMode.LineRenderer3D)
				return new Vector3(centerX, 0, centerY);
			else
				return new Vector3(centerX, centerY, 0);
		}

		private void SetupOrthographicCamera(Vector3 gridCenter)
		{
			mainCamera.orthographic = true;

			if (renderMode == GameRenderMode.Mesh3D || renderMode == GameRenderMode.LineRenderer3D)
			{
				mainCamera.transform.position = new Vector3(gridCenter.x, 10, gridCenter.z);
				mainCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
			}
			else
			{
				mainCamera.transform.position = new Vector3(gridCenter.x, gridCenter.y, -10);
				mainCamera.transform.rotation = Quaternion.identity;
			}

			float gridWidth = CurrentLevel.width + cameraPadding;
			float gridHeight = CurrentLevel.height + cameraPadding;
			float screenRatio = (float)Screen.width / Screen.height;
			float targetRatio = gridWidth / gridHeight;

			if (screenRatio >= targetRatio)
				mainCamera.orthographicSize = gridHeight / 2f;
			else
				mainCamera.orthographicSize = gridWidth / (2f * screenRatio);
		}

		private void SetupPerspectiveCamera(Vector3 gridCenter)
		{
			mainCamera.orthographic = false;
			mainCamera.fieldOfView = 60f;

			float distance = Mathf.Max(CurrentLevel.width, CurrentLevel.height) * 0.7f + 10f;
			Vector3 offset = Quaternion.Euler(30, 45, 0) * (Vector3.back * distance);
			mainCamera.transform.position = gridCenter + offset;
			mainCamera.transform.LookAt(gridCenter);
		}

		void GenerateArrows()
		{
			foreach (var arrowPath in CurrentLevel.arrowPaths)
			{
				if (arrowPath.body == null || arrowPath.body.Count < 2)
					continue;

				CreateArrow(arrowPath);
			}

			LevelController.LevelRepresentation.Arrows = arrows;
		}

		void GenerateGridDots()
		{
			if (gridDotPrefab == null) return;

			var gridDotParent = Instantiate(new GameObject("GridDotParent"), gridRoot);
			for (int y = 0; y < CurrentLevel.height; y++)
			{
				for (int x = 0; x < CurrentLevel.width; x++)
				{
					Vector2Int gridPos = new Vector2Int(x, y);
					Vector3 worldPos = coordinateConverter.GridToWorld(gridPos);

					GameObject dot = Instantiate(
						gridDotPrefab,
						worldPos,
						Quaternion.identity,
						gridDotParent.transform
					);

					dot.name = $"GridDot_{x}_{y}";
					_gridDots.Add(dot.GetComponent<Transform>());
					_gridDotMap[gridPos] = dot;
					dot.SetActive(false);

					Renderer renderer = dot.GetComponent<Renderer>();
					if (renderer != null)
					{
						renderer.material.color = gridDotColor;
					}

					SpriteRenderer spriteRenderer = dot.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null)
					{
						spriteRenderer.color = gridDotColor;
						spriteRenderer.sortingOrder = -1;
					}
				}
			}
		}

		public void EnableGridDot(Vector2Int pos)
		{
			if (_gridDotMap.TryGetValue(pos, out GameObject dot))
			{
				dot.SetActive(true);
			}
		}

		public void DisableGridDot(Vector2Int pos)
		{
			if (_gridDotMap.TryGetValue(pos, out GameObject dot))
			{
				dot.SetActive(false);
			}
		}

		public void EnableGridDotsAlongLine(Vector2Int from, Vector2Int to)
		{
			if (from == to)
			{
				EnableGridDot(to);
				return;
			}

			Vector2Int dir = new Vector2Int(System.Math.Sign(to.x - from.x), System.Math.Sign(to.y - from.y));
			Vector2Int current = from + dir;
			int maxSteps = 1000;

			while (current != to && maxSteps-- > 0)
			{
				EnableGridDot(current);
				current += dir;
			}
			EnableGridDot(to);
		}

		public void DisableGridDotsAlongLine(Vector2Int from, Vector2Int to)
		{
			if (from == to)
			{
				DisableGridDot(to);
				return;
			}

			Vector2Int dir = new Vector2Int(System.Math.Sign(to.x - from.x), System.Math.Sign(to.y - from.y));
			Vector2Int current = from + dir;
			int maxSteps = 1000;

			while (current != to && maxSteps-- > 0)
			{
				DisableGridDot(current);
				current += dir;
			}
			DisableGridDot(to);
		}

		private void CreateArrow(ArrowPath arrowPath)
		{
			GameObject arrowContainer = new GameObject($"Arrow_{arrowPath.body[0]}");
			arrowContainer.transform.SetParent(gridRoot);

			Arrow arrow = arrowContainer.AddComponent<Arrow>();

			IArrowHead head = arrowFactory.CreateArrowHead(arrowPath.body[0], arrowContainer.transform, arrowPath.color);

			IArrowRenderer renderer = arrowFactory.CreateArrowRenderer(
				arrowContainer.transform,
				arrowPath.body,
				arrowPath.color,
				arrow
			);

			arrow.Initialize(
				arrowPath,
				head,
				renderer,
				movementValidator,
				coordinateConverter,
				arrowPath.color
			);

			if (cubeRowManager == null)
			{
				cubeRowManager = FindAnyObjectByType<CubeRowManager>();
			}
			cubeRowManager?.GenerateCubesForArrow(arrowPath);
		}

		public void RegisterArrow(Vector2Int tailPosition, Arrow arrow)
		{
			arrows[tailPosition] = arrow;
		}

		public void UnregisterArrow(Vector2Int tailPosition)
		{
			if (arrows.ContainsKey(tailPosition))
			{
				arrows.Remove(tailPosition);
			}
		}

		public void RemoveArrow(Vector2Int tailPosition)
		{
			if (!arrows.ContainsKey(tailPosition))
			{
				Debug.LogWarning("Tried to remove arrow that doesn't exist at: " + tailPosition);
				return;
			}

			arrows.Remove(tailPosition);
			LevelController.OnArrowRemoved();
			OnArrowCompleted();
		}

		public void RemoveArrow(Arrow arrow)
		{
			if (!arrows.ContainsValue(arrow))
			{
				Debug.LogWarning("Tried to remove arrow that doesn't exist at: " + arrow);
				return;
			}

			var item = arrows.First(kvp => kvp.Value == arrow);
			arrows.Remove(item.Key);
			OnArrowCompleted();
		}

		public List<Arrow> GetAllArrows()
		{
			return arrows.Values.ToList();
		}

		public bool HasArrowAt(Vector2Int position)
		{
			foreach (var arrow in arrows.Values)
			{
				if (arrow.Body.Contains(position))
					return true;
			}
			return false;
		}

		public bool DoesArrowCrossDiagonal(Vector2Int from, Vector2Int to)
		{
			if (from.x == to.x || from.y == to.y) return false; // Not a diagonal move

			int dx = Math.Abs(to.x - from.x);
			int dy = Math.Abs(to.y - from.y);

			if (dx == 1 && dy == 1)
			{
				Vector2Int cross1 = new Vector2Int(to.x, from.y);
				Vector2Int cross2 = new Vector2Int(from.x, to.y);

				foreach (var arrow in arrows.Values)
				{
					var body = arrow.Body;
					for (int i = 0; i < body.Count - 1; i++)
					{
						if ((body[i] == cross1 && body[i + 1] == cross2) ||
							(body[i] == cross2 && body[i + 1] == cross1))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public Arrow GetArrowAt(Vector2Int pos)
		{
			if (arrows.ContainsKey(pos))
				return arrows[pos];
			return null;
		}

		public bool CanArrowMove(Arrow arrow)
		{
			Vector2Int head = arrow.Body[0];

			Vector2Int[] directions = new Vector2Int[]
			{
			new Vector2Int(0, 1),
			new Vector2Int(0, -1),
			new Vector2Int(1, 0),
			new Vector2Int(-1, 0)
			};

			foreach (var dir in directions)
			{
				Vector2Int nextPos = head + dir;
				if (IsValidMove(nextPos) && !arrow.Body.Contains(nextPos))
					return true;
			}

			return false;
		}

		public List<Vector2Int> GetValidMoves(Vector2Int position)
		{
			List<Vector2Int> validMoves = new List<Vector2Int>();

			Vector2Int[] directions = new Vector2Int[]
			{
			new Vector2Int(0, 1),
			new Vector2Int(0, -1),
			new Vector2Int(1, 0),
			new Vector2Int(-1, 0)
			};

			foreach (var dir in directions)
			{
				Vector2Int nextPos = position + dir;
				if (IsValidMove(nextPos))
					validMoves.Add(nextPos);
			}

			return validMoves;
		}

		bool IsValidMove(Vector2Int pos)
		{
			if (!IsInsideGrid(pos)) return false;
			if (IsBlocked(pos)) return false;
			return true;
		}

		void GenerateBlockers()
		{
			GameObject prefab = GetPrefabForMode(blocker2D, blocker3D);
			if (prefab == null) return;

			foreach (var pos in CurrentLevel.blockers)
			{
				blockers[pos] = Instantiate(
					prefab,
					coordinateConverter.GridToWorld(pos),
					Quaternion.identity,
					gridRoot
				);
			}
		}

		void GenerateHoles()
		{
			GameObject prefab = GetPrefabForMode(hole2D, hole3D);
			if (prefab == null) return;

			foreach (var pos in CurrentLevel.holes)
			{
				holes[pos] = Instantiate(
					prefab,
					coordinateConverter.GridToWorld(pos),
					Quaternion.identity,
					gridRoot
				);
			}
		}

		void GeneratePortals()
		{
			GameObject prefab = GetPrefabForMode(portal2D, portal3D);
			if (prefab == null) return;

			foreach (var pos in CurrentLevel.portals)
			{
				portals[pos] = Instantiate(
					prefab,
					coordinateConverter.GridToWorld(pos),
					Quaternion.identity,
					gridRoot
				);
			}
		}

		private GameObject GetPrefabForMode(GameObject prefab2D, GameObject prefab3D)
		{
			if (renderMode == GameRenderMode.Mesh3D || renderMode == GameRenderMode.LineRenderer3D)
				return prefab3D;
			else
				return prefab2D;
		}

		public bool IsInsideGrid(Vector2Int pos)
		{
			return pos.x >= 0 && pos.y >= 0 &&
				   pos.x < CurrentLevel.width &&
				   pos.y < CurrentLevel.height;
		}

		public bool IsBlocked(Vector2Int pos)
		{
			return blockers.ContainsKey(pos);
		}

		public bool IsHole(Vector2Int pos)
		{
			return holes.ContainsKey(pos);
		}

		public bool IsPortal(Vector2Int pos)
		{
			return portals.ContainsKey(pos);
		}

		public Vector2Int GetPairedPortal(Vector2Int entry)
		{
			foreach (var p in portals.Keys)
			{
				if (p != entry)
					return p;
			}
			return entry;
		}

		public ICoordinateConverter GetCoordinateConverter()
		{
			if (coordinateConverter == null)
			{
				InitializeFactories();
			}
			return coordinateConverter;
		}

		public IMovementValidator GetMovementValidator() => movementValidator;


		public void LoseLife(Arrow arrow, bool isNotValidMove)
		{
			if (isNotValidMove)
			{
				// if user is not correct
				if (!_wrongArrow.Contains(arrow))
				{
					MyEventArgs.GameControllerEvents.OnWrongObjectClicked?.Dispatch(isNotValidMove);
					_wrongArrow.Add(arrow);
					LevelController.SubstractMove();
				}
			}
			else
			{
				// if user is correct
				if (GameConstrain == GameConditionType.Moves || GameConstrain == GameConditionType.Moves_and_Time)
				{
					LevelController.SubstractMove();
				}
			}
		}

		void RestartLevel()
		{
			ClearGrid();
			InitLevel();
		}

		public Bounds GetGridWorldBounds()
		{
			Vector3 min, max;

			if (renderMode == GameRenderMode.Mesh3D)
			{
				min = new Vector3(-0.5f, 0, -0.5f);
				max = new Vector3(CurrentLevel.width - 0.5f, 0, CurrentLevel.height - 0.5f);
			}
			else
			{
				min = new Vector3(-0.5f, -0.5f, 0);
				max = new Vector3(CurrentLevel.width - 0.5f, CurrentLevel.height - 0.5f, 0);
			}

			Bounds bounds = new Bounds();
			bounds.SetMinMax(min, max);
			return bounds;
		}

		public float GetInitialOrthoSize()
		{
			float gridWidth = CurrentLevel.width + cameraPadding;
			float gridHeight = CurrentLevel.height + cameraPadding;
			float screenRatio = (float)Screen.width / Screen.height;
			float targetRatio = gridWidth / gridHeight;

			if (screenRatio >= targetRatio)
				return gridHeight / 2f;
			else
				return gridWidth / (2f * screenRatio);
		}

		void OnArrowCompleted()
		{
			completedArrows++;
			_levelRepresentation.SetCompleteArrow(completedArrows);

			Debug.Log($"Arrows: {completedArrows} / {totalArrows}");

			if (_levelRepresentation.AllArrowComplete)
			{
				StartCoroutine(WaitCall(1, OnAllArrowComplete));
			}
		}

		private void OnAllArrowComplete()
		{
			if (_transformWaveAnimation == null)
			{
				_transformWaveAnimation = FindAnyObjectByType<TransformWaveAnimation>();
			}

			foreach (var dots in _gridDots)
			{
				dots.localScale = Vector3.one;
				dots.GetComponent<SpriteRenderer>().sprite = StarSprite;
			}
			_transformWaveAnimation.PlayAnimation(EnumExtension.GetRandom<StartPoint>(), _gridDots);
			WaveAnimationUtility3D.OnComplete += OnGridAnimationComplete;
		}

		private void OnGridAnimationComplete()
		{
			DOVirtual.DelayedCall(1f, () =>
			{
				GameController.GameComplete();
			});
		}

		private IEnumerator WaitCall(int second, Action action)
		{
			yield return new WaitForSeconds(second);
			action?.Invoke();
		}

		public float GetTime() => timer;
		public int GetCompletedArrows() => completedArrows;
		public int GetTotalArrows() => totalArrows;

		public void OnDestroy()
		{
			MyEventArgs.GameControllerEvents.OnScreenOpen.RemoveListener(OnScreenClose);
		}
	}
}