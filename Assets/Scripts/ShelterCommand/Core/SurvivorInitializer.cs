using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Generates and spawns all survivors at game start.
    ///
    /// Flow per survivor:
    ///   1. SurvivorProfileGenerator.Generate()  — random data (includes gender)
    ///   2. Create a root GameObject              — no capsule mesh on the root
    ///   3. Add SurvivorBehavior + all components by code
    ///   4. SetProfile() + SetStartingRoom()      — survivor receives all info
    ///   5. Spawn a gender-correct visual prefab  — child of the root
    ///   6. Warp to SurvivorSpawnZone → PlaceInStartingRoom()
    ///   7. SurvivorIdleMovement walks to its IdlePoint
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

        [Header("Visual System")]
        [Tooltip("ScriptableObject with the male and female visual prefab pools.")]
        [SerializeField] private SurvivorVisualConfig visualConfig;

        // ── Constants ─────────────────────────────────────────────────────────────

        private const float NavMeshSampleRadius   = 6f;
        private const float MinSeparationDistance = 0.8f;
        private const int   MaxPlacementAttempts  = 30;
        private const string VisualChildName      = "Model";

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
            // 1. Generate profile (includes gender)
            SurvivorGeneratedProfile profile = SurvivorProfileGenerator.Generate();

            // 2. Find NavMesh position near SurvivorSpawnZone
            if (!TryFindSpawnPosition(out Vector3 spawnPos))
            {
                Debug.LogWarning("[SurvivorInitializer] Aucune position libre trouvée — survivant ignoré.");
                return null;
            }

            // 3. Create root GameObject (no mesh on the root itself)
            GameObject go = CreateRootObject(profile.survivorName, spawnPos, parent);

            // 4. Add CapsuleCollider on root for raycasts and NavMeshAgent footprint
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.3f;
            col.center = new Vector3(0f, 0.9f, 0f);

            // 5. Add SurvivorBehavior and configure it
            SurvivorBehavior sb = go.AddComponent<SurvivorBehavior>();
            sb.SetProfile(profile);
            if (startingRoom != null) sb.SetStartingRoom(startingRoom);

            // 6. Add NavMeshAgent
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            ConfigureAgent(agent);

            // 7. Spawn visual model as child and retrieve its Animator
            Animator modelAnimator = SpawnVisualModel(go.transform, profile.gender);

            // 8. Add SurvivorAnimatorController and wire the Animator
            SurvivorAnimatorController animCtrl = go.AddComponent<SurvivorAnimatorController>();
            if (modelAnimator != null)
                animCtrl.SetAnimator(modelAnimator);

            // 9. Add SurvivorInteractable (enables E-key interaction)
            go.AddComponent<SurvivorInteractable>();

            // 10. Warp into starting room (now that agent is on NavMesh)
            if (startingRoom != null)
                sb.PlaceInStartingRoom();

            // 11. Walk to idle point
            Transform idle = AssignIdlePoint();
            if (idle != null)
            {
                SurvivorIdleMovement mov = go.AddComponent<SurvivorIdleMovement>();
                mov.SetTarget(idle.position);
            }

            return sb;
        }

        // ── Private — visual model ────────────────────────────────────────────────

        /// <summary>
        /// Instantiates a random visual prefab matching the gender as a child of parent.
        /// Returns the Animator found on the instantiated model, or null if unavailable.
        /// </summary>
        private Animator SpawnVisualModel(Transform parent, SurvivorGender gender)
        {
            if (visualConfig == null)
            {
                Debug.LogWarning("[SurvivorInitializer] SurvivorVisualConfig non assigné — aucun modèle 3D.");
                return null;
            }

            GameObject prefab = visualConfig.GetRandomPrefab(gender);
            if (prefab == null) return null;

            GameObject model = Instantiate(prefab, parent);
            model.name = VisualChildName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            // Keep the prefab's authored scale (0.4, 0.4, 0.4) — do NOT reset to Vector3.one

            // Disable any collider on the visual model — the root already owns one
            foreach (Collider c in model.GetComponentsInChildren<Collider>())
                c.enabled = false;

            return model.GetComponentInChildren<Animator>();
        }

        // ── Private — root factory ────────────────────────────────────────────────

        private static GameObject CreateRootObject(string survivorName, Vector3 position, Transform parent)
        {
            GameObject go = new GameObject(survivorName);
            go.transform.SetParent(parent);
            go.transform.position   = position;
            go.transform.localScale = Vector3.one;
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
