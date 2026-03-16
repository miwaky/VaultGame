using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShelterCommand
{
    /// <summary>
    /// Main controller for the computer terminal software menu.
    /// Manages navigation between the main menu and the sub-panels:
    /// Camera (owned by CameraWallPanelUI inside ComputerUI), Schedule, Exploration, and Residents.
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

        // ── Camera panel (now part of ComputerUI) ────────────────────────────────
        [Header("Camera Panel")]
        [Tooltip("The CameraWallPanelUI GameObject living inside ComputerUI.")]
        [SerializeField] private CameraWallPanelUI cameraWallPanelUI;

        // ── Sub-panels ────────────────────────────────────────────────────────────
        [Header("Sub-Panels")]
        [SerializeField] private GameObject schedulePanel;
        [SerializeField] private GameObject explorationPanel;
        [SerializeField] private GameObject residentsPanel;

        // ── Panel close buttons ───────────────────────────────────────────────────
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

            // Ensure the camera panel is hidden at startup
            if (cameraWallPanelUI != null)
                cameraWallPanelUI.gameObject.SetActive(false);
        }

        private void Start()
        {
            shelterHUD = FindFirstObjectByType<ShelterHUD>();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Opens the computer interface. Locks FPS, shows cursor, hides crosshair.</summary>
        public void Open(OfficeInteractionSystem interaction, Action onQuit)
        {
            interactionSystem = interaction;
            onQuitCallback    = onQuit;

            if (shelterHUD == null)
                shelterHUD = FindFirstObjectByType<ShelterHUD>();

            interaction.SetFPSLocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            SetCrosshair(false);

            gameObject.SetActive(true);
            ShowMainMenu();
        }

        /// <summary>Closes the interface, restores FPS control, and hides the cursor.</summary>
        public void Close()
        {
            // Make sure any active camera controller is deactivated before closing.
            cameraWallPanelUI?.Close();

            HideAllPanels();
            gameObject.SetActive(false);

            if (interactionSystem != null)
                interactionSystem.SetFPSLocked(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            SetCrosshair(true);

            onQuitCallback?.Invoke();
        }

        /// <summary>Returns to the main menu from any sub-panel (called by X buttons or CameraWallPanelUI).</summary>
        public void ShowMainMenu()
        {
            HideAllPanels();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            interactionSystem?.SetFPSLocked(true);
            SafeSetActive(mainMenuPanel, true);
        }

        // ── Navigation ───────────────────────────────────────────────────────────

        /// <summary>Opens the camera panel embedded inside ComputerUI.</summary>
        private void OpenCameraPanel()
        {
            if (cameraWallPanelUI == null)
            {
                Debug.LogWarning("[ComputerMenuController] CameraWallPanelUI non assigné dans l'Inspector.");
                return;
            }

            HideAllPanels();

            // Discover all SecurityCameraController in the scene
            SecurityCameraController[] controllers =
                FindObjectsByType<SecurityCameraController>(FindObjectsSortMode.None);

            Debug.Log($"[ComputerMenuController] OpenCameraPanel — {controllers.Length} SecurityCameraController trouvés.");

            foreach (var c in controllers)
                Debug.Log($"[ComputerMenuController]   → {c.name} (actif={c.gameObject.activeSelf}, SecurityCamera={c.SecurityCamera})");

            // Sort by name for deterministic ordering
            System.Array.Sort(controllers, (a, b) =>
                string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

            cameraWallPanelUI.gameObject.SetActive(true);
            Debug.Log($"[ComputerMenuController] cameraWallPanelUI.activeSelf après SetActive(true) = {cameraWallPanelUI.gameObject.activeSelf}");

            cameraWallPanelUI.Open(controllers, this);
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

        private void SetCrosshair(bool visible)
        {
            if (crosshairObject != null)
                crosshairObject.SetActive(visible);

            shelterHUD?.SetCrosshairVisible(visible);
        }

        private void HideAllPanels()
        {
            SafeSetActive(mainMenuPanel,    false);
            SafeSetActive(schedulePanel,    false);
            SafeSetActive(explorationPanel, false);
            SafeSetActive(residentsPanel,   false);

            // Close camera panel without destroying — it deactivates itself.
            if (cameraWallPanelUI != null && cameraWallPanelUI.gameObject.activeSelf)
                cameraWallPanelUI.Close();
        }

        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
