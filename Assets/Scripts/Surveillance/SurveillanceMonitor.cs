using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Linq;

public class SurveillanceMonitor : MonoBehaviour
{
    [Header("Monitor Settings")]
    [SerializeField] private List<SurveillanceCamera> surveillanceCameras = new List<SurveillanceCamera>();
    [SerializeField] private int surveillanceCameraPriority = 20;
    [SerializeField] private int inactivePriority = 0;
    
    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private string playerTag = "Player";
    
    private InputActionMap playerActionMap;
    private InputAction interactAction;
    private InputAction navigateLeftAction;
    private InputAction navigateRightAction;
    
    private Transform player;
    private PlayerController playerController;
    private bool isMonitoring;
    private int currentCameraIndex;
    private List<SurveillanceCamera> activeCameras = new List<SurveillanceCamera>();
    
    private void Awake()
    {
        if (inputActions != null)
        {
            playerActionMap = inputActions.FindActionMap("Player");
            
            if (playerActionMap != null)
            {
                interactAction = playerActionMap.FindAction("Interact");
                navigateLeftAction = playerActionMap.FindAction("Turn left");
                navigateRightAction = playerActionMap.FindAction("Turn Right");
            }
        }
        
        UpdateActiveCamerasList();
    }
    
    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.started += OnInteract;
        }
        
        if (navigateLeftAction != null)
        {
            navigateLeftAction.started += OnNavigateLeft;
        }
        
        if (navigateRightAction != null)
        {
            navigateRightAction.started += OnNavigateRight;
        }
    }
    
    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteract;
        }
        
        if (navigateLeftAction != null)
        {
            navigateLeftAction.started -= OnNavigateLeft;
        }
        
        if (navigateRightAction != null)
        {
            navigateRightAction.started -= OnNavigateRight;
        }
    }
    
    private void Update()
    {
        CheckPlayerProximity();
    }
    
    private void CheckPlayerProximity()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }
    }
    
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= interactionDistance)
        {
            if (!isMonitoring)
            {
                EnterMonitoringMode();
            }
            else
            {
                ExitMonitoringMode();
            }
        }
    }
    
    private void OnNavigateLeft(InputAction.CallbackContext context)
    {
        if (!isMonitoring) return;
        
        if (activeCameras.Count == 0) return;
        
        currentCameraIndex--;
        if (currentCameraIndex < 0)
        {
            currentCameraIndex = activeCameras.Count - 1;
        }
        
        UpdateCurrentCamera();
    }
    
    private void OnNavigateRight(InputAction.CallbackContext context)
    {
        if (!isMonitoring) return;
        
        if (activeCameras.Count == 0) return;
        
        currentCameraIndex++;
        if (currentCameraIndex >= activeCameras.Count)
        {
            currentCameraIndex = 0;
        }
        
        UpdateCurrentCamera();
    }
    
    private void EnterMonitoringMode()
    {
        UpdateActiveCamerasList();
        
        if (activeCameras.Count == 0)
        {
            Debug.LogWarning("Aucune caméra de surveillance active !");
            return;
        }
        
        isMonitoring = true;
        currentCameraIndex = 0;
        
        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
        }
        
        UpdateCurrentCamera();
        
        Debug.Log($"Mode surveillance activé - {activeCameras[currentCameraIndex].DisplayName}");
    }
    
    private void ExitMonitoringMode()
    {
        isMonitoring = false;
        
        if (playerController != null)
        {
            playerController.SetControlsEnabled(true);
        }
        
        foreach (var cam in activeCameras)
        {
            if (cam != null && cam.CinemachineCamera != null)
            {
                cam.CinemachineCamera.Priority = inactivePriority;
            }
        }
        
        Debug.Log("Mode surveillance désactivé");
    }
    
    private void UpdateActiveCamerasList()
    {
        activeCameras.Clear();
        
        foreach (var cam in surveillanceCameras)
        {
            if (cam != null && cam.Activated)
            {
                activeCameras.Add(cam);
            }
        }
    }
    
    private void UpdateCurrentCamera()
    {
        if (activeCameras.Count == 0) return;
        
        for (int i = 0; i < activeCameras.Count; i++)
        {
            if (activeCameras[i] != null && activeCameras[i].CinemachineCamera != null)
            {
                activeCameras[i].CinemachineCamera.Priority = (i == currentCameraIndex) 
                    ? surveillanceCameraPriority 
                    : inactivePriority;
            }
        }
        
        Debug.Log($"Caméra active : {activeCameras[currentCameraIndex].DisplayName}");
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
    
    public void AddSurveillanceCamera(SurveillanceCamera camera)
    {
        if (!surveillanceCameras.Contains(camera))
        {
            surveillanceCameras.Add(camera);
            UpdateActiveCamerasList();
        }
    }
    
    public void RemoveSurveillanceCamera(SurveillanceCamera camera)
    {
        if (surveillanceCameras.Contains(camera))
        {
            surveillanceCameras.Remove(camera);
            UpdateActiveCamerasList();
        }
    }
}
