using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Defines a named zone on the exploration map.
    /// The distance in days is measured from the shelter base (G5 by default).
    ///
    /// Create via: Assets > Create > ShelterCommand > ExplorationZone
    /// </summary>
    [CreateAssetMenu(menuName = "ShelterCommand/ExplorationZone", fileName = "Zone_New")]
    public class ExplorationZone : ScriptableObject
    {
        [Tooltip("Display name of this zone (e.g. 'Zone F3', 'Supermarché', 'Hôpital').")]
        public string zoneName = "Zone";

        [Tooltip("Number of days to travel from the shelter base (G5) to this zone and back.")]
        [Min(1)]
        public int daysFromBase = 2;

        [Tooltip("Short description shown when the zone is hovered or selected.")]
        [TextArea(1, 3)]
        public string description = "";

        [Tooltip("Colour tint applied to the zone button when unselected.")]
        public Color zoneColor = new Color(0.2f, 0.6f, 0.2f, 1f);

        [Tooltip("Radio calls that fire on specific mission days when exploring this zone.")]
        public RadioCallEvent[] radioCalls = System.Array.Empty<RadioCallEvent>();

        [Tooltip("Zone-specific encounter pool. Merged with the global pool in RadioCallManager. " +
                 "Leave empty to use only the global pool.")]
        public EncounterEventPool encounterPool;

        [Tooltip("Equipment the team typically carries for this zone. Shown in the exploration tooltip.")]
        public string[] equipment = new[] { "Rations (2j)", "Eau (3L)" };
    }
}
