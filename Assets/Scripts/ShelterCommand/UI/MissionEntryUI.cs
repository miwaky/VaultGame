using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// One row in the EN EXPLORATION list.
    ///
    /// Two modes:
    ///   • Active  — <see cref="BindActiveMission"/> : "Déplacement vers ZONE" (white, Rappeler visible)
    ///   • Pending — <see cref="BindPending"/>       : "Demain → ZONE" (grey, no Rappeler)
    ///
    /// Hover shows <see cref="MissionTooltipUI"/> with participants / equipment / direction.
    /// </summary>
    public class MissionEntryUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private static readonly Color ColorActive  = Color.white;
        private static readonly Color ColorPending = new Color(0.55f, 0.55f, 0.55f);

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI destinationText;

        [Header("Recall (optional)")]
        [SerializeField] private Button recallButton;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private ActiveMission          boundMission;
        private List<SurvivorBehavior> pendingSurvivors;
        private string[]               pendingEquipment;
        private string                 pendingDirection;
        private MissionTooltipUI       tooltip;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Binds to an already-active mission (departed).</summary>
        public void BindActiveMission(ActiveMission mission, MissionTooltipUI sharedTooltip)
        {
            boundMission     = mission;
            pendingSurvivors = null;
            tooltip          = sharedTooltip;

            if (mission == null) return;

            string dest = mission.MissionDef != null
                ? mission.MissionDef.displayName
                : mission.Zone?.zoneName ?? "Zone inconnue";

            SetText(destinationText, $"Déplacement vers {dest.ToUpper()}");
            if (destinationText != null) destinationText.color = ColorActive;

            if (recallButton != null)
            {
                recallButton.gameObject.SetActive(true);
                recallButton.onClick.RemoveAllListeners();
                recallButton.onClick.AddListener(OnRecall);
            }
        }

        /// <summary>
        /// Binds to a pending mission (departs tomorrow at 07:00).
        /// No recall button is shown.
        /// </summary>
        public void BindPending(string destinationLabel,
                                List<SurvivorBehavior> survivors,
                                string[] equipment,
                                MissionTooltipUI sharedTooltip)
        {
            boundMission     = null;
            pendingSurvivors = survivors;
            pendingEquipment = equipment;
            pendingDirection = destinationLabel;
            tooltip          = sharedTooltip;

            SetText(destinationText, $"Demain → {destinationLabel.ToUpper()}");
            if (destinationText != null) destinationText.color = ColorPending;

            if (recallButton != null)
                recallButton.gameObject.SetActive(false);
        }

        // ── Pointer events ────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (boundMission != null)
                tooltip?.Show(boundMission);
            else if (pendingSurvivors != null)
                tooltip?.ShowPending(pendingSurvivors, pendingEquipment, pendingDirection);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            tooltip?.MoveToPointer();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void OnDisable()  => tooltip?.Hide();
        private void OnDestroy()  => tooltip?.Hide();

        private void OnRecall()
        {
            tooltip?.Hide();
            if (boundMission == null) return;
            RadioCallManager.Instance?.RecallMission(boundMission);
            Destroy(gameObject);
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
