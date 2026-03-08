using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider))]
public class CameraTrigger : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera targetCamera;
    [SerializeField] private int activePriority = 10;
    [SerializeField] private int inactivePriority = 0;
    
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    
    private void Start()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"CameraTrigger on {gameObject.name}: Collider is not set as trigger. Setting it now.");
            triggerCollider.isTrigger = true;
        }
        
        if (targetCamera == null)
        {
            Debug.LogError($"CameraTrigger on {gameObject.name}: No target camera assigned!");
        }
        else
        {
            targetCamera.Priority = inactivePriority;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            ActivateCamera();
        }
    }
    
    private void ActivateCamera()
    {
        if (targetCamera != null)
        {
            CinemachineCamera[] allCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            
            foreach (CinemachineCamera cam in allCameras)
            {
                if (cam == targetCamera)
                {
                    cam.Priority = activePriority;
                }
                else
                {
                    cam.Priority = inactivePriority;
                }
            }
        }
    }
}
