using UnityEngine;
using Unity.Cinemachine;

#if UNITY_EDITOR
using UnityEditor;

public class FixCameraPriorities : MonoBehaviour
{
    [MenuItem("Tools/Fix Camera Priorities")]
    public static void FixPriorities()
    {
        CinemachineCamera[] allCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        
        if (allCameras.Length == 0)
        {
            Debug.LogWarning("No Cinemachine cameras found in the scene!");
            return;
        }
        
        foreach (CinemachineCamera cam in allCameras)
        {
            SerializedObject so = new SerializedObject(cam);
            SerializedProperty priorityProp = so.FindProperty("Priority.m_Value");
            
            if (cam.name == "VCam_Surveillance")
            {
                priorityProp.intValue = 10;
                Debug.Log($"Set {cam.name} priority to 10 (Active)");
            }
            else
            {
                priorityProp.intValue = 0;
                Debug.Log($"Set {cam.name} priority to 0");
            }
            
            so.ApplyModifiedProperties();
        }
        
        Debug.Log($"Fixed priorities for {allCameras.Length} cameras!");
        EditorUtility.DisplayDialog("Success", 
            $"Camera priorities fixed!\n\n" +
            $"VCam_Surveillance: Priority 10 (Active)\n" +
            $"Other cameras: Priority 0\n\n" +
            $"You can now test in Play mode!", 
            "OK");
    }
}
#endif
