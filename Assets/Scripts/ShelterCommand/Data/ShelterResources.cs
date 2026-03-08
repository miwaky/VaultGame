using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Plain data container for all shelter resources, exposed via events.
    /// </summary>
    [Serializable]
    public class ShelterResources
    {
        [Range(0, 500)] public int food = 150;
        [Range(0, 500)] public int water = 150;
        [Range(0, 200)] public int medicine = 60;
        [Range(0, 500)] public int materials = 100;
        [Range(0, 100)] public int energy = 80;

        /// <summary>Applies daily consumption based on survivor count.</summary>
        public void ApplyDailyConsumption(int survivorCount)
        {
            food = Mathf.Max(0, food - survivorCount * 3);
            water = Mathf.Max(0, water - survivorCount * 2);
            energy = Mathf.Max(0, energy - 5);
        }
    }
}
