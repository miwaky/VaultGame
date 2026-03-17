using System;
using System.Collections;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Central controller for exploration radio dialogues.
    ///
    /// Called by <see cref="RadioCallManager"/> when a <see cref="RadioCallEvent"/> fires.
    /// Drives <see cref="DialoguePanelUI"/>, blocks the player, plays radio sound,
    /// fires <see cref="DialogueEventData"/> effects on choice, and handles timed choices.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static DialogueManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("UI")]
        [SerializeField] private DialoguePanelUI dialoguePanel;

        [Header("Audio")]
        [SerializeField] private AudioSource radioAudioSource;
        [Tooltip("Short crackle / static clip played when the radio opens.")]
        [SerializeField] private AudioClip   radioStaticClip;

        [Header("Radio Prop")]
        [Tooltip("Radio.prefab — instantiated in the player's hand during a call.")]
        [SerializeField] private GameObject  radioPrefab;
        [Tooltip("Empty child transform of the camera that defines where the radio sits in hand. " +
                 "Suggested local position: (0.25, -0.22, 0.45).")]
        [SerializeField] private Transform   handAnchor;

        // ── Dependencies ──────────────────────────────────────────────────────────
        private ShelterFPSController    playerController;
        private ShelterResourceManager  resourceManager;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private ActiveMission        currentMission;
        private Action               onConversationEnd;
        private bool                 isDialogueActive;
        private ExplorationDialogue  currentNode;
        private DialogueContext      currentContext;
        private Coroutine            timerCoroutine;
        private GameObject           radioInstance;

        /// <summary>The active mission tied to the current dialogue, or null when no dialogue is running.</summary>
        public ActiveMission CurrentMission => currentMission;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            playerController = FindFirstObjectByType<ShelterFPSController>();
            resourceManager  = FindFirstObjectByType<ShelterResourceManager>();

            if (dialoguePanel == null)
                // FindAnyObjectByType includes inactive GameObjects — required because
                // DialoguePanel is disabled by default.
                dialoguePanel = FindAnyObjectByType<DialoguePanelUI>(FindObjectsInactive.Include);

            if (dialoguePanel == null)
                Debug.LogError("[DialogueManager] DialoguePanelUI introuvable (actif ou inactif) — " +
                               "câbler la référence dans l'Inspector.");

            dialoguePanel?.Hide();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Starts a radio dialogue sequence.</summary>
        public void StartDialogue(RadioCallEvent callEvent, ActiveMission mission, Action onEnd = null)
        {
            Debug.Log($"[DialogueManager] StartDialogue('{callEvent?.name}') — dialogue={callEvent?.dialogue?.name ?? "NULL"}, panel={dialoguePanel != null}");

            if (callEvent?.dialogue == null)
            {
                Debug.LogError($"[DialogueManager] ❌ callEvent.dialogue est null — abandon.");
                onEnd?.Invoke();
                return;
            }

            currentMission    = mission;
            onConversationEnd = onEnd;
            isDialogueActive  = true;

            playerController?.SetLocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            // ── Radio prop ────────────────────────────────────────────────────────
            ShowRadioProp();

            // ── Audio: static crackle first, then voice clip ──────────────────────
            if (radioAudioSource != null)
            {
                if (radioStaticClip != null)
                    radioAudioSource.PlayOneShot(radioStaticClip);

                if (callEvent.radioSound != null)
                    radioAudioSource.PlayOneShot(callEvent.radioSound);
            }

            ShowNode(callEvent.dialogue);
        }

        /// <summary>Advances to a node after a choice is selected.</summary>
        public void OnChoiceSelected(DialogueChoice choice)
        {
            if (choice == null) return;

            StopTimer();
            ApplyEvent(choice.eventTrigger);

            if (choice.followUpCall != null && currentMission != null)
                RadioCallManager.Instance?.ScheduleFollowUpCall(
                    choice.followUpCall, currentMission, choice.followUpDelayDays);

            if (choice.nextDialogue != null)
                ShowNode(choice.nextDialogue);
            else
                EndDialogue();
        }

        /// <summary>Closes the dialogue (OK button or timeout with index -1).</summary>
        public void EndDialogue()
        {
            if (!isDialogueActive) return;
            isDialogueActive = false;

            StopTimer();

            if (currentNode?.followUpCall != null && currentMission != null)
                RadioCallManager.Instance?.ScheduleFollowUpCall(
                    currentNode.followUpCall, currentMission, currentNode.followUpDelayDays);

            dialoguePanel?.Hide();

            playerController?.SetLocked(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            HideRadioProp();

            currentNode    = null;
            currentContext = null;

            Action callback = onConversationEnd;
            onConversationEnd = null;
            callback?.Invoke();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void ShowNode(ExplorationDialogue node)
        {
            if (node == null) { EndDialogue(); return; }

            StopTimer();

            currentNode    = node;
            currentContext = DialogueContext.Build(node, currentMission);
            Debug.Log($"[DialogueManager] ShowNode '{node.name}' — panel={dialoguePanel != null}, panelActive={dialoguePanel?.gameObject.activeSelf}");
            dialoguePanel?.Show(node, currentContext, this);
            Debug.Log($"[DialogueManager] Après Show — panelActive={dialoguePanel?.gameObject.activeSelf}");

            if (node.hasTimeLimit)
                timerCoroutine = StartCoroutine(RunTimer(node));
        }

        private IEnumerator RunTimer(ExplorationDialogue node)
        {
            float remaining = node.timeLimitSeconds;

            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                dialoguePanel?.UpdateTimer(remaining, node.timeLimitSeconds);
                yield return null;
            }

            dialoguePanel?.UpdateTimer(0f, node.timeLimitSeconds);

            // Time's up — auto-select
            int idx = node.timeoutChoiceIndex;

            if (idx < 0 || node.choices == null || node.choices.Length == 0)
            {
                Debug.Log("[DialogueManager] Timer écoulé → OK automatique.");
                EndDialogue();
            }
            else
            {
                idx = Mathf.Clamp(idx, 0, node.choices.Length - 1);
                Debug.Log($"[DialogueManager] Timer écoulé → choix auto [{idx}].");
                OnChoiceSelected(node.choices[idx]);
            }
        }

        private void StopTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
            dialoguePanel?.HideTimer();
        }

        // ── Radio prop ────────────────────────────────────────────────────────────

        private void ShowRadioProp()
        {
            if (radioPrefab == null || handAnchor == null) return;
            if (radioInstance != null) return;   // already shown

            radioInstance = Instantiate(radioPrefab, handAnchor);
            radioInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void HideRadioProp()
        {
            if (radioInstance == null) return;
            Destroy(radioInstance);
            radioInstance = null;
        }

        private void ApplyEvent(DialogueEventData evt)
        {
            if (evt == null || evt.eventType == DialogueEventType.None) return;

            switch (evt.eventType)
            {
                case DialogueEventType.AddResource:
                    ApplyResourceEntries(evt.resources, evt.selectionMode, add: true);
                    break;

                case DialogueEventType.LoseResource:
                    ApplyResourceEntries(evt.resources, evt.selectionMode, add: false);
                    break;

                case DialogueEventType.Injury:
                    ApplyInjury();
                    break;

                case DialogueEventType.MissionReturn:
                    if (currentMission != null)
                    {
                        RadioCallManager.Instance?.RecallMission(currentMission);
                        currentMission = null;
                    }
                    break;

                case DialogueEventType.MissionContinue:
                    Debug.Log("[DialogueManager] Mission continue.");
                    break;

                case DialogueEventType.StartMission:
                    if (evt.targetMission != null)
                    {
                        RadioCallManager.Instance?.StartMissionFromData(evt.targetMission, currentMission);
                        // currentMission is now the new one — RadioCallManager handles the handoff
                        currentMission = null;
                    }
                    else
                    {
                        Debug.LogWarning("[DialogueManager] StartMission : aucune MissionData assignée.");
                    }
                    break;
            }
        }

        private void ApplyResourceEntries(ResourceEntry[] entries, ResourceSelectionMode mode, bool add)
        {
            if (entries == null || entries.Length == 0) return;

            StorageSpawner spawner = StorageSpawner.Instance;

            if (add)
            {
                if (spawner != null)
                {
                    void SpawnEntry(ResourceEntry e)
                    {
                        int amount = e.ResolveAmount();
                        spawner.SpawnItems(e.resourceType, amount);
                        currentMission?.AccumulateResource(e.resourceType, 0);
                        Debug.Log($"[DialogueManager] +{amount} {e.resourceType} sur les étagères.");
                    }

                    if (mode == ResourceSelectionMode.RandomOne)
                        SpawnEntry(entries[UnityEngine.Random.Range(0, entries.Length)]);
                    else
                        foreach (ResourceEntry e in entries) SpawnEntry(e);
                }
            }
            else
            {
                spawner?.RemoveFromEntries(entries, mode);
            }
        }

        private void ApplyInjury()
        {
            if (currentMission == null) return;
            foreach (SurvivorBehavior s in currentMission.Survivors)
            {
                s.MakeSick();
                Debug.Log($"[DialogueManager] {s.SurvivorName} blessé.");
                break;
            }
        }
    }
}


