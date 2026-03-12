using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Plain data container for all shelter resources, exposed via events.
    /// Food and Water are stored as float to support fractional hourly production.
    /// </summary>
    [Serializable]
    public class ShelterResources
    {
        [Range(0f, 500f)] public float food = 20f;
        [Range(0f, 500f)] public float water = 20f;
        [Range(0, 200)] public int medicine = 60;
        [Range(0, 500)] public int materials = 100;
        [Range(0, 100)] public int energy = 80;

        /// <summary>Applies daily consumption based on survivor count (1 Food + 1 Water per survivor).</summary>
        public void ApplyDailyConsumption(int survivorCount)
        {
            food  = Mathf.Max(0f, food  - survivorCount);
            water = Mathf.Max(0f, water - survivorCount);
            energy = Mathf.Max(0, energy - 5);
        }
    }
}
