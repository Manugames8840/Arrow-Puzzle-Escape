using System.Collections;
using UnityEngine;

namespace ArrowOut
{
	public class LineRendererBlink : MonoBehaviour
	{
		[Header("References")]
		public LineRenderer lineRenderer;

		[Header("Blink Settings")]
		public float blinkInterval = 0.2f;
		public int blinkCount = 5; // how many times to blink (-1 = infinite)

		private Color _originalStartColor;
		private Color _originalEndColor;
		private Coroutine _blinkCoroutine;

		void Start()
		{
			if (lineRenderer == null)
				lineRenderer = GetComponent<LineRenderer>();
		}

		public void StartBlink()
		{
			if (_blinkCoroutine != null)
				StopCoroutine(_blinkCoroutine);

			// Cache original colors
			_originalStartColor = lineRenderer.startColor;
			_originalEndColor = lineRenderer.endColor;

			_blinkCoroutine = StartCoroutine(BlinkRoutine());
		}

		
		private IEnumerator BlinkRoutine()
		{
			bool isWhite = false;
			int count = 0;

			while (blinkCount == -1 || count < blinkCount * 2) // *2 because each blink = 2 toggles
			{
				isWhite = !isWhite;

				if (isWhite)
					SetColor(Color.white, Color.white);
				else
					SetColor(_originalStartColor, _originalEndColor);

				count++;
				yield return new WaitForSeconds(blinkInterval);
			}

			// Always end on original color
			SetColor(_originalStartColor, _originalEndColor);
			_blinkCoroutine = null;
		}

		public void StopBlink()
		{
			if (_blinkCoroutine != null)
			{
				StopCoroutine(_blinkCoroutine);
				_blinkCoroutine = null;
			}

			// Restore original color
			SetColor(_originalStartColor, _originalEndColor);
		}

		private void SetColor(Color start, Color end)
		{
			lineRenderer.startColor = start;
			lineRenderer.endColor = end;
		}
	}
}