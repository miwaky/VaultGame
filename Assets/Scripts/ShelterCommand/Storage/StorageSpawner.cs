using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Spawns physical resource items directly onto storage shelves.
    ///
    /// Used by <see cref="RadioCallManager"/> (survivor return) and
    /// <see cref="Core.DialogueManager"/> (AddResource events).
    ///
    /// Place one instance in the scene and configure the item prefab catalogue.
    /// All prefabs must have a <see cref="ResourceItemBehavior"/> component.
    /// </summary>
    public class StorageSpawner : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static StorageSpawner Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [System.Serializable]
        public struct ItemPrefabEntry
        {
            public ResourceType   resourceType;
            public GameObject     prefab;
        }

        [Tooltip("Map each ResourceType to its physical item prefab.")]
        [SerializeField] private ItemPrefabEntry[] prefabCatalogue = System.Array.Empty<ItemPrefabEntry>();

        // ── Private ───────────────────────────────────────────────────────────────
        private readonly Dictionary<ResourceType, GameObject> prefabMap =
            new Dictionary<ResourceType, GameObject>();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (ItemPrefabEntry e in prefabCatalogue)
                if (e.prefab != null)
                    prefabMap[e.resourceType] = e.prefab;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns <paramref name="count"/> physical items of the given <paramref name="type"/>
        /// onto the best available shelf. Items that don't fit are silently discarded.
        /// </summary>
        /// <returns>Number of items actually placed.</returns>
        public int SpawnItems(ResourceType type, int count)
        {
            if (!prefabMap.TryGetValue(type, out GameObject prefab))
            {
                Debug.LogWarning($"[StorageSpawner] Pas de prefab pour {type}.");
                return 0;
            }

            int placed = 0;

            for (int i = 0; i < count; i++)
            {
                StorageShelf shelf = StorageRegistry.FindShelfForType(type);
                if (shelf == null)
                {
                    Debug.LogWarning($"[StorageSpawner] Plus d'espace pour {type} ({placed}/{count} placés).");
                    break;
                }

                StorageSlot slot = shelf.GetFreeSlot();
                if (slot == null) continue;

                GameObject             go   = Instantiate(prefab);
                ResourceItemBehavior   item = go.GetComponent<ResourceItemBehavior>();

                if (item == null)
                {
                    Debug.LogWarning($"[StorageSpawner] {prefab.name} n'a pas de ResourceItemBehavior.");
                    Destroy(go);
                    continue;
                }

                item.OnStored(slot, shelf.ItemPlacementOffset);
                slot.Occupy(item);
                shelf.LockType(type);
                go.SetActive(true);
                placed++;
            }

            if (placed > 0)
                StorageRegistry.NotifyItemAdded();

            return placed;
        }

        /// <summary>
        /// Convenience: spawns items for every <see cref="ResourceEntry"/> in a list,
        /// respecting the <see cref="ResourceSelectionMode"/> (All or RandomOne).
        /// </summary>
        public void SpawnFromEntries(ResourceEntry[] entries, ResourceSelectionMode mode)
        {
            if (entries == null || entries.Length == 0) return;

            if (mode == ResourceSelectionMode.RandomOne)
            {
                ResourceEntry picked = entries[Random.Range(0, entries.Length)];
                SpawnItems(picked.resourceType, picked.ResolveAmount());
            }
            else
            {
                foreach (ResourceEntry e in entries)
                    SpawnItems(e.resourceType, e.ResolveAmount());
            }
        }

        /// <summary>
        /// Removes <paramref name="count"/> physical items of the given type from shelves.
        /// Returns the number actually removed.
        /// </summary>
        public int RemoveItems(ResourceType type, int count)
        {
            int removed = 0;
            for (int i = 0; i < count; i++)
            {
                bool ok = StorageRegistry.ConsumeItem(type);
                if (!ok) break;
                removed++;
            }
            return removed;
        }

        /// <summary>
        /// Removes items for every <see cref="ResourceEntry"/>, respecting selection mode.
        /// </summary>
        public void RemoveFromEntries(ResourceEntry[] entries, ResourceSelectionMode mode)
        {
            if (entries == null || entries.Length == 0) return;

            if (mode == ResourceSelectionMode.RandomOne)
            {
                ResourceEntry picked = entries[Random.Range(0, entries.Length)];
                RemoveItems(picked.resourceType, picked.ResolveAmount());
            }
            else
            {
                foreach (ResourceEntry e in entries)
                    RemoveItems(e.resourceType, e.ResolveAmount());
            }
        }
    }
}
