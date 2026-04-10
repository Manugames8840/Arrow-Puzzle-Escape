using System;
using System.Collections.Generic;
using ArrowOut;
using Frameork;
using Framework;
using Framework.Core;
using MaskTransitions;
using UnityEngine;
using Watermelon;

namespace Framework
{
	public class GameController : MonoBehaviour
	{
		[SerializeField] UIController uiController;
		public static int LevelCompleteCount;
		public static LoadingScreenUI LOADINGSCREENUI;
		private static ParticlesController particlesController;
		private static FloatingTextController floatingTextController;
		private static GridManager gridManager;
		private static LevelController levelController;
		private static PUController puController;
		private static TutorialController tutorialController;
		private static RaycastController raycastController;
		private static bool isGameActivated;
		private static bool isGameFinished;
		private static bool isRevived;

		private static TweenCase boostersTweenCase;
		private static TweenCase waitTweenCase;

		public static event SimpleCallback OnGameStartd;
		public static bool IsGameActivated => isGameActivated;

		public LoadingScreenUI LoadingScreenUI;
		public GameConditionType GameConstrain = GameConditionType.Star;
		public ArrowOut.CameraController CameraCtrl;
		[SerializeField] MoveSystem moveSystem;

		[BoxGroup("Top Panel")]
		[SerializeField] LevelPanel levelPanel;

		private void Awake()
		{
			GameData gameData = GameData.Data;
			if (gameData == null)
				Debug.LogError("GameData is null. Please add the Game Settings component to the Project Init Settings and link Game Data scriptable object.");

			// Cache components
			gameObject.CacheComponent(out particlesController);
			gameObject.CacheComponent(out floatingTextController);
			gameObject.CacheComponent(out gridManager);
			gameObject.CacheComponent(out levelController);
			gameObject.CacheComponent(out raycastController);
			gameObject.CacheComponent(out puController);
			gameObject.CacheComponent(out tutorialController);

			gridManager.Init();
			levelController.Init(gameData.LevelDatabase, GameConstrain);
			Invoke(nameof(OnLevelLoaded), 0.3f);

			AdsManager.EnableBanner();
		}

		private void OnLevelLoaded()
		{
			CameraCtrl?.SetUp();

			// Initialize UI Controller to let other classes use UIController.GetPage method
			uiController.Init();

			// Initialize other controlles
			particlesController.Init();
			floatingTextController.Init();
			raycastController.Init();
			puController.Init();
			puController.InitBehaviors();

			tutorialController.Init();

			// Initialize currency cloud and pages
			uiController.InitPages();

			moveSystem.Init();
			moveSystem.SetConstrain(GameConstrain);
			moveSystem.OnMoveAdded(LevelController.GameplayMove.CurrentMove);

			// Mark game as inactive
			isGameActivated = false;
			isGameFinished = false;
			isRevived = false;

			LOADINGSCREENUI = LoadingScreenUI;
			OnStart();
		}

		private void OnEnable()
		{
			RaycastController.OnObjectTouched += OnObjectTouched;
		}

		private void OnDisable()
		{
			RaycastController.OnObjectTouched -= OnObjectTouched;
		}

		private void OnStart()
		{
			var levelIndex = ActiveSession.Current.DisplayLevelIndex;

			// Display default page
			UIController.ShowPage<UIGame>();

			PUController.OnLevelLoaded(levelIndex);

			OnGameStartd?.Invoke();

			waitTweenCase = UIController.WaitForPopupsClose(() =>
			{
				// Enable tutorials
				//TutorialController.ActivateTutorial(TutorialController.GetTutorial(TutorialID.PUHint));
				//TutorialController.ActivateTutorial(TutorialController.GetTutorial(TutorialID.PUMagnet));
				//TutorialController.ActivateTutorial(TutorialController.GetTutorial(TutorialID.PUHammer));
			});
		}

		private void OnDestroy()
		{
			boostersTweenCase.KillActive();
			waitTweenCase.KillActive();

			// Reset time scale
			Time.timeScale = 1.0f;
		}

		public static void GameComplete()
		{
			if (isGameFinished)
				return;

			isGameFinished = true;

			levelController.HandleGameEnd();
			levelController.OnGameCompleted();

			PUController.OnLevelEnded();

			AudioController.PlaySound(AudioController.AudioClips.win);

			OnLevelCompleted();

			MyEventArgs.GameControllerEvents.LevelCompleteCount++;

			CollectNewFeatures();

			UIController.ShowPage<UIComplete>();
		}

