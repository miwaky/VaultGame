using UnityEngine;
using Unity.Cinemachine;

#if UNITY_EDITOR
using UnityEditor;

public class SurveillanceSetupHelper : MonoBehaviour
{
    [MenuItem("Tools/Setup Surveillance System")]
    public static void SetupSurveillanceSystem()
    {
        if (!EditorUtility.DisplayDialog("Créer Système de Surveillance",
            "Créer un système de surveillance avec moniteur et caméras ?",
            "Oui", "Annuler"))
        {
            return;
        }
        
        GameObject monitorObj = CreateMonitor();
        
        GameObject[] surveillanceCams = new GameObject[3];
        surveillanceCams[0] = CreateSurveillanceCamera("SurveillanceCam_Entrance", new Vector3(5, 3, 5), new Vector3(30, -135, 0), "Caméra Entrée");
        surveillanceCams[1] = CreateSurveillanceCamera("SurveillanceCam_Hallway", new Vector3(0, 3, 10), new Vector3(30, -180, 0), "Caméra Couloir");
        surveillanceCams[2] = CreateSurveillanceCamera("SurveillanceCam_Exit", new Vector3(-5, 3, 5), new Vector3(30, -225, 0), "Caméra Sortie");
        
        SurveillanceMonitor monitor = monitorObj.GetComponent<SurveillanceMonitor>();
        if (monitor != null)
        {
            SerializedObject so = new SerializedObject(monitor);
            SerializedProperty camerasProperty = so.FindProperty("surveillanceCameras");
            
            camerasProperty.ClearArray();
            for (int i = 0; i < surveillanceCams.Length; i++)
            {
                camerasProperty.InsertArrayElementAtIndex(i);
                camerasProperty.GetArrayElementAtIndex(i).objectReferenceValue = 
                    surveillanceCams[i].GetComponent<SurveillanceCamera>();
            }
            
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerController controller = playerObj.GetComponent<PlayerController>();
                if (controller != null)
                {
                    SerializedObject playerSO = new SerializedObject(controller);
                    SerializedProperty inputActionsProperty = playerSO.FindProperty("inputActions");
                    
                    so.FindProperty("inputActions").objectReferenceValue = inputActionsProperty.objectReferenceValue;
                }
            }
            
            so.ApplyModifiedProperties();
        }
        
        Selection.activeGameObject = monitorObj;
        
        Debug.Log("Système de surveillance créé avec succès !");
        Debug.Log("- 1 Moniteur de surveillance à (0, 1, 0)");
        Debug.Log("- 3 Caméras de surveillance configurées");
        Debug.Log("Approchez-vous du moniteur et appuyez sur E pour interagir !");
    }
    
    private static GameObject CreateMonitor()
    {
        GameObject monitorObj = new GameObject("SurveillanceMonitor");
        monitorObj.transform.position = new Vector3(0, 1, 0);
        
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(monitorObj.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(1.5f, 1f, 0.1f);
        
        Material monitorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        monitorMat.color = new Color(0.2f, 0.2f, 0.2f);
        visual.GetComponent<Renderer>().material = monitorMat;
        
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "Screen";
        screen.transform.SetParent(monitorObj.transform);
        screen.transform.localPosition = new Vector3(0, 0, -0.051f);
        screen.transform.localScale = new Vector3(1.2f, 0.7f, 1f);
        screen.transform.localRotation = Quaternion.identity;
        
        Material screenMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        screenMat.color = new Color(0.1f, 0.3f, 0.5f);
        screen.GetComponent<Renderer>().material = screenMat;
        
        Object.DestroyImmediate(screen.GetComponent<Collider>());
        
        monitorObj.AddComponent<SurveillanceMonitor>();
        
        return monitorObj;
    }
    
    private static GameObject CreateSurveillanceCamera(string name, Vector3 position, Vector3 rotation, string displayName)
    {
        GameObject camObj = new GameObject(name);
        camObj.transform.position = position;
        camObj.transform.rotation = Quaternion.Euler(rotation);
        
        CinemachineCamera cinemachineCam = camObj.AddComponent<CinemachineCamera>();
        cinemachineCam.Priority = 0;
        
        SurveillanceCamera survCam = camObj.AddComponent<SurveillanceCamera>();
        
        SerializedObject so = new SerializedObject(survCam);
        so.FindProperty("activated").boolValue = true;
        so.FindProperty("cameraDisplayName").stringValue = displayName;
        so.ApplyModifiedProperties();
        
        return camObj;
    }
}
#endif
