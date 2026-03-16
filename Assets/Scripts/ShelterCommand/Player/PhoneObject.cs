using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Manages the physical phone GameObject held in the player's hand.
    ///
    /// Attach this to the PhoneObject child of HandAnchor:
    ///
    ///   Player
    ///   └─ Head / Camera
    ///      └─ HandAnchor
    ///         └─ PhoneObject   ← this component goes here
    ///              ├─ PhoneMesh
    ///              ├─ PhoneScreen
    ///              └─ TelephoneUI
    ///
    /// TelephoneController calls Show() and Hide() — no other script should touch SetActive directly.
    /// </summary>
    public class PhoneObject : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Optional: local transform offset when held")]
        [SerializeField] private Vector3 localPosition = new Vector3(0.2f, -0.25f, 0.45f);
        [SerializeField] private Vector3 localEulerAngles = new Vector3(0f, 0f, 0f);

        // ── Lifecycle ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            ApplyLocalTransform();
            gameObject.SetActive(false);
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Makes the phone visible in the player's hand.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Hides the phone from the player's hand.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ── Private ────────────────────────────────────────────────────────────────

        private void ApplyLocalTransform()
        {
            transform.localPosition    = localPosition;
            transform.localEulerAngles = localEulerAngles;
        }
    }
}
