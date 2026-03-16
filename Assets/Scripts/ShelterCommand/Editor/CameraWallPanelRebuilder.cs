using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ShelterCommand;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Builds the CameraWallPanel inside the ComputerUI and wires it to CameraWallPanelUI.
    /// The panel contains: NavRow (camera list), CameraViewport (feed), CameraName (label), CloseBtn.
    /// Menu: Window → ShelterCommand → Rebuild CameraWallPanel (ComputerUI)
    /// </summary>
    internal static class CameraWallPanelRebuilder
    {
        [MenuItem("Window/ShelterCommand/Rebuild CameraWallPanel (ComputerUI)")]
        public static void Rebuild()
        {
            // ── Find ComputerMenuController ─────────────────────────────────────────
            ComputerMenuController menu = Object.FindFirstObjectByType<ComputerMenuController>();
            if (menu == null)
            {
                Debug.LogError("[CameraWallPanelRebuilder] ComputerMenuController introuvable dans la scène.");
                return;
            }

            Canvas canvas = menu.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[CameraWallPanelRebuilder] Canvas parent du ComputerMenuController introuvable.");
                return;
            }

            GameObject canvasGo = canvas.gameObject;

            // ── Remove stale CameraWallPanel if any ────────────────────────────────
            Transform old = canvasGo.transform.Find("CameraWallPanel");
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old.gameObject);
                Debug.Log("[CameraWallPanelRebuilder] Ancien CameraWallPanel supprimé.");
            }

            // ── Root panel ──────────────────────────────────────────────────────────
            GameObject wall = new GameObject("CameraWallPanel");
            wall.transform.SetParent(canvasGo.transform, false);
            RectTransform wallRT = wall.AddComponent<RectTransform>();
            wallRT.anchorMin = Vector2.zero;
            wallRT.anchorMax = Vector2.one;
            wallRT.sizeDelta = Vector2.zero;
            wall.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.04f, 0.97f);
            Undo.RegisterCreatedObjectUndo(wall, "Rebuild CameraWallPanel (ComputerUI)");

            // ── NavRow (top strip, 8 % height) ─────────────────────────────────────
            GameObject navRow = new GameObject("NavRow");
            navRow.transform.SetParent(wall.transform, false);
            RectTransform navRT = navRow.AddComponent<RectTransform>();
            navRT.anchorMin = new Vector2(0f, 0.92f);
            navRT.anchorMax = new Vector2(1f, 1f);
            navRT.sizeDelta = Vector2.zero;
            navRow.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.03f, 1f);
            HorizontalLayoutGroup navHlg = navRow.AddComponent<HorizontalLayoutGroup>();
            navHlg.childControlWidth      = false;
            navHlg.childControlHeight     = true;
            navHlg.childForceExpandWidth  = false;
            navHlg.childForceExpandHeight = true;
            navHlg.spacing  = 4;
            navHlg.padding  = new RectOffset(4, 4, 4, 4);

            // ── CameraViewport (fills remaining 92 %) ──────────────────────────────
            GameObject viewport = new GameObject("CameraViewport");
            viewport.transform.SetParent(wall.transform, false);
            RectTransform vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = new Vector2(0f, 0.07f);
            vpRT.anchorMax = new Vector2(1f, 0.92f);
            vpRT.sizeDelta = Vector2.zero;
            RawImage rawImg = viewport.AddComponent<RawImage>();
            rawImg.color = new Color(0.85f, 1f, 0.85f);

            // ── CameraName (bottom strip, 7 % height) ─────────────────────────────
            GameObject camNameRow = new GameObject("CameraNameRow");
            camNameRow.transform.SetParent(wall.transform, false);
            RectTransform cnRT = camNameRow.AddComponent<RectTransform>();
            cnRT.anchorMin = new Vector2(0f, 0f);
            cnRT.anchorMax = new Vector2(1f, 0.07f);
            cnRT.sizeDelta = Vector2.zero;
            camNameRow.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.03f, 1f);

            HorizontalLayoutGroup cnHlg = camNameRow.AddComponent<HorizontalLayoutGroup>();
            cnHlg.childControlWidth      = true;
            cnHlg.childControlHeight     = true;
            cnHlg.childForceExpandWidth  = true;
            cnHlg.childForceExpandHeight = true;
            cnHlg.padding = new RectOffset(8, 4, 4, 4);
            cnHlg.spacing = 4;

            // Camera name label
            GameObject camNameGo = new GameObject("CameraName");
            camNameGo.transform.SetParent(camNameRow.transform, false);
            camNameGo.AddComponent<RectTransform>();
            TextMeshProUGUI camNameTMP = camNameGo.AddComponent<TextMeshProUGUI>();
            camNameTMP.text      = "CAM-XX";
            camNameTMP.fontSize  = 12;
            camNameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            camNameTMP.color     = new Color(0.5f, 1f, 0.5f);

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(camNameRow.transform, false);
            spacer.AddComponent<RectTransform>();
            LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleWidth = 1f;

            // Close button
            Button closeBtn = CreateButton(camNameRow, "CloseBtn", "✕ QUITTER", fixedWidth: 120f);

            wall.SetActive(false);

            // ── Wire CameraWallPanelUI ───────────────────────────────────────────────
            CameraWallPanelUI panelUI = wall.AddComponent<CameraWallPanelUI>();
            SerializedObject  so      = new SerializedObject(panelUI);
            so.FindProperty("navRow").objectReferenceValue          = navRow.transform;
            so.FindProperty("cameraViewport").objectReferenceValue  = rawImg;
            so.FindProperty("cameraNameText").objectReferenceValue  = camNameTMP;
            so.FindProperty("closeButton").objectReferenceValue     = closeBtn;
            so.ApplyModifiedProperties();

            // ── Wire ComputerMenuController ────────────────────────────────────────
            SerializedObject menuSO = new SerializedObject(menu);
            menuSO.FindProperty("cameraWallPanelUI").objectReferenceValue = panelUI;
            menuSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(panelUI);
            EditorUtility.SetDirty(menu);

            Debug.Log("[CameraWallPanelRebuilder] ✔ CameraWallPanel reconstruit dans le ComputerUI et câblé à CameraWallPanelUI.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static Button CreateButton(GameObject parent, string name, string label,
            float fixedWidth = -1f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.14f, 0.08f, 1f);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            if (fixedWidth > 0f)
            {
                LayoutElement le = go.AddComponent<LayoutElement>();
                le.minWidth       = fixedWidth;
                le.preferredWidth = fixedWidth;
            }

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 11;
            tmp.color     = new Color(0.7f, 1f, 0.7f);

            return btn;
        }
    }
}
