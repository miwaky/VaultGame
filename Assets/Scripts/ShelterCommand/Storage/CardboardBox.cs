using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// A cardboard box that accumulates <see cref="ResourceItemBehavior"/> items spawned
    /// by <see cref="ResourceSpawner"/>. The player carries the box and holds E near a
    /// <see cref="StorageShelf"/> to stock all contents at once.
    ///
    /// Each box is dedicated to a single <see cref="ResourceType"/> (set at first add).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CardboardBox : MonoBehaviour, IInteractable
    {
        // ── Constants ────────────────────────────────────────────────────────────
        private const int DefaultMaxCapacity = 6;

        // ── IInteractable ────────────────────────────────────────────────────────
        public string PromptLabel    => $"[E] Porter le carton ({ItemCount}/{MaxCapacity})";
        public bool   IsInteractable => !isCarried;

        // ── Inspector ────────────────────────────────────────────────────────────
        [Tooltip("Maximum number of items this box can hold before a new box is spawned.")]
        [SerializeField] private int maxCapacity = DefaultMaxCapacity;

        // ── Properties ───────────────────────────────────────────────────────────
        public int           ItemCount   => items.Count;
        public int           MaxCapacity => maxCapacity;
        public bool          IsFull      => items.Count >= maxCapacity;
        public bool          IsCarried   => isCarried;
        public bool          IsEmpty     => items.Count == 0;

        /// <summary>The resource type stored in this box. Null if empty.</summary>
        public ResourceType? ContentType { get; private set; }

        /// <summary>
        /// Fires when the player picks up this box so <see cref="ResourceSpawner"/>
        /// can clear its cached reference and spawn a fresh box on the next production tick.
        /// </summary>
        public event Action<CardboardBox> OnPickedUpEvent;

        // ── State ────────────────────────────────────────────────────────────────
        private readonly List<ResourceItemBehavior> items = new List<ResourceItemBehavior>();
        private bool          isCarried;
        private Rigidbody     rb;
        private Collider      col;
        private BoxSpawnPoint spawnPoint; // the point that spawned this box, released on pickup

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            rb  = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            // Start kinematic and anchored — identical to a StorageSlot item.
            // Physics only activates when the player drops the box in the world.
            SetPhysics(PhysicsMode.Anchored);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Adds an item into the box. Items are hidden inside the box visually.
        /// Returns false if the box is full or the item type doesn't match.
        /// </summary>
        public bool TryAddItem(ResourceItemBehavior item)
        {
            if (item == null || IsFull) return false;

            if (ContentType.HasValue && item.ItemData != null && item.ItemData.resourceType != ContentType.Value)
            {
                Debug.LogWarning($"[CardboardBox] Type mismatch: box is {ContentType.Value}, item is {item.ItemData.resourceType}.");
                return false;
            }

            if (!ContentType.HasValue && item.ItemData != null)
                ContentType = item.ItemData.resourceType;

            // Hide item inside the box
            item.gameObject.SetActive(false);
            items.Add(item);
            return true;
        }

        /// <summary>
        /// Removes and returns one item from the box (re-enables it in the world).
        /// Used by <see cref="StorageShelf.StockFromBox"/>.
        /// </summary>
        public ResourceItemBehavior TakeItem()
        {
            if (items.Count == 0) return null;

            ResourceItemBehavior item = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);

            item.gameObject.SetActive(true);
            item.transform.SetParent(null, true);

            if (items.Count == 0)
                ContentType = null;

            return item;
        }

        /// <summary>Called by <see cref="ItemCarrySystem"/> when the player picks up this box.</summary>
        public void OnPickedUp(Transform carryParent)
        {
            isCarried = true;

            // Free the spawn point so a new box can appear there.
            if (spawnPoint != null) { spawnPoint.Release(); spawnPoint = null; }

            // Notify ResourceSpawner (and any other listeners) so they clear their
            // cached reference — prevents them from filling this carried box further.
            OnPickedUpEvent?.Invoke(this);

            // Kill physics entirely before reparenting so the Rigidbody
            // doesn't interpolate from its old world position.
            SetPhysics(PhysicsMode.Carried);

            transform.SetParent(carryParent, true);  // worldPositionStays=true to keep world pos during reparent

            // Snap to carry point: zero local position & compensate baked FBX rotation
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Force the Rigidbody's internal position to match immediately —
            // prevents interpolation ghost from the previous world position.
            if (rb != null)
            {
                rb.position = transform.position;
                rb.rotation = transform.rotation;
            }
        }

        /// <summary>Releases the box back into the world — full physics resumes.</summary>
        public void OnDropped()
        {
            isCarried = false;
            transform.SetParent(null, true);
            SetPhysics(PhysicsMode.Free);
        }

        /// <summary>Called by <see cref="BoxSpawnPoint"/> right after spawning to register the origin point.</summary>
        public void RegisterSpawnPoint(BoxSpawnPoint point) => spawnPoint = point;

        // ── IInteractable ────────────────────────────────────────────────────────

        /// <summary>Player presses E on the box → ItemCarrySystem picks it up.</summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            ItemCarrySystem carry = interactionSystem.GetComponent<ItemCarrySystem>()
                                 ?? interactionSystem.GetComponentInParent<ItemCarrySystem>();
            if (carry == null) return;

            carry.TryPickUpBox(this);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private enum PhysicsMode { Anchored, Carried, Free }

        /// <summary>
        /// Controls the physical state of the box.
        /// Anchored : kinematic, collider on  → stays put on spawn point (like a shelf item).
        /// Carried  : kinematic, collider off → pure Transform child, zero physics drag.
        /// Free     : non-kinematic, collider on → falls and collides normally when dropped.
        /// </summary>
        private void SetPhysics(PhysicsMode mode)
        {
            switch (mode)
            {
                case PhysicsMode.Anchored:
                    if (rb  != null) { rb.isKinematic = true;  rb.useGravity = false; rb.interpolation = RigidbodyInterpolation.None; }
                    if (col != null) { col.isTrigger  = false; col.enabled   = true; }
                    break;

                case PhysicsMode.Carried:
                    // No interpolation while carried — the box is a pure Transform child.
                    if (rb  != null) { rb.isKinematic = true;  rb.useGravity = false; rb.interpolation = RigidbodyInterpolation.None; }
                    if (col != null) { col.enabled = false; }
                    break;

                case PhysicsMode.Free:
                    // Restore interpolation for smooth physics when dropped in the world.
                    if (rb  != null) { rb.isKinematic = false; rb.useGravity = true;  rb.interpolation = RigidbodyInterpolation.Interpolate; }
                    if (col != null) { col.isTrigger  = false; col.enabled   = true; }
                    break;
            }
        }
    }
}
