using UnityEngine;

/// <summary>
/// Represents a single tile in the Dark Protocol grid-based tactical map.
/// Enhanced to work with the modular GridableGround system.
/// </summary>
public class Tile : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Tile Properties")]
    [SerializeField] private int x;
    [SerializeField] private int y;
    [SerializeField] private bool isOccupied;
    [SerializeField] private bool isWalkable = true;
    [SerializeField] private float movementCost = 1.0f;
    
    [Header("Visualization")]
    [SerializeField] private GameObject cornerPrefab;
    [SerializeField] private float cornerSize = 0.1f;
    [SerializeField] private float cornerLength = 0.2f;
    [SerializeField] private Color defaultCornerColor = new Color(0.6f, 0.8f, 1.0f, 0.8f);
    [SerializeField] private Color occupiedCornerColor = new Color(1.0f, 0.4f, 0.4f, 0.8f);
    [SerializeField] private Color highlightedCornerColor = new Color(1.0f, 1.0f, 0.4f, 0.9f);
    [SerializeField] private Color unwalkableCornerColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    [SerializeField] private Color movementRangeColor = new Color(0.3f, 0.9f, 0.6f, 0.8f);
    [SerializeField] private Color attackRangeColor = new Color(1.0f, 0.5f, 0.2f, 0.8f);
    [SerializeField] private bool useCornerMarkers = true;
    [SerializeField] private Vector3 tileSize = Vector3.one;
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float gizmoAlpha = 0.2f;
    
    #endregion

    #region Private Variables
    
    private GameObject[] _corners = new GameObject[4];
    private Transform _tileTransform;
    private bool _isHighlighted = false;
    private bool _isInMovementRange = false;
    private bool _isInAttackRange = false;
    private GridableGround _parentGridSection;
    private float _defaultAlpha = 0.8f;
    
    #endregion

    #region Public Properties
    
    /// <summary>
    /// Gets or sets the X coordinate of the tile
    /// </summary>
    public int X
    {
        get => x;
        set
        {
            x = value;
            UpdateName();
        }
    }
    
    /// <summary>
    /// Gets or sets the Y coordinate of the tile
    /// </summary>
    public int Y
    {
        get => y;
        set
        {
            y = value;
            UpdateName();
        }
    }
    
    /// <summary>
    /// Gets or sets whether the tile is occupied
    /// </summary>
    public bool IsOccupied
    {
        get => isOccupied;
        set => SetOccupied(value);
    }
    
    /// <summary>
    /// Gets whether the tile is currently highlighted
    /// </summary>
    public bool IsHighlighted => _isHighlighted;
    
    /// <summary>
    /// Gets whether the tile is walkable
    /// </summary>
    public bool IsWalkable => isWalkable;
    
    /// <summary>
    /// Gets the movement cost of this tile
    /// </summary>
    public float MovementCost => movementCost;
    
    /// <summary>
    /// Gets the parent grid section this tile belongs to
    /// </summary>
    public GridableGround ParentGridSection => _parentGridSection;
    
    /// <summary>
    /// Gets whether this tile is in movement range
    /// </summary>
    public bool IsInMovementRange => _isInMovementRange;
    
    /// <summary>
    /// Gets whether this tile is in attack range
    /// </summary>
    public bool IsInAttackRange => _isInAttackRange;
    
    #endregion

    #region Unity Lifecycle Methods
    
    private void Awake()
    {
        // Cache transform
        _tileTransform = transform;
        
        // Set initial name based on coordinates
        UpdateName();
        
        // Create corner markers if enabled
        if (useCornerMarkers)
        {
            CreateCornerMarkers();
        }
    }
    
    private void Start()
    {
        // Update visuals based on initial state
        UpdateVisuals();
        
        // Register with grid manager if available
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterTile(this);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister from grid manager if available
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterTile(this);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Set gizmo color based on state
        Color gizmoColor = defaultCornerColor;
        
        if (!isWalkable)
        {
            gizmoColor = unwalkableCornerColor;
        }
        else if (_isInMovementRange)
        {
            gizmoColor = movementRangeColor;
        }
        else if (_isInAttackRange)
        {
            gizmoColor = attackRangeColor;
        }
        else if (_isHighlighted)
        {
            gizmoColor = highlightedCornerColor;
        }
        else if (isOccupied)
        {
            gizmoColor = occupiedCornerColor;
        }
        
        // Apply transparency
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoAlpha);
        
        // Only draw a very subtle outline in gizmo mode
        Gizmos.DrawWireCube(transform.position, tileSize);
    }
    
    private void Update()
    {
        // Optional: Add camera distance fading for corners
        if (Camera.main != null && _corners != null && _corners.Length > 0 && _corners[0] != null)
        {
            float distanceToCamera = Vector3.Distance(transform.position, Camera.main.transform.position);
            
            // Calculate opacity based on distance
            float opacity = Mathf.Lerp(_defaultAlpha, 0.2f, Mathf.InverseLerp(5f, 30f, distanceToCamera));
            
            // Apply opacity to all corners
            foreach (GameObject corner in _corners)
            {
                if (corner == null) continue;
                
                Renderer[] renderers = corner.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        Color currentColor = renderer.material.color;
                        renderer.material.color = new Color(
                            currentColor.r,
                            currentColor.g,
                            currentColor.b,
                            opacity
                        );
                    }
                }
            }
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Sets whether the tile is occupied and updates visuals
    /// </summary>
    /// <param name="occupied">Is the tile occupied?</param>
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        
        // Update visuals
        UpdateVisuals();
    }
    
    /// <summary>
    /// Sets the tile's coordinates
    /// </summary>
    /// <param name="newX">X coordinate</param>
    /// <param name="newY">Y coordinate</param>
    public void SetCoordinates(int newX, int newY)
    {
        x = newX;
        y = newY;
        UpdateName();
    }
    
    /// <summary>
    /// Sets the highlight state of the tile
    /// </summary>
    /// <param name="highlighted">Should the tile be highlighted?</param>
    public void SetHighlighted(bool highlighted)
    {
        _isHighlighted = highlighted;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Sets whether this tile is in movement range
    /// </summary>
    /// <param name="inRange">Is the tile in movement range?</param>
    public void SetInMovementRange(bool inRange)
    {
        if (_isInMovementRange != inRange)
        {
            _isInMovementRange = inRange;
            UpdateVisuals();
        }
    }
    
    /// <summary>
    /// Sets whether this tile is in attack range
    /// </summary>
    /// <param name="inRange">Is the tile in attack range?</param>
    public void SetInAttackRange(bool inRange)
    {
        if (_isInAttackRange != inRange)
        {
            _isInAttackRange = inRange;
            UpdateVisuals();
        }
    }
    
    /// <summary>
    /// Sets whether this tile is walkable
    /// </summary>
    /// <param name="walkable">Is the tile walkable?</param>
    public void SetWalkable(bool walkable)
    {
        if (isWalkable != walkable)
        {
            isWalkable = walkable;
            UpdateVisuals();
        }
    }
    
    /// <summary>
    /// Sets the movement cost for this tile
    /// </summary>
    /// <param name="cost">Movement cost (1.0 = normal)</param>
    public void SetMovementCost(float cost)
    {
        movementCost = Mathf.Max(0.1f, cost);
    }
    
    /// <summary>
    /// Sets the parent grid section for this tile
    /// </summary>
    /// <param name="gridSection">The parent GridableGround</param>
    public void SetGridSection(GridableGround gridSection)
    {
        _parentGridSection = gridSection;
    }
    
    /// <summary>
    /// Sets the base color for this tile's corners
    /// </summary>
    /// <param name="color">The new color</param>
    public void SetColor(Color color)
    {
        defaultCornerColor = color;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Sets the alpha transparency for this tile's corners
    /// </summary>
    /// <param name="alpha">Alpha value (0-1)</param>
    public void SetAlpha(float alpha)
    {
        _defaultAlpha = Mathf.Clamp01(alpha);
        
        // Apply to colors
        defaultCornerColor = new Color(defaultCornerColor.r, defaultCornerColor.g, defaultCornerColor.b, _defaultAlpha);
        occupiedCornerColor = new Color(occupiedCornerColor.r, occupiedCornerColor.g, occupiedCornerColor.b, _defaultAlpha);
        highlightedCornerColor = new Color(highlightedCornerColor.r, highlightedCornerColor.g, highlightedCornerColor.b, _defaultAlpha);
        unwalkableCornerColor = new Color(unwalkableCornerColor.r, unwalkableCornerColor.g, unwalkableCornerColor.b, _defaultAlpha);
        movementRangeColor = new Color(movementRangeColor.r, movementRangeColor.g, movementRangeColor.b, _defaultAlpha);
        attackRangeColor = new Color(attackRangeColor.r, attackRangeColor.g, attackRangeColor.b, _defaultAlpha);
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Gets the world position of this tile
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }
    
    /// <summary>
    /// Clears all tactical state flags
    /// </summary>
    public void ClearTacticalState()
    {
        _isInMovementRange = false;
        _isInAttackRange = false;
        UpdateVisuals();
    }
    
    #endregion

    #region Private Methods
    
    /// <summary>
    /// Updates the GameObject name to reflect its coordinates
    /// </summary>
    private void UpdateName()
    {
        gameObject.name = $"Tile ({x}, {y})";
    }
    
    /// <summary>
    /// Creates the corner markers for the tile
    /// </summary>
    private void CreateCornerMarkers()
    {
        if (cornerPrefab == null)
        {
            // Create a simple corner marker if no prefab is provided
            CreateSimpleCornerMarkers();
            return;
        }
        
        // Calculate half size for positioning
        float halfWidth = tileSize.x * 0.5f;
        float halfHeight = tileSize.z * 0.5f;
        
        // Corner positions (top-left, top-right, bottom-right, bottom-left)
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-halfWidth, 0, halfHeight),
            new Vector3(halfWidth, 0, halfHeight),
            new Vector3(halfWidth, 0, -halfHeight),
            new Vector3(-halfWidth, 0, -halfHeight)
        };
        
        // Corner rotations (adjust as needed based on your corner prefab design)
        Quaternion[] rotations = new Quaternion[]
        {
            Quaternion.Euler(0, 0, 0),
            Quaternion.Euler(0, 90, 0),
            Quaternion.Euler(0, 180, 0),
            Quaternion.Euler(0, 270, 0)
        };
        
        // Create and position each corner
        for (int i = 0; i < 4; i++)
        {
            _corners[i] = Instantiate(cornerPrefab, _tileTransform);
            _corners[i].transform.localPosition = positions[i];
            _corners[i].transform.localRotation = rotations[i];
            
            // Set color
            Renderer renderer = _corners[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = defaultCornerColor;
            }
        }
    }
    
    /// <summary>
    /// Creates simple corner markers using primitive cubes
    /// </summary>
    private void CreateSimpleCornerMarkers()
    {
        // Calculate half size for positioning
        float halfWidth = tileSize.x * 0.5f;
        float halfHeight = tileSize.z * 0.5f;
        
        // Corner positions
        Vector3[] positions = new Vector3[4]
        {
            new Vector3(-halfWidth, 0, halfHeight),  // Top-left
            new Vector3(halfWidth, 0, halfHeight),   // Top-right
            new Vector3(halfWidth, 0, -halfHeight),  // Bottom-right
            new Vector3(-halfWidth, 0, -halfHeight)  // Bottom-left
        };
        
        // Create corner markers
        for (int i = 0; i < 4; i++)
        {
            // Create parent for each corner to organize the L shape
            GameObject cornerParent = new GameObject($"Corner_{i}");
            cornerParent.transform.SetParent(_tileTransform, false);
            cornerParent.transform.localPosition = positions[i];
            _corners[i] = cornerParent;
            
            // Create first line of the L-shape
            GameObject horizontalLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            horizontalLine.transform.SetParent(cornerParent.transform, false);
            
            // Adjust scale based on the corner settings
            horizontalLine.transform.localScale = new Vector3(cornerLength, cornerSize, cornerSize);
            
            // Position the line to form an L shape
            if (i == 0) // Top-left
            {
                horizontalLine.transform.localPosition = new Vector3(cornerLength/2, 0, 0);
                
                GameObject verticalLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                verticalLine.transform.SetParent(cornerParent.transform, false);
                verticalLine.transform.localScale = new Vector3(cornerSize, cornerSize, cornerLength);
                verticalLine.transform.localPosition = new Vector3(0, 0, -cornerLength/2);
            }
            else if (i == 1) // Top-right
            {
                horizontalLine.transform.localPosition = new Vector3(-cornerLength/2, 0, 0);
                
                GameObject verticalLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                verticalLine.transform.SetParent(cornerParent.transform, false);
                verticalLine.transform.localScale = new Vector3(cornerSize, cornerSize, cornerLength);
                verticalLine.transform.localPosition = new Vector3(0, 0, -cornerLength/2);
            }
            else if (i == 2) // Bottom-right
            {
                horizontalLine.transform.localPosition = new Vector3(-cornerLength/2, 0, 0);
                
                GameObject verticalLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                verticalLine.transform.SetParent(cornerParent.transform, false);
                verticalLine.transform.localScale = new Vector3(cornerSize, cornerSize, cornerLength);
                verticalLine.transform.localPosition = new Vector3(0, 0, cornerLength/2);
            }
            else // Bottom-left
            {
                horizontalLine.transform.localPosition = new Vector3(cornerLength/2, 0, 0);
                
                GameObject verticalLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                verticalLine.transform.SetParent(cornerParent.transform, false);
                verticalLine.transform.localScale = new Vector3(cornerSize, cornerSize, cornerLength);
                verticalLine.transform.localPosition = new Vector3(0, 0, cornerLength/2);
            }
            
            // Get all renderers in this corner
            Renderer[] renderers = cornerParent.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = defaultCornerColor;
            }
        }
    }
    
    /// <summary>
    /// Updates the visual representation of the tile based on its state
    /// </summary>
    private void UpdateVisuals()
    {
        if (!useCornerMarkers || _corners == null || _corners[0] == null)
            return;
            
        // Determine the appropriate color based on state
        Color cornerColor = defaultCornerColor;
        
        // Priority order for visualization
        if (!isWalkable)
        {
            cornerColor = unwalkableCornerColor;
        }
        else if (_isHighlighted)
        {
            cornerColor = highlightedCornerColor;
        }
        else if (_isInMovementRange)
        {
            cornerColor = movementRangeColor;
        }
        else if (_isInAttackRange)
        {
            cornerColor = attackRangeColor;
        }
        else if (isOccupied)
        {
            cornerColor = occupiedCornerColor;
        }
        
        // Update each corner's color
        foreach (GameObject corner in _corners)
        {
            if (corner == null) continue;
            
            // Get all renderers in this corner
            Renderer[] renderers = corner.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Keep same alpha when updating color
                    float alpha = renderer.material.color.a;
                    renderer.material.color = new Color(cornerColor.r, cornerColor.g, cornerColor.b, alpha);
                }
            }
        }
    }
    
    #endregion
}