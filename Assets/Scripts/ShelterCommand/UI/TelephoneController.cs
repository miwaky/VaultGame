using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace ShelterCommand
{
    /// <summary>
    /// Hub de communication téléphonique du joueur.
    ///
    /// Responsabilités :
    ///   • Ouvrir/fermer le <see cref="TelephonePanelUI"/> avec la touche T.
    ///   • Bloquer le déplacement FPS et afficher le curseur pendant l'ouverture.
    ///   • Gérer les appels entrants (sonnerie + notification visuelle).
    ///   • Répondre à un appel et déléguer au <see cref="DialogueManager"/>.
    ///
    /// Attach this to the same GameObject as <see cref="ShelterFPSController"/> or to any
    /// persistent manager object in the scene. Wire all references in the Inspector.
    /// </summary>
    public class TelephoneController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("UI")]
        [SerializeField] private GameObject telephonePanel;
        [SerializeField] private TelephonePanelUI telephonePanelUI;

        [Header("Physical Phone")]
        [Tooltip("The PhoneObject component on the hand-held phone GameObject (child of HandAnchor).")]
        [SerializeField] private PhoneObject phoneObject;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   ringtoneClip;

        [Header("Dependencies")]
        [SerializeField] private ShelterFPSController fpsController;
        [SerializeField] private ShelterHUD           shelterHUD;

        // ── Runtime ───────────────────────────────────────────────────────────────

        private bool                 isOpen;
        private RadioCallEvent       pendingCallEvent;
        private ActiveMission        pendingMission;
        private Action               onCallAnswered;

        // ── Public read ───────────────────────────────────────────────────────────

        /// <summary>True when a call is waiting to be answered.</summary>
        public bool HasIncomingCall => pendingCallEvent != null;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            if (fpsController == null)
                fpsController = FindFirstObjectByType<ShelterFPSController>();

            if (shelterHUD == null)
                shelterHUD = FindFirstObjectByType<ShelterHUD>();

            if (telephonePanel != null)
                telephonePanel.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                if (isOpen)
                    Close();
                else
                    Open();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the telephone panel, shows the physical phone in hand, locks the player, and shows the cursor.</summary>
        public void Open()
        {
            if (isOpen) return;
            isOpen = true;

            phoneObject?.Show();

            if (telephonePanel != null)
                telephonePanel.SetActive(true);

            telephonePanelUI?.Open(this);

            LockPlayer(true);

            Debug.Log("[TelephoneController] Téléphone ouvert.");
        }

        /// <summary>Closes the telephone panel, hides the physical phone, and restores player control.</summary>
        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;

            phoneObject?.Hide();

            if (telephonePanel != null)
                telephonePanel.SetActive(false);

            LockPlayer(false);

            Debug.Log("[TelephoneController] Téléphone fermé.");
        }

        /// <summary>
        /// Called by external systems (e.g. <see cref="RadioCallManager"/>) to trigger
        /// an incoming call: plays ringtone, shows notification, and stores the pending call.
        /// </summary>
        /// <summary>
        /// Notifie le joueur d'un appel entrant via bannière HUD et highlight du panel.
        /// <paramref name="onAnsweredCallback"/> est appelé après la fin du dialogue (pour chaîner les appels).
        /// </summary>
        public void ReceiveIncomingCall(RadioCallEvent callEvent, ActiveMission mission, Action onAnsweredCallback = null)
        {
            pendingCallEvent = callEvent;
            pendingMission   = mission;
            onCallAnswered   = onAnsweredCallback;

            string caller = GetCallerName(callEvent);

            PlayRingtone();
            shelterHUD?.ShowIncomingCallBanner(caller);
            telephonePanelUI?.ShowIncomingCall(true, caller);

            // Si le panel est déjà ouvert, on rafraîchit directement.
            if (isOpen)
                telephonePanelUI?.Open(this);

            Debug.Log($"[TelephoneController] Appel entrant : {callEvent?.name}");
        }

        /// <summary>Answers the pending incoming call and opens the radio dialogue.</summary>
        public void AnswerIncomingCall()
        {
            if (pendingCallEvent == null)
            {
                Debug.LogWarning("[TelephoneController] AnswerIncomingCall appelé sans appel entrant.");
                return;
            }

            StopRingtone();
            shelterHUD?.HideIncomingCallBanner();
            telephonePanelUI?.ShowIncomingCall(false);

            RadioCallEvent callToAnswer  = pendingCallEvent;
            ActiveMission  mission       = pendingMission;
            Action         callback      = onCallAnswered;
            ClearPendingCall();

            Close();

            DialogueManager.Instance?.StartDialogue(callToAnswer, mission, callback);

            Debug.Log($"[TelephoneController] Appel répondu : {callToAnswer.name}");
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void LockPlayer(bool locked)
        {
            if (fpsController != null)
                fpsController.SetLocked(locked);

            Cursor.lockState = locked ? CursorLockMode.None  : CursorLockMode.Locked;
            Cursor.visible   = locked;
        }

        private void PlayRingtone()
        {
            if (audioSource == null || ringtoneClip == null) return;
            audioSource.clip   = ringtoneClip;
            audioSource.loop   = true;
            audioSource.Play();
        }

        private void StopRingtone()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        private void ClearPendingCall()
        {
            pendingCallEvent = null;
            pendingMission   = null;
            onCallAnswered   = null;
        }

        private static string GetCallerName(RadioCallEvent callEvent)
        {
            if (callEvent == null) return "Inconnu";
            return string.IsNullOrEmpty(callEvent.dialogue?.speakerName)
                ? callEvent.name
                : callEvent.dialogue.speakerName;
        }
    }
}
