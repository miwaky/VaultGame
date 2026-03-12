using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// Attached to the Player GameObject.
    /// Manages picking up, holding, and dropping <see cref="ResourceItemBehavior"/> objects
    /// and <see cref="CardboardBox"/> boxes.
    ///
    /// Wire up <see cref="carryPoint"/> to a Transform child of the camera.
    /// </summary>
    public class ItemCarrySystem : MonoBehaviour
    {
        // ── Constants ────────────────────────────────────────────────────────────
        private const float BoxStockHoldDuration = 1.5f;

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Carry")]
        [Tooltip("Transform where the carried item or box is attached (child of player camera).")]
        [SerializeField] private Transform carryPoint;

        // ── Properties ───────────────────────────────────────────────────────────
        public bool                 IsCarrying      => carriedItem != null;
        public bool                 IsCarryingBox   => carriedBox  != null;
        public ResourceItemBehavior CarriedItem     => carriedItem;
        public CardboardBox         CarriedBox      => carriedBox;

        // ── State ────────────────────────────────────────────────────────────────
        private ResourceItemBehavior carriedItem;
        private CardboardBox         carriedBox;
        private StorageShelf         activeShelf;
        private float                holdTimer;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Keyboard.current == null) return;

            // G — drop whatever is carried
            if ((IsCarrying || IsCarryingBox) && Keyboard.current.gKey.wasPressedThisFrame)
                DropAll();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Picks up an individual item. Drops current carry first.</summary>
        public void TryPickUp(ResourceItemBehavior item)
        {
            if (item == null || item.IsCarried) return;
            if (IsCarrying || IsCarryingBox) DropAll();

            if (carryPoint == null) { Debug.LogError("[ItemCarrySystem] carryPoint non assigné."); return; }

            carriedItem = item;
            item.OnPickedUp(carryPoint);
            Debug.Log($"[ItemCarrySystem] Pris : {item.ItemData?.displayName}");
        }

        /// <summary>Picks up a cardboard box. Drops current carry first.</summary>
        public void TryPickUpBox(CardboardBox box)
        {
            if (box == null || box.IsCarried) return;
            if (IsCarrying || IsCarryingBox) DropAll();

            if (carryPoint == null) { Debug.LogError("[ItemCarrySystem] carryPoint non assigné."); return; }

            carriedBox = box;
            box.OnPickedUp(carryPoint);
            Debug.Log($"[ItemCarrySystem] Carton pris ({box.ItemCount} objets).");
        }

        /// <summary>Drops the currently carried item. Returns it.</summary>
        public ResourceItemBehavior Drop()
        {
            if (!IsCarrying) return null;
            ResourceItemBehavior dropped = carriedItem;
            dropped.OnDropped();
            carriedItem = null;
            return dropped;
        }

        /// <summary>Drops everything (item or box).</summary>
        public void DropAll()
        {
            if (IsCarrying)   { carriedItem.OnDropped(); carriedItem = null; }
            if (IsCarryingBox){ carriedBox.OnDropped();  carriedBox  = null; holdTimer = 0f; activeShelf = null; }
        }

        /// <summary>
        /// Notifies which shelf is currently in the player's crosshair.
        /// Used for the hold-E stocking mechanic.
        /// </summary>
        public void SetActiveShelf(StorageShelf shelf) => activeShelf = shelf;

        /// <summary>Hold-E stocking progress (0–1).</summary>
        public float StockProgress => IsCarryingBox ? Mathf.Clamp01(holdTimer / BoxStockHoldDuration) : 0f;
    }
}

