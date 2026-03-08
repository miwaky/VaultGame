using UnityEngine;
using Unity.Cinemachine;

#if UNITY_EDITOR
using UnityEditor;

public class SceneSetupHelper : MonoBehaviour
{
    [MenuItem("Tools/Setup Tank Controls Scene")]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene", 
            "This will create a basic tank controls scene with:\n" +
            "- Ground plane\n" +
            "- Player character (Naël)\n" +
            "- 3 zones with cameras and triggers\n\n" +
            "Continue?", 
            "Yes", "Cancel"))
        {
            return;
        }

        CreateGround();
        GameObject player = CreatePlayer();
        CreateCameraSystem();
        CreateZones();

        Selection.activeGameObject = player;
        SceneView.lastActiveSceneView.FrameSelected();
        
        Debug.Log("Scene setup complete! Don't forget to assign the PlayerInputAction asset to the PlayerController!");
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5);
    }

    private static GameObject CreatePlayer()
    {
        GameObject player = new GameObject("Naël");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1, 0);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.radius = 0.3f;
        cc.height = 1.8f;
        cc.center = new Vector3(0, 0.9f, 0);

        PlayerController controller = player.AddComponent<PlayerController>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(player.transform);
        visual.transform.localPosition = new Vector3(0, 0.9f, 0);
        visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        GameObject directionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        directionIndicator.name = "Direction";
        directionIndicator.transform.SetParent(visual.transform);
        directionIndicator.transform.localPosition = new Vector3(0, 0, 0.4f);
        directionIndicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);
        
        Object.DestroyImmediate(directionIndicator.GetComponent<Collider>());

        return player;
    }

    private static void CreateCameraSystem()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        if (mainCamera.GetComponent<CinemachineBrain>() == null)
        {
            mainCamera.gameObject.AddComponent<CinemachineBrain>();
        }

        mainCamera.gameObject.AddComponent<CameraManager>();
    }

    private static void CreateZones()
    {
        CreateZone("Zone_Surveillance", new Vector3(0, 0, 0), new Vector3(10, 3, 10), 
                   new Vector3(0, 5, -8), new Vector3(20, 0, 0), 10);

        CreateZone("Zone_CouloirA", new Vector3(10, 0, 0), new Vector3(3, 3, 15), 
                   new Vector3(10, 6, 5), new Vector3(30, 180, 0), 0);

        CreateZone("Zone_CouloirB", new Vector3(0, 0, 15), new Vector3(15, 3, 3), 
                   new Vector3(-5, 6, 15), new Vector3(30, 90, 0), 0);
    }

    private static void CreateZone(string zoneName, Vector3 position, Vector3 size, 
                                   Vector3 cameraPos, Vector3 cameraRot, int priority)
    {
        GameObject zone = new GameObject(zoneName);
        zone.transform.position = position;

        GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.name = "Walls";
        walls.transform.SetParent(zone.transform);
        walls.transform.localPosition = new Vector3(0, size.y / 2, 0);
        walls.transform.localScale = size;

        GameObject vcam = new GameObject("VCam_" + zoneName.Replace("Zone_", ""));
        vcam.transform.position = cameraPos;
        vcam.transform.rotation = Quaternion.Euler(cameraRot);
        
        CinemachineCamera cam = vcam.AddComponent<CinemachineCamera>();
        
        SerializedObject camSO = new SerializedObject(cam);
        SerializedProperty priorityProp = camSO.FindProperty("Priority.m_Value");
        if (priorityProp != null)
        {
            priorityProp.intValue = priority;
            camSO.ApplyModifiedProperties();
        }

        GameObject trigger = new GameObject("Trigger_" + zoneName.Replace("Zone_", ""));
        trigger.transform.position = position;
        trigger.transform.SetParent(zone.transform);
        
        BoxCollider boxCollider = trigger.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.center = new Vector3(0, size.y / 2, 0);
        boxCollider.size = size;

        CameraTrigger cameraTrigger = trigger.AddComponent<CameraTrigger>();
        
        SerializedObject so = new SerializedObject(cameraTrigger);
        so.FindProperty("targetCamera").objectReferenceValue = cam;
        so.FindProperty("activePriority").intValue = 10;
        so.FindProperty("inactivePriority").intValue = 0;
        so.ApplyModifiedProperties();
    }
}
#endif
