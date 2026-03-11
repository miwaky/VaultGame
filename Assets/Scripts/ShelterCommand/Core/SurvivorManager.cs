using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Manages all survivors. Spawns the starting roster at runtime as simple capsule proxies.
    /// Placement is handled later — survivors are created at the origin of the Survivors root.
    /// </summary>
    public class SurvivorManager : MonoBehaviour
    {
        [Header("Survivor Data Assets (matched by name)")]
        [SerializeField] private List<SurvivorData> survivorDataList = new List<SurvivorData>();

        [Header("Initializer (optional)")]
        [Tooltip("Assign a SurvivorInitializer to use procedural generation. Leave empty to use the legacy capsule spawn.")]
        [SerializeField] private SurvivorInitializer survivorInitializer;

        public event Action<SurvivorBehavior> OnSurvivorSelected;
        public event Action<SurvivorBehavior> OnSurvivorDied;
        public event Action OnPopulationChanged;

        private readonly List<SurvivorBehavior> survivors = new List<SurvivorBehavior>();
        private SurvivorBehavior selectedSurvivor;

        // Starting roster — 5 survivors + Steve
        private static readonly string[] StartingNames =
            { "Steve", "Aria", "Borek", "Chloé", "Daan", "Elsa" };

        public IReadOnlyList<SurvivorBehavior> Survivors => survivors;
        public SurvivorBehavior SelectedSurvivor => selectedSurvivor;
        public int AliveSurvivorCount => GetAliveSurvivors().Count;

        private void Start()
        {
            if (survivorInitializer != null)
            {
                // SurvivorInitializer is the sole authority — always spawn fresh.
                survivorInitializer.SpawnAll();
                CollectFromInitializer();
            }
            else
            {
                SpawnStartingSurvivors();
            }
        }

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Advances all survivors by one day.</summary>
        public void TickDay(ShelterResources resources)
        {
            foreach (SurvivorBehavior survivor in survivors)
            {
                if (survivor != null && survivor.IsAlive)
                    survivor.TickDay(resources);
            }
        }

        /// <summary>Selects a survivor to receive orders.</summary>
        public void SelectSurvivor(SurvivorBehavior survivor)
        {
            selectedSurvivor = survivor;
            OnSurvivorSelected?.Invoke(survivor);
        }

        /// <summary>Deselects current survivor.</summary>
        public void DeselectSurvivor()
        {
            selectedSurvivor = null;
            OnSurvivorSelected?.Invoke(null);
        }

        /// <summary>Issues an order to the currently selected survivor.</summary>
        public bool IssueOrderToSelected(OrderType order, ShelterResources resources)
        {
            if (selectedSurvivor == null) return false;
            bool accepted = selectedSurvivor.IssueOrder(order, resources);
            if (accepted) DeselectSurvivor();
            return accepted;
        }

        /// <summary>Returns all alive survivors in a specific room (matched by ShelterRoom reference).</summary>
        public List<SurvivorBehavior> GetSurvivorsInRoom(ShelterRoom room)
        {
            List<SurvivorBehavior> result = new List<SurvivorBehavior>();
            foreach (SurvivorBehavior s in survivors)
            {
                if (s != null && s.IsAlive && s.CurrentRoom == room)
                    result.Add(s);
            }
            return result;
        }

        /// <summary>
        /// Registers an externally created survivor (e.g. rescue event).
        /// The caller is responsible for creating and positioning the GameObject.
        /// </summary>
        public void AddSurvivor(SurvivorBehavior sb)
        {
            if (sb == null) return;
            survivors.Add(sb);
            RegisterSurvivorEvents(sb);
            OnPopulationChanged?.Invoke();
        }

        public List<SurvivorBehavior> GetAliveSurvivors()
        {
            List<SurvivorBehavior> alive = new List<SurvivorBehavior>();
            foreach (SurvivorBehavior s in survivors)
            {
                if (s != null && s.IsAlive) alive.Add(s);
            }
            return alive;
        }

        // ── Private ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Collects survivors created by SurvivorInitializer into this manager's list.
        /// </summary>
        private void CollectFromInitializer()
        {
            foreach (SurvivorBehavior sb in survivorInitializer.SpawnedSurvivors)
            {
                if (sb == null) continue;
                survivors.Add(sb);
                RegisterSurvivorEvents(sb);
            }
            Debug.Log($"[SurvivorManager] {survivors.Count} survivants collectés depuis SurvivorInitializer.");
            OnPopulationChanged?.Invoke();
        }

        /// <summary>
        /// Spawns the 6 starting survivors as capsule GameObjects under a "Survivors" root.
        /// Any pre-existing SurvivorBehavior in the scene is collected first to avoid duplicates.
        /// Placement is intentionally left at world origin — positioning happens later.
        /// </summary>
        private void SpawnStartingSurvivors()
        {
            // Find or create the Survivors root
            GameObject survivorsRoot = GameObject.Find("Survivors");
            if (survivorsRoot == null)
                survivorsRoot = new GameObject("Survivors");

            // Spawn only the survivors that are not yet present
            HashSet<string> existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SurvivorBehavior sb in survivors)
                existingNames.Add(sb.gameObject.name);

            for (int i = 0; i < StartingNames.Length; i++)
            {
                string survivorName = StartingNames[i];
                if (existingNames.Contains(survivorName)) continue;

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = survivorName;
                go.transform.SetParent(survivorsRoot.transform);
                go.transform.localPosition = new Vector3(i * 1.2f, 0f, 0f);
                go.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
                go.GetComponent<Renderer>().material =
                    new Material(Shader.Find("Standard")) { color = Color.HSVToRGB(i / (float)StartingNames.Length, 0.5f, 0.75f) };

                SurvivorBehavior sb = go.AddComponent<SurvivorBehavior>();

                // Generate a minimal profile so the survivor has a name
                SurvivorGeneratedProfile profile = SurvivorProfileGenerator.Generate();
                profile.survivorName = survivorName;
                sb.SetProfile(profile);

                survivors.Add(sb);
                RegisterSurvivorEvents(sb);
            }

            Debug.Log($"[SurvivorManager] {survivors.Count} survivants actifs.");
            OnPopulationChanged?.Invoke();
        }

        private void RegisterSurvivorEvents(SurvivorBehavior sb)
        {
            sb.OnSurvivorDied += HandleSurvivorDeath;
        }

        private void HandleSurvivorDeath(SurvivorBehavior survivor)
        {
            if (selectedSurvivor == survivor) DeselectSurvivor();
            OnSurvivorDied?.Invoke(survivor);
            OnPopulationChanged?.Invoke();
        }
    }
}
