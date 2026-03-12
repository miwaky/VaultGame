using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShelterCommand
{
    /// <summary>
    /// Main controller for the computer terminal software menu.
    /// Manages navigation between the main menu and the four sub-panels:
    /// Camera (delegates to ShelterHUD.CameraWallPanel), Schedule, Exploration, and Residents.
    ///
    /// Attach this to the root Canvas of the computer UI.
    /// Wire up all serialized fields in the Inspector.
    /// </summary>
    public class ComputerMenuController : MonoBehaviour
    {
        // ── Main menu ────────────────────────────────────────────────────────────
        [Header("Main Menu")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button cameraButton;
        [SerializeField] private Button scheduleButton;
        [SerializeField] private Button explorationButton;
        [SerializeField] private Button residentsButton;

        [Header("Close Button (haut droite du menu principal)")]
        [Tooltip("Le bouton X en haut à droite qui ferme le terminal.")]
        [SerializeField] private Button mainMenuCloseButton;

        // ── Sub-panels (Schedule, Exploration, Residents only) ───────────────────
        // Camera is handled exclusively by ShelterHUD.CameraWallPanel.
        [Header("Sub-Panels")]
        [SerializeField] private GameObject schedulePanel;
        [SerializeField] private GameObject explorationPanel;
        [SerializeField] private GameObject residentsPanel;

        // ── X buttons on Exploration panel ───────────────────────────────────────
        [Header("Panel Close Buttons")]
        [Tooltip("The X button on the ExplorationPanel.")]
        [SerializeField] private Button explorationCloseButton;

        // ── Dependencies ─────────────────────────────────────────────────────────
        [Header("Dependencies")]
        [Tooltip("Reference to the SchedulePanelUI component on the SchedulePanel.")]
        [SerializeField] private SchedulePanelUI schedulePanelUI;

        [Tooltip("Reference to the ResidentsPanelUI component on the ResidentsPanel.")]
        [SerializeField] private ResidentsPanelUI residentsPanelUI;

        [Tooltip("Reference to the ExplorationPanelUI component on the ExplorationPanel.")]
        [SerializeField] private ExplorationPanelUI explorationPanelUI;

        [Tooltip("The FPS crosshair GameObject to hide while the terminal is open.")]
        [SerializeField] private GameObject crosshairObject;

        // ── State ────────────────────────────────────────────────────────────────
        private OfficeInteractionSystem interactionSystem;
        private Action onQuitCallback;
        private ShelterHUD shelterHUD;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            shelterHUD = FindFirstObjectByType<ShelterHUD>();
            if (shelterHUD == null)
                Debug.LogWarning("[ComputerMenuController] ShelterHUD introuvable — le panel Caméra ne fonctionnera pas.");
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the computer interface. Locks FPS, shows cursor, hides crosshair.
        /// </summary>
        public void Open(OfficeInteractionSystem interaction, Action onQuit)
        {
            interactionSystem = interaction;
            onQuitCallback    = onQuit;

            if (shelterHUD == null)
                shelterHUD = FindFirstObjectByType<ShelterHUD>();

            interaction.SetFPSLocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            // Hide FPS crosshair — always go through ShelterHUD to keep a single source of truth
            SetCrosshair(false);

            gameObject.SetActive(true);
            ShowMainMenu();
        }

        /// <summary>Closes the interface, restores FPS control, and hides the cursor.</summary>
        public void Close()
        {
            HideAllPanels();
            gameObject.SetActive(false);

            if (interactionSystem != null)
                interactionSystem.SetFPSLocked(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            // Restore crosshair for FPS gameplay
            SetCrosshair(true);

            onQuitCallback?.Invoke();
        }

        /// <summary>Returns to the main menu from any sub-panel (called by X buttons).</summary>
        public void ShowMainMenu()
        {
            // NOTE: we stay inside the terminal — do NOT call CloseAllAndReturnToFPS here.
            // Just re-show the main menu, keep FPS locked and crosshair hidden.
            HideAllPanels();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            interactionSystem?.SetFPSLocked(true);
            SafeSetActive(mainMenuPanel, true);
        }

        // ── Navigation ───────────────────────────────────────────────────────────

        /// <summary>Delegates camera display to the existing ShelterHUD.CameraWallPanel.</summary>
        private void OpenCameraPanel()
        {
            HideAllPanels();

            if (shelterHUD == null)
            {
                Debug.LogWarning("[ComputerMenuController] ShelterHUD introuvable.");
                return;
            }

            // Hide our own Canvas so only the ShelterHUD camera wall is visible.
            // ComputerUI stays active so Update() and button listeners remain live.
            SafeSetActive(mainMenuPanel, false);

            SecurityCamera[] cameras = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
                cameras[i].CameraLabel = $"CAM-{i + 1:D2}";

            // ShelterHUD shows its CameraWallPanel without sidebar/dossier/mission buttons
            shelterHUD.OpenCameraWall(cameras, fromTerminal: true);
        }

        private void OpenSchedulePanel()
        {
            HideAllPanels();
            SafeSetActive(schedulePanel, true);
            schedulePanelUI?.Populate();
        }

        private void OpenExplorationPanel()
        {
            HideAllPanels();
            SafeSetActive(explorationPanel, true);
            explorationPanelUI?.Populate();
        }

        private void OpenResidentsPanel()
        {
            HideAllPanels();
            SafeSetActive(residentsPanel, true);
            residentsPanelUI?.Populate();
        }

        // ── Button wiring ────────────────────────────────────────────────────────

        private void BindButtons()
        {
            cameraButton?.onClick.AddListener(OpenCameraPanel);
            scheduleButton?.onClick.AddListener(OpenSchedulePanel);
            explorationButton?.onClick.AddListener(OpenExplorationPanel);
            residentsButton?.onClick.AddListener(OpenResidentsPanel);
            mainMenuCloseButton?.onClick.AddListener(Close);
            explorationCloseButton?.onClick.AddListener(ShowMainMenu);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Single entry point for crosshair visibility.
        /// Uses the direct reference first, then falls back to ShelterHUD.
        /// Always syncs both so they stay consistent.
        /// </summary>
        private void SetCrosshair(bool visible)
        {
            if (crosshairObject != null)
                crosshairObject.SetActive(visible);

            // Also tell ShelterHUD so CloseAllAndReturnToFPS does not conflict
            shelterHUD?.SetCrosshairVisible(visible);
        }

        private void HideAllPanels()
        {
            SafeSetActive(mainMenuPanel,    false);
            SafeSetActive(schedulePanel,    false);
            SafeSetActive(explorationPanel, false);
            SafeSetActive(residentsPanel,   false);
        }

        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
