using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Centralized manager for all shelter resources.
    /// Fires events when resources change so the HUD can update.
    /// Food and Water are tracked as float to support fractional hourly production.
    /// </summary>
    public class ShelterResourceManager : MonoBehaviour
    {
        [Header("Initial Resources")]
        [SerializeField] private ShelterResources resources = new ShelterResources();

        public event Action OnResourcesChanged;

        // Public accessors — Food and Water exposed as float for production math
        public float Food     => resources.food;
        public float Water    => resources.water;
        public int   Medicine  => resources.medicine;
        public int   Materials => resources.materials;
        public int   Energy    => resources.energy;

        // Integer shims used by HUD display (floor)
        public int FoodInt  => Mathf.FloorToInt(resources.food);
        public int WaterInt => Mathf.FloorToInt(resources.water);

        /// <summary>Returns a reference to the live resource container (modified by survivors).</summary>
        public ShelterResources Resources => resources;

        /// <summary>Applies end-of-day consumption (1 Food + 1 Water per survivor).</summary>
        public void ApplyDailyConsumption(int survivorCount)
        {
            resources.ApplyDailyConsumption(survivorCount);
            NotifyChanged();
        }

        /// <summary>Adds fractional food from hourly farm production.</summary>
        public void AddFood(float amount)
        {
            resources.food = Mathf.Clamp(resources.food + amount, 0f, 500f);
            NotifyChanged();
        }

        /// <summary>Adds fractional water from hourly water production.</summary>
        public void AddWater(float amount)
        {
            resources.water = Mathf.Clamp(resources.water + amount, 0f, 500f);
            NotifyChanged();
        }

        /// <summary>Adds resources (e.g., from a completed mission).</summary>
        public void AddResources(int food = 0, int water = 0, int medicine = 0, int materials = 0, int energy = 0)
        {
            resources.food      = Mathf.Clamp(resources.food + food, 0f, 500f);
            resources.water     = Mathf.Clamp(resources.water + water, 0f, 500f);
            resources.medicine  = Mathf.Clamp(resources.medicine + medicine, 0, 200);
            resources.materials = Mathf.Clamp(resources.materials + materials, 0, 500);
            resources.energy    = Mathf.Clamp(resources.energy + energy, 0, 100);
            NotifyChanged();
        }

        /// <summary>Consumes a resource. Returns false if insufficient stock.</summary>
        public bool ConsumeResource(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Food:
                    if (resources.food < amount) return false;
                    resources.food = Mathf.Max(0f, resources.food - amount);
                    break;
                case ResourceType.Water:
                    if (resources.water < amount) return false;
                    resources.water = Mathf.Max(0f, resources.water - amount);
                    break;
                case ResourceType.Medicine:
                    if (resources.medicine < amount) return false;
                    resources.medicine -= amount;
                    break;
                case ResourceType.Materials:
                    if (resources.materials < amount) return false;
                    resources.materials -= amount;
                    break;
                case ResourceType.Energy:
                    if (resources.energy < amount) return false;
                    resources.energy -= amount;
                    break;
            }
            NotifyChanged();
            return true;
        }

        private void NotifyChanged() => OnResourcesChanged?.Invoke();
    }

    public enum ResourceType { Food, Water, Medicine, Materials, Energy }
}
