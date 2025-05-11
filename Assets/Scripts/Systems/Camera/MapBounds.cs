using UnityEngine;
using DarkProtocol.Grid;

/// <summary>
/// Restricts the main camera's movement to stay within defined map boundaries.
/// Works in conjunction with GridManager to ensure the camera view never leaves the grid area.
/// </summary>
public class MapBounds : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Boundary Settings")]
    [Tooltip("Minimum bounds for camera movement (X,Z)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-5f, -5f);
    
    [Tooltip("Maximum bounds for camera movement (X,Z)")]
    [SerializeField] private Vector2 maxBounds = new Vector2(5f, 5f);
    
    [Header("Additional Settings")]
    [Tooltip("Should bounds be visualized in the editor?")]
    [SerializeField] private bool visualizeBounds = true;
    
    [Tooltip("Color for bounds visualization")]
    [SerializeField] private Color boundsColor = new Color(1f, 0.5f, 0f, 0.5f);
    
    [Tooltip("Automatically calculate bounds from GridManager?")]
    [SerializeField] private bool autoCalculateFromGrid = true;
    
    [Tooltip("Reference to the grid manager (optional, will find automatically if not set)")]
    [SerializeField] private GridManager gridManager;
    
    [Tooltip("Optional padding to add around grid bounds")]
    [SerializeField] private float gridPadding = 5.0f;
    
    [Tooltip("Visual buffer around the grid (percentage of grid size)")]
    [SerializeField] private float viewBuffer = 0.1f; // 10% extra view area
    
    #endregion

    #region Private Variables
    
    private Camera _mainCamera;
    private bool _boundsInitialized = false;
    
    #endregion

    #region Properties
    
    /// <summary>
    /// Gets or sets the minimum bounds (X,Z)
    /// </summary>
    public Vector2 MinBounds
    {
        get => minBounds;
        set
        {
            minBounds = value;
            _boundsInitialized = true;
        }
    }
    
    /// <summary>
    /// Gets or sets the maximum bounds (X,Z)
    /// </summary>
    public Vector2 MaxBounds
    {
        get => maxBounds;
        set
        {
            maxBounds = value;
            _boundsInitialized = true;
        }
    }
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        // Cache the main camera reference
        _mainCamera = Camera.main;
        
        if (_mainCamera == null)
        {
            Debug.LogError("No main camera found! MapBounds requires a camera tagged as 'MainCamera'.");
            enabled = false;
            return;
        }
        
        // Find grid manager if not assigned
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
            
            if (gridManager == null)
            {
                Debug.LogWarning("GridManager not found! Using default bounds.");
                _boundsInitialized = true;
                return;
            }
        }
        
        // Auto-calculate bounds from GridManager if enabled
        if (autoCalculateFromGrid)
        {
            CalculateBoundsFromGrid();
        }
    }
    
    private void LateUpdate()
    {
        if (_mainCamera == null || !_boundsInitialized)
            return;
        
        // Get current camera position
        Vector3 position = _mainCamera.transform.position;
        
        // Clamp X and Z within bounds (leave Y unchanged)
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.z = Mathf.Clamp(position.z, minBounds.y, maxBounds.y);
        
        // Update camera position
        _mainCamera.transform.position = position;
    }
    
    private void OnDrawGizmos()
    {
        if (!visualizeBounds)
            return;
        
        Gizmos.color = boundsColor;
        
        // Calculate the corners of the boundary rectangle
        Vector3 bottomLeft = new Vector3(minBounds.x, 0, minBounds.y);
        Vector3 bottomRight = new Vector3(maxBounds.x, 0, minBounds.y);
        Vector3 topLeft = new Vector3(minBounds.x, 0, maxBounds.y);
        Vector3 topRight = new Vector3(maxBounds.x, 0, maxBounds.y);
        
        // Draw the boundary lines
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // Draw vertical lines to indicate boundaries in 3D space
        float height = 10f; // Arbitrary height for vertical lines
        Gizmos.DrawLine(bottomLeft, bottomLeft + Vector3.up * height);
        Gizmos.DrawLine(bottomRight, bottomRight + Vector3.up * height);
        Gizmos.DrawLine(topLeft, topLeft + Vector3.up * height);
        Gizmos.DrawLine(topRight, topRight + Vector3.up * height);
        
        // Draw top square to complete the boundary box
        Vector3 heightVector = Vector3.up * height;
        Gizmos.DrawLine(bottomLeft + heightVector, bottomRight + heightVector);
        Gizmos.DrawLine(bottomRight + heightVector, topRight + heightVector);
        Gizmos.DrawLine(topRight + heightVector, topLeft + heightVector);
        Gizmos.DrawLine(topLeft + heightVector, bottomLeft + heightVector);
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Sets the camera bounds explicitly
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        _boundsInitialized = true;
        
        Debug.Log($"Camera bounds set to Min: {min}, Max: {max}");
    }
    
    /// <summary>
    /// Recalculates the bounds based on the current GridManager
    /// </summary>
    public void CalculateBoundsFromGrid()
    {
        if (gridManager == null)
        {
            Debug.LogWarning("GridManager not found! Cannot calculate bounds from grid.");
            return;
        }
        
        if (gridManager.gridData == null)
        {
            Debug.LogWarning("GridData not found! Cannot calculate bounds from grid.");
            return;
        }
        
        // Get grid dimensions from the grid data
        int width = gridManager.gridData.Width;
        int height = gridManager.gridData.Height;
        float cellSize = gridManager.gridData.CellSize;
        Vector3 mapOrigin = gridManager.gridData.MapOrigin;
        
        // Calculate total grid size
        float gridWidth = width * cellSize;
        float gridHeight = height * cellSize;
        
        // Add buffer space for better visibility beyond the edge
        float bufferX = gridWidth * viewBuffer;
        float bufferZ = gridHeight * viewBuffer;
        
        // Calculate bounds with padding and buffer
        minBounds = new Vector2(
            mapOrigin.x - gridPadding - bufferX,
            mapOrigin.z - gridPadding - bufferZ
        );
        
        maxBounds = new Vector2(
            mapOrigin.x + gridWidth + gridPadding + bufferX,
            mapOrigin.z + gridHeight + gridPadding + bufferZ
        );
        
        _boundsInitialized = true;
        
        Debug.Log($"Camera bounds calculated from grid: Min: {minBounds}, Max: {maxBounds}");
    }
    
    /// <summary>
    /// Tests if a position is within bounds
    /// </summary>
    public bool IsPositionInBounds(Vector3 position)
    {
        return position.x >= minBounds.x && position.x <= maxBounds.x &&
               position.z >= minBounds.y && position.z <= maxBounds.y;
    }
    
    #endregion
}