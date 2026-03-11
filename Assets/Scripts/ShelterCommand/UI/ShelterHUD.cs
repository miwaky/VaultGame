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

        // ── Order Panel ─────────────────────────────────────────────────────────
        [Header("Order Panel")]
        [SerializeField] private GameObject orderPanel;
        [SerializeField] private TextMeshProUGUI selectedSurvivorNameText;
        [SerializeField] private TextMeshProUGUI selectedSurvivorStatsText;
        [SerializeField] private Button repairGeneratorButton;
        [SerializeField] private Button transportResourcesButton;
        [SerializeField] private Button craftToolsButton;
        [SerializeField] private Button goEatButton;
        [SerializeField] private Button goSleepButton;
        [SerializeField] private Button goInfirmaryButton;
        [SerializeField] private Button arrestSurvivorButton;
        [SerializeField] private Button patrolZoneButton;
        [SerializeField] private Button cancelOrderButton;

        // ── Mission Map ─────────────────────────────────────────────────────────
        // (removed)

        // ── Survivor Dossier ────────────────────────────────────────────────────
        [Header("Survivor Dossier")]
        [SerializeField] private GameObject survivorDossierPanel;
        [SerializeField] private Transform survivorListContainer;
        [SerializeField] private GameObject survivorEntryPrefab;
        [SerializeField] private Button closeDossierButton;

        // ── Radio Panel ─────────────────────────────────────────────────────────
        [Header("Radio Panel")]
        [SerializeField] private GameObject radioPanel;
        [SerializeField] private TextMeshProUGUI radioMessageText;
        [SerializeField] private Button closeRadioButton;

        // ── Event Popup ─────────────────────────────────────────────────────────
        [Header("Event Popup")]
        [SerializeField] private GameObject eventPopupPanel;
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescriptionText;
        [SerializeField] private Button eventChoice0Button;
        [SerializeField] private Button eventChoice1Button;
        [SerializeField] private TextMeshProUGUI eventChoice0Label;
        [SerializeField] private TextMeshProUGUI eventChoice1Label;

        // ── Mission Result ──────────────────────────────────────────────────────
        // (removed)

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
        private ShelterEvent pendingEvent;
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
                CloseAllAndReturnToFPS();
        }

        // ── Event subscriptions ─────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            gm.ResourceManager.OnResourcesChanged += RefreshStatusBar;
            gm.SurvivorManager.OnPopulationChanged += RefreshStatusBar;
            gm.SurvivorManager.OnSurvivorSelected += RefreshOrderPanel;
            gm.SurvivorManager.OnSurvivorDied += s => ShowNotification($"{s.SurvivorName} est mort.");
            gm.DayManager.OnDayStarted += d => { RefreshStatusBar(); ShowNotification($"Jour {d} — L'abri s'eveille."); };
            gm.DayManager.OnGameOver += () => { HideAllPanels(); SafeSetActive(gameOverPanel, true); };
            gm.DayManager.OnGameWon += () => { SafeSetActive(gameWonPanel, true); };
            gm.EventSystem.OnEventTriggered += ShowEventPopup;
            gm.CameraRoomController.OnSurvivorClickedInCamera += s =>
            {
                gm.SurvivorManager.SelectSurvivor(s);
                SafeSetActive(orderPanel, true);
            };
        }

        // ── Button setup ────────────────────────────────────────────────────────

        private void SetupButtons()
        {
            closeCameraWallButton?.onClick.AddListener(CloseAllAndReturnToFPS);
            openDossierButton?.onClick.AddListener(ShowDossier);

            camPrevButton?.onClick.AddListener(CycleCameraPrev);
            camNextButton?.onClick.AddListener(CycleCameraNext);

            repairGeneratorButton?.onClick.AddListener(() => IssueOrder(OrderType.RepairGenerator));
            transportResourcesButton?.onClick.AddListener(() => IssueOrder(OrderType.TransportResources));
            craftToolsButton?.onClick.AddListener(() => IssueOrder(OrderType.CraftTools));
            goEatButton?.onClick.AddListener(() => IssueOrder(OrderType.GoEat));
            goSleepButton?.onClick.AddListener(() => IssueOrder(OrderType.GoSleep));
            goInfirmaryButton?.onClick.AddListener(() => IssueOrder(OrderType.GoToInfirmary));
            arrestSurvivorButton?.onClick.AddListener(() => IssueOrder(OrderType.ArrestSurvivor));
            patrolZoneButton?.onClick.AddListener(() => IssueOrder(OrderType.PatrolZone));
            cancelOrderButton?.onClick.AddListener(() =>
            {
                gm.SurvivorManager.DeselectSurvivor();
                SafeSetActive(orderPanel, false);
            });

            closeDossierButton?.onClick.AddListener(() =>
            {
                SafeSetActive(survivorDossierPanel, false);
                SafeSetActive(cameraWallPanel, true);
            });

            closeRadioButton?.onClick.AddListener(CloseAllAndReturnToFPS);
            eventChoice0Button?.onClick.AddListener(() => ResolveEvent(0));
            eventChoice1Button?.onClick.AddListener(() => ResolveEvent(1));
        }

        private void BindRenderTextures() { /* No-op — textures bound dynamically per SecurityCamera */ }
        private void BindRT(RawImage img, ShelterRoomType room) { }

        // ── Public API — called by props ─────────────────────────────────────────

        /// <summary>
        /// Opens the camera terminal with the provided list of SecurityCameras.
        /// Shows the first camera immediately; sidebar hidden until camera is active.
        /// </summary>
        public void OpenCameraWall(SecurityCamera[] cameras)
        {
            HideAllPanels();
            availableCameras   = cameras ?? new SecurityCamera[0];
            currentCameraIndex = -1;

            SafeSetActive(cameraWallPanel,  true);
            SafeSetActive(crosshair,        false);
            SafeSetActive(survivorSidebar,  false);
            SafeSetActive(orderPanel,       false);

            if (availableCameras.Length > 0)
                ShowCamera(0);
        }

        /// <summary>Legacy overload — auto-discovers all SecurityCameras in the scene.</summary>
        public void OpenCameraWall()
        {
            OpenCameraWall(FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None));
        }

        // ── Camera cycling ────────────────────────────────────────────────────────

        private void ShowCamera(int index)
        {
            if (availableCameras == null || availableCameras.Length == 0) return;
            index = Mathf.Clamp(index, 0, availableCameras.Length - 1);
            currentCameraIndex = index;

            SecurityCamera cam = availableCameras[index];
            if (cameraFeedImage != null && cam.RenderTexture != null)
                cameraFeedImage.texture = cam.RenderTexture;
            SetText(cameraLabelText, $"{cam.CameraLabel}   [{index + 1} / {availableCameras.Length}]");

            SafeSetActive(survivorSidebar, true);
            PopulateFullSurvivorSidebar();
            SafeSetActive(orderPanel, false);
        }

        private void CycleCameraNext()
        {
            if (availableCameras == null || availableCameras.Length == 0) return;
            // Circular: wraps from last → first
            ShowCamera((currentCameraIndex + 1) % availableCameras.Length);
        }

        private void CycleCameraPrev()
        {
            if (availableCameras == null || availableCameras.Length == 0) return;
            // Circular: wraps from first → last
            int prev = (currentCameraIndex - 1 + availableCameras.Length) % availableCameras.Length;
            ShowCamera(prev);
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
            if (pendingEvent != null) { ShowNotification("Resolvez l'evenement avant de continuer."); return; }

            // Only deselect the room camera if one was actually selected (full-screen mode).
            // When coming from the PC terminal there is no active room — skipping avoids
            // accidentally disabling the full-screen camera (or the player camera if mis-assigned).
            if (gm.CameraRoomController.IsInFullScreen)
                gm.CameraRoomController.DeselectRoom();

            HideAllPanels();
            SafeSetActive(crosshair, true);
            OfficeInteractionSystem interact = FindFirstObjectByType<OfficeInteractionSystem>();
            interact?.SetFPSLocked(false);
            FindFirstObjectByType<ComputerTerminalProp>()?.NotifyTerminalClosed();
        }

        // ── Dossier ─────────────────────────────────────────────────────────────

        private void ShowDossier()
        {
            SafeSetActive(survivorDossierPanel, true);
            SafeSetActive(cameraWallPanel, false);
            PopulateDossier();
        }

        private void PopulateDossier()
        {
            if (survivorListContainer == null || survivorEntryPrefab == null)
            {
                Debug.LogWarning("[ShelterHUD] survivorListContainer ou survivorEntryPrefab non assigné.");
                return;
            }
            foreach (Transform child in survivorListContainer) Destroy(child.gameObject);
            foreach (SurvivorBehavior sb in gm.SurvivorManager.Survivors)
            {
                if (sb == null) continue;
                GameObject go = Instantiate(survivorEntryPrefab, survivorListContainer);
                go.SetActive(true);
                go.GetComponent<SurvivorEntryUI>()?.Bind(sb);
            }
        }

        private void ToggleMissionTeam(SurvivorBehavior sb, Button btn) { }          // kept for compat
        private static void UpdateMissionToggleColor(Button btn, bool selected) { }  // kept for compat

        // ── Status bar ───────────────────────────────────────────────────────────

        private void RefreshStatusBar()
        {
            if (gm == null) return;
            ShelterResourceManager rm = gm.ResourceManager;
            SetText(foodText, $"NOURR. {rm.Food}");
            SetText(waterText, $"EAU {rm.Water}");
            SetText(medicineText, $"MED. {rm.Medicine}");
            SetText(materialsText, $"MAT. {rm.Materials}");
            SetText(energyText, $"ENRG. {rm.Energy}%");
            SetText(dayText, $"JOUR {gm.DayManager.CurrentDay}");
            SetText(populationText, $"POP. {gm.SurvivorManager.AliveSurvivorCount}");
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

        // ── Order panel ──────────────────────────────────────────────────────────

        private void RefreshOrderPanel(SurvivorBehavior survivor)
        {
            if (survivor == null) { SafeSetActive(orderPanel, false); return; }
            SetText(selectedSurvivorNameText, survivor.SurvivorName.ToUpper());
            string s = $"Faim:{survivor.Hunger}  Fatigue:{survivor.Fatigue}  Stress:{survivor.Stress}  Moral:{survivor.Morale}";
            if (survivor.IsSick) s += "  [MALADE]";
            if (survivor.IsArrested) s += "  [ARRETE]";
            SetText(selectedSurvivorStatsText, s);
            SafeSetActive(orderPanel, true);
        }

        private void IssueOrder(OrderType order)
        {
            bool ok = gm.SurvivorManager.IssueOrderToSelected(order, gm.ResourceManager.Resources);
            ShowNotification(ok ? $"Ordre : {OrderLabel(order)}" : "Ordre refuse — moral trop bas.");
            SafeSetActive(orderPanel, false);
        }

        private static string OrderLabel(OrderType o) => o switch
        {
            OrderType.RepairGenerator => "Reparer generateur",
            OrderType.TransportResources => "Transporter ressources",
            OrderType.CraftTools => "Fabriquer outils",
            OrderType.GoEat => "Aller manger",
            OrderType.GoSleep => "Aller dormir",
            OrderType.GoToInfirmary => "Aller infirmerie",
            OrderType.ArrestSurvivor => "Arreter",
            OrderType.PatrolZone => "Patrouiller",
            _ => o.ToString()
        };

        // ── Missions — REMOVED ────────────────────────────────────────────────────

        // ── Events ───────────────────────────────────────────────────────────────

        private void ShowEventPopup(ShelterEvent ev)
        {
            pendingEvent = ev;
            SafeSetActive(eventPopupPanel, true);
            SetText(eventTitleText, $"! {ev.Title.ToUpper()}");
            SetText(eventDescriptionText, ev.Description);

            if (ev.Choices.Length > 0)
            {
                if (eventChoice0Button != null) SafeSetActive(eventChoice0Button.gameObject, true);
                SetText(eventChoice0Label, $"{ev.Choices[0].Label}\n<size=9><color=#88cc88>{ev.Choices[0].Tooltip}</color></size>");
            }
            if (ev.Choices.Length > 1)
            {
                if (eventChoice1Button != null) SafeSetActive(eventChoice1Button.gameObject, true);
                SetText(eventChoice1Label, $"{ev.Choices[1].Label}\n<size=9><color=#88cc88>{ev.Choices[1].Tooltip}</color></size>");
            }

            // Release cursor so player can click
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            OfficeInteractionSystem interact = FindFirstObjectByType<OfficeInteractionSystem>();
            interact?.SetFPSLocked(true);
            SafeSetActive(crosshair, false);
        }

        private void ResolveEvent(int choice)
        {
            if (pendingEvent == null) return;
            gm.EventSystem.ResolveEvent(pendingEvent, choice, gm.SurvivorManager, gm.ResourceManager);
            ShowNotification($"Decision : {pendingEvent.Choices[choice].Label}");
            pendingEvent = null;
            SafeSetActive(eventPopupPanel, false);

            // Restore FPS — cursor was released by ShowEventPopup
            OfficeInteractionSystem interact = FindFirstObjectByType<OfficeInteractionSystem>();
            interact?.SetFPSLocked(false);
            SafeSetActive(crosshair, true);
        }

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
            SafeSetActive(orderPanel,           false);
            SafeSetActive(survivorDossierPanel, false);
            SafeSetActive(radioPanel,           false);
            SafeSetActive(eventPopupPanel,      false);
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
