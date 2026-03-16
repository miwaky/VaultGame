using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Self-contained camera wall panel living inside the ComputerUI.
    ///
    /// Features:
    ///   - Single camera feed with RawImage viewport
    ///   - Prev/Next navigation
    ///   - Zoom via mouse scroll wheel (handled by SecurityCameraController)
    ///   - NPC tooltip: raycasts from the surveillance camera when the cursor
    ///     hovers the viewport and shows the survivor's name near the cursor.
    ///
    /// Expected hierarchy (wired via Inspector):
    ///   CameraWallPanelUI
    ///     TitleBar / CloseBtn
    ///     CameraViewport      ← RawImage
    ///     CameraName          ← TMP overlay
    ///     BottomBar / PrevBtn, NextBtn
    ///     NPCTooltip          ← toggled active/inactive
    ///       Text              ← TMP label
    /// </summary>
    public class CameraWallPanelUI : MonoBehaviour
    {
        // ── Inspector wiring ─────────────────────────────────────────────────────

        [Header("UI References")]
        [Tooltip("Parent transform of the dynamically generated nav buttons.")]
        [SerializeField] private Transform navRow;

        [Tooltip("RawImage that displays the current camera's RenderTexture.")]
        [SerializeField] private RawImage cameraViewport;

        [Tooltip("TextMeshProUGUI showing the active camera's label.")]
        [SerializeField] private TextMeshProUGUI cameraNameText;

        [Tooltip("Button that closes this panel and returns to the ComputerUI main menu.")]
        [SerializeField] private Button closeButton;

        [Tooltip("Button cycling to the previous camera.")]
        [SerializeField] private Button prevButton;

        [Tooltip("Button cycling to the next camera.")]
        [SerializeField] private Button nextButton;

        [Header("NPC Tooltip")]
        [Tooltip("Root GameObject of the tooltip (toggled active/inactive).")]
        [SerializeField] private GameObject npcTooltip;

        [Tooltip("TextMeshProUGUI inside NPCTooltip that displays the survivor name.")]
        [SerializeField] private TextMeshProUGUI npcTooltipText;

        [Tooltip("RectTransform of the tooltip root — used to position it near the cursor.")]
        [SerializeField] private RectTransform npcTooltipRect;

        [Header("Raycast")]
        [Tooltip("Layer mask for the NPC raycast. Leave as Everything if unsure.")]
        [SerializeField] private LayerMask npcRaycastMask = Physics.DefaultRaycastLayers;

        [Tooltip("Maximum raycast distance (metres) from the surveillance camera.")]
        [SerializeField] private float npcRaycastDistance = 200f;

        [Header("Nav Button Style")]
        [SerializeField] private Color navButtonNormal    = new Color(0.08f, 0.14f, 0.08f, 1f);
        [SerializeField] private Color navButtonSelected  = new Color(0.15f, 0.40f, 0.15f, 1f);
        [SerializeField] private Color navButtonTextColor = new Color(0.70f, 1.00f, 0.70f, 1f);

        // ── Runtime state ────────────────────────────────────────────────────────

        private readonly List<SecurityCameraController> cameras    = new();
        private readonly List<Button>                   navButtons = new();
        private int                                     activeIndex = -1;
        private ComputerMenuController                  menuController;
        private Canvas                                  rootCanvas;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Currently active controller, or null.</summary>
        public SecurityCameraController ActiveController =>
            (activeIndex >= 0 && activeIndex < cameras.Count) ? cameras[activeIndex] : null;

        /// <summary>Opens the panel with the given camera controllers.</summary>
        public void Open(IEnumerable<SecurityCameraController> cameraControllers, ComputerMenuController menu)
        {
            menuController = menu;
            cameras.Clear();
            cameras.AddRange(cameraControllers);

            for (int i = 0; i < cameras.Count; i++)
            {
                SecurityCamera sc = cameras[i].SecurityCamera
                                    ?? cameras[i].GetComponentInChildren<SecurityCamera>();
                if (sc != null && sc.CameraLabel == "CAM-XX")
                    sc.CameraLabel = $"CAM-{i + 1:D2}";
            }

            BuildNavRow();

            if (cameras.Count > 0)
                SetActiveCamera(0);
            else
                ShowNoCameraMessage();
        }

        /// <summary>Deactivates the current controller and hides the panel.</summary>
        public void Close()
        {
            DeactivateCurrentCamera();
            HideTooltip();
            gameObject.SetActive(false);
        }

        /// <summary>Switches to the camera at the given index.</summary>
        public void SetActiveCamera(int index)
        {
            if (index < 0 || index >= cameras.Count) return;

            DeactivateCurrentCamera();
            activeIndex = index;

            SecurityCameraController ctrl = cameras[activeIndex];
            ctrl.SetActive(true);

            SecurityCamera secCam = ctrl.SecurityCamera
                                    ?? ctrl.GetComponentInChildren<SecurityCamera>();

            if (cameraViewport != null && secCam?.RenderTexture != null)
                cameraViewport.texture = secCam.RenderTexture;

            string label = secCam != null ? secCam.CameraLabel : $"CAM-{activeIndex + 1:D2}";
            SetText(cameraNameText, label);
            RefreshNavHighlight();
        }

        /// <summary>Cycles to the previous camera (wraps around).</summary>
        public void CyclePrev()
        {
            if (cameras.Count == 0) return;
            SetActiveCamera((activeIndex - 1 + cameras.Count) % cameras.Count);
        }

        /// <summary>Cycles to the next camera (wraps around).</summary>
        public void CycleNext()
        {
            if (cameras.Count == 0) return;
            SetActiveCamera((activeIndex + 1) % cameras.Count);
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            closeButton?.onClick.AddListener(OnCloseClicked);
            prevButton?.onClick.AddListener(CyclePrev);
            nextButton?.onClick.AddListener(CycleNext);
            rootCanvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            if (transform.localScale == Vector3.zero)
                transform.localScale = Vector3.one;
        }

        private void Update()
        {
            HandleTooltip();
            HandleKeyboardCycling();
        }

        // ── Keyboard cycling ──────────────────────────────────────────────────────

        private void HandleKeyboardCycling()
        {
            if (cameras.Count == 0) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
                CycleNext();
            else if (Keyboard.current.aKey.wasPressedThisFrame)
                CyclePrev();
        }

        // ── Tooltip / Raycast ─────────────────────────────────────────────────────

        private void HandleTooltip()
        {
            if (npcTooltip == null || cameraViewport == null) return;

            Camera surCam = ActiveController?.SurveillanceCamera;
            if (surCam == null) { HideTooltip(); return; }

            Vector2 mouseScreen = Input.mousePosition;

            // Determine UI camera (null for ScreenSpaceOverlay)
            Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                           ? rootCanvas.worldCamera : null;

            // Only process hover when cursor is inside the viewport
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    cameraViewport.rectTransform, mouseScreen, uiCam))
            {
                HideTooltip();
                return;
            }

            // Convert screen position → local UV within the RawImage rect
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cameraViewport.rectTransform, mouseScreen, uiCam, out Vector2 localPoint);

            Vector2 uv = Rect.PointToNormalized(cameraViewport.rectTransform.rect, localPoint);

            // Respect the RawImage's uvRect (handles flipped/offset textures)
            Rect uvRect = cameraViewport.uvRect;
            Vector2 viewportPoint = new Vector2(
                uvRect.x + uv.x * uvRect.width,
                uvRect.y + uv.y * uvRect.height);

            // Raycast from the surveillance camera through the hovered viewport pixel
            Ray ray = surCam.ViewportPointToRay(new Vector3(viewportPoint.x, viewportPoint.y, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, npcRaycastDistance, npcRaycastMask))
            {
                SurvivorBehavior survivor = hit.collider.GetComponentInParent<SurvivorBehavior>();
                if (survivor != null && survivor.IsAlive)
                {
                    ShowTooltip(survivor.SurvivorName, OrderLabel(survivor.CurrentOrder), mouseScreen, uiCam);
                    return;
                }
            }

            HideTooltip();
        }

        private void ShowTooltip(string survivorName, string task, Vector2 screenPos, Camera uiCam)
        {
            if (npcTooltip == null) return;
            npcTooltip.SetActive(true);
            SetText(npcTooltipText, $"{survivorName}\n<size=9><color=#aaffaa>{task}</color></size>");

            if (npcTooltipRect != null)
            {
                // Use this panel's own RectTransform as reference so anchoredPosition
                // and the converted local point share the same coordinate space.
                RectTransform panelRT = GetComponent<RectTransform>();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        panelRT, screenPos, uiCam, out Vector2 localPos))
                {
                    // Place tooltip 6px right and 6px below the cursor tip
                    npcTooltipRect.anchoredPosition = localPos + new Vector2(6f, -6f);
                }
            }
        }

        /// <summary>Returns a human-readable label for the given order.</summary>
        private static string OrderLabel(OrderType order) => order switch
        {
            OrderType.RepairGenerator    => "Répare le générateur",
            OrderType.TransportResources => "Transporte des ressources",
            OrderType.CraftTools         => "Fabrique des outils",
            OrderType.GoEat              => "Mange",
            OrderType.GoSleep            => "Dort",
            OrderType.GoToInfirmary      => "À l'infirmerie",
            OrderType.ArrestSurvivor     => "Arrête un survivant",
            OrderType.PatrolZone         => "Patrouille",
            _                            => order.ToString()
        };

        private void HideTooltip()
        {
            if (npcTooltip != null && npcTooltip.activeSelf)
                npcTooltip.SetActive(false);
        }

        // ── Nav buttons ───────────────────────────────────────────────────────────

        private void BuildNavRow()
        {
            if (navRow == null) return;

            foreach (Button b in navButtons)
                if (b != null) Destroy(b.gameObject);
            navButtons.Clear();

            for (int i = 0; i < cameras.Count; i++)
            {
                int capturedIndex = i;
                SecurityCamera sc = cameras[i].SecurityCamera
                                    ?? cameras[i].GetComponentInChildren<SecurityCamera>();
                string label = sc != null ? sc.CameraLabel : $"CAM-{i + 1:D2}";

                Button btn = CreateNavButton(label);
                btn.onClick.AddListener(() => SetActiveCamera(capturedIndex));
                btn.transform.SetParent(navRow, false);
                navButtons.Add(btn);
            }
        }

        private Button CreateNavButton(string label)
        {
            GameObject go     = new GameObject(label);
            go.AddComponent<RectTransform>();

            Image img         = go.AddComponent<Image>();
            img.color         = navButtonNormal;

            Button btn        = go.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb       = btn.colors;
            cb.normalColor      = navButtonNormal;
            cb.highlightedColor = Color.Lerp(navButtonNormal, Color.white, 0.15f);
            cb.selectedColor    = navButtonSelected;
            btn.colors          = cb;

            LayoutElement le   = go.AddComponent<LayoutElement>();
            le.preferredWidth  = 140f;
            le.preferredHeight = 32f;

            GameObject lblGo   = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            RectTransform lrt  = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin      = Vector2.zero;
            lrt.anchorMax      = Vector2.one;
            lrt.sizeDelta      = Vector2.zero;

            TextMeshProUGUI tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 11f;
            tmp.color     = navButtonTextColor;

            return btn;
        }

        private void RefreshNavHighlight()
        {
            for (int i = 0; i < navButtons.Count; i++)
            {
                if (navButtons[i] == null) continue;
                Image img = navButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == activeIndex) ? navButtonSelected : navButtonNormal;
            }
        }

        // ── Utility ───────────────────────────────────────────────────────────────

        private void DeactivateCurrentCamera()
        {
            if (activeIndex >= 0 && activeIndex < cameras.Count)
                cameras[activeIndex]?.SetActive(false);
        }

        private void ClearViewport()
        {
            if (cameraViewport != null) cameraViewport.texture = null;
            SetText(cameraNameText, "---");
        }

        private void ShowNoCameraMessage()
        {
            ClearViewport();
            SetText(cameraNameText, "AUCUNE CAMÉRA DISPONIBLE");
        }

        private void OnCloseClicked()
        {
            Close();
            menuController?.ShowMainMenu();
        }

        private static void SetText(TextMeshProUGUI t, string s)
        {
            if (t != null) t.text = s;
        }
    }
}
