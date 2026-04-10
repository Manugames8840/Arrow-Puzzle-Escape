using System;
using UnityEngine;
using Watermelon;

namespace ArrowOut
{
	/// <summary>
	/// Sits on each segment collider child and forwards
	/// OnMouseDown to the parent IClickableObject.
	/// </summary>
	public class ColliderClickForwarder : MonoBehaviour, IClickableObject
	{
		private IClickableObjectRenderer _lineRenderer2DArrow;

		public void Initialize(IClickableObjectRenderer lineRenderer2DArrow)
		{
			_lineRenderer2DArrow = lineRenderer2DArrow;
		}

		public void OnObjectClicked()
		{
			_lineRenderer2DArrow.OnObjectClicked();
		}

		public bool CanBeClicked()
		{
			return true;
		}

		public void OnClickBlocked()
		{
			_lineRenderer2DArrow.OnClickBlocked();
		}
	}
}