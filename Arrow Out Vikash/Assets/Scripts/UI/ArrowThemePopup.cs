using UnityEngine;
using UnityEngine.UI;

namespace ArrowOut.UI
{
	public class ArrowThemePopup : MonoBehaviour
	{
		[Header("Theme Buttons")]
		public Button whiteArrowButton;
		public Button blackArrowButton;
		public Button colorsArrowButton;
		public Button ChangeTheme;
		private int _currentSeleted;

		void Start()
		{
			// Subscribe the buttons to the theme change function
			if (whiteArrowButton != null)
				whiteArrowButton.onClick.AddListener(() =>
				{
					_currentSeleted = 0;
				});

			if (blackArrowButton != null)
				blackArrowButton.onClick.AddListener(() =>
				{
					_currentSeleted = 1;
				});

			if (colorsArrowButton != null)
				colorsArrowButton.onClick.AddListener(() =>
				{
					_currentSeleted = 2;
				});

			if (ChangeTheme != null)
			{
				ChangeTheme.onClick.AddListener(() =>
				{
					PlayerPrefs.SetInt("CurrentTheme", _currentSeleted);
					SetTheme((ArrowColorMode)_currentSeleted);
				});
			}
		}

		private void SetTheme(ArrowColorMode mode)
		{
			if (GridManager.Instance != null)
			{
				GridManager.Instance.SetAllArrowsTheme(mode);
			}
			else
			{
				Debug.LogWarning("GridManager Instance is null. Cannot change arrow theme right now.");
			}
		}
	}
}
