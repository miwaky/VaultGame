using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Makes a survivor detectable and interactable by OfficeInteractionSystem.
    /// Implements IInteractable — no extra wiring required:
    /// the existing raycast loop picks it up automatically when the player looks at the survivor.
    /// Pressing E opens SurvivorInteractionUI with the survivor's generated profile.
    /// </summary>
    [RequireComponent(typeof(SurvivorBehavior))]
    public class SurvivorInteractable : MonoBehaviour, IInteractable
    {
        // ── IInteractable ─────────────────────────────────────────────────────────

        public string PromptLabel  => $"Parler à {survivorBehavior.SurvivorName}";
        public bool IsInteractable => survivorBehavior != null && survivorBehavior.IsAlive;

        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            if (SurvivorInteractionUI.Instance == null)
            {
                Debug.LogError("[SurvivorInteractable] SurvivorInteractionUI introuvable — " +
                               "ajoute le composant sur le Canvas HUD et assigne le champ 'Panel'.");
                return;
            }

            SurvivorInteractionUI.Instance.Show(survivorBehavior);

            // Lock FPS movement while the UI panel is open
            interactionSystem.SetFPSLocked(true);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private SurvivorBehavior survivorBehavior;

        private void Awake()
        {
            survivorBehavior = GetComponent<SurvivorBehavior>();
            EnsureCollider();
        }

        private void EnsureCollider()
        {
            bool hasCollider = GetComponent<Collider>() != null ||
                               GetComponentInChildren<Collider>() != null;
            if (!hasCollider)
            {
                CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 1.8f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0f, 0.9f, 0f);
            }
        }
    }
}
