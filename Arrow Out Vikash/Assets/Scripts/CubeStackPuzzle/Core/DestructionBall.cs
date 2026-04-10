using System;
using System.Collections;
using UnityEngine;

namespace CubeStackPuzzle
{
	/// <summary>
	/// A projectile ball shot by a DestructionCharacter toward a front-row cube.
	/// Moves from the character's position to the target cube,
	/// then triggers destruction on impact.
	/// </summary>
	public class DestructionBall : MonoBehaviour
	{
		[SerializeField] private float speed = 18f;
		[SerializeField] private float hitDistanceThreshold = 0.15f;

		private Cube _targetCube;
		private Vector3 _targetPosition;
		private Action _onHit;
		private Material _ballMaterial;
		private bool _hasHit;

		// ════════════════════════════════════════════════════════════════════
		// PUBLIC API
		// ════════════════════════════════════════════════════════════════════

		/// <summary>
		/// Launch the ball toward a target cube.
		/// When it arrives, onHit is invoked and the ball self-destructs.
		/// </summary>
		/// <param name="targetCube">The front-row cube to hit.</param>
		/// <param name="ballColor">Color of the ball visual.</param>
		/// <param name="onHit">Callback invoked when the ball reaches the target.</param>
		public void Launch(Cube targetCube, CubeColor ballColor, Action onHit)
		{
			_targetCube = targetCube;
			_targetPosition = targetCube.transform.position;
			_onHit = onHit;
			_hasHit = false;

			// Create a small sphere visual
			CreateVisual(ballColor);

			StartCoroutine(MoveToTarget());
		}

		// ════════════════════════════════════════════════════════════════════
		// MOVEMENT
		// ════════════════════════════════════════════════════════════════════

		private IEnumerator MoveToTarget()
		{
			while (!_hasHit)
			{
				// Update target position in case cube moved (shouldn't happen, but safe)
				if (_targetCube != null && !_targetCube.IsDestroyed)
					_targetPosition = _targetCube.transform.position;

				// Move toward target
				Vector3 direction = (_targetPosition - transform.position).normalized;
				transform.position += direction * speed * Time.deltaTime;

				// Check if we've arrived
				float distance = Vector3.Distance(transform.position, _targetPosition);
				if (distance <= hitDistanceThreshold)
				{
					Hit();
					yield break;
				}

				yield return null;
			}
		}

		private void Hit()
		{
			if (_hasHit) return;
			_hasHit = true;

			// Invoke the callback so the character knows to proceed
			_onHit?.Invoke();

			// Self-destruct
			Destroy(gameObject);
		}

		// ════════════════════════════════════════════════════════════════════
		// VISUALS
		// ════════════════════════════════════════════════════════════════════

		private void CreateVisual(CubeColor color)
		{
			// If this GameObject already has a MeshRenderer, just tint it
			var existingMR = GetComponentInChildren<MeshRenderer>();
			if (existingMR != null)
			{
				_ballMaterial = new Material(existingMR.sharedMaterial ??
					new Material(Shader.Find("Universal Render Pipeline/Lit")));
				_ballMaterial.color = CubeColorUtility.ToUnityColor(color);
				existingMR.material = _ballMaterial;
				return;
			}

			// Otherwise, create a primitive sphere
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.SetParent(transform, false);
			sphere.transform.localPosition = Vector3.zero;
			sphere.transform.localScale = Vector3.one * 0.3f;
			sphere.name = "BallVisual";

			// Remove collider from primitive (we use distance check, not physics)
			var col = sphere.GetComponent<Collider>();
			if (col != null) Destroy(col);

			var mr = sphere.GetComponent<MeshRenderer>();
			if (mr != null)
			{
				_ballMaterial = new Material(mr.sharedMaterial ??
					new Material(Shader.Find("Universal Render Pipeline/Lit")));
				_ballMaterial.color = CubeColorUtility.ToUnityColor(color);
				mr.material = _ballMaterial;
			}
		}

		private void OnDestroy()
		{
			if (_ballMaterial != null)
				Destroy(_ballMaterial);
		}
	}
}
