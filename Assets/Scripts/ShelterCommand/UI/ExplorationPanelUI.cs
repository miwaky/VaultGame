using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Panel d'exploration — sans gestion de missions.
    ///
    /// GAUCHE  — Liste cliquable des survivants disponibles (scroll).
    /// CENTRE  — Carte avec zones cliquables (MapZoneButton).
    /// DROITE  — Résumé (survivants, zone, durée) + bouton Envoyer.
    ///           En bas : survivants en exploration + bouton Rappeler.
    /// </summary>
    public class ExplorationPanelUI : MonoBehaviour
    {
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

        // ── Exploring survivors list ──────────────────────────────────────────────
        [Header("Exploring Survivors (right column, bottom)")]
        [SerializeField] private Transform          exploringListContainer;
        [SerializeField] private GameObject         exploringRowPrefab;

        // ── Dependencies ──────────────────────────────────────────────────────────
        private SurvivorManager survivorManager;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<(Button btn, SurvivorBehavior survivor)> survivorRows =
            new List<(Button, SurvivorBehavior)>();

        private readonly HashSet<SurvivorBehavior> selectedSurvivors =
            new HashSet<SurvivorBehavior>();

        private readonly List<MapZoneButton> zoneButtons = new List<MapZoneButton>();
        private MapZoneButton selectedZone;

        private static readonly Color ColorAvailable = new Color(0.12f, 0.22f, 0.12f, 1f);
        private static readonly Color ColorSelected  = new Color(0.1f,  0.50f, 0.15f, 1f);

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            launchButton?.onClick.AddListener(OnLaunch);
        }

        private void OnEnable()
        {
            survivorManager ??= FindFirstObjectByType<SurvivorManager>();
            if (survivorManager != null)
                survivorManager.OnPopulationChanged += Populate;

            RegisterZoneButtons();
            Populate();
        }

        private void OnDisable()
        {
            if (survivorManager != null)
                survivorManager.OnPopulationChanged -= Populate;
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

            foreach (SurvivorBehavior survivor in survivorManager.Survivors)
            {
                if (survivor == null || !survivor.IsAlive || survivor.IsOnMission) continue;

                GameObject      row = Instantiate(survivorRowPrefab, survivorListContainer);
                Button          btn = row.GetComponent<Button>();
                TextMeshProUGUI lbl = row.GetComponentInChildren<TextMeshProUGUI>();

                if (lbl != null) lbl.text = survivor.SurvivorName;

                if (btn != null)
                {
                    Image bg = btn.GetComponent<Image>();
                    if (bg != null) bg.color = ColorAvailable;

                    SurvivorBehavior captured = survivor;
                    btn.onClick.AddListener(() => ToggleSurvivor(captured));
                }

                survivorRows.Add((btn, survivor));
            }
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

            foreach (SurvivorBehavior survivor in selectedSurvivors)
                survivor.SetOnMission(true);

            ExplorationZone zone = selectedZone.Zone;
            ShowFeedback($"En route vers {zone.zoneName} — {zone.daysFromBase} jour(s).", false);

            Debug.Log($"[ExplorationPanelUI] Exploration → {zone.zoneName} " +
                      $"({selectedSurvivors.Count} survivant(s), {zone.daysFromBase}j).");

            selectedSurvivors.Clear();
            selectedZone.SetSelected(false);
            selectedZone = null;

            BuildSurvivorList();
            BuildExploringList();
            RefreshSummary();
        }

        // ── Exploring survivors list ──────────────────────────────────────────────

        private void BuildExploringList()
        {
            if (exploringListContainer == null || exploringRowPrefab == null) return;

            foreach (Transform child in exploringListContainer)
                Destroy(child.gameObject);

            if (survivorManager == null) return;

            foreach (SurvivorBehavior survivor in survivorManager.Survivors)
            {
                if (survivor == null || !survivor.IsAlive || !survivor.IsOnMission) continue;

                GameObject      row = Instantiate(exploringRowPrefab, exploringListContainer);
                TextMeshProUGUI lbl = row.GetComponentInChildren<TextMeshProUGUI>();
                Button          btn = row.GetComponentInChildren<Button>();

                if (lbl != null) lbl.text = survivor.SurvivorName;

                if (btn != null)
                {
                    SurvivorBehavior captured = survivor;
                    btn.onClick.AddListener(() => RecallSurvivor(captured));
                }
            }
        }

        private void RecallSurvivor(SurvivorBehavior survivor)
        {
            if (survivor == null) return;
            survivor.SetOnMission(false);
            ShowFeedback($"{survivor.SurvivorName} est rentré.", false);
            BuildSurvivorList();
            BuildExploringList();
            RefreshSummary();
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
