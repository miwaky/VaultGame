using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace ShelterCommand
{
    /// <summary>
    /// Casts a ray from the player's camera. When an IInteractable is in range,
    /// shows a prompt and triggers interaction on E key press.
    /// </summary>
    public class OfficeInteractionSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private ShelterFPSController fpsController;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask;

        [Header("UI Prompt")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TextMeshProUGUI promptText;

        private IInteractable currentTarget;

        // ── Debug ────────────────────────────────────────────────────────────────
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;  // désactive en prod

        private float debugLogCooldown;
        private const float DebugLogInterval = 0.5f;  // log toutes les 0.5s max pour ne pas spammer

        private void Start()
        {
            if (playerCamera == null)
                Debug.LogError("[Interaction] playerCamera NON assignée sur " + gameObject.name);
            else
                Debug.Log("[Interaction] playerCamera OK : " + playerCamera.name);

            if (fpsController == null)
                Debug.LogWarning("[Interaction] fpsController non assigné — le lock check sera ignoré.");

            int effectiveMask = interactionMask.value == 0 ? ~0 : interactionMask.value;
            Debug.Log($"[Interaction] interactionMask = {interactionMask.value} → effectiveMask = {effectiveMask}  | distance = {interactionDistance}");
        }

        private void Update()
        {
            ScanForInteractable();

            if (currentTarget != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log($"[Interaction] E pressé → Interact() sur '{(currentTarget as MonoBehaviour)?.gameObject.name}'");
                currentTarget.Interact(this);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Locks/unlocks the FPS controller without showing cursor (used by world props).</summary>
        public void SetFPSLocked(bool locked)
        {
            if (fpsController != null) fpsController.SetLocked(locked);
        }

        public ShelterFPSController FPSController => fpsController;

        // ── Private ──────────────────────────────────────────────────────────────

        private void ScanForInteractable()
        {
            if (playerCamera == null) return;

            if (fpsController != null && fpsController.IsLocked)
            {
                SetPrompt(false, "");
                currentTarget = null;
                return;
            }

            int effectiveMask = interactionMask.value == 0 ? ~0 : interactionMask.value;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            bool shouldLog = debugMode && Time.time > debugLogCooldown;

            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, effectiveMask))
            {
                if (shouldLog)
                {
                    debugLogCooldown = Time.time + DebugLogInterval;
                    Debug.Log($"[Interaction] Raycast touche : '{hit.collider.gameObject.name}' " +
                              $"| layer = '{LayerMask.LayerToName(hit.collider.gameObject.layer)}' " +
                              $"| distance = {hit.distance:F2}m");
                }

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.IsInteractable)
                {
                    currentTarget = interactable;
                    SetPrompt(true, $"[E] {interactable.PromptLabel}");
                    return;
                }
                else if (shouldLog)
                {
                    Debug.LogWarning($"[Interaction] '{hit.collider.gameObject.name}' touché MAIS pas d'IInteractable trouvé sur lui ou ses parents.");
                }
            }
            else if (shouldLog)
            {
                debugLogCooldown = Time.time + DebugLogInterval;
                Debug.Log($"[Interaction] Aucun hit — ray depuis {ray.origin:F2} direction {ray.direction:F2} | masque = {effectiveMask}");
            }

            currentTarget = null;
            SetPrompt(false, "");
        }

        private void SetPrompt(bool visible, string text)
        {
            if (promptRoot != null) promptRoot.SetActive(visible);
            if (promptText != null && visible) promptText.text = text;
        }
    }

    /// <summary>Contract for any interactable prop in the office.</summary>
    public interface IInteractable
    {
        string PromptLabel { get; }
        bool IsInteractable { get; }
        void Interact(OfficeInteractionSystem interactionSystem);
    }
}
