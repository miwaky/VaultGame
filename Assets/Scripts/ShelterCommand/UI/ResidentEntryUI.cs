using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// A single selectable entry in the Residents panel survivor list.
    /// Clicking the entry invokes the selection callback provided by ResidentsPanelUI.
    /// Wire up nameText and background in the Inspector (or configure via prefab).
    /// </summary>
    public class ResidentEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image           background;

        private static readonly Color AliveColor    = new Color(0.08f, 0.14f, 0.08f, 0.9f);
        private static readonly Color DeadColor     = new Color(0.25f, 0.05f, 0.05f, 0.9f);
        private static readonly Color SelectedColor = new Color(0.20f, 0.50f, 0.20f, 1.00f);

        private Button button;

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Binds this entry to a survivor and optionally registers a selection callback.
        /// </summary>
        /// <param name="survivor">The survivor to display.</param>
        /// <param name="onSelected">Optional callback invoked when the player clicks this entry.</param>
        public void Bind(SurvivorBehavior survivor, Action onSelected)
        {
            if (nameText != null)
            {
                string status = !survivor.IsAlive    ? " [MORT]"    :
                                 survivor.IsOnMission ? " [MISSION]" :
                                 survivor.IsSick      ? " [MALADE]"  :
                                 survivor.IsArrested  ? " [ARRÊTÉ]"  : "";
                nameText.text  = survivor.SurvivorName.ToUpper() + status;
                nameText.color = survivor.IsAlive ? new Color(0.6f, 1f, 0.6f) : new Color(0.5f, 0.3f, 0.3f);
            }

            if (background != null)
                background.color = survivor.IsAlive ? AliveColor : DeadColor;

            button = GetComponent<Button>();
            if (button != null && onSelected != null)
                button.onClick.AddListener(() => onSelected.Invoke());
        }
    }
}
