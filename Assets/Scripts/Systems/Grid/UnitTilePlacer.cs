using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the placement of units on the grid for Dark Protocol.
/// Provides methods for placing, moving, and removing units.
/// </summary>
public class UnitTilePlacer : MonoBehaviour
{
    #region Singleton Pattern
    
    private static UnitTilePlacer _instance;
    
    /// <summary>
    /// Singleton instance of the UnitTilePlacer
    /// </summary>
    public static UnitTilePlacer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<UnitTilePlacer>();
                
                if (_instance == null)
                {
                    Debug.LogError("No UnitTilePlacer found in scene. Please add one.");
                }
            }
            
            return _instance;
        }
    }
    
    private void Awake()
    {
        // Ensure singleton pattern
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Multiple UnitTilePlacers found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Create units container if needed
        if (unitsContainer == null)
        {
            GameObject container = new GameObject("Units");
            unitsContainer = container.transform;
        }
    }
    
    #endregion

    #region Inspector Fields
    
    [Header("References")]
    [Tooltip("Reference to the grid manager")]
    [SerializeField] private GridManager gridManager;
    
    [Tooltip("Unit prefabs that can be placed")]
    [SerializeField] private List<GameObject> unitPrefabs = new List<GameObject>();
    
    [Tooltip("Default unit prefab to use when not specified")]
    [SerializeField] private GameObject defaultUnitPrefab;
    
    [Tooltip("Container transform for all instantiated units")]
    [SerializeField] private Transform unitsContainer;
    
    [Header("Placement Settings")]
    [Tooltip("Height offset for units above tiles")]
    [SerializeField] private float unitHeightOffset = 0.5f;
    
    [Tooltip("Should units be automatically selected when placed?")]
    [SerializeField] private bool autoSelectPlacedUnits = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    #endregion

    #region Private Variables
    
    // Dictionary to track which unit is on which tile
    private Dictionary<Tile, Unit> _unitsByTile = new Dictionary<Tile, Unit>();
    
    // Dictionary to track which tile a unit is on
    private Dictionary<Unit, Tile> _tilesByUnit = new Dictionary<Unit, Tile>();
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        // Get GridManager reference if not assigned
        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
            
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found! UnitTilePlacer requires GridManager.");
                enabled = false;
                return;
            }
        }
    }
    
    #endregion

    #region Unit Placement Methods
    
    /// <summary>
    /// Places a unit at the specified grid coordinates using the default prefab
    /// </summary>
    /// <param name="x">X coordinate on the grid</param>
    /// <param name="y">Y coordinate on the grid</param>
    /// <returns>The placed Unit component, or null if placement failed</returns>
    public Unit PlaceUnitAt(int x, int y)
    {
        return PlaceUnitAt(x, y, defaultUnitPrefab);
    }
    
    /// <summary>
    /// Places a specific unit prefab at the specified grid coordinates
    /// </summary>
    /// <param name="x">X coordinate on the grid</param>
    /// <param name="y">Y coordinate on the grid</param>
    /// <param name="unitPrefab">The unit prefab to instantiate</param>
    /// <returns>The placed Unit component, or null if placement failed</returns>
    public Unit PlaceUnitAt(int x, int y, GameObject unitPrefab)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager reference is missing!");
            return null;
        }
        
        if (unitPrefab == null)
        {
            Debug.LogError("Unit prefab is null!");
            return null;
        }
        
        // Get the tile at the specified coordinates
        Tile tile = gridManager.GetTileAt(x, y);
        
        if (tile == null)
        {
            Debug.LogWarning($"No tile found at coordinates ({x}, {y})");
            return null;
        }
        
        // Check if the tile is already occupied
        if (tile.IsOccupied)
        {
            Debug.LogWarning($"Tile at ({x}, {y}) is already occupied!");
            return null;
        }
        
        // Calculate the position (slightly above the tile)
        Vector3 position = tile.transform.position + Vector3.up * unitHeightOffset;
        
        // Instantiate the unit
        GameObject unitObject = Instantiate(unitPrefab, position, Quaternion.identity);
        
        // Set parent to the units container
        if (unitsContainer != null)
        {
            unitObject.transform.SetParent(unitsContainer);
        }
        
        // Get the Unit component
        Unit unit = unitObject.GetComponent<Unit>();
        
        if (unit == null)
        {
            Debug.LogError("Instantiated prefab does not have a Unit component!");
            Destroy(unitObject);
            return null;
        }
        
        // Set the unit name if it's not already set
        if (string.IsNullOrEmpty(unit.UnitName))
        {
            // You might want to implement a custom name generator
            // For now, just use a simple format
            string unitName = $"Unit_{x}_{y}";
            
            // Note: You'd need to add a method to set the unit name
            // since UnitName is currently a read-only property
            // unit.SetUnitName(unitName);
        }
        
        // Mark the tile as occupied
        tile.SetOccupied(true);
        
        // Update tracking dictionaries
        _unitsByTile[tile] = unit;
        _tilesByUnit[unit] = tile;
        
        if (showDebugInfo)
        {
            Debug.Log($"Placed {unit.UnitName} at tile ({x}, {y})");
        }
        
        // Auto-select if enabled and the unit is a player unit
        if (autoSelectPlacedUnits && unit.Team == Unit.TeamType.Player)
        {
            // Only select if it's the player's turn
            if (GameManager.Instance != null && GameManager.Instance.IsPlayerTurn())
            {
                unit.Select();
            }
        }
        
        return unit;
    }
    
    /// <summary>
    /// Places a unit at a tile found by raycasting at the specified world position
    /// </summary>
    /// <param name="worldPosition">The world position to raycast from</param>
    /// <param name="unitPrefab">The unit prefab to instantiate (optional)</param>
    /// <returns>The placed Unit component, or null if placement failed</returns>
    public Unit PlaceUnitAtPosition(Vector3 worldPosition, GameObject unitPrefab = null)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager reference is missing!");
            return null;
        }
        
        // Use default prefab if none provided
        if (unitPrefab == null)
        {
            unitPrefab = defaultUnitPrefab;
            
            if (unitPrefab == null)
            {
                Debug.LogError("No default unit prefab assigned!");
                return null;
            }
        }
        
        // Convert world position to grid coordinates
        if (gridManager.WorldToGridPosition(worldPosition, out int x, out int y))
        {
            return PlaceUnitAt(x, y, unitPrefab);
        }
        
        Debug.LogWarning($"No valid grid position found at {worldPosition}");
        return null;
    }
    
    /// <summary>
    /// Removes a unit from the grid
    /// </summary>
    /// <param name="unit">The unit to remove</param>
    /// <returns>True if the unit was successfully removed</returns>
    public bool RemoveUnit(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("Attempted to remove a null unit!");
            return false;
        }
        
        // Find the tile this unit is on
        if (_tilesByUnit.TryGetValue(unit, out Tile tile))
        {
            // Mark tile as unoccupied
            tile.SetOccupied(false);
            
            // Remove from tracking dictionaries
            _unitsByTile.Remove(tile);
            _tilesByUnit.Remove(unit);
            
            // Deselect if selected
            if (unit.IsSelected)
            {
                unit.Deselect();
            }
            
            // Destroy the unit
            Destroy(unit.gameObject);
            
            if (showDebugInfo)
            {
                Debug.Log($"Removed {unit.UnitName} from tile ({tile.X}, {tile.Y})");
            }
            
            return true;
        }
        
        Debug.LogWarning($"Unit {unit.UnitName} not found on any tile!");
        return false;
    }
    
    /// <summary>
    /// Moves a unit from its current tile to a new tile
    /// </summary>
    /// <param name="unit">The unit to move</param>
    /// <param name="targetX">Target X coordinate</param>
    /// <param name="targetY">Target Y coordinate</param>
    /// <returns>True if the move was successful</returns>
    public bool MoveUnit(Unit unit, int targetX, int targetY)
    {
        if (unit == null || gridManager == null)
            return false;
        
        // Check if unit is on a tracked tile
        if (!_tilesByUnit.TryGetValue(unit, out Tile currentTile))
        {
            Debug.LogWarning($"Unit {unit.UnitName} is not on a tracked tile!");
            return false;
        }
        
        // Get the target tile
        Tile targetTile = gridManager.GetTileAt(targetX, targetY);
        
        if (targetTile == null)
        {
            Debug.LogWarning($"No tile found at coordinates ({targetX}, {targetY})");
            return false;
        }
        
        // Check if target tile is available
        if (targetTile.IsOccupied)
        {
            Debug.LogWarning($"Target tile at ({targetX}, {targetY}) is already occupied!");
            return false;
        }
        
        // Update the current tile's occupancy
        currentTile.SetOccupied(false);
        
        // Update the target tile's occupancy
        targetTile.SetOccupied(true);
        
        // Update tracking dictionaries
        _unitsByTile.Remove(currentTile);
        _unitsByTile[targetTile] = unit;
        _tilesByUnit[unit] = targetTile;
        
        // Update unit position
        Vector3 newPosition = targetTile.transform.position + Vector3.up * unitHeightOffset;
        unit.transform.position = newPosition;
        
        if (showDebugInfo)
        {
            Debug.Log($"Moved {unit.UnitName} from ({currentTile.X}, {currentTile.Y}) to ({targetX}, {targetY})");
        }
        
        return true;
    }
    
    #endregion

    #region Query Methods
    
    /// <summary>
    /// Gets the unit at the specified grid coordinates
    /// </summary>
    /// <returns>The Unit at the position, or null if none</returns>
    public Unit GetUnitAt(int x, int y)
    {
        if (gridManager == null)
            return null;
        
        Tile tile = gridManager.GetTileAt(x, y);
        
        if (tile != null && _unitsByTile.TryGetValue(tile, out Unit unit))
        {
            return unit;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the unit at the specified world position
    /// </summary>
    /// <returns>The Unit at the position, or null if none</returns>
    public Unit GetUnitAtPosition(Vector3 worldPosition)
    {
        if (gridManager == null)
            return null;
        
        if (gridManager.WorldToGridPosition(worldPosition, out int x, out int y))
        {
            return GetUnitAt(x, y);
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the tile that a specific unit is on
    /// </summary>
    /// <returns>The Tile the unit is on, or null if not found</returns>
    public Tile GetTileForUnit(Unit unit)
    {
        if (unit != null && _tilesByUnit.TryGetValue(unit, out Tile tile))
        {
            return tile;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the grid coordinates of a specific unit
    /// </summary>
    /// <param name="unit">The unit to locate</param>
    /// <param name="x">Output X coordinate</param>
    /// <param name="y">Output Y coordinate</param>
    /// <returns>True if the unit was found on the grid</returns>
    public bool GetUnitGridPosition(Unit unit, out int x, out int y)
    {
        x = -1;
        y = -1;
        
        Tile tile = GetTileForUnit(unit);
        
        if (tile != null)
        {
            x = tile.X;
            y = tile.Y;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets all units of a specific team
    /// </summary>
    public List<Unit> GetUnitsOfTeam(Unit.TeamType team)
    {
        List<Unit> result = new List<Unit>();
        
        foreach (Unit unit in _tilesByUnit.Keys)
        {
            if (unit.Team == team)
            {
                result.Add(unit);
            }
        }
        
        return result;
    }
    
    #endregion

    #region Batch Operations
    
    /// <summary>
    /// Clears all units from the grid
    /// </summary>
    public void ClearAllUnits()
    {
        List<Unit> unitsToRemove = new List<Unit>(_tilesByUnit.Keys);
        
        foreach (Unit unit in unitsToRemove)
        {
            RemoveUnit(unit);
        }
        
        // Just to be safe, clear the dictionaries
        _unitsByTile.Clear();
        _tilesByUnit.Clear();
        
        Debug.Log("Cleared all units from the grid");
    }
    
    /// <summary>
    /// Places multiple units of a specific team at random valid locations
    /// </summary>
    /// <param name="count">Number of units to place</param>
    /// <param name="team">Team type for the units</param>
    /// <param name="xMin">Minimum X coordinate</param>
    /// <param name="xMax">Maximum X coordinate</param>
    /// <param name="yMin">Minimum Y coordinate</param>
    /// <param name="yMax">Maximum Y coordinate</param>
    /// <returns>List of placed units</returns>
    public List<Unit> PlaceRandomUnits(int count, Unit.TeamType team, int xMin, int xMax, int yMin, int yMax)
    {
        if (count <= 0 || unitPrefabs.Count == 0)
            return new List<Unit>();
        
        List<Unit> placedUnits = new List<Unit>();
        List<Tile> availableTiles = new List<Tile>();
        
        // Get all available tiles in the specified range
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                
                if (tile != null && !tile.IsOccupied)
                {
                    availableTiles.Add(tile);
                }
            }
        }
        
        // Shuffle the available tiles
        for (int i = 0; i < availableTiles.Count; i++)
        {
            int swapIndex = Random.Range(i, availableTiles.Count);
            if (swapIndex != i)
            {
                Tile temp = availableTiles[i];
                availableTiles[i] = availableTiles[swapIndex];
                availableTiles[swapIndex] = temp;
            }
        }
        
        // Place units at random positions
        int unitsPlaced = 0;
        int tileIndex = 0;
        
        while (unitsPlaced < count && tileIndex < availableTiles.Count)
        {
            Tile tile = availableTiles[tileIndex];
            
            // Select a random unit prefab
            GameObject prefab = unitPrefabs[Random.Range(0, unitPrefabs.Count)];
            
            Unit unit = PlaceUnitAt(tile.X, tile.Y, prefab);
            
            if (unit != null)
            {
                // Note: You'd need to add a method to set the unit team
                // since Team is currently a read-only property
                // unit.SetTeam(team);
                
                placedUnits.Add(unit);
                unitsPlaced++;
            }
            
            tileIndex++;
        }
        
        return placedUnits;
    }
    
    #endregion
}