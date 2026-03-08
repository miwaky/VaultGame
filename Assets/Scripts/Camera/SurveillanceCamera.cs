using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class SurveillanceCamera : MonoBehaviour
{
    [Header("Surveillance Settings")]
    [SerializeField] private bool activated = true;
    [SerializeField] private string cameraDisplayName = "Camera 1";
    
    public bool Activated => activated;
    public string DisplayName => cameraDisplayName;
    public CinemachineCamera CinemachineCamera { get; private set; }
    
    private void Awake()
    {
        CinemachineCamera = GetComponent<CinemachineCamera>();
    }
    
    public void SetActivated(bool value)
    {
        activated = value;
    }
}
