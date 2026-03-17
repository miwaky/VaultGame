using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Panel d'exploration avec navigation par onglets.
    ///
    /// ONGLET 1 — Nouvelle expédition : liste survivants + carte + résumé/lancement.
    /// ONGLET 2 — Gérer expéditions   : liste des missions actives et en attente.
    /// </summary>
    public class ExplorationPanelUI : MonoBehaviour
    {
        // ── Navigation ────────────────────────────────────────────────────────────
        [Header("Navigation")]
        [SerializeField] private GameObject viewNouvelleExpedition;
        [SerializeField] private GameObject viewGererExpeditions;
        [SerializeField] private Button     btnNouvelleExpedition;
        [SerializeField] private Button     btnGererExpeditions;

        private static readonly Color ColorTabActive   = new Color(0.10f, 0.40f, 0.10f, 1f);
        private static readonly Color ColorTabInactive = new Color(0.06f, 0.20f, 0.06f, 1f);

        // ── Survivor list ──────────────────────────────────────────────────────────
        [Header("Survivor List (left column)")]
        [SerializeField] private Transform          survivorListContainer;
        [SerializeField] private GameObject         survivorRowPrefab;

        // ── Map ───────────────────────────────────────────────────────────────────
        [Header("Map (centre)")]
        [Tooltip("Parent transform that contains all MapZoneButton children.")]
        [SerializeField] private Transform          mapZonesContainer;

        // ── Summary & launch ──────────────────────────────────────────────────────
        [Header("Summary & Launch (right column)")]
        [SerializeField] private TextMeshProUGUI    selectedSurvivorsLabel;
        [SerializeField] private TextMeshProUGUI    selectedZoneLabel;
        [SerializeField] private TextMeshProUGUI    durationLabel;
        [SerializeField] private Button             launchButton;
        [SerializeField] private TextMeshProUGUI    launchFeedbackText;

        [Header("Equipment panel")]
        [SerializeField] private MissionEquipmentUI equipmentPanel;

        // ── Exploring missions list ───────────────────────────────────────────────
        [Header("Exploring Missions (vue Gérer)")]
        [Tooltip("Parent transform where MissionEntryUI rows are spawned.")]
        [SerializeField] private Transform  exploringListContainer;
        [Tooltip("Prefab with a MissionEntryUI component.")]
        [SerializeField] private GameObject exploringRowPrefab;
        [Tooltip("Shared tooltip panel (child of the root Canvas). Set once in the Inspector.")]
        [SerializeField] private MissionTooltipUI missionTooltip;

        // ── Dependencies ──────────────────────────────────────────────────────────
        private SurvivorManager  survivorManager;
        private DayCycleManager  dayCycleManager;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<(Button btn, SurvivorBehavior survivor)> survivorRows =
            new List<(Button, SurvivorBehavior)>();

        private readonly HashSet<SurvivorBehavior> selectedSurvivors =
            new HashSet<SurvivorBehavior>();

        private readonly List<MapZoneButton> zoneButtons = new List<MapZoneButton>();
        private MapZoneButton selectedZone;

        private static readonly Color ColorAvailable = new Color(0.12f, 0.22f, 0.12f, 1f);
        private static readonly Color ColorSelected  = new Color(0.1f,  0.50f, 0.15f, 1f);
        private static readonly Color ColorPending   = new Color(0.35f, 0.10f, 0.10f, 1f);

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            launchButton?.onClick.AddListener(OnLaunch);
            btnNouvelleExpedition?.onClick.AddListener(ShowNouvelleExpedition);
            btnGererExpeditions?.onClick.AddListener(ShowGererExpeditions);

            // Vue par défaut : Nouvelle expédition
            ShowNouvelleExpedition();
        }

        private void OnEnable()
        {
            survivorManager ??= FindFirstObjectByType<SurvivorManager>();
            if (survivorManager != null)
                survivorManager.OnPopulationChanged += Populate;

            // Refresh when missions activate (06:00 OnPreWorkStart)
            dayCycleManager ??= FindFirstObjectByType<DayCycleManager>();
            if (dayCycleManager != null)
            {
                dayCycleManager.OnPreWorkStart += BuildExploringList;
                dayCycleManager.OnWorkStart    += BuildExploringList;
            }

            RegisterZoneButtons();
            Populate();
        }

        private void OnDisable()
        {
            if (survivorManager != null)
                survivorManager.OnPopulationChanged -= Populate;

            if (dayCycleManager != null)
            {
                dayCycleManager.OnPreWorkStart -= BuildExploringList;
                dayCycleManager.OnWorkStart    -= BuildExploringList;
            }
        }

        // ── Navigation ────────────────────────────────────────────────────────────

        /// <summary>Affiche la vue "Nouvelle expédition" et met le bouton correspondant en actif.</summary>
        public void ShowNouvelleExpedition()
        {
            viewNouvelleExpedition?.SetActive(true);
            viewGererExpeditions?.SetActive(false);
            SetTabColor(btnNouvelleExpedition, active: true);
            SetTabColor(btnGererExpeditions,   active: false);

            BuildSurvivorList();
            RefreshSummary();
        }

        /// <summary>Affiche la vue "Gérer expéditions" et rafraîchit la liste des missions.</summary>
        public void ShowGererExpeditions()
        {
            viewNouvelleExpedition?.SetActive(false);
            viewGererExpeditions?.SetActive(true);
            SetTabColor(btnNouvelleExpedition, active: false);
            SetTabColor(btnGererExpeditions,   active: true);

            BuildExploringList();
        }

        private static void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? ColorTabActive : ColorTabInactive;

            // Texte légèrement plus lumineux sur l'onglet actif
            TextMeshProUGUI lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.color = active
                ? new Color(0.8f, 1f, 0.8f, 1f)
                : new Color(0.5f, 0.7f, 0.5f, 1f);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Rebuilds all lists and resets selection.</summary>
        public void Populate()
        {
            selectedSurvivors.Clear();
            selectedZone?.SetSelected(false);
            selectedZone = null;

            BuildSurvivorList();
            BuildExploringList();
            RefreshSummary();
        }

        // ── Survivor list ─────────────────────────────────────────────────────────

        private void BuildSurvivorList()
        {
            if (survivorListContainer == null || survivorRowPrefab == null) return;

            foreach (Transform child in survivorListContainer)
                Destroy(child.gameObject);

            survivorRows.Clear();
            if (survivorManager == null) return;

            // Collect pending survivors (assigned but not yet departed)
            HashSet<SurvivorBehavior> pendingSurvivors = GetPendingSurvivors();

            foreach (SurvivorBehavior survivor in survivorManager.Survivors)
            {
                if (survivor == null || !survivor.IsAlive) continue;
                // Hide survivors who have actually departed (IsOnMission = SetActive(false))
                if (survivor.IsOnMission) continue;

                bool isPending   = pendingSurvivors.Contains(survivor);
                bool isAvailable = !isPending && !selectedSurvivors.Contains(survivor);

                GameObject      row = Instantiate(survivorRowPrefab, survivorListContainer);
                Button          btn = row.GetComponent<Button>();
                TextMeshProUGUI lbl = row.GetComponentInChildren<TextMeshProUGUI>();

                if (lbl != null)
                    lbl.text = isPending ? $"{survivor.SurvivorName} (en attente)" : survivor.SurvivorName;

                if (btn != null)
                {
                    Image bg = btn.GetComponent<Image>();
                    if (bg != null)
                        bg.color = isPending
                            ? ColorPending
                            : selectedSurvivors.Contains(survivor) ? ColorSelected : ColorAvailable;

                    btn.interactable = !isPending;

                    if (!isPending)
                    {
                        SurvivorBehavior captured = survivor;
                        btn.onClick.AddListener(() => ToggleSurvivor(captured));
                    }
                }

                survivorRows.Add((btn, survivor));
            }
        }

        /// <summary>Returns survivors assigned to pending missions (not yet departed).</summary>
        private HashSet<SurvivorBehavior> GetPendingSurvivors()
        {
            var result = new HashSet<SurvivorBehavior>();
            RadioCallManager rcm = RadioCallManager.Instance;
            if (rcm == null) return result;

            foreach ((List<SurvivorBehavior> survivors, ExplorationZone _) in rcm.PendingZoneMissions)
                foreach (SurvivorBehavior s in survivors) if (s != null) result.Add(s);

            foreach ((List<SurvivorBehavior> survivors, MissionData _) in rcm.PendingDataMissions)
                foreach (SurvivorBehavior s in survivors) if (s != null) result.Add(s);

            return result;
        }

        private void ToggleSurvivor(SurvivorBehavior survivor)
        {
            if (survivor == null || survivor.IsOnMission) return;

            bool nowSelected = !selectedSurvivors.Contains(survivor);
            if (nowSelected) selectedSurvivors.Add(survivor);
            else             selectedSurvivors.Remove(survivor);

            foreach ((Button btn, SurvivorBehavior s) in survivorRows)
            {
                if (s != survivor) continue;
                Image bg = btn != null ? btn.GetComponent<Image>() : null;
                if (bg != null) bg.color = nowSelected ? ColorSelected : ColorAvailable;
                break;
            }

            RefreshSummary();
        }

        // ── Map zones ─────────────────────────────────────────────────────────────

        private void RegisterZoneButtons()
        {
            zoneButtons.Clear();
            if (mapZonesContainer == null) return;

            foreach (MapZoneButton zone in mapZonesContainer.GetComponentsInChildren<MapZoneButton>(true))
            {
                zoneButtons.Add(zone);
                zone.OnZoneSelected -= HandleZoneSelected;
                zone.OnZoneSelected += HandleZoneSelected;
                zone.SetSelected(false);
            }
        }

        private void HandleZoneSelected(MapZoneButton clicked)
        {
            if (selectedZone != null && selectedZone != clicked)
                selectedZone.SetSelected(false);

            if (selectedZone == clicked)
            {
                clicked.SetSelected(false);
                selectedZone = null;
            }
            else
            {
                selectedZone = clicked;
                selectedZone.SetSelected(true);
            }

            RefreshSummary();
        }

        // ── Summary panel ─────────────────────────────────────────────────────────

        private void RefreshSummary()
        {
            if (selectedSurvivorsLabel != null)
            {
                if (selectedSurvivors.Count == 0)
                {
                    selectedSurvivorsLabel.text  = "Aucun survivant sélectionné";
                    selectedSurvivorsLabel.color = new Color(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (SurvivorBehavior s in selectedSurvivors)
                    { if (sb.Length > 0) sb.Append(", "); sb.Append(s.SurvivorName); }
                    selectedSurvivorsLabel.text  = sb.ToString();
                    selectedSurvivorsLabel.color = Color.white;
                }
            }

            bool zoneReady = selectedZone != null && selectedZone.Zone != null;

            if (selectedZoneLabel != null)
            {
                selectedZoneLabel.text  = zoneReady ? selectedZone.Zone.zoneName : "Aucune zone sélectionnée";
                selectedZoneLabel.color = zoneReady ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            }

            if (durationLabel != null)
                durationLabel.text = zoneReady ? $"{selectedZone.Zone.daysFromBase} jour(s)" : "— jour(s)";

            if (launchButton != null)
                launchButton.interactable = selectedSurvivors.Count > 0 && zoneReady;
        }

        // ── Launch ────────────────────────────────────────────────────────────────

        private void OnLaunch()
        {
            if (selectedSurvivors.Count == 0 || selectedZone == null || selectedZone.Zone == null)
            {
                ShowFeedback("Sélectionnez un survivant et une zone.", true);
                return;
            }

            ExplorationZone zone = selectedZone.Zone;

            // Ouvre le panel d'équipement — le lancement réel se fait dans OnEquipmentConfirmed
            if (equipmentPanel != null)
            {
                equipmentPanel.Open(zone.zoneName, zone.daysFromBase,
                    selectedSurvivors.Count,
                    (food, water) => OnEquipmentConfirmed(zone, food, water));
            }
            else
            {
                // Fallback sans panel d'équipement
                LaunchMission(zone, 0, 0);
            }
        }

        private void OnEquipmentConfirmed(ExplorationZone zone, int food, int water)
        {
            LaunchMission(zone, food, water);
        }

        private void LaunchMission(ExplorationZone zone, int food, int water)
        {
            if (RadioCallManager.Instance != null)
                RadioCallManager.Instance.ScheduleExploration(
                    new System.Collections.Generic.List<SurvivorBehavior>(selectedSurvivors), zone);
            else
            {
                foreach (SurvivorBehavior survivor in selectedSurvivors)
                    survivor.SetOnMission(true);
                Debug.LogWarning("[ExplorationPanelUI] RadioCallManager absent — départ immédiat.");
            }

            string provisions = food > 0 || water > 0
                ? $"  ({food} nourritures, {water} eaux)"
                : "";
            ShowFeedback($"Départ demain 07:00 → {zone.zoneName} ({zone.daysFromBase}j){provisions}.", false);

            selectedSurvivors.Clear();
            selectedZone.SetSelected(false);
            selectedZone = null;

            BuildSurvivorList();
            BuildExploringList();
            RefreshSummary();
        }

        // ── Exploring missions list ───────────────────────────────────────────────

        private void BuildExploringList()
        {
            if (exploringListContainer == null || exploringRowPrefab == null) return;

            foreach (Transform child in exploringListContainer)
                Destroy(child.gameObject);

            RadioCallManager rcm = RadioCallManager.Instance;
            if (rcm == null) return;

            // ── Active missions (already departed) ────────────────────────────────
            foreach (ActiveMission mission in rcm.ActiveMissions)
            {
                if (mission == null) continue;
                GameObject     row   = Instantiate(exploringRowPrefab, exploringListContainer);
                MissionEntryUI entry = row.GetComponent<MissionEntryUI>();
                entry?.BindActiveMission(mission);
            }

            // ── Pending zone missions (depart tomorrow) ───────────────────────────
            foreach ((List<SurvivorBehavior> survivors, ExplorationZone zone) in rcm.PendingZoneMissions)
            {
                if (zone == null) continue;
                GameObject     row   = Instantiate(exploringRowPrefab, exploringListContainer);
                MissionEntryUI entry = row.GetComponent<MissionEntryUI>();
                entry?.BindPendingZone(survivors, zone);
            }

            // ── Pending data-driven missions (depart tomorrow) ────────────────────
            foreach ((List<SurvivorBehavior> survivors, MissionData data) in rcm.PendingDataMissions)
            {
                if (data == null) continue;
                GameObject     row   = Instantiate(exploringRowPrefab, exploringListContainer);
                MissionEntryUI entry = row.GetComponent<MissionEntryUI>();
                entry?.BindPendingData(survivors, data);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ShowFeedback(string message, bool error)
        {
            if (launchFeedbackText == null) return;
            launchFeedbackText.text  = message;
            launchFeedbackText.color = error ? new Color(1f, 0.35f, 0.35f) : new Color(0.35f, 1f, 0.35f);
        }
    }
}
