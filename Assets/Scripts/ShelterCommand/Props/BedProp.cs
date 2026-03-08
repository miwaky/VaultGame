using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// The office cot. Interacting with it advances to the next day after a short confirmation.
    /// </summary>
    public class BedProp : MonoBehaviour, IInteractable
    {
        public string PromptLabel => "Dormir (passer au jour suivant)";
        public bool IsInteractable => true;

        private ShelterGameManager gm;
        private ShelterHUD hud;

        private void Start()
        {
            gm = ShelterGameManager.Instance;
            hud = FindFirstObjectByType<ShelterHUD>();
        }

        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            if (gm == null || gm.DayManager.IsGameOver) return;

            gm.RequestNextDay();
            hud?.ShowNotificationPublic($"Vous dormez... Jour {gm.DayManager.CurrentDay} commence.");
            Debug.Log("[BedProp] Player slept — day advanced.");
        }
    }
}
