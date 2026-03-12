using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Runtime data for a single exploration mission.
    /// Created by ExplorationManager when a mission is launched.
    /// </summary>
    public class ExplorationMission
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        public string Destination  { get; }
        public int    DurationDays { get; }

        // ── Progress ─────────────────────────────────────────────────────────────
        public int  CurrentDay         { get; private set; } = 1;
        public int  RemainingDays      => Mathf.Max(0, DurationDays - CurrentDay + 1);
        public bool IsComplete         => CurrentDay > DurationDays;

        // ── Resources gathered during the mission ─────────────────────────────────
        public float FoodGathered      { get; private set; }
        public float WaterGathered     { get; private set; }
        public float MaterialsGathered { get; private set; }

        // ── Survivors on this mission ─────────────────────────────────────────────
        public IReadOnlyList<SurvivorBehavior> Survivors => survivors;
        private readonly List<SurvivorBehavior> survivors;

        public ExplorationMission(IEnumerable<SurvivorBehavior> assignedSurvivors, string destination, int durationDays)
        {
            survivors     = new List<SurvivorBehavior>(assignedSurvivors);
            Destination   = destination;
            DurationDays  = Mathf.Max(1, durationDays);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Advances the mission by one day and accumulates gathered resources.</summary>
        public void TickDay(float foodPerSurvivorPerDay, float waterPerSurvivorPerDay, float materialsPerSurvivorPerDay)
        {
            if (IsComplete) return;

            int count          = survivors.Count;
            FoodGathered      += foodPerSurvivorPerDay      * count;
            WaterGathered     += waterPerSurvivorPerDay     * count;
            MaterialsGathered += materialsPerSurvivorPerDay * count;

            CurrentDay++;
        }

        /// <summary>Returns a user-friendly progress string for the mission.</summary>
        public string GetProgressText()
        {
            return IsComplete
                ? "Mission terminée"
                : $"Jour {CurrentDay} / {DurationDays} — {RemainingDays} jour(s) restant(s)";
        }
    }
}
