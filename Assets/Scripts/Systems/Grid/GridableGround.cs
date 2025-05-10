using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Makes any ground object automatically generate a tactical grid.
/// Attach this to plane objects to create modular grid sections.
/// </summary>
[ExecuteInEditMode]
public class GridableGround : MonoBehaviour
{
    #region Grid Properties
    
    [Header("Grid Generation")]
    [Tooltip("Generate grid automatically on this ground object")]
    [SerializeField] private bool generateGrid = true;
    
    [Tooltip("Cell size in world units")]
    [SerializeField] private float cellSize = 1f;
    
    [Tooltip("Offset from ground surface")]
    [SerializeField] private float heightOffset = 0.01f;
    
    [Tooltip("Whether to automatically subdivide based on object size")]
    [SerializeField] private bool autoSubdivide = true;
    
    [Tooltip("Manual grid dimensions (if not using auto-subdivide)")]
    [SerializeField] private Vector2Int gridDimensions = new Vector2Int(10, 10);
    
    [Tooltip("Grid offset from object center")]
    [SerializeField] private Vector2 gridOffset = Vector2.zero;
    
    [Header("Visualization")]
    [Tooltip("Prefab for tile object")]
    [SerializeField] private GameObject tilePrefab;
    
    [Tooltip("Should grid automatically align to world grid?")]
    [SerializeField] private bool snapToWorldGrid = true;
    
    [Range(0, 1)]
    [Tooltip("Alpha transparency of grid")]
    [SerializeField] private float gridAlpha = 0.8f;
    
    [Tooltip("Default color for grid")]
    [SerializeField] private Color gridColor = new Color(0.4f, 0.6f, 1f, 0.8f);
    
    [Header("Runtime Behavior")]
    [Tooltip("Register with global GridManager on start")]
    [SerializeField] private bool registerWithGridManager = true;
    
    [Tooltip("Should this grid be walkable by default?")]
    [SerializeField] private bool isWalkable = true;
    
    [Tooltip("Movement cost modifier for this terrain (1.0 = normal)")]
    [SerializeField] private float movementCostModifier = 1.0f;
    
    #endregion

    #region Private Variables
    
    private List<Tile> _generatedTiles = new List<Tile>();
    private Bounds _objectBounds;
    private Transform _tileContainer;
    private bool _initialized = false;
    
    #endregion

