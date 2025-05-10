using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a modular grid system composed of multiple grid sections for Dark Protocol.
/// Acts as a central registry and coordinator for all grid-based operations.
/// </summary>
public class GridManager : MonoBehaviour
{
    #region Singleton Pattern
    
    private static GridManager _instance;
    
    /// <summary>
    /// Singleton instance of the GridManager
    /// </summary>
    public static GridManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GridManager>();
                
                if (_instance == null)
                {
                    Debug.LogError("No GridManager found in scene. Please add one.");
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
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Multiple GridManagers found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
    
    #endregion

    #region Inspector Fields
    
    [Header("Grid Settings")]
    [Tooltip("Should grid sections automatically register on startup?")]
    [SerializeField] private bool autoDiscoverGridSections = true;
    
    [Tooltip("Debug visualization mode")]
    [SerializeField] private bool debugMode = false;
    
    [Tooltip("Layer for raycasting to find tiles")]
    [SerializeField] private LayerMask tileLayerMask = Physics.DefaultRaycastLayers;
    
    [Header("Path Visualization")]
    [Tooltip("Whether to show path visualization")]
    [SerializeField] private bool showPaths = true;
    
    [Tooltip("Color for viable paths")]
    [SerializeField] private Color pathColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    
    [Tooltip("Duration to show path visualization")]
    [SerializeField] private float pathVisualizationDuration = 5f;
    
    #endregion

    #region Private Variables
    
    // Registry of all grid sections
    private List<GridableGround> _gridSections = new List<GridableGround>();
    
    // Dictionary for looking up tiles by position (rounded to nearest 0.1)
    private Dictionary<Vector3Int, Tile> _tilesByPosition = new Dictionary<Vector3Int, Tile>();
    
    // Registry of all tiles
    private List<Tile> _allTiles = new List<Tile>();
    
    // Current visible movement range
    private List<Tile> _currentMovementRange = new List<Tile>();
    
    // Current visible attack range
    private List<Tile> _currentAttackRange = new List<Tile>();
    
    // State tracking
    private bool _isShowingMovementRange = false;
    private bool _isShowingAttackRange = false;
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        if (autoDiscoverGridSections)
        {
            DiscoverGridSections();
        }
    }
    
    #endregion

    #region Grid Section Management
    
    /// <summary>
    /// Automatically finds and registers all GridableGround components in the scene
    /// </summary>
    public void DiscoverGridSections()
    {
        GridableGround[] sections = FindObjectsByType<GridableGround>(FindObjectsSortMode.None);
        
        foreach (GridableGround section in sections)
        {
            RegisterGridSection(section);
        }
        
        Debug.Log($"Discovered and registered {sections.Length} grid sections");
    }
    
