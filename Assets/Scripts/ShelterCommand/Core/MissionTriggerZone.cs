using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Starts a <see cref="MissionData"/> when the player enters the attached trigger collider.
    ///
    /// Requires a Collider component set to Is Trigger = true.
    /// The mission uses survivors already on an active mission (carry-over) if
    /// <see cref="inheritSurvivorsFromMission"/> is set; otherwise you must wire
    /// survivors manually or rely on the exploration panel having launched one first.
    ///
    /// Typical use-case: a door, a map zone border, or a hidden area that unlocks
    /// a mission not available from the main exploration panel.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MissionTriggerZone : MonoBehaviour
    {
        [Tooltip("Mission to start when the player enters this zone.")]
        public MissionData mission;

        [Tooltip("If true, the trigger can only fire once per game session.")]
        public bool triggerOnce = true;

        [Tooltip("Optional visual indicator to hide after the trigger fires (e.g. a door frame glow).")]
        [SerializeField] private GameObject visualIndicator;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private bool hasTriggered = false;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered && triggerOnce) return;
            if (!other.CompareTag("Player")) return;
            if (mission == null)
            {
                Debug.LogWarning($"[MissionTriggerZone] '{name}' : aucune MissionData assignée.");
                return;
            }

            hasTriggered = true;

            if (visualIndicator != null)
                visualIndicator.SetActive(false);

            RadioCallManager.Instance?.StartMissionFromData(mission, survivorSource: null);
            Debug.Log($"[MissionTriggerZone] Mission '{mission.missionID}' démarrée par trigger '{name}'.");
        }
    }
}
