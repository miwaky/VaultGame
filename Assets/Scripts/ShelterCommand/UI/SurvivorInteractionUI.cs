using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Displays a survivor's generated profile (name, presentation text, stats, profession, traits)
    /// when the player interacts with them in the shelter.
    ///
    /// Usage:
    ///   - Assign this component to the HUD Canvas.
    ///   - Call Show(SurvivorBehavior) from the player's interaction system.
    ///   - The Close button hides the panel and returns control to the player.
    ///
    /// Wire-up:
    ///   SurvivorInteractable → calls SurvivorInteractionUI.Show(survivorBehavior)
    /// </summary>
    public class SurvivorInteractionUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI survivorNameText;
        [SerializeField] private TextMeshProUGUI presentationText;
        [SerializeField] private TextMeshProUGUI identityText;
        [SerializeField] private TextMeshProUGUI statsText;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        // ── Singleton accessor for easy access from Interactable scripts ──────────
        public static SurvivorInteractionUI Instance { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            closeButton?.onClick.AddListener(Hide);
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Displays the profile panel for the given survivor.
        /// Safely handles survivors without a generated profile (falls back to ScriptableObject data).
        /// </summary>
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
                // Fallback for survivors without a generated profile
                SetText(survivorNameText, survivor.SurvivorName.ToUpper());
                SetText(presentationText, "Informations non disponibles.");
                SetText(identityText,     string.Empty);

                if (survivor.Data != null)
                {
                    SurvivorData d = survivor.Data;
                    string fallbackStats = $"Force {d.strength}  •  Intel. {d.intelligence}  •  Tech. {d.technical}" +
                                          $"\nLoyauté {d.loyalty}  •  Endurance {d.endurance}";
                    SetText(statsText, fallbackStats);
                }
                else
                {
                    SetText(statsText, string.Empty);
                }
            }

            SafeSetActive(panel, true);
        }

        /// <summary>Hides the interaction panel.</summary>
        public void Hide()
        {
            SafeSetActive(panel, false);
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
