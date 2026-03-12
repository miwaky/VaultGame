using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Populates the Schedule panel with one row per alive survivor.
    /// Each row shows the survivor's name and a ◄/► task cycle selector.
    /// Task changes are applied immediately on each click — no validation step needed.
    ///
    /// Requires a ScheduleManager, SurvivorManager, and ScheduleExecutor in the scene.
    /// Wire up the container, row prefab, and close button in the Inspector.
    /// </summary>
    public class SchedulePanelUI : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Vertical layout container inside the ScrollView.")]
        [SerializeField] private Transform rowContainer;

        [Tooltip("Prefab for a single survivor row. Must contain a ScheduleRowUI component.")]
        [SerializeField] private GameObject scheduleRowPrefab;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        // ── Dependencies ────────────────────────────────────────────────────────
        private SurvivorManager    survivorManager;
        private ScheduleManager    scheduleManager;
        private ScheduleExecutor   scheduleExecutor;
        private ComputerMenuController menuController;

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            closeButton?.onClick.AddListener(OnClose);
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Rebuilds the survivor list from scratch.</summary>
        public void Populate()
        {
            ResolveReferences();

            if (rowContainer == null || scheduleRowPrefab == null)
            {
                Debug.LogWarning("[SchedulePanelUI] rowContainer ou scheduleRowPrefab non assigné.");
                return;
            }

            // Clear old rows
            foreach (Transform child in rowContainer)
                Destroy(child.gameObject);

            if (survivorManager == null) return;

            foreach (SurvivorBehavior survivor in survivorManager.Survivors)
            {
                // Show all alive survivors, including those currently on a mission.
                // Survivors on a mission will appear with their task selector locked.
                if (survivor == null || !survivor.IsAlive) continue;

                GameObject row = Instantiate(scheduleRowPrefab, rowContainer);
                ScheduleRowUI rowUI = row.GetComponent<ScheduleRowUI>();
                if (rowUI != null)
                    rowUI.Bind(survivor, scheduleManager);
                else
                    Debug.LogWarning("[SchedulePanelUI] scheduleRowPrefab n'a pas de composant ScheduleRowUI.");
            }

            // Trigger executor so rooms are immediately updated
            scheduleExecutor?.Execute();
        }

        // ── Private ─────────────────────────────────────────────────────────────

        private void OnClose()
        {
            menuController?.ShowMainMenu();
        }

        private void ResolveReferences()
        {
            if (survivorManager == null)
                survivorManager = FindFirstObjectByType<SurvivorManager>();

            if (scheduleManager == null)
                scheduleManager = FindFirstObjectByType<ScheduleManager>();

            if (scheduleExecutor == null)
                scheduleExecutor = FindFirstObjectByType<ScheduleExecutor>();

            if (menuController == null)
                menuController = FindFirstObjectByType<ComputerMenuController>();
        }
    }
}
