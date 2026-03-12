using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Global registry of all active <see cref="StorageShelf"/> instances.
    /// Used by the consumption system to find and remove items without scene queries.
    /// Static — no MonoBehaviour needed.
    /// </summary>
    public static class StorageRegistry
    {
        private static readonly List<StorageShelf> shelves = new List<StorageShelf>();

        /// <summary>Registers a shelf when it is enabled.</summary>
        public static void Register(StorageShelf shelf)
        {
            if (!shelves.Contains(shelf))
                shelves.Add(shelf);
        }

        /// <summary>Unregisters a shelf when it is destroyed.</summary>
        public static void Unregister(StorageShelf shelf)
        {
            shelves.Remove(shelf);
        }

        /// <summary>
        /// Finds and destroys the first stored item of the given type across all shelves.
        /// Returns true if an item was found and consumed.
        /// </summary>
        public static bool ConsumeItem(ResourceType type)
        {
            foreach (StorageShelf shelf in shelves)
            {
                if (shelf.ConsumeItem(type))
                {
                    Debug.Log($"[StorageRegistry] Consommé : {type} depuis {shelf.gameObject.name}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>Counts total items of a given type across all shelves.</summary>
        public static int CountItems(ResourceType type)
        {
            int count = 0;
            foreach (StorageShelf shelf in shelves)
                foreach (ResourceItemBehavior _ in shelf.GetItemsOfType(type))
                    count++;
            return count;
        }

        /// <summary>
        /// Retourne la première étagère qui accepte ce type ET a un slot libre.
        /// Priorité : 1) étagère déjà dédiée à ce type, 2) étagère neutre vide.
        /// Retourne null si aucune étagère disponible.
        /// </summary>
        public static StorageShelf FindShelfForType(ResourceType type)
        {
            StorageShelf neutral = null;

            foreach (StorageShelf shelf in shelves)
            {
                if (shelf.GetFreeSlot() == null) continue; // étagère pleine, ignorer

                if (shelf.AcceptedType.HasValue)
                {
                    if (shelf.AcceptedType.Value == type)
                        return shelf; // étagère déjà dédiée à ce type → prioritaire
                }
                else
                {
                    neutral ??= shelf; // étagère neutre → en réserve
                }
            }

            return neutral; // null si aucune place disponible
        }

        /// <summary>Returns all registered shelves (read-only).</summary>
        public static IReadOnlyList<StorageShelf> AllShelves => shelves;
    }
}