		private static void CollectNewFeatures()
		{
			ActiveSession activeSession = ActiveSession.Current;
			int nextLevelIndex = activeSession.DisplayLevelIndex;

			// Collect PUs
			PUBehavior[] powerUps = PUController.ActivePowerUps;
			foreach (PUBehavior pu in powerUps)
			{
				PUSettings settings = pu.Settings;
				if (settings.RequiredLevel != 0 && !settings.IsUnlocked && (settings.RequiredLevel - 1) == nextLevelIndex)
				{
					FeatureAnnouncementPopup.RegisterFeature(settings.AnnouncementPopupData);
				}
			}
		}

		public static void GameOver(bool allowRevive, float uiDelay = 0.0f)
		{
			if (isGameFinished)
				return;

			if (isRevived)
				allowRevive = false;

			isGameFinished = true;

			levelController.OnLevelFailed();
			levelController.HandleGameEnd();
			MyEventArgs.GameControllerEvents.LevelCompleteCount = 0;
			PUController.OnLevelEnded();

			AudioController.PlaySound(AudioController.AudioClips.lose);

			if (uiDelay > 0)
			{
				Tween.DelayedCall(uiDelay, () =>
				{
					UIGameOver.Show(allowRevive);
				});
			}
			else
			{
				UIGameOver.Show(allowRevive);
			}
		}

		public static void Revive(int seconds)
		{
			if (!isGameFinished)
				return;

			isGameFinished = false;
			isRevived = true;

			levelController.OnRevived(seconds);

			AudioController.PlaySound(AudioController.AudioClips.revive);

			UIController.HidePage<UIGameOver>();
		}

		public static void ReviveMoves(int move)
		{
			if (!isGameFinished)
				return;

			isGameFinished = false;
			isRevived = true;

			levelController.OnRevivedMoves(move);

			AudioController.PlaySound(AudioController.AudioClips.revive);

			UIController.HidePage<UIGameOver>();
		}

		public static void OnLevelCompleted()
		{
			ActiveSession currentSession = ActiveSession.Current;

			int levelIndex = currentSession.DisplayLevelIndex;
			currentSession.SetLevelIndex(levelIndex + 1);

			LevelController.InvokeScenario(LevelScenario.LevelCompleted);

			AnalyticsController.OnLevelComplete("level_complete", new Dictionary<string, object> {
				{ "level", levelIndex },
				{ "timeTaken", LevelController.GameplayTimer?.GetTotalTimeSpent().TotalSeconds } ,
				{ "timeRequired", LevelController.GameplayTimer?.CurrentTimeSpan.TotalSeconds}
			});
		}

		public static void OnLevelFailed()
		{
			LivesSystem.UnlockLife(true);

			ActiveSession currentSession = ActiveSession.Current;
			int levelIndex = currentSession.DisplayLevelIndex;

			LevelController.InvokeScenario(LevelScenario.LevelFailed);

			AnalyticsController.OnLevelFailed("level_failed", new Dictionary<string, object> {
				{ "level", levelIndex },
				{ "timeTaken", LevelController.GameplayTimer?.GetTotalTimeSpent().TotalSeconds},
				{ "timeRequired", LevelController.GameplayTimer?.CurrentTimeSpan.TotalSeconds}
			});
		}

		public static void Replay(SimpleCallback onReplayCallback = null)
		{
			if (LivesSystem.Lives > 0 || LivesSystem.InfiniteMode)
			{
				onReplayCallback?.Invoke();

				LivesSystem.UnlockLife();

				Overlay.Show(0.3f, () =>
				{
					Unload(() =>
					{
						LoadGameGameScene();
					});
				});
			}
			else
			{
				UIAddLivesPanel.Show((lifeRecieved) =>
				{
					if (lifeRecieved)
					{
						onReplayCallback?.Invoke();

						Overlay.Show(0.3f, () =>
						{
							Unload(() =>
							{
								LivesSystem.LockLife();
								LoadGameGameScene();
							});
						});
					}
					else
					{
						MusicSource musicSource = MusicSource.ActiveMusicSource;
						if (musicSource != null)
							musicSource.Fade(0.2f, 0.3f);

						Overlay.Show(0.3f, () =>
						{
							LoadMenu(() =>
							{
								if (musicSource != null)
									musicSource.Fade(1, 0.3f);
							});
						});
					}
				});
			}

			ActiveSession currentSession = ActiveSession.Current;
			int levelIndex = currentSession.DisplayLevelIndex;

			AnalyticsController.OnLevelReplay("level_replay", new Dictionary<string, object> {
				{ "level", levelIndex },
				{ "timeTaken", LevelController.GameplayTimer?.GetTotalTimeSpent().TotalSeconds},
				{ "timeRequired", LevelController.GameplayTimer?.CurrentTimeSpan.TotalSeconds}
			});
		}

