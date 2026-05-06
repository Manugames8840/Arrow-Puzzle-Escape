using System;
using System.Collections.Generic;
using Frameork;
using TMPro;
using UnityEngine;

namespace Framework
{
	public class MoveSystem : MonoBehaviour
	{
		public GameConditionType Condition = GameConditionType.Star;

		public RectTransform StarContainer;
		public List<GameObject> Stars = new List<GameObject>();
		public TextMeshProUGUI MoveText;
		public bool EnableOnAwake;
		private int TotalMove;

		public void Awake()
		{
			if (EnableOnAwake)
			{
				Init();
			}
		}

		public void SetConstrain(GameConditionType gameConditionType)
		{
			Condition = gameConditionType;
		}

		public void Init()
		{
			if (Condition == GameConditionType.Star || Condition == GameConditionType.Moves)
			{
				StarContainer.gameObject.SetActive(Condition == GameConditionType.Star);
				MoveText.gameObject.SetActive(Condition == GameConditionType.Moves);
			}
			else
			{
				gameObject.SetActive(false);
			}

			GameController.OnGameStartd += OnGameStarted;
			LevelController.GameplayMove.OnMoveChange += OnMoveChange;
			LevelController.GameplayMove.OnMoveAdded += OnMoveAdded;
		}

		private void OnGameStarted()
		{
			LevelController.LevelLoaded -= OnGameStarted;
		}

		public void OnMoveChange(float moveValue)
		{
			if (Condition == GameConditionType.Star)
			{
				foreach (var star in Stars)
				{
					if (star.activeInHierarchy)
					{
						star.gameObject.SetActive(false);
						break;
					}
				}
			}
			else
			{
				MoveText.SetText(moveValue.ToString());
			}
		}

		public void OnMoveAdded(int moveValue)
		{
			if (Condition == GameConditionType.Star)
			{
				for (int i = 0; i < (int)moveValue; i++)
				{
					if (Stars.Count < i)
					{
						break;
					}
					Stars[i].gameObject.SetActive(true);
				}
			}
			else
			{
				MoveText.text = moveValue.ToString();
			}
		}

		private void OnDestroy()
		{
			LevelController.LevelLoaded -= OnGameStarted;
			LevelController.GameplayMove.OnMoveAdded -= OnMoveAdded;
		}
	}
}
