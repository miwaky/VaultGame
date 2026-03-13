using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// A possible mission the player can chain into at the end of another mission.
    /// Presented as a choice button in the final dialogue node.
    /// </summary>
    [Serializable]
    public class MissionFollowUp
    {
        [Tooltip("Label shown on the choice button.")]
        public string choiceLabel = "Continuer l'exploration";

        [Tooltip("Mission that starts if this option is chosen. Survivors carry over.")]
        public MissionData targetMission;
    }

    /// <summary>
    /// Self-contained definition of an exploration mission.
    ///
    /// One asset = one mission. Contains:
    ///   — The exploration zone (location + travel time)
    ///   — The ordered radio call sequence
    ///   — The missions the player can chain into at the end
    ///   — Whether a physical location trigger is required to start it
    ///
    /// Start via <see cref="RadioCallManager.StartMissionFromData"/> or
    /// via a <see cref="MissionTriggerZone"/> placed in the world.
    /// </summary>
    [CreateAssetMenu(menuName = "ShelterCommand/Mission/MissionData", fileName = "Mission_New")]
    public class MissionData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier used in logs and the dialogue editor.")]
        public string missionID = "mission_001";

        [Tooltip("Display name shown in UI.")]
        public string displayName = "Nouvelle mission";

        [Header("Zone")]
        [Tooltip("Exploration zone where this mission takes place.")]
        public ExplorationZone zone;

        [Header("Radio Calls")]
        [Tooltip("Fixed radio calls tied to a specific mission day (triggerDay on each asset).")]
        public RadioCallEvent[] radioCalls = Array.Empty<RadioCallEvent>();

        [Tooltip("Mission-specific encounter pool, merged with the global pool. " +
                 "Leave empty to rely solely on the global pool in RadioCallManager.")]
        public EncounterEventPool encounterPool;

        [Tooltip("Equipment the team carries for this mission. Shown in the exploration tooltip. " +
                 "Overrides zone equipment when set.")]
        public string[] equipment = System.Array.Empty<string>();

        [Header("Follow-ups")]
        [Tooltip("Missions the player can chain into when this mission ends. " +
                 "Each entry becomes a choice button in the last dialogue node. " +
                 "Leave empty to only show the 'Return home' option.")]
        public MissionFollowUp[] followUps = Array.Empty<MissionFollowUp>();

        [Header("Trigger")]
        [Tooltip("If true, this mission can only be started by a MissionTriggerZone " +
                 "in the world — it won't appear in the normal exploration panel.")]
        public bool requiresLocationTrigger = false;
    }
}
