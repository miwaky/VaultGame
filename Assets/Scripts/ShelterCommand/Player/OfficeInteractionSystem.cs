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
        private ItemCarrySystem carrySystem;

        // ── Debug ────────────────────────────────────────────────────────────────
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        private float debugLogCooldown;
        private const float DebugLogInterval = 0.5f;

        private void Start()
        {
            carrySystem = GetComponent<ItemCarrySystem>() ?? GetComponentInParent<ItemCarrySystem>();

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

            if (Keyboard.current == null) return;

            // Do not trigger new interactions while the survivor dialogue is open
            if (SurvivorInteractionUI.Instance != null && SurvivorInteractionUI.Instance.IsVisible)
                return;

            if (currentTarget != null && Keyboard.current.eKey.wasPressedThisFrame)
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
                carrySystem?.SetActiveShelf(null);
                return;
            }

            int effectiveMask = interactionMask.value == 0 ? ~0 : interactionMask.value;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            bool shouldLog = debugMode && Time.time > debugLogCooldown;

            // RaycastAll so that a cardboard box sitting behind a shelf mesh is still reachable.
            // We walk hits sorted by distance and take the first one that has an IInteractable.
            RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, effectiveMask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            if (shouldLog && hits.Length > 0)
            {
                debugLogCooldown = Time.time + DebugLogInterval;
                Debug.Log($"[Interaction] RaycastAll : {hits.Length} hit(s) — premier='{hits[0].collider.gameObject.name}' à {hits[0].distance:F2}m");
            }
            else if (shouldLog)
            {
                debugLogCooldown = Time.time + DebugLogInterval;
                Debug.Log($"[Interaction] Aucun hit — ray depuis {ray.origin:F2} direction {ray.direction:F2} | masque = {effectiveMask}");
            }

            foreach (RaycastHit hit in hits)
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.IsInteractable) continue;

                currentTarget = interactable;

                // Notify carry system when a shelf is in range (for hold-E box stocking)
                StorageShelf shelf = interactable as StorageShelf;
                carrySystem?.SetActiveShelf(shelf);

                string prompt;
                if (shelf != null && carrySystem != null && carrySystem.IsCarryingBox)
                {
                    CardboardBox box = carrySystem.CarriedBox;
                    prompt = box.IsEmpty
                        ? "[E] Carton vide"
                        : $"[E] Déposer {box.ItemCount} objet(s) sur l'étagère";
                }
                else
                {
                    prompt = interactable.PromptLabel;
                }

                SetPrompt(true, prompt);
                return;
            }

            currentTarget = null;
            carrySystem?.SetActiveShelf(null);
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
