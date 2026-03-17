using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Manages the Residents panel of the computer terminal.
    /// Left column  : scrollable list of survivor buttons.
    /// Right column : rich profile view via ResidentDetailPanelUI
    ///                (stats bars, talents, traits, real-time needs).
    ///
    /// Wire up listContainer, residentEntryPrefab, detailsColumn and closeButton
    /// in the Inspector. Legacy TMP labels (detailNameText etc.) are kept as
    /// optional fallback references but are hidden when the rich panel is active.
    /// </summary>
    public class ResidentsPanelUI : MonoBehaviour
    {
        // ── Left column ──────────────────────────────────────────────────────────
        [Header("Survivor List (left)")]
        [SerializeField] private Transform  listContainer;
        [SerializeField] private GameObject residentEntryPrefab;

        // ── Right column — rich panel ────────────────────────────────────────────
        [Header("Details (right)")]
        [Tooltip("The DetailsColumn RectTransform. ResidentDetailPanelUI is built inside it.")]
        [SerializeField] private RectTransform detailsColumn;

        [Tooltip("Legacy TMP labels — kept for Inspector wiring but hidden at runtime.")]
        [SerializeField] private GameObject detailsRoot;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailIdentityText;
        [SerializeField] private TextMeshProUGUI detailStatsText;
        [SerializeField] private TextMeshProUGUI detailTraitsText;
        [SerializeField] private TextMeshProUGUI detailStatusText;

        // ── Navigation ───────────────────────────────────────────────────────────
        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        // ── Runtime ──────────────────────────────────────────────────────────────
        private SurvivorManager        survivorManager;
        private ComputerMenuController  menuController;
        private ResidentDetailPanelUI   detailPanel;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            closeButton?.onClick.AddListener(OnClose);
            BuildDetailPanel();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Rebuilds the survivor list and auto-selects the first alive one.</summary>
        public void Populate()
        {
            ResolveReferences();
            ClearList();

            if (survivorManager == null) return;

            detailPanel?.Hide();
            bool first = true;

            foreach (SurvivorBehavior survivor in survivorManager.Survivors)
            {
                if (survivor == null) continue;

                GameObject entry = Instantiate(residentEntryPrefab, listContainer);
                ResidentEntryUI entryUI = entry.GetComponent<ResidentEntryUI>();
                if (entryUI != null)
                {
                    SurvivorBehavior captured = survivor;
                    entryUI.Bind(survivor, () => ShowDetails(captured));
                }

                if (first && survivor.IsAlive)
                {
                    ShowDetails(survivor);
                    first = false;
                }
            }
        }

        // ── Private ──────────────────────────────────────────────────────────────

        /// <summary>Creates the ResidentDetailPanelUI component and hides legacy labels.</summary>
        private void BuildDetailPanel()
        {
            // Hide legacy raw-text labels — the rich panel takes over
            SafeSetActive(detailsRoot, false);
            SafeSetActive(detailNameText?.gameObject,     false);
            SafeSetActive(detailIdentityText?.gameObject, false);
            SafeSetActive(detailStatsText?.gameObject,    false);
            SafeSetActive(detailTraitsText?.gameObject,   false);
            SafeSetActive(detailStatusText?.gameObject,   false);

            if (detailsColumn == null) return;

            // Attach the rich panel component to the DetailsColumn itself
            detailPanel = detailsColumn.gameObject.GetComponent<ResidentDetailPanelUI>();
            if (detailPanel == null)
                detailPanel = detailsColumn.gameObject.AddComponent<ResidentDetailPanelUI>();

            // Inject the column reference via reflection-free setter
            detailPanel.InjectColumn(detailsColumn);
            detailPanel.Hide();
        }

        private void ShowDetails(SurvivorBehavior survivor)
        {
            if (survivor == null) return;
            detailPanel?.Show(survivor);
        }

        private void ClearList()
        {
            if (listContainer == null) return;
            foreach (Transform child in listContainer)
                Destroy(child.gameObject);
        }

        private void OnClose() => menuController?.ShowMainMenu();

        private void ResolveReferences()
        {
            if (survivorManager == null)
                survivorManager = FindFirstObjectByType<SurvivorManager>();
            if (menuController == null)
                menuController = FindFirstObjectByType<ComputerMenuController>();
        }

        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
