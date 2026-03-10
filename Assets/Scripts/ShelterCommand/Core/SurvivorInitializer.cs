using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Spawns and fully initializes survivors at game start.
    /// Responsibilities:
    ///   1. Spawn each survivor at SurvivorSpawnArea.
    ///   2. Assign a procedurally generated profile (stats, profession, traits, age).
    ///   3. Instantiate a visual mesh chosen randomly from SurvivorMeshes.
    ///   4. Assign a unique IdlePoint to each survivor and move them there via NavMeshAgent.
    ///
    /// Designed to work alongside SurvivorManager: call Initialize() from SurvivorManager or
    /// assign it as a dependency in ShelterGameManager.
    /// </summary>
    public class SurvivorInitializer : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Population")]
        [Tooltip("Number of survivors to spawn if no SurvivorData list is provided.")]
        [SerializeField] private int survivorCount = 6;

        [Header("Named Survivors (optional)")]
        [Tooltip("Pre-built SurvivorData assets. If provided, these are used instead of random generation.")]
        [SerializeField] private List<SurvivorData> namedSurvivorData = new List<SurvivorData>();

        [Header("Spawn Area")]
        [Tooltip("Survivors appear at this transform's position (randomized within a radius).")]
        [SerializeField] private Transform survivorSpawnArea;
        [SerializeField] private float spawnRadius = 1.5f;

        [Header("Idle Points")]
        [Tooltip("One point per survivor. Each survivor moves to a unique point after spawn.")]
        [SerializeField] private Transform[] idlePoints;

        [Header("Visual Meshes")]
        [Tooltip("Pool of base GameObjects. One is randomly chosen per survivor to build the visual.")]
        [SerializeField] private GameObject[] survivorMeshes;

        [Header("Fallback Settings")]
        [Tooltip("Scale applied to the visual mesh child.")]
        [SerializeField] private Vector3 meshScale = new Vector3(0.4f, 0.7f, 0.4f);

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<SurvivorBehavior> spawnedSurvivors = new List<SurvivorBehavior>();
        private readonly HashSet<int> usedIdlePointIndices = new HashSet<int>();

        // ── Constants ─────────────────────────────────────────────────────────────
        private const float NavMeshSampleRadius = 3f;

        // ── Accessors ─────────────────────────────────────────────────────────────
        public IReadOnlyList<SurvivorBehavior> SpawnedSurvivors => spawnedSurvivors;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            SpawnAll();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns and initializes all survivors. Called automatically on Start.
        /// Can also be called manually to re-initialize the population.
        /// </summary>
        public void SpawnAll()
        {
            spawnedSurvivors.Clear();
            usedIdlePointIndices.Clear();

            // Find or create the Survivors root GameObject
            GameObject survivorsRoot = GameObject.Find("Survivors") ?? new GameObject("Survivors");

            int count = namedSurvivorData != null && namedSurvivorData.Count > 0
                ? namedSurvivorData.Count
                : survivorCount;

            for (int i = 0; i < count; i++)
            {
                SurvivorData dataAsset = (namedSurvivorData != null && i < namedSurvivorData.Count)
                    ? namedSurvivorData[i]
                    : null;

                SurvivorBehavior sb = SpawnOneSurvivor(i, dataAsset, survivorsRoot.transform);
                if (sb != null)
                    spawnedSurvivors.Add(sb);
            }

            Debug.Log($"[SurvivorInitializer] {spawnedSurvivors.Count} survivants initialisés.");
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private SurvivorBehavior SpawnOneSurvivor(int index, SurvivorData dataAsset, Transform parent)
        {
            // 1. Determine spawn position
            Vector3 spawnPos = GetSpawnPosition(index);

            // 2. Create root GameObject
            GameObject go = new GameObject("Survivor_" + index);
            go.transform.SetParent(parent);
            go.transform.position = spawnPos;

            // 3. Attach SurvivorBehavior
            SurvivorBehavior sb = go.AddComponent<SurvivorBehavior>();

            // 4. Generate or convert profile
            SurvivorGeneratedProfile profile = dataAsset != null
                ? SurvivorProfileGenerator.FromSurvivorData(dataAsset)
                : SurvivorProfileGenerator.Generate();

            sb.SetGeneratedProfile(profile);
            go.name = profile.survivorName;

            // Assign the ScriptableObject data for legacy compatibility
            if (dataAsset != null)
                sb.SetData(dataAsset);

            // 5. Build visual
            BuildVisual(go, index);

            // 6. Add NavMeshAgent for movement
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.height = 1.8f;
            agent.radius = 0.3f;
            agent.speed  = 1.2f;
            agent.stoppingDistance = 0.4f;
            agent.angularSpeed = 120f;
            agent.acceleration = 6f;

            // 7. Add SurvivorMarker for camera interaction
            SurvivorMarker marker = go.AddComponent<SurvivorMarker>();

            // 8. Add SurvivorInteractable for direct FPS interaction
            go.AddComponent<SurvivorInteractable>();

            // 9. Assign and move to idle point
            Transform idleTarget = AssignIdlePoint();
            if (idleTarget != null)
            {
                SurvivorIdleMovement movement = go.AddComponent<SurvivorIdleMovement>();
                movement.SetTarget(idleTarget.position);
            }

            return sb;
        }

        private void BuildVisual(GameObject parent, int index)
        {
            if (survivorMeshes != null && survivorMeshes.Length > 0)
            {
                // Pick a random mesh from the pool
                int meshIndex = Random.Range(0, survivorMeshes.Length);
                GameObject meshPrefab = survivorMeshes[meshIndex];

                if (meshPrefab != null)
                {
                    GameObject visual = Instantiate(meshPrefab, parent.transform);
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale    = Vector3.one;
                    return;
                }
            }

            // Fallback: colored capsule
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Visual";
            capsule.transform.SetParent(parent.transform);
            capsule.transform.localPosition = Vector3.zero;
            capsule.transform.localScale    = meshScale;

            float hue = index / Mathf.Max(1f, survivorCount - 1f);
            capsule.GetComponent<Renderer>().material =
                new Material(Shader.Find("Standard")) { color = Color.HSVToRGB(hue, 0.55f, 0.75f) };

            // Remove capsule collider (root handles physics)
            Collider col = capsule.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private Vector3 GetSpawnPosition(int index)
        {
            Vector3 center = survivorSpawnArea != null ? survivorSpawnArea.position : Vector3.zero;
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

            // Snap to NavMesh if available
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
                return hit.position;

            return candidate;
        }

        private Transform AssignIdlePoint()
        {
            if (idlePoints == null || idlePoints.Length == 0) return null;

            // Build a shuffled list of free indices
            List<int> freeIndices = new List<int>();
            for (int i = 0; i < idlePoints.Length; i++)
            {
                if (!usedIdlePointIndices.Contains(i) && idlePoints[i] != null)
                    freeIndices.Add(i);
            }

            if (freeIndices.Count == 0) return null;

            int chosen = freeIndices[Random.Range(0, freeIndices.Count)];
            usedIdlePointIndices.Add(chosen);
            return idlePoints[chosen];
        }
    }
}
