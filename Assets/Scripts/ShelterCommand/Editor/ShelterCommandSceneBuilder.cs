using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Editor tool that builds the entire Shelter Command prototype scene from scratch.
    /// Menu: Tools > Shelter Command > Build Scene
    /// </summary>
    public static class ShelterCommandSceneBuilder
    {
        // ── Layout constants ─────────────────────────────────────────────────────
        private const float RoomSize = 12f;
        private const float WallHeight = 3f;
        private const float OfficeZ = 0f;
        private const float DormZ = 20f;
        private const float CafeteriaZ = 40f;
        private const float StorageX = 20f;
        private const float StorageZ = 20f;
        private const float EntranceZ = -20f;

        // ── Materials ────────────────────────────────────────────────────────────
        private static Material wallMat;
        private static Material floorMat;
        private static Material ceilingMat;
        private static Material darkMetalMat;
        private static Material screenMat;

        [MenuItem("Tools/Shelter Command/Build Scene")]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog("Shelter Command — Build Scene",
                "Cela va construire la scène complète du prototype.\nContinuer ?",
                "Oui, construire", "Annuler")) return;

            LoadOrCreateMaterials();

            // Root object
            GameObject root = new GameObject("ShelterCommand_Root");

            // Build environment
            GameObject office = BuildOfficeRoom(root);
            GameObject dormitory = BuildRoom("Dortoir", new Vector3(0, 0, DormZ), Color.grey, root);
            GameObject cafeteria = BuildRoom("Cantine", new Vector3(0, 0, CafeteriaZ), new Color(0.4f, 0.3f, 0.2f), root);
            GameObject storage = BuildRoom("Stockage", new Vector3(StorageX, 0, StorageZ), new Color(0.3f, 0.35f, 0.3f), root);
            GameObject entrance = BuildRoom("Entree", new Vector3(0, 0, EntranceZ), new Color(0.25f, 0.25f, 0.3f), root);

            // Player / camera
            GameObject player = BuildPlayer(office);

            // Surveillance cameras in each room
            Camera dormCam = BuildRoomCamera("Cam_Dortoir", dormitory, new Vector3(0, WallHeight - 0.5f, 5), new Vector3(35, 180, 0));
            Camera cafetCam = BuildRoomCamera("Cam_Cantine", cafeteria, new Vector3(0, WallHeight - 0.5f, 5), new Vector3(35, 180, 0));
            Camera storageCam = BuildRoomCamera("Cam_Stockage", storage, new Vector3(0, WallHeight - 0.5f, 5), new Vector3(35, 180, 0));
            Camera entCam = BuildRoomCamera("Cam_Entree", entrance, new Vector3(0, WallHeight - 0.5f, 5), new Vector3(35, 180, 0));

            // Render textures
            RenderTexture dormRT = CreateRT("RT_Dortoir");
            RenderTexture cafetRT = CreateRT("RT_Cantine");
            RenderTexture storageRT = CreateRT("RT_Stockage");
            RenderTexture entRT = CreateRT("RT_Entree");

            if (dormCam) dormCam.targetTexture = dormRT;
            if (cafetCam) cafetCam.targetTexture = cafetRT;
            if (storageCam) storageCam.targetTexture = storageRT;
            if (entCam) entCam.targetTexture = entRT;

            // Survivors
            GameObject survivorsRoot = BuildSurvivors(dormitory, cafeteria, storage, entrance, root);

            // Game systems
            GameObject systems = BuildGameSystems(root, dormCam, cafetCam, storageCam, entCam,
                dormRT, cafetRT, storageRT, entRT, survivorsRoot);

            // Spawn points
            BuildSpawnPoints(dormitory, cafeteria, storage, entrance, systems);

            // Lighting
            BuildLighting(office, dormitory, cafeteria, storage, entrance, systems);

            // UI Canvas
            BuildHUDCanvas(systems, dormRT, cafetRT, storageRT, entRT);

            // CRT effect on main camera
            Camera mainCam = player.GetComponentInChildren<Camera>();
            if (mainCam != null)
            {
                AddCRTEffect(mainCam);
                mainCam.gameObject.AddComponent<PSXPostProcess>();
            }

            Selection.activeGameObject = root;
            Debug.Log("[ShelterCommandSceneBuilder] Scene built successfully!");
            EditorUtility.DisplayDialog("Succès", "La scène Shelter Command a été construite.\n\nAssignez les SurvivorData assets manuellement dans le SurvivorManager.", "OK");
        }

        // ── Environment construction ─────────────────────────────────────────────

        private static GameObject BuildOfficeRoom(GameObject parent)
        {
            GameObject room = new GameObject("Bureau");
            room.transform.SetParent(parent.transform);
            room.transform.localPosition = new Vector3(0, 0, OfficeZ);

            BuildBox(room, RoomSize, WallHeight, RoomSize, floorMat, wallMat, ceilingMat);

            // Monitor wall prop (visual only — actual UI is Canvas)
            BuildPrimitive(PrimitiveType.Cube, "MonitorWall_Prop", room,
                new Vector3(0, 1.5f, -RoomSize * 0.5f + 0.2f),
                new Vector3(8f, 2.5f, 0.1f), darkMetalMat);

            // Desk
            BuildPrimitive(PrimitiveType.Cube, "Desk", room,
                new Vector3(0, 0.4f, -RoomSize * 0.5f + 1.5f),
                new Vector3(4f, 0.8f, 1.2f), darkMetalMat);

            return room;
        }

        private static GameObject BuildRoom(string name, Vector3 localPos, Color floorTint, GameObject parent)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent.transform);
            room.transform.localPosition = localPos;

            Material roomFloor = new Material(floorMat) { color = floorTint };
            BuildBox(room, RoomSize, WallHeight, RoomSize, roomFloor, wallMat, ceilingMat);

            // Simple furniture / props
            switch (name)
            {
                case "Dortoir":
                    for (int i = -1; i <= 1; i++)
                    {
                        BuildPrimitive(PrimitiveType.Cube, $"Bed_{i}", room,
                            new Vector3(i * 3f, 0.3f, 0), new Vector3(1.2f, 0.5f, 2.5f), darkMetalMat);
                    }
                    break;
                case "Cantine":
                    BuildPrimitive(PrimitiveType.Cube, "Table", room,
                        new Vector3(0, 0.5f, 0), new Vector3(3f, 0.1f, 1.5f), darkMetalMat);
                    break;
                case "Stockage":
                    for (int x = -1; x <= 1; x++)
                    {
                        BuildPrimitive(PrimitiveType.Cube, $"Shelf_{x}", room,
                            new Vector3(x * 3f, 1f, -2f), new Vector3(2f, 2f, 0.5f), darkMetalMat);
                    }
                    break;
                case "Entree":
                    BuildPrimitive(PrimitiveType.Cube, "Vault_Door", room,
                        new Vector3(0, 1.5f, -5f), new Vector3(3f, 3f, 0.3f), darkMetalMat);
                    break;
            }

            return room;
        }

        private static void BuildBox(GameObject parent, float w, float h, float d,
            Material floor, Material wall, Material ceiling)
        {
            // Floor
            BuildPrimitive(PrimitiveType.Cube, "Floor", parent,
                new Vector3(0, -0.05f, 0), new Vector3(w, 0.1f, d), floor);
            // Ceiling
            BuildPrimitive(PrimitiveType.Cube, "Ceiling", parent,
                new Vector3(0, h + 0.05f, 0), new Vector3(w, 0.1f, d), ceiling);
            // Walls
            BuildPrimitive(PrimitiveType.Cube, "Wall_N", parent,
                new Vector3(0, h * 0.5f, d * 0.5f), new Vector3(w, h, 0.1f), wall);
            BuildPrimitive(PrimitiveType.Cube, "Wall_S", parent,
                new Vector3(0, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.1f), wall);
            BuildPrimitive(PrimitiveType.Cube, "Wall_E", parent,
                new Vector3(w * 0.5f, h * 0.5f, 0), new Vector3(0.1f, h, d), wall);
            BuildPrimitive(PrimitiveType.Cube, "Wall_W", parent,
                new Vector3(-w * 0.5f, h * 0.5f, 0), new Vector3(0.1f, h, d), wall);
        }

        // ── Player ───────────────────────────────────────────────────────────────

        private static GameObject BuildPlayer(GameObject office)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(office.transform);
            player.transform.localPosition = new Vector3(0, 0f, -3f);
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);

            // FPS controller
            ShelterFPSController fps = player.AddComponent<ShelterFPSController>();

            // Head / Camera
            GameObject head = new GameObject("Head");
            head.transform.SetParent(player.transform);
            head.transform.localPosition = new Vector3(0, 1.6f, 0);
            head.transform.localRotation = Quaternion.identity;

            Camera cam = head.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.tag = "MainCamera";

            // Wire camera reference into FPS controller
            SerializedObject fpsSO = new SerializedObject(fps);
            fpsSO.FindProperty("cameraTransform").objectReferenceValue = head.transform;
            fpsSO.ApplyModifiedProperties();

            // Interaction system on Head (uses the camera's forward)
            OfficeInteractionSystem interact = head.AddComponent<OfficeInteractionSystem>();
            SerializedObject interactSO = new SerializedObject(interact);
            interactSO.FindProperty("playerCamera").objectReferenceValue = cam;
            interactSO.FindProperty("fpsController").objectReferenceValue = fps;
            // Set interactionMask to Everything so props are found regardless of layer
            interactSO.FindProperty("interactionMask").intValue = ~0;
            interactSO.ApplyModifiedProperties();

            // Props on the desk facing the player — these open UI panels
            BuildComputerProp(office);
            BuildRadioProp(office);
            BuildMapProp(office);
            BuildBedProp(office);

            return player;
        }

        private static void BuildComputerProp(GameObject office)
        {
            // Monitor on desk — slightly raised, facing player (south)
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ComputerTerminal";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(0f, 1.0f, -RoomSize * 0.5f + 0.35f);
            go.transform.localScale = new Vector3(1.4f, 0.9f, 0.15f);
            Material m = new Material(Shader.Find("Standard")) { color = new Color(0.05f, 0.35f, 0.1f) };
            go.GetComponent<Renderer>().material = m;
            go.AddComponent<ComputerTerminalProp>();
        }

        private static void BuildRadioProp(GameObject office)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Radio";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(-2.2f, 0.85f, -RoomSize * 0.5f + 1.5f);
            go.transform.localScale = new Vector3(0.5f, 0.3f, 0.4f);
            Material m = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.2f, 0.1f) };
            go.GetComponent<Renderer>().material = m;
            go.AddComponent<RadioProp>();
        }

        private static void BuildMapProp(GameObject office)
        {
            // World map pinned to east wall
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "WorldMap";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(RoomSize * 0.5f - 0.1f, 1.5f, 0f);
            go.transform.localScale = new Vector3(0.05f, 1.8f, 2.8f);
            Material m = new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.2f, 0.35f) };
            go.GetComponent<Renderer>().material = m;
            go.AddComponent<MissionMapProp>();
        }

        private static void BuildBedProp(GameObject office)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Bed";
            go.transform.SetParent(office.transform);
            go.transform.localPosition = new Vector3(-RoomSize * 0.5f + 1.5f, 0.3f, 2f);
            go.transform.localScale = new Vector3(1.2f, 0.5f, 2.5f);
            Material m = new Material(Shader.Find("Standard")) { color = new Color(0.3f, 0.25f, 0.2f) };
            go.GetComponent<Renderer>().material = m;
            go.AddComponent<BedProp>();
        }

        // ── Cameras ──────────────────────────────────────────────────────────────

        private static Camera BuildRoomCamera(string camName, GameObject room,
            Vector3 localPos, Vector3 localRot)
        {
            GameObject go = new GameObject(camName);
            go.transform.SetParent(room.transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localRot);

            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = 80f;
            cam.nearClipPlane = 0.1f;
            cam.depth = -1;

            // Visual indicator — tiny cube marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "CamMarker";
            marker.transform.SetParent(go.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.15f;
            Material m = new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.8f, 0.1f) };
            marker.GetComponent<Renderer>().material = m;
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            return cam;
        }

        private static RenderTexture CreateRT(string rtName)
        {
            RenderTexture rt = new RenderTexture(512, 384, 16);
            rt.filterMode = FilterMode.Point;
            rt.name = rtName;
            AssetDatabase.CreateAsset(rt, $"Assets/Materials/{rtName}.renderTexture");
            return rt;
        }

        // ── Survivors ────────────────────────────────────────────────────────────

        private static GameObject BuildSurvivors(GameObject dorm, GameObject cafet,
            GameObject storage, GameObject entrance, GameObject root)
        {
            GameObject survivorsRoot = new GameObject("Survivors");
            survivorsRoot.transform.SetParent(root.transform);

            string[] names = { "Aria", "Borek", "Chloé", "Daan", "Elsa",
                               "Farid", "Gwen", "Henk", "Iris", "Joël" };

            // Spawn points per room — positions local to each room
            (GameObject room, ShelterRoomType roomType, Vector3 localOffset)[] slots =
            {
                (dorm,     ShelterRoomType.Dormitory, new Vector3(-3,  0.5f, -1)),
                (dorm,     ShelterRoomType.Dormitory, new Vector3( 0,  0.5f, -1)),
                (dorm,     ShelterRoomType.Dormitory, new Vector3( 3,  0.5f, -1)),
                (cafet,    ShelterRoomType.Cafeteria, new Vector3(-2,  0.5f,  0)),
                (cafet,    ShelterRoomType.Cafeteria, new Vector3( 2,  0.5f,  0)),
                (storage,  ShelterRoomType.Storage,   new Vector3(-2,  0.5f,  0)),
                (storage,  ShelterRoomType.Storage,   new Vector3( 2,  0.5f,  0)),
                (entrance, ShelterRoomType.Entrance,  new Vector3(-2,  0.5f,  1)),
                (entrance, ShelterRoomType.Entrance,  new Vector3( 0,  0.5f,  1)),
                (dorm,     ShelterRoomType.Dormitory, new Vector3( 0,  0.5f,  2)),
            };

            for (int i = 0; i < names.Length; i++)
            {
                var slot = slots[i];
                Vector3 worldPos = slot.room.transform.position + slot.localOffset;

                GameObject survivor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                survivor.name = names[i];
                survivor.transform.SetParent(survivorsRoot.transform);
                survivor.transform.position = worldPos;
                survivor.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);

                Color survivorColor = Color.HSVToRGB(i / 10f, 0.5f, 0.7f);
                Material mat = new Material(Shader.Find("Standard")) { color = survivorColor };
                survivor.GetComponent<Renderer>().material = mat;

                SurvivorBehavior sb = survivor.AddComponent<SurvivorBehavior>();
                // Store spawn room on the behavior for runtime use
                SerializedObject sbSO = new SerializedObject(sb);
                sbSO.FindProperty("startRoom").intValue = (int)slot.roomType;
                sbSO.ApplyModifiedProperties();

                survivor.AddComponent<SurvivorMarker>();
                survivor.layer = LayerMask.NameToLayer("Default");
            }

            Debug.Log($"[ShelterCommandSceneBuilder] Spawned {names.Length} survivors.");
            return survivorsRoot;
        }

        // ── Game Systems ─────────────────────────────────────────────────────────

        private static GameObject BuildGameSystems(GameObject parent,
            Camera dormCam, Camera cafetCam, Camera storageCam, Camera entCam,
            RenderTexture dormRT, RenderTexture cafetRT, RenderTexture storageRT, RenderTexture entRT,
            GameObject survivorsRoot)
        {
            GameObject systems = new GameObject("GameSystems");
            systems.transform.SetParent(parent.transform);

            // Resource Manager
            systems.AddComponent<ShelterResourceManager>();

            // Survivor Manager — auto-load SurvivorData assets from project
            SurvivorManager sm = systems.AddComponent<SurvivorManager>();
            string[] assetGuids = AssetDatabase.FindAssets("t:SurvivorData");
            if (assetGuids.Length > 0)
            {
                SerializedObject smSO = new SerializedObject(sm);
                SerializedProperty listProp = smSO.FindProperty("survivorDataList");
                listProp.ClearArray();
                // Sort by name so assignment order is deterministic
                List<string> guids = new List<string>(assetGuids);
                guids.Sort((a, b) =>
                {
                    string na = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(a));
                    string nb = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(b));
                    return string.Compare(na, nb, System.StringComparison.Ordinal);
                });
                for (int i = 0; i < guids.Count; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    SurvivorData sd = AssetDatabase.LoadAssetAtPath<SurvivorData>(path);
                    if (sd != null)
                    {
                        listProp.InsertArrayElementAtIndex(i);
                        listProp.GetArrayElementAtIndex(i).objectReferenceValue = sd;
                    }
                }
                smSO.ApplyModifiedProperties();
                Debug.Log($"[ShelterCommandSceneBuilder] Assigned {guids.Count} SurvivorData assets to SurvivorManager.");
            }

            // Event System
            systems.AddComponent<ShelterEventSystem>();

            // Mission System
            systems.AddComponent<MissionSystem>();

            // Day Manager
            DayManager dayManager = systems.AddComponent<DayManager>();

            // Camera Room Controller
            CameraRoomController crc = systems.AddComponent<CameraRoomController>();
            SerializedObject crcSO = new SerializedObject(crc);
            SerializedProperty roomCamProp = crcSO.FindProperty("roomCameras");
            roomCamProp.arraySize = 4;

            void SetRoomCam(int idx, ShelterRoomType room, Camera cam, RenderTexture rt)
            {
                SerializedProperty elem = roomCamProp.GetArrayElementAtIndex(idx);
                elem.FindPropertyRelative("Room").enumValueIndex = (int)room;
                elem.FindPropertyRelative("RenderCamera").objectReferenceValue = cam;
                elem.FindPropertyRelative("RenderTexture").objectReferenceValue = rt;
            }

            SetRoomCam(0, ShelterRoomType.Dormitory, dormCam, dormRT);
            SetRoomCam(1, ShelterRoomType.Cafeteria, cafetCam, cafetRT);
            SetRoomCam(2, ShelterRoomType.Storage, storageCam, storageRT);
            SetRoomCam(3, ShelterRoomType.Entrance, entCam, entRT);
            crcSO.ApplyModifiedProperties();

            // SurvivorManager reference (already added above with data list)
            SurvivorManager survivorManager = systems.GetComponent<SurvivorManager>();

            // Wire references for DayManager
            SerializedObject dmSO = new SerializedObject(dayManager);
            dmSO.FindProperty("survivorManager").objectReferenceValue = survivorManager;
            dmSO.FindProperty("resourceManager").objectReferenceValue = systems.GetComponent<ShelterResourceManager>();
            dmSO.FindProperty("eventSystem").objectReferenceValue     = systems.GetComponent<ShelterEventSystem>();
            dmSO.FindProperty("missionSystem").objectReferenceValue   = systems.GetComponent<MissionSystem>();
            dmSO.ApplyModifiedProperties();

            // Game Manager (must come last)
            ShelterGameManager gm = systems.AddComponent<ShelterGameManager>();
            SerializedObject gmSO = new SerializedObject(gm);
            gmSO.FindProperty("dayManager").objectReferenceValue = dayManager;
            gmSO.FindProperty("survivorManager").objectReferenceValue = survivorManager;
            gmSO.FindProperty("resourceManager").objectReferenceValue = systems.GetComponent<ShelterResourceManager>();
            gmSO.FindProperty("eventSystem").objectReferenceValue = systems.GetComponent<ShelterEventSystem>();
            gmSO.FindProperty("missionSystem").objectReferenceValue = systems.GetComponent<MissionSystem>();
            gmSO.FindProperty("cameraRoomController").objectReferenceValue = crc;
            gmSO.ApplyModifiedProperties();

            return systems;
        }

        // ── Spawn Points ──────────────────────────────────────────────────────────

        private static void BuildSpawnPoints(GameObject dorm, GameObject cafet,
            GameObject storage, GameObject entrance, GameObject systems)
        {
            Transform[] dormSpawns    = CreateSpawnPoints("DormSpawns", dorm, new[] {
                new Vector3(-3, 0, -2), new Vector3( 0, 0, -2), new Vector3(3, 0, -2),
                new Vector3(-3, 0,  1), new Vector3( 0, 0,  1), new Vector3(3, 0,  1),
            });
            Transform[] cafetSpawns   = CreateSpawnPoints("CafetSpawns", cafet, new[] {
                new Vector3(-2, 0, 0), new Vector3(2, 0, 0), new Vector3(0, 0, -1),
            });
            Transform[] storageSpawns = CreateSpawnPoints("StorageSpawns", storage, new[] {
                new Vector3(-2, 0, 1), new Vector3(2, 0, 1), new Vector3(0, 0, 2),
            });
            Transform[] entranceSpawns = CreateSpawnPoints("EntranceSpawns", entrance, new[] {
                new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1),
            });

            // Wire spawn points into SurvivorRoomRegistry so survivors teleport at runtime
            SurvivorRoomRegistry registry = systems.AddComponent<SurvivorRoomRegistry>();
            SerializedObject regSO = new SerializedObject(registry);
            SerializedProperty slotsProp = regSO.FindProperty("roomSlots");
            slotsProp.arraySize = 4;

            void SetSlot(int idx, ShelterRoomType room, Transform[] spawns)
            {
                SerializedProperty elem = slotsProp.GetArrayElementAtIndex(idx);
                elem.FindPropertyRelative("room").enumValueIndex = (int)room;
                SerializedProperty pts = elem.FindPropertyRelative("spawnPoints");
                pts.arraySize = spawns.Length;
                for (int i = 0; i < spawns.Length; i++)
                    pts.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            }

            SetSlot(0, ShelterRoomType.Dormitory, dormSpawns);
            SetSlot(1, ShelterRoomType.Cafeteria,  cafetSpawns);
            SetSlot(2, ShelterRoomType.Storage,    storageSpawns);
            SetSlot(3, ShelterRoomType.Entrance,   entranceSpawns);
            regSO.ApplyModifiedProperties();
        }

        private static Transform[] CreateSpawnPoints(string groupName, GameObject parent, Vector3[] localPositions)
        {
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(parent.transform);
            group.transform.localPosition = Vector3.zero;

            Transform[] spawns = new Transform[localPositions.Length];
            for (int i = 0; i < localPositions.Length; i++)
            {
                GameObject sp = new GameObject($"Spawn_{i}");
                sp.transform.SetParent(group.transform);
                sp.transform.localPosition = localPositions[i];
                spawns[i] = sp.transform;
            }
            return spawns;
        }

        // ── Lighting ──────────────────────────────────────────────────────────────

        private static void BuildLighting(GameObject office, GameObject dorm, GameObject cafet,
            GameObject storage, GameObject entrance, GameObject systems)
        {
            // Ambient
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);

            // Office main light (unstable)
            Light officeLight = CreatePointLight("OfficeLight", office,
                new Vector3(0, WallHeight - 0.3f, 0), Color.cyan, 3f, 8f);

            // Room lights
            CreatePointLight("DormLight", dorm,
                new Vector3(0, WallHeight - 0.3f, 0), new Color(0.6f, 0.4f, 0.2f), 1.5f, 12f);
            CreatePointLight("CafetLight", cafet,
                new Vector3(0, WallHeight - 0.3f, 0), new Color(0.5f, 0.5f, 0.3f), 1.5f, 12f);
            CreatePointLight("StorageLight", storage,
                new Vector3(0, WallHeight - 0.3f, 0), new Color(0.4f, 0.4f, 0.5f), 1f, 12f);
            CreatePointLight("EntranceLight", entrance,
                new Vector3(0, WallHeight - 0.3f, 0), new Color(0.3f, 0.4f, 0.3f), 1f, 12f);

            // Wire unstable lights to PSXPostProcess on main camera - done at runtime
        }

        private static Light CreatePointLight(string name, GameObject parent, Vector3 localPos,
            Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.Soft;
            return l;
        }

        // ── HUD Canvas ────────────────────────────────────────────────────────────

        private static void BuildHUDCanvas(GameObject systems,
            RenderTexture dormRT, RenderTexture cafetRT, RenderTexture storageRT, RenderTexture entRT)
        {
            GameObject canvasGo = new GameObject("HUD_Canvas");
            canvasGo.transform.SetParent(systems.transform);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Resource bar (top) — ancré en haut, hauteur 40, positionné en dedans de l'écran
            GameObject resourceBar = CreatePanel(canvasGo, "ResourceBar",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 40), new Vector2(0, -20));
            resourceBar.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.05f, 0.92f);
            SetupResourceBarTexts(resourceBar);

            // Camera wall panel (center) — caché par défaut, s'ouvre via ComputerTerminalProp
            GameObject cameraWall = CreatePanel(canvasGo, "CameraWallPanel",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            cameraWall.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            SetupCameraWall(cameraWall, dormRT, cafetRT, storageRT, entRT);
            cameraWall.SetActive(false);

            // Bottom bar supprimé — les boutons MISSIONS et DOSSIERS sont dans ActionRow du CameraWallPanel

            // Full screen camera overlay
            GameObject fullScreenPanel = CreatePanel(canvasGo, "FullScreenCameraPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fullScreenPanel.GetComponent<Image>().color = Color.black;
            SetupFullScreenPanel(fullScreenPanel);
            fullScreenPanel.SetActive(false);

            // Order panel
            GameObject orderPanel = CreatePanel(canvasGo, "OrderPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(300, 340), new Vector2(0, 185));
            orderPanel.GetComponent<Image>().color = new Color(0.03f, 0.1f, 0.05f, 0.95f);
            SetupOrderPanel(orderPanel);
            orderPanel.SetActive(false);

            // Event popup
            GameObject eventPopup = CreatePanel(canvasGo, "EventPopupPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480, 280), new Vector2(0, 0));
            eventPopup.GetComponent<Image>().color = new Color(0.1f, 0.02f, 0.02f, 0.97f);
            SetupEventPopup(eventPopup);
            eventPopup.SetActive(false);

            // Mission map panel
            GameObject missionMap = CreatePanel(canvasGo, "MissionMapPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500, 360), new Vector2(0, 0));
            missionMap.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.1f, 0.97f);
            SetupMissionMap(missionMap);
            missionMap.SetActive(false);

            // Survivor dossier panel
            GameObject dossierPanel = CreatePanel(canvasGo, "SurvivorDossierPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 400), new Vector2(0, 0));
            dossierPanel.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.97f);
            GameObject survivorEntryPrefab = BuildSurvivorEntryPrefab();
            survivorEntryPrefab.transform.SetParent(canvasGo.transform, false); // kept hidden in canvas
            SetupDossierPanel(dossierPanel);
            dossierPanel.SetActive(false);

            // Game over panel
            GameObject gameOverPanel = CreatePanel(canvasGo, "GameOverPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            gameOverPanel.GetComponent<Image>().color = new Color(0.4f, 0f, 0f, 0.85f);
            CreateLabel(gameOverPanel, "GameOverText", "L'ABRI EST TOMBÉ",
                Vector2.zero, new Vector2(0, 50));
            gameOverPanel.SetActive(false);

            // Game won panel
            GameObject gameWonPanel = CreatePanel(canvasGo, "GameWonPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            gameWonPanel.GetComponent<Image>().color = new Color(0f, 0.3f, 0.05f, 0.85f);
            CreateLabel(gameWonPanel, "GameWonText", "L'ABRI A SURVÉCU — VICTOIRE",
                Vector2.zero, new Vector2(0, 50));
            gameWonPanel.SetActive(false);

            // Mission result panel
            GameObject missionResultPanel = CreatePanel(canvasGo, "MissionResultPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 200), new Vector2(0, 100));
            missionResultPanel.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.02f, 0.97f);
            TextMeshProUGUI missionResultText = CreateLabel(missionResultPanel, "MissionResultText", "",
                Vector2.zero, new Vector2(0, 30));
            Button closeMissionResult = CreateButton(missionResultPanel, "CloseMissionResult", "FERMER",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120, 35), new Vector2(0, 15));
            missionResultPanel.SetActive(false);

            // Radio panel
            GameObject radioPanel = CreatePanel(canvasGo, "RadioPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 220), new Vector2(0, 0));
            radioPanel.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.02f, 0.97f);
            CreateLabel(radioPanel, "RadioTitle", "[ TRANSMISSION RADIO ]",
                new Vector2(0.5f, 1f), new Vector2(0f, -25f), 13f);
            TextMeshProUGUI radioMsgText = CreateLabel(radioPanel, "RadioMessage", "...",
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), 12f);
            radioMsgText.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 80);
            radioMsgText.enableWordWrapping = true;
            Button closeRadioBtn = CreateButton(radioPanel, "CloseRadioBtn", "FERMER",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120, 34), new Vector2(0, 20));
            radioPanel.SetActive(false);

            // Notification bar
            GameObject notifGo = new GameObject("Notification");
            notifGo.transform.SetParent(canvasGo.transform, false);
            RectTransform notifRect = notifGo.AddComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0, 0.5f);
            notifRect.anchorMax = new Vector2(1, 0.5f);
            notifRect.sizeDelta = new Vector2(0, 36);
            notifRect.anchoredPosition = new Vector2(0, -120);
            TextMeshProUGUI notifText = notifGo.AddComponent<TextMeshProUGUI>();
            notifText.alignment = TextAlignmentOptions.Center;
            notifText.fontSize = 14;
            notifText.color = new Color(0.8f, 1f, 0.6f);
            notifGo.SetActive(false);

            // Crosshair — small dot in center, visible in FPS mode
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvasGo.transform, false);
            RectTransform chRect = crosshair.AddComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.5f, 0.5f);
            chRect.anchorMax = new Vector2(0.5f, 0.5f);
            chRect.sizeDelta = new Vector2(8, 8);
            chRect.anchoredPosition = Vector2.zero;
            Image chImg = crosshair.AddComponent<Image>();
            chImg.color = new Color(0.7f, 1f, 0.7f, 0.9f);

            // Interaction prompt — shown when near a prop
            GameObject promptRoot = new GameObject("InteractionPrompt");
            promptRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform prRect = promptRoot.AddComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0.5f, 0.5f);
            prRect.anchorMax = new Vector2(0.5f, 0.5f);
            prRect.sizeDelta = new Vector2(320, 36);
            prRect.anchoredPosition = new Vector2(0, -80);
            promptRoot.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            GameObject promptTextGo = new GameObject("PromptText");
            promptTextGo.transform.SetParent(promptRoot.transform, false);
            RectTransform ptRect = promptTextGo.AddComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero; ptRect.anchorMax = Vector2.one; ptRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI promptTMP = promptTextGo.AddComponent<TextMeshProUGUI>();
            promptTMP.fontSize = 13;
            promptTMP.color = new Color(0.8f, 1f, 0.6f);
            promptTMP.alignment = TextAlignmentOptions.Center;
            promptRoot.SetActive(false);

            // Wire HUD
            ShelterHUD hud = canvasGo.AddComponent<ShelterHUD>();
            WireHUD(hud, canvasGo, resourceBar, cameraWall, fullScreenPanel, orderPanel,
                eventPopup, missionMap, dossierPanel, survivorEntryPrefab,
                radioPanel, radioMsgText, closeRadioBtn,
                gameOverPanel, gameWonPanel,
                missionResultPanel, missionResultText, closeMissionResult, notifText,
                crosshair, promptRoot, promptTMP);

            // Wire interaction prompt into OfficeInteractionSystem on the Head
            OfficeInteractionSystem interact = Object.FindFirstObjectByType<OfficeInteractionSystem>();
            if (interact != null)
            {
                SerializedObject interactSO = new SerializedObject(interact);
                interactSO.FindProperty("promptRoot").objectReferenceValue = promptRoot;
                interactSO.FindProperty("promptText").objectReferenceValue = promptTMP;
                interactSO.ApplyModifiedProperties();
            }
        }

        private static void SetupResourceBarTexts(GameObject bar)
        {
            string[] ids = { "FoodText", "WaterText", "MedicineText", "MaterialsText", "EnergyText", "DayText", "PopulationText" };
            string[] defaults = { "Nourriture: —", "Eau: —", "Médecine: —", "Matériaux: —", "Énergie: —%", "Jour —", "Pop: —" };
            float step = 1f / ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                float xMin = i * step;
                float xMax = (i + 1) * step;
                GameObject go = new GameObject(ids[i]);
                go.transform.SetParent(bar.transform, false);
                RectTransform rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(xMin, 0);
                rt.anchorMax = new Vector2(xMax, 1);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = defaults[i];
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 11;
                tmp.color = new Color(0.5f, 1f, 0.5f);
            }
        }

        private static void SetupCameraWall(GameObject wall,
            RenderTexture dormRT, RenderTexture cafetRT, RenderTexture storageRT, RenderTexture entRT)
        {
            // ── Layout constants ────────────────────────────────────────────────────
            // Two button rows at the bottom of the feed area (each 7% of panel height)
            // Row 1 (top row, y 0.07→0.14): MISSIONS | DOSSIERS
            // Row 2 (bottom row, y 0→0.07):  PRÉC. | SUIV. | [label] | QUITTER
            const float splitX   = 0.72f;   // camera feed / sidebar split
            const float rowBot   = 0f;
            const float rowBotH  = 0.07f;   // bottom nav row  (PRÉC / SUIV / QUITTER)
            const float rowMidH  = 0.14f;   // action row above (MISSIONS / DOSSIERS)
            const float feedBot  = 0.14f;   // feed starts above both rows

            // ── Camera feed border ────────────────────────────────────────────────
            GameObject feedBorder = new GameObject("CameraFeedBorder");
            feedBorder.transform.SetParent(wall.transform, false);
            RectTransform fbrt = feedBorder.AddComponent<RectTransform>();
            fbrt.anchorMin = new Vector2(0f, feedBot);
            fbrt.anchorMax = new Vector2(splitX, 1f);
            fbrt.sizeDelta = Vector2.zero;
            feedBorder.AddComponent<Image>().color = new Color(0.06f, 0.10f, 0.06f, 1f);

            // RawImage — receives the SecurityCamera's RenderTexture at runtime
            GameObject feedGo = new GameObject("CameraFeedImage");
            feedGo.transform.SetParent(feedBorder.transform, false);
            RectTransform firt = feedGo.AddComponent<RectTransform>();
            firt.anchorMin = new Vector2(0.005f, 0.01f);
            firt.anchorMax = new Vector2(0.995f, 0.99f);
            firt.sizeDelta = Vector2.zero;
            feedGo.AddComponent<RawImage>().color = new Color(0.85f, 1f, 0.85f);

            // Camera label — top-left corner of feed
            GameObject lblGo = new GameObject("CameraLabelText");
            lblGo.transform.SetParent(feedBorder.transform, false);
            RectTransform lrt2 = lblGo.AddComponent<RectTransform>();
            lrt2.anchorMin = new Vector2(0f, 0.96f); lrt2.anchorMax = new Vector2(0.5f, 1f);
            lrt2.sizeDelta = Vector2.zero; lrt2.anchoredPosition = Vector2.zero;
            TextMeshProUGUI lblTMP = lblGo.AddComponent<TextMeshProUGUI>();
            lblTMP.text = "CAM-XX  [1/1]";
            lblTMP.alignment = TextAlignmentOptions.TopLeft;
            lblTMP.fontSize = 10;
            lblTMP.color = new Color(0.4f, 1f, 0.4f);
            lblTMP.margin = new Vector4(6, 4, 0, 0);

            // ── Row 1 (action row): MISSIONS EXTÉRIEURES | DOSSIERS SURVIVANTS ────
            GameObject rowAction = new GameObject("ActionRow");
            rowAction.transform.SetParent(wall.transform, false);
            RectTransform rart = rowAction.AddComponent<RectTransform>();
            rart.anchorMin = new Vector2(0f,    rowBotH);
            rart.anchorMax = new Vector2(splitX, rowMidH);
            rart.sizeDelta = Vector2.zero;
            rowAction.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.04f, 1f);
            HorizontalLayoutGroup rahlg = rowAction.AddComponent<HorizontalLayoutGroup>();
            rahlg.childControlWidth  = true;  rahlg.childControlHeight  = true;
            rahlg.childForceExpandWidth = true; rahlg.childForceExpandHeight = true;
            rahlg.spacing = 2; rahlg.padding = new RectOffset(2, 2, 2, 2);

            CreateChildButton(rowAction, "OpenMissionMapBtn", "MISSIONS EXTÉRIEURES");
            CreateChildButton(rowAction, "OpenDossierBtn",    "DOSSIERS SURVIVANTS");

            // ── Row 2 (nav row): ◄ PRÉC. | SUIV. ► | [spacer] | ✕ QUITTER ────────
            GameObject rowNav = new GameObject("NavRow");
            rowNav.transform.SetParent(wall.transform, false);
            RectTransform rnrt = rowNav.AddComponent<RectTransform>();
            rnrt.anchorMin = new Vector2(0f,    rowBot);
            rnrt.anchorMax = new Vector2(splitX, rowBotH);
            rnrt.sizeDelta = Vector2.zero;
            rowNav.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.03f, 1f);
            HorizontalLayoutGroup rnhlg = rowNav.AddComponent<HorizontalLayoutGroup>();
            rnhlg.childControlWidth  = true; rnhlg.childControlHeight  = true;
            rnhlg.childForceExpandWidth = false; rnhlg.childForceExpandHeight = true;
            rnhlg.spacing = 2; rnhlg.padding = new RectOffset(2, 2, 2, 2);

            CreateChildButton(rowNav, "CamPrevBtn",        "◄ PRÉC.", fixedWidth: 110f);
            CreateChildButton(rowNav, "CamNextBtn",        "SUIV. ►", fixedWidth: 110f);
            CreateFlexSpacer(rowNav);
            CreateChildButton(rowNav, "CloseCameraWallBtn","✕ QUITTER", fixedWidth: 120f);

            // ── Right panel (28%): survivor sidebar ──────────────────────────────
            GameObject sidebar = new GameObject("SurvivorSidebar");
            sidebar.transform.SetParent(wall.transform, false);
            RectTransform sbrt = sidebar.AddComponent<RectTransform>();
            sbrt.anchorMin = new Vector2(splitX + 0.01f, 0f);
            sbrt.anchorMax = Vector2.one;
            sbrt.sizeDelta = Vector2.zero;
            sidebar.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.04f, 0.97f);

            // "RÉSIDENTS" title
            GameObject sideTitle = new GameObject("SidebarTitle");
            sideTitle.transform.SetParent(sidebar.transform, false);
            RectTransform strt = sideTitle.AddComponent<RectTransform>();
            strt.anchorMin = new Vector2(0, 1); strt.anchorMax = new Vector2(1, 1);
            strt.sizeDelta = new Vector2(0, 28); strt.anchoredPosition = new Vector2(0, -14);
            TextMeshProUGUI stmp = sideTitle.AddComponent<TextMeshProUGUI>();
            stmp.text = "RÉSIDENTS"; stmp.alignment = TextAlignmentOptions.Center;
            stmp.fontSize = 11; stmp.color = new Color(0.5f, 1f, 0.5f);

            // Scrollable survivor name list (top 60%)
            GameObject nameList = new GameObject("SurvivorNameList");
            nameList.transform.SetParent(sidebar.transform, false);
            RectTransform nlrt = nameList.AddComponent<RectTransform>();
            nlrt.anchorMin = new Vector2(0, 0.38f); nlrt.anchorMax = new Vector2(1, 0.95f);
            nlrt.sizeDelta = Vector2.zero;
            nameList.AddComponent<Image>().color = new Color(0, 0, 0, 0.08f);
            VerticalLayoutGroup nlvlg = nameList.AddComponent<VerticalLayoutGroup>();
            nlvlg.padding = new RectOffset(3, 3, 3, 3); nlvlg.spacing = 2;
            nlvlg.childControlHeight = false; nlvlg.childControlWidth = true;
            nlvlg.childForceExpandHeight = false;

            // Order panel (bottom 38%)
            GameObject sideOrder = new GameObject("SidebarOrderPanel");
            sideOrder.transform.SetParent(sidebar.transform, false);
            RectTransform sort = sideOrder.AddComponent<RectTransform>();
            sort.anchorMin = Vector2.zero; sort.anchorMax = new Vector2(1, 0.37f);
            sort.sizeDelta = Vector2.zero;
            sideOrder.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.03f, 0.98f);
            SetupSidebarOrderPanel(sideOrder);
        }

        /// <summary>Creates a button as a direct child (for HorizontalLayoutGroup rows).</summary>
        private static void CreateChildButton(GameObject parent, string name, string label,
                                               float fixedWidth = -1f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.14f, 0.08f, 1f);
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.18f, 0.35f, 0.18f, 1f);
            cb.pressedColor     = new Color(0.05f, 0.10f, 0.05f, 1f);
            btn.colors = cb;

            if (fixedWidth > 0f)
            {
                LayoutElement le = go.AddComponent<LayoutElement>();
                le.preferredWidth  = fixedWidth;
                le.flexibleWidth   = 0;
            }

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 10; tmp.color = new Color(0.5f, 1f, 0.5f);
        }

        /// <summary>Flexible spacer element for HorizontalLayoutGroup.</summary>
        private static void CreateFlexSpacer(GameObject parent)
        {
            GameObject go = new GameObject("Spacer");
            go.transform.SetParent(parent.transform, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
        }

        private static void SetupSidebarOrderPanel(GameObject panel)
        {
            // Selected survivor info
            GameObject nameGo = new GameObject("SurvivorName");
            nameGo.transform.SetParent(panel.transform, false);
            RectTransform nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0,1); nrt.anchorMax = new Vector2(1,1);
            nrt.sizeDelta = new Vector2(0,22); nrt.anchoredPosition = new Vector2(0,-11);
            TextMeshProUGUI ntmp = nameGo.AddComponent<TextMeshProUGUI>();
            ntmp.text = "— sélectionnez —"; ntmp.alignment = TextAlignmentOptions.Center;
            ntmp.fontSize = 10; ntmp.color = new Color(0.5f,1f,0.5f); ntmp.fontStyle = FontStyles.Bold;

            GameObject statsGo = new GameObject("SurvivorStats");
            statsGo.transform.SetParent(panel.transform, false);
            RectTransform srt2 = statsGo.AddComponent<RectTransform>();
            srt2.anchorMin = new Vector2(0,1); srt2.anchorMax = new Vector2(1,1);
            srt2.sizeDelta = new Vector2(0,18); srt2.anchoredPosition = new Vector2(0,-30);
            TextMeshProUGUI stmp2 = statsGo.AddComponent<TextMeshProUGUI>();
            stmp2.text = ""; stmp2.alignment = TextAlignmentOptions.Center;
            stmp2.fontSize = 8; stmp2.color = new Color(0.4f,0.8f,0.4f);

            // Order buttons (compact, 2-column)
            string[] btnNames = { "RepairGeneratorBtn","TransportResourcesBtn","CraftToolsBtn",
                                   "GoEatBtn","GoSleepBtn","GoInfirmaryBtn","ArrestBtn","PatrolBtn" };
            string[] btnLabels = { "Réparer","Transporter","Fabriquer","Manger","Dormir","Infirmerie","Arrêter","Patrouiller" };
            for (int i = 0; i < btnNames.Length; i++)
            {
                float col = (i % 2 == 0) ? 0.02f : 0.51f;
                float row = 1f - 0.18f - Mathf.Floor(i / 2f) * 0.20f;
                GameObject btn = new GameObject(btnNames[i]);
                btn.transform.SetParent(panel.transform, false);
                RectTransform brt = btn.AddComponent<RectTransform>();
                brt.anchorMin = new Vector2(col, row - 0.17f);
                brt.anchorMax = new Vector2(col + 0.47f, row);
                brt.sizeDelta = Vector2.zero;
                btn.AddComponent<Image>().color = new Color(0.1f,0.2f,0.1f,0.9f);
                Button b = btn.AddComponent<Button>();
                b.targetGraphic = btn.GetComponent<Image>();
                GameObject lblGo = new GameObject("Label");
                lblGo.transform.SetParent(btn.transform, false);
                RectTransform llrt = lblGo.AddComponent<RectTransform>();
                llrt.anchorMin = Vector2.zero; llrt.anchorMax = Vector2.one; llrt.sizeDelta = Vector2.zero;
                TextMeshProUGUI ltmp = lblGo.AddComponent<TextMeshProUGUI>();
                ltmp.text = btnLabels[i]; ltmp.fontSize = 9;
                ltmp.alignment = TextAlignmentOptions.Center;
                ltmp.color = new Color(0.7f,1f,0.7f);
            }

            // Cancel
            CreateButton(panel, "CancelBtn", "✕ Annuler",
                new Vector2(0.5f,0f), new Vector2(0.5f,0f), new Vector2(90,20), new Vector2(0,6));
        }

        private static void SetupFullScreenPanel(GameObject panel)
        {
            // Raw image fills the screen
            GameObject rawGo = new GameObject("FullScreenImage");
            rawGo.transform.SetParent(panel.transform, false);
            RectTransform rt = rawGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(0, -80);
            rt.anchoredPosition = new Vector2(0, 40);
            rawGo.AddComponent<RawImage>();
            rawGo.AddComponent<CameraMonitorUI>();

            // Room label
            CreateLabel(panel, "RoomLabel", "CAM-01",
                new Vector2(0f, 1f), new Vector2(0, -25));

            // Close button
            CreateButton(panel, "CloseFullScreenButton", "✕ FERMER",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(110, 32), new Vector2(-60, -16));

            // CRT scanline overlay
            GameObject scanlines = new GameObject("ScanlineOverlay");
            scanlines.transform.SetParent(panel.transform, false);
            RectTransform slrt = scanlines.AddComponent<RectTransform>();
            slrt.anchorMin = Vector2.zero;
            slrt.anchorMax = Vector2.one;
            slrt.sizeDelta = Vector2.zero;
            Image slImg = scanlines.AddComponent<Image>();
            slImg.color = new Color(0, 0, 0, 0.06f);
            slImg.raycastTarget = false;
        }

        private static void SetupOrderPanel(GameObject panel)
        {
            CreateLabel(panel, "SurvivorName", "SÉLECTIONNER UN SURVIVANT",
                new Vector2(0.5f, 1f), new Vector2(0, -20));
            CreateLabel(panel, "SurvivorStats", "",
                new Vector2(0.5f, 1f), new Vector2(0, -55));

            // Work group
            CreateLabel(panel, "WorkLabel", "— TRAVAIL —",
                new Vector2(0.5f, 1f), new Vector2(0, -90));
            CreateButton(panel, "RepairGeneratorBtn", "Réparer le générateur",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -115));
            CreateButton(panel, "TransportResourcesBtn", "Transporter ressources",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -148));
            CreateButton(panel, "CraftToolsBtn", "Fabriquer des outils",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -181));

            // Needs group
            CreateLabel(panel, "NeedsLabel", "— BESOINS —",
                new Vector2(0.5f, 1f), new Vector2(0, -214));
            CreateButton(panel, "GoEatBtn", "Aller manger",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -239));
            CreateButton(panel, "GoSleepBtn", "Aller dormir",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -272));
            CreateButton(panel, "GoInfirmaryBtn", "Aller à l'infirmerie",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230, 28), new Vector2(0, -305));

            // Security
            CreateLabel(panel, "SecurityLabel", "— SÉCURITÉ —",
                new Vector2(0.5f, 0f), new Vector2(0, 100));
            CreateButton(panel, "ArrestBtn", "Arrêter le survivant",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(230, 28), new Vector2(0, 72));
            CreateButton(panel, "PatrolBtn", "Surveiller la zone",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(230, 28), new Vector2(0, 44));
            CreateButton(panel, "CancelBtn", "✕ Annuler",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(110, 28), new Vector2(0, 15));
        }

        private static void SetupEventPopup(GameObject panel)
        {
            CreateLabel(panel, "EventTitle", "⚠ ÉVÉNEMENT",
                new Vector2(0.5f, 1f), new Vector2(0, -25));
            TextMeshProUGUI desc = CreateLabel(panel, "EventDescription", "Description...",
                new Vector2(0.5f, 0.5f), new Vector2(0, 20));
            desc.fontSize = 13;

            CreateButton(panel, "EventChoice0", "Choix A",
                new Vector2(0.25f, 0f), new Vector2(0.25f, 0f), new Vector2(180, 36), new Vector2(0, 20));
            CreateButton(panel, "EventChoice1", "Choix B",
                new Vector2(0.75f, 0f), new Vector2(0.75f, 0f), new Vector2(180, 36), new Vector2(0, 20));
        }

        private static void SetupMissionMap(GameObject panel)
        {
            CreateLabel(panel, "MissionMapTitle", "MISSIONS EXTÉRIEURES",
                new Vector2(0.5f, 1f), new Vector2(0, -18));

            // ── Left column : mission list ──────────────────────────────────────
            GameObject leftCol = new GameObject("MissionListCol");
            leftCol.transform.SetParent(panel.transform, false);
            RectTransform lrt = leftCol.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0,0.08f); lrt.anchorMax = new Vector2(0.48f,0.92f);
            lrt.sizeDelta = new Vector2(-4,0); lrt.anchoredPosition = new Vector2(2,0);
            leftCol.AddComponent<Image>().color = new Color(0,0,0,0.2f);
            VerticalLayoutGroup vlg = leftCol.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6,6,6,6); vlg.spacing = 6;
            vlg.childControlHeight = false; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            for (int i = 0; i < MissionSystem.AvailableMissions.Length; i++)
            {
                GameObject btnGo = new GameObject($"MissionBtn_{i}");
                btnGo.transform.SetParent(leftCol.transform, false);
                RectTransform brt = btnGo.AddComponent<RectTransform>();
                brt.sizeDelta = new Vector2(0, 36);
                btnGo.AddComponent<Image>().color = new Color(0.08f,0.15f,0.08f,0.9f);
                Button b = btnGo.AddComponent<Button>();
                b.targetGraphic = btnGo.GetComponent<Image>();
                GameObject lblGo = new GameObject("Label");
                lblGo.transform.SetParent(btnGo.transform, false);
                RectTransform llrt = lblGo.AddComponent<RectTransform>();
                llrt.anchorMin = Vector2.zero; llrt.anchorMax = Vector2.one; llrt.sizeDelta = Vector2.zero;
                TextMeshProUGUI ltmp = lblGo.AddComponent<TextMeshProUGUI>();
                ltmp.text = MissionSystem.AvailableMissions[i].LocationName;
                ltmp.fontSize = 12; ltmp.alignment = TextAlignmentOptions.Center;
                ltmp.color = new Color(0.6f,1f,0.6f);
            }

            // ── Right column : details + survivor selection ─────────────────────
            GameObject rightCol = new GameObject("MissionDetailCol");
            rightCol.transform.SetParent(panel.transform, false);
            RectTransform rrt = rightCol.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.52f,0.08f); rrt.anchorMax = new Vector2(1f,0.92f);
            rrt.sizeDelta = new Vector2(-4,0); rrt.anchoredPosition = new Vector2(-2,0);
            rightCol.AddComponent<Image>().color = new Color(0,0,0,0.2f);

            // Mission info text (top 45%)
            GameObject infoGo = new GameObject("MissionInfo");
            infoGo.transform.SetParent(rightCol.transform, false);
            RectTransform irt = infoGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0,0.55f); irt.anchorMax = new Vector2(1,1f);
            irt.sizeDelta = new Vector2(-8,0); irt.anchoredPosition = new Vector2(4,0);
            TextMeshProUGUI infoTMP = infoGo.AddComponent<TextMeshProUGUI>();
            infoTMP.text = "← Sélectionnez une mission";
            infoTMP.fontSize = 11; infoTMP.enableWordWrapping = true;
            infoTMP.color = new Color(0.5f,1f,0.5f);

            // Survivor team selection header
            GameObject teamHeader = new GameObject("TeamHeader");
            teamHeader.transform.SetParent(rightCol.transform, false);
            RectTransform thrt = teamHeader.AddComponent<RectTransform>();
            thrt.anchorMin = new Vector2(0,0.48f); thrt.anchorMax = new Vector2(1,0.55f);
            thrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI thTMP = teamHeader.AddComponent<TextMeshProUGUI>();
            thTMP.text = "ÉQUIPE (max 3) — cliquez pour sélectionner :";
            thTMP.fontSize = 9; thTMP.color = new Color(0.4f,0.8f,0.4f);
            thTMP.alignment = TextAlignmentOptions.Center;

            // Scrollable survivor selection list (bottom 45%)
            GameObject teamScrollGo = new GameObject("TeamScrollView");
            teamScrollGo.transform.SetParent(rightCol.transform, false);
            RectTransform tsrt = teamScrollGo.AddComponent<RectTransform>();
            tsrt.anchorMin = new Vector2(0,0.12f); tsrt.anchorMax = new Vector2(1,0.48f);
            tsrt.sizeDelta = Vector2.zero;
            teamScrollGo.AddComponent<Image>().color = new Color(0,0,0,0.15f);
            ScrollRect teamScroll = teamScrollGo.AddComponent<ScrollRect>();
            teamScroll.horizontal = false;

            GameObject teamMask = new GameObject("TeamMask");
            teamMask.transform.SetParent(teamScrollGo.transform, false);
            RectTransform tmrt = teamMask.AddComponent<RectTransform>();
            tmrt.anchorMin = Vector2.zero; tmrt.anchorMax = Vector2.one; tmrt.sizeDelta = Vector2.zero;
            teamMask.AddComponent<Image>().color = new Color(1,1,1,0.01f);
            teamMask.AddComponent<Mask>().showMaskGraphic = false;

            GameObject teamList = new GameObject("MissionTeamList");
            teamList.transform.SetParent(teamMask.transform, false);
            RectTransform tlrt = teamList.AddComponent<RectTransform>();
            tlrt.anchorMin = new Vector2(0,1); tlrt.anchorMax = new Vector2(1,1);
            tlrt.pivot = new Vector2(0.5f,1f); tlrt.sizeDelta = new Vector2(0,0);
            VerticalLayoutGroup tvlg = teamList.AddComponent<VerticalLayoutGroup>();
            tvlg.padding = new RectOffset(4,4,4,4); tvlg.spacing = 3;
            tvlg.childControlHeight = false; tvlg.childControlWidth = true;
            tvlg.childForceExpandHeight = false;
            ContentSizeFitter tcsf = teamList.AddComponent<ContentSizeFitter>();
            tcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            teamScroll.content = tlrt;
            teamScroll.viewport = tmrt;

            // Launch & close buttons
            CreateButton(panel, "LaunchMissionBtn", "▶ ENVOYER L'ÉQUIPE",
                new Vector2(0.76f,0f), new Vector2(0.76f,0f), new Vector2(190,32), new Vector2(0,18));
            CreateButton(panel, "CloseMissionMapBtn", "✕ FERMER",
                new Vector2(0f,0f), new Vector2(0f,0f), new Vector2(100,28), new Vector2(54,18));

            // Provision buttons (food/water adjustments)
            CreateButton(panel, "FoodProvMinusBtn",  "- Nourr.",
                new Vector2(0.52f,0f), new Vector2(0.52f,0f), new Vector2(70,28), new Vector2(40,18));
            CreateButton(panel, "FoodProvPlusBtn",   "+ Nourr.",
                new Vector2(0.52f,0f), new Vector2(0.52f,0f), new Vector2(70,28), new Vector2(115,18));
            CreateButton(panel, "WaterProvMinusBtn", "- Eau",
                new Vector2(0.52f,0f), new Vector2(0.52f,0f), new Vector2(60,28), new Vector2(192,18));
            CreateButton(panel, "WaterProvPlusBtn",  "+ Eau",
                new Vector2(0.52f,0f), new Vector2(0.52f,0f), new Vector2(60,28), new Vector2(257,18));
        }

        private static void SetupDossierPanel(GameObject panel)
        {
            CreateLabel(panel, "DossierTitle", "DOSSIERS DES SURVIVANTS",
                new Vector2(0.5f, 1f), new Vector2(0, -18));

            // ScrollView
            GameObject scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panel.transform, false);
            RectTransform svrt = scrollGo.AddComponent<RectTransform>();
            svrt.anchorMin = new Vector2(0,0.08f); svrt.anchorMax = new Vector2(1,0.92f);
            svrt.sizeDelta = new Vector2(-10,0); svrt.anchoredPosition = Vector2.zero;
            scrollGo.AddComponent<Image>().color = new Color(0,0,0,0.15f);
            ScrollRect sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;

            // Mask
            GameObject maskGo = new GameObject("ScrollArea");
            maskGo.transform.SetParent(scrollGo.transform, false);
            RectTransform mrt = maskGo.AddComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.sizeDelta = Vector2.zero;
            maskGo.AddComponent<Image>().color = new Color(1,1,1,0.01f);
            maskGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content (expands with entries)
            GameObject listContainer = new GameObject("SurvivorList");
            listContainer.transform.SetParent(maskGo.transform, false);
            RectTransform lrt2 = listContainer.AddComponent<RectTransform>();
            lrt2.anchorMin = new Vector2(0,1); lrt2.anchorMax = new Vector2(1,1);
            lrt2.pivot = new Vector2(0.5f,1f); lrt2.sizeDelta = new Vector2(0,0);
            VerticalLayoutGroup vlg2 = listContainer.AddComponent<VerticalLayoutGroup>();
            vlg2.padding = new RectOffset(6,6,6,6); vlg2.spacing = 5;
            vlg2.childControlHeight = false; vlg2.childControlWidth = true;
            vlg2.childForceExpandHeight = false;
            ContentSizeFitter csf = listContainer.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = lrt2;
            sr.viewport = mrt;

            CreateButton(panel, "CloseDossierBtn", "✕ FERMER",
                new Vector2(0.5f,0f), new Vector2(0.5f,0f), new Vector2(120,30), new Vector2(0,12));
        }

        private static void SetupBottomBar(GameObject bar) { /* supprimé — voir ActionRow dans SetupCameraWall */ }

        // ── HUD wiring via SerializedObject ──────────────────────────────────────

        private static void WireHUD(ShelterHUD hud, GameObject canvas,
            GameObject resourceBar, GameObject cameraWall,
            GameObject fullScreenPanel, GameObject orderPanel, GameObject eventPopup,
            GameObject missionMap, GameObject dossierPanel, GameObject survivorEntryPrefab,
            GameObject radioPanel, TextMeshProUGUI radioMsgText, Button closeRadioBtn,
            GameObject gameOverPanel, GameObject gameWonPanel,
            GameObject missionResultPanel, TextMeshProUGUI missionResultText,
            Button closeMissionResult, TextMeshProUGUI notifText,
            GameObject crosshair, GameObject promptRoot, TextMeshProUGUI promptTMP)
        {
            SerializedObject so = new SerializedObject(hud);

            // Resource bar
            BindTMP(so, "foodText",       resourceBar, "FoodText");
            BindTMP(so, "waterText",      resourceBar, "WaterText");
            BindTMP(so, "medicineText",   resourceBar, "MedicineText");
            BindTMP(so, "materialsText",  resourceBar, "MaterialsText");
            BindTMP(so, "energyText",     resourceBar, "EnergyText");
            BindTMP(so, "dayText",        resourceBar, "DayText");
            BindTMP(so, "populationText", resourceBar, "PopulationText");

            // ── Camera Wall (mono-viewer) ────────────────────────────────────────
            so.FindProperty("cameraWallPanel").objectReferenceValue = cameraWall;

            // Feed image and label (inside CameraFeedBorder)
            Transform feedBorder = cameraWall.transform.Find("CameraFeedBorder");
            if (feedBorder != null)
            {
                so.FindProperty("cameraFeedImage").objectReferenceValue =
                    feedBorder.Find("CameraFeedImage")?.GetComponent<RawImage>();
                so.FindProperty("cameraLabelText").objectReferenceValue =
                    feedBorder.Find("CameraLabelText")?.GetComponent<TextMeshProUGUI>();
            }

            // Prev / Next / Close navigation buttons (inside NavRow)
            BindButton(so, "camPrevButton",         cameraWall, "NavRow/CamPrevBtn");
            BindButton(so, "camNextButton",         cameraWall, "NavRow/CamNextBtn");
            BindButton(so, "closeCameraWallButton", cameraWall, "NavRow/CloseCameraWallBtn");

            // Action buttons (inside ActionRow — above nav row)
            BindButton(so, "openMissionMapButton", cameraWall, "ActionRow/OpenMissionMapBtn");
            BindButton(so, "openDossierButton",    cameraWall, "ActionRow/OpenDossierBtn");

            // Survivor sidebar (hidden by default — shown when camera is selected)
            GameObject sidebar = cameraWall.transform.Find("SurvivorSidebar")?.gameObject;
            if (sidebar != null)
            {
                so.FindProperty("survivorSidebar").objectReferenceValue = sidebar;
                so.FindProperty("survivorNameListContainer").objectReferenceValue =
                    sidebar.transform.Find("SurvivorNameList");

                GameObject sop = sidebar.transform.Find("SidebarOrderPanel")?.gameObject;
                if (sop != null)
                {
                    so.FindProperty("orderPanel").objectReferenceValue = sop;
                    BindTMP(so, "selectedSurvivorNameText",  sop, "SurvivorName");
                    BindTMP(so, "selectedSurvivorStatsText", sop, "SurvivorStats");
                    BindButton(so, "repairGeneratorButton",    sop, "RepairGeneratorBtn");
                    BindButton(so, "transportResourcesButton", sop, "TransportResourcesBtn");
                    BindButton(so, "craftToolsButton",         sop, "CraftToolsBtn");
                    BindButton(so, "goEatButton",              sop, "GoEatBtn");
                    BindButton(so, "goSleepButton",            sop, "GoSleepBtn");
                    BindButton(so, "goInfirmaryButton",        sop, "GoInfirmaryBtn");
                    BindButton(so, "arrestSurvivorButton",     sop, "ArrestBtn");
                    BindButton(so, "patrolZoneButton",         sop, "PatrolBtn");
                    BindButton(so, "cancelOrderButton",        sop, "CancelBtn");
                }
            }

            // Bottom bar supprimé — référence nulle intentionnelle

            // Event popup
            so.FindProperty("eventPopupPanel").objectReferenceValue = eventPopup;
            BindTMP(so, "eventTitleText",       eventPopup, "EventTitle");
            BindTMP(so, "eventDescriptionText", eventPopup, "EventDescription");
            BindButton(so, "eventChoice0Button", eventPopup, "EventChoice0");
            BindButton(so, "eventChoice1Button", eventPopup, "EventChoice1");
            {
                Transform c0 = eventPopup.transform.Find("EventChoice0");
                if (c0 != null) so.FindProperty("eventChoice0Label").objectReferenceValue = c0.GetComponentInChildren<TextMeshProUGUI>();
                Transform c1 = eventPopup.transform.Find("EventChoice1");
                if (c1 != null) so.FindProperty("eventChoice1Label").objectReferenceValue = c1.GetComponentInChildren<TextMeshProUGUI>();
            }

            // Mission map
            so.FindProperty("missionMapPanel").objectReferenceValue = missionMap;
            int missionCount = MissionSystem.AvailableMissions.Length;
            SerializedProperty missionBtnsArr = so.FindProperty("missionButtons");
            missionBtnsArr.arraySize = missionCount;
            // Buttons are now inside MissionListCol
            GameObject leftCol = missionMap.transform.Find("MissionListCol")?.gameObject;
            for (int i = 0; i < missionCount; i++)
            {
                Transform t = leftCol != null
                    ? leftCol.transform.Find($"MissionBtn_{i}")
                    : missionMap.transform.Find($"MissionBtn_{i}");
                if (t != null) missionBtnsArr.GetArrayElementAtIndex(i).objectReferenceValue = t.GetComponent<Button>();
            }
            // Info text and team list are inside MissionDetailCol
            GameObject rightCol = missionMap.transform.Find("MissionDetailCol")?.gameObject;
            if (rightCol != null)
            {
                so.FindProperty("missionInfoText").objectReferenceValue =
                    rightCol.transform.Find("MissionInfo")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("missionTeamListContainer").objectReferenceValue =
                    rightCol.transform.Find("TeamScrollView/TeamMask/MissionTeamList");
            }
            BindButton(so, "launchMissionButton",    missionMap, "LaunchMissionBtn");
            BindButton(so, "closeMissionMapButton",  missionMap, "CloseMissionMapBtn");
            BindButton(so, "foodProvMinusButton",    missionMap, "FoodProvMinusBtn");
            BindButton(so, "foodProvPlusButton",     missionMap, "FoodProvPlusBtn");
            BindButton(so, "waterProvMinusButton",   missionMap, "WaterProvMinusBtn");
            BindButton(so, "waterProvPlusButton",    missionMap, "WaterProvPlusBtn");

            // Dossier — scroll path is now ScrollView/ScrollArea/SurvivorList
            so.FindProperty("survivorDossierPanel").objectReferenceValue = dossierPanel;
            so.FindProperty("survivorListContainer").objectReferenceValue =
                dossierPanel.transform.Find("ScrollView/ScrollArea/SurvivorList");
            so.FindProperty("survivorEntryPrefab").objectReferenceValue = survivorEntryPrefab;
            BindButton(so, "closeDossierButton", dossierPanel, "CloseDossierBtn");

            // Radio
            so.FindProperty("radioPanel").objectReferenceValue = radioPanel;
            so.FindProperty("radioMessageText").objectReferenceValue = radioMsgText;
            so.FindProperty("closeRadioButton").objectReferenceValue = closeRadioBtn;

            // Game end
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            so.FindProperty("gameWonPanel").objectReferenceValue = gameWonPanel;

            // Mission result
            so.FindProperty("missionResultPanel").objectReferenceValue = missionResultPanel;
            so.FindProperty("missionResultText").objectReferenceValue = missionResultText;
            so.FindProperty("closeMissionResultButton").objectReferenceValue = closeMissionResult;

            // Notification & crosshair
            so.FindProperty("notificationText").objectReferenceValue = notifText;
            so.FindProperty("crosshair").objectReferenceValue = crosshair;

            so.ApplyModifiedProperties();
        }

        /// <summary>Builds a lightweight SurvivorEntry template used by the dossier list.</summary>
        private static GameObject BuildSurvivorEntryPrefab()
        {
            GameObject entry = new GameObject("SurvivorEntry_Template");
            RectTransform rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420, 60);
            entry.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.08f, 0.85f);

            SurvivorEntryUI entryUI = entry.AddComponent<SurvivorEntryUI>();

            // Name label
            GameObject nameGo = new GameObject("Name");
            nameGo.transform.SetParent(entry.transform, false);
            RectTransform nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0.45f, 1f);
            nrt.sizeDelta = new Vector2(-6, 0); nrt.anchoredPosition = new Vector2(6, 0);
            TextMeshProUGUI nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
            nameTMP.fontSize = 12; nameTMP.color = new Color(0.6f, 1f, 0.6f);
            nameTMP.fontStyle = FontStyles.Bold;

            // Stats label
            GameObject statsGo = new GameObject("Stats");
            statsGo.transform.SetParent(entry.transform, false);
            RectTransform srt = statsGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0f); srt.anchorMax = new Vector2(0.75f, 0.5f);
            srt.sizeDelta = new Vector2(-6, 0); srt.anchoredPosition = new Vector2(6, 0);
            TextMeshProUGUI statsTMP = statsGo.AddComponent<TextMeshProUGUI>();
            statsTMP.fontSize = 9; statsTMP.color = new Color(0.5f, 0.8f, 0.5f);
            statsTMP.enableWordWrapping = false;

            // Status label
            GameObject statusGo = new GameObject("Status");
            statusGo.transform.SetParent(entry.transform, false);
            RectTransform strt = statusGo.AddComponent<RectTransform>();
            strt.anchorMin = new Vector2(0.76f, 0.15f); strt.anchorMax = new Vector2(1f, 0.85f);
            strt.sizeDelta = Vector2.zero; strt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI statusTMP = statusGo.AddComponent<TextMeshProUGUI>();
            statusTMP.fontSize = 11; statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = new Color(0.3f, 1f, 0.3f);

            // Mission toggle button (right side of entry)
            GameObject toggleGo = new GameObject("MissionToggleBtn");
            toggleGo.transform.SetParent(entry.transform, false);
            RectTransform trt = toggleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.78f, 0.1f); trt.anchorMax = new Vector2(1f, 0.9f);
            trt.sizeDelta = Vector2.zero; trt.anchoredPosition = new Vector2(-4, 0);
            Image toggleImg = toggleGo.AddComponent<Image>();
            toggleImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            Button toggleBtn = toggleGo.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleImg;
            GameObject toggleLblGo = new GameObject("Label");
            toggleLblGo.transform.SetParent(toggleGo.transform, false);
            RectTransform tlrt = toggleLblGo.AddComponent<RectTransform>();
            tlrt.anchorMin = Vector2.zero; tlrt.anchorMax = Vector2.one; tlrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI toggleLbl = toggleLblGo.AddComponent<TextMeshProUGUI>();
            toggleLbl.text = "MISSION"; toggleLbl.fontSize = 9;
            toggleLbl.alignment = TextAlignmentOptions.Center;
            toggleLbl.color = new Color(0.7f, 1f, 0.7f);

            // Wire fields via SerializedObject
            SerializedObject entrySO = new SerializedObject(entryUI);
            entrySO.FindProperty("nameText").objectReferenceValue = nameTMP;
            entrySO.FindProperty("statsText").objectReferenceValue = statsTMP;
            entrySO.FindProperty("statusText").objectReferenceValue = statusTMP;
            entrySO.ApplyModifiedProperties();

            // Inactive — used as Instantiate template at runtime
            entry.SetActive(false);
            return entry;
        }

        // ── UI Helper methods ────────────────────────────────────────────────────

        private static GameObject CreatePanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.07f, 0.05f, 0.9f);
            return go;
        }

        private static TextMeshProUGUI CreateLabel(GameObject parent, string name, string text,
            Vector2 anchor, Vector2 anchoredPosition, float fontSize = 14)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(380, 30);
            rt.anchoredPosition = anchoredPosition;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.5f, 1f, 0.5f);
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button CreateButton(GameObject parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.2f, 0.08f, 0.9f);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.05f, 0.2f, 0.08f);
            cb.highlightedColor = new Color(0.1f, 0.4f, 0.15f);
            cb.pressedColor = new Color(0.0f, 0.1f, 0.04f);
            btn.colors = cb;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.color = new Color(0.7f, 1f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        // ── SerializedObject binding helpers ─────────────────────────────────────

        private static void BindTMP(SerializedObject so, string prop, GameObject container, string childPath)
        {
            Transform t = container.transform.Find(childPath);
            if (t != null)
                so.FindProperty(prop).objectReferenceValue = t.GetComponent<TextMeshProUGUI>();
        }

        private static void BindRawImage(SerializedObject so, string prop, GameObject container, string childPath)
        {
            Transform t = container.transform.Find(childPath);
            if (t != null)
                so.FindProperty(prop).objectReferenceValue = t.GetComponent<RawImage>();
        }

        private static void BindButton(SerializedObject so, string prop, GameObject container, string childPath)
        {
            Transform t = container.transform.Find(childPath);
            if (t != null)
                so.FindProperty(prop).objectReferenceValue = t.GetComponent<Button>();
            else
            {
                Button b = container.GetComponent<Button>();
                if (b != null) so.FindProperty(prop).objectReferenceValue = b;
            }
        }

        private static void BindButtonArray(SerializedObject so, string arrayProp,
            GameObject container, string[] childPaths)
        {
            SerializedProperty arr = so.FindProperty(arrayProp);
            arr.arraySize = childPaths.Length;
            for (int i = 0; i < childPaths.Length; i++)
            {
                Transform t = container.transform.Find(childPaths[i]);
                if (t != null) arr.GetArrayElementAtIndex(i).objectReferenceValue = t.GetComponent<Button>();
            }
        }

        private static void BindTMPArray(SerializedObject so, string arrayProp,
            GameObject container, string[] childPaths)
        {
            SerializedProperty arr = so.FindProperty(arrayProp);
            arr.arraySize = childPaths.Length;
            for (int i = 0; i < childPaths.Length; i++)
            {
                Transform t = container.transform.Find(childPaths[i]);
                if (t != null)
                {
                    TextMeshProUGUI tmp = t.GetComponentInChildren<TextMeshProUGUI>();
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = tmp;
                }
            }
        }

        // ── CRT Effect ────────────────────────────────────────────────────────────

        private static void AddCRTEffect(Camera cam)
        {
            Material crtMat = new Material(Shader.Find("ShelterCommand/CRTScreen"));
            if (crtMat.shader == null || !crtMat.shader.isSupported)
            {
                Debug.LogWarning("[ShelterCommandSceneBuilder] CRTScreen shader not found or not supported. Skipping CRT effect.");
                return;
            }

            AssetDatabase.CreateAsset(crtMat, "Assets/Materials/CRTScreen_Mat.mat");

            CRTEffect crtEffect = cam.gameObject.AddComponent<CRTEffect>();
            SerializedObject so = new SerializedObject(crtEffect);
            so.FindProperty("crtMaterial").objectReferenceValue = crtMat;
            so.ApplyModifiedProperties();
        }

        // ── Materials ─────────────────────────────────────────────────────────────

        private static void LoadOrCreateMaterials()
        {
            wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall_Material.mat");
            floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Floor_Material.mat");
            ceilingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ceiling_Material.mat");

            if (wallMat == null)
            {
                wallMat = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.22f, 0.2f) };
                AssetDatabase.CreateAsset(wallMat, "Assets/Materials/Wall_Material.mat");
            }
            else
            {
                wallMat.color = new Color(0.2f, 0.22f, 0.2f);
            }

            if (floorMat == null)
            {
                floorMat = new Material(Shader.Find("Standard")) { color = new Color(0.15f, 0.15f, 0.15f) };
                AssetDatabase.CreateAsset(floorMat, "Assets/Materials/Floor_Material.mat");
            }

            if (ceilingMat == null)
            {
                ceilingMat = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.12f, 0.12f) };
                AssetDatabase.CreateAsset(ceilingMat, "Assets/Materials/Ceiling_Material.mat");
            }

            darkMetalMat = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.18f, 0.18f, 0.2f)
            };
            AssetDatabase.CreateAsset(darkMetalMat, "Assets/Materials/DarkMetal_Mat.mat");

            screenMat = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.05f, 0.35f, 0.1f)
            };
            AssetDatabase.CreateAsset(screenMat, "Assets/Materials/Screen_Mat.mat");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static GameObject BuildPrimitive(PrimitiveType type, string name, GameObject parent,
            Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
