using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// A physical resource item in the world.
    /// Can be picked up by the player via <see cref="ItemCarrySystem"/>.
    /// Can be stored in a <see cref="StorageSlot"/> on a <see cref="StorageShelf"/>.
    ///
    /// Requires a Collider on the same GameObject for raycast detection.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ResourceItemBehavior : MonoBehaviour, IInteractable
    {
        // ── IInteractable ────────────────────────────────────────────────────────
        public string PromptLabel  => isStored ? "" : $"[E] Prendre : {(itemData != null ? itemData.displayName : "Objet")}";
        public bool   IsInteractable => !isStored && !isCarried;

        // ── Properties ───────────────────────────────────────────────────────────
        public ResourceItemData ItemData   => itemData;
        public bool             IsCarried  => isCarried;
        public bool             IsStored   => isStored;
        public StorageSlot      OccupiedSlot { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [SerializeField] private ResourceItemData itemData;

        // ── State ────────────────────────────────────────────────────────────────
        private bool isCarried;
        private bool isStored;
        private Rigidbody rb;
        private Collider  col;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            rb  = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            if (itemData != null)
                ApplyColor();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Initialises the item with its data after runtime spawn.</summary>
        public void Initialize(ResourceItemData data)
        {
            itemData = data;
            ApplyColor();
        }

        /// <summary>
        /// Called by <see cref="ItemCarrySystem"/> when the player picks up this item.
        /// Kinematic while carried so it doesn't drift.
        /// </summary>
        public void OnPickedUp(Transform carryParent)
        {
            isCarried = true;
            SetPhysics(false);

            transform.SetParent(carryParent, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Releases the item into the world with full physics.
        /// </summary>
        public void OnDropped()
        {
            isCarried = false;
            transform.SetParent(null, true);
            SetPhysics(true);
        }

        /// <summary>
        /// Snaps the item into a <see cref="StorageSlot"/>.
        /// Compensates the inherited world scale of the shelf hierarchy so the item
        /// keeps its original visual size regardless of the parent's scale.
        /// </summary>
        /// <param name="slot">The slot to snap into.</param>
        /// <param name="worldOffset">Additional world-space offset (e.g. lift above shelf surface).</param>
        public void OnStored(StorageSlot slot, Vector3 worldOffset = default)
        {
            isCarried    = false;
            isStored     = true;
            OccupiedSlot = slot;

            // Capture current world scale BEFORE reparenting
            Vector3 worldScaleBefore = transform.lossyScale;

            transform.SetParent(slot.transform, true);
            transform.position = slot.transform.position + worldOffset;
            transform.rotation = Quaternion.identity;

            // Reapply original world scale: localScale = desiredWorldScale / parentLossyScale
            Vector3 ps = slot.transform.lossyScale;
            transform.localScale = new Vector3(
                ps.x != 0f ? worldScaleBefore.x / ps.x : worldScaleBefore.x,
                ps.y != 0f ? worldScaleBefore.y / ps.y : worldScaleBefore.y,
                ps.z != 0f ? worldScaleBefore.z / ps.z : worldScaleBefore.z
            );

            SetPhysics(false); // stored items are kinematic, no gravity
        }

        /// <summary>Removes the item from its slot (pick up from shelf or consume).</summary>
        public void OnRemovedFromStorage()
        {
            isStored = false;
            if (OccupiedSlot != null)
            {
                OccupiedSlot.Release();
                OccupiedSlot = null;
            }
        }

        // ── IInteractable ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="OfficeInteractionSystem"/> when the player presses E.
        /// Delegates to <see cref="ItemCarrySystem"/>.
        /// </summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            ItemCarrySystem carry = interactionSystem.GetComponent<ItemCarrySystem>();
            if (carry == null)
                carry = interactionSystem.GetComponentInParent<ItemCarrySystem>();

            if (carry == null)
            {
                Debug.LogWarning("[ResourceItemBehavior] ItemCarrySystem introuvable sur le joueur.");
                return;
            }

            carry.TryPickUp(this);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Enables or disables Rigidbody simulation.
        /// enabled=true  → full physics (gravity, collisions).
        /// enabled=false → kinematic, no gravity (carried or stored).
        /// The collider remains active in both modes for raycast detection.
        /// </summary>
        private void SetPhysics(bool enabled)
        {
            if (rb  != null) { rb.isKinematic = !enabled; rb.useGravity = enabled; }
            if (col != null) col.enabled = true; // always on for raycast
        }

        private void ApplyColor()
        {
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                r.material = new Material(r.sharedMaterial);
                r.material.color = itemData.itemColor;
            }
        }
    }
}
