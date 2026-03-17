using System;
using UnityEngine;

namespace ShelterCommand
{
    // ── Event types ───────────────────────────────────────────────────────────────

    public enum DialogueEventType
    {
        None,
        AddResource,
        LoseResource,
        Injury,
        MissionReturn,
        MissionContinue,
        /// <summary>
        /// Immediately starts a new mission. Survivors from the current mission carry over.
        /// Set <see cref="DialogueEventData.targetMission"/> to specify which mission to start.
        /// </summary>
        StartMission
    }

    /// <summary>
    /// How the resource type is chosen when multiple entries are listed.
    /// </summary>
    public enum ResourceSelectionMode
    {
        /// <summary>All listed resource entries are applied.</summary>
        All,
        /// <summary>One entry is picked at random (uniform).</summary>
        RandomOne
    }

    /// <summary>
    /// A single resource entry inside a <see cref="DialogueEventData"/>.
    /// Both the resource type and the quantity support optional randomisation.
    /// </summary>
    [Serializable]
    public class ResourceEntry
    {
        [Tooltip("Type of resource to add or remove.")]
        public ResourceType resourceType;

        [Tooltip("Fixed amount. Ignored when useRandomAmount is true.")]
        [Min(0)]
        public int amount = 1;

        [Tooltip("If true, picks a random int in [minAmount, maxAmount] each time.")]
        public bool useRandomAmount = false;

        [Tooltip("Minimum (inclusive) when useRandomAmount is true.")]
        [Min(0)]
        public int minAmount = 1;

        [Tooltip("Maximum (inclusive) when useRandomAmount is true.")]
        [Min(1)]
        public int maxAmount = 5;

        /// <summary>Resolves the effective amount (fixed or random) and caches it.</summary>
        public int ResolveAmount() =>
            useRandomAmount ? UnityEngine.Random.Range(minAmount, maxAmount + 1) : amount;
    }

    // ── Event data ────────────────────────────────────────────────────────────────

    [Serializable]
    public class DialogueEventData
    {
        [Tooltip("What happens when this event fires.")]
        public DialogueEventType eventType = DialogueEventType.None;

        [Tooltip("All = apply every entry. RandomOne = pick one entry at random.")]
        public ResourceSelectionMode selectionMode = ResourceSelectionMode.All;

        [Tooltip("Resource entries to add or remove (AddResource / LoseResource only).")]
        public ResourceEntry[] resources = Array.Empty<ResourceEntry>();

        [Tooltip("Mission to start (StartMission event only). Survivors carry over.")]
        public MissionData targetMission;
    }

    // ── Choice ────────────────────────────────────────────────────────────────────

    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("Text shown on the choice button. Supports {amount} and {resource} tokens.")]
        public string choiceText = "…";

        [Tooltip("Next dialogue node. Leave null to end the conversation.")]
        public ExplorationDialogue nextDialogue;

        [Tooltip("Event triggered when this choice is selected.")]
        public DialogueEventData eventTrigger;

        [Tooltip("Optional follow-up radio call scheduled when this choice ends the conversation.")]
        public RadioCallEvent followUpCall;

        [Tooltip("Days before the follow-up call fires (1 = tomorrow).")]
        [Min(1)]
        public int followUpDelayDays = 1;

        [Tooltip("Conditions required for this choice to be available. Leave empty for unconditional choices.")]
        public DialogueChoiceCondition condition;
    }

    // ── ScriptableObject ──────────────────────────────────────────────────────────

    /// <summary>
    /// A single node in an exploration dialogue tree.
    ///
    /// <b>Node event</b>: fires immediately when this node is displayed (before choices).
    /// Use it to resolve resources found and populate {amount}/{resource} tokens in the text.
    ///
    /// <b>Follow-up call</b>: when this node has no choices (OK button) and a followUpCall
    /// is set, that call is scheduled followUpDelayDays in the future once the player clicks OK.
    ///
    /// <b>Text tokens</b> (replaced at display time):
    ///   {amount}    → total items resolved by the node event
    ///   {resource}  → localised name of the first resource type
    ///   {survivors} → comma-separated names of mission survivors
    /// </summary>
    [CreateAssetMenu(menuName = "ShelterCommand/Dialogue/ExplorationDialogue", fileName = "Dialogue_New")]
    public class ExplorationDialogue : ScriptableObject
    {
        [Tooltip("Unique identifier used by DialogueEditorTool and debug logs.")]
        public string dialogueID = "dialogue_001";

        [Tooltip("Name displayed above the dialogue text (e.g. name of the explorer).")]
        public string speakerName = "Explorateur";

        [Tooltip("Body text of the radio transmission. Supports {amount}, {resource}, {survivors} tokens.")]
        [TextArea(3, 6)]
        public string dialogueText = "…";

        [Tooltip("Event fired automatically when this node is displayed (e.g. AddResource to populate {amount}).")]
        public DialogueEventData nodeEvent;

        [Tooltip("Follow-up radio call scheduled when the player clicks OK on a terminal node (no choices).")]
        public RadioCallEvent followUpCall;

        [Tooltip("Days before the follow-up call fires (1 = tomorrow).")]
        [Min(1)]
        public int followUpDelayDays = 1;

        [Header("Timer")]
        [Tooltip("If true, the player must choose before the timer runs out.")]
        public bool hasTimeLimit = false;

        [Tooltip("Seconds the player has to answer. When it hits 0, timeoutChoiceIndex is auto-selected.")]
        [Min(1f)]
        public float timeLimitSeconds = 30f;

        [Tooltip("Index in choices[] auto-selected on timeout. -1 = close dialogue (OK behaviour).")]
        public int timeoutChoiceIndex = 0;

        [Tooltip("Choices offered to the player. Leave empty to display a single [OK] button.")]
        public DialogueChoice[] choices = Array.Empty<DialogueChoice>();
    }
}
