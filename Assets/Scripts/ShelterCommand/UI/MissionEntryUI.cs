using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Displays a single active exploration mission inside the ExplorationPanelUI.
    ///
    /// Visual layout (horizontal bar):
    ///   [Destination]  [Survivants]  [Progression]  [Ressources]
    ///
    /// Wire up all TextMeshProUGUI references in the prefab Inspector.
    /// </summary>
    public class MissionEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI destinationText;
        [SerializeField] private TextMeshProUGUI survivorsText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI resourcesText;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Binds this row to a mission and refreshes all display fields.</summary>
        public void Bind(ExplorationMission mission)
        {
            if (mission == null) return;
            Refresh(mission);
        }

        /// <summary>Updates display fields from a live mission (call each frame or on tick).</summary>
        public void Refresh(ExplorationMission mission)
        {
            if (mission == null) return;

            SetText(destinationText, mission.Destination.ToUpper());

            // Build survivor name list
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < mission.Survivors.Count; i++)
            {
                SurvivorBehavior survivor = mission.Survivors[i];
                if (survivor == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(survivor.SurvivorName);
            }
            SetText(survivorsText, sb.Length > 0 ? sb.ToString() : "—");

            SetText(progressText, mission.GetProgressText());

            SetText(resourcesText,
                $"🍖 +{mission.FoodGathered:F0}  " +
                $"💧 +{mission.WaterGathered:F0}  " +
                $"🔧 +{mission.MaterialsGathered:F0}");
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
