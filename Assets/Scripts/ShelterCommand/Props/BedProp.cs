using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// The office cot. Interacting with it advances to the next day and resets
    /// the in-game clock to 06:00 so DayCycleManager stays in sync with DayManager.
    /// </summary>
    public class BedProp : MonoBehaviour, IInteractable
    {
        public string PromptLabel => "Dormir (passer au jour suivant)";
        public bool IsInteractable => true;

        private ShelterGameManager gm;
        private ShelterHUD         hud;
        private DayCycleManager    dayCycleManager;

        private void Start()
        {
            gm              = ShelterGameManager.Instance;
            hud             = FindFirstObjectByType<ShelterHUD>();
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
        }

        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            if (gm == null || gm.DayManager.IsGameOver) return;

            // SkipToNextMorning handles the day advance + clock reset atomically,
            // preventing a double AdvanceDay() at the following midnight.
            if (dayCycleManager != null)
                dayCycleManager.SkipToNextMorning();
            else
                gm.RequestNextDay();   // fallback if DayCycleManager not found

            hud?.ShowNotificationPublic($"Vous dormez... Jour {gm.DayManager.CurrentDay} commence.");
            Debug.Log("[BedProp] Player slept — day advanced, clock reset to 06:00.");
        }
    }
}
