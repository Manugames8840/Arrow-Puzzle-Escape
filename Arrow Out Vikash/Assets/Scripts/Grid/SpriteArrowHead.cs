using Dreamteck.Splines;
using UnityEngine;

namespace ArrowOut
{
	/// <summary>
	/// 2D Sprite-based arrow head
	/// </summary>
	public class SpriteArrowHead : ArrowHeadBase
	{
		private SpriteRenderer spriteRenderer;

		public void Setup(Sprite headSprite, Color color)
		{
			spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
			spriteRenderer.material = GridManager.Instance.SpriteHeadMat;
			spriteRenderer.sprite = headSprite;
			spriteRenderer.sortingOrder = 1;
			spriteRenderer.color = color;
			var box = gameObject.AddComponent<BoxCollider2D>();
			box.size = Vector2.one * 1.5f;
		}

		public override void SetColor(Color color)
		{
			currentColor = color;
			if (spriteRenderer != null)
				spriteRenderer.color = color;
		}

		public void SetRotation(Vector2Int direction)
		{
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0, 0, angle);
		}
	}
}
