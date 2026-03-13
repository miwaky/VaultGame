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
            // Reset the schedule at PreWork (06:00) so survivors still benefit from
            // the previous day's assignment during the overnight work tick at 07:00.
            // Resetting on DayManager.OnDayStarted was too early and wiped the
            // schedule before HourlyProductionManager could count workers.
            DayCycleManager cycle = FindFirstObjectByType<DayCycleManager>();
            if (cycle != null)
                cycle.OnPreWorkStart += ResetSchedule;
            else
                Debug.LogWarning("[ScheduleManager] DayCycleManager introuvable — reset automatique désactivé.");
        }

        private void OnDestroy()
        {
            DayCycleManager cycle = FindFirstObjectByType<DayCycleManager>();
            if (cycle != null)
                cycle.OnPreWorkStart -= ResetSchedule;
        }
    }
}
