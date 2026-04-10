using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CubeStackPuzzle
{
	/// <summary>
	/// Manages a single vertical stack (column) of cubes.
	/// The front cube is always the bottom-most cube (index 0).
	/// </summary>
	public class CubeColumn
	{
		private readonly List<Cube> _cubes = new List<Cube>();
		private readonly int _columnIndex;
		private readonly float _verticalSpacing;
		private readonly float _cubeSize;
		private readonly Vector3 _basePosition;

		public int ColumnIndex => _columnIndex;
		public int CubeCount => _cubes.Count;
		public bool IsEmpty => _cubes.Count == 0;
		public Vector3 BasePosition => _basePosition;

		// ────────────────────────────────────────────────────────────────────

		public CubeColumn(int columnIndex, Vector3 basePosition, float cubeSize, float verticalSpacing)
		{
			_columnIndex = columnIndex;
			_basePosition = basePosition;
			_cubeSize = cubeSize;
			_verticalSpacing = verticalSpacing;
		}

		// ── Stack Operations ───────────────────────────────────────────────

		/// <summary>Add a cube to the top of this column.</summary>
		public void AddCube(Cube cube)
		{
			_cubes.Add(cube);
		}

		/// <summary>Returns the front (bottom-most) cube, or null if empty.</summary>
		public Cube GetFrontCube()
		{
			if (_cubes.Count == 0) return null;
			return _cubes[0];
		}

		/// <summary>
		/// Remove the front cube, destroy it, then shift all remaining cubes down.
		/// </summary>
		public void RemoveFrontCube()
		{
			if (_cubes.Count == 0) return;

			Cube front = _cubes[0];
			_cubes.RemoveAt(0);

			front.DestroyCube();

			ApplyGravity();
		}

		/// <summary>
		/// Shift all cubes downward to fill the gap left by a removed cube.
		/// </summary>
		public void ApplyGravity()
		{
			float step = _cubeSize + _verticalSpacing;

			for (int i = 0; i < _cubes.Count; i++)
			{
				if (_cubes[i] == null || _cubes[i].IsDestroyed) continue;

				_cubes[i].SetHeightLevel(i);
				Vector3 targetPos = _basePosition + Vector3.up * (i * step);
				_cubes[i].transform.DOMove(targetPos, 0.3f);
			}
		}

		/// <summary>Returns all live cubes in this column (bottom to top).</summary>
		public IReadOnlyList<Cube> GetAllCubes()
		{
			return _cubes.AsReadOnly();
		}

		/// <summary>Remove null / destroyed references from the internal list.</summary>
		public void CleanUp()
		{
			_cubes.RemoveAll(c => c == null || c.IsDestroyed);
		}
	}
}
