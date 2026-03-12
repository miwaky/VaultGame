using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Manages all active exploration missions.
    ///
    /// — StartMission() hides each survivor (SetOnMission true → SetActive false)
    ///   and fires OnMissionStarted.
    /// — Each call to TickDay() advances all missions; completed ones return
    ///   survivors to the shelter and deposit gathered resources via
    ///   ShelterResourceManager.AddResources(), then fire OnMissionCompleted.
    ///
    /// Hooks automatically into DayManager.OnDayStarted in Start().
    /// </summary>
    public class ExplorationManager : MonoBehaviour
    {
        // ── Resource rates ────────────────────────────────────────────────────────
        [Header("Ressources par survivant par jour")]
        [SerializeField] private float foodPerSurvivorPerDay      = 5f;
        [SerializeField] private float waterPerSurvivorPerDay     = 3f;
        [SerializeField] private float materialsPerSurvivorPerDay = 2f;

        // ── State ──────────────────────────────────────────────────────────────────
        private readonly List<ExplorationMission> activeMissions = new List<ExplorationMission>();

        // ── Dependencies (resolved in Start) ─────────────────────────────────────
        private ShelterResourceManager resourceManager;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a new mission begins.</summary>
        public event Action<ExplorationMission> OnMissionStarted;

        /// <summary>Raised when a mission completes and survivors are back in the shelter.</summary>
        public event Action<ExplorationMission> OnMissionCompleted;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>All currently active (not yet completed) missions.</summary>
        public IReadOnlyList<ExplorationMission> ActiveMissions => activeMissions;

        /// <summary>
        /// Launches a new exploration mission.
        /// Hides each survivor from the shelter (SetOnMission true) and fires OnMissionStarted.
        /// </summary>
        public ExplorationMission StartMission(
            IEnumerable<SurvivorBehavior> survivors,
            string destination,
            int durationDays)
        {
            if (survivors == null) throw new ArgumentNullException(nameof(survivors));

            ExplorationMission mission = new ExplorationMission(survivors, destination, durationDays);

            foreach (SurvivorBehavior survivor in mission.Survivors)
            {
                if (survivor == null) continue;
                survivor.SetOnMission(true);
            }

            activeMissions.Add(mission);
            OnMissionStarted?.Invoke(mission);

            Debug.Log($"[ExplorationManager] Mission lancée → {destination} " +
                      $"({mission.Survivors.Count} survivant(s), {durationDays} jour(s)).");

            return mission;
        }

        /// <summary>
        /// Advances all active missions by one day.
        /// Called automatically via DayManager.OnDayStarted.
        /// </summary>
        public void TickDay()
        {
            List<ExplorationMission> completed = null;

            foreach (ExplorationMission mission in activeMissions)
            {
                mission.TickDay(foodPerSurvivorPerDay, waterPerSurvivorPerDay, materialsPerSurvivorPerDay);

                if (mission.IsComplete)
                {
                    completed ??= new List<ExplorationMission>();
                    completed.Add(mission);
                }
            }

            if (completed == null) return;

            foreach (ExplorationMission mission in completed)
                ReturnMission(mission);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        private void Start()
        {
            resourceManager = FindFirstObjectByType<ShelterResourceManager>();

            DayManager dayManager = FindFirstObjectByType<DayManager>();
            if (dayManager != null)
                dayManager.OnDayStarted += _ => TickDay();
            else
                Debug.LogWarning("[ExplorationManager] DayManager introuvable — tick automatique désactivé.");
        }

        // ── Private ────────────────────────────────────────────────────────────────

        private void ReturnMission(ExplorationMission mission)
        {
            activeMissions.Remove(mission);

            // Bring survivors back to the shelter
            foreach (SurvivorBehavior survivor in mission.Survivors)
            {
                if (survivor == null) continue;
                survivor.SetOnMission(false);
            }

            // Deposit gathered resources through the proper manager so HUD events fire
            if (resourceManager != null)
            {
                resourceManager.AddResources(
                    food:      Mathf.RoundToInt(mission.FoodGathered),
                    water:     Mathf.RoundToInt(mission.WaterGathered),
                    materials: Mathf.RoundToInt(mission.MaterialsGathered));
            }

            OnMissionCompleted?.Invoke(mission);

            Debug.Log($"[ExplorationManager] Mission terminée → {mission.Destination}. " +
                      $"Retour : nourriture +{mission.FoodGathered:F1}, " +
                      $"eau +{mission.WaterGathered:F1}, " +
                      $"matériaux +{mission.MaterialsGathered:F1}.");
        }
    }
}
