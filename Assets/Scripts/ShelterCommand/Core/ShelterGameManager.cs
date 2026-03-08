using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Root coordinator for Shelter Command. Wires all sub-systems together.
    /// Designed as a singleton for easy cross-system access.
    /// </summary>
    public class ShelterGameManager : MonoBehaviour
    {
        public static ShelterGameManager Instance { get; private set; }

        [Header("Core Systems")]
        [SerializeField] private DayManager dayManager;
        [SerializeField] private SurvivorManager survivorManager;
        [SerializeField] private ShelterResourceManager resourceManager;
        [SerializeField] private ShelterEventSystem eventSystem;
        [SerializeField] private MissionSystem missionSystem;
        [SerializeField] private CameraRoomController cameraRoomController;

        // Public accessors for UI systems
        public DayManager DayManager => dayManager;
        public SurvivorManager SurvivorManager => survivorManager;
        public ShelterResourceManager ResourceManager => resourceManager;
        public ShelterEventSystem EventSystem => eventSystem;
        public MissionSystem MissionSystem => missionSystem;
        public CameraRoomController CameraRoomController => cameraRoomController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void SubscribeToEvents()
        {
            if (dayManager != null)
            {
                dayManager.OnGameOver += HandleGameOver;
                dayManager.OnGameWon += HandleGameWon;
            }

            if (missionSystem != null)
            {
                missionSystem.OnMissionCompleted += HandleMissionCompleted;
            }

            if (survivorManager != null)
            {
                survivorManager.OnSurvivorDied += HandleSurvivorDied;
            }
        }

        private void HandleGameOver()
        {
            Debug.Log("[ShelterGameManager] Game Over.");
        }

        private void HandleGameWon()
        {
            Debug.Log("[ShelterGameManager] Victory!");
        }

        private void HandleMissionCompleted(MissionResult result)
        {
            if (result.Success)
            {
                resourceManager.AddResources(
                    food: result.FoodGained,
                    water: result.WaterGained,
                    medicine: result.MedicineGained,
                    materials: result.MaterialsGained
                );
            }
        }

        private void HandleSurvivorDied(SurvivorBehavior survivor)
        {
            Debug.Log($"[ShelterGameManager] {survivor.SurvivorName} died.");
        }

        /// <summary>Convenience method called by the HUD "Next Day" button.</summary>
        public void RequestNextDay()
        {
            dayManager.AdvanceDay();
        }
    }
}
