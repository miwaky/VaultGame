using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Simulates an in-game clock that advances automatically in real-time.
    /// Drives the three daily phases:
    ///   PreWork  — 06:00 → 07:00  (player configures tasks)
    ///   Work     — 07:00 → 19:00  (NPCs execute tasks, production runs)
    ///   PostWork — 19:00 → 00:00  (NPCs idle, social events)
    ///
    /// At midnight (00:00) the day resets and loops back to PreWork at 06:00.
    /// Call DayManager.AdvanceDay() externally at midnight if you want a day tick.
    ///
    /// Set minutesPerSecond to control time speed (e.g. 1 = 1 game-minute per real-second).
    /// </summary>
    public class DayCycleManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Time Speed")]
        [Tooltip("How many in-game minutes pass per real second. Default: 1.")]
        [SerializeField] private float minutesPerSecond = 1f;

        [Header("Starting Time")]
        [Tooltip("Hour at which the simulation starts (0-23).")]
        [SerializeField] [Range(0, 23)] private int startHour = 6;

        [Header("Day Integration (optional)")]
        [Tooltip("If assigned, AdvanceDay() is called automatically at midnight.")]
        [SerializeField] private DayManager dayManager;

        // ── Phase boundaries (in total minutes from midnight) ─────────────────────

        private const int PreWorkStart  =  6 * 60;   // 06:00 = 360
        private const int WorkStart     =  7 * 60;   // 07:00 = 420
        private const int PostWorkStart = 19 * 60;   // 19:00 = 1140
        private const int MidnightReset = 24 * 60;   // 00:00 = 1440

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fires when the clock reaches 06:00 (PreWork phase starts).</summary>
        public event Action OnPreWorkStart;

        /// <summary>Fires when the clock reaches 07:00 (Work phase starts).</summary>
        public event Action OnWorkStart;

        /// <summary>Fires when the clock reaches 19:00 (PostWork phase starts).</summary>
        public event Action OnPostWorkStart;

        /// <summary>Fires every real-time frame with the updated hour and minute.</summary>
        public event Action<int, int> OnTimeChanged;

        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Current in-game hour (0-23).</summary>
        public int CurrentHour { get; private set; }

        /// <summary>Current in-game minute (0-59).</summary>
        public int CurrentMinute { get; private set; }

        /// <summary>Current day phase.</summary>
        public DayPhase CurrentPhase { get; private set; }

        /// <summary>Elapsed in-game minutes since midnight.</summary>
        private float totalMinutes;

        private DayPhase lastPhase;
        private bool hasFiredMidnightAdvance;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Start at the configured hour
            totalMinutes = startHour * 60f;
            ApplyTime(totalMinutes);

            if (dayManager == null)
                dayManager = FindFirstObjectByType<DayManager>();
        }

        private void Update()
        {
            AdvanceTime(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Overrides the current time. Useful for testing or event scripting.</summary>
        public void SetTime(int hour, int minute)
        {
            totalMinutes = hour * 60f + minute;
            ApplyTime(totalMinutes);
        }

        /// <summary>Sets the time-speed multiplier at runtime.</summary>
        public void SetMinutesPerSecond(float value)
        {
            minutesPerSecond = Mathf.Max(0f, value);
        }

        /// <summary>Returns a formatted string like "08:30".</summary>
        public string GetFormattedTime() => $"{CurrentHour:D2}:{CurrentMinute:D2}";

        // ── Private ───────────────────────────────────────────────────────────────

        private void AdvanceTime(float deltaTime)
        {
            totalMinutes += minutesPerSecond * deltaTime;

            // Midnight rollover
            if (totalMinutes >= MidnightReset)
            {
                if (!hasFiredMidnightAdvance)
                {
                    hasFiredMidnightAdvance = true;
                    dayManager?.AdvanceDay();
                }

                // Roll to PreWork start so the next day begins at 06:00
                if (totalMinutes >= MidnightReset + (PreWorkStart))
                {
                    totalMinutes -= MidnightReset;
                    hasFiredMidnightAdvance = false;
                }
            }

            ApplyTime(totalMinutes);
        }

        private void ApplyTime(float minutes)
        {
            int totalInt    = Mathf.FloorToInt(minutes);
            int newHour     = (totalInt / 60) % 24;
            int newMinute   = totalInt % 60;

            bool timeChanged = newHour != CurrentHour || newMinute != CurrentMinute;

            CurrentHour   = newHour;
            CurrentMinute = newMinute;

            // Determine current phase
            DayPhase newPhase = ComputePhase(totalInt % MidnightReset);

            if (timeChanged)
                OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);

            if (newPhase != lastPhase)
            {
                lastPhase    = newPhase;
                CurrentPhase = newPhase;
                FirePhaseEvent(newPhase);
            }
        }

        private static DayPhase ComputePhase(int minuteOfDay) =>
            minuteOfDay < PreWorkStart  ? DayPhase.PostWork  :   // 00:00–06:00 treated as late PostWork
            minuteOfDay < WorkStart     ? DayPhase.PreWork   :   // 06:00–07:00
            minuteOfDay < PostWorkStart ? DayPhase.Work      :   // 07:00–19:00
                                          DayPhase.PostWork;     // 19:00–00:00

        private void FirePhaseEvent(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.PreWork:   OnPreWorkStart?.Invoke();  break;
                case DayPhase.Work:      OnWorkStart?.Invoke();     break;
                case DayPhase.PostWork:  OnPostWorkStart?.Invoke(); break;
            }

            Debug.Log($"[DayCycleManager] Phase → {phase} à {GetFormattedTime()}");
        }
    }
}
