using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public enum WallSide
    {
        North,
        South,
        East,
        West
    }

    [System.Serializable]
    public class RoomConfig
    {
        public Vector3 size = new Vector3(5f, 3f, 5f);
        public Vector3 position = Vector3.zero;
        public string roomName = "Room";
        public Material wallMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;
        public List<WallSide> doorsides = new List<WallSide>();
        public float doorWidth = 1.2f;
        public float doorHeight = 2.2f;
    }

    [System.Serializable]
    public class CorridorConfig
    {
        public Vector3 start = Vector3.zero;
        public Vector3 end = Vector3.zero;
        public float width = 2f;
        public float height = 3f;
        public Material wallMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;
        public List<WallSide> openSides = new List<WallSide>();
    }

    [System.Serializable]
    public class DoorConfig
    {
        public Vector3 position;
        public Vector3 size = new Vector3(1.2f, 2.2f, 0.2f);
        public Quaternion rotation = Quaternion.identity;
    }

    private const string GENERATED_LEVEL_PARENT_NAME = "GeneratedLevel";

    public static GameObject GenerateRoom(RoomConfig config)
    {
        GameObject roomObj = new GameObject(config.roomName);
        
        ProBuilderMesh floor = CreateFloor(config);
        ProBuilderMesh ceiling = CreateCeiling(config);
        List<ProBuilderMesh> walls = CreateWalls(config);

        floor.transform.SetParent(roomObj.transform);
        ceiling.transform.SetParent(roomObj.transform);
        foreach (var wall in walls)
        {
            wall.transform.SetParent(roomObj.transform);
        }

        roomObj.transform.position = config.position;
        
        return roomObj;
    }

    public static GameObject GenerateCorridor(CorridorConfig config)
    {
        GameObject corridorObj = new GameObject("Corridor");
        
        Vector3 direction = (config.end - config.start).normalized;
        float length = Vector3.Distance(config.start, config.end);
        Vector3 center = (config.start + config.end) / 2f;
        
        Quaternion rotation = Quaternion.LookRotation(direction);
        
        RoomConfig corridorAsRoom = new RoomConfig
        {
            size = new Vector3(config.width, config.height, length),
            position = center,
            roomName = "Corridor",
            wallMaterial = config.wallMaterial,
            floorMaterial = config.floorMaterial,
            ceilingMaterial = config.ceilingMaterial,
            doorsides = new List<WallSide>()
        };
        
        ProBuilderMesh floor = CreateFloor(corridorAsRoom);
        ProBuilderMesh ceiling = CreateCeiling(corridorAsRoom);
        List<ProBuilderMesh> walls = CreateWallsWithOpenings(corridorAsRoom, config.openSides);
        
        floor.transform.SetParent(corridorObj.transform);
        ceiling.transform.SetParent(corridorObj.transform);
        foreach (var wall in walls)
        {
            wall.transform.SetParent(corridorObj.transform);
        }
        
        corridorObj.transform.rotation = rotation;
        corridorObj.transform.position = center;
        
        return corridorObj;
    }

    public static void CreateDoorOpening(GameObject room, DoorConfig doorConfig)
    {
        ProBuilderMesh[] walls = room.GetComponentsInChildren<ProBuilderMesh>();
        
        Debug.Log($"[CreateDoorOpening] Pièce {room.name}, Position locale porte: {doorConfig.position}");
        
        Vector3 doorWorldPos = room.transform.TransformPoint(doorConfig.position);
        
        ProBuilderMesh closestWall = null;
        float closestDistance = float.MaxValue;
        
        foreach (var wall in walls)
        {
            if (wall.name.Contains("Wall"))
            {
                MeshFilter meshFilter = wall.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Bounds wallBounds = meshFilter.sharedMesh.bounds;
                    Vector3 wallWorldCenter = wall.transform.TransformPoint(wallBounds.center);
                    
                    float distance = Vector3.Distance(wallWorldCenter, doorWorldPos);
                    
                    Debug.Log($"  -> Mur {wall.name}: Centre mesh local={wallBounds.center}, Centre monde={wallWorldCenter}, DoorWorld={doorWorldPos}, Distance={distance}");
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestWall = wall;
                    }
                }
            }
        }
        
        if (closestWall != null && closestDistance < 5f)
        {
            Debug.Log($"     ✓ Mur le plus proche: {closestWall.name} (distance={closestDistance}). Création ouverture...");
        }
        else
        {
            Debug.LogWarning($"     ✗ Aucun mur trouvé assez proche (distance min={closestDistance})");
        }
    }

    private static ProBuilderMesh CreateFloor(RoomConfig config)
    {
        ProBuilderMesh floor = ProBuilderMesh.Create();
        floor.CreateShapeFromPolygon(
            new Vector3[]
            {
                new Vector3(-config.size.x / 2, 0, -config.size.z / 2),
                new Vector3(config.size.x / 2, 0, -config.size.z / 2),
                new Vector3(config.size.x / 2, 0, config.size.z / 2),
                new Vector3(-config.size.x / 2, 0, config.size.z / 2)
            },
            0.1f,
            false
        );
        
        floor.name = "Floor";
        
        if (config.floorMaterial != null)
        {
            ApplyMaterial(floor, config.floorMaterial);
        }
        
        floor.ToMesh();
        floor.Refresh();
        EnsureBoxCollider(floor);
        
        return floor;
    }

    private static ProBuilderMesh CreateCeiling(RoomConfig config)
    {
        ProBuilderMesh ceiling = ProBuilderMesh.Create();
        ceiling.CreateShapeFromPolygon(
            new Vector3[]
            {
                new Vector3(-config.size.x / 2, config.size.y, -config.size.z / 2),
                new Vector3(-config.size.x / 2, config.size.y, config.size.z / 2),
                new Vector3(config.size.x / 2, config.size.y, config.size.z / 2),
                new Vector3(config.size.x / 2, config.size.y, -config.size.z / 2)
            },
            0.1f,
            false
        );
        
        ceiling.name = "Ceiling";
        
        if (config.ceilingMaterial != null)
        {
            ApplyMaterial(ceiling, config.ceilingMaterial);
        }
        
        ceiling.ToMesh();
        ceiling.Refresh();
        EnsureBoxCollider(ceiling);
        
        return ceiling;
    }

    private static List<ProBuilderMesh> CreateWalls(RoomConfig config)
    {
        List<ProBuilderMesh> walls = new List<ProBuilderMesh>();
        
        bool hasNorthDoor = config.doorsides != null && config.doorsides.Contains(WallSide.North);
        bool hasSouthDoor = config.doorsides != null && config.doorsides.Contains(WallSide.South);
        bool hasEastDoor = config.doorsides != null && config.doorsides.Contains(WallSide.East);
        bool hasWestDoor = config.doorsides != null && config.doorsides.Contains(WallSide.West);
        
        ProBuilderMesh wallNorth = CreateWallWithDoor(
            new Vector3(-config.size.x / 2, 0, config.size.z / 2),
            new Vector3(config.size.x / 2, 0, config.size.z / 2),
            config.size.y,
            "Wall_North",
            config.wallMaterial,
            hasNorthDoor,
            config.doorWidth,
            config.doorHeight
        );
        walls.Add(wallNorth);
        
        ProBuilderMesh wallSouth = CreateWallWithDoor(
            new Vector3(config.size.x / 2, 0, -config.size.z / 2),
            new Vector3(-config.size.x / 2, 0, -config.size.z / 2),
            config.size.y,
            "Wall_South",
            config.wallMaterial,
            hasSouthDoor,
            config.doorWidth,
            config.doorHeight
        );
        walls.Add(wallSouth);
        
        ProBuilderMesh wallEast = CreateWallWithDoor(
            new Vector3(config.size.x / 2, 0, config.size.z / 2),
            new Vector3(config.size.x / 2, 0, -config.size.z / 2),
            config.size.y,
            "Wall_East",
            config.wallMaterial,
            hasEastDoor,
            config.doorWidth,
            config.doorHeight
        );
        walls.Add(wallEast);
        
        ProBuilderMesh wallWest = CreateWallWithDoor(
            new Vector3(-config.size.x / 2, 0, -config.size.z / 2),
            new Vector3(-config.size.x / 2, 0, config.size.z / 2),
            config.size.y,
            "Wall_West",
            config.wallMaterial,
            hasWestDoor,
            config.doorWidth,
            config.doorHeight
        );
        walls.Add(wallWest);
        
        return walls;
    }

    private static List<ProBuilderMesh> CreateWallsWithOpenings(RoomConfig config, List<WallSide> openSides)
    {
        List<ProBuilderMesh> walls = new List<ProBuilderMesh>();
        
        bool hasNorthDoor = config.doorsides != null && config.doorsides.Contains(WallSide.North);
        bool hasSouthDoor = config.doorsides != null && config.doorsides.Contains(WallSide.South);
        bool hasEastDoor = config.doorsides != null && config.doorsides.Contains(WallSide.East);
        bool hasWestDoor = config.doorsides != null && config.doorsides.Contains(WallSide.West);
        
        bool skipNorth = openSides != null && openSides.Contains(WallSide.North);
        bool skipSouth = openSides != null && openSides.Contains(WallSide.South);
        bool skipEast = openSides != null && openSides.Contains(WallSide.East);
        bool skipWest = openSides != null && openSides.Contains(WallSide.West);
        
        if (!skipNorth)
        {
            ProBuilderMesh wallNorth = CreateWallWithDoor(
                new Vector3(-config.size.x / 2, 0, config.size.z / 2),
                new Vector3(config.size.x / 2, 0, config.size.z / 2),
                config.size.y,
                "Wall_North",
                config.wallMaterial,
                hasNorthDoor,
                config.doorWidth,
                config.doorHeight
            );
            walls.Add(wallNorth);
        }
        
        if (!skipSouth)
        {
            ProBuilderMesh wallSouth = CreateWallWithDoor(
                new Vector3(config.size.x / 2, 0, -config.size.z / 2),
                new Vector3(-config.size.x / 2, 0, -config.size.z / 2),
                config.size.y,
                "Wall_South",
                config.wallMaterial,
                hasSouthDoor,
                config.doorWidth,
                config.doorHeight
            );
            walls.Add(wallSouth);
        }
        
        if (!skipEast)
        {
            ProBuilderMesh wallEast = CreateWallWithDoor(
                new Vector3(config.size.x / 2, 0, config.size.z / 2),
                new Vector3(config.size.x / 2, 0, -config.size.z / 2),
                config.size.y,
                "Wall_East",
                config.wallMaterial,
                hasEastDoor,
                config.doorWidth,
                config.doorHeight
            );
            walls.Add(wallEast);
        }
        
        if (!skipWest)
        {
            ProBuilderMesh wallWest = CreateWallWithDoor(
                new Vector3(-config.size.x / 2, 0, -config.size.z / 2),
                new Vector3(-config.size.x / 2, 0, config.size.z / 2),
                config.size.y,
                "Wall_West",
                config.wallMaterial,
                hasWestDoor,
                config.doorWidth,
                config.doorHeight
            );
            walls.Add(wallWest);
        }
        
        return walls;
    }

    private static ProBuilderMesh CreateWallWithDoor(Vector3 start, Vector3 end, float height, string name, Material material, bool hasDoor, float doorWidth, float doorHeight)
    {
        if (!hasDoor)
        {
            return CreateWall(start, end, height, name, material);
        }
        
        GameObject wallContainer = new GameObject(name);
        
        Vector3 wallDirection = (end - start).normalized;
        float wallLength = Vector3.Distance(start, end);
        
        float doorStartPos = (wallLength - doorWidth) / 2f;
        float doorEndPos = doorStartPos + doorWidth;
        
        Vector3 doorBottomLeft = start + wallDirection * doorStartPos;
        Vector3 doorBottomRight = start + wallDirection * doorEndPos;
        Vector3 doorTopLeft = doorBottomLeft + Vector3.up * doorHeight;
        Vector3 doorTopRight = doorBottomRight + Vector3.up * doorHeight;
        
        Vector3 startTop = start + Vector3.up * height;
        Vector3 endTop = end + Vector3.up * height;
        Vector3 doorLeftTop = doorBottomLeft + Vector3.up * height;
        Vector3 doorRightTop = doorBottomRight + Vector3.up * height;
        
        if (doorStartPos > 0.1f)
        {
            ProBuilderMesh leftWall = ProBuilderMesh.Create();
            leftWall.CreateShapeFromPolygon(
                new Vector3[] { start, doorBottomLeft, doorLeftTop, startTop },
                0.01f,
                false
            );
            leftWall.transform.SetParent(wallContainer.transform);
            leftWall.name = "Left";
            if (material != null)
                leftWall.GetComponent<MeshRenderer>().sharedMaterial = material;
            leftWall.ToMesh();
            leftWall.Refresh();
            EnsureBoxCollider(leftWall);
        }
        
        if (wallLength - doorEndPos > 0.1f)
        {
            ProBuilderMesh rightWall = ProBuilderMesh.Create();
            rightWall.CreateShapeFromPolygon(
                new Vector3[] { doorBottomRight, end, endTop, doorRightTop },
                0.01f,
                false
            );
            rightWall.transform.SetParent(wallContainer.transform);
            rightWall.name = "Right";
            if (material != null)
                rightWall.GetComponent<MeshRenderer>().sharedMaterial = material;
            rightWall.ToMesh();
            rightWall.Refresh();
            EnsureBoxCollider(rightWall);
        }
        
        if (height - doorHeight > 0.1f)
        {
            ProBuilderMesh topWall = ProBuilderMesh.Create();
            topWall.CreateShapeFromPolygon(
                new Vector3[] { doorTopLeft, doorTopRight, doorRightTop, doorLeftTop },
                0.01f,
                false
            );
            topWall.transform.SetParent(wallContainer.transform);
            topWall.name = "Top";
            if (material != null)
                topWall.GetComponent<MeshRenderer>().sharedMaterial = material;
            topWall.ToMesh();
            topWall.Refresh();
            EnsureBoxCollider(topWall);
        }
        
        ProBuilderMesh containerMesh = wallContainer.AddComponent<ProBuilderMesh>();
        return containerMesh;
    }

    private static ProBuilderMesh CreateWall(Vector3 start, Vector3 end, float height, string name, Material material)
    {
        ProBuilderMesh wall = ProBuilderMesh.Create();
        
        wall.CreateShapeFromPolygon(
            new Vector3[]
            {
                start,
                end,
                new Vector3(end.x, height, end.z),
                new Vector3(start.x, height, start.z)
            },
            0.1f,
            false
        );
        
        wall.name = name;
        
        if (material != null)
        {
            ApplyMaterial(wall, material);
        }
        
        wall.ToMesh();
        wall.Refresh();
        EnsureBoxCollider(wall);
        
        return wall;
    }

    public static ProBuilderMesh CreateFloorForRoom(Vector3 size, Material material)
    {
        ProBuilderMesh floor = ProBuilderMesh.Create();
        floor.CreateShapeFromPolygon(
            new Vector3[]
            {
                new Vector3(-size.x / 2, 0, -size.z / 2),
                new Vector3(size.x / 2, 0, -size.z / 2),
                new Vector3(size.x / 2, 0, size.z / 2),
                new Vector3(-size.x / 2, 0, size.z / 2)
            },
            0.1f,
            false
        );
        
        floor.name = "Floor";
        
        if (material != null)
        {
            ApplyMaterial(floor, material);
        }
        
        floor.ToMesh();
        floor.Refresh();
        EnsureBoxCollider(floor);
        
        return floor;
    }
    
    public static ProBuilderMesh CreateCeilingForRoom(Vector3 size, float height, Material material)
    {
        ProBuilderMesh ceiling = ProBuilderMesh.Create();
        ceiling.CreateShapeFromPolygon(
            new Vector3[]
            {
                new Vector3(-size.x / 2, height, size.z / 2),
                new Vector3(size.x / 2, height, size.z / 2),
                new Vector3(size.x / 2, height, -size.z / 2),
                new Vector3(-size.x / 2, height, -size.z / 2)
            },
            0.1f,
            false
        );
        
        ceiling.name = "Ceiling";
        
        if (material != null)
        {
            ApplyMaterial(ceiling, material);
        }
        
        ceiling.ToMesh();
        ceiling.Refresh();
        EnsureBoxCollider(ceiling);
        
        return ceiling;
    }
    
    public static ProBuilderMesh CreateWallSegment(Vector3 start, Vector3 end, float height, Material material, string name)
    {
        ProBuilderMesh wall = ProBuilderMesh.Create();
        wall.CreateShapeFromPolygon(
            new Vector3[]
            {
                start,
                end,
                new Vector3(end.x, height, end.z),
                new Vector3(start.x, height, start.z)
            },
            0.1f,
            false
        );
        
        wall.name = name;
        
        if (material != null)
        {
            ApplyMaterial(wall, material);
        }
        
        wall.ToMesh();
        wall.Refresh();
        EnsureBoxCollider(wall);
        
        return wall;
    }
    
    public static void CutDoorInWallSegment(ProBuilderMesh wallSegment, DoorConfig doorConfig, Material wallMaterial)
    {
        Vector3 segmentStart = wallSegment.GetComponent<MeshFilter>().sharedMesh.bounds.min;
        Vector3 segmentEnd = wallSegment.GetComponent<MeshFilter>().sharedMesh.bounds.max;
        
        float segmentWidth = Mathf.Abs(segmentEnd.x - segmentStart.x);
        float segmentDepth = Mathf.Abs(segmentEnd.z - segmentStart.z);
        float segmentHeight = Mathf.Abs(segmentEnd.y - segmentStart.y);
        
        bool isHorizontal = segmentWidth > segmentDepth;
        float wallLength = isHorizontal ? segmentWidth : segmentDepth;
        
        float doorWidth = doorConfig.size.x;
        float doorHeight = doorConfig.size.y;
        
        float leftWidth = (wallLength - doorWidth) / 2f;
        float rightWidth = leftWidth;
        
        GameObject wallParent = wallSegment.transform.parent.gameObject;
        Vector3 wallPos = wallSegment.transform.localPosition;
        string wallName = wallSegment.name;
        
        DestroyImmediate(wallSegment.gameObject);
        
        if (leftWidth > 0.1f)
        {
            Vector3 leftStart, leftEnd;
            if (isHorizontal)
            {
                leftStart = new Vector3(-wallLength / 2, 0, 0);
                leftEnd = new Vector3(-wallLength / 2 + leftWidth, 0, 0);
            }
            else
            {
                leftStart = new Vector3(0, 0, wallLength / 2);
                leftEnd = new Vector3(0, 0, wallLength / 2 - leftWidth);
            }
            
            ProBuilderMesh leftWall = CreateWallSegment(leftStart, leftEnd, segmentHeight, wallMaterial, wallName + "_Left");
            leftWall.transform.SetParent(wallParent.transform);
            leftWall.transform.localPosition = wallPos;
        }
        
        if (rightWidth > 0.1f)
        {
            Vector3 rightStart, rightEnd;
            if (isHorizontal)
            {
                rightStart = new Vector3(wallLength / 2 - rightWidth, 0, 0);
                rightEnd = new Vector3(wallLength / 2, 0, 0);
            }
            else
            {
                rightStart = new Vector3(0, 0, -wallLength / 2 + rightWidth);
                rightEnd = new Vector3(0, 0, -wallLength / 2);
            }
            
            ProBuilderMesh rightWall = CreateWallSegment(rightStart, rightEnd, segmentHeight, wallMaterial, wallName + "_Right");
            rightWall.transform.SetParent(wallParent.transform);
            rightWall.transform.localPosition = wallPos;
        }
        
        float topHeight = segmentHeight - doorHeight;
        if (topHeight > 0.1f)
        {
            Vector3 topStart, topEnd;
            if (isHorizontal)
            {
                topStart = new Vector3(-doorWidth / 2, doorHeight, 0);
                topEnd = new Vector3(doorWidth / 2, doorHeight, 0);
            }
            else
            {
                topStart = new Vector3(0, doorHeight, doorWidth / 2);
                topEnd = new Vector3(0, doorHeight, -doorWidth / 2);
            }
            
            ProBuilderMesh topWall = ProBuilderMesh.Create();
            topWall.CreateShapeFromPolygon(
                new Vector3[]
                {
                    topStart,
                    topEnd,
                    new Vector3(topEnd.x, segmentHeight, topEnd.z),
                    new Vector3(topStart.x, segmentHeight, topStart.z)
                },
                0.1f,
                false
            );
            
            topWall.name = wallName + "_Top";
            
            if (wallMaterial != null)
            {
                ApplyMaterial(topWall, wallMaterial);
            }
            
            topWall.ToMesh();
            topWall.Refresh();
            
            topWall.transform.SetParent(wallParent.transform);
            topWall.transform.localPosition = wallPos;
        }
        
        Debug.Log($"✓ Ouverture découpée dans le segment de mur (remplacement par segments gauche/droite/haut)");
    }

    private static void ApplyMaterial(ProBuilderMesh mesh, Material material)
    {
        if (material != null)
        {
            mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    /// <summary>Adds a BoxCollider fitted to the local bounds of the ProBuilderMesh.</summary>
    private static void EnsureBoxCollider(ProBuilderMesh mesh)
    {
        MeshFilter filter = mesh.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;

        BoxCollider col = mesh.GetComponent<BoxCollider>();
        if (col == null)
            col = mesh.gameObject.AddComponent<BoxCollider>();

        Bounds bounds = filter.sharedMesh.bounds;
        col.center = bounds.center;
        col.size   = bounds.size;
    }

    public static GameObject CreateDoor(Vector3 position, Quaternion rotation, Vector3 size, Material material)
    {
        GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObj.name = "Door";
        doorObj.transform.position = position + Vector3.up * (size.y * 0.5f);
        doorObj.transform.rotation = rotation;
        doorObj.transform.localScale = size;
        
        if (material != null)
        {
            doorObj.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        
        return doorObj;
    }

    public static GameObject GetOrCreateLevelParent()
    {
        GameObject parent = GameObject.Find(GENERATED_LEVEL_PARENT_NAME);
        if (parent == null)
        {
            parent = new GameObject(GENERATED_LEVEL_PARENT_NAME);
        }
        return parent;
    }

    public static void ClearGeneratedLevel()
    {
        GameObject parent = GameObject.Find(GENERATED_LEVEL_PARENT_NAME);
        if (parent != null)
        {
            DestroyImmediate(parent);
        }
    }
    
    public static GameObject CreateStairs(Vector3 position, Quaternion rotation, float width, float depth, float totalHeight, int stepCount, Material material)
    {
        GameObject stairsParent = new GameObject("Stairs");
        stairsParent.transform.position = position;
        stairsParent.transform.rotation = rotation;
        
        float stepHeight = totalHeight / stepCount;
        float stepDepth = depth / stepCount;
        
        for (int i = 0; i < stepCount; i++)
        {
            float currentY = i * stepHeight;
            float currentZ = i * stepDepth;
            
            ProBuilderMesh step = ProBuilderMesh.Create();
            
            float stepThickness = stepHeight;
            
            Vector3[] bottomVertices = new Vector3[]
            {
                new Vector3(-width / 2f, currentY, currentZ),
                new Vector3(width / 2f, currentY, currentZ),
                new Vector3(width / 2f, currentY, currentZ + stepDepth),
                new Vector3(-width / 2f, currentY, currentZ + stepDepth)
            };
            
            step.CreateShapeFromPolygon(bottomVertices, stepThickness, false);
            
            step.name = $"Step_{i + 1}";
            
            if (material != null)
            {
                ApplyMaterial(step, material);
            }
            
            step.ToMesh();
            step.Refresh();
            
            step.transform.SetParent(stairsParent.transform);
            step.transform.localPosition = Vector3.zero;
        }
        
        return stairsParent;
    }
    
    public static void AddDoorBetweenRoomAndCorridor(GameObject room, GameObject corridor, float doorWidth = 1.2f, float doorHeight = 2.2f)
    {
        Vector3 roomPos = room.transform.position;
        Vector3 corridorPos = corridor.transform.position;
        
        MeshFilter roomFloorMesh = room.transform.Find("Floor")?.GetComponent<MeshFilter>();
        MeshFilter corridorFloorMesh = corridor.transform.Find("Floor")?.GetComponent<MeshFilter>();
        
        if (roomFloorMesh == null || corridorFloorMesh == null) return;
        
        Bounds roomBounds = roomFloorMesh.sharedMesh.bounds;
        Bounds corridorBounds = corridorFloorMesh.sharedMesh.bounds;
        
        Vector3 roomSize = new Vector3(
            roomBounds.size.x,
            roomBounds.size.y * room.transform.localScale.y,
            roomBounds.size.z
        );
        
        Vector3 delta = corridorPos - roomPos;
        float deltaX = Mathf.Abs(delta.x);
        float deltaZ = Mathf.Abs(delta.z);
        
        DoorConfig doorConfig = new DoorConfig
        {
            size = new Vector3(doorWidth, doorHeight, 0.2f)
        };
        
        if (deltaX > deltaZ)
        {
            float doorX = delta.x > 0 ? roomSize.x / 2 : -roomSize.x / 2;
            doorConfig.position = new Vector3(doorX, doorHeight / 2, 0);
            doorConfig.rotation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            float doorZ = delta.z > 0 ? roomSize.z / 2 : -roomSize.z / 2;
            doorConfig.position = new Vector3(0, doorHeight / 2, doorZ);
            doorConfig.rotation = Quaternion.identity;
        }
        
        ProBuilderMesh[] walls = room.GetComponentsInChildren<ProBuilderMesh>();
        foreach (var wall in walls)
        {
            if (wall.name.StartsWith("Wall"))
            {
                Vector3 wallCenter = wall.transform.position;
                Vector3 doorWorldPos = room.transform.TransformPoint(doorConfig.position);
                float distance = Vector3.Distance(wallCenter, doorWorldPos);
                
                if (distance < 0.5f)
                {
                    break;
                }
            }
        }
    }
}
