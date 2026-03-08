using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// The world map on the office wall. Opens the mission dispatch panel.
    /// </summary>
    public class MissionMapProp : MonoBehaviour, IInteractable
    {
        public string PromptLabel => "Consulter la carte du monde";
        public bool IsInteractable => true;

        private ShelterHUD hud;

        private void Start()
        {
            hud = FindFirstObjectByType<ShelterHUD>();
        }

        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            interactionSystem.SetFPSLocked(true);
            hud?.OpenMissionMap();
            Debug.Log("[MissionMapProp] Carte du monde ouverte.");
        }
    }
}
