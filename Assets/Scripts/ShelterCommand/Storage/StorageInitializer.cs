using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Fills the shelter's StorageShelves with a starting stock of food and water items
    /// at the beginning of the game, so the player sees resources already on the shelves.
    ///
    /// Setup:
    ///   - Assign foodItemPrefab / waterItemPrefab (same prefabs used by ResourceSpawner).
    ///   - Set startingFood and startingWater counts.
    ///   - The component finds ALL StorageShelf instances in the scene automatically.
    ///   - Shelves are filled in order: first food, then water. The first shelf that has
    ///     free slots receives items, then the next one, etc.
    ///
    /// This component destroys itself after initialisation to avoid overhead.
    /// </summary>
    public class StorageInitializer : MonoBehaviour
    {
        [Header("Item Prefabs")]
        [SerializeField] private GameObject foodItemPrefab;
        [SerializeField] private GameObject waterItemPrefab;

        [Header("Starting Stock")]
        [SerializeField] private int startingFood  = 10;
        [SerializeField] private int startingWater = 10;

        private void Start()
        {
            // Attendre un frame pour que tous les StorageShelf aient eu le temps de s'enregistrer
            // dans StorageRegistry via leur Awake().
            SpawnItems(foodItemPrefab,  ResourceType.Food,  startingFood);
            SpawnItems(waterItemPrefab, ResourceType.Water, startingWater);

            Debug.Log($"[StorageInitializer] Stockage initial terminé : {startingFood} nourritures, {startingWater} eaux.");
            Destroy(this);
        }

        // ── Private ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Instancie <paramref name="count"/> items et les place sur les étagères via
        /// StorageRegistry.FindShelfForType() — même logique que le dépôt en jeu :
        /// étagère du même type en priorité, étagère neutre en fallback.
        /// </summary>
        private void SpawnItems(GameObject prefab, ResourceType type, int count)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[StorageInitializer] Prefab manquant pour {type}.");
                return;
            }

            int placed = 0;

            for (int i = 0; i < count; i++)
            {
                StorageShelf shelf = StorageRegistry.FindShelfForType(type);
                if (shelf == null)
                {
                    Debug.LogWarning($"[StorageInitializer] Plus d'espace disponible pour {type} ({i}/{count} placés).");
                    break;
                }

                StorageSlot slot = shelf.GetFreeSlot();
                if (slot == null) continue; // ne devrait pas arriver après FindShelfForType

                GameObject itemGo = Instantiate(prefab);
                ResourceItemBehavior item = itemGo.GetComponent<ResourceItemBehavior>();

                if (item == null)
                {
                    Debug.LogWarning($"[StorageInitializer] {prefab.name} n'a pas de ResourceItemBehavior.");
                    Destroy(itemGo);
                    continue;
                }

                item.OnStored(slot, shelf.ItemPlacementOffset);
                slot.Occupy(item);

                // Verrouille le type de l'étagère dès le premier item
                shelf.LockType(type);

                itemGo.SetActive(true);
                placed++;
            }

            Debug.Log($"[StorageInitializer] {placed}/{count} {type} placés.");
        }
    }
}
