using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Marks a dedicated position on a shelf where a <see cref="CardboardBox"/> can spawn.
    /// Attach this to an empty child GameObject positioned on the shelf surface.
    ///
    /// Set <see cref="resourceType"/> to restrict this point to food or water boxes.
    /// The spawn point tracks occupancy so <see cref="ResourceSpawner"/> never stacks two boxes.
    /// </summary>
    public class BoxSpawnPoint : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Tooltip("Resource type this spawn point accepts. Matches the box content type.")]
        [SerializeField] private ResourceType resourceType;

        // ── Properties ───────────────────────────────────────────────────────────
        public ResourceType  ResourceType => resourceType;
        public bool          IsOccupied   => occupyingBox != null && !occupyingBox.IsCarried;
        public CardboardBox  OccupyingBox => occupyingBox;

        // ── State ────────────────────────────────────────────────────────────────
        private CardboardBox occupyingBox;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns a box prefab at this point and marks the point as occupied.
        /// Returns the spawned <see cref="CardboardBox"/>, or null on failure.
        /// </summary>
        public CardboardBox Spawn(GameObject boxPrefab)
        {
            if (boxPrefab == null)
            {
                Debug.LogWarning($"[BoxSpawnPoint] boxPrefab null sur {gameObject.name}.");
                return null;
            }

            GameObject go = Instantiate(boxPrefab, transform.position, transform.rotation);
            CardboardBox cb = go.GetComponent<CardboardBox>();

            if (cb == null)
            {
                Debug.LogError($"[BoxSpawnPoint] Le prefab '{boxPrefab.name}' n'a pas de CardboardBox.");
                Destroy(go);
                return null;
            }

            occupyingBox = cb;
            cb.RegisterSpawnPoint(this);
            Debug.Log($"[BoxSpawnPoint] Carton spawné sur '{gameObject.name}' à {transform.position}.");
            return cb;
        }

        /// <summary>Frees the spawn point (called when the player picks up the box).</summary>
        public void Release()
        {
            occupyingBox = null;
        }

#if UNITY_EDITOR
        // Visual gizmo to locate spawn points in the Scene view.
        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
            Gizmos.DrawRay(transform.position, transform.up * 0.4f);
        }
#endif
    }
}
