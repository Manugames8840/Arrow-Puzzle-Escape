using System.Collections;
using System.Collections.Generic;
using Frameork;
using Framework;
using Framework.Core;
using UnityEngine;
using Watermelon;

namespace ArrowOut
{
	public enum ArrowColorMode
	{
		White,
		Black,
		Colors
	}

	public class Arrow : MonoBehaviour, IArrowInputHandler
	{
		// Dependencies (injected)
		private IArrowHead arrowHead;
		private IArrowRenderer arrowRenderer;
		private IMovementValidator movementValidator;
		private ICoordinateConverter coordinateConverter;

		// State
		private List<Vector2Int> body;
		private Vector2Int tailPosition;
		private bool isExtending;
		private bool isValidMove;
		private List<Vector2Int> previewPath;
		private List<Vector2Int> originalBodyPositions;
		private bool isIntroPlaying;
		private bool isRemoving;

		// Configuration
		private Color originalColor = Color.white;
		private Color currentThemeColor = Color.white;
		private Color invalidColor = Color.red;
		private float extendSpeed = 0.01f;
		private float introSegmentDelay = 0.1f;
		private int initialLength;

		// Public properties
		public int ColorId;
		public List<Vector2Int> Body => body;
		public Vector2Int TailPosition => tailPosition;

		public void Initialize(ArrowPath arrowPath, IArrowHead head, IArrowRenderer renderer, IMovementValidator validator, ICoordinateConverter converter, Color color)
		{
			body = new List<Vector2Int>(arrowPath.body);
			originalBodyPositions = new List<Vector2Int>(arrowPath.body);
			tailPosition = body[body.Count - 1];

			arrowHead = head;
			arrowRenderer = renderer;
			movementValidator = validator;
			coordinateConverter = converter;
			originalColor = color;
			currentThemeColor = color;
			initialLength = body.Count;

			UpdateHeadRotation();
			SetTheme((ArrowColorMode)PlayerPrefs.GetInt("CurrentTheme", 1));
			GridManager.Instance.RegisterArrow(tailPosition, this);
		}

		private void UpdateHeadRotation()
		{
			if (body.Count < 2 || arrowHead == null) return;

			Vector2Int direction = body[1] - body[0];

			if (arrowHead is SpriteArrowHead spriteHead)
			{
				spriteHead.SetRotation(direction);
			}
			else if (arrowHead is MeshArrowHead meshHead)
			{
				Vector3 worldDir = coordinateConverter.GridToWorld(body[1]) -
								   coordinateConverter.GridToWorld(body[0]);
				meshHead.SetRotation(worldDir);
			}
		}

		public void Cleanup()
		{
			// Prevent double cleanup
			if (gameObject == null) return;

			// Mark as cleaning up
			isExtending = true;

			// Cleanup visuals
			if (arrowHead != null && arrowHead.GetGameObject() != null)
			{
				arrowHead.GetGameObject().SetActive(false);
			}

			if (arrowRenderer != null)
			{
				arrowRenderer.Destroy();
			}

			// Destroy this game object
			Destroy(gameObject);
		}

		// ============= INPUT HANDLING =============
		public bool IsMouseOver(Vector2 mousePosition)
		{
			Vector2Int gridPos = coordinateConverter.ScreenToGrid(mousePosition);
			return body.Contains(gridPos);
		}

		public void MouseDown()
		{
			if (isExtending || isIntroPlaying) return;


			// Check if arrow can move
			isValidMove = movementValidator.CanMove(body);

			//if (!isValidMove)
			//{
			//	// Invalid - show red color
			//	SetColor(invalidColor);
			//}
			LevelController.OnObjectPicked(this);
		}

		public bool CanMove()
		{
			return movementValidator.CanMove(body);
		}

		// In Arrow.MouseUp
		public void MouseUp()
		{
			Debug.Log($"MouseUp - isExtending: {isExtending}, isValidMove: {isValidMove}, gameObject: {gameObject != null}");

			if (isExtending || isIntroPlaying || this == null || gameObject == null)
				return;

#if MODULE_HAPTIC
			Haptic.Play(Haptic.HAPTIC_HARD);
#endif

			HidePreview();

			if (!isValidMove)
			{
				Debug.Log("Invalid move - bouncing back");
				arrowRenderer.OnClickBlocked();
				SetColor(invalidColor);
				AudioController.PlaySound(AudioController.AudioClips.actionError);
				StartCoroutine(ExtendAndBounceArrow());
				LevelController.OnArrowRemoved();
				isExtending = false;
			}
			else
			{
				AudioController.PlaySound(AudioController.AudioClips.actionDone);
				OnArrowCollected();
			}
			GridManager.Instance.LoseLife(this, !isValidMove);
		}

