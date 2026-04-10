using System;
using System.Collections.Generic;
using System.Linq;
using ArrowOut;
using UnityEngine;

namespace Framework
{
	public class LevelRepresentation
	{
		public bool AllArrowComplete => _completeArrow >= _totalArrow;
		public GameConditionType GameConstrain = GameConditionType.Moves_and_Time;
		public Dictionary<Vector2Int, Arrow> Arrows = new Dictionary<Vector2Int, Arrow>();

		public LevelData LevelData { get; private set; }
		public int TotalArrow => _totalArrow;
		private int _totalArrow;
		private int _completeArrow;

		public LevelRepresentation(LevelData level, GameConditionType gameConstrain = GameConditionType.Moves_and_Time)
		{
			LevelData = level;
			GameConstrain = gameConstrain;
		}

		public void SetTotalArrow(int totalArrow)
		{
			_totalArrow = totalArrow;
		}

		public void SetCompleteArrow(int completeArrow)
		{
			_completeArrow = completeArrow;
		}

		public List<Arrow> ElementForMagnet(int number)
		{
			return GetNElements(Arrows, number);
		}

		public List<Arrow> GetNElements(Dictionary<Vector2Int, Arrow> dict, int count)
		{
			if (dict == null || dict.Count == 0)
			{
				Debug.LogWarning("Dictionary is empty!");
				return new List<Arrow>();
			}

			int finalCount = Mathf.Min(count, dict.Count);

			return dict
				.Take(finalCount)
				.Select(x => x.Value)   // 👈 Take only Arrow
				.ToList();
		}

		/// <summary>
		/// Finds an arrow that can move (has valid moves).
		/// Prioritizes arrows that can directly reach a hole.
		/// </summary>
		public Arrow FindHintArrow()
		{
			List<Arrow> allArrows = Arrows.Values.ToList();
			Arrow bestCandidate = null;

			foreach (var arrow in allArrows)
			{
				if (!arrow.CanMove())
					continue;
				else
				{
					bestCandidate = arrow;
					break;
				}
			}
			return bestCandidate;
		}
	}
}