    #region Unity Lifecycle
    
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            // Runtime initialization
            if (registerWithGridManager && GridManager.Instance != null)
            {
                // Register this gridable section with the GridManager
                GridManager.Instance.RegisterGridSection(this);
            }
        }
        
        if (!_initialized && generateGrid)
        {
            InitializeGrid();
        }
    }
    
    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            if (registerWithGridManager && GridManager.Instance != null)
            {
                // Unregister from GridManager
                GridManager.Instance.UnregisterGridSection(this);
            }
        }
    }
    
    private void Start()
    {
        if (!_initialized && generateGrid)
        {
            InitializeGrid();
        }
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;
            
        // If we're in the editor and not playing, reinitialize when properties change
        if (_initialized && generateGrid)
        {
            ClearGrid();
            InitializeGrid();
        }
    }
    #endif
    
    #endregion

    #region Grid Generation
    
    /// <summary>
    /// Initializes the tactical grid on this ground object
    /// </summary>
    public void InitializeGrid()
    {
        // Calculate bounds of this object
        CalculateObjectBounds();
        
        // Create container for tile objects
        CreateTileContainer();
        
        // Generate grid based on calculated bounds
        GenerateGrid();
        
        _initialized = true;
    }
    
    /// <summary>
    /// Calculates bounds of the ground object for grid generation
    /// </summary>
    private void CalculateObjectBounds()
    {
        Renderer renderer = GetComponent<Renderer>();
        
        if (renderer != null)
        {
            // Use the renderer bounds
            _objectBounds = renderer.bounds;
        }
        else
        {
            // Fallback to collider if available
            Collider collider = GetComponent<Collider>();
            
            if (collider != null)
            {
                _objectBounds = collider.bounds;
            }
            else
            {
                // No renderer or collider, use transform as fallback
                _objectBounds = new Bounds(transform.position, transform.localScale);
                Debug.LogWarning($"GridableGround on {gameObject.name} has no Renderer or Collider. Using Transform for bounds calculation.");
            }
        }
    }
    
    /// <summary>
    /// Creates a container object for organizing tiles
    /// </summary>
    private void CreateTileContainer()
    {
        // Check if container already exists
        Transform existingContainer = transform.Find("TileContainer");
        
        if (existingContainer != null)
        {
            _tileContainer = existingContainer;
            return;
        }
        
        // Create new container
        GameObject container = new GameObject("TileContainer");
        container.transform.SetParent(transform, false);
        container.transform.localPosition = Vector3.zero;
        _tileContainer = container.transform;
    }
    
    /// <summary>
    /// Generates the tactical grid based on calculated dimensions
    /// </summary>
    private void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            Debug.LogError($"GridableGround on {gameObject.name} has no tilePrefab assigned.");
            return;
        }
        
        // Calculate grid dimensions
        Vector2Int dimensions = CalculateGridDimensions();
        
        // Calculate world position of grid origin
        Vector3 gridOrigin = CalculateGridOrigin(dimensions);
        
        // Generate the grid of tiles
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int z = 0; z < dimensions.y; z++)
            {
                // Calculate tile position
                Vector3 tilePosition = gridOrigin + new Vector3(
                    x * cellSize, 
                    heightOffset, 
                    z * cellSize);
                
                // Snap to world grid if enabled
                if (snapToWorldGrid)
                {
                    tilePosition.x = Mathf.Round(tilePosition.x / cellSize) * cellSize;
                    tilePosition.z = Mathf.Round(tilePosition.z / cellSize) * cellSize;
                }
                
                // Create the tile
                GameObject tileObject = Instantiate(tilePrefab, tilePosition, Quaternion.identity, _tileContainer);
                Tile tile = tileObject.GetComponent<Tile>();
                
                if (tile != null)
                {
                    // Configure the tile
                    tile.SetCoordinates(x, z);
                    tile.SetGridSection(this);
                    
                    // Set properties
                    tile.SetWalkable(isWalkable);
                    tile.SetMovementCost(movementCostModifier);
                    
                    // Apply visualization settings
                    tile.SetAlpha(gridAlpha);
                    tile.SetColor(gridColor);
                    
                    // Add to list of generated tiles
                    _generatedTiles.Add(tile);
                }
            }
        }
        
        Debug.Log($"Generated grid of {dimensions.x}x{dimensions.y} tiles on {gameObject.name}");
    }
    
    /// <summary>
    /// Calculates the dimensions of the grid
    /// </summary>
    private Vector2Int CalculateGridDimensions()
    {
        if (!autoSubdivide)
        {
            return gridDimensions;
        }
        
        // Calculate dimensions based on bounds and cell size
        int xCount = Mathf.FloorToInt(_objectBounds.size.x / cellSize);
        int zCount = Mathf.FloorToInt(_objectBounds.size.z / cellSize);
        
        // Ensure at least 1x1
        xCount = Mathf.Max(1, xCount);
        zCount = Mathf.Max(1, zCount);
        
        return new Vector2Int(xCount, zCount);
    }
    
    /// <summary>
    /// Calculates the world position of the grid origin
    /// </summary>
    private Vector3 CalculateGridOrigin(Vector2Int dimensions)
    {
        // Start from the bottom left corner of the bounds
        Vector3 origin = new Vector3(
            _objectBounds.min.x,
            _objectBounds.max.y, // Use top of object for height
            _objectBounds.min.z
        );
        
        // Center the grid on the object
        if (autoSubdivide)
        {
            float xOffset = (_objectBounds.size.x - dimensions.x * cellSize) * 0.5f;
            float zOffset = (_objectBounds.size.z - dimensions.y * cellSize) * 0.5f;
            
            origin.x += xOffset;
            origin.z += zOffset;
        }
        
        // Apply custom offset
        origin.x += gridOffset.x;
        origin.z += gridOffset.y;
        
        return origin;
    }
    
    /// <summary>
    /// Clears all generated tiles
    /// </summary>
    public void ClearGrid()
    {
        foreach (Tile tile in _generatedTiles)
        {
            if (tile != null)
            {
                // In edit mode
                if (!Application.isPlaying)
                {
                    DestroyImmediate(tile.gameObject);
                }
                // In play mode
                else
                {
                    Destroy(tile.gameObject);
                }
            }
        }
        
        _generatedTiles.Clear();
        _initialized = false;
        
        // Clear the container
        if (_tileContainer != null)
        {
            while (_tileContainer.childCount > 0)
            {
                Transform child = _tileContainer.GetChild(0);
                
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child.gameObject);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// Regenerates the grid with current settings
    /// </summary>
    public void RegenerateGrid()
    {
        ClearGrid();
        InitializeGrid();
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Gets all tiles generated by this grid section
    /// </summary>
    public List<Tile> GetTiles()
    {
        return _generatedTiles;
    }
    
    /// <summary>
    /// Gets the bounds of this grid section
    /// </summary>
    public Bounds GetBounds()
    {
        return _objectBounds;
    }
    
    /// <summary>
    /// Gets the cell size of this grid section
    /// </summary>
    public float GetCellSize()
    {
        return cellSize;
    }
    
    /// <summary>
    /// Sets the walkability of all tiles in this grid section
    /// </summary>
    public void SetWalkable(bool walkable)
    {
        isWalkable = walkable;
        
        foreach (Tile tile in _generatedTiles)
        {
            if (tile != null)
            {
                tile.SetWalkable(walkable);
            }
        }
    }
    
    /// <summary>
    /// Sets the movement cost modifier of all tiles in this grid section
    /// </summary>
    public void SetMovementCost(float costModifier)
    {
        movementCostModifier = costModifier;
        
        foreach (Tile tile in _generatedTiles)
        {
            if (tile != null)
            {
                tile.SetMovementCost(costModifier);
            }
        }
    }
    
    /// <summary>
    /// Gets the tile at the specified local coordinates
    /// </summary>
    public Tile GetTileAt(int x, int z)
    {
        foreach (Tile tile in _generatedTiles)
        {
            if (tile != null && tile.X == x && tile.Y == z)
            {
                return tile;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Attempts to get the tile at a world position
    /// </summary>
    public bool TryGetTileAtPosition(Vector3 worldPosition, out Tile tile)
    {
        // Check if the position is within this grid section's bounds
        if (!_objectBounds.Contains(worldPosition))
        {
            tile = null;
            return false;
        }
        
        // Find the closest tile to the position
        Tile closestTile = null;
        float closestDistance = float.MaxValue;
        
        foreach (Tile t in _generatedTiles)
        {
            if (t == null) continue;
            
            float distance = Vector3.Distance(t.transform.position, worldPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTile = t;
            }
        }
        
        // Check if we found a tile within a reasonable distance
        if (closestTile != null && closestDistance <= cellSize * 0.75f)
        {
            tile = closestTile;
            return true;
        }
        
        tile = null;
        return false;
    }
    
    #endregion
}