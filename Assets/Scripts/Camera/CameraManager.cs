using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineBrain cinemachineBrain;
    
    private void Awake()
    {
        if (cinemachineBrain == null)
        {
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
            
            if (cinemachineBrain == null)
            {
                cinemachineBrain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
            }
        }
        
        cinemachineBrain.DefaultBlend.Time = 0f;
        cinemachineBrain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Cut;
    }
}
