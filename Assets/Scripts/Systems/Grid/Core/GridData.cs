using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Core data structure for the grid system, replacing the GameObject-based approach.
    /// Implements a high-performance, data-driven grid for large tactical maps.
    /// </summary>
    [CreateAssetMenu(fileName = "New Grid Data", menuName = "Dark Protocol/Grid/Grid Data")]
    public class GridData : ScriptableObject
    {
        #region Map Settings
        [Header("Map Dimensions")]
        [SerializeField] private int width = 50;
        [SerializeField] private int height = 50;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 mapOrigin = Vector3.zero;
        
        [Header("Optimization")]
        [SerializeField] private int chunkSize = 10;
        [SerializeField] private float maxVisibleDistance = 20f;
        [Tooltip("Percentage of tiles to update per frame for large operations")]
        [Range(0.01f, 1f)]
        [SerializeField] private float updateBudgetPerFrame = 0.1f;
        #endregion

        // The core data structure - stores all tile data efficiently
        private TileData[,] _tileData;
        
        // Dictionary for quick lookups of objects on tiles
        private Dictionary<Vector2Int, GameObject> _occupants = new Dictionary<Vector2Int, GameObject>();
        
        // Cache for pathfinding - NEW: This should be using string keys not Vector2Int
        private Dictionary<string, List<Vector2Int>> _pathStringCache = new Dictionary<string, List<Vector2Int>>();
        
        // Reference to chunk renderers
        private List<GridChunkRenderer> _chunkRenderers = new List<GridChunkRenderer>();

        #region Properties
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 MapOrigin => mapOrigin;
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the grid with default values
        /// </summary>
        public void Initialize()
        {
            // Center the grid on its own midpoint
            mapOrigin = new Vector3(
                -width  * cellSize * 0.5f,
                mapOrigin.y,
                -height * cellSize * 0.5f
            );
            // Create the tile data array
            _tileData = new TileData[width, height];
            
            // Initialize each tile with default values
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    _tileData[x, z] = new TileData
                    {
                        Position = new Vector2Int(x, z),
                        TerrainType = TerrainType.Ground,
                        MovementCost = 1,
                        IsWalkable = true,
                        IsOccupied = false,
                        Elevation = 0,
                        CoverType = CoverType.None
                    };
                }
            }
            
            // Clear collections
            _occupants.Clear();
            _pathStringCache.Clear();
            
            Debug.Log($"Grid initialized: {width}x{height} ({width * height} tiles)");
        }
        
        /// <summary>
        /// Generate chunk renderers for efficient rendering
        /// </summary>
        /// <param name="parent">Parent transform for the chunks</param>
        public void GenerateChunkRenderers(Transform parent)
        {
            // Clean up any existing renderers
            foreach (var renderer in _chunkRenderers)
            {
                if (renderer != null)
                {
                    if (Application.isPlaying)
                        GameObject.Destroy(renderer.gameObject);
                    else
                        GameObject.DestroyImmediate(renderer.gameObject);
                }
            }
            _chunkRenderers.Clear();
            
            // Calculate how many chunks we need
            int chunksX = Mathf.CeilToInt((float)width / chunkSize);
            int chunksZ = Mathf.CeilToInt((float)height / chunkSize);
            
            // Create chunk game objects and renderers
            for (int chunkX = 0; chunkX < chunksX; chunkX++)
            {
                for (int chunkZ = 0; chunkZ < chunksZ; chunkZ++)
                {
                    // Calculate bounds for this chunk
                    int startX = chunkX * chunkSize;
                    int startZ = chunkZ * chunkSize;
                    int endX = Mathf.Min(startX + chunkSize, width);
                    int endZ = Mathf.Min(startZ + chunkSize, height);
                    
                    // Create a new chunk renderer
                    GameObject chunkObject = new GameObject($"GridChunk_{chunkX}_{chunkZ}");
                    chunkObject.transform.SetParent(parent);
                    
                    // Position at the center of the chunk
                    float centerX = mapOrigin.x + (startX + (endX - startX) * 0.5f) * cellSize;
                    float centerZ = mapOrigin.z + (startZ + (endZ - startZ) * 0.5f) * cellSize;
                    chunkObject.transform.position = new Vector3(centerX, mapOrigin.y, centerZ);
                    
                    // Add the renderer component
                    GridChunkRenderer renderer = chunkObject.AddComponent<GridChunkRenderer>();
                    renderer.Initialize(this, new RectInt(startX, startZ, endX - startX, endZ - startZ));
                    
                    _chunkRenderers.Add(renderer);
                }
            }
            
            Debug.Log($"Created {_chunkRenderers.Count} grid chunk renderers");
        }
        #endregion

        #region Tile Access Methods
        /// <summary>
        /// Check if the given coordinates are within the grid bounds
        /// </summary>
        public bool IsValidPosition(int x, int z)
        {
            return x >= 0 && x < width && z >= 0 && z < height;
        }
        
        /// <summary>
        /// Check if the given coordinates are within the grid bounds
        /// </summary>
        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }
        
        /// <summary>
        /// Get the tile data at the specified coordinates
        /// </summary>
        public TileData GetTileData(int x, int z)
        {
            if (!IsValidPosition(x, z))
                return null;
                
            return _tileData[x, z];
        }
        
        /// <summary>
        /// Get the tile data at the specified coordinates
        /// </summary>
        public TileData GetTileData(Vector2Int position)
        {
            return GetTileData(position.x, position.y);
        }
        
        /// <summary>
        /// Set a tile's terrain type
        /// </summary>
        public void SetTileTerrain(int x, int z, TerrainType terrainType, float movementCost = 1f)
        {
            if (!IsValidPosition(x, z))
                return;
                
            _tileData[x, z].TerrainType = terrainType;
            _tileData[x, z].MovementCost = movementCost;
            
            // Update renderers
            UpdateChunkAtPosition(x, z);
        }
        
        /// <summary>
        /// Set whether a tile is walkable
        /// </summary>
        public void SetTileWalkable(int x, int z, bool walkable)
        {
            if (!IsValidPosition(x, z))
                return;
                
            _tileData[x, z].IsWalkable = walkable;
            
            // Update renderers
            UpdateChunkAtPosition(x, z);
        }
        
        /// <summary>
        /// Set a tile's elevation
        /// </summary>
        public void SetTileElevation(int x, int z, float elevation)
        {
            if (!IsValidPosition(x, z))
                return;
                
            _tileData[x, z].Elevation = elevation;
            
            // Update renderers
            UpdateChunkAtPosition(x, z);
        }
        
        /// <summary>
        /// Set a tile's cover type
        /// </summary>
        public void SetTileCover(int x, int z, CoverType coverType)
        {
            if (!IsValidPosition(x, z))
                return;
                
            _tileData[x, z].CoverType = coverType;
            
            // Update renderers
            UpdateChunkAtPosition(x, z);
        }
        
        /// <summary>
        /// Set the occupancy state of a tile and register the occupant
        /// </summary>
        public void SetTileOccupancy(int x, int z, bool occupied, GameObject occupant = null)
        {
            if (!IsValidPosition(x, z))
                return;
                
            Vector2Int pos = new Vector2Int(x, z);
            _tileData[x, z].IsOccupied = occupied;
            
            if (occupied && occupant != null)
            {
                _occupants[pos] = occupant;
            }
            else if (!occupied)
            {
                _occupants.Remove(pos);
            }
            
            // Update renderers
            UpdateChunkAtPosition(x, z);
            
            // Invalidate path cache since occupancy changed
            _pathStringCache.Clear();
        }
        
        /// <summary>
        /// Update the chunk renderer for a specific position
        /// </summary>
        private void UpdateChunkAtPosition(int x, int z)
        {
            foreach (var renderer in _chunkRenderers)
            {
                if (renderer.ContainsPosition(x, z))
                {
                    renderer.MarkDirty();
                    break;
                }
            }
        }
        #endregion

        #region Coordinate Conversion
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int z)
        {
            return new Vector3(
                mapOrigin.x + x * cellSize + cellSize * 0.5f,
                mapOrigin.y + (IsValidPosition(x, z) ? _tileData[x, z].Elevation : 0),
                mapOrigin.z + z * cellSize + cellSize * 0.5f
            );
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return GridToWorldPosition(gridPos.x, gridPos.y);
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public bool WorldToGridPosition(Vector3 worldPos, out int x, out int z)
        {
            // Adjust for origin
            Vector3 relativePos = worldPos - mapOrigin;
            
            // Calculate grid position
            x = Mathf.FloorToInt(relativePos.x / cellSize);
            z = Mathf.FloorToInt(relativePos.z / cellSize);
            
            return IsValidPosition(x, z);
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public bool WorldToGridPosition(Vector3 worldPos, out Vector2Int gridPos)
        {
            bool result = WorldToGridPosition(worldPos, out int x, out int z);
            gridPos = new Vector2Int(x, z);
            return result;
        }
        #endregion

        #region Occupancy and Object Management
        /// <summary>
        /// Get the object occupying a tile, if any
        /// </summary>
        public GameObject GetOccupant(int x, int z)
        {
            Vector2Int pos = new Vector2Int(x, z);
            return _occupants.TryGetValue(pos, out GameObject occupant) ? occupant : null;
        }
        
        /// <summary>
        /// Get the position of an object on the grid
        /// </summary>
        public bool GetObjectPosition(GameObject obj, out Vector2Int position)
        {
            foreach (KeyValuePair<Vector2Int, GameObject> pair in _occupants)
            {
                if (pair.Value == obj)
                {
                    position = pair.Key;
                    return true;
                }
            }
            
            position = Vector2Int.zero;
            return false;
        }
        
        /// <summary>
        /// Move an object from one tile to another
        /// </summary>
        public bool MoveObject(GameObject obj, Vector2Int fromPos, Vector2Int toPos)
        {
            // Check if positions are valid
            if (!IsValidPosition(fromPos) || !IsValidPosition(toPos))
                return false;
                
            // Check if destination is walkable and not occupied
            TileData destTile = GetTileData(toPos);
            if (!destTile.IsWalkable || destTile.IsOccupied)
                return false;
                
            // Check if the object is actually at the start position
            if (!_occupants.TryGetValue(fromPos, out GameObject occupant) || occupant != obj)
                return false;
                
            // Update occupancy
            SetTileOccupancy(fromPos.x, fromPos.y, false);
            SetTileOccupancy(toPos.x, toPos.y, true, obj);
            
            // Success
            return true;
        }
        #endregion

        #region Pathfinding and Movement
        /// <summary>
        /// Find path between two points using A* pathfinding
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool ignoreOccupied = false)
        {
            // Invalid positions?
            if (!IsValidPosition(start) || !IsValidPosition(end))
                return null;
                
            // Check cache for identical path request
            string cacheKey = $"{start.x},{start.y}_{end.x},{end.y}_{ignoreOccupied}";
            
            // Use string-based cache lookup 
            if (_pathStringCache.TryGetValue(cacheKey, out List<Vector2Int> cachedPath))
            {
                Debug.Log($"Using cached path from {start} to {end}");
                return new List<Vector2Int>(cachedPath); // Return copy to prevent modifications
            }
            
            // A* implementation
            var openSet = new PriorityQueue<Vector2Int>();
            var closedSet = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var fScore = new Dictionary<Vector2Int, float>();
            
            // Initialize
            openSet.Enqueue(start, 0);
            gScore[start] = 0;
            fScore[start] = ManhattanDistance(start, end);
            
            while (openSet.Count > 0)
            {
                Vector2Int current = openSet.Dequeue();
                
                // Reached the goal
                if (current.Equals(end))
                {
                    List<Vector2Int> path = ReconstructPath(cameFrom, current);
                    // Cache result using the string key
                    _pathStringCache[cacheKey] = new List<Vector2Int>(path);
                    Debug.Log($"Found path from {start} to {end} with {path.Count} points");
                    return path;
                }
                
                closedSet.Add(current);
                
                // Check neighbors
                foreach (Vector2Int neighbor in GetNeighbors(current))
                {
                    if (closedSet.Contains(neighbor))
                        continue;
                        
                    // Get the tile data
                    TileData tile = GetTileData(neighbor);
                    
                    // Skip unwalkable tiles and occupied tiles (unless ignoring occupancy)
                    if (!tile.IsWalkable || (!ignoreOccupied && tile.IsOccupied && !neighbor.Equals(end)))
                        continue;
                        
                    // Calculate new path cost
                    float tentativeGScore = gScore[current] + tile.MovementCost;
                    
                    // Check if this is a better path
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        // Record this path
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + ManhattanDistance(neighbor, end);
                        
                        // Add to open set
                        if (!openSet.Contains(neighbor))
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
            
            // No path found
            Debug.LogWarning($"No path found from {start} to {end}");
            return null;
        }
        
        /// <summary>
        /// Get all neighboring tiles
        /// </summary>
        private List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>(4);
            
            // Four cardinal directions
            Vector2Int[] directions = new[]
            {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(0, 1),   // Up
                new Vector2Int(0, -1)   // Down
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = pos + dir;
                if (IsValidPosition(neighbor))
                    neighbors.Add(neighbor);
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Manhattan distance heuristic
        /// </summary>
        private float ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        /// <summary>
        /// Reconstruct path from A* result
        /// </summary>
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            
            return path;
        }
        
        /// <summary>
        /// Calculate movement range for a unit from a starting position
        /// </summary>
        public List<Vector2Int> CalculateMovementRange(Vector2Int start, int movementPoints)
        {
            if (!IsValidPosition(start))
                return new List<Vector2Int>();
                
            var result = new List<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<MovementNode>();
            
            // Start from the beginning
            queue.Enqueue(new MovementNode { Position = start, RemainingPoints = movementPoints });
            visited.Add(start);
            result.Add(start);
            
            while (queue.Count > 0)
            {
                MovementNode node = queue.Dequeue();
                
                // Check each neighbor
                foreach (Vector2Int neighbor in GetNeighbors(node.Position))
                {
                    // Skip if already visited
                    if (visited.Contains(neighbor))
                        continue;
                        
                    // Get the tile
                    TileData tile = GetTileData(neighbor);
                    
                    // Skip unwalkable or occupied tiles
                    if (!tile.IsWalkable || tile.IsOccupied)
                        continue;
                        
                    // Calculate movement cost
                    float cost = tile.MovementCost;
                    
                    // Check if we have enough points to move here
                    float remainingPoints = node.RemainingPoints - cost;
                    if (remainingPoints < 0)
                        continue;
                        
                    // Add to results
                    result.Add(neighbor);
                    visited.Add(neighbor);
                    
                    // Continue exploring from this node if we have points left
                    if (remainingPoints > 0)
                    {
                        queue.Enqueue(new MovementNode 
                        { 
                            Position = neighbor, 
                            RemainingPoints = remainingPoints 
                        });
                    }
                }
            }
            
            return result;
        }
        #endregion

        #region Visibility and Chunk Management
        /// <summary>
        /// Update chunk visibility based on camera position
        /// </summary>
        public void UpdateChunkVisibility(Vector3 cameraPosition)
        {
            foreach (var chunk in _chunkRenderers)
            {
                if (chunk == null)
                    continue;
                    
                // Calculate distance to chunk center
                float distance = Vector3.Distance(chunk.transform.position, cameraPosition);
                
                // Enable/disable based on distance
                chunk.gameObject.SetActive(distance <= maxVisibleDistance);
            }
        }
        #endregion

        #region Serialization Support
        /// <summary>
        /// Save the grid data to a binary file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                // Create serializable data
                SerializableGridData data = new SerializableGridData
                {
                    Width = width,
                    Height = height,
                    CellSize = cellSize,
                    MapOrigin = new SerializableVector3(mapOrigin),
                    TileData = new SerializableTileData[width * height]
                };
                
                // Copy tile data
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        int index = z * width + x;
                        TileData tile = _tileData[x, z];
                        
                        data.TileData[index] = new SerializableTileData
                        {
                            X = x,
                            Z = z,
                            TerrainType = (int)tile.TerrainType,
                            MovementCost = tile.MovementCost,
                            IsWalkable = tile.IsWalkable,
                            IsOccupied = tile.IsOccupied,
                            Elevation = tile.Elevation,
                            CoverType = (int)tile.CoverType
                        };
                    }
                }
                
                // Serialize to JSON
                string json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(filePath, json);
                
                Debug.Log($"Grid data saved to {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save grid data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load the grid data from a binary file
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            try
            {
                // Read the file
                string json = System.IO.File.ReadAllText(filePath);
                
                // Deserialize
                SerializableGridData data = JsonUtility.FromJson<SerializableGridData>(json);
                
                // Apply settings
                width = data.Width;
                height = data.Height;
                cellSize = data.CellSize;
                mapOrigin = data.MapOrigin.ToVector3();
                
                // Create the tile array
                _tileData = new TileData[width, height];
                
                // Initialize with default values
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        _tileData[x, z] = new TileData
                        {
                            Position = new Vector2Int(x, z),
                            TerrainType = TerrainType.Ground,
                            MovementCost = 1,
                            IsWalkable = true,
                            IsOccupied = false,
                            Elevation = 0,
                            CoverType = CoverType.None
                        };
                    }
                }
                
                // Apply tile data
                foreach (SerializableTileData tileData in data.TileData)
                {
                    int x = tileData.X;
                    int z = tileData.Z;
                    
                    if (IsValidPosition(x, z))
                    {
                        _tileData[x, z].TerrainType = (TerrainType)tileData.TerrainType;
                        _tileData[x, z].MovementCost = tileData.MovementCost;
                        _tileData[x, z].IsWalkable = tileData.IsWalkable;
                        _tileData[x, z].IsOccupied = tileData.IsOccupied;
                        _tileData[x, z].Elevation = tileData.Elevation;
                        _tileData[x, z].CoverType = (CoverType)tileData.CoverType;
                    }
                }
                
                Debug.Log($"Grid data loaded from {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load grid data: {e.Message}");
            }
        }
        #endregion
    }

    #region Core Tile Data
    /// <summary>
    /// Core data structure for a single tile
    /// </summary>
    [Serializable]
    public class TileData
    {
        public Vector2Int Position;
        public TerrainType TerrainType;
        public float MovementCost;
        public bool IsWalkable;
        public bool IsOccupied;
        public float Elevation;
        public CoverType CoverType;
        
        // Additional fields can be added here for game mechanics
        // For example, line of sight blockers, interactables, etc.
    }
    
    /// <summary>
    /// Types of terrain for visual representation and gameplay rules
    /// </summary>
    public enum TerrainType
    {
        Ground,
        Water,
        Mud,
        Sand,
        Road,
        Rocks,
        Metal,
        Grass,
        Snow,
        Ice,
        Lava,
        Wall,
        Obstacle
    }
    
    /// <summary>
    /// Types of cover for tactical gameplay
    /// </summary>
    public enum CoverType
    {
        None,
        Half,       // Half cover (e.g., low walls)
        Full,       // Full cover (e.g., high walls)
        Destructible // Destructible cover
    }
    #endregion

    #region Helper Classes
    /// <summary>
    /// Simple priority queue for A* pathfinding
    /// </summary>
    public class PriorityQueue<T>
    {
        private List<KeyValuePair<T, float>> _elements = new List<KeyValuePair<T, float>>();
        
        public int Count => _elements.Count;
        
        public void Enqueue(T item, float priority)
        {
            _elements.Add(new KeyValuePair<T, float>(item, priority));
        }
        
        public T Dequeue()
        {
            int bestIndex = 0;
            
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i].Value < _elements[bestIndex].Value)
                {
                    bestIndex = i;
                }
            }
            
            T bestItem = _elements[bestIndex].Key;
            _elements.RemoveAt(bestIndex);
            return bestItem;
        }
        
        public bool Contains(T item)
        {
            return _elements.Exists(x => x.Key.Equals(item));
        }
    }
    
    /// <summary>
    /// Node class for movement range calculation
    /// </summary>
    public class MovementNode
    {
        public Vector2Int Position;
        public float RemainingPoints;
    }
    
    /// <summary>
    /// Serializable grid data for saving/loading
    /// </summary>
    [Serializable]
    public class SerializableGridData
    {
        public int Width;
        public int Height;
        public float CellSize;
        public SerializableVector3 MapOrigin;
        public SerializableTileData[] TileData;
    }
    
    /// <summary>
    /// Serializable tile data for saving/loading
    /// </summary>
    [Serializable]
    public class SerializableTileData
    {
        public int X;
        public int Z;
        public int TerrainType;
        public float MovementCost;
        public bool IsWalkable;
        public bool IsOccupied;
        public float Elevation;
        public int CoverType;
    }
    
    /// <summary>
    /// Serializable Vector3 for JSON serialization
    /// </summary>
    [Serializable]
    public class SerializableVector3
    {
        public float X, Y, Z;
        
        public SerializableVector3(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }
        
        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }
    #endregion
}