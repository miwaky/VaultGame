using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Represents a physical room in the shelter via a trigger volume.
    /// Place this component on an empty GameObject with a Box or Sphere Collider set to Is Trigger.
    /// Survivors use GetRandomSpawnPoint() to navigate into the room.
    ///
    /// The room can be defined in two ways (Inspector):
    ///   a) Automatic: uses the trigger bounds to generate random interior points (no extra setup).
    ///   b) Manual:    assign specific SpawnPoints child Transforms for precise control.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShelterRoom : MonoBehaviour
    {
        [Header("Room Identity")]
        [Tooltip("Display name shown in UI and logs.")]
        [SerializeField] private string roomName = "Room";

        [Header("Spawn Points (optional)")]
        [Tooltip("Explicit spawn positions inside this room. If empty, random points inside the trigger bounds are used.")]
        [SerializeField] private Transform[] spawnPoints;

        // ── Accessors ─────────────────────────────────────────────────────────────
        public string RoomName => roomName;

        // ── Private ───────────────────────────────────────────────────────────────
        private Collider roomCollider;

        // Track which points are currently occupied to spread survivors out
        private readonly List<int> usedPointIndices = new List<int>();

        private void Awake()
        {
            roomCollider = GetComponent<Collider>();
            roomCollider.isTrigger = true;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a world-space position inside this room.
        /// Prefers explicit SpawnPoints; falls back to a random point within trigger bounds.
        /// </summary>
        public Vector3 GetRandomSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Pick a point that hasn't been used yet; reset if all used
                List<int> available = new List<int>();
                for (int i = 0; i < spawnPoints.Length; i++)
                    if (!usedPointIndices.Contains(i) && spawnPoints[i] != null)
                        available.Add(i);

                if (available.Count == 0) usedPointIndices.Clear();

                // Rebuild after clear
                for (int i = 0; i < spawnPoints.Length; i++)
                    if (!usedPointIndices.Contains(i) && spawnPoints[i] != null)
                        available.Add(i);

                if (available.Count > 0)
                {
                    int chosen = available[Random.Range(0, available.Count)];
                    usedPointIndices.Add(chosen);
                    return spawnPoints[chosen].position;
                }
            }

            // Fallback: random point within the trigger's axis-aligned bounding box
            return RandomPointInBounds(roomCollider.bounds);
        }

        /// <summary>Clears used-point tracking (call at start of each day).</summary>
        public void ResetOccupancy() => usedPointIndices.Clear();

        // ── Private helpers ───────────────────────────────────────────────────────

        private static Vector3 RandomPointInBounds(Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.min.y,   // stay at floor level
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.25f);
            Collider col = GetComponent<Collider>();
            if (col != null) Gizmos.DrawCube(col.bounds.center, col.bounds.size);

            if (spawnPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (Transform sp in spawnPoints)
                    if (sp != null) Gizmos.DrawSphere(sp.position, 0.15f);
            }
        }
    }
}
