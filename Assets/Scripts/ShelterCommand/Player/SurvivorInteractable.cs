using UnityEngine;
using UnityEngine.AI;

namespace ShelterCommand
{
    /// <summary>
    /// Makes a survivor detectable and interactable by OfficeInteractionSystem.
    /// Implements IInteractable — the existing raycast loop picks it up automatically.
    ///
    /// On interaction:
    ///   1. The NPC instantly turns to face the player (Y-axis only).
    ///   2. The NavMeshAgent is stopped during the conversation.
    ///   3. DialogueManager opens the shared DialoguePanelUI.
    ///   4. Agent and idle movement resume when dialogue ends.
    /// </summary>
    [RequireComponent(typeof(SurvivorBehavior))]
    public class SurvivorInteractable : MonoBehaviour, IInteractable
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Tooltip("Pool de lignes de dialogue selon l'état du PNJ. Optionnel.")]
        [SerializeField] private SurvivorNpcDialogue dialogueConfig;

        // ── Constants ─────────────────────────────────────────────────────────────

        private const float ColliderHeight = 1.8f;
        private const float ColliderRadius = 0.3f;

        // ── State ─────────────────────────────────────────────────────────────────

        private SurvivorBehavior     survivorBehavior;
        private NavMeshAgent          agent;
        private SurvivorIdleMovement  idleMovement;

        // ── IInteractable ─────────────────────────────────────────────────────────

        public string PromptLabel  => $"Parler à {survivorBehavior.SurvivorName}";
        public bool IsInteractable => survivorBehavior != null && survivorBehavior.IsAlive;

        /// <summary>Called by OfficeInteractionSystem when the player presses E.</summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            // 1. Face the player (Y-axis only)
            FaceTarget(interactionSystem.transform.position);

            // 2. Pause NavMeshAgent movement
            PauseMovement();

            // 3. Build a one-shot ExplorationDialogue from the NPC's current state
            ExplorationDialogue node = BuildDialogueNode();

            // 4. Wrap into a minimal RadioCallEvent and hand off to DialogueManager
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[SurvivorInteractable] DialogueManager introuvable dans la scène.");
                ResumeMovement();
                return;
            }

            RadioCallEvent callEvent = ScriptableObject.CreateInstance<RadioCallEvent>();
            callEvent.dialogue = node;

            DialogueManager.Instance.StartDialogue(callEvent, mission: null, onEnd: ResumeMovement);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            survivorBehavior = GetComponent<SurvivorBehavior>();
            agent            = GetComponent<NavMeshAgent>();
            idleMovement     = GetComponent<SurvivorIdleMovement>();
            EnsureCollider();
        }

        // ── Private — face player ─────────────────────────────────────────────────

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // ── Private — movement pause / resume ─────────────────────────────────────

        private void PauseMovement()
        {
            if (idleMovement != null) idleMovement.enabled = false;
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity  = Vector3.zero;
            }
        }

        private void ResumeMovement()
        {
            if (agent  != null) agent.isStopped = false;
            if (idleMovement != null) idleMovement.enabled = true;
        }

        // ── Private — dialogue node builder ──────────────────────────────────────

        private ExplorationDialogue BuildDialogueNode()
        {
            ExplorationDialogue node = ScriptableObject.CreateInstance<ExplorationDialogue>();
            node.name         = $"NPC_{survivorBehavior.SurvivorName}";
            node.speakerName  = survivorBehavior.SurvivorName;
            node.choices      = System.Array.Empty<DialogueChoice>();
            node.hasTimeLimit = false;

            string line = dialogueConfig != null
                ? dialogueConfig.GetRandomLine(survivorBehavior)
                : DefaultLine();

            SurvivorGeneratedProfile profile = survivorBehavior.GeneratedProfile;
            string profileBlock = profile != null
                ? $"\n\n<color=#8CD4FF>{profile.GetIdentityDisplayText()}</color>" +
                  $"\n<color=#90EE90>{profile.GetStatsDisplayText()}</color>"
                : string.Empty;

            node.dialogueText = line + profileBlock;
            return node;
        }

        private string DefaultLine()
        {
            return survivorBehavior.IsWorking
                ? $"{survivorBehavior.SurvivorName} est concentré sur sa tâche."
                : $"{survivorBehavior.SurvivorName} vous regarde.";
        }

        // ── Private — collider guard ──────────────────────────────────────────────

        private void EnsureCollider()
        {
            bool hasCollider = GetComponent<Collider>()           != null ||
                               GetComponentInChildren<Collider>() != null;
            if (hasCollider) return;

            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.height = ColliderHeight;
            col.radius = ColliderRadius;
            col.center = new Vector3(0f, ColliderHeight * 0.5f, 0f);
        }
    }
}
