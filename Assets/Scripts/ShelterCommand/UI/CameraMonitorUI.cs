using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ShelterCommand
{
    /// <summary>
    /// Handles click detection on the full-screen camera RawImage.
    /// Translates screen-space clicks into world-space raycasts for survivor selection.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class CameraMonitorUI : MonoBehaviour, IPointerClickHandler
    {
        private CameraRoomController cameraController;
        private RawImage rawImage;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
        }

        private void Start()
        {
            if (ShelterGameManager.Instance != null)
            {
                cameraController = ShelterGameManager.Instance.CameraRoomController;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (cameraController == null || !cameraController.IsInFullScreen) return;
            cameraController.TrySelectSurvivorAtScreenPoint(eventData.position);
        }
    }
}
