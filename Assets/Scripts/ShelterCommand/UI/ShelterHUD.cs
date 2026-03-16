// SHELTER HUD — version propre

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// HUD principal de Shelter Command.
    /// Gère : barre de statut, panel radio, notifications, game over/won, crosshair.
    /// Le panel caméras est géré par CameraWallPanelUI dans le ComputerUI.
    /// </summary>
    public class ShelterHUD : MonoBehaviour
    {
        // ── Status Bar ──────────────────────────────────────────────────────────
        [Header("Status Bar")]
        [SerializeField] private TextMeshProUGUI foodText;
        [SerializeField] private TextMeshProUGUI waterText;
        [SerializeField] private TextMeshProUGUI medicineText;
        [SerializeField] private TextMeshProUGUI materialsText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI populationText;

        // ── Radio Panel ─────────────────────────────────────────────────────────
        [Header("Radio Panel")]
        [SerializeField] private GameObject radioPanel;
        [SerializeField] private TextMeshProUGUI radioMessageText;
        [SerializeField] private Button closeRadioButton;

        // ── Notification ────────────────────────────────────────────────────────
        [Header("Notification")]
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float notificationDuration = 3.5f;
        private float notificationTimer;

        // ── Incoming Call Banner ─────────────────────────────────────────────────
        [Header("Incoming Call Banner")]
        [Tooltip("Label persistant affiché tant qu'un appel téléphonique est en attente.")]
        [SerializeField] private TextMeshProUGUI incomingCallBannerText;
        [SerializeField] private float blinkInterval = 0.6f;
        private bool  isBannerVisible;
        private float blinkTimer;

        // ── Game End ────────────────────────────────────────────────────────────
        [Header("Game End")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject gameWonPanel;

        // ── Crosshair ──────────────────────────────────────────────────────────
        [Header("Crosshair")]
        [SerializeField] private GameObject crosshair;

        // ── State ───────────────────────────────────────────────────────────────
        private ShelterGameManager gm;
        private DayCycleManager    dayCycleManager;

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Start()
        {
            gm = ShelterGameManager.Instance;
            if (gm == null) { Debug.LogError("[ShelterHUD] ShelterGameManager introuvable."); return; }

            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
            if (dayCycleManager != null)
            {
                dayCycleManager.OnTimeChanged += OnTimeChanged;
                SetText(timeText, dayCycleManager.GetFormattedTime());
            }

            SubscribeEvents();
            SetupButtons();
            HideAllPanels();
            RefreshStatusBar();
        }

        private void Update()
        {
            TickNotification();
            TickBanner();
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                ComputerMenuController terminal = FindFirstObjectByType<ComputerMenuController>();
                if (terminal != null && terminal.gameObject.activeSelf)
                    terminal.Close();
                else
                    CloseAllAndReturnToFPS();
            }
        }

        private void OnDestroy()
        {
            if (dayCycleManager != null)
                dayCycleManager.OnTimeChanged -= OnTimeChanged;
        }

        // ── Event subscriptions ─────────────────────────────────────────────────

        private void OnTimeChanged(int hour, int minute)
        {
            SetText(timeText, $"{hour:D2}:{minute:D2}");
        }

        private void SubscribeEvents()
        {
            gm.ResourceManager.OnResourcesChanged  += RefreshStatusBar;
            gm.SurvivorManager.OnPopulationChanged += RefreshStatusBar;
            gm.SurvivorManager.OnSurvivorDied      += s => ShowNotification($"{s.SurvivorName} est mort.");
            gm.DayManager.OnDayStarted             += d => { RefreshStatusBar(); ShowNotification($"Jour {d} — L'abri s'eveille."); };
            gm.DayManager.OnGameOver               += () => { HideAllPanels(); SafeSetActive(gameOverPanel, true); };
            gm.DayManager.OnGameWon                += () => { SafeSetActive(gameWonPanel, true); };
            gm.CameraRoomController.OnSurvivorClickedInCamera += s => gm.SurvivorManager.SelectSurvivor(s);
        }

        // ── Button setup ────────────────────────────────────────────────────────

        private void SetupButtons()
        {
            closeRadioButton?.onClick.AddListener(CloseAllAndReturnToFPS);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Shows or hides the FPS crosshair. Called by ComputerMenuController.</summary>
        public void SetCrosshairVisible(bool visible) => SafeSetActive(crosshair, visible);

        /// <summary>
        /// Affiche la bannière persistante d'appel entrant.
        /// Texte clignotant jusqu'à l'appel à <see cref="HideIncomingCallBanner"/>.
        /// </summary>
        public void ShowIncomingCallBanner(string callerName)
        {
            if (incomingCallBannerText == null) return;
            isBannerVisible = true;
            incomingCallBannerText.text      = $"APPEL ENTRANT — {callerName}  [ T pour répondre ]";
            incomingCallBannerText.gameObject.SetActive(true);
            blinkTimer = 0f;
        }

        /// <summary>Masque la bannière d'appel entrant.</summary>
        public void HideIncomingCallBanner()
        {
            isBannerVisible = false;
            if (incomingCallBannerText != null)
                incomingCallBannerText.gameObject.SetActive(false);
        }

        private void TickBanner()
        {
            if (!isBannerVisible || incomingCallBannerText == null) return;
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                incomingCallBannerText.enabled = !incomingCallBannerText.enabled;
            }
        }

        /// <summary>Shows radio panel with message (called by RadioProp).</summary>
        public void ShowRadioPanel(string message)
        {
            HideAllPanels();
            SafeSetActive(radioPanel, true);
            SetText(radioMessageText, message);
            SafeSetActive(crosshair, false);
        }

        /// <summary>Shows a notification without locking FPS (called by BedProp).</summary>
        public void ShowNotificationPublic(string message) => ShowNotification(message);

        /// <summary>Closes all panels and returns to FPS mode.</summary>
        public void CloseAllAndReturnToFPS()
        {
            if (gm.CameraRoomController.IsInFullScreen)
                gm.CameraRoomController.DeselectRoom();

            HideAllPanels();
            SafeSetActive(crosshair, true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            OfficeInteractionSystem interact = FindFirstObjectByType<OfficeInteractionSystem>();
            interact?.SetFPSLocked(false);
            FindFirstObjectByType<ComputerTerminalProp>()?.NotifyTerminalClosed();
        }

        // ── Status bar ───────────────────────────────────────────────────────────

        private void RefreshStatusBar()
        {
            if (gm == null) return;
            ShelterResourceManager rm = gm.ResourceManager;
            SetText(foodText,      $"NOURR. {rm.FoodInt}");
            SetText(waterText,     $"EAU {rm.WaterInt}");
            SetText(medicineText,  $"MED. {rm.Medicine}");
            SetText(materialsText, $"MAT. {rm.Materials}");
            SetText(energyText,    $"ENRG. {rm.Energy}%");
            SetText(dayText,       $"JOUR {gm.DayManager.CurrentDay}");
            SetText(populationText,$"POP. {gm.SurvivorManager.AliveSurvivorCount}");
        }

        // ── Utility ──────────────────────────────────────────────────────────────

        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private void HideAllPanels()
        {
            SafeSetActive(radioPanel,    false);
            SafeSetActive(gameOverPanel, false);
            SafeSetActive(gameWonPanel,  false);
        }

        private void ShowNotification(string message)
        {
            SetText(notificationText, message);
            notificationTimer = notificationDuration;
            if (notificationText != null) notificationText.gameObject.SetActive(true);
        }

        private void TickNotification()
        {
            if (notificationTimer <= 0f) return;
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f && notificationText != null)
                notificationText.gameObject.SetActive(false);
        }

        private static void SetText(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }
    }
}
