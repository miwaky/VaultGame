using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// A single row in the Schedule panel.
    ///
    /// Visual layout (full-width horizontal bar):
    ///   [NomSurvivant]  [◄]  [● TaskLabel / Statut]  [►]
    ///
    /// The name occupies the left third; the ◄/label/► selector fills the rest.
    /// Wire up nameText, taskLabel, prevButton, nextButton via the prefab.
    /// Task changes are committed immediately on every click — no separate validation step.
    /// When the survivor is on a mission, the task selector is disabled and the label shows
    /// "En exploration" in a muted colour.
    /// </summary>
    public class ScheduleRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI taskLabel;
        [SerializeField] private Button          prevButton;
        [SerializeField] private Button          nextButton;

        private static readonly Color ColorExploring = new Color(0.55f, 0.78f, 1f);
        private static readonly Color ColorNormal    = Color.white;

        private SurvivorBehavior boundSurvivor;
        private ScheduleManager  scheduleManager;
        private int              currentIndex;
        private ScheduleExecutor scheduleExecutor;

        private static readonly DailyTask[] Tasks = DailyTaskLabels.All;

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Binds this row to a survivor and initialises the task selector.</summary>
        public void Bind(SurvivorBehavior survivor, ScheduleManager manager)
        {
            boundSurvivor    = survivor;
            scheduleManager  = manager;
            scheduleExecutor = FindFirstObjectByType<ScheduleExecutor>();

            if (nameText != null)
                nameText.text = survivor.SurvivorName.ToUpper();

            // Restore current assignment
            if (manager != null)
            {
                DailyTask current = manager.GetTask(survivor);
                currentIndex = System.Array.IndexOf(Tasks, current);
                if (currentIndex < 0) currentIndex = 0;
            }

            RefreshLabel();
            ApplyMissionState();

            prevButton?.onClick.AddListener(CyclePrev);
            nextButton?.onClick.AddListener(CycleNext);
        }

        // ── Private ─────────────────────────────────────────────────────────────

        private void CycleNext()
        {
            if (boundSurvivor != null && boundSurvivor.IsOnMission) return;
            currentIndex = (currentIndex + 1) % Tasks.Length;
            CommitTask();
        }

        private void CyclePrev()
        {
            if (boundSurvivor != null && boundSurvivor.IsOnMission) return;
            currentIndex = (currentIndex - 1 + Tasks.Length) % Tasks.Length;
            CommitTask();
        }

        private void CommitTask()
        {
            RefreshLabel();
            if (scheduleManager != null && boundSurvivor != null)
            {
                scheduleManager.SetTask(boundSurvivor, Tasks[currentIndex]);
                scheduleExecutor?.Execute();
            }
        }

        private void RefreshLabel()
        {
            if (taskLabel == null) return;
            taskLabel.text = $"● {DailyTaskLabels.GetLabel(Tasks[currentIndex])}";
        }

        /// <summary>
        /// Locks or unlocks the task selector based on the survivor's IsOnMission state.
        /// When exploring, the label is replaced with "En exploration" in a muted blue.
        /// </summary>
        private void ApplyMissionState()
        {
            bool isExploring = boundSurvivor != null && boundSurvivor.IsOnMission;

            if (prevButton != null) prevButton.interactable = !isExploring;
            if (nextButton != null) nextButton.interactable = !isExploring;

            if (taskLabel != null)
            {
                taskLabel.text  = isExploring ? "● En exploration" : $"● {DailyTaskLabels.GetLabel(Tasks[currentIndex])}";
                taskLabel.color = isExploring ? ColorExploring : ColorNormal;
            }
        }

        private void OnDestroy()
        {
            prevButton?.onClick.RemoveListener(CyclePrev);
            nextButton?.onClick.RemoveListener(CycleNext);
        }
    }
}
