using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// Attached to each survivor GameObject. Handles mouse-over highlight and click detection
    /// when viewed through a surveillance camera.
    /// </summary>
    [RequireComponent(typeof(SurvivorBehavior))]
    public class SurvivorMarker : MonoBehaviour
    {
        [Header("Visual Highlight")]
        [SerializeField] private Renderer[] renderersToHighlight;
        [SerializeField] private Color highlightColor = new Color(0.2f, 0.8f, 0.2f, 1f);

        private SurvivorBehavior survivorBehavior;
        private Color[] originalColors;
        private bool isHighlighted;

        private void Awake()
        {
            survivorBehavior = GetComponent<SurvivorBehavior>();
            CacheOriginalColors();
        }

        private void OnMouseEnter()
        {
            if (!CanInteract()) return;
            SetHighlight(true);
        }

        private void OnMouseExit()
        {
            SetHighlight(false);
        }

        private void OnMouseDown()
        {
            if (!CanInteract()) return;

            CameraRoomController controller = FindFirstObjectByType<CameraRoomController>();
            if (controller != null && controller.IsInFullScreen)
            {
                controller.TrySelectSurvivorAtScreenPoint(Mouse.current.position.ReadValue());
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private bool CanInteract()
        {
            CameraRoomController controller = FindFirstObjectByType<CameraRoomController>();
            return controller != null && controller.IsInFullScreen && survivorBehavior.IsAlive;
        }

        private void SetHighlight(bool enabled)
        {
            isHighlighted = enabled;
            if (renderersToHighlight == null) return;

            for (int i = 0; i < renderersToHighlight.Length; i++)
            {
                if (renderersToHighlight[i] == null) continue;
                renderersToHighlight[i].material.color = enabled
                    ? highlightColor
                    : (originalColors != null && i < originalColors.Length
                        ? originalColors[i]
                        : Color.white);
            }
        }

        private void CacheOriginalColors()
        {
            if (renderersToHighlight == null) return;
            originalColors = new Color[renderersToHighlight.Length];
            for (int i = 0; i < renderersToHighlight.Length; i++)
            {
                if (renderersToHighlight[i] != null)
                {
                    originalColors[i] = renderersToHighlight[i].material.color;
                }
            }
        }
    }
}
