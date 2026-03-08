using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Interactable switch that toggles the shelter's electricity.
    /// Attach to any GameObject. Wire up the optional indicator Light
    /// (a small LED on the switch model) to reflect power state.
    /// </summary>
    public class PowerSwitchProp : MonoBehaviour, IInteractable
    {
        [Header("Indicator (optional)")]
        [SerializeField] private Light indicatorLight;
        [SerializeField] private Color onColor  = new Color(0.2f, 1f, 0.2f);
        [SerializeField] private Color offColor = new Color(1f, 0.1f, 0.1f);

        public bool IsInteractable => true;

        public string PromptLabel => ElectricitySystem.Instance != null && ElectricitySystem.Instance.IsOn
            ? "Couper l'électricité"
            : "Allumer l'électricité";

        private void OnEnable()
        {
            if (ElectricitySystem.Instance != null)
                ElectricitySystem.Instance.OnPowerChanged += RefreshIndicator;
        }

        private void OnDisable()
        {
            if (ElectricitySystem.Instance != null)
                ElectricitySystem.Instance.OnPowerChanged -= RefreshIndicator;
        }

        private void Start()
        {
            if (ElectricitySystem.Instance != null)
                RefreshIndicator(ElectricitySystem.Instance.Power);
        }

        /// <summary>Called by OfficeInteractionSystem when the player presses E.</summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            if (ElectricitySystem.Instance == null)
            {
                Debug.LogWarning("[PowerSwitchProp] ElectricitySystem introuvable dans la scène.");
                return;
            }
            ElectricitySystem.Instance.Toggle();
        }

        private void RefreshIndicator(float power)
        {
            if (indicatorLight == null) return;
            indicatorLight.color     = power > 0f ? onColor : offColor;
            indicatorLight.intensity = power > 0f ? 0.6f : 0.3f;
        }
    }
}
