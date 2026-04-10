using System;
using UnityEngine;

namespace CubeStackPuzzle
{
    /// <summary>
    /// Manages destruction character slots using 3D Transform positions.
    /// Characters spawn at world-space slot transforms, NOT UI elements.
    /// </summary>
    public class SlotManager : MonoBehaviour
    {
        public static SlotManager Instance { get; private set; }

        // ── Configuration ──────────────────────────────────────────────────
        [Header("── Slot Transforms (3D Positions) ────────────")]
        [Tooltip("Drag scene Transforms here. Each is a world-space position where a character can be placed.")]
        [SerializeField] private Transform[] slotPositions;

        [Header("── Lock Settings ─────────────────────────────")]
        [Tooltip("How many slots are available from the start. The rest are locked.")]
        [SerializeField] private int initiallyUnlocked = 5;

        // ── Events ─────────────────────────────────────────────────────────
        public event Action<int> OnSlotOccupied;
        public event Action<int> OnSlotReleased;
        public event Action<int> OnSlotUnlocked;
        public event Action      OnAllSlotsFull;

        // ── Slot Data ──────────────────────────────────────────────────────
        [Serializable]
        public class Slot
        {
            public bool isLocked;
            public bool isOccupied;
            [HideInInspector] public DestructionCharacter currentCharacter;
        }

        [Header("── Runtime State (Read Only) ──────────────────")]
        [SerializeField] private Slot[] slots;

        // ── Public Accessors ───────────────────────────────────────────────
        public int    TotalSlots => slots != null ? slots.Length : 0;
        public Slot[] Slots      => slots;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            InitializeSlots();
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        private void InitializeSlots()
        {
            if (slotPositions == null || slotPositions.Length == 0)
            {
                Debug.LogWarning("[SlotManager] No slot transforms assigned!");
                slots = new Slot[0];
                return;
            }

            slots = new Slot[slotPositions.Length];
            for (int i = 0; i < slotPositions.Length; i++)
            {
                slots[i] = new Slot
                {
                    isLocked         = (i >= initiallyUnlocked),
                    isOccupied       = false,
                    currentCharacter = null
                };
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Try to place a character into an available slot.
        /// Returns the slot index and outputs the slot Transform.
        /// Returns -1 if all slots are full (game over).
        /// </summary>
        public int TryOccupySlot(DestructionCharacter character, out Transform slotTransform)
        {
            slotTransform = null;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].isLocked || slots[i].isOccupied)
                    continue;

                slots[i].isOccupied       = true;
                slots[i].currentCharacter  = character;
                slotTransform              = slotPositions[i];
                OnSlotOccupied?.Invoke(i);
                return i;
            }

            // All available slots are full → game over
            OnAllSlotsFull?.Invoke();
            return -1;
        }

        /// <summary>
        /// Release a slot after its character finishes.
        /// </summary>
        public void ReleaseSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return;

            slots[index].isOccupied       = false;
            slots[index].currentCharacter  = null;
            OnSlotReleased?.Invoke(index);
        }

        /// <summary>
        /// Unlock a specific locked slot by index.
        /// </summary>
        public void UnlockSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            if (!slots[index].isLocked) return;

            slots[index].isLocked = false;
            OnSlotUnlocked?.Invoke(index);
        }

        /// <summary>
        /// Unlock the first locked slot found.
        /// </summary>
        public void UnlockNextLockedSlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].isLocked)
                {
                    UnlockSlot(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Returns true if all non-locked slots are occupied.
        /// </summary>
        public bool AreAllAvailableSlotsFull()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].isLocked && !slots[i].isOccupied)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Number of currently free (unlocked + unoccupied) slots.
        /// </summary>
        public int GetAvailableSlotCount()
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].isLocked && !slots[i].isOccupied)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Get the world-space position of a slot by index.
        /// </summary>
        public Vector3 GetSlotPosition(int index)
        {
            if (index < 0 || index >= slotPositions.Length) return Vector3.zero;
            return slotPositions[index] != null ? slotPositions[index].position : Vector3.zero;
        }

        // =====================================================================
        // GIZMOS — Visualize slots in Scene view
        // =====================================================================

        private void OnDrawGizmos()
        {
            if (slotPositions == null) return;

            for (int i = 0; i < slotPositions.Length; i++)
            {
                if (slotPositions[i] == null) continue;

                bool locked = (i >= initiallyUnlocked);

                // In play mode, use runtime state
                if (Application.isPlaying && slots != null && i < slots.Length)
                {
                    if (slots[i].isLocked)
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                    else if (slots[i].isOccupied)
                        Gizmos.color = new Color(0.9f, 0.3f, 0.2f, 0.8f);
                    else
                        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.8f);
                }
                else
                {
                    Gizmos.color = locked
                        ? new Color(0.5f, 0.5f, 0.5f, 0.6f)
                        : new Color(0.2f, 0.8f, 0.3f, 0.8f);
                }

                Gizmos.DrawWireSphere(slotPositions[i].position, 0.4f);
                Gizmos.DrawSphere(slotPositions[i].position, 0.15f);

#if UNITY_EDITOR
                string label = locked ? $"Slot {i} [LOCKED]" : $"Slot {i}";
                UnityEditor.Handles.Label(slotPositions[i].position + Vector3.up * 0.6f, label);
#endif
            }
        }
    }
}
