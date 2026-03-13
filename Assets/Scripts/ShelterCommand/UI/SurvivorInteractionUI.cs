using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Displays a survivor's generated profile when the player interacts with them.
    /// If no panel is assigned in the Inspector, a minimal fallback UI is built at runtime.
    ///
    /// Setup (manual, recommandé) :
    ///   Ajouter ce composant sur le Canvas HUD. Assigner Panel, champs texte,
    ///   bouton Fermer, et OfficeInteractionSystem dans l'Inspector.
    ///
    /// Setup (fallback automatique) :
    ///   Laisser Panel vide — un panel sombre compact est créé automatiquement.
    /// </summary>
    public class SurvivorInteractionUI : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("Root du panel d'info. Laisser vide pour auto-générer.")]
        [SerializeField] private GameObject panel;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI survivorNameText;
        [SerializeField] private TextMeshProUGUI presentationText;
        [SerializeField] private TextMeshProUGUI identityText;
        [SerializeField] private TextMeshProUGUI statsText;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        [Header("FPS Controller")]
        [Tooltip("Référence à OfficeInteractionSystem pour unlock le joueur à la fermeture.")]
        [SerializeField] private OfficeInteractionSystem interactionSystem;

        private ShelterHUD shelterHUD;

        // ── Singleton ─────────────────────────────────────────────────────────────
        public static SurvivorInteractionUI Instance { get; private set; }

        /// <summary>True when the dialogue panel is currently shown.</summary>
        public bool IsVisible => panel != null && panel.activeSelf;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (panel == null)
                BuildFallbackPanel();
        }

        private void Start()
        {
            if (interactionSystem == null)
                interactionSystem = FindFirstObjectByType<OfficeInteractionSystem>();

            shelterHUD = FindFirstObjectByType<ShelterHUD>();

            closeButton?.onClick.AddListener(Hide);
            Hide();
        }

        private void Update()
        {
            // Close dialogue with E — Escape intentionally excluded to avoid closing the terminal
            if (panel != null && panel.activeSelf)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    Hide();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Affiche le panel de profil pour le survivant donné.</summary>
        public void Show(SurvivorBehavior survivor)
        {
            if (survivor == null) return;

            SurvivorGeneratedProfile profile = survivor.GeneratedProfile;

            if (profile != null)
            {
                SetText(survivorNameText, profile.survivorName.ToUpper());
                SetText(presentationText, profile.PresentationText);
                SetText(identityText,     profile.GetIdentityDisplayText());
                SetText(statsText,        profile.GetStatsDisplayText());
            }
            else
            {
                SetText(survivorNameText, survivor.SurvivorName.ToUpper());
                SetText(presentationText, "Informations non disponibles.");
                SetText(identityText,     string.Empty);

                if (survivor.Data != null)
                {
                    SurvivorData d = survivor.Data;
                    SetText(statsText,
                        $"Force {d.strength}  •  Intel. {d.intelligence}  •  Tech. {d.technical}" +
                        $"\nLoyauté {d.loyalty}  •  Endurance {d.endurance}");
                }
                else { SetText(statsText, string.Empty); }
            }

            SafeSetActive(panel, true);
        }

        /// <summary>Cache le panel, déverrouille le FPS controller et restaure le crosshair.</summary>
        public void Hide()
        {
            SafeSetActive(panel, false);
            interactionSystem?.SetFPSLocked(false);
            shelterHUD?.SetCrosshairVisible(true);
        }

        // ── Fallback UI builder ───────────────────────────────────────────────────

        private void BuildFallbackPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[SurvivorInteractionUI] Aucun Canvas trouvé — impossible de créer le panel.");
                return;
            }

            // Root panel
            GameObject panelGo = new GameObject("SurvivorInfoPanel");
            panelGo.transform.SetParent(canvas.transform, false);
            RectTransform pr = panelGo.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.58f, 0.04f);
            pr.anchorMax = new Vector2(0.99f, 0.58f);
            pr.offsetMin = pr.offsetMax = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.93f);
            panel = panelGo;

            // Layout
            VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;
            layout.childControlWidth     = true;
            layout.childControlHeight    = false;
            layout.childForceExpandWidth = true;

            // Labels
            survivorNameText = MakeLabel(panelGo, "NameText",  22, FontStyles.Bold,   Color.white);
            presentationText = MakeLabel(panelGo, "PresText",  13, FontStyles.Italic,  new Color(0.82f, 0.82f, 0.82f));
            identityText     = MakeLabel(panelGo, "IdentText", 12, FontStyles.Normal,  new Color(0.55f, 0.85f, 1f));
            statsText        = MakeLabel(panelGo, "StatsText", 12, FontStyles.Normal,  new Color(0.6f, 1f, 0.6f));

            MakeDivider(panelGo);
            closeButton = MakeCloseButton(panelGo);

            Debug.Log("[SurvivorInteractionUI] Panel auto-généré. " +
                      "Assigne ton propre Panel dans l'Inspector pour le personnaliser.");
        }

        // ── UI factories ──────────────────────────────────────────────────────────

        private static TextMeshProUGUI MakeLabel(
            GameObject parent, string name, int fontSize, FontStyles style, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>().sizeDelta =
                new Vector2(0f, fontSize <= 14 ? fontSize * 3.4f : fontSize * 1.7f);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text               = string.Empty;
            tmp.fontSize           = fontSize;
            tmp.color              = color;
            tmp.fontStyle          = style;
            tmp.textWrappingMode   = TMPro.TextWrappingModes.Normal;
            tmp.overflowMode       = TextOverflowModes.Truncate;
            return tmp;
        }

        private static void MakeDivider(GameObject parent)
        {
            GameObject go = new GameObject("Divider");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        }

        private static Button MakeCloseButton(GameObject parent)
        {
            GameObject go = new GameObject("CloseButton");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 30f);
            go.AddComponent<Image>().color = new Color(0.75f, 0.18f, 0.18f, 0.9f);
            Button btn = go.AddComponent<Button>();

            GameObject label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            RectTransform lr = label.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Fermer  [E]";
            tmp.fontSize  = 13;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void SetText(TextMeshProUGUI tmp, string value)
        {
            if (tmp != null) tmp.text = value;
        }

        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
