using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// Controls a surveillance camera's pan/tilt/zoom via keyboard and mouse input.
    /// Attach to the CameraRoot GameObject.
    ///
    /// Expected hierarchy:
    ///   CameraRoot          ← this script
    ///     CameraPivot       ← receives the Euler rotation
    ///       Camera          ← the Unity Camera with SecurityCamera component
    /// </summary>
    public class SecurityCameraController : MonoBehaviour
    {
        [Header("Pivot Reference")]
        [Tooltip("The CameraPivot child whose local rotation is driven by yaw/pitch.")]
        [SerializeField] private Transform cameraPivot;

        [Header("Rotation Limits")]
        [SerializeField] private float yawMin   = -60f;
        [SerializeField] private float yawMax   =  60f;
        [SerializeField] private float pitchMin = -25f;
        [SerializeField] private float pitchMax =  35f;

        [Header("Speed")]
        [SerializeField] private float rotationSpeed = 60f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minFOV    = 20f;
        [SerializeField] private float maxFOV    = 60f;

        // Runtime state
        private float yaw;
        private float pitch;
        private bool  isActive;
        private Camera surveillanceCamera;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>The SecurityCamera component on the child Camera GameObject.</summary>
        public SecurityCamera SecurityCamera { get; private set; }

        /// <summary>The Unity Camera used for raycasting and FOV control.</summary>
        public Camera SurveillanceCamera => surveillanceCamera;

        /// <summary>Enable or disable keyboard/mouse control (called by CameraWallPanelUI).</summary>
        public void SetActive(bool active)
        {
            isActive = active;

            if (!active)
            {
                yaw   = 0f;
                pitch = 0f;
                ApplyRotation();

                // Reset FOV to default when switching away
                if (surveillanceCamera != null)
                    surveillanceCamera.fieldOfView = maxFOV;
            }
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (cameraPivot == null)
                cameraPivot = transform.Find("CameraPivot");

            if (cameraPivot == null)
                Debug.LogError($"[SecurityCameraController] CameraPivot introuvable sur {name}.");
        }

        private void Start()
        {
            SecurityCamera    = GetComponentInChildren<SecurityCamera>();
            surveillanceCamera = GetComponentInChildren<Camera>();

            if (SecurityCamera == null)
                Debug.LogWarning($"[SecurityCameraController] Aucun SecurityCamera dans les enfants de {name}.");
            if (surveillanceCamera == null)
                Debug.LogWarning($"[SecurityCameraController] Aucun Camera dans les enfants de {name}.");
        }

        private void Update()
        {
            if (!isActive) return;

            HandleRotation();
            HandleZoom();
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private void HandleRotation()
        {
            float delta = rotationSpeed * Time.deltaTime;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.isPressed || keyboard.leftArrowKey.isPressed)  yaw -= delta;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) yaw += delta;
            if (keyboard.zKey.isPressed || keyboard.upArrowKey.isPressed)    pitch -= delta;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  pitch += delta;

            yaw   = Mathf.Clamp(yaw,   yawMin,   yawMax);
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            ApplyRotation();
        }

        private void HandleZoom()
        {
            if (surveillanceCamera == null) return;

            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            surveillanceCamera.fieldOfView -= scroll * zoomSpeed;
            surveillanceCamera.fieldOfView  = Mathf.Clamp(surveillanceCamera.fieldOfView, minFOV, maxFOV);
        }

        private void ApplyRotation()
        {
            if (cameraPivot == null) return;
            cameraPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
