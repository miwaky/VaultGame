using System.Collections.Generic;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// The office radio. Each interaction cycles through radio messages and can trigger events.
    /// </summary>
    public class RadioProp : MonoBehaviour, IInteractable
    {
        public string PromptLabel => "Écouter la radio";
        public bool IsInteractable => true;

        private static readonly List<string> RadioMessages = new List<string>
        {
            "... statique ... des signaux ont été captés à l'est...",
            "Ici radio Delta-7. Quelqu'un reçoit ce message ? Répondez.",
            "Les températures extérieures restent mortelles. Ne sortez pas.",
            "... une communauté a été repérée à 40 km au nord ...",
            "Alerte : mouvement suspect détecté près des zones industrielles.",
            "La contamination atmosphérique diminue. Espoir pour dans 6 mois.",
            "... vous n'êtes pas seuls ... continuez à tenir ...",
        };

        private int messageIndex;
        private ShelterHUD hud;

        private void Start()
        {
            hud = FindFirstObjectByType<ShelterHUD>();
            messageIndex = 0;
        }

        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            string message = RadioMessages[messageIndex % RadioMessages.Count];
            messageIndex++;

            interactionSystem.SetFPSLocked(true);
            hud?.ShowRadioPanel(message);
            Debug.Log($"[RadioProp] Message radio : {message}");
        }
    }
}
