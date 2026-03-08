using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GridRoom
{
    public string id;
    public string displayName;
    public List<Vector2Int> cells = new List<Vector2Int>();
    public float height = 3f;
    public Color previewColor = Color.blue;
    public List<GridDoor> doors = new List<GridDoor>();
    
    public Material wallMaterial = null;
    public Material floorMaterial = null;
    public Material ceilingMaterial = null;
    
    public bool hasFloor = false;
    public bool hasCeiling = false;
    
    public bool useCustomSize = false;
    public float customWidth = 5f;
    public float customDepth = 5f;
    
    public float floorHeight = 0f;
    public float ceilingHeight = 3f;
    
    public float GetActualWidth(float cellSize)
    {
        if (useCustomSize)
            return customWidth;
        
        Vector2Int gridSize = GetGridSize();
        return gridSize.x * cellSize;
    }
    
    public float GetActualDepth(float cellSize)
    {
        if (useCustomSize)
            return customDepth;
        
        Vector2Int gridSize = GetGridSize();
        return gridSize.y * cellSize;
    }
    
    public float GetMaxPossibleWidth(float cellSize)
    {
        Vector2Int gridSize = GetGridSize();
        return gridSize.x * cellSize;
    }
    
    public float GetMaxPossibleDepth(float cellSize)
    {
        Vector2Int gridSize = GetGridSize();
        return gridSize.y * cellSize;
    }
    
    public Vector2Int GetMinCell()
    {
        if (cells.Count == 0) return Vector2Int.zero;
        Vector2Int min = cells[0];
        foreach (var cell in cells)
        {
            if (cell.x < min.x) min.x = cell.x;
            if (cell.y < min.y) min.y = cell.y;
        }
        return min;
    }
    
    public Vector2Int GetMaxCell()
    {
        if (cells.Count == 0) return Vector2Int.zero;
        Vector2Int max = cells[0];
        foreach (var cell in cells)
        {
            if (cell.x > max.x) max.x = cell.x;
            if (cell.y > max.y) max.y = cell.y;
        }
        return max;
    }
    
    public Vector2Int GetGridSize()
    {
        Vector2Int min = GetMinCell();
        Vector2Int max = GetMaxCell();
        return new Vector2Int(max.x - min.x + 1, max.y - min.y + 1);
    }
    
    public Vector2Int GetBoundsMin()
    {
        return GetMinCell();
    }
    
    public Vector2Int GetBoundsMax()
    {
        return GetMaxCell();
    }
}

[System.Serializable]
public class GridDoor
{
    public string roomId1;
    public string roomId2;
    public Vector2Int cell1;
    public Vector2Int cell2;
    
    public enum Side { North, South, East, West }
    public Side side;
    
    public enum DoorType { Standard, Conduit }
    public DoorType doorType = DoorType.Standard;
    
    public float width = 1.2f;
    public float height = 2.5f;
    
    public GridDoor(string r1, string r2, Vector2Int c1, Vector2Int c2, Side s)
    {
        roomId1 = r1;
        roomId2 = r2;
        cell1 = c1;
        cell2 = c2;
        side = s;
        doorType = DoorType.Standard;
        width = 1.2f;
        height = 2.5f;
    }
    
    public Vector3 GetWorldPosition(float cellSize)
    {
        float x1 = cell1.x * cellSize;
        float z1 = -cell1.y * cellSize;
        float x2 = cell2.x * cellSize;
        float z2 = -cell2.y * cellSize;
        
        return new Vector3((x1 + x2) * 0.5f, 0, (z1 + z2) * 0.5f);
    }
    
    public Quaternion GetWorldRotation()
    {
        if (side == Side.North || side == Side.South)
            return Quaternion.Euler(0, 0, 0);
        else
            return Quaternion.Euler(0, 90, 0);
    }
}

[System.Serializable]
public class GridStairs
{
    public Vector2Int cell;
    public int fromFloor;
    public int toFloor;
    
    public enum Direction { North, South, East, West }
    public Direction direction = Direction.North;
    
    public GridStairs(Vector2Int c, int from, int to, Direction dir)
    {
        cell = c;
        fromFloor = from;
        toFloor = to;
        direction = dir;
    }
    
    public Vector3 GetWorldPosition(float cellSize)
    {
        float x = cell.x * cellSize;
        float z = -cell.y * cellSize;
        return new Vector3(x, 0, z);
    }
    
