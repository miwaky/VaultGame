using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// The office cot. Interacting with it:
    ///   1. Applies the daily consumption (food + water) for all occupants in the shelter.
    ///   2. Advances to the next day and resets the in-game clock to 06:00.
    /// </summary>
    public class BedProp : MonoBehaviour, IInteractable
    {
        public string PromptLabel => "Dormir (passer au jour suivant)";
        public bool IsInteractable => true;

        private ShelterGameManager      gm;
        private ShelterHUD              hud;
        private DayCycleManager         dayCycleManager;
        private HourlyProductionManager productionManager;

        private void Start()
        {
            gm                = ShelterGameManager.Instance;
            hud               = FindFirstObjectByType<ShelterHUD>();
            dayCycleManager   = FindFirstObjectByType<DayCycleManager>();
            productionManager = FindFirstObjectByType<HourlyProductionManager>();
        }

        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            if (gm == null || gm.DayManager.IsGameOver) return;

            // 1. Consommer les ressources pour la nuit avant d'avancer le jour
            productionManager?.TriggerSleepConsumption();

            // 2. SkipToNextMorning gère l'avancement du jour + reset horloge sans double midnight
            if (dayCycleManager != null)
                dayCycleManager.SkipToNextMorning();
            else
                gm.RequestNextDay();

            hud?.ShowNotificationPublic($"Vous dormez... Jour {gm.DayManager.CurrentDay} commence.");
            Debug.Log("[BedProp] Player slept — consumption applied, day advanced, clock reset to 06:00.");
        }
    }
}