		public void OnArrowCollected()
		{
			StartCoroutine(ExtendArrow());
		}

		public void ShowPreview()
		{
			Vector2Int head = body[0];
			Vector2Int direction = GetCurrentDirection();

			previewPath = new List<Vector2Int>();
			previewPath.Add(head);

			Vector2Int checkPos = head + direction;

			// Build preview until blocked or hole
			while (GridManager.Instance.IsInsideGrid(checkPos))
			{
				//if (GridManager.Instance.IsBlocked(checkPos))
				//	break;

				//if (GridManager.Instance.HasArrowAt(checkPos))
				//	break;

				previewPath.Add(checkPos);

				if (GridManager.Instance.IsHole(checkPos))
					break;

				checkPos += direction;
			}

			// 🔥 EXTEND OUTSIDE GRID PROPERLY
			if (!GridManager.Instance.IsInsideGrid(checkPos))
			{
				int extendLength = 10; // how far outside you want

				Vector2Int extendPos = checkPos;

				for (int i = 0; i < extendLength; i++)
				{
					previewPath.Add(extendPos);
					extendPos += direction;
				}
			}

			if (arrowRenderer != null)
			{
				arrowRenderer.ShowPreview(previewPath);
			}
		}

		public void HidePreview()
		{
			if (arrowRenderer != null)
			{
				arrowRenderer.HidePreview();
			}
		}

		public void ShowHint()
		{
			arrowRenderer.ShowHint();
			ShowPreview();
		}

		// ============= ARROW EXTENSION LOGIC =============

		private IEnumerator ExtendAndBounceArrow()
		{
			isExtending = true;
			Vector2Int direction = GetCurrentDirection();

			List<Vector2Int> originalBody = new List<Vector2Int>(body);
			List<Vector2Int> pathTaken = new List<Vector2Int>();
			Stack<Vector2Int> vacatedTails = new Stack<Vector2Int>();

			while (true)
			{
				Vector2Int currentPos = body[0];
				Vector2Int nextPos = currentPos + direction;

				bool isBlocked = !GridManager.Instance.IsInsideGrid(nextPos) ||
								 GridManager.Instance.IsBlocked(nextPos) ||
								 (GridManager.Instance.HasArrowAt(nextPos) && !body.Contains(nextPos));

				if (!isBlocked && direction.x != 0 && direction.y != 0)
				{
					if (GridManager.Instance.DoesArrowCrossDiagonal(currentPos, nextPos))
					{
						isBlocked = true;
					}
				}

				if (isBlocked)
				{
					break;
				}

				// Move head forward
				body.Insert(0, nextPos);
				pathTaken.Add(nextPos);
				if (originalBodyPositions != null && originalBodyPositions.Contains(nextPos))
					GridManager.Instance.DisableGridDot(nextPos);

				// Remove tail to maintain constant length
				if (body.Count > initialLength)
				{
					Vector2Int vacatedPos = body[body.Count - 1];
					vacatedTails.Push(vacatedPos);
					body.RemoveAt(body.Count - 1);

					if (originalBodyPositions != null && originalBodyPositions.Contains(vacatedPos))
						GridManager.Instance.EnableGridDot(vacatedPos);
				}

				UpdateVisuals();
				yield return new WaitForSeconds(extendSpeed);
			}

			if (pathTaken.Count > 0)
			{
				yield return new WaitForSeconds(extendSpeed); // small pause at impact

				// Bounce back
				while (pathTaken.Count > 0)
				{
					Vector2Int currentHead = body[0];
					body.RemoveAt(0);
					if (originalBodyPositions != null && originalBodyPositions.Contains(currentHead))
						GridManager.Instance.EnableGridDot(currentHead);

					if (vacatedTails.Count > 0)
					{
						Vector2Int restoredTail = vacatedTails.Pop();
						body.Add(restoredTail);
						if (originalBodyPositions != null && originalBodyPositions.Contains(restoredTail))
							GridManager.Instance.DisableGridDot(restoredTail);
					}

					pathTaken.RemoveAt(pathTaken.Count - 1);
					UpdateVisuals();
					yield return new WaitForSeconds(extendSpeed);
				}
			}

			// Ensure body is exactly original
			body = new List<Vector2Int>(originalBody);
			foreach (var pos in body)
			{
				GridManager.Instance.DisableGridDot(pos);
			}
			UpdateVisuals();

			isExtending = false;
		}

