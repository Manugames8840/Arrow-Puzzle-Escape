using ArrowOut.UI.Components;
using BaseView;
using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowOut.UI.Controllers
{
	public class UIThemeController : Behaviour<UIThemeView>
	{
		public MainMenuButtons MainMenuButton;
		private UIThemeView m_View;
		[SerializeField] private int _currentSeleted;

		protected override void Init()
		{
			base.Init();
			m_View = (UIThemeView)Prefab;

			// Subscribe the buttons to the theme change function
			if (m_View.WhiteArrowButton != null)
				m_View.WhiteArrowButton.onClick.AddListener(() =>
				{
					_currentSeleted = 0;
				});

			if (m_View.BlackArrowButton != null)
				m_View.BlackArrowButton.onClick.AddListener(() =>
				{
					_currentSeleted = 1;
					m_View.BlackArrowButton.transform.GetChild(1).gameObject.SetActive(true);
					m_View.ColorsArrowButton.transform.GetChild(1).gameObject.SetActive(false);
				});

			if (m_View.ColorsArrowButton != null)
				m_View.ColorsArrowButton.onClick.AddListener(() =>
				{
					_currentSeleted = 2;
					m_View.ColorsArrowButton.transform.GetChild(1).gameObject.SetActive(true);
					m_View.BlackArrowButton.transform.GetChild(1).gameObject.SetActive(false);
				});

			if (m_View.ChangeTheme != null)
			{
				m_View.ChangeTheme.onClick.AddListener(() =>
				{
					PlayerPrefs.SetInt("CurrentTheme", _currentSeleted);
					SetTheme((ArrowColorMode)_currentSeleted);
				});
			}

			if (PlayerPrefs.GetInt("CurrentTheme", 0) == 0)
			{
				m_View.ColorsArrowButton.transform.GetChild(1).gameObject.SetActive(true);
			}
			else
			{
				m_View.BlackArrowButton.transform.GetChild(1).gameObject.SetActive(true);
			}

			MainMenuButton.AssgineAction(OpenCurrentPanel);
		}

		private void SetTheme(ArrowColorMode mode)
		{
			CloseCurrentPanel();
			if (GridManager.Instance != null)
			{
				GridManager.Instance.SetAllArrowsTheme(mode);
			}
			else
			{
				Debug.LogWarning("GridManager Instance is null. Cannot change arrow theme right now.");
			}
		}

		public override void ShowPanel(bool on)
		{

		}

		public override bool IsShow()
		{
			return false;
		}
	}
}