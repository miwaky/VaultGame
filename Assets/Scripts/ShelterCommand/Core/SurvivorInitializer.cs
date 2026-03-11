using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Generates and spawns all survivors at game start.
    ///
    /// Flow per survivor:
    ///   1. SurvivorProfileGenerator.Generate()  — random data
    ///   2. Create a fresh Capsule GameObject     — no prefab dependency
    ///   3. Add SurvivorBehavior + all components by code
    ///   4. SetProfile() + SetStartingRoom()      — survivor receives all info
    ///   5. Warp to SurvivorSpawnZone → PlaceInStartingRoom()
    ///   6. SurvivorIdleMovement walks to its IdlePoint
    /// </summary>
    public class SurvivorInitializer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Population")]
        [Tooltip("Number of survivors generated at game start.")]
        [SerializeField] private int survivorCount = 5;

        [Header("Spawn Zone")]
        [Tooltip("Survivors appear here. Place this Transform inside the shelter on the NavMesh.")]
        [SerializeField] private Transform survivorSpawnZone;
        [Tooltip("Maximum horizontal scatter around the spawn zone center (0 = exact position).")]
        [SerializeField] private float spawnRadius = 1f;

        [Header("Starting Room")]
        [Tooltip("The ShelterRoom where all survivors appear on day 1. " +
                 "Requires a ShelterRoom component + Collider set to Is Trigger.")]
        [SerializeField] private ShelterRoom startingRoom;

        [Header("Idle Points")]
        [Tooltip("One Transform per survivor. Each walks to a unique point after spawning.")]
        [SerializeField] private Transform[] idlePoints;

        [Header("Roster Config (optional)")]
        [Tooltip("ScriptableObject populated at runtime with all generated profiles.")]
        [SerializeField] private SurvivorRosterConfig rosterConfig;

        // ── Constants ─────────────────────────────────────────────────────────────

        private const float NavMeshSampleRadius  = 6f;
        private const float MinSeparationDistance = 0.8f;
        private const int   MaxPlacementAttempts  = 30;

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly List<SurvivorBehavior> spawnedSurvivors = new List<SurvivorBehavior>();
        private readonly HashSet<int>           usedIdlePoints   = new HashSet<int>();

        public IReadOnlyList<SurvivorBehavior> SpawnedSurvivors => spawnedSurvivors;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        // SpawnAll() is driven by SurvivorManager — do NOT call it from Start()
        // to prevent double-spawn when both components share the same GameObject.

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Generates and spawns all survivors. Safe to call again to reinitialize.</summary>
        public void SpawnAll()
        {
            spawnedSurvivors.Clear();
            usedIdlePoints.Clear();

            GameObject root = GetOrCreateRoot();

            for (int i = 0; i < survivorCount; i++)
            {
                SurvivorBehavior sb = SpawnOne(root.transform);
                if (sb != null) spawnedSurvivors.Add(sb);
            }

            Debug.Log($"[SurvivorInitializer] {spawnedSurvivors.Count}/{survivorCount} survivants spawned.");

            if (rosterConfig != null)
            {
                List<SurvivorGeneratedProfile> profiles = new List<SurvivorGeneratedProfile>();
                foreach (SurvivorBehavior sb in spawnedSurvivors)
                    if (sb?.GeneratedProfile != null) profiles.Add(sb.GeneratedProfile);
                rosterConfig.Populate(profiles);
            }
        }

        // ── Private — pipeline ────────────────────────────────────────────────────

        private SurvivorBehavior SpawnOne(Transform parent)
        {
            // 1. Generate profile
            SurvivorGeneratedProfile profile = SurvivorProfileGenerator.Generate();

            // 2. Find NavMesh position near SurvivorSpawnZone
            if (!TryFindSpawnPosition(out Vector3 spawnPos))
            {
                Debug.LogWarning("[SurvivorInitializer] Aucune position libre trouvée — survivant ignoré.");
                return null;
            }

            // 3. Create capsule GameObject
            GameObject go = CreateCapsuleObject(spawnPos, parent);

            // 4. Add SurvivorBehavior and configure it
            SurvivorBehavior sb = go.AddComponent<SurvivorBehavior>();
            sb.SetProfile(profile);
            if (startingRoom != null) sb.SetStartingRoom(startingRoom);

            // 5. Add NavMeshAgent
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            ConfigureAgent(agent);

            // 6. Add SurvivorInteractable (enables E-key interaction)
            go.AddComponent<SurvivorInteractable>();

            // 7. Warp into starting room (now that agent is on NavMesh)
            if (startingRoom != null)
                sb.PlaceInStartingRoom();

            // 8. Walk to idle point
            Transform idle = AssignIdlePoint();
            if (idle != null)
            {
                SurvivorIdleMovement mov = go.AddComponent<SurvivorIdleMovement>();
                mov.SetTarget(idle.position);
            }

            return sb;
        }

        // ── Private — capsule factory ─────────────────────────────────────────────

        private static GameObject CreateCapsuleObject(Vector3 position, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(parent);
            go.transform.position   = position;
            go.transform.localScale = Vector3.one;

            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.76f, 0.60f, 0.42f)
                };
            }

            // CapsuleCollider added by CreatePrimitive is reused by SurvivorBehavior.Awake()
            return go;
        }

        // ── Private — NavMeshAgent ────────────────────────────────────────────────

        private static void ConfigureAgent(NavMeshAgent agent)
        {
            agent.height                = 1.8f;
            agent.radius                = 0.3f;
            agent.baseOffset            = 0f;
            agent.speed                 = 1.2f;
            agent.angularSpeed          = 120f;
            agent.acceleration          = 6f;
            agent.stoppingDistance      = 0.4f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            agent.avoidancePriority     = Random.Range(30, 70);
        }

        // ── Private — spawn positioning ───────────────────────────────────────────

        private bool TryFindSpawnPosition(out Vector3 result)
        {
            if (survivorSpawnZone == null)
            {
                Debug.LogError("[SurvivorInitializer] SurvivorSpawnZone non assigné !");
                result = Vector3.zero;
                return false;
            }

            Vector3 center = survivorSpawnZone.position;

            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                Vector2 circle    = spawnRadius > 0f ? Random.insideUnitCircle * spawnRadius : Vector2.zero;
                Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
                    continue;

                if (!IsOccupied(hit.position))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = Vector3.zero;
            return false;
        }

        private bool IsOccupied(Vector3 pos)
        {
            foreach (SurvivorBehavior sb in spawnedSurvivors)
                if (sb != null && Vector3.Distance(sb.transform.position, pos) < MinSeparationDistance)
                    return true;
            return false;
        }

        // ── Private — idle points ─────────────────────────────────────────────────

        private Transform AssignIdlePoint()
        {
            if (idlePoints == null || idlePoints.Length == 0) return null;

            List<int> free = new List<int>();
            for (int i = 0; i < idlePoints.Length; i++)
                if (!usedIdlePoints.Contains(i) && idlePoints[i] != null) free.Add(i);

            if (free.Count == 0) return null;

            int chosen = free[Random.Range(0, free.Count)];
            usedIdlePoints.Add(chosen);
            return idlePoints[chosen];
        }

        // ── Private — helpers ─────────────────────────────────────────────────────

        private static GameObject GetOrCreateRoot()
        {
            GameObject root = GameObject.Find("Survivors");
            if (root == null) root = new GameObject("Survivors");
            return root;
        }
    }
}
