using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Stores and manages the daily schedule for every survivor.
    /// Subscribes to DayManager.OnDayStarted to reset all assignments each new day.
    /// </summary>
    public class ScheduleManager : MonoBehaviour
    {
        private readonly Dictionary<SurvivorBehavior, DailyTask> schedule =
            new Dictionary<SurvivorBehavior, DailyTask>();

        public event Action OnScheduleReset;

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Assigns a daily task to a survivor. Overwrites any previous assignment.</summary>
        public void SetTask(SurvivorBehavior survivor, DailyTask task)
        {
            if (survivor == null) return;
            schedule[survivor] = task;
        }

        /// <summary>Returns the task currently assigned to a survivor. Defaults to SeReposer.</summary>
        public DailyTask GetTask(SurvivorBehavior survivor)
        {
            if (survivor != null && schedule.TryGetValue(survivor, out DailyTask task))
                return task;
            return DailyTask.SeReposer;
        }

        /// <summary>Clears all assignments and notifies listeners.</summary>
        public void ResetSchedule()
        {
            schedule.Clear();
            OnScheduleReset?.Invoke();
            Debug.Log("[ScheduleManager] Emploi du temps réinitialisé pour la nouvelle journée.");
        }

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Start()
        {
            DayManager dayManager = FindFirstObjectByType<DayManager>();
            if (dayManager != null)
                dayManager.OnDayStarted += _ => ResetSchedule();
            else
                Debug.LogWarning("[ScheduleManager] DayManager introuvable — reset automatique désactivé.");
        }
    }
}
