using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Orchestre la construction de la scène Shelter Command.
    /// Chaque section est accessible via un menu indépendant ou via Build All.
    /// Menu racine : Tools > Shelter Command
    /// </summary>
    public static class ShelterCommandSceneBuilder
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const float RoomSize   = 12f;
        private const float WallHeight = 3f;
        private const float DormZ      = 20f;
        private const float CafeteriaZ = 40f;
        private const float StorageX   = 20f;
        private const float StorageZ   = 20f;
        private const float EntranceZ  = -20f;
        private const string RootName  = "ShelterCommand_Root";

        // ── Shared materials ──────────────────────────────────────────────────────
        private static Material wallMat;
        private static Material floorMat;
        private static Material ceilingMat;
        private static Material darkMetalMat;

        // ─────────────────────────────────────────────────────────────────────────
        // MENUS
        // ─────────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Shelter Command/Build All")]
        public static void BuildAll()
        {
            if (!Confirm("Construire la scène complète ?")) return;
            LoadOrCreateMaterials();
            ShareMaterials();
            GameObject root = GetOrCreateRoot();
            Section_OfficeAndPlayer(root);
            Section_ShelterRooms(root);
            Section_CamerasAndRT(root);
            Section_Survivors(root);
            Section_GameSystems(root);
            Section_Lighting(root);
            Section_HUD(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Build/1 - Office + Player")]
        public static void Menu_Office()
        {
            LoadOrCreateMaterials();
            ShareMaterials();
            GameObject root = GetOrCreateRoot();
            Section_OfficeAndPlayer(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Build/2 - Shelter Rooms")]
        public static void Menu_Rooms()
        {
            LoadOrCreateMaterials();
            GameObject root = GetOrCreateRoot();
            Section_ShelterRooms(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Build/3 - Cameras + RenderTextures")]
        public static void Menu_Cameras()
        {
            GameObject root = GetOrCreateRoot();
            Section_CamerasAndRT(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Build/4 - Survivors")]
        public static void Menu_Survivors()
        {
            GameObject root = GetOrCreateRoot();
            Section_Survivors(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Build/5 - Game Systems")]
        public static void Menu_GameSystems()
        {
            GameObject root = GetOrCreateRoot();
            Section_GameSystems(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Build/6 - Lighting")]
        public static void Menu_Lighting()
        {
            GameObject root = GetOrCreateRoot();
            Section_Lighting(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Build/7 - HUD Canvas")]
        public static void Menu_HUD()
        {
            GameObject root = GetOrCreateRoot();
            Section_HUD(root);
            Finalize(root);
        }

        [MenuItem("Tools/Shelter Command/Destroy Scene")]
        public static void DestroyScene()
        {
            if (!Confirm("Détruire toute la scène Shelter Command ?")) return;
            GameObject root = GameObject.Find(RootName);
            if (root != null) Object.DestroyImmediate(root);
            string[] temp =
            {
                "Assets/Materials/RT_Dortoir.renderTexture",
                "Assets/Materials/RT_Cantine.renderTexture",
                "Assets/Materials/RT_Stockage.renderTexture",
                "Assets/Materials/RT_Entree.renderTexture",
                "Assets/Materials/DarkMetal_Mat.mat",
            };
            foreach (string p in temp) AssetDatabase.DeleteAsset(p);
            AssetDatabase.Refresh();
            Debug.Log("[ShelterCommandSceneBuilder] Scène détruite.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // SECTIONS
        // ─────────────────────────────────────────────────────────────────────────

        private static void Section_OfficeAndPlayer(GameObject root)
        {
            ShelterSupervisorRoomBuilder.WallMat      = wallMat;
            ShelterSupervisorRoomBuilder.FloorMat     = floorMat;
            ShelterSupervisorRoomBuilder.CeilingMat   = ceilingMat;
            ShelterSupervisorRoomBuilder.DarkMetalMat = darkMetalMat;
            GameObject office = ShelterSupervisorRoomBuilder.BuildOfficeRoom(root);
            ShelterSupervisorRoomBuilder.BuildPlayer(office);
        }

        private static void Section_ShelterRooms(GameObject root)
        {
            EnsureMaterials();
            MakeRoom("Dortoir",  new Vector3(0,       0, DormZ),      Color.grey,                    root);
            MakeRoom("Cantine",  new Vector3(0,       0, CafeteriaZ), new Color(0.4f, 0.3f, 0.2f),  root);
            MakeRoom("Stockage", new Vector3(StorageX,0, StorageZ),   new Color(0.3f, 0.35f, 0.3f), root);
            MakeRoom("Entree",   new Vector3(0,       0, EntranceZ),  new Color(0.25f, 0.25f, 0.3f),root);
        }

        private static void Section_CamerasAndRT(GameObject root)
        {
            Camera dormCam    = MakeRoomCamera("Cam_Dortoir",  FindChild(root,"Dortoir"),  new Vector3(0,WallHeight-0.5f,5), new Vector3(35,180,0));
            Camera cafetCam   = MakeRoomCamera("Cam_Cantine",  FindChild(root,"Cantine"),  new Vector3(0,WallHeight-0.5f,5), new Vector3(35,180,0));
            Camera storageCam = MakeRoomCamera("Cam_Stockage", FindChild(root,"Stockage"), new Vector3(0,WallHeight-0.5f,5), new Vector3(35,180,0));
            Camera entCam     = MakeRoomCamera("Cam_Entree",   FindChild(root,"Entree"),   new Vector3(0,WallHeight-0.5f,5), new Vector3(35,180,0));

            RenderTexture dormRT    = MakeRT("RT_Dortoir");
            RenderTexture cafetRT   = MakeRT("RT_Cantine");
            RenderTexture storageRT = MakeRT("RT_Stockage");
            RenderTexture entRT     = MakeRT("RT_Entree");

            if (dormCam)    dormCam.targetTexture    = dormRT;
            if (cafetCam)   cafetCam.targetTexture   = cafetRT;
            if (storageCam) storageCam.targetTexture = storageRT;
            if (entCam)     entCam.targetTexture     = entRT;
        }

        private static void Section_Survivors(GameObject root)
        {
            GameObject dorm    = FindChild(root, "Dortoir");
            GameObject cafet   = FindChild(root, "Cantine");
            GameObject storage = FindChild(root, "Stockage");
            GameObject entrance= FindChild(root, "Entree");
            if (!dorm || !cafet || !storage || !entrance)
            {
                Debug.LogWarning("[ShelterCommandSceneBuilder] Salles manquantes — lance d'abord le menu 2.");
                return;
            }
            SpawnSurvivors(dorm, cafet, storage, entrance, root);
        }

        private static void Section_GameSystems(GameObject root)
        {
            Camera dormCam    = FindCamera(root, "Cam_Dortoir");
            Camera cafetCam   = FindCamera(root, "Cam_Cantine");
            Camera storageCam = FindCamera(root, "Cam_Stockage");
            Camera entCam     = FindCamera(root, "Cam_Entree");

            RenderTexture rtDorm    = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Dortoir.renderTexture");
            RenderTexture rtCafet   = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Cantine.renderTexture");
            RenderTexture rtStorage = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Stockage.renderTexture");
            RenderTexture rtEnt     = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Entree.renderTexture");

            BuildManagers(root, dormCam, cafetCam, storageCam, entCam, rtDorm, rtCafet, rtStorage, rtEnt);
            BuildSpawnPoints(root);
            ShelterRoomParametersBuilder.Build(GetOrCreateSystems(root));
        }

        private static void Section_Lighting(GameObject root)
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
            AddLight("OfficeLight",   FindChild(root,"SupervisorOffice"), new Vector3(0,WallHeight-0.3f,0), Color.cyan,                    3f,  8f);
            AddLight("DormLight",     FindChild(root,"Dortoir"),          new Vector3(0,WallHeight-0.3f,0), new Color(0.6f, 0.4f, 0.2f), 1.5f,12f);
            AddLight("CafetLight",    FindChild(root,"Cantine"),          new Vector3(0,WallHeight-0.3f,0), new Color(0.5f, 0.5f, 0.3f), 1.5f,12f);
            AddLight("StorageLight",  FindChild(root,"Stockage"),         new Vector3(0,WallHeight-0.3f,0), new Color(0.4f, 0.4f, 0.5f), 1f,  12f);
            AddLight("EntranceLight", FindChild(root,"Entree"),           new Vector3(0,WallHeight-0.3f,0), new Color(0.3f, 0.4f, 0.3f), 1f,  12f);
        }

        private static void Section_HUD(GameObject root)
        {
            RenderTexture dormRT    = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Dortoir.renderTexture");
            RenderTexture cafetRT   = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Cantine.renderTexture");
            RenderTexture storageRT = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Stockage.renderTexture");
            RenderTexture entRT     = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Materials/RT_Entree.renderTexture");
            ShelterSupervisorRoomBuilder.BuildHUDCanvas(GetOrCreateSystems(root), dormRT, cafetRT, storageRT, entRT);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // BUILDERS
        // ─────────────────────────────────────────────────────────────────────────

        private static void MakeRoom(string name, Vector3 worldPos, Color floorTint, GameObject parent)
        {
            if (FindChild(parent, name) != null)
            {
                Debug.LogWarning($"[ShelterCommandSceneBuilder] '{name}' existe déjà — ignoré.");
                return;
            }
            EnsureMaterials();
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent.transform);
            room.transform.localPosition = worldPos;
            Material rf = new Material(floorMat) { color = floorTint };
            MakeBox(room, RoomSize, WallHeight, RoomSize, rf, wallMat, ceilingMat);
            switch (name)
            {
                case "Dortoir":
                    for (int i = -1; i <= 1; i++)
                        MakePrimitive(PrimitiveType.Cube, $"Bed_{i+2}", room, new Vector3(i*3f,0.3f,0), new Vector3(1.2f,0.5f,2.5f), darkMetalMat);
                    break;
                case "Cantine":
                    MakePrimitive(PrimitiveType.Cube, "Table", room, new Vector3(0,0.5f,0), new Vector3(3f,0.1f,1.5f), darkMetalMat);
                    break;
                case "Stockage":
                    for (int x = -1; x <= 1; x++)
                        MakePrimitive(PrimitiveType.Cube, $"Shelf_{x+2}", room, new Vector3(x*3f,1f,-2f), new Vector3(2f,2f,0.5f), darkMetalMat);
                    break;
                case "Entree":
                    MakePrimitive(PrimitiveType.Cube, "Vault_Door", room, new Vector3(0,1.5f,-5f), new Vector3(3f,3f,0.3f), darkMetalMat);
                    break;
            }
        }

        private static void MakeBox(GameObject p, float w, float h, float d,
            Material floor, Material wall, Material ceil)
        {
            MakePrimitive(PrimitiveType.Cube,"Floor",  p, new Vector3(0,-0.05f,0),           new Vector3(w,0.1f,d), floor);
            MakePrimitive(PrimitiveType.Cube,"Ceiling",p, new Vector3(0,h+0.05f,0),          new Vector3(w,0.1f,d), ceil);
            MakePrimitive(PrimitiveType.Cube,"Wall_N", p, new Vector3(0,h*.5f,d*.5f),        new Vector3(w,h,0.1f), wall);
            MakePrimitive(PrimitiveType.Cube,"Wall_S", p, new Vector3(0,h*.5f,-d*.5f),       new Vector3(w,h,0.1f), wall);
            MakePrimitive(PrimitiveType.Cube,"Wall_E", p, new Vector3(w*.5f,h*.5f,0),        new Vector3(0.1f,h,d), wall);
            MakePrimitive(PrimitiveType.Cube,"Wall_W", p, new Vector3(-w*.5f,h*.5f,0),       new Vector3(0.1f,h,d), wall);
        }

        private static Camera MakeRoomCamera(string camName, GameObject room, Vector3 localPos, Vector3 localRot)
        {
            if (room == null)
            {
                Debug.LogWarning($"[ShelterCommandSceneBuilder] MakeRoomCamera '{camName}': salle parent null — ignoré.");
                return null;
            }
            GameObject go = new GameObject(camName);
            go.transform.SetParent(room.transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localRot);

            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView   = 80f;
            cam.nearClipPlane = 0.1f;
            cam.depth         = -1;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "CamMarker";
            marker.transform.SetParent(go.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale    = Vector3.one * 0.15f;
            marker.GetComponent<Renderer>().material =
                new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.8f, 0.1f) };
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            SecurityCamera sc = go.AddComponent<SecurityCamera>();
            sc.CameraLabel = camName;

            return cam;
        }

        private static RenderTexture MakeRT(string rtName)
        {
            string path = $"Assets/Materials/{rtName}.renderTexture";
            RenderTexture existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (existing != null) return existing;
            RenderTexture rt = new RenderTexture(512, 384, 16) { filterMode = FilterMode.Point, name = rtName };
            AssetDatabase.CreateAsset(rt, path);
            return rt;
        }

        private static void SpawnSurvivors(GameObject dorm, GameObject cafet,
            GameObject storage, GameObject entrance, GameObject root)
        {
            if (FindChild(root, "Survivors") != null)
            {
                Debug.LogWarning("[ShelterCommandSceneBuilder] Survivors existent déjà — ignorés.");
                return;
            }
            GameObject sr = new GameObject("Survivors");
            sr.transform.SetParent(root.transform);

            string[] names = { "Aria","Borek","Chloé","Daan","Elsa","Farid","Gwen","Henk","Iris","Joël" };
            (GameObject room, ShelterRoomType type, Vector3 offset)[] slots =
            {
                (dorm,    ShelterRoomType.Dormitory, new Vector3(-3,0.5f,-1)),
                (dorm,    ShelterRoomType.Dormitory, new Vector3( 0,0.5f,-1)),
                (dorm,    ShelterRoomType.Dormitory, new Vector3( 3,0.5f,-1)),
                (cafet,   ShelterRoomType.Cafeteria, new Vector3(-2,0.5f, 0)),
                (cafet,   ShelterRoomType.Cafeteria, new Vector3( 2,0.5f, 0)),
                (storage, ShelterRoomType.Storage,   new Vector3(-2,0.5f, 0)),
                (storage, ShelterRoomType.Storage,   new Vector3( 2,0.5f, 0)),
                (entrance,ShelterRoomType.Entrance,  new Vector3(-2,0.5f, 1)),
                (entrance,ShelterRoomType.Entrance,  new Vector3( 0,0.5f, 1)),
                (dorm,    ShelterRoomType.Dormitory, new Vector3( 0,0.5f, 2)),
            };

            for (int i = 0; i < names.Length; i++)
            {
                var slot = slots[i];
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                s.name = names[i];
                s.transform.SetParent(sr.transform);
                s.transform.position   = slot.room.transform.position + slot.offset;
                s.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
                s.GetComponent<Renderer>().material =
                    new Material(Shader.Find("Standard")) { color = Color.HSVToRGB(i/10f, 0.5f, 0.7f) };
                SurvivorBehavior sb = s.AddComponent<SurvivorBehavior>();
                SerializedObject so = new SerializedObject(sb);
                so.FindProperty("startRoom").intValue = (int)slot.type;
                so.ApplyModifiedProperties();
                s.AddComponent<SurvivorMarker>();
            }
            Debug.Log($"[ShelterCommandSceneBuilder] {names.Length} survivants spawnés.");
        }

        private static void BuildManagers(GameObject root,
            Camera dormCam, Camera cafetCam, Camera storageCam, Camera entCam,
            RenderTexture dormRT, RenderTexture cafetRT, RenderTexture storageRT, RenderTexture entRT)
        {
            GameObject sys = GetOrCreateSystems(root);

            var rm  = sys.GetComponent<ShelterResourceManager>() ?? sys.AddComponent<ShelterResourceManager>();
            var sm  = sys.GetComponent<SurvivorManager>()        ?? sys.AddComponent<SurvivorManager>();
            var es  = sys.GetComponent<ShelterEventSystem>()     ?? sys.AddComponent<ShelterEventSystem>();
            var ms  = sys.GetComponent<MissionSystem>()          ?? sys.AddComponent<MissionSystem>();
            var dm  = sys.GetComponent<DayManager>()             ?? sys.AddComponent<DayManager>();

            // Auto-assign SurvivorData
            string[] guids = AssetDatabase.FindAssets("t:SurvivorData");
            if (guids.Length > 0)
            {
                SerializedObject smSO = new SerializedObject(sm);
                SerializedProperty list = smSO.FindProperty("survivorDataList");
                list.ClearArray();
                List<string> sorted = new List<string>(guids);
                sorted.Sort((a, b) => string.Compare(
                    System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(a)),
                    System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(b)),
                    System.StringComparison.Ordinal));
                for (int i = 0; i < sorted.Count; i++)
                {
                    SurvivorData sd = AssetDatabase.LoadAssetAtPath<SurvivorData>(AssetDatabase.GUIDToAssetPath(sorted[i]));
                    if (!sd) continue;
                    list.InsertArrayElementAtIndex(i);
                    list.GetArrayElementAtIndex(i).objectReferenceValue = sd;
                }
                smSO.ApplyModifiedProperties();
            }

            // CameraRoomController
            var crc = sys.GetComponent<CameraRoomController>() ?? sys.AddComponent<CameraRoomController>();
            {
                SerializedObject crcSO = new SerializedObject(crc);
                SerializedProperty rcp = crcSO.FindProperty("roomCameras");
                rcp.arraySize = 4;
                SetRoomCam(rcp, 0, ShelterRoomType.Dormitory, dormCam,    dormRT);
                SetRoomCam(rcp, 1, ShelterRoomType.Cafeteria, cafetCam,   cafetRT);
                SetRoomCam(rcp, 2, ShelterRoomType.Storage,   storageCam, storageRT);
                SetRoomCam(rcp, 3, ShelterRoomType.Entrance,  entCam,     entRT);
                crcSO.ApplyModifiedProperties();
            }

            // DayManager wiring
            {
                SerializedObject dmSO = new SerializedObject(dm);
                dmSO.FindProperty("survivorManager").objectReferenceValue = sm;
                dmSO.FindProperty("resourceManager").objectReferenceValue = rm;
                dmSO.FindProperty("eventSystem").objectReferenceValue     = es;
                dmSO.FindProperty("missionSystem").objectReferenceValue   = ms;
                dmSO.ApplyModifiedProperties();
            }

            // ShelterGameManager
            var gm = sys.GetComponent<ShelterGameManager>() ?? sys.AddComponent<ShelterGameManager>();
            {
                SerializedObject gmSO = new SerializedObject(gm);
                gmSO.FindProperty("dayManager").objectReferenceValue           = dm;
                gmSO.FindProperty("survivorManager").objectReferenceValue      = sm;
                gmSO.FindProperty("resourceManager").objectReferenceValue      = rm;
                gmSO.FindProperty("eventSystem").objectReferenceValue          = es;
                gmSO.FindProperty("missionSystem").objectReferenceValue        = ms;
                gmSO.FindProperty("cameraRoomController").objectReferenceValue = crc;
                gmSO.ApplyModifiedProperties();
            }
        }

        private static void SetRoomCam(SerializedProperty rcp, int idx,
            ShelterRoomType room, Camera cam, RenderTexture rt)
        {
            SerializedProperty e = rcp.GetArrayElementAtIndex(idx);
            e.FindPropertyRelative("Room").enumValueIndex              = (int)room;
            e.FindPropertyRelative("RenderCamera").objectReferenceValue  = cam;
            e.FindPropertyRelative("RenderTexture").objectReferenceValue = rt;
        }

        private static void BuildSpawnPoints(GameObject root)
        {
            GameObject sys     = GetOrCreateSystems(root);
            GameObject dorm    = FindChild(root, "Dortoir");
            GameObject cafet   = FindChild(root, "Cantine");
            GameObject storage = FindChild(root, "Stockage");
            GameObject entrance= FindChild(root, "Entree");
            if (!dorm || !cafet || !storage || !entrance) return;

            Transform[] ds = MakeSpawnGroup("DormSpawns",     dorm,    new[]{new Vector3(-3,0,-2),new Vector3(0,0,-2),new Vector3(3,0,-2),new Vector3(-3,0,1),new Vector3(0,0,1),new Vector3(3,0,1)});
            Transform[] cs = MakeSpawnGroup("CafetSpawns",    cafet,   new[]{new Vector3(-2,0,0),new Vector3(2,0,0),new Vector3(0,0,-1)});
            Transform[] ss = MakeSpawnGroup("StorageSpawns",  storage, new[]{new Vector3(-2,0,1),new Vector3(2,0,1),new Vector3(0,0,2)});
            Transform[] es = MakeSpawnGroup("EntranceSpawns", entrance,new[]{new Vector3(-1,0,0),new Vector3(1,0,0),new Vector3(0,0,1)});

            var reg = sys.GetComponent<SurvivorRoomRegistry>() ?? sys.AddComponent<SurvivorRoomRegistry>();
            SerializedObject regSO = new SerializedObject(reg);
            SerializedProperty slots = regSO.FindProperty("roomSlots");
            slots.arraySize = 4;
            SetSpawnSlot(slots, 0, ShelterRoomType.Dormitory, ds);
            SetSpawnSlot(slots, 1, ShelterRoomType.Cafeteria,  cs);
            SetSpawnSlot(slots, 2, ShelterRoomType.Storage,    ss);
            SetSpawnSlot(slots, 3, ShelterRoomType.Entrance,   es);
            regSO.ApplyModifiedProperties();
        }

        private static void SetSpawnSlot(SerializedProperty slots, int idx,
            ShelterRoomType room, Transform[] spawns)
        {
            SerializedProperty e = slots.GetArrayElementAtIndex(idx);
            e.FindPropertyRelative("room").enumValueIndex = (int)room;
            SerializedProperty pts = e.FindPropertyRelative("spawnPoints");
            pts.arraySize = spawns.Length;
            for (int i = 0; i < spawns.Length; i++)
                pts.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
        }

        private static Transform[] MakeSpawnGroup(string groupName, GameObject parent, Vector3[] positions)
        {
            Transform old = parent.transform.Find(groupName);
            if (old) Object.DestroyImmediate(old.gameObject);
            GameObject g = new GameObject(groupName);
            g.transform.SetParent(parent.transform);
            g.transform.localPosition = Vector3.zero;
            Transform[] result = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject sp = new GameObject($"Spawn_{i}");
                sp.transform.SetParent(g.transform);
                sp.transform.localPosition = positions[i];
                result[i] = sp.transform;
            }
            return result;
        }

        private static void AddLight(string name, GameObject parent, Vector3 localPos,
            Color color, float intensity, float range)
        {
            if (!parent) return;
            Transform old = parent.transform.Find(name);
            if (old) Object.DestroyImmediate(old.gameObject);
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            Light l = go.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = color;
            l.intensity = intensity;
            l.range     = range;
            l.shadows   = LightShadows.Soft;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // MATÉRIAUX
        // ─────────────────────────────────────────────────────────────────────────

        private static void LoadOrCreateMaterials()
        {
            const string f = "Assets/Materials";
            wallMat = AssetDatabase.LoadAssetAtPath<Material>($"{f}/Wall_Material.mat");
            if (!wallMat) { wallMat = new Material(Shader.Find("Standard")) { color = new Color(0.2f,0.22f,0.2f) }; AssetDatabase.CreateAsset(wallMat, $"{f}/Wall_Material.mat"); }
            else { wallMat.color = new Color(0.2f, 0.22f, 0.2f); }

            floorMat = AssetDatabase.LoadAssetAtPath<Material>($"{f}/Floor_Material.mat");
            if (!floorMat) { floorMat = new Material(Shader.Find("Standard")) { color = new Color(0.15f,0.15f,0.15f) }; AssetDatabase.CreateAsset(floorMat, $"{f}/Floor_Material.mat"); }

            ceilingMat = AssetDatabase.LoadAssetAtPath<Material>($"{f}/Ceiling_Material.mat");
            if (!ceilingMat) { ceilingMat = new Material(Shader.Find("Standard")) { color = new Color(0.12f,0.12f,0.12f) }; AssetDatabase.CreateAsset(ceilingMat, $"{f}/Ceiling_Material.mat"); }

            darkMetalMat = AssetDatabase.LoadAssetAtPath<Material>($"{f}/DarkMetal_Mat.mat");
            if (!darkMetalMat) { darkMetalMat = new Material(Shader.Find("Standard")) { color = new Color(0.18f,0.18f,0.2f) }; AssetDatabase.CreateAsset(darkMetalMat, $"{f}/DarkMetal_Mat.mat"); }
        }

        private static void ShareMaterials()
        {
            ShelterSupervisorRoomBuilder.WallMat      = wallMat;
            ShelterSupervisorRoomBuilder.FloorMat     = floorMat;
            ShelterSupervisorRoomBuilder.CeilingMat   = ceilingMat;
            ShelterSupervisorRoomBuilder.DarkMetalMat = darkMetalMat;
        }

        private static void EnsureMaterials()
        {
            if (!wallMat || !floorMat || !ceilingMat || !darkMetalMat) LoadOrCreateMaterials();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────

        private static GameObject GetOrCreateRoot() =>
            GameObject.Find(RootName) ?? new GameObject(RootName);

        private static GameObject GetOrCreateSystems(GameObject root)
        {
            GameObject s = FindChild(root, "GameSystems");
            if (!s) { s = new GameObject("GameSystems"); s.transform.SetParent(root.transform); }
            return s;
        }

        private static GameObject FindChild(GameObject parent, string name)
        {
            Transform t = parent.transform.Find(name);
            return t ? t.gameObject : null;
        }

        private static Camera FindCamera(GameObject root, string camName)
        {
            foreach (Camera c in root.GetComponentsInChildren<Camera>())
                if (c.name == camName) return c;
            return null;
        }

        private static void Finalize(GameObject root)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            Debug.Log("[ShelterCommandSceneBuilder] Build terminé.");
        }

        private static bool Confirm(string msg) =>
            EditorUtility.DisplayDialog("Shelter Command", msg, "Oui", "Annuler");

        private static GameObject MakePrimitive(PrimitiveType type, string name, GameObject parent,
            Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
