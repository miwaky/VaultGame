using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// One entry in an <see cref="EncounterEventPool"/>.
    /// </summary>
    [Serializable]
    public class WeightedEncounter
    {
        [Tooltip("Relative weight. Higher = more likely to be chosen from the pool.")]
        [Min(0f)]
        public float weight = 1f;

        [Tooltip("The radio call (and its dialogue) triggered when this entry is drawn.")]
        public RadioCallEvent radioCall;
    }

    /// <summary>
    /// A weighted list of random encounter events drawn once per mission day.
    ///
    /// How it works:
    ///   1. Each day, a <see cref="dailyEventChance"/> roll decides whether ANY event fires.
    ///   2. If it does, one entry is picked by weighted random (higher weight = more likely).
    ///
    /// Pools are cumulative:
    ///   — <see cref="RadioCallManager"/> holds a global pool (all zones).
    ///   — <see cref="ExplorationZone"/> / <see cref="MissionData"/> can add a zone-specific pool.
    ///   Both pools are merged before drawing, so zone events supplement global ones.
    ///
    /// Create via: Assets > Create > ShelterCommand > Mission > EncounterEventPool
    /// </summary>
    [CreateAssetMenu(menuName = "ShelterCommand/Mission/EncounterEventPool", fileName = "Pool_New")]
    public class EncounterEventPool : ScriptableObject
    {
        [Tooltip("Human-readable identifier for logs and the editor.")]
        public string poolID = "pool_generic";

        [Tooltip("Probability (0–100 %) that any event fires on a given day. " +
                 "Set to 100 for guaranteed daily events, 0 to disable the pool.")]
        [Range(0f, 100f)]
        public float dailyEventChance = 80f;

        [Tooltip("Weighted list of possible events. One is drawn per day (when the daily roll succeeds).")]
        public WeightedEncounter[] encounters = Array.Empty<WeightedEncounter>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Tries to draw one <see cref="RadioCallEvent"/> from this pool.
        /// Returns null if the daily chance roll fails or the pool is empty.
        /// </summary>
        public RadioCallEvent TryDraw()
        {
            if (encounters == null || encounters.Length == 0) return null;
            if (UnityEngine.Random.Range(0f, 100f) > dailyEventChance) return null;

            float total = 0f;
            foreach (WeightedEncounter e in encounters)
                if (e.radioCall != null) total += e.weight;

            if (total <= 0f) return null;

            float roll       = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;

            foreach (WeightedEncounter e in encounters)
            {
                if (e.radioCall == null) continue;
                cumulative += e.weight;
                if (roll <= cumulative) return e.radioCall;
            }

            return null;
        }
    }
}
