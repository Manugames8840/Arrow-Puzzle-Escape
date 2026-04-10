using System;
using UnityEngine;

namespace CubeStackPuzzle
{
    /// <summary>
    /// Orchestrates the cube-stack puzzle game flow.
    /// Spawns random destruction characters based on CubeStack definitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── References ─────────────────────────────────────────────────────
        [Header("── References ─────────────────────────────────")]
        [SerializeField] private CubeBoard cubeBoard;
        [SerializeField] private SlotManager slotManager;

        [Header("── Character Prefab ───────────────────────────")]
        [Tooltip("Optional prefab for the destruction character. If null, a primitive sphere is created.")]
        [SerializeField] private GameObject characterPrefab;

        // ── Events ─────────────────────────────────────────────────────────
        public event Action OnGameOver;
        public event Action OnLevelCleared;
        public event Action OnBoardGenerated;
        public event Action OnCharacterSpawned;

        // ── State ──────────────────────────────────────────────────────────
        public bool IsGameOver   { get; private set; }
        public bool IsLevelClear { get; private set; }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            // Auto-find if not assigned
            if (cubeBoard == null)   cubeBoard   = FindAnyObjectByType<CubeBoard>();
            if (slotManager == null) slotManager = FindAnyObjectByType<SlotManager>();

            // Subscribe to events
            if (slotManager != null)
                slotManager.OnAllSlotsFull += HandleGameOver;

            // Generate the board
            if (cubeBoard != null)
            {
                cubeBoard.GenerateBoard();
                OnBoardGenerated?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (slotManager != null)
                slotManager.OnAllSlotsFull -= HandleGameOver;
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Spawns a random destruction character.
        /// Color and destruction value are picked from a random CubeStack
        /// defined in CubeBoard.
        /// Called by the UI spawn button.
        /// </summary>
        public bool SpawnRandomCharacter()
        {
            if (IsGameOver || IsLevelClear) return false;

            if (cubeBoard == null || cubeBoard.CubeStacks == null || cubeBoard.CubeStacks.Length == 0)
            {
                Debug.LogWarning("[GameManager] No CubeStacks defined on CubeBoard!");
                return false;
            }

            // Pick a random CubeStack
            CubeStack[] stacks = cubeBoard.CubeStacks;
            CubeStack chosenStack = stacks[UnityEngine.Random.Range(0, stacks.Length)];

            CubeColor color          = chosenStack.color;
            int       destructionVal = chosenStack.cubeCount;

            // Create the character GameObject
            GameObject charGO;
            if (characterPrefab != null)
                charGO = Instantiate(characterPrefab);
            else
                charGO = new GameObject($"Character_{color}");

            DestructionCharacter character = charGO.GetComponent<DestructionCharacter>();
            if (character == null)
                character = charGO.AddComponent<DestructionCharacter>();

            // Try to occupy a 3D slot
            int slotIndex = slotManager.TryOccupySlot(character, out Transform slotTransform);
            if (slotIndex < 0)
            {
                // All slots full — game over was triggered
                Destroy(charGO);
                return false;
            }

            // Position at the slot transform
            if (slotTransform != null)
            {
                charGO.transform.position = slotTransform.position;
                charGO.transform.rotation = slotTransform.rotation;
                charGO.transform.SetParent(slotTransform);
            }

            // Initialize (starts the destruction coroutine)
            character.Init(color, destructionVal, slotIndex, cubeBoard, slotManager);

            // Listen for completion to check level clear
            character.OnDestructionFinished += CheckLevelClear;

            OnCharacterSpawned?.Invoke();
            return true;
        }

        /// <summary>
        /// Restart the level — regenerate the board and reset state.
        /// </summary>
        public void RestartLevel()
        {
            IsGameOver   = false;
            IsLevelClear = false;

            if (cubeBoard != null)
            {
                cubeBoard.ClearBoard();
                cubeBoard.GenerateBoard();
                OnBoardGenerated?.Invoke();
            }
        }

        // =====================================================================
        // INTERNAL
        // =====================================================================

        private void HandleGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            Debug.Log("[GameManager] GAME OVER — all slots are full!");
            OnGameOver?.Invoke();
        }

        private void CheckLevelClear()
        {
            if (IsLevelClear) return;

            if (cubeBoard != null && cubeBoard.IsBoardEmpty())
            {
                IsLevelClear = true;
                Debug.Log("[GameManager] LEVEL CLEARED — board is empty!");
                OnLevelCleared?.Invoke();
            }
        }
    }
}
