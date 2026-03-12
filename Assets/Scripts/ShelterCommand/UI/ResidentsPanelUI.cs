using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Manages the Residents panel of the computer terminal.
    /// Left column: scrollable list of survivor buttons.
    /// Right column: profile details of the selected survivor (name, age, profession, stats, traits).
    ///
    /// Wire up all serialized fields in the Inspector.
    /// </summary>
    public class ResidentsPanelUI : MonoBehaviour
    {
        // ── Left column — survivor list ─────────────────────────────────────────
        [Header("Survivor List (left)")]
        [SerializeField] private Transform    listContainer;
        [SerializeField] private GameObject   residentEntryPrefab;

        // ── Right column — survivor details ─────────────────────────────────────
        [Header("Details (right)")]
        [SerializeField] private GameObject      detailsRoot;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailIdentityText;
        [SerializeField] private TextMeshProUGUI detailStatsText;
        [SerializeField] private TextMeshProUGUI detailTraitsText;
        [SerializeField] private TextMeshProUGUI detailStatusText;

        // ── Navigation ──────────────────────────────────────────────────────────
        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        // ── Dependencies ────────────────────────────────────────────────────────
        private SurvivorManager       survivorManager;
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

        /// <summary>Rebuilds the survivor list and auto-selects the first alive one.</summary>
        public void Populate()
        {
            ResolveReferences();
            ClearList();

            if (survivorManager == null) return;

            SafeSetActive(detailsRoot, false);
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

        // ── Private ─────────────────────────────────────────────────────────────

        private void ShowDetails(SurvivorBehavior survivor)
        {
            if (survivor == null) return;

            SafeSetActive(detailsRoot, true);
            SurvivorGeneratedProfile p = survivor.GeneratedProfile;

            SetText(detailNameText, survivor.SurvivorName.ToUpper());

            if (p != null)
            {
                SetText(detailIdentityText, p.GetIdentityDisplayText());
                SetText(detailStatsText,    p.GetStatsDisplayText());
                SetText(detailTraitsText,
                    $"+ {TraitLabels.GetLabel(p.positiveTrait)}" +
                    $"\n— {TraitLabels.GetLabel(p.negativeTrait)}");
            }
            else
            {
                SetText(detailIdentityText, "—");
                SetText(detailStatsText,    "—");
                SetText(detailTraitsText,   "—");
            }

            string status = !survivor.IsAlive    ? "DÉCÉDÉ"    :
                             survivor.IsOnMission ? "EN MISSION" :
                             survivor.IsSick      ? "MALADE"     :
                             survivor.IsArrested  ? "ARRÊTÉ"     :
                                                    "Actif";
            SetText(detailStatusText, $"État : {status}");
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

        private static void SetText(TextMeshProUGUI t, string s)
        {
            if (t != null) t.text = s;
        }

        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
