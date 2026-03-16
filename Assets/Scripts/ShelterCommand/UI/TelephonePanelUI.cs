using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// UI controller for the telephone panel.
    /// Displays call options, handles incoming call notifications, and delegates actions
    /// to <see cref="TelephoneController"/>.
    /// </summary>
    public class TelephonePanelUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Buttons")]
        [SerializeField] private Button buttonCallTeam;
        [SerializeField] private Button buttonCallGuard;
        [SerializeField] private Button buttonAnswer;
        [SerializeField] private Button buttonClose;

        [Header("Guard Orders Panel")]
        [Tooltip("The GuardOrdersPanel GameObject — toggled when the player taps 'Call Guard'.")]
        [SerializeField] private GameObject guardOrdersPanel;

        [Header("Incoming Call Highlight")]
        [Tooltip("Root image or panel that highlights when a call is incoming.")]
        [SerializeField] private GameObject incomingCallHighlight;

        [Header("Status Label")]
        [SerializeField] private TextMeshProUGUI statusLabel;

        // ── Runtime ───────────────────────────────────────────────────────────────

        private TelephoneController telephoneController;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            buttonCallTeam?.onClick.AddListener(OnCallTeamClicked);
            buttonCallGuard?.onClick.AddListener(OnCallGuardClicked);
            buttonAnswer?.onClick.AddListener(OnAnswerClicked);
            buttonClose?.onClick.AddListener(OnCloseClicked);

            SetIncomingCallVisible(false);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Initialises the panel with its controller and refreshes button states.</summary>
        public void Open(TelephoneController controller)
        {
            telephoneController = controller;
            RefreshAnswerButton();
            SetStatus(telephoneController.HasIncomingCall ? "Appel entrant…" : "Prêt");
        }

        /// <summary>Shows or hides the incoming call notification and highlights the Answer button.</summary>
        public void ShowIncomingCall(bool visible, string callerName = "")
        {
            SetIncomingCallVisible(visible);
            RefreshAnswerButton();
            SetStatus(visible ? $"Appel entrant : {callerName}" : "Prêt");
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void OnCallTeamClicked()
        {
            // Placeholder — team call list will be implemented later.
            SetStatus("[Appel équipe — à implémenter]");
            Debug.Log("[TelephonePanelUI] Call Team — placeholder.");
        }

        private void OnCallGuardClicked()
        {
            if (guardOrdersPanel == null)
            {
                Debug.LogWarning("[TelephonePanelUI] guardOrdersPanel non assigné dans l'Inspector.");
                return;
            }

            guardOrdersPanel.SetActive(!guardOrdersPanel.activeSelf);
            SetStatus(guardOrdersPanel.activeSelf ? "Steve — ordres disponibles" : "Prêt");
            Debug.Log("[TelephonePanelUI] Call Guard — GuardOrdersPanel toggled.");
        }

        private void OnAnswerClicked()
        {
            if (telephoneController == null) return;
            telephoneController.AnswerIncomingCall();
        }

        private void OnCloseClicked()
        {
            telephoneController?.Close();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void RefreshAnswerButton()
        {
            if (buttonAnswer != null)
                buttonAnswer.interactable = telephoneController != null && telephoneController.HasIncomingCall;
        }

        private void SetIncomingCallVisible(bool visible)
        {
            if (incomingCallHighlight != null)
                incomingCallHighlight.SetActive(visible);
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
