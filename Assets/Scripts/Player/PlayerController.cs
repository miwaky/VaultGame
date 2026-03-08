using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeedMultiplier = 1.8f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Inertia Settings")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.81f;
    
    private CharacterController characterController;
    private InputActionMap playerActionMap;
    private InputAction moveForwardAction;
    private InputAction moveBackwardAction;
    private InputAction turnLeftAction;
    private InputAction turnRightAction;
    private InputAction runAction;
    
    private float currentForwardSpeed;
    private float currentRotationInput;
    private bool isRunning;
    private float verticalVelocity;
    private bool controlsEnabled = true;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        if (inputActions != null)
        {
            playerActionMap = inputActions.FindActionMap("Player");
            
            if (playerActionMap != null)
            {
                moveForwardAction = playerActionMap.FindAction("move forward");
                moveBackwardAction = playerActionMap.FindAction("Move backward");
                turnLeftAction = playerActionMap.FindAction("Turn left");
                turnRightAction = playerActionMap.FindAction("Turn Right");
                runAction = playerActionMap.FindAction("Run");
            }
        }
    }
    
    private void OnEnable()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Enable();
        }
        
        if (runAction != null)
        {
            runAction.started += OnRunStarted;
            runAction.canceled += OnRunCanceled;
        }
    }
    
    private void OnDisable()
    {
        if (runAction != null)
        {
            runAction.started -= OnRunStarted;
            runAction.canceled -= OnRunCanceled;
        }
        
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
    }
    
    private void Update()
    {
        if (controlsEnabled)
        {
            HandleRotation();
            HandleMovement();
        }
        ApplyGravity();
    }
    
    private void HandleRotation()
    {
        float turnInput = 0f;
        
        if (turnLeftAction != null && turnLeftAction.IsPressed())
        {
            turnInput = -1f;
        }
        else if (turnRightAction != null && turnRightAction.IsPressed())
        {
            turnInput = 1f;
        }
        
        currentRotationInput = turnInput;
        
        float rotation = currentRotationInput * rotationSpeed * Time.deltaTime;
        transform.Rotate(0f, rotation, 0f);
    }
    
    private void HandleMovement()
    {
        float targetSpeed = 0f;
        
        if (moveForwardAction != null && moveForwardAction.IsPressed())
        {
            targetSpeed = walkSpeed;
            if (isRunning)
            {
                targetSpeed *= runSpeedMultiplier;
            }
        }
        else if (moveBackwardAction != null && moveBackwardAction.IsPressed())
        {
            targetSpeed = -walkSpeed * 0.7f;
        }
        
        float speedChangeRate = (targetSpeed != 0f) ? acceleration : deceleration;
        currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, speedChangeRate * Time.deltaTime);
        
        Vector3 moveDirection = transform.forward * currentForwardSpeed;
        moveDirection.y = verticalVelocity;
        
        characterController.Move(moveDirection * Time.deltaTime);
    }
    
    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
    
    private void OnRunStarted(InputAction.CallbackContext context)
    {
        isRunning = true;
    }
    
    private void OnRunCanceled(InputAction.CallbackContext context)
    {
        isRunning = false;
    }
    
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        
        if (!enabled)
        {
            currentForwardSpeed = 0f;
            currentRotationInput = 0f;
            isRunning = false;
        }
    }
}
