using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Framework;
using Framework.Core;



namespace Watermelon
{
    [RequireComponent(typeof(DevPanel))]
    public class DevPanelGameButtons : MonoBehaviour
    {
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button prevLevelButton;
        [SerializeField] Button failLevelButton;
        [SerializeField] Button completeLevelButton;

        private DevPanel devPanel;

        private void Awake()
        {
            devPanel = GetComponent<DevPanel>();

            nextLevelButton.onClick.AddListener(() => OnNextLevelButtonClicked());
            prevLevelButton.onClick.AddListener(() => OnPrevLevelButtonClicked());
            failLevelButton.onClick.AddListener(() => OnFailLevelButtonClicked());
            completeLevelButton.onClick.AddListener(() => OnCompleteLevelButtonClicked());
        }

        private void OnPrevLevelButtonClicked()
        {
            ActiveSession activeSession = ActiveSession.Current;
            activeSession.OnLevelCompleted();

            int prevLevelIndex = Mathf.Clamp(ActiveSession.Current.DisplayLevelIndex - 1, 0, int.MaxValue);
            activeSession.SetLevelIndex(prevLevelIndex);

            Overlay.Show(0.3f, () =>
            {
                GameController.Unload(() =>
                {
                    SceneManager.LoadScene(GameConsts.SCENE_GAME);
                });
            });

            devPanel.DisablePanel();
        }

        private void OnNextLevelButtonClicked()
        {
            ActiveSession activeSession = ActiveSession.Current;
            activeSession.OnLevelCompleted();

            activeSession.SetLevelIndex(activeSession.DisplayLevelIndex + 1);

            Overlay.Show(0.3f, () =>
            {
                GameController.Unload(() =>
                {
                    SceneManager.LoadScene(GameConsts.SCENE_GAME);
                });
            });

            devPanel.DisablePanel();
        }

        private void OnFailLevelButtonClicked()
        {
            GameController.GameOver(true);

            devPanel.DisablePanel();
        }

        private void OnCompleteLevelButtonClicked()
        {
            GameController.GameComplete();

            devPanel.DisablePanel();
        }
    }
}
