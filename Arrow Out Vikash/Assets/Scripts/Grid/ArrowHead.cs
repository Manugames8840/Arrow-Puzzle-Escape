using UnityEngine;

namespace ArrowOut
{
	/// <summary>
	/// Base class for arrow heads
	/// Open/Closed Principle: Open for extension, closed for modification
	/// </summary>
	public abstract class ArrowHeadBase : MonoBehaviour, IArrowHead
	{
		protected Vector2Int gridPosition;
		protected ICoordinateConverter coordinateConverter;
		protected Color currentColor = Color.white;

		public virtual void Initialize(Vector2Int position, Transform parent, Vector3 rotationOffset = default)
		{
			gridPosition = position;
			transform.SetParent(parent);
			coordinateConverter = GridManager.Instance.GetCoordinateConverter();
			transform.position = coordinateConverter.GridToWorld(position);
			//transform.localEulerAngles = Quaternion.Euler(rotationOffset.x, rotationOffset.y, rotationOffset.z);
		}

		public virtual void UpdatePosition(Vector2Int position)
		{
			gridPosition = position;
			transform.position = coordinateConverter.GridToWorld(position);
		}

		public abstract void SetColor(Color color);

		public GameObject GetGameObject() => gameObject;
	}
}