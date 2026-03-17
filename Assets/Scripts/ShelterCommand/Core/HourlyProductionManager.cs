using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Calculates and applies Food and Water production every in-game hour.
    /// Production is driven by the number of survivors currently assigned to
    /// <see cref="DailyTask.SoccuperDeLaFerme"/> and <see cref="DailyTask.SoccuperDeLeau"/>
    /// according to <see cref="ScheduleManager"/>.
    ///
    /// Rates:
    ///   FoodProductionPerHour  = 0.4 per worker
    ///   WaterProductionPerHour = 0.35 per worker
    ///
    /// Daily consumption (at midnight):
    ///   Food  -= Population  (1 Food / survivor / day)
    ///   Water -= Population  (1 Water / survivor / day)
    /// </summary>
    public class HourlyProductionManager : MonoBehaviour
    {
        // ── Constants ──────────────────────────────────────────────────────────────
        private const float FoodProductionPerHour  = 0.4f;
        private const float WaterProductionPerHour = 0.35f;

        // ── Inspector ──────────────────────────────────────────────────────────────
        [Header("Dependencies")]
        [Tooltip("Clock that drives the hourly ticks.")]
        [SerializeField] private DayCycleManager dayCycleManager;

        [Tooltip("Task registry — queried every hour to count active workers.")]
        [SerializeField] private ScheduleManager scheduleManager;

        [Tooltip("Survivor roster — queried for alive survivor count.")]
        [SerializeField] private SurvivorManager survivorManager;

        [Tooltip("Resource bank that receives production and consumption.")]
        [SerializeField] private ShelterResourceManager resourceManager;

        // ── Events ─────────────────────────────────────────────────────────────────
        /// <summary>Fired after each hourly production tick. Args: foodAdded, waterAdded.</summary>
        public event Action<float, float> OnHourlyProduction;

        /// <summary>Fired after midnight consumption is applied. Args: foodConsumed, waterConsumed.</summary>
        public event Action<int, int> OnDailyConsumption;

        // ── State ──────────────────────────────────────────────────────────────────
        private int lastProcessedHour = -1;
        private bool midnightConsumed = false;

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            if (dayCycleManager != null)
                dayCycleManager.OnTimeChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            if (dayCycleManager != null)
                dayCycleManager.OnTimeChanged -= HandleTimeChanged;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Returns the number of workers currently assigned to the farm.</summary>
        public int WorkersFarm  => CountWorkersForTask(DailyTask.SoccuperDeLaFerme);

        /// <summary>Returns the number of workers currently assigned to water management.</summary>
        public int WorkersWater => CountWorkersForTask(DailyTask.SoccuperDeLeau);

        /// <summary>
        /// Triggers the daily consumption immediately (used by BedProp when the player sleeps).
        /// </summary>
        public void TriggerSleepConsumption() => ApplyMidnightConsumption();

        // ── Private ────────────────────────────────────────────────────────────────

        private void HandleTimeChanged(int hour, int minute)
        {
            // Midnight consumption — triggered once at 00:00
            if (hour == 0 && minute == 0)
            {
                if (!midnightConsumed)
                {
                    midnightConsumed = true;
                    ApplyMidnightConsumption();
                }
            }
            else
            {
                midnightConsumed = false;
            }

            // Hourly production — triggered once per new hour
            if (minute == 0 && hour != lastProcessedHour)
            {
                lastProcessedHour = hour;
                ApplyHourlyProduction(hour);
            }
        }

        private void ApplyHourlyProduction(int hour)
        {
            int farmWorkers  = WorkersFarm;
            int waterWorkers = WorkersWater;

            float foodGain  = farmWorkers  * FoodProductionPerHour;
            float waterGain = waterWorkers * WaterProductionPerHour;

            if (foodGain > 0f)
                resourceManager.AddFood(foodGain);

            if (waterGain > 0f)
                resourceManager.AddWater(waterGain);

            OnHourlyProduction?.Invoke(foodGain, waterGain);

            Debug.Log($"[HourlyProductionManager] {hour:D2}:00 — " +
                      $"Ferme: {farmWorkers} travailleurs → +{foodGain:F2} nourriture | " +
                      $"Eau: {waterWorkers} travailleurs → +{waterGain:F2} eau | " +
                      $"Stock: {resourceManager.Food:F1} nourr. / {resourceManager.Water:F1} eau");
        }

        private void ApplyMidnightConsumption()
        {
            if (survivorManager == null || resourceManager == null) return;

            // Survivants présents dans l'abri (vivants et pas en mission)
            // + 1 pour le garde Steve + 1 pour le joueur
            const int GuardAndPlayerCount = 2;
            int survivorsInShelter = 0;
            foreach (SurvivorBehavior s in survivorManager.GetAliveSurvivors())
            {
                if (!s.IsOnMission)
                    survivorsInShelter++;
            }
            int totalConsumers = survivorsInShelter + GuardAndPlayerCount;

            int foodConsumed  = 0;
            int waterConsumed = 0;

            for (int i = 0; i < totalConsumers; i++)
            {
                if (StorageRegistry.ConsumeItem(ResourceType.Food))
                    foodConsumed++;
            }
            for (int i = 0; i < totalConsumers; i++)
            {
                if (StorageRegistry.ConsumeItem(ResourceType.Water))
                    waterConsumed++;
            }

            // Synchronise le compteur abstrait avec ce qui a réellement été retiré
            if (foodConsumed  > 0) resourceManager.AddFood(-foodConsumed);
            if (waterConsumed > 0) resourceManager.AddWater(-waterConsumed);

            OnDailyConsumption?.Invoke(foodConsumed, waterConsumed);

            Debug.Log($"[HourlyProductionManager] Consommation journalière : " +
                      $"-{foodConsumed} nourr. / -{waterConsumed} eau " +
                      $"({survivorsInShelter} survivants abri + {GuardAndPlayerCount} garde/joueur = {totalConsumers} total) | " +
                      $"Stock restant: {resourceManager.Food:F1} nourr. / {resourceManager.Water:F1} eau");
        }

        private int CountWorkersForTask(DailyTask task)
        {
            if (scheduleManager == null || survivorManager == null) return 0;

            int count = 0;
            foreach (SurvivorBehavior survivor in survivorManager.GetAliveSurvivors())
            {
                if (scheduleManager.GetTask(survivor) == task)
                    count++;
            }
            return count;
        }

        private void ResolveDependencies()
        {
            if (dayCycleManager == null)
                dayCycleManager = FindFirstObjectByType<DayCycleManager>();

            if (scheduleManager == null)
                scheduleManager = FindFirstObjectByType<ScheduleManager>();

            if (survivorManager == null)
                survivorManager = FindFirstObjectByType<SurvivorManager>();

            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ShelterResourceManager>();

            if (dayCycleManager  == null) Debug.LogError("[HourlyProductionManager] DayCycleManager introuvable.");
            if (scheduleManager  == null) Debug.LogError("[HourlyProductionManager] ScheduleManager introuvable.");
            if (survivorManager  == null) Debug.LogError("[HourlyProductionManager] SurvivorManager introuvable.");
            if (resourceManager  == null) Debug.LogError("[HourlyProductionManager] ShelterResourceManager introuvable.");
        }
    }
}
