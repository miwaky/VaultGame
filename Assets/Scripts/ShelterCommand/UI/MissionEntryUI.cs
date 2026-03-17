using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Card dans la vue "Gérer expéditions".
    ///
    /// Affiche pour chaque mission :
    ///   • Destination et position actuelle (En préparation → étapes → destination)
    ///   • Nombre de personnages et leurs noms
    ///   • Bouton dont le libellé et l'action changent selon l'état :
    ///       - Pending  → "Annuler"     → supprime la mission avant le départ
    ///       - Active   → "Retour (Xj)" → rappel + X jours de trajet retour affichés
    /// </summary>
    public class MissionEntryUI : MonoBehaviour
    {
        private static readonly Color ColActive    = new Color(0.85f, 1.00f, 0.85f);
        private static readonly Color ColPending   = new Color(0.70f, 0.70f, 0.70f);
        private static readonly Color ColPosition  = new Color(0.40f, 1.00f, 0.60f);
        private static readonly Color ColPrep      = new Color(1.00f, 0.80f, 0.30f);
        private static readonly Color ColSurvivor  = new Color(0.75f, 0.95f, 0.75f);
        private static readonly Color ColLabel     = new Color(0.45f, 0.65f, 0.45f);
        private static readonly Color ColCancel    = new Color(0.80f, 0.20f, 0.20f);
        private static readonly Color ColRecall    = new Color(0.70f, 0.45f, 0.10f);

        [Header("Card labels")]
        [SerializeField] private TextMeshProUGUI destinationLabel;
        [SerializeField] private TextMeshProUGUI positionLabel;
        [SerializeField] private TextMeshProUGUI survivorsCountLabel;
        [SerializeField] private TextMeshProUGUI survivorsNamesLabel;

        [Header("Action button")]
        [SerializeField] private Button          recallButton;
        [SerializeField] private TextMeshProUGUI recallButtonLabel;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private ActiveMission          boundMission;

        // Pending-only — nécessaires pour l'annulation
        private List<SurvivorBehavior> pendingSurvivors;
        private ExplorationZone        pendingZone;
        private MissionData            pendingData;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnDestroy() { }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Lie la card à une mission active (déjà partie).</summary>
        public void BindActiveMission(ActiveMission mission)
        {
            boundMission     = mission;
            pendingSurvivors = null;
            pendingZone      = null;
            pendingData      = null;

            if (mission == null) return;

            string dest      = GetDestName(mission);
            int    totalDays = mission.Zone?.daysFromBase ?? 1;

            Set(destinationLabel, $"→  {dest.ToUpper()}", ColActive);
            Set(positionLabel,    GetPositionText(dest, totalDays, mission.MissionDay),
                                  mission.MissionDay >= totalDays ? ColPosition : ColPrep);

            BindSurvivors(mission.Survivors);

            // Bouton : "Retour (Xj)"
            int returnDays = mission.ReturnDays;
            string btnText = returnDays <= 1 ? "Rappeler" : $"Retour ({returnDays}j)";
            SetupButton(btnText, ColRecall, OnRecallActive);
        }

        /// <summary>Lie la card à une mission zone en attente (départ demain).</summary>
        public void BindPendingZone(List<SurvivorBehavior> survivors, ExplorationZone zone)
        {
            boundMission     = null;
            pendingSurvivors = survivors;
            pendingZone      = zone;
            pendingData      = null;

            string dest = zone?.zoneName ?? "Zone inconnue";
            Set(destinationLabel, $"→  {dest.ToUpper()}", ColPending);
            Set(positionLabel,    "Position : En préparation", ColPrep);
            BindSurvivors(survivors);
            SetupButton("Annuler", ColCancel, OnCancelPending);
        }

        /// <summary>Lie la card à une mission data-driven en attente (départ demain).</summary>
        public void BindPendingData(List<SurvivorBehavior> survivors, MissionData data)
        {
            boundMission     = null;
            pendingSurvivors = survivors;
            pendingZone      = null;
            pendingData      = data;

            string dest = data?.displayName ?? "Mission inconnue";
            Set(destinationLabel, $"→  {dest.ToUpper()}", ColPending);
            Set(positionLabel,    "Position : En préparation", ColPrep);
            BindSurvivors(survivors);
            SetupButton("Annuler", ColCancel, OnCancelPending);
        }

        // ── Position actuelle ─────────────────────────────────────────────────────

        private static string GetPositionText(string destination, int totalDays, int missionDay)
        {
            if (missionDay <= 0)           return "Position : En préparation";
            if (missionDay >= totalDays)   return $"Position : {destination} (arrivée)";
            return $"Position : Jour {missionDay} / {totalDays} — en route";
        }

        private static string GetDestName(ActiveMission m)
            => m.MissionDef != null ? m.MissionDef.displayName : m.Zone?.zoneName ?? "Zone inconnue";

        // ── Survivants ────────────────────────────────────────────────────────────

        private void BindSurvivors(IList<SurvivorBehavior> survivors)
        {
            int count = survivors?.Count ?? 0;
            Set(survivorsCountLabel, $"Équipe : {count} survivant{(count > 1 ? "s" : "")}", ColLabel);

            if (survivorsNamesLabel == null) return;
            if (count == 0) { survivorsNamesLabel.text = "—"; survivorsNamesLabel.color = ColLabel; return; }

            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (survivors[i] == null) continue;
                if (sb.Length > 0) sb.Append(",  ");
                sb.Append(survivors[i].SurvivorName);
            }
            survivorsNamesLabel.text  = sb.ToString();
            survivorsNamesLabel.color = ColSurvivor;
        }

        // ── Bouton ────────────────────────────────────────────────────────────────

        private void SetupButton(string label, Color bgColor, UnityEngine.Events.UnityAction action)
        {
            if (recallButton == null) return;
            recallButton.gameObject.SetActive(true);
            recallButton.onClick.RemoveAllListeners();
            recallButton.onClick.AddListener(action);

            Image bg = recallButton.GetComponent<Image>();
            if (bg != null) bg.color = bgColor;

            if (recallButtonLabel != null)
            {
                recallButtonLabel.text  = label;
                recallButtonLabel.color = Color.white;
            }
        }

        private void OnRecallActive()
        {
            if (boundMission == null) return;
            RadioCallManager.Instance?.RecallMission(boundMission);
            Destroy(gameObject);
        }

        private void OnCancelPending()
        {
            RadioCallManager rcm = RadioCallManager.Instance;
            if (rcm == null) return;

            if (pendingZone != null)
                rcm.CancelPendingZoneMission(pendingSurvivors, pendingZone);
            else if (pendingData != null)
                rcm.CancelPendingDataMission(pendingSurvivors, pendingData);

            Destroy(gameObject);
        }

        // ── Utilitaire ────────────────────────────────────────────────────────────

        private static void Set(TextMeshProUGUI label, string text, Color col)
        {
            if (label == null) return;
            label.text  = text;
            label.color = col;
        }
    }
}
