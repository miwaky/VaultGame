using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// The office computer terminal prop.
    /// Pressing E opens/closes the ComputerMenuController software interface.
    /// The menu controller handles FPS locking, cursor management, and panel navigation.
    /// </summary>
    public class ComputerTerminalProp : MonoBehaviour, IInteractable
    {
        public string PromptLabel    => isMenuOpen ? "Fermer le terminal" : "Ouvrir l'ordinateur";
        public bool   IsInteractable => true;

        [Tooltip("Reference to the ComputerMenuController on the computer UI Canvas.")]
        [SerializeField] private ComputerMenuController menuController;

        private bool isMenuOpen;

        private void Start()
        {
            if (menuController == null)
                menuController = FindFirstObjectByType<ComputerMenuController>();

            if (menuController == null)
                Debug.LogError("[ComputerTerminalProp] ComputerMenuController introuvable — " +
                               "assure-toi que le Canvas ordinateur est présent dans la scène.");
        }

        /// <summary>Called by OfficeInteractionSystem when the player presses E.</summary>
        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            if (menuController == null)
            {
                menuController = FindFirstObjectByType<ComputerMenuController>();
                if (menuController == null)
                {
                    Debug.LogError("[ComputerTerminalProp] ComputerMenuController toujours introuvable.");
                    return;
                }
            }

            if (isMenuOpen)
            {
                menuController.Close();
            }
            else
            {
                isMenuOpen = true;
                menuController.Open(interactionSystem, OnMenuQuit);
                Debug.Log("[ComputerTerminalProp] Terminal ouvert.");
            }
        }

        /// <summary>Called by ComputerMenuController or ShelterHUD when the terminal is closed from within the UI.</summary>
        public void NotifyTerminalClosed() => isMenuOpen = false;

        // ── Private ─────────────────────────────────────────────────────────────

        private void OnMenuQuit()
        {
            isMenuOpen = false;
            Debug.Log("[ComputerTerminalProp] Menu fermé par le joueur.");
        }
    }
}
