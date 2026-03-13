using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Spawns <see cref="CardboardBox"/> objects on designated <see cref="BoxSpawnPoint"/>
    /// shelves when <see cref="HourlyProductionManager"/> triggers hourly production.
    ///
    /// Items are stored inside boxes (inactive) until the player carries the box to a
    /// <see cref="StorageShelf"/> and holds E to stock them.
    ///
    /// Setup:
    ///   1. Create empty child GameObjects on your spawn shelves.
    ///   2. Add <see cref="BoxSpawnPoint"/> to each and set its ResourceType.
    ///   3. Assign all food spawn points to foodSpawnPoints and
    ///      water spawn points to waterSpawnPoints.
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Item Prefabs")]
        [Tooltip("Prefab for a food item. Must have ResourceItemBehavior + Collider.")]
        [SerializeField] private GameObject foodItemPrefab;

        [Tooltip("Prefab for a water item. Must have ResourceItemBehavior + Collider.")]
        [SerializeField] private GameObject waterItemPrefab;

        [Header("Box Prefab")]
        [Tooltip("Prefab for the cardboard box. Must have CardboardBox + Rigidbody + Collider.")]
        [SerializeField] private GameObject cardboardBoxPrefab;

        [Header("Spawn Points")]
        [Tooltip("Dedicated shelf positions where food boxes can appear.")]
        [SerializeField] private BoxSpawnPoint[] foodSpawnPoints;

        [Tooltip("Dedicated shelf positions where water boxes can appear.")]
        [SerializeField] private BoxSpawnPoint[] waterSpawnPoints;

        // ── State ────────────────────────────────────────────────────────────────
        private float foodAccumulator;
        private float waterAccumulator;

        private CardboardBox currentFoodBox;
        private CardboardBox currentWaterBox;

        // ── Dependencies ─────────────────────────────────────────────────────────
        private HourlyProductionManager productionManager;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            productionManager = FindFirstObjectByType<HourlyProductionManager>();
            if (productionManager == null)
                Debug.LogError("[ResourceSpawner] HourlyProductionManager introuvable dans la scène.");
        }

        private void OnEnable()
        {
            if (productionManager != null)
                productionManager.OnHourlyProduction += HandleHourlyProduction;
        }

        private void OnDisable()
        {
            if (productionManager != null)
                productionManager.OnHourlyProduction -= HandleHourlyProduction;

            // Clean up event subscriptions on the tracked boxes
            UnsubscribeBox(currentFoodBox);
            UnsubscribeBox(currentWaterBox);
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void HandleHourlyProduction(float foodProduced, float waterProduced)
        {
            foodAccumulator  += foodProduced;
            waterAccumulator += waterProduced;

            while (foodAccumulator >= 1f)
            {
                AddItemToBox(foodItemPrefab, foodSpawnPoints, ref currentFoodBox);
                foodAccumulator -= 1f;
            }

            while (waterAccumulator >= 1f)
            {
                AddItemToBox(waterItemPrefab, waterSpawnPoints, ref currentWaterBox);
                waterAccumulator -= 1f;
            }
        }

        private void AddItemToBox(GameObject itemPrefab, BoxSpawnPoint[] spawnPoints, ref CardboardBox box)
        {
            if (itemPrefab == null) return;

            // A carried or destroyed box is no longer a valid target — always spawn a fresh one.
            // Note: IsCarried is always false here because OnPickedUpEvent already nullified 'box'.
            // The IsCarried guard is kept as a safety net.
            if (box == null || box.IsFull || box.IsCarried || !box.gameObject.activeInHierarchy)
            {
                UnsubscribeBox(box);
                box = SpawnBox(spawnPoints);
                SubscribeBox(box);
            }

            if (box == null) return;

            // Instantiate the item inactive at origin — never an active physics object
            // until TakeItem() re-enables it inside a StorageSlot.
            GameObject itemGo = Instantiate(itemPrefab, Vector3.zero, Quaternion.identity);
            itemGo.SetActive(false);

            ResourceItemBehavior item = itemGo.GetComponent<ResourceItemBehavior>();
            if (item == null)
            {
                Debug.LogWarning("[ResourceSpawner] Prefab d'item sans ResourceItemBehavior.");
                Destroy(itemGo);
                return;
            }

            if (!box.TryAddItem(item))
            {
                Destroy(itemGo);
                Debug.LogWarning("[ResourceSpawner] Impossible d'ajouter l'item dans le carton.");
            }
        }

        /// <summary>
        /// Called when the player picks up a tracked box.
        /// Clears the reference so the next production tick spawns a fresh box.
        /// </summary>
        private void OnBoxPickedUp(CardboardBox box)
        {
            if (box == currentFoodBox)
            {
                currentFoodBox = null;
                Debug.Log("[ResourceSpawner] Carton nourriture emporté — prochain tick créera un nouveau carton.");
            }
            else if (box == currentWaterBox)
            {
                currentWaterBox = null;
                Debug.Log("[ResourceSpawner] Carton eau emporté — prochain tick créera un nouveau carton.");
            }
        }

        private void SubscribeBox(CardboardBox box)
        {
            if (box != null) box.OnPickedUpEvent += OnBoxPickedUp;
        }

        private void UnsubscribeBox(CardboardBox box)
        {
            if (box != null) box.OnPickedUpEvent -= OnBoxPickedUp;
        }

        /// <summary>
        /// Finds the first free <see cref="BoxSpawnPoint"/> and spawns a box on it.
        /// Returns null if all points are occupied.
        /// </summary>
        private CardboardBox SpawnBox(BoxSpawnPoint[] spawnPoints)
        {
            if (cardboardBoxPrefab == null)
            {
                Debug.LogWarning("[ResourceSpawner] cardboardBoxPrefab non assigné.");
                return null;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[ResourceSpawner] Aucun BoxSpawnPoint assigné.");
                return null;
            }

            foreach (BoxSpawnPoint point in spawnPoints)
            {
                if (point == null || point.IsOccupied) continue;
                return point.Spawn(cardboardBoxPrefab);
            }

            Debug.Log("[ResourceSpawner] Tous les spawn points sont occupés.");
            return null;
        }
    }
}
