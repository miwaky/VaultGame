using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Builds the supervisor's room: office environment, player, interactive props,
    /// surveillance HUD canvas, and all their wiring.
    /// Can be called standalone via menu or by ShelterCommandSceneBuilder.BuildScene().
    /// </summary>
    internal static class ShelterSupervisorRoomBuilder
    {
        // ── Shared material references (set externally by the main builder) ────────
        internal static Material WallMat;
        internal static Material FloorMat;
        internal static Material CeilingMat;
        internal static Material DarkMetalMat;

        private const float RoomSize   = 12f;
        private const float WallHeight = 3f;
        private const float OfficeZ    = 0f;

        // ── Standalone menu entry ────────────────────────────────────────────────

        [UnityEditor.MenuItem("Tools/Shelter Command/Build Supervisor Room")]
        public static void BuildStandalone()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Salle du Superviseur",
                "Créer la salle du superviseur (bureau + joueur FPS + HUD) ?",
                "Oui", "Annuler")) return;

            EnsureMaterials();

            GameObject root = new GameObject("Bureau_Root");

            GameObject office = BuildOfficeRoom(root);
            BuildPlayer(office);

            // Pas de RenderTextures en mode standalone — le HUD est créé sans feeds
            BuildHUDCanvas(root, null, null, null, null);

            UnityEditor.Selection.activeGameObject = root;
            Debug.Log("[ShelterSupervisorRoomBuilder] Salle du superviseur créée de façon indépendante.");
        }

        /// <summary>Loads or creates fallback materials when called standalone.</summary>
        private static void EnsureMaterials()
        {
            if (WallMat == null)
                WallMat = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.22f, 0.2f) };
            if (FloorMat == null)
                FloorMat = new Material(Shader.Find("Standard")) { color = new Color(0.15f, 0.15f, 0.15f) };
            if (CeilingMat == null)
                CeilingMat = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.12f, 0.12f) };
            if (DarkMetalMat == null)
                DarkMetalMat = new Material(Shader.Find("Standard")) { color = new Color(0.18f, 0.18f, 0.2f) };
        }

        // ── Office room ──────────────────────────────────────────────────────────

        internal static GameObject BuildOfficeRoom(GameObject parent)
        {
            GameObject room = new GameObject("Bureau");
            room.transform.SetParent(parent.transform);
            room.transform.localPosition = new Vector3(0, 0, OfficeZ);

            BuildBox(room, RoomSize, WallHeight, RoomSize, FloorMat, WallMat, CeilingMat);

            // Monitor wall prop (visual only — actual UI is Canvas)
            BuildPrimitive(PrimitiveType.Cube, "MonitorWall_Prop", room,
                new Vector3(0, 1.5f, -RoomSize * 0.5f + 0.2f),
                new Vector3(8f, 2.5f, 0.1f), DarkMetalMat);

            // Desk
            BuildPrimitive(PrimitiveType.Cube, "Desk", room,
                new Vector3(0, 0.4f, -RoomSize * 0.5f + 1.5f),
                new Vector3(4f, 0.8f, 1.2f), DarkMetalMat);

            return room;
        }

        // ── Player ───────────────────────────────────────────────────────────────

        internal static GameObject BuildPlayer(GameObject office)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(office.transform);
            player.transform.localPosition = new Vector3(0, 0f, -3f);
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);

            ShelterFPSController fps = player.AddComponent<ShelterFPSController>();

            // Head / Camera
            GameObject head = new GameObject("Head");
            head.transform.SetParent(player.transform);
            head.transform.localPosition = new Vector3(0, 1.6f, 0);
            head.transform.localRotation = Quaternion.identity;

            Camera cam = head.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.tag = "MainCamera";

            SerializedObject fpsSO = new SerializedObject(fps);
            fpsSO.FindProperty("cameraTransform").objectReferenceValue = head.transform;
            fpsSO.ApplyModifiedProperties();

            OfficeInteractionSystem interact = head.AddComponent<OfficeInteractionSystem>();
            SerializedObject interactSO = new SerializedObject(interact);
            interactSO.FindProperty("playerCamera").objectReferenceValue = cam;
            interactSO.FindProperty("fpsController").objectReferenceValue = fps;
            interactSO.FindProperty("interactionMask").intValue = ~0;
            interactSO.ApplyModifiedProperties();

            // Props on the desk
            BuildComputerProp(office);
            BuildRadioProp(office);
            BuildMapProp(office);
            BuildBedProp(office);

            // CRT post-process on the main camera
            AddCRTEffect(cam);
            head.AddComponent<PSXPostProcess>();

            return player;
        }

        // ── Props ────────────────────────────────────────────────────────────────

        private static void BuildComputerProp(GameObject office)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ComputerTerminal";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(0f, 1.0f, -RoomSize * 0.5f + 0.35f);
            go.transform.localScale = new Vector3(1.4f, 0.9f, 0.15f);
            go.GetComponent<Renderer>().material =
                new Material(Shader.Find("Standard")) { color = new Color(0.05f, 0.35f, 0.1f) };
            go.AddComponent<ComputerTerminalProp>();
        }

        private static void BuildRadioProp(GameObject office)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Radio";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(-2.2f, 0.85f, -RoomSize * 0.5f + 1.5f);
            go.transform.localScale = new Vector3(0.5f, 0.3f, 0.4f);
            go.GetComponent<Renderer>().material =
                new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.2f, 0.1f) };
            go.AddComponent<RadioProp>();
        }

        private static void BuildMapProp(GameObject office)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "WorldMap";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(RoomSize * 0.5f - 0.1f, 1.5f, 0f);
            go.transform.localScale = new Vector3(0.05f, 1.8f, 2.8f);
            go.GetComponent<Renderer>().material =
                new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.2f, 0.35f) };
            go.AddComponent<MissionMapProp>();
        }

        private static void BuildBedProp(GameObject office)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Bed";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(-RoomSize * 0.5f + 1.5f, 0.3f, 2f);
            go.transform.localScale = new Vector3(1.2f, 0.5f, 2.5f);
            go.GetComponent<Renderer>().material =
                new Material(Shader.Find("Standard")) { color = new Color(0.3f, 0.25f, 0.2f) };
            go.AddComponent<BedProp>();
        }

        // ── HUD Canvas ────────────────────────────────────────────────────────────

        internal static void BuildHUDCanvas(GameObject systems,
            RenderTexture dormRT, RenderTexture cafetRT,
            RenderTexture storageRT, RenderTexture entRT)
        {
            GameObject canvasGo = new GameObject("HUD_Canvas");
            canvasGo.transform.SetParent(systems.transform);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Resource bar
            GameObject resourceBar = CreatePanel(canvasGo, "ResourceBar",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 40), new Vector2(0, -20));
            resourceBar.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.05f, 0.92f);
            SetupResourceBarTexts(resourceBar);

            // Camera wall panel
            GameObject cameraWall = CreatePanel(canvasGo, "CameraWallPanel",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            cameraWall.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            SetupCameraWall(cameraWall, dormRT, cafetRT, storageRT, entRT);
            cameraWall.SetActive(false);

            // Full screen camera overlay
            GameObject fullScreenPanel = CreatePanel(canvasGo, "FullScreenCameraPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fullScreenPanel.GetComponent<Image>().color = Color.black;
            SetupFullScreenPanel(fullScreenPanel);
            fullScreenPanel.SetActive(false);

            // Order panel
            GameObject orderPanel = CreatePanel(canvasGo, "OrderPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(300, 340), new Vector2(0, 185));
            orderPanel.GetComponent<Image>().color = new Color(0.03f, 0.1f, 0.05f, 0.95f);
            SetupOrderPanel(orderPanel);
            orderPanel.SetActive(false);

            // Event popup
            GameObject eventPopup = CreatePanel(canvasGo, "EventPopupPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480, 280), new Vector2(0, 0));
            eventPopup.GetComponent<Image>().color = new Color(0.1f, 0.02f, 0.02f, 0.97f);
            SetupEventPopup(eventPopup);
            eventPopup.SetActive(false);

            // Mission map panel
            GameObject missionMap = CreatePanel(canvasGo, "MissionMapPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500, 360), new Vector2(0, 0));
            missionMap.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.1f, 0.97f);
            SetupMissionMap(missionMap);
            missionMap.SetActive(false);

            // Survivor dossier panel
            GameObject dossierPanel = CreatePanel(canvasGo, "SurvivorDossierPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 400), new Vector2(0, 0));
            dossierPanel.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.97f);
            GameObject survivorEntryPrefab = BuildSurvivorEntryPrefab();
            survivorEntryPrefab.transform.SetParent(canvasGo.transform, false);
            SetupDossierPanel(dossierPanel);
            dossierPanel.SetActive(false);

            // Game over panel
            GameObject gameOverPanel = CreatePanel(canvasGo, "GameOverPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            gameOverPanel.GetComponent<Image>().color = new Color(0.4f, 0f, 0f, 0.85f);
            CreateLabel(gameOverPanel, "GameOverText", "L'ABRI EST TOMBÉ",
                Vector2.zero, new Vector2(0, 50));
            gameOverPanel.SetActive(false);

            // Game won panel
            GameObject gameWonPanel = CreatePanel(canvasGo, "GameWonPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            gameWonPanel.GetComponent<Image>().color = new Color(0f, 0.3f, 0.05f, 0.85f);
            CreateLabel(gameWonPanel, "GameWonText", "L'ABRI A SURVÉCU — VICTOIRE",
                Vector2.zero, new Vector2(0, 50));
            gameWonPanel.SetActive(false);

            // Mission result panel
            GameObject missionResultPanel = CreatePanel(canvasGo, "MissionResultPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 200), new Vector2(0, 100));
            missionResultPanel.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.02f, 0.97f);
            TextMeshProUGUI missionResultText = CreateLabel(missionResultPanel, "MissionResultText", "",
                Vector2.zero, new Vector2(0, 30));
            Button closeMissionResult = CreateButton(missionResultPanel, "CloseMissionResult", "FERMER",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120, 35), new Vector2(0, 15));
            missionResultPanel.SetActive(false);

            // Radio panel
            GameObject radioPanel = CreatePanel(canvasGo, "RadioPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 220), new Vector2(0, 0));
            radioPanel.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.02f, 0.97f);
            CreateLabel(radioPanel, "RadioTitle", "[ TRANSMISSION RADIO ]",
                new Vector2(0.5f, 1f), new Vector2(0f, -25f), 13f);
            TextMeshProUGUI radioMsgText = CreateLabel(radioPanel, "RadioMessage", "...",
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), 12f);
            radioMsgText.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 80);
            radioMsgText.textWrappingMode   = TMPro.TextWrappingModes.Normal;
            Button closeRadioBtn = CreateButton(radioPanel, "CloseRadioBtn", "FERMER",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120, 34), new Vector2(0, 20));
            radioPanel.SetActive(false);

            // Notification bar
            GameObject notifGo = new GameObject("Notification");
            notifGo.transform.SetParent(canvasGo.transform, false);
            RectTransform notifRect = notifGo.AddComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0, 0.5f);
            notifRect.anchorMax = new Vector2(1, 0.5f);
            notifRect.sizeDelta = new Vector2(0, 36);
            notifRect.anchoredPosition = new Vector2(0, -120);
            TextMeshProUGUI notifText = notifGo.AddComponent<TextMeshProUGUI>();
            notifText.alignment = TextAlignmentOptions.Center;
            notifText.fontSize = 14;
            notifText.color = new Color(0.8f, 1f, 0.6f);
            notifGo.SetActive(false);

            // Crosshair
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvasGo.transform, false);
            RectTransform chRect = crosshair.AddComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.5f, 0.5f);
            chRect.anchorMax = new Vector2(0.5f, 0.5f);
            chRect.sizeDelta = new Vector2(8, 8);
            chRect.anchoredPosition = Vector2.zero;
            crosshair.AddComponent<Image>().color = new Color(0.7f, 1f, 0.7f, 0.9f);

            // Interaction prompt
            GameObject promptRoot = new GameObject("InteractionPrompt");
            promptRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform prRect = promptRoot.AddComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0.5f, 0.5f);
            prRect.anchorMax = new Vector2(0.5f, 0.5f);
            prRect.sizeDelta = new Vector2(320, 36);
            prRect.anchoredPosition = new Vector2(0, -80);
            promptRoot.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            GameObject promptTextGo = new GameObject("PromptText");
            promptTextGo.transform.SetParent(promptRoot.transform, false);
            RectTransform ptRect = promptTextGo.AddComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero; ptRect.anchorMax = Vector2.one; ptRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI promptTMP = promptTextGo.AddComponent<TextMeshProUGUI>();
            promptTMP.fontSize = 13;
            promptTMP.color = new Color(0.8f, 1f, 0.6f);
            promptTMP.alignment = TextAlignmentOptions.Center;
            promptRoot.SetActive(false);

            // Wire HUD
            ShelterHUD hud = canvasGo.AddComponent<ShelterHUD>();
            WireHUD(hud, canvasGo, resourceBar, cameraWall, fullScreenPanel, orderPanel,
                eventPopup, missionMap, dossierPanel, survivorEntryPrefab,
                radioPanel, radioMsgText, closeRadioBtn,
                gameOverPanel, gameWonPanel,
                missionResultPanel, missionResultText, closeMissionResult,
                notifText, crosshair, promptRoot, promptTMP);

            // Wire interaction prompt into OfficeInteractionSystem
            OfficeInteractionSystem interact = Object.FindFirstObjectByType<OfficeInteractionSystem>();
            if (interact != null)
            {
                SerializedObject interactSO = new SerializedObject(interact);
                interactSO.FindProperty("promptRoot").objectReferenceValue = promptRoot;
                interactSO.FindProperty("promptText").objectReferenceValue = promptTMP;
                interactSO.ApplyModifiedProperties();
            }
        }

        // ── Camera wall setup ─────────────────────────────────────────────────────

        private static void SetupCameraWall(GameObject wall,
            RenderTexture dormRT, RenderTexture cafetRT,
            RenderTexture storageRT, RenderTexture entRT)
        {
            const float splitX  = 0.72f;
            const float rowBot  = 0f;
            const float rowBotH = 0.07f;
            const float rowMidH = 0.14f;
            const float feedBot = 0.14f;

            // Camera feed border
            GameObject feedBorder = new GameObject("CameraFeedBorder");
            feedBorder.transform.SetParent(wall.transform, false);
            RectTransform fbrt = feedBorder.AddComponent<RectTransform>();
            fbrt.anchorMin = new Vector2(0f, feedBot);
            fbrt.anchorMax = new Vector2(splitX, 1f);
            fbrt.sizeDelta = Vector2.zero;
            feedBorder.AddComponent<Image>().color = new Color(0.06f, 0.10f, 0.06f, 1f);

            // RawImage — receives SecurityCamera RenderTexture at runtime
            GameObject feedGo = new GameObject("CameraFeedImage");
            feedGo.transform.SetParent(feedBorder.transform, false);
            RectTransform firt = feedGo.AddComponent<RectTransform>();
            firt.anchorMin = new Vector2(0.005f, 0.01f);
            firt.anchorMax = new Vector2(0.995f, 0.99f);
            firt.sizeDelta = Vector2.zero;
            feedGo.AddComponent<RawImage>().color = new Color(0.85f, 1f, 0.85f);

            // Camera label
            GameObject lblGo = new GameObject("CameraLabelText");
            lblGo.transform.SetParent(feedBorder.transform, false);
            RectTransform lrt2 = lblGo.AddComponent<RectTransform>();
            lrt2.anchorMin = new Vector2(0f, 0.96f); lrt2.anchorMax = new Vector2(0.5f, 1f);
            lrt2.sizeDelta = Vector2.zero; lrt2.anchoredPosition = Vector2.zero;
            TextMeshProUGUI lblTMP = lblGo.AddComponent<TextMeshProUGUI>();
            lblTMP.text = "CAM-XX  [1/1]";
            lblTMP.alignment = TextAlignmentOptions.TopLeft;
            lblTMP.fontSize = 10;
            lblTMP.color = new Color(0.4f, 1f, 0.4f);
            lblTMP.margin = new Vector4(6, 4, 0, 0);

            // Action row: MISSIONS | DOSSIERS
            GameObject rowAction = new GameObject("ActionRow");
            rowAction.transform.SetParent(wall.transform, false);
            RectTransform rart = rowAction.AddComponent<RectTransform>();
            rart.anchorMin = new Vector2(0f, rowBotH);
            rart.anchorMax = new Vector2(splitX, rowMidH);
            rart.sizeDelta = Vector2.zero;
            rowAction.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.04f, 1f);
            HorizontalLayoutGroup rahlg = rowAction.AddComponent<HorizontalLayoutGroup>();
            rahlg.childControlWidth = true; rahlg.childControlHeight = true;
            rahlg.childForceExpandWidth = true; rahlg.childForceExpandHeight = true;
            rahlg.spacing = 2; rahlg.padding = new RectOffset(2, 2, 2, 2);
            CreateChildButton(rowAction, "OpenMissionMapBtn", "MISSIONS EXTÉRIEURES");
            CreateChildButton(rowAction, "OpenDossierBtn",    "DOSSIERS SURVIVANTS");

            // Nav row: PRÉC. | SUIV. | QUITTER
            GameObject rowNav = new GameObject("NavRow");
            rowNav.transform.SetParent(wall.transform, false);
            RectTransform rnrt = rowNav.AddComponent<RectTransform>();
            rnrt.anchorMin = new Vector2(0f, rowBot);
            rnrt.anchorMax = new Vector2(splitX, rowBotH);
            rnrt.sizeDelta = Vector2.zero;
            rowNav.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.03f, 1f);
            HorizontalLayoutGroup rnhlg = rowNav.AddComponent<HorizontalLayoutGroup>();
            rnhlg.childControlWidth = true; rnhlg.childControlHeight = true;
            rnhlg.childForceExpandWidth = false; rnhlg.childForceExpandHeight = true;
            rnhlg.spacing = 2; rnhlg.padding = new RectOffset(2, 2, 2, 2);
            CreateChildButton(rowNav, "CamPrevBtn",         "◄ PRÉC.",    fixedWidth: 110f);
            CreateChildButton(rowNav, "CamNextBtn",         "SUIV. ►",    fixedWidth: 110f);
            CreateFlexSpacer(rowNav);
            CreateChildButton(rowNav, "CloseCameraWallBtn", "✕ QUITTER",  fixedWidth: 120f);

            // Survivor sidebar (right 28%)
            GameObject sidebar = new GameObject("SurvivorSidebar");
            sidebar.transform.SetParent(wall.transform, false);
            RectTransform sbrt = sidebar.AddComponent<RectTransform>();
            sbrt.anchorMin = new Vector2(splitX + 0.01f, 0f);
            sbrt.anchorMax = Vector2.one;
            sbrt.sizeDelta = Vector2.zero;
            sidebar.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.04f, 0.97f);

            // "RÉSIDENTS" title
            GameObject sideTitle = new GameObject("SidebarTitle");
            sideTitle.transform.SetParent(sidebar.transform, false);
            RectTransform strt = sideTitle.AddComponent<RectTransform>();
            strt.anchorMin = new Vector2(0, 1); strt.anchorMax = new Vector2(1, 1);
            strt.sizeDelta = new Vector2(0, 28); strt.anchoredPosition = new Vector2(0, -14);
            TextMeshProUGUI stmp = sideTitle.AddComponent<TextMeshProUGUI>();
            stmp.text = "RÉSIDENTS"; stmp.alignment = TextAlignmentOptions.Center;
            stmp.fontSize = 11; stmp.color = new Color(0.5f, 1f, 0.5f);

            // Scrollable survivor name list
            GameObject nameList = new GameObject("SurvivorNameList");
            nameList.transform.SetParent(sidebar.transform, false);
            RectTransform nlrt = nameList.AddComponent<RectTransform>();
            nlrt.anchorMin = new Vector2(0, 0.38f); nlrt.anchorMax = new Vector2(1, 0.95f);
            nlrt.sizeDelta = Vector2.zero;
            nameList.AddComponent<Image>().color = new Color(0, 0, 0, 0.08f);
            VerticalLayoutGroup nlvlg = nameList.AddComponent<VerticalLayoutGroup>();
            nlvlg.padding = new RectOffset(3, 3, 3, 3); nlvlg.spacing = 2;
            nlvlg.childControlHeight = false; nlvlg.childControlWidth = true;
            nlvlg.childForceExpandHeight = false;

            // Order panel (bottom 38% of sidebar)
            GameObject sideOrder = new GameObject("SidebarOrderPanel");
            sideOrder.transform.SetParent(sidebar.transform, false);
            RectTransform sort = sideOrder.AddComponent<RectTransform>();
            sort.anchorMin = Vector2.zero; sort.anchorMax = new Vector2(1, 0.37f);
            sort.sizeDelta = Vector2.zero;
            sideOrder.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.03f, 0.98f);
            SetupSidebarOrderPanel(sideOrder);
        }

        // ── Panel setups ─────────────────────────────────────────────────────────

        private static void SetupResourceBarTexts(GameObject bar)
        {
            string[] ids      = { "FoodText", "WaterText", "MedicineText", "MaterialsText", "EnergyText", "DayText", "PopulationText" };
            string[] defaults = { "Nourriture: —", "Eau: —", "Médecine: —", "Matériaux: —", "Énergie: —%", "Jour —", "Pop: —" };
            float step = 1f / ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                GameObject go = new GameObject(ids[i]);
                go.transform.SetParent(bar.transform, false);
                RectTransform rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(i * step, 0);
                rt.anchorMax = new Vector2((i + 1) * step, 1);
                rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
                TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = defaults[i];
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 11;
                tmp.color = new Color(0.5f, 1f, 0.5f);
            }
        }

        private static void SetupSidebarOrderPanel(GameObject panel)
        {
            GameObject nameGo = new GameObject("SurvivorName");
            nameGo.transform.SetParent(panel.transform, false);
            RectTransform nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1);
            nrt.sizeDelta = new Vector2(0, 22); nrt.anchoredPosition = new Vector2(0, -11);
            TextMeshProUGUI ntmp = nameGo.AddComponent<TextMeshProUGUI>();
            ntmp.text = "— sélectionnez —"; ntmp.alignment = TextAlignmentOptions.Center;
            ntmp.fontSize = 10; ntmp.color = new Color(0.5f, 1f, 0.5f); ntmp.fontStyle = FontStyles.Bold;

            GameObject statsGo = new GameObject("SurvivorStats");
            statsGo.transform.SetParent(panel.transform, false);
            RectTransform srt2 = statsGo.AddComponent<RectTransform>();
            srt2.anchorMin = new Vector2(0, 1); srt2.anchorMax = new Vector2(1, 1);
            srt2.sizeDelta = new Vector2(0, 18); srt2.anchoredPosition = new Vector2(0, -30);
            TextMeshProUGUI stmp2 = statsGo.AddComponent<TextMeshProUGUI>();
            stmp2.text = ""; stmp2.alignment = TextAlignmentOptions.Center;
            stmp2.fontSize = 8; stmp2.color = new Color(0.4f, 0.8f, 0.4f);

            string[] btnNames  = { "RepairGeneratorBtn","TransportResourcesBtn","CraftToolsBtn","GoEatBtn","GoSleepBtn","GoInfirmaryBtn","ArrestBtn","PatrolBtn" };
            string[] btnLabels = { "Réparer","Transporter","Fabriquer","Manger","Dormir","Infirmerie","Arrêter","Patrouiller" };
            for (int i = 0; i < btnNames.Length; i++)
            {
                float col = (i % 2 == 0) ? 0.02f : 0.51f;
                float row = 1f - 0.18f - Mathf.Floor(i / 2f) * 0.20f;
                GameObject btn = new GameObject(btnNames[i]);
                btn.transform.SetParent(panel.transform, false);
                RectTransform brt = btn.AddComponent<RectTransform>();
                brt.anchorMin = new Vector2(col, row - 0.17f);
                brt.anchorMax = new Vector2(col + 0.47f, row);
                brt.sizeDelta = Vector2.zero;
                btn.AddComponent<Image>().color = new Color(0.1f, 0.2f, 0.1f, 0.9f);
                Button b = btn.AddComponent<Button>();
                b.targetGraphic = btn.GetComponent<Image>();
                GameObject lblGo = new GameObject("Label");
                lblGo.transform.SetParent(btn.transform, false);
                RectTransform llrt = lblGo.AddComponent<RectTransform>();
                llrt.anchorMin = Vector2.zero; llrt.anchorMax = Vector2.one; llrt.sizeDelta = Vector2.zero;
                TextMeshProUGUI ltmp = lblGo.AddComponent<TextMeshProUGUI>();
                ltmp.text = btnLabels[i]; ltmp.fontSize = 9;
                ltmp.alignment = TextAlignmentOptions.Center;
                ltmp.color = new Color(0.7f, 1f, 0.7f);
            }

            CreateButton(panel, "CancelBtn", "✕ Annuler",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(90, 20), new Vector2(0, 6));
        }

        private static void SetupFullScreenPanel(GameObject panel)
        {
            GameObject rawGo = new GameObject("FullScreenImage");
            rawGo.transform.SetParent(panel.transform, false);
            RectTransform rt = rawGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(0, -80); rt.anchoredPosition = new Vector2(0, 40);
            rawGo.AddComponent<RawImage>();
            rawGo.AddComponent<CameraMonitorUI>();

            CreateLabel(panel, "RoomLabel", "CAM-01", new Vector2(0f, 1f), new Vector2(0, -25));
            CreateButton(panel, "CloseFullScreenButton", "✕ FERMER",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(110, 32), new Vector2(-60, -16));

            GameObject scanlines = new GameObject("ScanlineOverlay");
            scanlines.transform.SetParent(panel.transform, false);
            RectTransform slrt = scanlines.AddComponent<RectTransform>();
            slrt.anchorMin = Vector2.zero; slrt.anchorMax = Vector2.one; slrt.sizeDelta = Vector2.zero;
            Image slImg = scanlines.AddComponent<Image>();
            slImg.color = new Color(0, 0, 0, 0.06f);
            slImg.raycastTarget = false;
        }

        private static void SetupOrderPanel(GameObject panel)
        {
            CreateLabel(panel, "SurvivorName", "SÉLECTIONNER UN SURVIVANT", new Vector2(0.5f, 1f), new Vector2(0, -20));
            CreateLabel(panel, "SurvivorStats", "", new Vector2(0.5f, 1f), new Vector2(0, -55));
            CreateLabel(panel, "WorkLabel", "— TRAVAIL —", new Vector2(0.5f, 1f), new Vector2(0, -90));
            CreateButton(panel, "RepairGeneratorBtn",   "Réparer le générateur",   new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -115));
            CreateButton(panel, "TransportResourcesBtn","Transporter ressources",   new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -148));
            CreateButton(panel, "CraftToolsBtn",        "Fabriquer des outils",     new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -181));
            CreateLabel(panel, "NeedsLabel", "— BESOINS —", new Vector2(0.5f, 1f), new Vector2(0, -214));
            CreateButton(panel, "GoEatBtn",       "Aller manger",        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -239));
            CreateButton(panel, "GoSleepBtn",     "Aller dormir",        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -272));
            CreateButton(panel, "GoInfirmaryBtn", "Aller à l'infirmerie",new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -305));
            CreateLabel(panel, "SecurityLabel", "— SÉCURITÉ —", new Vector2(0.5f, 0f), new Vector2(0, 100));
            CreateButton(panel, "ArrestBtn", "Arrêter le survivant", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(230, 28), new Vector2(0, 72));
            CreateButton(panel, "PatrolBtn", "Surveiller la zone",   new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(230, 28), new Vector2(0, 44));
            CreateButton(panel, "CancelBtn", "✕ Annuler",            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(110, 28), new Vector2(0, 15));
        }

        private static void SetupEventPopup(GameObject panel)
        {
            CreateLabel(panel, "EventTitle", "⚠ ÉVÉNEMENT", new Vector2(0.5f, 1f), new Vector2(0, -25));
            TextMeshProUGUI desc = CreateLabel(panel, "EventDescription", "Description...", new Vector2(0.5f, 0.5f), new Vector2(0, 20));
            desc.fontSize = 13;
            CreateButton(panel, "EventChoice0", "Choix A", new Vector2(0.25f, 0f), new Vector2(0.25f, 0f), new Vector2(180, 36), new Vector2(0, 20));
            CreateButton(panel, "EventChoice1", "Choix B", new Vector2(0.75f, 0f), new Vector2(0.75f, 0f), new Vector2(180, 36), new Vector2(0, 20));
        }

        private static void SetupMissionMap(GameObject panel)
        {
            // Mission system removed — panel kept as empty placeholder.
            CreateLabel(panel, "MissionMapTitle", "MISSIONS — RETIRÉ", new Vector2(0.5f, 1f), new Vector2(0, -18));
        }

        private static void SetupDossierPanel(GameObject panel)
        {
            CreateLabel(panel, "DossierTitle", "DOSSIERS DES SURVIVANTS", new Vector2(0.5f, 1f), new Vector2(0, -18));

            GameObject scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panel.transform, false);
            RectTransform svrt = scrollGo.AddComponent<RectTransform>();
            svrt.anchorMin = new Vector2(0, 0.08f); svrt.anchorMax = new Vector2(1, 0.92f);
            svrt.sizeDelta = new Vector2(-10, 0); svrt.anchoredPosition = Vector2.zero;
            scrollGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
            ScrollRect sr = scrollGo.AddComponent<ScrollRect>(); sr.horizontal = false;

            GameObject maskGo = new GameObject("ScrollArea");
            maskGo.transform.SetParent(scrollGo.transform, false);
            RectTransform mrt = maskGo.AddComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.sizeDelta = Vector2.zero;
            maskGo.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGo.AddComponent<Mask>().showMaskGraphic = false;

            GameObject listContainer = new GameObject("SurvivorList");
            listContainer.transform.SetParent(maskGo.transform, false);
            RectTransform lrt2 = listContainer.AddComponent<RectTransform>();
            lrt2.anchorMin = new Vector2(0, 1); lrt2.anchorMax = new Vector2(1, 1);
            lrt2.pivot = new Vector2(0.5f, 1f); lrt2.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup vlg2 = listContainer.AddComponent<VerticalLayoutGroup>();
            vlg2.padding = new RectOffset(6, 6, 6, 6); vlg2.spacing = 5;
            vlg2.childControlHeight = false; vlg2.childControlWidth = true; vlg2.childForceExpandHeight = false;
            ContentSizeFitter csf = listContainer.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = lrt2; sr.viewport = mrt;

            CreateButton(panel, "CloseDossierBtn", "✕ FERMER",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120, 30), new Vector2(0, 12));
        }

        // ── Survivor entry template ───────────────────────────────────────────────

        private static GameObject BuildSurvivorEntryPrefab()
        {
            GameObject entry = new GameObject("SurvivorEntry_Template");
            RectTransform rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420, 60);
            entry.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.08f, 0.85f);
            SurvivorEntryUI entryUI = entry.AddComponent<SurvivorEntryUI>();

            GameObject nameGo = new GameObject("Name");
            nameGo.transform.SetParent(entry.transform, false);
            RectTransform nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0.45f, 1f);
            nrt.sizeDelta = new Vector2(-6, 0); nrt.anchoredPosition = new Vector2(6, 0);
            TextMeshProUGUI nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
            nameTMP.fontSize = 12; nameTMP.color = new Color(0.6f, 1f, 0.6f);
            nameTMP.fontStyle = FontStyles.Bold;

            GameObject statsGo = new GameObject("Stats");
            statsGo.transform.SetParent(entry.transform, false);
            RectTransform srt = statsGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0f); srt.anchorMax = new Vector2(0.75f, 0.5f);
            srt.sizeDelta = new Vector2(-6, 0); srt.anchoredPosition = new Vector2(6, 0);
            TextMeshProUGUI statsTMP = statsGo.AddComponent<TextMeshProUGUI>();
            statsTMP.fontSize = 9; statsTMP.color = new Color(0.5f, 0.8f, 0.5f);
            statsTMP.textWrappingMode   = TMPro.TextWrappingModes.NoWrap;

            GameObject statusGo = new GameObject("Status");
            statusGo.transform.SetParent(entry.transform, false);
            RectTransform strt = statusGo.AddComponent<RectTransform>();
            strt.anchorMin = new Vector2(0.76f, 0.15f); strt.anchorMax = new Vector2(1f, 0.85f);
            strt.sizeDelta = Vector2.zero; strt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI statusTMP = statusGo.AddComponent<TextMeshProUGUI>();
            statusTMP.fontSize = 11; statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = new Color(0.3f, 1f, 0.3f);

            GameObject toggleGo = new GameObject("MissionToggleBtn");
            toggleGo.transform.SetParent(entry.transform, false);
            RectTransform trt = toggleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.78f, 0.1f); trt.anchorMax = new Vector2(1f, 0.9f);
            trt.sizeDelta = Vector2.zero; trt.anchoredPosition = new Vector2(-4, 0);
            Image toggleImg = toggleGo.AddComponent<Image>();
            toggleImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            Button toggleBtn = toggleGo.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleImg;
            GameObject toggleLblGo = new GameObject("Label");
            toggleLblGo.transform.SetParent(toggleGo.transform, false);
            RectTransform tlrt = toggleLblGo.AddComponent<RectTransform>();
            tlrt.anchorMin = Vector2.zero; tlrt.anchorMax = Vector2.one; tlrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI toggleLbl = toggleLblGo.AddComponent<TextMeshProUGUI>();
            toggleLbl.text = "MISSION"; toggleLbl.fontSize = 9;
            toggleLbl.alignment = TextAlignmentOptions.Center;
            toggleLbl.color = new Color(0.7f, 1f, 0.7f);

            SerializedObject entrySO = new SerializedObject(entryUI);
            entrySO.FindProperty("nameText").objectReferenceValue   = nameTMP;
            entrySO.FindProperty("statsText").objectReferenceValue  = statsTMP;
            entrySO.FindProperty("statusText").objectReferenceValue = statusTMP;
            entrySO.ApplyModifiedProperties();

            entry.SetActive(false);
            return entry;
        }

        // ── HUD wiring ────────────────────────────────────────────────────────────

        private static void WireHUD(ShelterHUD hud, GameObject canvas,
            GameObject resourceBar, GameObject cameraWall,
            GameObject fullScreenPanel, GameObject orderPanel, GameObject eventPopup,
            GameObject missionMap, GameObject dossierPanel, GameObject survivorEntryPrefab,
            GameObject radioPanel, TextMeshProUGUI radioMsgText, Button closeRadioBtn,
            GameObject gameOverPanel, GameObject gameWonPanel,
            GameObject missionResultPanel, TextMeshProUGUI missionResultText,
            Button closeMissionResult, TextMeshProUGUI notifText,
            GameObject crosshair, GameObject promptRoot, TextMeshProUGUI promptTMP)
        {
            SerializedObject so = new SerializedObject(hud);

            // Resource bar
            BindTMP(so, "foodText",       resourceBar, "FoodText");
            BindTMP(so, "waterText",      resourceBar, "WaterText");
            BindTMP(so, "medicineText",   resourceBar, "MedicineText");
            BindTMP(so, "materialsText",  resourceBar, "MaterialsText");
            BindTMP(so, "energyText",     resourceBar, "EnergyText");
            BindTMP(so, "dayText",        resourceBar, "DayText");
            BindTMP(so, "populationText", resourceBar, "PopulationText");

            // Camera wall
            so.FindProperty("cameraWallPanel").objectReferenceValue = cameraWall;
            Transform feedBorder = cameraWall.transform.Find("CameraFeedBorder");
            if (feedBorder != null)
            {
                so.FindProperty("cameraFeedImage").objectReferenceValue =
                    feedBorder.Find("CameraFeedImage")?.GetComponent<RawImage>();
                so.FindProperty("cameraLabelText").objectReferenceValue =
                    feedBorder.Find("CameraLabelText")?.GetComponent<TextMeshProUGUI>();
            }
            BindButton(so, "camPrevButton",         cameraWall, "NavRow/CamPrevBtn");
            BindButton(so, "camNextButton",         cameraWall, "NavRow/CamNextBtn");
            BindButton(so, "closeCameraWallButton", cameraWall, "NavRow/CloseCameraWallBtn");
            BindButton(so, "openMissionMapButton",  cameraWall, "ActionRow/OpenMissionMapBtn");
            BindButton(so, "openDossierButton",     cameraWall, "ActionRow/OpenDossierBtn");

            // Sidebar
            GameObject sidebar = cameraWall.transform.Find("SurvivorSidebar")?.gameObject;
            if (sidebar != null)
            {
                so.FindProperty("survivorSidebar").objectReferenceValue = sidebar;
                so.FindProperty("survivorNameListContainer").objectReferenceValue =
                    sidebar.transform.Find("SurvivorNameList");
                GameObject sop = sidebar.transform.Find("SidebarOrderPanel")?.gameObject;
                if (sop != null)
                {
                    so.FindProperty("orderPanel").objectReferenceValue = sop;
                    BindTMP(so, "selectedSurvivorNameText",  sop, "SurvivorName");
                    BindTMP(so, "selectedSurvivorStatsText", sop, "SurvivorStats");
                    BindButton(so, "repairGeneratorButton",    sop, "RepairGeneratorBtn");
                    BindButton(so, "transportResourcesButton", sop, "TransportResourcesBtn");
                    BindButton(so, "craftToolsButton",         sop, "CraftToolsBtn");
                    BindButton(so, "goEatButton",              sop, "GoEatBtn");
                    BindButton(so, "goSleepButton",            sop, "GoSleepBtn");
                    BindButton(so, "goInfirmaryButton",        sop, "GoInfirmaryBtn");
                    BindButton(so, "arrestSurvivorButton",     sop, "ArrestBtn");
                    BindButton(so, "patrolZoneButton",         sop, "PatrolBtn");
                    BindButton(so, "cancelOrderButton",        sop, "CancelBtn");
                }
            }

            // Event popup
            so.FindProperty("eventPopupPanel").objectReferenceValue = eventPopup;
            BindTMP(so, "eventTitleText",       eventPopup, "EventTitle");
            BindTMP(so, "eventDescriptionText", eventPopup, "EventDescription");
            BindButton(so, "eventChoice0Button", eventPopup, "EventChoice0");
            BindButton(so, "eventChoice1Button", eventPopup, "EventChoice1");
            Transform c0 = eventPopup.transform.Find("EventChoice0");
            if (c0 != null) so.FindProperty("eventChoice0Label").objectReferenceValue = c0.GetComponentInChildren<TextMeshProUGUI>();
            Transform c1 = eventPopup.transform.Find("EventChoice1");
            if (c1 != null) so.FindProperty("eventChoice1Label").objectReferenceValue = c1.GetComponentInChildren<TextMeshProUGUI>();

            // Dossier
            so.FindProperty("survivorDossierPanel").objectReferenceValue = dossierPanel;
            so.FindProperty("survivorListContainer").objectReferenceValue =
                dossierPanel.transform.Find("ScrollView/ScrollArea/SurvivorList");
            so.FindProperty("survivorEntryPrefab").objectReferenceValue = survivorEntryPrefab;
            BindButton(so, "closeDossierButton", dossierPanel, "CloseDossierBtn");

            // Radio
            so.FindProperty("radioPanel").objectReferenceValue = radioPanel;
            so.FindProperty("radioMessageText").objectReferenceValue = radioMsgText;
            so.FindProperty("closeRadioButton").objectReferenceValue = closeRadioBtn;

            // Game end
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            so.FindProperty("gameWonPanel").objectReferenceValue  = gameWonPanel;

            // Notification & crosshair
            so.FindProperty("notificationText").objectReferenceValue = notifText;
            so.FindProperty("crosshair").objectReferenceValue        = crosshair;

            so.ApplyModifiedProperties();
        }

        // ── CRT ──────────────────────────────────────────────────────────────────

        private static void AddCRTEffect(Camera cam)
        {
            Material crtMat = new Material(Shader.Find("ShelterCommand/CRTScreen"));
            if (crtMat.shader == null || !crtMat.shader.isSupported)
            {
                Debug.LogWarning("[ShelterSupervisorRoomBuilder] CRTScreen shader introuvable — CRT ignoré.");
                return;
            }
            AssetDatabase.CreateAsset(crtMat, "Assets/Materials/CRTScreen_Mat.mat");
            CRTEffect crtEffect = cam.gameObject.AddComponent<CRTEffect>();
            SerializedObject so = new SerializedObject(crtEffect);
            so.FindProperty("crtMaterial").objectReferenceValue = crtMat;
            so.ApplyModifiedProperties();
        }

        // ── UI helpers ────────────────────────────────────────────────────────────

        internal static GameObject CreatePanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta; rt.anchoredPosition = anchoredPosition;
            go.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.05f, 0.9f);
            return go;
        }

        internal static TextMeshProUGUI CreateLabel(GameObject parent, string name, string text,
            Vector2 anchor, Vector2 anchoredPosition, float fontSize = 14f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(380, 30); rt.anchoredPosition = anchoredPosition;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.color = new Color(0.5f, 1f, 0.5f);
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        internal static Button CreateButton(GameObject parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta; rt.anchoredPosition = anchoredPos;
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.2f, 0.08f, 0.9f);
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor      = new Color(0.05f, 0.2f, 0.08f);
            cb.highlightedColor = new Color(0.1f,  0.4f, 0.15f);
            cb.pressedColor     = new Color(0.0f,  0.1f, 0.04f);
            btn.colors = cb;
            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 11;
            tmp.color = new Color(0.7f, 1f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        private static void CreateChildButton(GameObject parent, string name, string label, float fixedWidth = -1f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.14f, 0.08f, 1f);
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.18f, 0.35f, 0.18f, 1f);
            cb.pressedColor     = new Color(0.05f, 0.10f, 0.05f, 1f);
            btn.colors = cb;
            if (fixedWidth > 0f) { LayoutElement le = go.AddComponent<LayoutElement>(); le.preferredWidth = fixedWidth; le.flexibleWidth = 0; }
            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 10; tmp.color = new Color(0.5f, 1f, 0.5f);
        }

        private static void CreateFlexSpacer(GameObject parent)
        {
            GameObject go = new GameObject("Spacer");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        // ── SerializedObject binding helpers ─────────────────────────────────────

        private static void BindTMP(SerializedObject so, string prop, GameObject container, string childPath)
        {
            Transform t = container.transform.Find(childPath);
            if (t != null) so.FindProperty(prop).objectReferenceValue = t.GetComponent<TextMeshProUGUI>();
        }

        private static void BindButton(SerializedObject so, string prop, GameObject container, string childPath)
        {
            Transform t = container.transform.Find(childPath);
            if (t != null) so.FindProperty(prop).objectReferenceValue = t.GetComponent<Button>();
            else { Button b = container.GetComponent<Button>(); if (b != null) so.FindProperty(prop).objectReferenceValue = b; }
        }

        // ── Misc ─────────────────────────────────────────────────────────────────

        private static void BuildBox(GameObject parent, float w, float h, float d,
            Material floor, Material wall, Material ceiling)
        {
            BuildPrimitive(PrimitiveType.Cube, "Floor",   parent, new Vector3(0, -0.05f,       0), new Vector3(w,    0.1f, d), floor);
            BuildPrimitive(PrimitiveType.Cube, "Ceiling", parent, new Vector3(0,  h + 0.05f,   0), new Vector3(w,    0.1f, d), ceiling);
            BuildPrimitive(PrimitiveType.Cube, "Wall_N",  parent, new Vector3(0,  h * 0.5f,  d * 0.5f), new Vector3(w, h, 0.1f), wall);
            BuildPrimitive(PrimitiveType.Cube, "Wall_S",  parent, new Vector3(0,  h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.1f), wall);
            BuildPrimitive(PrimitiveType.Cube, "Wall_E",  parent, new Vector3( w * 0.5f, h * 0.5f, 0), new Vector3(0.1f, h, d), wall);
            BuildPrimitive(PrimitiveType.Cube, "Wall_W",  parent, new Vector3(-w * 0.5f, h * 0.5f, 0), new Vector3(0.1f, h, d), wall);
        }

        private static GameObject BuildPrimitive(PrimitiveType type, string name, GameObject parent,
            Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject FindSystemsRoot(GameObject root)
        {
            Transform t = root.transform.Find("GameSystems");
            return t != null ? t.gameObject : root;
        }
    }
}
