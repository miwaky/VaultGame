using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// A physical storage shelf with a fixed number of slots.
    /// The player can deposit a carried item by pressing E while looking at the shelf.
    ///
    /// Setup in the Inspector:
    ///   - Add <see cref="StorageSlot"/> components as children and reference them in <see cref="slots"/>.
    ///   - Alternatively, use <see cref="autoGenerateSlots"/> to generate slots procedurally in a row.
    ///
    /// Also acts as a registry queried by the consumption system to find stored items by type.
    /// </summary>
    public class StorageShelf : MonoBehaviour, IInteractable
    {
        // ── IInteractable ────────────────────────────────────────────────────────
        public string PromptLabel
        {
            get
            {
                if (AcceptedType.HasValue)
                    return $"[E] Déposer ({AcceptedType.Value})";
                return "[E] Déposer ici";
            }
        }
        public bool IsInteractable => true;

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Slots")]
        [Tooltip("Manual list of StorageSlot children. Leave empty to use auto-generation.")]
        [SerializeField] private List<StorageSlot> slots = new List<StorageSlot>();

        [Tooltip("If true, generates slot GameObjects automatically on Awake.")]
        [SerializeField] private bool autoGenerateSlots = true;

        [Tooltip("Number of slots to auto-generate.")]
        [SerializeField] private int slotCount = 6;

        [Tooltip("Local offset between each auto-generated slot.")]
        [SerializeField] private Vector3 slotSpacing = new Vector3(0.35f, 0f, 0f);

        [Tooltip("Local origin of the first auto-generated slot.")]
        [SerializeField] private Vector3 slotOrigin = new Vector3(-0.875f, 0f, 0f);

        [Tooltip("World-space offset applied to an item when it is placed in a slot (e.g. lift it above the shelf surface).")]
        [SerializeField] private Vector3 itemPlacementOffset = new Vector3(0f, 0.05f, 0f);

        // ── Properties ───────────────────────────────────────────────────────────

        /// <summary>World-space offset applied when an item is snapped into a slot.</summary>
        public Vector3 ItemPlacementOffset => itemPlacementOffset;

        /// <summary>
        /// The resource type this shelf is dedicated to.
        /// Set automatically on first deposit. Null means the shelf is neutral.
        /// </summary>
        public ResourceType? AcceptedType { get; private set; }

        private void Awake()
        {
            if (autoGenerateSlots && slots.Count == 0)
                GenerateSlots();

            StorageRegistry.Register(this);
        }

        private void OnDestroy()
        {
            StorageRegistry.Unregister(this);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Force le type accepté par cette étagère.
        /// Utilisé par StorageInitializer pour verrouiller le type dès le premier item placé.
        /// </summary>
        public void LockType(ResourceType type)
        {
            AcceptedType = type;
        }

        /// <summary>Returns the first free slot, or null if the shelf is full.</summary>
        public StorageSlot GetFreeSlot()
        {
            foreach (StorageSlot slot in slots)
                if (!slot.IsOccupied) return slot;
            return null;
        }

        /// <summary>
        /// Pile (stack) logic — index 0 est le fond, le dernier ajouté est en haut.
        /// On consomme toujours depuis le haut (dernier slot occupé = indice le plus élevé).
        /// L'index 0 est donc toujours le DERNIER à être consommé.
        /// </summary>
        public bool ConsumeItem(ResourceType type)
        {
            // Trouver le slot occupé le plus haut (dernier entré)
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                StorageSlot slot = slots[i];
                if (slot.IsOccupied &&
                    slot.StoredItem != null &&
                    slot.StoredItem.ItemData != null &&
                    slot.StoredItem.ItemData.resourceType == type)
                {
                    ResourceItemBehavior item = slot.StoredItem;
                    item.OnRemovedFromStorage();
                    Destroy(item.gameObject);
                    RefreshAcceptedType();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Détache et retourne l'item du haut de la pile (dernier entré, premier sorti).
        /// Index 0 est toujours le dernier récupéré.
        /// </summary>
        public ResourceItemBehavior TakeItem(ResourceType type)
        {
            // Trouver le slot occupé le plus haut (dernier entré)
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                StorageSlot slot = slots[i];
                if (slot.IsOccupied &&
                    slot.StoredItem != null &&
                    slot.StoredItem.ItemData != null &&
                    slot.StoredItem.ItemData.resourceType == type)
                {
                    ResourceItemBehavior item = slot.StoredItem;
                    item.OnRemovedFromStorage();
                    RefreshAcceptedType();
                    return item;
                }
            }
            return null;
        }

        /// <summary>Returns all items of the given type currently stored on this shelf.</summary>
        public IEnumerable<ResourceItemBehavior> GetItemsOfType(ResourceType type)
        {
            foreach (StorageSlot slot in slots)
            {
                if (slot.IsOccupied &&
                    slot.StoredItem != null &&
                    slot.StoredItem.ItemData != null &&
                    slot.StoredItem.ItemData.resourceType == type)
                    yield return slot.StoredItem;
            }
        }

        // ── IInteractable ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="OfficeInteractionSystem"/> when the player presses E.
        /// Accepts both individual items and cardboard boxes.
        /// Dépôt : on cherche d'abord une étagère avec le même type, sinon une étagère neutre.
        /// Retrait : on prend depuis le haut de la pile (le dernier arrivé).
        /// </summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            ItemCarrySystem carry = interactionSystem.GetComponent<ItemCarrySystem>()
                                 ?? interactionSystem.GetComponentInParent<ItemCarrySystem>();

            if (carry == null) return;

            // ── Cardboard box held ───────────────────────────────────────────────
            if (carry.IsCarryingBox)
            {
                CardboardBox box = carry.CarriedBox;

                if (box.IsEmpty)
                {
                    Debug.Log("[StorageShelf] Le carton est vide, rien à déposer.");
                    return;
                }

                StockFromBox(box);

                if (box.IsEmpty)
                    carry.DropAll();

                return;
            }

            // ── Individual item held — déposer ───────────────────────────────────
            if (carry.IsCarrying)
            {
                ResourceItemBehavior item = carry.CarriedItem;

                // Refus si étagère dédiée à un autre type
                if (AcceptedType.HasValue && item.ItemData != null && item.ItemData.resourceType != AcceptedType.Value)
                {
                    Debug.Log($"[StorageShelf] Refus : étagère réservée à {AcceptedType.Value}.");
                    return;
                }

                StorageSlot slot = GetFreeSlot();
                if (slot == null) { Debug.Log("[StorageShelf] Étagère pleine."); return; }

                carry.Drop();
                StoreItemInSlot(item, slot);
                return;
            }

            // ── Rien tenu — ramasser depuis le haut de la pile ───────────────────
            PickUpFromShelf(interactionSystem);
        }

        /// <summary>
        /// Deposits all compatible items from a <see cref="CardboardBox"/> into free slots.
        /// Called when the player presses E while carrying a box in front of the shelf.
        /// </summary>
        public void StockFromBox(CardboardBox box)
        {
            if (box == null || box.ItemCount == 0)
            {
                Debug.Log("[StorageShelf] StockFromBox : carton null ou vide.");
                return;
            }

            // Type check — refuse only if both types are known AND incompatible
            if (AcceptedType.HasValue && box.ContentType.HasValue && box.ContentType.Value != AcceptedType.Value)
            {
                Debug.LogWarning($"[StorageShelf] Refus : étagère={AcceptedType.Value}, carton={box.ContentType.Value}.");
                return;
            }

            int deposited = 0;
            while (box.ItemCount > 0)
            {
                StorageSlot slot = GetFreeSlot();
                if (slot == null)
                {
                    Debug.Log("[StorageShelf] Étagère pleine, dépôt partiel.");
                    break;
                }

                ResourceItemBehavior item = box.TakeItem();
                if (item == null) break;

                StoreItemInSlot(item, slot);
                deposited++;
            }

            Debug.Log($"[StorageShelf] {deposited} objet(s) stocké(s) — étagère={AcceptedType}, carton restant={box.ItemCount}.");
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void StoreItemInSlot(ResourceItemBehavior item, StorageSlot slot)
        {
            // Lock shelf type on first deposit
            if (!AcceptedType.HasValue && item.ItemData != null)
                AcceptedType = item.ItemData.resourceType;

            slot.Occupy(item);
            item.OnStored(slot, itemPlacementOffset);
            Debug.Log($"[StorageShelf] '{item.ItemData?.displayName}' → slot {slot.name}.");
        }

        /// <summary>
        /// Resets <see cref="AcceptedType"/> if the shelf is now completely empty,
        /// allowing a different resource type to be deposited next.
        /// </summary>
        private void RefreshAcceptedType()
        {
            if (!AcceptedType.HasValue) return;

            foreach (StorageSlot slot in slots)
            {
                if (slot.IsOccupied) return; // still has items — keep the lock
            }

            AcceptedType = null;
            Debug.Log($"[StorageShelf] Étagère '{gameObject.name}' vidée — type déverrouillé.");
        }

        private void PickUpFromShelf(OfficeInteractionSystem interactionSystem)
        {
            ItemCarrySystem carry = interactionSystem.GetComponent<ItemCarrySystem>()
                                 ?? interactionSystem.GetComponentInParent<ItemCarrySystem>();
            if (carry == null || carry.IsCarrying) return;

            // Prendre depuis le haut de la pile — le dernier slot occupé (indice le plus élevé)
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                StorageSlot slot = slots[i];
                if (slot.IsOccupied && slot.StoredItem != null)
                {
                    ResourceItemBehavior item = slot.StoredItem;
                    item.OnRemovedFromStorage();
                    RefreshAcceptedType();
                    carry.TryPickUp(item);
                    return;
                }
            }
        }

        private void GenerateSlots()
        {
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotGo = new GameObject($"Slot_{i}");
                slotGo.transform.SetParent(transform, false);
                slotGo.transform.localPosition = slotOrigin + slotSpacing * i;
                slots.Add(slotGo.AddComponent<StorageSlot>());
            }
        }
    }
}
