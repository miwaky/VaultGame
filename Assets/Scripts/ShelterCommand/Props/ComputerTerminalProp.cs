using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// The office computer terminal. Cycles through all SecurityCamera instances in the scene.
    /// Pressing E opens/closes the terminal. Inside the terminal, Prev/Next navigate cameras.
    /// </summary>
    public class ComputerTerminalProp : MonoBehaviour, IInteractable
    {
        public string PromptLabel => isHUDOpen ? "Fermer le terminal" : "Ouvrir la surveillance";
        public bool   IsInteractable => true;

        private bool isHUDOpen;
        private ShelterHUD hud;

        private void Start()
        {
            hud = FindFirstObjectByType<ShelterHUD>();
        }

        public void Interact(OfficeInteractionSystem interactionSystem)
        {
            isHUDOpen = !isHUDOpen;

            if (isHUDOpen)
            {
                interactionSystem.SetFPSLocked(true);
                SecurityCamera[] cameras = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);

                // Auto-number labels: CAM-01, CAM-02, …
                for (int i = 0; i < cameras.Length; i++)
                    cameras[i].CameraLabel = $"CAM-{i + 1:D2}";

                hud?.OpenCameraWall(cameras);
            }
            else
            {
                hud?.CloseAllAndReturnToFPS();
                isHUDOpen = false;
            }

            Debug.Log($"[ComputerTerminalProp] Terminal {(isHUDOpen ? "ouvert" : "ferme")}.");
        }

        /// <summary>Called by ShelterHUD when the player exits the terminal from within the UI.</summary>
        public void NotifyTerminalClosed() => isHUDOpen = false;
    }
}
