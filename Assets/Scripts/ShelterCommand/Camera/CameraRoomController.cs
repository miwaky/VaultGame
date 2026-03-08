using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// Manages the four surveillance cameras (Dormitory, Cafeteria, Storage, Entrance).
    /// Handles camera switching, full-screen mode, and survivor click-through.
    /// </summary>
    public class CameraRoomController : MonoBehaviour
    {
        [System.Serializable]
        public class RoomCamera
        {
            public ShelterRoomType Room;
            public Camera RenderCamera;
            public RenderTexture RenderTexture;
            [HideInInspector] public string DisplayName;
        }

        [Header("Room Cameras")]
        [SerializeField] private List<RoomCamera> roomCameras = new List<RoomCamera>();

        [Header("Full-Screen Camera")]
        [SerializeField] private Camera fullScreenCamera;

        [Header("Interaction")]
        [SerializeField] private LayerMask survivorLayerMask;
        [SerializeField] private float raycastDistance = 100f;

        public event Action<ShelterRoomType> OnCameraSelected;
        public event Action OnCameraDeselected;
        public event Action<SurvivorBehavior> OnSurvivorClickedInCamera;

        private ShelterRoomType? activeRoom;
        private bool isInFullScreen;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Activates full-screen view of the given room's camera.</summary>
        public void SelectRoom(ShelterRoomType room)
        {
            activeRoom = room;
            isInFullScreen = true;

            RoomCamera rc = GetRoomCamera(room);
            if (rc != null && fullScreenCamera != null && rc.RenderCamera != null)
            {
                // Copy transform of the room camera to the full-screen camera
                fullScreenCamera.transform.SetPositionAndRotation(
                    rc.RenderCamera.transform.position,
                    rc.RenderCamera.transform.rotation);
                fullScreenCamera.gameObject.SetActive(true);
                fullScreenCamera.depth = 10;
            }

            OnCameraSelected?.Invoke(room);
            Debug.Log($"[CameraRoomController] Room selected: {room}");
        }

        /// <summary>Returns to the office view (exits full-screen camera).</summary>
        public void DeselectRoom()
        {
            activeRoom = null;
            isInFullScreen = false;

            if (fullScreenCamera != null)
            {
                fullScreenCamera.gameObject.SetActive(false);
            }

            OnCameraDeselected?.Invoke();
        }

        public bool IsInFullScreen => isInFullScreen;
        public ShelterRoomType? ActiveRoom => activeRoom;

        /// <summary>
        /// Call this from the HUD when the player clicks on the full-screen camera view.
        /// Raycasts into the room to find a survivor.
        /// </summary>
        public void TrySelectSurvivorAtScreenPoint(Vector2 screenPoint)
        {
            if (!isInFullScreen || activeRoom == null) return;

            Camera cam = fullScreenCamera != null ? fullScreenCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, survivorLayerMask))
            {
                SurvivorBehavior survivor = hit.collider.GetComponentInParent<SurvivorBehavior>();
                if (survivor != null && survivor.IsAlive)
                {
                    OnSurvivorClickedInCamera?.Invoke(survivor);
                    Debug.Log($"[CameraRoomController] Survivor clicked: {survivor.SurvivorName}");
                }
            }
        }

        /// <summary>Returns the RenderTexture for the given room for display in the HUD.</summary>
        public RenderTexture GetRenderTexture(ShelterRoomType room)
        {
            RoomCamera rc = GetRoomCamera(room);
            return rc?.RenderTexture;
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private RoomCamera GetRoomCamera(ShelterRoomType room)
        {
            foreach (RoomCamera rc in roomCameras)
            {
                if (rc.Room == room) return rc;
            }
            return null;
        }
    }
}
