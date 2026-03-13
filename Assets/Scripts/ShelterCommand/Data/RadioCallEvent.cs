using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// How the trigger time of a radio call is determined each day.
    /// </summary>
    public enum TriggerTimeMode
    {
        /// <summary>Call fires at an exact hour:minute.</summary>
        Fixed,
        /// <summary>Call fires at a random minute within a hour range.</summary>
        Random
    }

    /// <summary>
    /// Defines a scheduled radio call that occurs during an exploration mission.
    /// Attach one or more to an <see cref="ExplorationZone"/> to build the event timeline.
    ///
    /// Time modes:
    ///   Fixed  → fires exactly at <see cref="fixedHour"/>:<see cref="fixedMinute"/>
    ///   Random → fires at a random minute between
    ///            <see cref="randomHourMin"/>:00 and <see cref="randomHourMax"/>:59
    /// </summary>
    [CreateAssetMenu(menuName = "ShelterCommand/Dialogue/RadioCallEvent", fileName = "RadioCall_New")]
    public class RadioCallEvent : ScriptableObject
    {
        // ── Mission day ───────────────────────────────────────────────────────────
        [Tooltip("Day of the mission on which this call triggers (1 = first day out).")]
        [Min(1)]
        public int triggerDay = 1;

        // ── Time of day ───────────────────────────────────────────────────────────
        [Header("Time of Day")]
        [Tooltip("Fixed: exact time. Random: anywhere in [randomHourMin, randomHourMax].")]
        public TriggerTimeMode timeMode = TriggerTimeMode.Fixed;

        [Tooltip("Hour when the call fires (Fixed mode, 0-23).")]
        [Range(0, 23)]
        public int fixedHour = 10;

        [Tooltip("Minute when the call fires (Fixed mode, 0-59).")]
        [Range(0, 59)]
        public int fixedMinute = 0;

        [Tooltip("Earliest hour for random trigger (inclusive, 0-23).")]
        [Range(0, 23)]
        public int randomHourMin = 8;

        [Tooltip("Latest hour for random trigger (inclusive, 0-23). Must be ≥ randomHourMin.")]
        [Range(0, 23)]
        public int randomHourMax = 18;

        // ── Content ───────────────────────────────────────────────────────────────
        [Header("Content")]
        [Tooltip("Root dialogue node to play when this call is received.")]
        public ExplorationDialogue dialogue;

        [Tooltip("Sound played when the call comes in (optional).")]
        public AudioClip radioSound;

        [Tooltip("If true, this call can only fire once per mission instance.")]
        public bool fireOnce = true;

        // ── Runtime ───────────────────────────────────────────────────────────────
        // Reset by RadioCallManager when a new mission starts.
        [System.NonSerialized] public bool HasFired;

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves and returns the trigger time in total minutes from midnight.
        /// Call once per day to lock in the time (random is re-rolled each call).
        /// </summary>
        public int ResolveTriggerMinutes()
        {
            if (timeMode == TriggerTimeMode.Fixed)
                return fixedHour * 60 + fixedMinute;

            int clampedMax = Mathf.Max(randomHourMin, randomHourMax);
            int hour       = Random.Range(randomHourMin, clampedMax + 1);
            int minute     = Random.Range(0, 60);
            return hour * 60 + minute;
        }
    }
}
