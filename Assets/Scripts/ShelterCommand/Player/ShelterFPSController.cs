using UnityEngine;
using UnityEngine.InputSystem;

namespace ShelterCommand
{
    /// <summary>
    /// FPS controller for Shelter Command. Handles mouse look and WASD movement.
    /// Movement is deliberately slow — the player is confined to a small office.
    /// Can be locked externally (e.g. while reading a UI panel).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ShelterFPSController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Look Settings")]
        [SerializeField] private float mouseSensitivity = 80f;
        [SerializeField] private float verticalClampAngle = 70f;

        [Header("Move Settings")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float sprintSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController characterController;
        private float verticalRotation;
        private float verticalVelocity;
        private bool isLocked;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (isLocked) return;

            HandleLook();
            HandleMove();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Locks or unlocks player input (look + move).
        /// Does NOT change cursor visibility — callers handle that themselves.
        /// </summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }

        public bool IsLocked => isLocked;

        // ── Private ──────────────────────────────────────────────────────────────

        private void HandleLook()
        {
            if (Mouse.current == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float deltaX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float deltaY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

            // Horizontal — rotate body
            transform.Rotate(Vector3.up * deltaX);

            // Vertical — rotate camera only
            verticalRotation -= deltaY;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalClampAngle, verticalClampAngle);
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }

        private void HandleMove()
        {
            Vector3 move = Vector3.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    move += transform.forward;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    move -= transform.forward;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    move -= transform.right;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    move += transform.right;
            }

            // Gravity — clamp prevents accumulation from external physics disturbances
            if (characterController.isGrounded)
                verticalVelocity = -2f;
            else
                verticalVelocity = Mathf.Max(verticalVelocity + gravity * Time.deltaTime, gravity);

            bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

            move = move.normalized * currentSpeed;
            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }
    }
}
