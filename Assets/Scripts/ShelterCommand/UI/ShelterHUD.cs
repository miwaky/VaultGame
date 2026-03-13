// SHELTER HUD — version propre
// L'ancien ShelterHUD.cs doit être supprimé manuellement depuis Unity (clic droit > Delete)
// puis ce fichier renommé en ShelterHUD.cs
// La classe s'appelle DEJA ShelterHUD pour compatibilité avec les autres scripts.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// HUD principal de Shelter Command (version propre).
    /// Chaque panneau s'ouvre via un prop physique du bureau.
    /// ESC ou bouton Fermer retournent au mode FPS.
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

        // ── Camera Wall ──────────────────────────────────────────────────────────
        [Header("Camera Wall (mono-viewer)")]
        [SerializeField] private GameObject cameraWallPanel;
        [SerializeField] private RawImage   cameraFeedImage;
        [SerializeField] private TextMeshProUGUI cameraLabelText;
        [SerializeField] private Button     camPrevButton;
        [SerializeField] private Button     camNextButton;
        [SerializeField] private Button     closeCameraWallButton;
        [SerializeField] private GameObject survivorSidebar;        // hidden until a camera is active
        [SerializeField] private Transform  survivorNameListContainer;
        [SerializeField] private Button     openDossierButton;
        [SerializeField] private Button     openMissionMapButton;

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
        private static int radioIndex;

        // Camera cycling state
        private SecurityCamera[] availableCameras = new SecurityCamera[0];
        private int currentCameraIndex = -1;
        private ComputerTerminalProp activeTerminal;

        private static readonly string[] RadioMessages =
        {
            "Signal capte... Ici Foxtrot-7. Survivants reperes a 40 km nord.",
            "...parasites... quelqu'un... entend... [SIGNAL PERDU]",
            "Alerte : mouvement de masse detecte a l'est. Nature inconnue.",
            "Base Delta. Carburant epuise. On ne peut plus tenir. Bonne chance.",
            "Frequence secours 146.520 MHz. Rejoignez si vous pouvez.",
            "Medecin de Sainte-Anne. Antibiotiques disponibles. Quelqu'un recoit?",
            "Abri Bravo-9. Generateur tient. Deux semaines de vivres restantes.",
        };

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Start()
        {
            gm = ShelterGameManager.Instance;
            if (gm == null) { Debug.LogError("[ShelterHUD] ShelterGameManager introuvable."); return; }

            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
            if (dayCycleManager != null)
            {
                dayCycleManager.OnTimeChanged += OnTimeChanged;
                // Display initial time immediately
                SetText(timeText, dayCycleManager.GetFormattedTime());
            }

            SubscribeEvents();
            SetupButtons();
            HideAllPanels();
            RefreshStatusBar();
            // Sidebar hidden until a camera is selected
            SafeSetActive(survivorSidebar, false);
        }

        private void Update()
        {
            TickNotification();
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                // If the computer terminal canvas is open, delegate to its Close() so all panels
                // are properly hidden and state is fully reset — prevents the black-screen bug.
                ComputerMenuController terminal = FindFirstObjectByType<ComputerMenuController>();
                if (terminal != null && terminal.gameObject.activeSelf)
                    terminal.Close();
                else
                    CloseAllAndReturnToFPS();
            }
        }

        // ── Event subscriptions ─────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (dayCycleManager != null)
                dayCycleManager.OnTimeChanged -= OnTimeChanged;
        }

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

        /// <summary>
        /// Handles the Close button on the CameraWallPanel.
        /// If the terminal is open, returns to the ComputerMenuController main menu.
        /// Otherwise performs a full FPS return.
        /// </summary>
        private void OnCloseCameraWall()
        {
            ComputerMenuController menu = FindFirstObjectByType<ComputerMenuController>();
            if (menu != null && menu.gameObject.activeSelf)
            {
                // Coming from the terminal — hide the HUD camera wall and go back to main menu.
                SafeSetActive(cameraWallPanel, false);
                SafeSetActive(survivorSidebar, false);
                menu.ShowMainMenu();
            }
            else
            {
                CloseAllAndReturnToFPS();
            }
        }

        private void SetupButtons()
        {
            closeCameraWallButton?.onClick.AddListener(OnCloseCameraWall);
            camPrevButton?.onClick.AddListener(CycleCameraPrev);
            camNextButton?.onClick.AddListener(CycleCameraNext);
            closeRadioButton?.onClick.AddListener(CloseAllAndReturnToFPS);
        }

        private void BindRenderTextures() { /* No-op — textures bound dynamically per SecurityCamera */ }
        private void BindRT(RawImage img, ShelterRoomType room) { }

        // ── Public API — called by props ─────────────────────────────────────────

        /// <summary>Shows or hides the FPS crosshair. Called by ComputerMenuController.</summary>
        public void SetCrosshairVisible(bool visible) => SafeSetActive(crosshair, visible);

        /// <summary>
        /// Opens the camera terminal with the provided list of SecurityCameras.
        /// When <paramref name="fromTerminal"/> is true the survivor sidebar,
        /// dossier button, and mission button are hidden (terminal has its own panels).
        /// </summary>
        public void OpenCameraWall(SecurityCamera[] cameras, bool fromTerminal = false)
        {
            HideAllPanels();
            availableCameras   = cameras ?? new SecurityCamera[0];
            currentCameraIndex = -1;

            SafeSetActive(cameraWallPanel,  true);
            SafeSetActive(crosshair,        false);
            SafeSetActive(survivorSidebar,  false);

            // Hide sidebar-related actions when opened from the computer terminal
            if (openMissionMapButton != null) openMissionMapButton.gameObject.SetActive(false);

            if (availableCameras.Length > 0)
                ShowCamera(0, fromTerminal);
        }

        /// <summary>Legacy overload — auto-discovers all SecurityCameras in the scene.</summary>
        public void OpenCameraWall()
        {
            OpenCameraWall(FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None));
        }

        // ── Camera cycling ────────────────────────────────────────────────────────

        private void ShowCamera(int index, bool sidebarHidden = false)
        {
            if (availableCameras == null || availableCameras.Length == 0) return;
            index = Mathf.Clamp(index, 0, availableCameras.Length - 1);
            currentCameraIndex = index;

            SecurityCamera cam = availableCameras[index];
            if (cameraFeedImage != null && cam.RenderTexture != null)
                cameraFeedImage.texture = cam.RenderTexture;
            SetText(cameraLabelText, $"{cam.CameraLabel}   [{index + 1} / {availableCameras.Length}]");

            if (!sidebarHidden)
            {
                SafeSetActive(survivorSidebar, true);
                PopulateFullSurvivorSidebar();
            }
        }

        private void CycleCameraNext()
        {
            if (availableCameras == null || availableCameras.Length == 0) return;
            bool sidebar = survivorSidebar != null && survivorSidebar.activeSelf;
            ShowCamera((currentCameraIndex + 1) % availableCameras.Length, !sidebar);
        }

        private void CycleCameraPrev()
        {
            if (availableCameras == null || availableCameras.Length == 0) return;
            bool sidebar = survivorSidebar != null && survivorSidebar.activeSelf;
            int prev = (currentCameraIndex - 1 + availableCameras.Length) % availableCameras.Length;
            ShowCamera(prev, !sidebar);
        }

        /// <summary>Opens mission map (called by MissionMapProp or bottom bar).</summary>
        public void OpenMissionMap()
        {
            // Mission system removed — no-op kept for scene reference compatibility.
        }

        // ── Survivor sidebar ──────────────────────────────────────────────────────

        /// <summary>Populates the sidebar with all survivors regardless of room.</summary>
        private void PopulateFullSurvivorSidebar()
        {
            if (survivorNameListContainer == null) return;
            foreach (Transform child in survivorNameListContainer) Destroy(child.gameObject);

            foreach (SurvivorBehavior sb in gm.SurvivorManager.Survivors)
            {
                if (sb == null) continue;

                SurvivorBehavior captured = sb;
                GameObject btnGo = new GameObject($"SidebarBtn_{sb.SurvivorName}");
                btnGo.transform.SetParent(survivorNameListContainer, false);
                RectTransform brt = btnGo.AddComponent<RectTransform>();
                brt.sizeDelta = new Vector2(0, 26);
                Image bg = btnGo.AddComponent<Image>();
                bg.color = !sb.IsAlive    ? new Color(0.25f, 0.05f, 0.05f, 0.9f) :
                            sb.IsOnMission ? new Color(0.12f, 0.12f, 0.05f, 0.9f) :
                                             new Color(0.08f, 0.14f, 0.08f, 0.9f);
                Button btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = bg;
                if (sb.IsAlive && !sb.IsOnMission)
                    btn.onClick.AddListener(() => { gm.SurvivorManager.SelectSurvivor(captured); HighlightSidebarButton(btnGo); });

                GameObject lbl = new GameObject("Label");
                lbl.transform.SetParent(btnGo.transform, false);
                RectTransform lrt = lbl.AddComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
                TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
                string status = !sb.IsAlive    ? " [MORT]"    :
                                 sb.IsOnMission ? " [MISSION]" :
                                 sb.IsSick      ? " [MALADE]"  :
                                 sb.IsArrested  ? " [ARRÊTÉ]"  : "";
                tmp.text      = sb.SurvivorName.ToUpper() + status;
                tmp.fontSize  = 10;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color     = !sb.IsAlive    ? new Color(0.5f, 0.3f, 0.3f) :
                                 sb.IsOnMission ? new Color(0.8f, 0.8f, 0.3f) :
                                                  new Color(0.6f, 1f, 0.6f);
            }
        }

        private void HighlightSidebarButton(GameObject selected)
        {
            if (survivorNameListContainer == null) return;
            foreach (Transform child in survivorNameListContainer)
            {
                Image img = child.GetComponent<Image>();
                if (img != null)
                    img.color = child.gameObject == selected
                        ? new Color(0.2f, 0.5f, 0.2f, 1f)
                        : new Color(0.08f, 0.14f, 0.08f, 0.9f);
            }
        }

        // ── Mission team list — REMOVED ───────────────────────────────────────────

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

            // Restore FPS cursor state
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            OfficeInteractionSystem interact = FindFirstObjectByType<OfficeInteractionSystem>();
            interact?.SetFPSLocked(false);
            FindFirstObjectByType<ComputerTerminalProp>()?.NotifyTerminalClosed();
        }

        // ── Dossier — REMOVED ────────────────────────────────────────────────────

        // ── Status bar ───────────────────────────────────────────────────────────

        private void RefreshStatusBar()
        {
            if (gm == null) return;
            ShelterResourceManager rm = gm.ResourceManager;
            // Food and Water are float — display as integer for readability
            SetText(foodText,      $"NOURR. {rm.FoodInt}");
            SetText(waterText,     $"EAU {rm.WaterInt}");
            SetText(medicineText,  $"MED. {rm.Medicine}");
            SetText(materialsText, $"MAT. {rm.Materials}");
            SetText(energyText,    $"ENRG. {rm.Energy}%");
            SetText(dayText,       $"JOUR {gm.DayManager.CurrentDay}");
            SetText(populationText,$"POP. {gm.SurvivorManager.AliveSurvivorCount}");
        }

        // ── Camera wall ──────────────────────────────────────────────────────────

        /// <summary>
        /// <summary>Stub kept for internal compatibility — use ShowCamera() for room-specific view.</summary>
        private void OpenRoomView(ShelterRoomType room)
        {
            // Navigate to any camera — room type no longer used for filtering
            if (availableCameras != null && availableCameras.Length > 0)
                ShowCamera(0);
        }

        private void OpenFullScreenCamera(ShelterRoomType room) => OpenRoomView(room);

        private static string RoomLabel(ShelterRoomType r) => r switch
        {
            ShelterRoomType.Dorm     => "CAM-01  DORTOIR",
            ShelterRoomType.Hospital => "CAM-02  HÔPITAL",
            ShelterRoomType.Factory  => "CAM-03  USINE",
            ShelterRoomType.Restroom => "CAM-04  SALLE DE REPOS",
            _ => "CAM-???"
        };

        // ── Utility ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Calls SetActive on a panel only if it is a live Unity object.
        /// The C# null-conditional ?. bypasses Unity's == overload — a destroyed
        /// GameObject is "fake-null" and still passes the ?. check, crashing on
        /// the native side. This helper uses Unity's == comparison instead.
        /// </summary>
        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private void HideAllPanels()
        {
            SafeSetActive(cameraWallPanel,      false);
            SafeSetActive(survivorSidebar,      false);
            SafeSetActive(radioPanel,           false);
            SafeSetActive(gameOverPanel,        false);
            SafeSetActive(gameWonPanel,         false);
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