		private IEnumerator ExtendArrow()
		{
			isExtending = true;
			isRemoving = true;
			Vector2Int direction = GetCurrentDirection();
			OnArrowComplete();

			while (true)
			{
				Vector2Int nextPos = body[0] + direction;

				// Move head forward
				body.Insert(0, nextPos);
				if (originalBodyPositions != null && originalBodyPositions.Contains(nextPos))
					GridManager.Instance.DisableGridDot(nextPos);

				// Remove tail to maintain constant length
				if (body.Count > initialLength)
				{
					Vector2Int vacatedPos = body[body.Count - 1];
					body.RemoveAt(body.Count - 1);

					if (originalBodyPositions != null && originalBodyPositions.Contains(vacatedPos))
						GridManager.Instance.EnableGridDot(vacatedPos);
				}

				UpdateVisuals();

				// Check if reached hole (inside grid)
				if (GridManager.Instance.IsInsideGrid(nextPos))
				{
					if (GridManager.Instance.IsHole(nextPos))
					{
						yield return new WaitForSeconds(extendSpeed);
						CompletePath();
						yield break;
					}
				}
				else
				{
					// Outside grid - check if fully outside
					if (IsFullyOutsideGrid())
					{
						CompletePathOutside();
						yield break;
					}
				}

				yield return new WaitForSeconds(extendSpeed);
			}
		}

		private bool IsFullyOutsideGrid()
		{
			for (int i = 0; i < body.Count; i++)
			{
				if (GridManager.Instance.IsInsideGrid(body[i]))
					return false;
			}
			return true;
		}

		public void CompletePathOutside()
		{
			// Arrow fully exited grid
			OnArrowComplete();
			Cleanup();
		}

		private void OnArrowComplete()
		{
			GridManager.Instance.RemoveArrow(tailPosition);
		}

		private Vector2Int GetCurrentDirection()
		{
			if (body.Count < 2) return Vector2Int.right;
			Vector2Int rawDir = body[0] - body[1];
			return new Vector2Int(System.Math.Sign(rawDir.x), System.Math.Sign(rawDir.y));
		}

		private void CompletePath()
		{
			tailPosition = body[body.Count - 1];
			StartCoroutine(AnimateRemoval());
		}

		private void UpdateVisuals()
		{
			if (arrowRenderer != null)
			{
				arrowRenderer.UpdatePath(body);
			}

			if (arrowHead != null && body.Count > 0)
			{
				arrowHead.UpdatePosition(body[0]);
			}

			UpdateHeadRotation();
		}

		// ============= INTRO ANIMATION =============

		/// <summary>
		/// Sets the delay between each segment reveal during the intro animation.
		/// </summary>
		public void SetIntroAnimSpeed(float speed)
		{
			introSegmentDelay = speed;
		}

		private Coroutine _introCoroutine;

		/// <summary>
		/// Plays an intro animation that grows the arrow from tail to head.
		/// All segments start hidden, then reveal one by one from tail toward head.
		/// </summary>
		public Coroutine PlayIntroAnimation()
		{
			_introCoroutine = StartCoroutine(IntroAnimationCoroutine());
			return _introCoroutine;
		}

		public void CompleteIntroAnimation()
		{
			if (!isIntroPlaying) return;

			if (_introCoroutine != null)
			{
				StopCoroutine(_introCoroutine);
				_introCoroutine = null;
			}

			// Final: show the complete arrow with the head
			arrowRenderer?.UpdatePath(body);

			if (arrowHead != null && arrowHead.GetGameObject() != null)
			{
				arrowHead.GetGameObject().SetActive(true);
				arrowHead.UpdatePosition(body[0]);
			}

			UpdateHeadRotation();
			isIntroPlaying = false;
		}