    public Quaternion GetWorldRotation()
    {
        switch (direction)
        {
            case Direction.North: return Quaternion.Euler(0, 0, 0);
            case Direction.South: return Quaternion.Euler(0, 180, 0);
            case Direction.East: return Quaternion.Euler(0, 90, 0);
            case Direction.West: return Quaternion.Euler(0, 270, 0);
            default: return Quaternion.identity;
        }
    }
}

public class GridLayoutData
{
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 5f;
    public List<GridRoom> rooms = new List<GridRoom>();
    public List<GridDoor> doors = new List<GridDoor>();
    public List<GridStairs> stairs = new List<GridStairs>();
    
    private string[,] cellOccupancy;
    
    public GridLayoutData(int width, int height)
    {
        gridWidth = width;
        gridHeight = height;
        cellOccupancy = new string[width, height];
    }
    
    public void RebuildOccupancy()
    {
        cellOccupancy = new string[gridWidth, gridHeight];
        foreach (var room in rooms)
        {
            foreach (var cell in room.cells)
            {
                if (IsValidCell(cell))
                {
                    cellOccupancy[cell.x, cell.y] = room.id;
                }
            }
        }
    }
    
    public bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }
    
    public bool IsCellEmpty(Vector2Int cell)
    {
        if (!IsValidCell(cell)) return false;
        return string.IsNullOrEmpty(cellOccupancy[cell.x, cell.y]);
    }
    
    public string GetRoomAtCell(Vector2Int cell)
    {
        if (!IsValidCell(cell)) return null;
        return cellOccupancy[cell.x, cell.y];
    }
    
    public GridRoom FindRoom(string roomId)
    {
        return rooms.Find(r => r.id == roomId);
    }
    
    public GridRoom GetOrCreateRoom(Vector2Int cell)
    {
        string existingRoomId = GetRoomAtCell(cell);
        if (!string.IsNullOrEmpty(existingRoomId))
        {
            return FindRoom(existingRoomId);
        }
        
        string newId = GenerateRoomId();
        GridRoom newRoom = new GridRoom
        {
            id = newId,
            displayName = "Pièce " + newId,
            previewColor = GenerateRandomColor()
        };
        newRoom.cells.Add(cell);
        rooms.Add(newRoom);
        cellOccupancy[cell.x, cell.y] = newId;
        return newRoom;
    }
    
    public void RemoveRoom(string roomId)
    {
        GridRoom room = FindRoom(roomId);
        if (room != null)
        {
            foreach (var cell in room.cells)
            {
                if (IsValidCell(cell))
                {
                    cellOccupancy[cell.x, cell.y] = null;
                }
            }
            RemoveDoorsForRoom(roomId);
            rooms.Remove(room);
        }
    }
    
    public void AddCellToRoom(string roomId, Vector2Int cell)
    {
        if (!IsCellEmpty(cell)) return;
        
        GridRoom room = FindRoom(roomId);
        if (room != null && !room.cells.Contains(cell))
        {
            room.cells.Add(cell);
            cellOccupancy[cell.x, cell.y] = roomId;
        }
    }
    
    public void RemoveCellFromRoom(string roomId, Vector2Int cell)
    {
        GridRoom room = FindRoom(roomId);
        if (room != null && room.cells.Contains(cell))
        {
            room.cells.Remove(cell);
            if (IsValidCell(cell))
            {
                cellOccupancy[cell.x, cell.y] = null;
            }
            
            RemoveDoorsForCell(cell);
            
            if (room.cells.Count == 0)
            {
                RemoveRoom(roomId);
            }
        }
    }
    
    public GridDoor GetDoorAtCell(Vector2Int cell)
    {
        foreach (var door in doors)
        {
            if (door.cell1 == cell || door.cell2 == cell)
            {
                return door;
            }
        }
        return null;
    }
    
    public void RemoveDoorsForCell(Vector2Int cell)
    {
        doors.RemoveAll(d => d.cell1 == cell || d.cell2 == cell);
    }
    
    public bool AreRoomsAdjacent(string roomId1, string roomId2)
    {
        GridRoom room1 = FindRoom(roomId1);
        GridRoom room2 = FindRoom(roomId2);
        if (room1 == null || room2 == null) return false;
        
        foreach (var cell1 in room1.cells)
        {
            Vector2Int[] neighbors = {
                new Vector2Int(cell1.x + 1, cell1.y),
                new Vector2Int(cell1.x - 1, cell1.y),
                new Vector2Int(cell1.x, cell1.y + 1),
                new Vector2Int(cell1.x, cell1.y - 1)
            };
            
            foreach (var neighbor in neighbors)
            {
                if (room2.cells.Contains(neighbor))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public List<Vector2Int> GetSharedBorder(string roomId1, string roomId2)
    {
        List<Vector2Int> borderCells = new List<Vector2Int>();
        GridRoom room1 = FindRoom(roomId1);
        GridRoom room2 = FindRoom(roomId2);
        if (room1 == null || room2 == null) return borderCells;
        
        foreach (var cell1 in room1.cells)
        {
            Vector2Int[] neighbors = {
                new Vector2Int(cell1.x + 1, cell1.y),
                new Vector2Int(cell1.x - 1, cell1.y),
                new Vector2Int(cell1.x, cell1.y + 1),
                new Vector2Int(cell1.x, cell1.y - 1)
            };
            
            foreach (var neighbor in neighbors)
            {
                if (room2.cells.Contains(neighbor) && !borderCells.Contains(cell1))
                {
                    borderCells.Add(cell1);
                }
            }
        }
        return borderCells;
    }
    
    public bool TryAddDoor(Vector2Int cell1, Vector2Int cell2)
    {
        string room1Id = GetRoomAtCell(cell1);
        string room2Id = GetRoomAtCell(cell2);
        
        if (string.IsNullOrEmpty(room1Id) || string.IsNullOrEmpty(room2Id))
            return false;
        
        if (room1Id == room2Id)
            return false;
        
        if (DoorExistsBetween(cell1, cell2))
        {
            RemoveDoorBetween(cell1, cell2);
            return true;
        }
        
        GridDoor.Side side = GetDoorSide(cell1, cell2);
        GridDoor newDoor = new GridDoor(room1Id, room2Id, cell1, cell2, side);
        doors.Add(newDoor);
        return true;
    }
    
    public bool DoorExistsBetween(Vector2Int cell1, Vector2Int cell2)
    {
        foreach (var door in doors)
        {
            if ((door.cell1 == cell1 && door.cell2 == cell2) ||
                (door.cell1 == cell2 && door.cell2 == cell1))
            {
                return true;
            }
        }
        return false;
    }
    
    public void RemoveDoorBetween(Vector2Int cell1, Vector2Int cell2)
    {
        doors.RemoveAll(d => 
            (d.cell1 == cell1 && d.cell2 == cell2) ||
            (d.cell1 == cell2 && d.cell2 == cell1));
    }
    
    public void RemoveDoorsForRoom(string roomId)
    {
        doors.RemoveAll(d => d.roomId1 == roomId || d.roomId2 == roomId);
    }
    
    private GridDoor.Side GetDoorSide(Vector2Int cell1, Vector2Int cell2)
    {
        if (cell2.x > cell1.x) return GridDoor.Side.East;
        if (cell2.x < cell1.x) return GridDoor.Side.West;
        if (cell2.y > cell1.y) return GridDoor.Side.South;
        return GridDoor.Side.North;
    }
    
    public List<Vector2Int> GetAdjacentCells(Vector2Int cell)
    {
        List<Vector2Int> adjacent = new List<Vector2Int>();
        Vector2Int[] neighbors = {
            new Vector2Int(cell.x + 1, cell.y),
            new Vector2Int(cell.x - 1, cell.y),
            new Vector2Int(cell.x, cell.y + 1),
            new Vector2Int(cell.x, cell.y - 1)
        };
        
        foreach (var neighbor in neighbors)
        {
            if (IsValidCell(neighbor))
            {
                adjacent.Add(neighbor);
            }
        }
        return adjacent;
    }
    
    private string GenerateRoomId()
    {
        string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        foreach (var letter in letters)
        {
            if (FindRoom(letter) == null)
            {
                return letter;
            }
        }
        return "Room" + rooms.Count;
    }
    
    private Color GenerateRandomColor()
    {
        return new Color(Random.Range(0.3f, 0.9f), Random.Range(0.3f, 0.9f), Random.Range(0.3f, 0.9f), 0.7f);
    }
    
    public GridStairs GetStairsAtCell(Vector2Int cell)
    {
        return stairs.Find(s => s.cell == cell);
    }
    
    public void RemoveStairsAtCell(Vector2Int cell)
    {
        stairs.RemoveAll(s => s.cell == cell);
    }
    
    public bool TryAddStairs(Vector2Int cell, int fromFloor, int toFloor, GridStairs.Direction direction)
    {
        if (!IsValidCell(cell)) return false;
        
        RemoveStairsAtCell(cell);
        
        GridStairs newStairs = new GridStairs(cell, fromFloor, toFloor, direction);
        stairs.Add(newStairs);
        return true;
    }
}
