using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Floating tooltip shown when the player hovers over a mission entry in EN EXPLORATION.
    ///
    /// Layout expected in the prefab/scene:
    ///   MissionTooltipUI (RectTransform, initially inactive)
    ///   ├── ParticipantsLabel   (TextMeshProUGUI)
    ///   ├── EquipmentLabel      (TextMeshProUGUI)
    ///   └── DirectionLabel      (TextMeshProUGUI)
    ///
    /// Place this panel as a child of the root Canvas so it draws on top of everything.
    /// Assign the CanvasScaler root canvas to <see cref="rootCanvas"/> so pointer
    /// coordinates are converted correctly.
    /// </summary>
    public class MissionTooltipUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI participantsLabel;
        [SerializeField] private TextMeshProUGUI equipmentLabel;
        [SerializeField] private TextMeshProUGUI directionLabel;

        [Header("Layout")]
        [Tooltip("Canvas that owns this tooltip. Used for pointer → local coordinate conversion.")]
        [SerializeField] private Canvas rootCanvas;

        [Tooltip("Décalage en unités canvas depuis la position de la souris. (0,0) = coin bas-gauche sur le curseur.")]
        [SerializeField] private Vector2 pointerOffset = new Vector2(10f, 10f);

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            // Pivot (0,0) : coin bas-gauche positionné sur le curseur → tooltip se déploie vers le haut à droite.
            rectTransform.pivot        = Vector2.zero;
            // Anchor centré sur le Canvas pour que anchoredPosition = coordonnées locales Canvas.
            rectTransform.anchorMin    = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax    = new Vector2(0.5f, 0.5f);
            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the tooltip for an active <see cref="ActiveMission"/>.</summary>
        public void Show(ActiveMission mission)
        {
            if (mission == null) return;

            // Participants
            var sb = new StringBuilder();
            foreach (SurvivorBehavior s in mission.Survivors)
            {
                if (s == null) continue;
                if (sb.Length > 0) sb.Append("\n");
                sb.Append("• ").Append(s.SurvivorName);
            }
            SetText(participantsLabel, "PARTICIPANTS\n" + (sb.Length > 0 ? sb.ToString() : "—"));

            // Equipment
            string[] gear = mission.MissionDef?.equipment
                         ?? mission.Zone?.equipment
                         ?? System.Array.Empty<string>();
            SetText(equipmentLabel, BuildEquipmentText(gear));

            // Direction
            string dest = mission.MissionDef != null
                ? mission.MissionDef.displayName
                : mission.Zone?.zoneName ?? "Inconnu";
            int days = mission.Zone?.daysFromBase
                    ?? mission.MissionDef?.zone?.daysFromBase
                    ?? 0;
            SetText(directionLabel, "DIRECTION\n" + (days > 0 ? $"{dest}  ({days} j)" : dest));

            Activate();
        }

        /// <summary>Shows the tooltip for a pending (not yet departed) mission.</summary>
        public void ShowPending(System.Collections.Generic.List<SurvivorBehavior> survivors,
                                string[] equipment,
                                string direction)
        {
            // Participants
            var sb = new StringBuilder();
            if (survivors != null)
                foreach (SurvivorBehavior s in survivors)
                {
                    if (s == null) continue;
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("• ").Append(s.SurvivorName);
                }
            SetText(participantsLabel, "PARTICIPANTS\n" + (sb.Length > 0 ? sb.ToString() : "—"));

            SetText(equipmentLabel,  BuildEquipmentText(equipment));
            SetText(directionLabel,  "DIRECTION\n" + (direction ?? "—"));

            Activate();
        }

        /// <summary>Hides the tooltip.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Positionne le coin bas-gauche du tooltip sur la souris + offset.
        /// Utilise le Canvas racine pour la conversion screen → local, indépendamment de l'ancre.
        /// </summary>
        public void MoveToPointer()
        {
            if (rectTransform == null || rootCanvas == null) return;

            RectTransform canvasRT = rootCanvas.GetComponent<RectTransform>();
            Camera cam = rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            // ScreenPointToLocalPointInRectangle retourne les coords locales du Canvas
            // (origine au CENTRE du canvas). Avec anchor (0.5,0.5), anchoredPosition = localPoint.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT, Input.mousePosition, cam, out Vector2 localPoint)) return;

            rectTransform.anchoredPosition = localPoint + pointerOffset;
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void Activate()
        {
            gameObject.SetActive(true);
            MoveToPointer();
        }

        private static string BuildEquipmentText(string[] gear)
        {
            if (gear == null || gear.Length == 0) return "ÉQUIPEMENT\n—";
            var eq = new StringBuilder("ÉQUIPEMENT\n");
            foreach (string item in gear)
                eq.Append("• ").Append(item).Append("\n");
            return eq.ToString().TrimEnd('\n');
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