		public void CompleteActiveMovements()
		{
			if (isIntroPlaying)
			{
				CompleteIntroAnimation();
				return;
			}

			if (isExtending || isRemoving)
			{
				StopAllCoroutines();
				_introCoroutine = null;

				if (!isValidMove)
				{
					// Bouncing back: restore original position and make interactive again
					body = new List<Vector2Int>(originalBodyPositions);
					foreach (var pos in body)
					{
						GridManager.Instance.DisableGridDot(pos);
					}
					isExtending = false;
					UpdateVisuals();
				}
				else
				{
					// Exiting/collecting: instantly remove and cleanup
					if (GridManager.Instance != null)
					{
						GridManager.Instance.RemoveArrow(tailPosition);
					}
					Cleanup();
				}
			}
		}

		public void SetTheme(ArrowColorMode mode)
		{
			switch (mode)
			{
				case ArrowColorMode.White:
					currentThemeColor = Color.white;
					break;
				case ArrowColorMode.Black:
					currentThemeColor = Color.black;
					break;
				case ArrowColorMode.Colors:
					currentThemeColor = originalColor;
					break;
			}

			SetColor(currentThemeColor);
		}

		private IEnumerator IntroAnimationCoroutine()
		{
			isIntroPlaying = true;

			// Hide the arrow head initially
			if (arrowHead != null && arrowHead.GetGameObject() != null)
				arrowHead.GetGameObject().SetActive(false);

			// Progressively reveal from tail (last index) to head (index 0)
			// Build the path segment by segment: start with just the tail, add one segment at a time
			for (int revealCount = 1; revealCount <= body.Count; revealCount++)
			{
				// Show the last 'revealCount' segments (from tail side)
				int startIndex = body.Count - revealCount;
				List<Vector2Int> partialPath = body.GetRange(startIndex, revealCount);

				// Need at least 2 points for the renderer to draw
				if (partialPath.Count >= 2)
				{
					arrowRenderer?.UpdatePath(partialPath);
				}
				else if (partialPath.Count == 1)
				{
					// For a single point, create a tiny path so something is visible
					List<Vector2Int> tinyPath = new List<Vector2Int> { partialPath[0], partialPath[0] };
					arrowRenderer?.UpdatePath(tinyPath);
				}

				yield return new WaitForSeconds(introSegmentDelay);
			}

			// Final: show the complete arrow with the head
			arrowRenderer?.UpdatePath(body);

			if (arrowHead != null && arrowHead.GetGameObject() != null)
			{
				arrowHead.GetGameObject().SetActive(true);
				arrowHead.UpdatePosition(body[0]);
			}

			UpdateHeadRotation();
			isIntroPlaying = false;
		}

		// ============= VISUAL FEEDBACK =============

		private void SetColor(Color color)
		{
			arrowHead?.SetColor(color);
			arrowRenderer?.SetColor(color);
		}

		private IEnumerator AnimateRemoval()
		{
			isExtending = false;
			isRemoving = true;

			for (int i = body.Count - 1; i >= 0; i--)
			{
				if (i + 1 < body.Count)
				{
					Vector2Int vacatedPos = body[i + 1];
					if (originalBodyPositions != null && originalBodyPositions.Contains(vacatedPos))
						GridManager.Instance.EnableGridDot(vacatedPos);
				}

				List<Vector2Int> shrinkingPath = body.GetRange(0, i + 1);

				if (arrowRenderer != null)
					arrowRenderer.UpdatePath(shrinkingPath);

				yield return new WaitForSeconds(0.05f);
			}

			if (body.Count > 0)
			{
				if (originalBodyPositions != null && originalBodyPositions.Contains(body[0]))
					GridManager.Instance.EnableGridDot(body[0]);
			}

			GridManager.Instance.RemoveArrow(tailPosition);
			Cleanup();
		}

		void OnDestroy()
		{
			// Unregister from GridManager
			if (GridManager.Instance != null)
			{
				GridManager.Instance.UnregisterArrow(tailPosition);
			}

			if (arrowRenderer != null)
			{
				arrowRenderer.Destroy();
			}
		}
	}
}