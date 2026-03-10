using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Controls the in-game day cycle. Orchestrates daily ticks across all systems.
    /// </summary>
    public class DayManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SurvivorManager survivorManager;
        [SerializeField] private ShelterResourceManager resourceManager;
        [SerializeField] private ShelterEventSystem eventSystem;

        [Header("Day Settings")]
        [SerializeField] private int startingDay = 1;
        [SerializeField] private int gameDurationDays = 30;

        public event Action<int> OnDayStarted;
        public event Action<int> OnDayEnded;
        public event Action OnGameOver;
        public event Action OnGameWon;

        public int CurrentDay { get; private set; }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            CurrentDay = startingDay;
        }

        // Day 1 start is fired in Start so all other systems are initialized first.
        private void Start()
        {
            // Small delay ensures HUD has subscribed via ShelterGameManager
            Invoke(nameof(FireDayOneStart), 0.1f);
        }

        private void FireDayOneStart() => OnDayStarted?.Invoke(CurrentDay);

        /// <summary>Advances to the next day. Call this when the player clicks "Passer au jour suivant".</summary>
        public void AdvanceDay()
        {
            if (IsGameOver) return;

            OnDayEnded?.Invoke(CurrentDay);

            // Apply daily resource consumption
            int aliveCount = survivorManager.AliveSurvivorCount;
            resourceManager.ApplyDailyConsumption(aliveCount);

            // Tick survivors
            survivorManager.TickDay(resourceManager.Resources);

            CurrentDay++;

            // Try triggering a random event
            eventSystem.TryTriggerRandomEvent();

            // Check win/lose conditions
            CheckEndConditions();

            if (!IsGameOver)
            {
                OnDayStarted?.Invoke(CurrentDay);
                Debug.Log($"[DayManager] Day {CurrentDay} started.");
            }
        }

        private void CheckEndConditions()
        {
            if (survivorManager.AliveSurvivorCount == 0)
            {
                IsGameOver = true;
                OnGameOver?.Invoke();
                Debug.Log("[DayManager] GAME OVER — all survivors are dead.");
                return;
            }

            if (resourceManager.Energy <= 0 && resourceManager.Food <= 0)
            {
                IsGameOver = true;
                OnGameOver?.Invoke();
                Debug.Log("[DayManager] GAME OVER — shelter systems collapsed.");
                return;
            }

            if (CurrentDay > gameDurationDays)
            {
                OnGameWon?.Invoke();
                Debug.Log($"[DayManager] YOU WIN — shelter survived {gameDurationDays} days.");
            }
        }
    }
}
