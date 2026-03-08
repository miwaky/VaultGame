using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Level Preset", menuName = "Level Generator/Level Preset", order = 1)]
public class LevelLayoutPreset : ScriptableObject
{
    [System.Serializable]
    public class PresetMetadata
    {
        public string presetName = "New Preset";
        public string description = "";
        public string author = "";
        public string creationDate = "";
        public string lastModifiedDate = "";
        public int version = 1;
    }
    
    [Header("Métadonnées")]
    public PresetMetadata metadata = new PresetMetadata();
    
    [Header("Configuration Globale")]
    public int gridWidth = 16;
    public int gridHeight = 16;
    public float gridCellSize = 5f;
    public float floorHeight = 3.5f;
    
    [Header("Matériaux par Défaut")]
    public Material wallMaterial;
    public Material floorMaterial;
    public Material ceilingMaterial;
    public Material stairsMaterial;
    
    [Header("Options Globales")]
    public bool globalGenerateFloor = true;
    public bool globalGenerateCeiling = true;
    
    [Header("Données des Étages")]
    public List<GridLayoutData> floors = new List<GridLayoutData>();
    
    public void SaveCurrentState(
        int width, int height, float cellSize, float floorHeightValue,
        Material wall, Material floor, Material ceiling, Material stairs,
        bool genFloor, bool genCeiling,
        List<GridLayoutData> currentFloors)
    {
        gridWidth = width;
        gridHeight = height;
        gridCellSize = cellSize;
        floorHeight = floorHeightValue;
        
        wallMaterial = wall;
        floorMaterial = floor;
        ceilingMaterial = ceiling;
        stairsMaterial = stairs;
        
        globalGenerateFloor = genFloor;
        globalGenerateCeiling = genCeiling;
        
        floors = new List<GridLayoutData>();
        foreach (var floorData in currentFloors)
        {
            GridLayoutData newFloor = new GridLayoutData(floorData.gridWidth, floorData.gridHeight);
            
            foreach (var room in floorData.rooms)
            {
                GridRoom newRoom = new GridRoom
                {
                    id = room.id,
                    displayName = room.displayName,
                    cells = new List<Vector2Int>(room.cells),
                    useCustomSize = room.useCustomSize,
                    customWidth = room.customWidth,
                    customDepth = room.customDepth,
                    height = room.height,
                    floorHeight = room.floorHeight,
                    ceilingHeight = room.ceilingHeight,
                    hasFloor = room.hasFloor,
                    hasCeiling = room.hasCeiling,
                    previewColor = room.previewColor,
                    wallMaterial = room.wallMaterial,
                    floorMaterial = room.floorMaterial,
                    ceilingMaterial = room.ceilingMaterial
                };
                newFloor.rooms.Add(newRoom);
            }
            
            foreach (var door in floorData.doors)
            {
                GridDoor newDoor = new GridDoor(
                    door.roomId1,
                    door.roomId2,
                    door.cell1,
                    door.cell2,
                    door.side
                )
                {
                    doorType = door.doorType,
                    width = door.width,
                    height = door.height
                };
                newFloor.doors.Add(newDoor);
            }
            
            foreach (var stairsItem in floorData.stairs)
            {
                GridStairs newStairs = new GridStairs(
                    stairsItem.cell,
                    stairsItem.fromFloor,
                    stairsItem.toFloor,
                    stairsItem.direction
                );
                newFloor.stairs.Add(newStairs);
            }
            
            newFloor.RebuildOccupancy();
            floors.Add(newFloor);
        }
        
        metadata.lastModifiedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (string.IsNullOrEmpty(metadata.creationDate))
        {
            metadata.creationDate = metadata.lastModifiedDate;
        }
    }
}
