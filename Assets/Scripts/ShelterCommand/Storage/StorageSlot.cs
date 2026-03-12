using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// A single physical slot on a <see cref="StorageShelf"/>.
    /// Place this as a child of the shelf with its Transform centered on the slot position.
    /// </summary>
    public class StorageSlot : MonoBehaviour
    {
        public bool IsOccupied { get; private set; }
        public ResourceItemBehavior StoredItem { get; private set; }

        /// <summary>Marks the slot as occupied by an item.</summary>
        public void Occupy(ResourceItemBehavior item)
        {
            IsOccupied = true;
            StoredItem = item;
        }

        /// <summary>Clears the slot.</summary>
        public void Release()
        {
            IsOccupied = false;
            StoredItem = null;
        }
    }
}
