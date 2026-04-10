using System;
using System.Collections;
using UnityEngine;

namespace CubeStackPuzzle
{
    /// <summary>
    /// A destruction character that occupies a 3D slot.
    /// It shoots balls toward front-row cubes of its color.
    /// Each ball destroys one cube on impact, then the column applies gravity.
    /// </summary>
    public class DestructionCharacter : MonoBehaviour
    {
        [Header("Runtime State (Read Only)")]
        [SerializeField] private CubeColor targetColor;
        [SerializeField] private int remainingValue;
        [SerializeField] private int assignedSlotIndex = -1;

        [Header("Shooting Settings")]
        [Tooltip("Delay between each shot.")]
        [SerializeField] private float timeBetweenShots = 0.5f;

        [Tooltip("How often to check for available cubes when waiting.")]
        [SerializeField] private float waitCheckInterval = 0.2f;

        [Tooltip("Optional prefab for the ball projectile. If null, a primitive sphere is created.")]
        [SerializeField] private GameObject ballPrefab;

        private CubeBoard _board;
        private SlotManager _slotManager;
        private Coroutine _destructionRoutine;
        private Material _characterMaterial;
        private Cube _currentlyReservedCube; // track for cleanup

        // ── Events ─────────────────────────────────────────────────────────
        public event Action OnStepCompleted;
        public event Action OnDestructionFinished;

        // ── Public Properties ──────────────────────────────────────────────
        public CubeColor TargetColor    => targetColor;
        public int       RemainingValue => remainingValue;
        public int       SlotIndex      => assignedSlotIndex;

        // ════════════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize this character and start shooting.
        /// </summary>
        public void Init(CubeColor color, int destructionValue, int slotIndex,
                         CubeBoard board, SlotManager slotManager)
        {
            targetColor       = color;
            remainingValue    = destructionValue;
            assignedSlotIndex = slotIndex;
            _board            = board;
            _slotManager      = slotManager;
            _currentlyReservedCube = null;

            gameObject.name = $"Character_{color}_Slot{slotIndex}";

            ApplyCharacterVisual(color);

            _destructionRoutine = StartCoroutine(ShootingSequence());
        }

        // ════════════════════════════════════════════════════════════════════
        // SHOOTING COROUTINE
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator ShootingSequence()
        {
            // Small initial delay so the player can see the character spawn
            yield return new WaitForSeconds(0.3f);

            while (remainingValue > 0)
            {
                // 1. Wait until a matching, unreserved front-row cube is available
                Cube frontCube = null;
                while (frontCube == null)
                {
                    frontCube = _board.FindFrontRowCubeByColor(targetColor);

                    if (frontCube == null)
                    {
                        // No matching cube right now — wait and check again
                        yield return new WaitForSeconds(waitCheckInterval);

                        // Safety: if the board is empty AND no cubes of our color
                        // exist anywhere (not just front row), stop waiting
                        if (_board.IsBoardEmpty())
                            goto Done;
                    }
                }

                // 2. Reserve the cube so no other character targets it
                if (!frontCube.Reserve())
                {
                    // Another character just reserved it — try again next frame
                    yield return null;
                    continue;
                }

                _currentlyReservedCube = frontCube;

                // 3. Get the column for gravity later
                CubeColumn column = _board.GetColumnForCube(frontCube);
                if (column == null)
                {
                    frontCube.ReleaseReservation();
                    _currentlyReservedCube = null;
                    yield return null;
                    continue;
                }

                // 4. Shoot a ball toward the cube
                bool ballHit = false;

                ShootBall(frontCube, () =>
                {
                    // On hit: remove the front cube (destroys it + applies gravity)
                    column.RemoveFrontCube();
                    ballHit = true;
                });

                // 5. Wait for the ball to reach and destroy the cube
                yield return new WaitUntil(() => ballHit);

                _currentlyReservedCube = null;

                // 6. Decrement destruction value
                remainingValue--;

                // 7. Fire step event
                OnStepCompleted?.Invoke();

                // 8. Check if board is now empty
                if (_board.IsBoardEmpty())
                    break;

                // 9. Wait before next shot
                yield return new WaitForSeconds(timeBetweenShots);
            }

            Done:

            // Done — release the slot
            OnDestructionFinished?.Invoke();

            if (_slotManager != null)
                _slotManager.ReleaseSlot(assignedSlotIndex);

            // Self-destruct
            Destroy(gameObject, 0.15f);
        }

        // ════════════════════════════════════════════════════════════════════
        // BALL SPAWNING
        // ════════════════════════════════════════════════════════════════════

        private void ShootBall(Cube targetCube, Action onHit)
        {
            // Create the ball
            GameObject ballGO;
            if (ballPrefab != null)
                ballGO = Instantiate(ballPrefab, transform.position, Quaternion.identity);
            else
                ballGO = new GameObject($"Ball_{targetColor}");

            ballGO.transform.position = transform.position;

            DestructionBall ball = ballGO.GetComponent<DestructionBall>();
            if (ball == null)
                ball = ballGO.AddComponent<DestructionBall>();

            // Launch toward the target
            ball.Launch(targetCube, targetColor, onHit);
        }

        // ════════════════════════════════════════════════════════════════════
        // VISUALS
        // ════════════════════════════════════════════════════════════════════

        private void ApplyCharacterVisual(CubeColor color)
        {
            Color unityColor = CubeColorUtility.ToUnityColor(color);

            // Tint existing MeshRenderer if present
            var mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                _characterMaterial = new Material(mr.sharedMaterial ??
                    new Material(Shader.Find("Universal Render Pipeline/Lit")));
                _characterMaterial.color = unityColor;
                mr.material = _characterMaterial;
                return;
            }

            // Otherwise create a simple visual capsule (looks like a character)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale    = new Vector3(0.4f, 0.6f, 0.4f);
            visual.name = "CharacterVisual";

            // Remove collider from the visual primitive
            var col = visual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var visualMR = visual.GetComponent<MeshRenderer>();
            if (visualMR != null)
            {
                _characterMaterial = new Material(visualMR.sharedMaterial ??
                    new Material(Shader.Find("Universal Render Pipeline/Lit")));
                _characterMaterial.color = unityColor;
                visualMR.material = _characterMaterial;
            }
        }

        private void OnDestroy()
        {
            if (_destructionRoutine != null)
                StopCoroutine(_destructionRoutine);

            // Release any reserved cube if character is destroyed prematurely
            if (_currentlyReservedCube != null && !_currentlyReservedCube.IsDestroyed)
                _currentlyReservedCube.ReleaseReservation();

            if (_characterMaterial != null)
                Destroy(_characterMaterial);
        }
    }
}
