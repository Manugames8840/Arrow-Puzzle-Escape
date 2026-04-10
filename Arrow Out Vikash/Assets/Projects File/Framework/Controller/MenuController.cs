using Framework;
using Framework.Core;
using MaskTransitions;
using UnityEngine;

namespace Watermelon
{
	public class MenuController : MonoBehaviour
	{
		[SerializeField] UIController uiController;

		public LoadingScreenUI loadingScreenUI;
		public static LoadingScreenUI LOADINGSCREENUI;

		private static ParticlesController particlesController;
		private static FloatingTextController floatingTextController;
		private static PUController puController;

		private void Awake()
		{
			GameData gameData = GameData.Data;
			if (gameData == null)
				Debug.LogError("GameData is null. Please add the Game Settings component to the Project Init Settings and link Game Data scriptable object.");

			// Cache components
			gameObject.CacheComponent(out particlesController);
			gameObject.CacheComponent(out floatingTextController);
			gameObject.CacheComponent(out puController);

			LOADINGSCREENUI = loadingScreenUI;

			// Initialize UI Controller to let other classes use UIController.GetPage method
			uiController.Init();

			// Initialize other controllers
			particlesController.Init();
			floatingTextController.Init();

			puController.Init();

			// Initialize currency cloud and pages
			uiController.InitPages();
		}

		private void Start()
		{
			// Display default page
			UIController.ShowPage<UIMainMenu>();
		}

		public static void OnPlayButtonClicked()
		{
			ActiveSession session = ActiveSession.Current;
			OnMapLevelClicked(session.Save.MaxReachedLevelIndex);
		}

		public static void OnMapLevelClicked(int levelID)
		{
			if (LivesSystem.Lives > 0 || LivesSystem.InfiniteMode)
			{
				LivesSystem.LockLife();

				LoadGame(levelID);
			}
			else
			{
				UIAddLivesPanel.Show((lifeRecieved) =>
				{
					if (lifeRecieved)
					{
						LivesSystem.LockLife();

						LoadGame(levelID);
					}
				});
			}
		}

		public static void LoadGame(int levelID)
		{
			ActiveSession session = ActiveSession.Current;
			session.SetLevelIndex(levelID);

			Overlay.Show(0.3f, () =>
			{
				Unload(() =>
				{
					TransitionManager.Instance.LoadLevel(GameConsts.SCENE_GAME);
					//SceneLoader.LoadAsync(GameConsts.SCENE_GAME, LOADINGSCREENUI, LOADINGSCREENUI._loadingScreenConfig);
				});
			}, true);
		}

		public static void Unload(SimpleCallback onUnloaded)
		{
			onUnloaded?.Invoke();
		}
	}
}
