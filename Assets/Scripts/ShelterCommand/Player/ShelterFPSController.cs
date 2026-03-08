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

        /// <summary>Locks or unlocks player controls and cursor state.</summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
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

            // Gravity
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            move = move.normalized * moveSpeed;
            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }
    }
}