    /// <summary>
    /// Registers a grid section with the manager
    /// </summary>
    public void RegisterGridSection(GridableGround section)
    {
        if (!_gridSections.Contains(section))
        {
            _gridSections.Add(section);
            
            if (debugMode)
            {
                Debug.Log($"Registered grid section: {section.gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// Unregisters a grid section from the manager
    /// </summary>
    public void UnregisterGridSection(GridableGround section)
    {
        if (_gridSections.Contains(section))
        {
            _gridSections.Remove(section);
            
            if (debugMode)
            {
                Debug.Log($"Unregistered grid section: {section.gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// Gets all registered grid sections
    /// </summary>
    public List<GridableGround> GetAllGridSections()
    {
        return new List<GridableGround>(_gridSections);
    }
    
    #endregion

    #region Tile Registration and Lookup
    
    /// <summary>
    /// Registers a tile with the manager
    /// </summary>
    public void RegisterTile(Tile tile)
    {
        if (tile == null) return;
        
        if (!_allTiles.Contains(tile))
        {
            _allTiles.Add(tile);
            
            // Add to position lookup dictionary
            Vector3Int positionKey = GetPositionKey(tile.transform.position);
            _tilesByPosition[positionKey] = tile;
        }
    }
    
    /// <summary>
    /// Unregisters a tile from the manager
    /// </summary>
    public void UnregisterTile(Tile tile)
    {
        if (tile == null) return;
        
        _allTiles.Remove(tile);
        
        // Remove from position lookup
        Vector3Int positionKey = GetPositionKey(tile.transform.position);
        if (_tilesByPosition.ContainsKey(positionKey) && _tilesByPosition[positionKey] == tile)
        {
            _tilesByPosition.Remove(positionKey);
        }
    }
    
    /// <summary>
    /// Gets a unique key for position-based tile lookup
    /// </summary>
    private Vector3Int GetPositionKey(Vector3 position)
    {
        // Round to nearest grid cell (assuming 0.1 precision)
        int x = Mathf.RoundToInt(position.x * 10);
        int y = Mathf.RoundToInt(position.y * 10);
        int z = Mathf.RoundToInt(position.z * 10);
        
        return new Vector3Int(x, y, z);
    }
    
    /// <summary>
    /// Gets the tile at a specific world position
    /// </summary>
    public Tile GetTileAtPosition(Vector3 worldPosition)
    {
        // Try direct position lookup first (faster)
        Vector3Int positionKey = GetPositionKey(worldPosition);
        if (_tilesByPosition.TryGetValue(positionKey, out Tile exactTile))
        {
            return exactTile;
        }
        
        // If not found by exact position, ask each grid section
        foreach (GridableGround section in _gridSections)
        {
            if (section.TryGetTileAtPosition(worldPosition, out Tile tile))
            {
                return tile;
            }
        }
        
        // Try raycast as a last resort
        if (TryRaycastTile(worldPosition, out Tile raycastTile))
        {
            return raycastTile;
        }
        
        return null;
    }
    
    /// <summary>
    /// Attempts to find a tile by raycasting from above
    /// </summary>
    private bool TryRaycastTile(Vector3 position, out Tile tile)
    {
        // Cast ray from above the position straight down
        Vector3 rayStart = new Vector3(position.x, position.y + 10f, position.z);
        Ray ray = new Ray(rayStart, Vector3.down);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, tileLayerMask))
        {
            // Check if we hit a tile
            Tile hitTile = hit.collider.GetComponent<Tile>();
            if (hitTile != null)
            {
                tile = hitTile;
                return true;
            }
        }
        
        tile = null;
        return false;
    }
    
    /// <summary>
    /// Gets all tiles in the grid system
    /// </summary>
    public List<Tile> GetAllTiles()
    {
        return new List<Tile>(_allTiles);
    }
    
    /// <summary>
    /// Gets all walkable tiles in the grid system
    /// </summary>
    public List<Tile> GetAllWalkableTiles()
    {
        List<Tile> walkableTiles = new List<Tile>();
        
        foreach (Tile tile in _allTiles)
        {
            if (tile != null && tile.IsWalkable && !tile.IsOccupied)
            {
                walkableTiles.Add(tile);
            }
        }
        
        return walkableTiles;
    }
    
    #endregion

    #region Neighborhood and Distance Methods
    
    /// <summary>
    /// Gets all neighboring tiles of a specific tile
    /// </summary>
    /// <param name="tile">The central tile</param>
    /// <param name="includeDiagonals">Whether to include diagonal neighbors</param>
    /// <returns>List of neighboring tiles</returns>
    public List<Tile> GetNeighbors(Tile tile, bool includeDiagonals = false)
    {
        if (tile == null) return new List<Tile>();
        
        List<Tile> neighbors = new List<Tile>();
        Vector3 tilePos = tile.transform.position;
        float gridSize = 1.0f; // Default grid size
        
        // If the tile has a parent grid section, use its cell size
        if (tile.ParentGridSection != null)
        {
            gridSize = tile.ParentGridSection.GetCellSize();
        }
        
        // Define neighbor offsets based on whether diagonals are included
        List<Vector3> offsets = new List<Vector3>
        {
            new Vector3(gridSize, 0, 0),    // Right
            new Vector3(-gridSize, 0, 0),   // Left
            new Vector3(0, 0, gridSize),    // Forward
            new Vector3(0, 0, -gridSize)    // Back
        };
        
        if (includeDiagonals)
        {
            offsets.Add(new Vector3(gridSize, 0, gridSize));    // Forward-Right
            offsets.Add(new Vector3(-gridSize, 0, gridSize));   // Forward-Left
            offsets.Add(new Vector3(gridSize, 0, -gridSize));   // Back-Right
            offsets.Add(new Vector3(-gridSize, 0, -gridSize));  // Back-Left
        }
        
        // Check each potential neighbor position
        foreach (Vector3 offset in offsets)
        {
            Vector3 neighborPos = tilePos + offset;
            Tile neighbor = GetTileAtPosition(neighborPos);
            
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }
        
        return neighbors;
    }
    
    /// <summary>
    /// Gets the distance between two tiles in grid units
    /// </summary>
    public int GetTileDistance(Tile a, Tile b)
    {
        if (a == null || b == null) return int.MaxValue;
        
        // Get positions
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;
        
        // Get grid size (assumes uniform grid size)
        float gridSize = 1.0f;
        if (a.ParentGridSection != null)
        {
            gridSize = a.ParentGridSection.GetCellSize();
        }
        
        // Calculate manhattan distance in grid units
        float xDist = Mathf.Abs(posA.x - posB.x) / gridSize;
        float zDist = Mathf.Abs(posA.z - posB.z) / gridSize;
        
        return Mathf.RoundToInt(xDist + zDist);
    }
    
    #endregion

    #region Range Calculation Methods
    
    /// <summary>
    /// Calculates and shows the movement range from a starting tile
    /// </summary>
    /// <param name="startTile">The starting tile</param>
    /// <param name="movementPoints">Maximum movement points</param>
    /// <returns>List of tiles within movement range</returns>
    public List<Tile> ShowMovementRange(Tile startTile, int movementPoints)
    {
        // Clear any existing ranges first
        ClearAllRanges();
        
        if (startTile == null || movementPoints <= 0)
        {
            return new List<Tile>();
        }
        
        // Calculate the movement range
        List<Tile> movementRange = CalculateMovementRange(startTile, movementPoints);
        
        // Display the range
        foreach (Tile tile in movementRange)
        {
            tile.SetInMovementRange(true);
            _currentMovementRange.Add(tile);
        }
        
        _isShowingMovementRange = true;
        return movementRange;
    }
    
    /// <summary>
    /// Calculates and shows the attack range from a starting tile
    /// </summary>
    /// <param name="startTile">The starting tile</param>
    /// <param name="minRange">Minimum attack range</param>
    /// <param name="maxRange">Maximum attack range</param>
    /// <returns>List of tiles within attack range</returns>
    public List<Tile> ShowAttackRange(Tile startTile, int minRange, int maxRange)
    {
        // Clear any existing attack range first
        ClearAttackRange();
        
        if (startTile == null || maxRange <= 0)
        {
            return new List<Tile>();
        }
        
        // Calculate the attack range
        List<Tile> attackRange = CalculateAttackRange(startTile, minRange, maxRange);
        
        // Display the range
        foreach (Tile tile in attackRange)
        {
            tile.SetInAttackRange(true);
            _currentAttackRange.Add(tile);
        }
        
        _isShowingAttackRange = true;
        return attackRange;
    }
    
    /// <summary>
    /// Calculates tiles within movement range using Dijkstra's algorithm
    /// </summary>
    private List<Tile> CalculateMovementRange(Tile startTile, int movementPoints)
    {
        List<Tile> result = new List<Tile>();
        Dictionary<Tile, float> costToReach = new Dictionary<Tile, float>();
        
        // Priority queue implementation (simplistic for clarity)
        List<Tile> frontier = new List<Tile>();
        
        // Initialize
        costToReach[startTile] = 0;
        frontier.Add(startTile);
        result.Add(startTile);
        
        while (frontier.Count > 0)
        {
            // Find the tile with the lowest cost (priority queue would be more efficient)
            Tile current = null;
            float lowestCost = float.MaxValue;
            
            foreach (Tile tile in frontier)
            {
                if (costToReach[tile] < lowestCost)
                {
                    lowestCost = costToReach[tile];
                    current = tile;
                }
            }
            
            // Remove current from frontier
            frontier.Remove(current);
            
            // Check each neighbor
            foreach (Tile neighbor in GetNeighbors(current))
            {
                // Skip unwalkable or occupied tiles
                if (!neighbor.IsWalkable || neighbor.IsOccupied)
                {
                    continue;
                }
                
                // Calculate cost to reach this neighbor
                float newCost = costToReach[current] + neighbor.MovementCost;
                
                // If cost is within movement points and either we haven't seen this tile or found a better path
                if (newCost <= movementPoints && (!costToReach.ContainsKey(neighbor) || newCost < costToReach[neighbor]))
                {
                    costToReach[neighbor] = newCost;
                    
                    if (!result.Contains(neighbor))
                    {
                        result.Add(neighbor);
                    }
                    
                    if (!frontier.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculates tiles within attack range
    /// </summary>
    private List<Tile> CalculateAttackRange(Tile startTile, int minRange, int maxRange)
    {
        List<Tile> result = new List<Tile>();
        HashSet<Tile> visited = new HashSet<Tile>();
        Queue<TileDistance> frontier = new Queue<TileDistance>();
        
        // Add the start tile
        frontier.Enqueue(new TileDistance(startTile, 0));
        visited.Add(startTile);
        
        while (frontier.Count > 0)
        {
            TileDistance current = frontier.Dequeue();
            
            // If within attack range, add to result
            if (current.distance >= minRange && current.distance <= maxRange)
            {
                result.Add(current.tile);
            }
            
            // If we haven't reached max range, explore neighbors
            if (current.distance < maxRange)
            {
                foreach (Tile neighbor in GetNeighbors(current.tile))
                {
                    if (!visited.Contains(neighbor))
                    {
                        frontier.Enqueue(new TileDistance(neighbor, current.distance + 1));
                        visited.Add(neighbor);
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Clears the current movement range visualization
    /// </summary>
    public void ClearMovementRange()
    {
        foreach (Tile tile in _currentMovementRange)
        {
            if (tile != null)
            {
                tile.SetInMovementRange(false);
            }
        }
        
        _currentMovementRange.Clear();
        _isShowingMovementRange = false;
    }
    
    /// <summary>
    /// Clears the current attack range visualization
    /// </summary>
    public void ClearAttackRange()
    {
        foreach (Tile tile in _currentAttackRange)
        {
            if (tile != null)
            {
                tile.SetInAttackRange(false);
            }
        }
        
        _currentAttackRange.Clear();
        _isShowingAttackRange = false;
    }
    
    /// <summary>
    /// Clears all range visualizations
    /// </summary>
    public void ClearAllRanges()
    {
        ClearMovementRange();
        ClearAttackRange();
    }
    
    #endregion

    #region Path Finding
    
    /// <summary>
    /// Finds a path between two tiles using A* algorithm
    /// </summary>
    /// <param name="startTile">Starting tile</param>
    /// <param name="targetTile">Target tile</param>
    /// <param name="onlyWalkable">Only use walkable tiles</param>
    /// <returns>Ordered list of tiles forming the path</returns>
    public List<Tile> FindPath(Tile startTile, Tile targetTile, bool onlyWalkable = true)
    {
        if (startTile == null || targetTile == null)
        {
            return new List<Tile>();
        }
        
        // A* implementation
        Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();
        Dictionary<Tile, float> costSoFar = new Dictionary<Tile, float>();
        
        // Priority queue (simplified for clarity)
        List<Tile> openSet = new List<Tile>();
        HashSet<Tile> closedSet = new HashSet<Tile>();
        
        // Initialize start
        openSet.Add(startTile);
        costSoFar[startTile] = 0;
        
        while (openSet.Count > 0)
        {
            // Find tile with lowest fScore (would be more efficient with a proper priority queue)
            Tile current = openSet[0];
            float lowestFScore = CalculateFScore(current, targetTile, costSoFar);
            
            for (int i = 1; i < openSet.Count; i++)
            {
                float fScore = CalculateFScore(openSet[i], targetTile, costSoFar);
                if (fScore < lowestFScore)
                {
                    lowestFScore = fScore;
                    current = openSet[i];
                }
            }
            
            // If we reached the target, reconstruct and return the path
            if (current == targetTile)
            {
                return ReconstructPath(cameFrom, current);
            }
            
            // Process current
            openSet.Remove(current);
            closedSet.Add(current);
            
            // Check each neighbor
            foreach (Tile neighbor in GetNeighbors(current))
            {
                // Skip if already processed
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }
                
                // Skip unwalkable tiles if specified
                if (onlyWalkable && (!neighbor.IsWalkable || neighbor.IsOccupied) && neighbor != targetTile)
                {
                    continue;
                }
                
                // Calculate new cost
                float newCost = costSoFar[current] + neighbor.MovementCost;
                
                // If this is a better path
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    cameFrom[neighbor] = current;
                    
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        
        // No path found
        return new List<Tile>();
    }
    
    /// <summary>
    /// Calculates the F score for a tile in A* pathfinding
    /// </summary>
    private float CalculateFScore(Tile tile, Tile target, Dictionary<Tile, float> costSoFar)
    {
        float gScore = costSoFar[tile]; // Cost so far
        float hScore = EstimateDistance(tile, target); // Heuristic
        return gScore + hScore;
    }
    
    /// <summary>
    /// Estimates distance between tiles for A* heuristic
    /// </summary>
    private float EstimateDistance(Tile a, Tile b)
    {
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;
        
        // Manhattan distance
        return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.z - posB.z);
    }
    
    /// <summary>
    /// Reconstructs the path from A* cameFrom map
    /// </summary>
    private List<Tile> ReconstructPath(Dictionary<Tile, Tile> cameFrom, Tile current)
    {
        List<Tile> path = new List<Tile>();
        path.Add(current);
        
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        
        return path;
    }
    
    /// <summary>
    /// Shows a path visualization between tiles
    /// </summary>
    /// <param name="path">The path to visualize</param>
    public void VisualizePath(List<Tile> path)
    {
        if (!showPaths || path == null || path.Count < 2)
        {
            return;
        }
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (path[i] == null || path[i+1] == null) continue;
            
            Vector3 start = path[i].transform.position + Vector3.up * 0.1f;
            Vector3 end = path[i+1].transform.position + Vector3.up * 0.1f;
            
            Debug.DrawLine(start, end, pathColor, pathVisualizationDuration);
        }
    }
    
    #endregion

    #region Utility Struct
    
    /// <summary>
    /// Simple struct to track tile distance for attack range calculation
    /// </summary>
    private struct TileDistance
    {
        public Tile tile;
        public int distance;
        
        public TileDistance(Tile t, int d)
        {
            tile = t;
            distance = d;
        }
    }
    
    #endregion
}