		public static void OnCompleteRewardRecieved()
		{
			Overlay.Show(0.3f, () =>
			{
				Unload(() =>
				{
					LoadGameMenuScene();
				});
			});
		}

		public static void LoadMenu(SimpleCallback unloadCallback = null)
		{
			Unload(() =>
			{
				LoadGameMenuScene();
				unloadCallback?.Invoke();
			});
		}

		public static void Unload(SimpleCallback onUnloaded, bool isGameScene = false)
		{
			GameData gameData = GameData.Data;
			PUController.OnLevelEnded();
			SaveController.Save();

			if (Monetization.IsActive && Monetization.Settings.IsShowOnLevelComplete)
			{
				if (Monetization.Settings.LevelRequire < ActiveSession.Current.DisplayLevelIndex)
				{
					AdsManager.ShowInterstitial((result) =>
					{
						onUnloaded?.Invoke();
					}, "level_complete_Interstitial_ads");
				}
				else
				{
					onUnloaded?.Invoke();
				}
			}
			else
			{
				onUnloaded?.Invoke();
			}
		}

		public static void Unload(SimpleBoolCallback onUnloaded, bool isGameScene = false)
		{
			GameData gameData = GameData.Data;
			PUController.OnLevelEnded();
			SaveController.Save();

			if (Monetization.IsActive && Monetization.Settings.IsShowOnLevelComplete)
			{
				if (Monetization.Settings.LevelRequire < ActiveSession.Current.DisplayLevelIndex)
				{
					AdsManager.ShowInterstitial((result) =>
					{
						onUnloaded?.Invoke(Monetization.Settings.LevelRequire < ActiveSession.Current.DisplayLevelIndex);
					}, "level_complete_Interstitial_ads");
				}
				else
				{
					onUnloaded?.Invoke(Monetization.Settings.LevelRequire < ActiveSession.Current.DisplayLevelIndex);
				}
			}
			else
			{
				onUnloaded?.Invoke(Monetization.Settings.LevelRequire < ActiveSession.Current.DisplayLevelIndex);
			}
		}


		private void OnObjectTouched()
		{
			ActivateGame();

			RaycastController.OnObjectTouched -= OnObjectTouched;
		}

		public static void ActivateGame()
		{
			if (isGameActivated)
				return;

			isGameActivated = true;

			levelController.OnGameActivated();
		}

		public static void LoadGameMenuScene()
		{
			Overlay.Show(0.3f, () =>
			{
				Unload(() =>
				{
					TransitionManager.Instance.LoadLevel(GameConsts.SCENE_MENU);

					//SceneLoader.LoadAsync(GameConsts.SCENE_MENU, LOADINGSCREENUI, LOADINGSCREENUI._loadingScreenConfig);
				});
			}, true);
		}

		public static void LoadGameGameScene()
		{
			Overlay.Show(0.3f, () =>
			{
				Unload(() =>
				{
					TransitionManager.Instance.LoadLevel(GameConsts.SCENE_GAME);

					//SceneLoader.LoadAsync(GameConsts.SCENE_GAME, LOADINGSCREENUI, LOADINGSCREENUI._loadingScreenConfig);
				});
			}, true);
		}


		public static void LoadMainScene()
		{
			Overlay.Show(0.3f, () =>
			{
				Unload(() =>
				{
					TransitionManager.Instance.LoadLevel(GameConsts.SCENE_MENU);

					//SceneLoader.LoadAsync(GameConsts.SCENE_MENU, LOADINGSCREENUI, LOADINGSCREENUI._loadingScreenConfig);
				});
			}, true);
		}
	}
}
