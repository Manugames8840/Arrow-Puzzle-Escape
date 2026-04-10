using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Framework;
using Framework.Core;
using Frameork;
using ArrowOut;

namespace Framework
{
	[StaticUnload]
	public class LevelController : MonoBehaviour
	{
		public static readonly Ease.Type BLOCK_MOVE_EASE_TYPE = Ease.Type.QuadIn;
		public static readonly float BLOCK_MOVE_DURATION = 0.25f;

		public static LevelController levelController;
		public GridManager gridManager;

		public static LevelDatabase LevelDatabase => levelDatabase;
		public static LevelRepresentation LevelRepresentation { get; private set; }
		public static bool IsLevelLoaded { get; private set; } = false;
		public static GameplayTimer GameplayTimer { get; private set; }
		public static GameplayMove GameplayMove { get; private set; }

		public static event SimpleCallback LevelLoaded;
		public static event LevelScenarioDelegate LevelScenarioChanged;

		public static IArrowInputHandler _currentSelectedArrow;
		private static LevelDatabase levelDatabase;

		private GameConditionType _gameConstrain = GameConditionType.Star;

		public void Init(LevelDatabase levelDatabase, GameConditionType gameConstrain = GameConditionType.Star)
		{
			levelController = this;

			if (levelDatabase == null)
			{
				Debug.LogError("[LevelController] Level database is not set. Please check the Game Data scriptable object.");
				return;
			}

			LevelController.levelDatabase = levelDatabase;
			_gameConstrain = gameConstrain;
			levelDatabase.Init();
			IsLevelLoaded = false;

			MyEventArgs.GameControllerEvents.OnSettingBack.AddListener(SettingBackButtonPressed);

			ActiveSession activeSession = ActiveSession.Current;
			var displayedLevelIndex = activeSession.DisplayLevelIndex;
			var levelIndex = activeSession.GetLevelIndex(displayedLevelIndex);

			LoadLevel(displayedLevelIndex, levelIndex, activeSession.FirstStart);
		}

		public static LevelData SpecificVariationToLoad;

		public void LoadLevel(int displayedLevelIndex, int levelIndex, bool firstStart)
		{
			LevelData levelData;

			if (SpecificVariationToLoad != null)
			{
				levelData = SpecificVariationToLoad;
				SpecificVariationToLoad = null; // consume it
			}
			else
			{
				levelData = levelDatabase.GetLevel(levelIndex);
			}

			if (levelData == null)
			{
				Debug.LogError($"[LevelController] Failed to load LevelData at index {levelIndex}. Aborting.");
				return;
			}

			LevelRepresentation = new LevelRepresentation(levelData, _gameConstrain);
			gridManager.StartLevel(levelData, _gameConstrain);

			SetupGameConstrain();

			IsLevelLoaded = true;
			LevelLoaded?.Invoke();
			InvokeScenario(LevelScenario.LevelStarted);

			ActiveSession.Current.OnLevelStarted(levelData);
			SavePresets.CreateSave("Level " + (displayedLevelIndex + 1).ToString("0000"), "Levels");
		}

		private void SetupGameConstrain()
		{
			TeardownGameConstrain();

			switch (_gameConstrain)
			{
				case GameConditionType.Time:
					GameplayTimer = CreateTimer();
					InitTimer();
					break;

				case GameConditionType.Moves:
				case GameConditionType.Star:
					GameplayMove = CreateMove();
					InitMove();
					break;

				case GameConditionType.Moves_and_Time:
				case GameConditionType.Star_and_Time:
					GameplayTimer = CreateTimer();
					GameplayMove = CreateMove();
					InitTimer();
					InitMove();
					break;
			}
		}

		private void TeardownGameConstrain()
		{
			if (GameplayTimer != null)
			{
				GameplayTimer.OnTimerFinished -= OnGameplayTimerFinished;
				GameplayTimer = null;
			}

			if (GameplayMove != null)
			{
				GameplayMove.OnMoveFinished -= OnGameplayTimerFinished;
				GameplayMove = null;
			}
		}

		private GameplayTimer CreateTimer()
		{
			var timer = new GameplayTimer();
			timer.OnTimerFinished += OnGameplayTimerFinished;
			return timer;
		}

		private GameplayMove CreateMove()
		{
			var move = new GameplayMove();
			move.OnMoveFinished += OnGameplayTimerFinished;
			return move;
		}

		private static void InitTimer()
		{
			GameplayTimer.SetMaxTime(LevelRepresentation.LevelData.Duration);
		}

		private static void OnGameplayTimerFinished()
		{
			if (LevelRepresentation.AllArrowComplete)
				return;

			GameController.GameOver(true);
		}

		private void InitMove()
		{
			int maxMoves = _gameConstrain == GameConditionType.Star
				? 3
				: LevelRepresentation.LevelData.arrowPaths.Count + 5;

			GameplayMove.SetMaxMove(maxMoves);
		}

		public static void SubstractMove()
		{
			GameplayMove?.Substract(1);
		}

		private void SettingBackButtonPressed()
		{
			GameplayTimer?.Resume();
		}

		public void HandleGameEnd()
		{
			AudioController.PlaySound(AudioController.AudioClips.blockPick);
			GameplayTimer?.Pause();
		}

		public void OnGameCompleted()
		{
			ActiveSession.Current.OnLevelCompleted();
			SaveController.MarkAsSaveIsRequired();
		}

		public void OnRevived(int seconds)
		{
			if (GameplayTimer != null)
			{
				GameplayTimer.AdjustTime(seconds);
				GameplayTimer.Resume();
			}
		}

		public void OnRevivedMoves(int moves)
		{
			GameplayMove?.AdjustMove(moves);
		}

		public void OnLevelFailed() { }

		public void OnGameActivated()
		{
			GameplayTimer?.Start();
			GameplayMove?.Start();
		}

		private void Update()
		{
			GameplayTimer?.Update();
		}
		public static void OnObjectPicked(IArrowInputHandler levelBlockBehavior)
		{
			_currentSelectedArrow = levelBlockBehavior;
		}

		public static void OnObjectReleased()
		{
			_currentSelectedArrow?.MouseUp();
			_currentSelectedArrow = null;
		}

		public static void InvokeOrWait(SimpleCallback loadCallback)
		{
			if (IsLevelLoaded)
				loadCallback?.Invoke();
			else
				LevelLoaded += loadCallback;
		}

		public static void InvokeScenario(LevelScenario scenario)
		{
			LevelScenarioChanged?.Invoke(scenario);
		}

		private static void UnloadStatic()
		{
			LevelLoaded = null;
			LevelScenarioChanged = null;
		}

		internal static void OnArrowRemoved()
		{
			_currentSelectedArrow = null;
		}

		public delegate void LevelScenarioDelegate(LevelScenario type);
	}
}