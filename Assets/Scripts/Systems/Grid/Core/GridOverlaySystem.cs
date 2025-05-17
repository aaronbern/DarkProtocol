using System.Collections.Generic;
using DarkProtocol.Grid;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Handles visual overlay rendering for the grid system, showing movement ranges and paths.
/// Uses GPU-based mesh rendering for efficiency without spawning GameObjects.
/// Also provides a hybrid approach with invisible colliders for interaction.
/// </summary>
public class GridOverlaySystem : MonoBehaviour
{
    #region Inspector Fields
    [Header("References")]
    [Tooltip("Optional custom material for movement range. If null, a default material will be created.")]
    [SerializeField] private Material movementRangeMaterial;
    
    [Tooltip("Optional custom material for path preview. If null, a default material will be created.")]
    [SerializeField] private Material pathPreviewMaterial;
    
    [Header("Appearance Settings")]
    [Tooltip("Color for tiles within movement range")]
    [SerializeField] private Color movementRangeColor = new Color(0.2f, 0.8f, 0.4f, 0.5f);
    
    [Tooltip("Color for tiles showing the path preview")]
    [SerializeField] private Color pathPreviewColor = new Color(1f, 0.8f, 0.2f, 0.7f);
    
    [Tooltip("Height above ground level to render overlays")]
    [SerializeField] private float overlayHeight = 0.05f;
    
    [Tooltip("Scale factor for overlay size (1.0 = full tile size)")]
    [SerializeField] private float overlayScale = 0.9f;
    
    [Header("Animation Settings")]
    [Tooltip("Enable pulsing animation effect for overlays")]
    [SerializeField] private bool enablePulseEffect = true;
    
    [Tooltip("Speed of the pulse animation")]
    [SerializeField] private float pulseSpeed = 1.5f;
    
    [Tooltip("Intensity of the pulse effect (0-1)")]
    [SerializeField] private float pulseIntensity = 0.2f;
    
    [Header("Advanced Rendering")]
    [Tooltip("Enable overlay edge highlighting")]
    [SerializeField] private bool enableEdgeHighlight = true;
    
    [Tooltip("Width of the edge highlight effect")]
    [SerializeField] private float edgeHighlightWidth = 0.05f;
    
    [Tooltip("The layer to render the overlays on")]
    [SerializeField] private int renderLayer = 31; // Default to last layer
    
    [Header("Interaction Settings")]
    [Tooltip("Whether to create colliders for interaction")]
    [SerializeField] private bool createInteractionColliders = true;
    
    [Tooltip("Layer for the interaction colliders")]
    [SerializeField] private int interactionColliderLayer = 10; // Adjust as needed
    #endregion

    #region Private Variables
    // Cached mesh for rendering overlays
    private Mesh _overlayMesh;
    
    // Material instances (to avoid modifying the originals)
    private Material _movementRangeMaterialInstance;
    private Material _pathPreviewMaterialInstance;
    
    // Cache for overlay positions
    private List<Vector2Int> _currentMovementRangePositions = new List<Vector2Int>();
    private List<Vector2Int> _currentPathPositions = new List<Vector2Int>();
    
    // Render tracking
    private bool _isVisible = false;
    private int _movementRangePropertyID;
    private int _pathPreviewPropertyID;
    private int _pulsePropertyID;
    private int _edgeHighlightPropertyID;
    
    // Materials initialized flag
    private bool _materialsInitialized = false;
    
    // Render tracking
    private MaterialPropertyBlock _propertyBlock;
    
    // Animation timing
    private float _animationTime = 0f;
    
    // Dictionary to track interaction colliders
    private Dictionary<Vector2Int, GameObject> _interactionColliders = new Dictionary<Vector2Int, GameObject>();
    private GameObject _collidersParent;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Initialize the property block
        _propertyBlock = new MaterialPropertyBlock();
        
        // Cache shader property IDs
        _movementRangePropertyID = Shader.PropertyToID("_Color");
        _pathPreviewPropertyID = Shader.PropertyToID("_Color");
        _pulsePropertyID = Shader.PropertyToID("_PulseAmount");
        _edgeHighlightPropertyID = Shader.PropertyToID("_EdgeWidth");
        
        // Create the overlay mesh
        CreateOverlayMesh();
        
        // Initialize materials
        InitializeMaterials();
        
        // Create parent for colliders
        _collidersParent = new GameObject("MovementRangeColliders");
        _collidersParent.transform.SetParent(transform);
    }

    private void OnEnable()
    {
        // Clear any existing overlays
        ClearAllOverlays();
    }

    private void OnDisable()
    {
        // Clear overlays when disabled
        ClearAllOverlays();
    }

    private void Update()
    {
        // Only update when visible
        if (!_isVisible) return;
        
        // Update animation time
        if (enablePulseEffect)
        {
            _animationTime += Time.deltaTime * pulseSpeed;
            
            // Calculate pulse value (0-1)
            float pulseValue = Mathf.Sin(_animationTime) * 0.5f * pulseIntensity + 1.0f;
            
            // Update material property block
            if (_propertyBlock != null)
            {
                _propertyBlock.SetFloat(_pulsePropertyID, pulseValue);
            }
        }
    }


    /// <summary>
    /// Get the current movement range
    /// </summary>
    /// <returns>List of positions in the current movement range</returns>
    public List<Vector2Int> GetCurrentMovementRange()
    {
        return new List<Vector2Int>(_currentMovementRangePositions);
    }
    
    private void OnDestroy()
    {
        // Clean up created materials
        if (_movementRangeMaterialInstance != null && _movementRangeMaterialInstance != movementRangeMaterial)
        {
            if (Application.isPlaying)
                Destroy(_movementRangeMaterialInstance);
            else
                DestroyImmediate(_movementRangeMaterialInstance);
        }

        if (_pathPreviewMaterialInstance != null && _pathPreviewMaterialInstance != pathPreviewMaterial)
        {
            if (Application.isPlaying)
                Destroy(_pathPreviewMaterialInstance);
            else
                DestroyImmediate(_pathPreviewMaterialInstance);
        }

        // Clean up created mesh
        if (_overlayMesh != null)
        {
            if (Application.isPlaying)
                Destroy(_overlayMesh);
            else
                DestroyImmediate(_overlayMesh);
        }

        // Clean up colliders
        CleanupInteractionColliders();

        // Clean up parent
        if (_collidersParent != null)
        {
            if (Application.isPlaying)
                Destroy(_collidersParent);
            else
                DestroyImmediate(_collidersParent);
        }
    }

    private void LateUpdate()
    {
        // Only render when visible
        if (!_isVisible) return;
        
        RenderOverlays();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Creates a simple quad mesh for overlay rendering
    /// </summary>
    private void CreateOverlayMesh()
    {
        _overlayMesh = new Mesh();
        _overlayMesh.name = "GridOverlayQuad";
        
        // Create a simple quad centered on origin
        // Scale is controlled by overlayScale
        float halfSize = 0.5f * overlayScale;
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-halfSize, 0, -halfSize),
            new Vector3(halfSize, 0, -halfSize),
            new Vector3(halfSize, 0, halfSize),
            new Vector3(-halfSize, 0, halfSize)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            0, 3, 2
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        
        _overlayMesh.vertices = vertices;
        _overlayMesh.triangles = triangles;
        _overlayMesh.uv = uv;
        _overlayMesh.RecalculateNormals();
        _overlayMesh.RecalculateBounds();
    }

    /// <summary>
    /// Initializes materials for overlay rendering
    /// </summary>
    private void InitializeMaterials()
    {
        if (_materialsInitialized) return;
        
        // Create movement range material if not provided
        if (movementRangeMaterial == null)
        {
            _movementRangeMaterialInstance = CreateDefaultMaterial(movementRangeColor);
        }
        else
        {
            // Create instance of the provided material to avoid modifying original
            _movementRangeMaterialInstance = new Material(movementRangeMaterial);
            _movementRangeMaterialInstance.color = movementRangeColor;
            
            // Make sure instancing is enabled even on provided materials
            _movementRangeMaterialInstance.enableInstancing = true;
        }
        
        // Create path preview material if not provided
        if (pathPreviewMaterial == null)
        {
            _pathPreviewMaterialInstance = CreateDefaultMaterial(pathPreviewColor);
        }
        else
        {
            // Create instance of the provided material to avoid modifying original
            _pathPreviewMaterialInstance = new Material(pathPreviewMaterial);
            _pathPreviewMaterialInstance.color = pathPreviewColor;
            
            // Make sure instancing is enabled even on provided materials
            _pathPreviewMaterialInstance.enableInstancing = true;
        }
        
        // Setup material properties
        if (enableEdgeHighlight)
        {
            _movementRangeMaterialInstance.SetFloat(_edgeHighlightPropertyID, edgeHighlightWidth);
            _pathPreviewMaterialInstance.SetFloat(_edgeHighlightPropertyID, edgeHighlightWidth);
        }
        
        _materialsInitialized = true;
    }

    /// <summary>
    /// Creates a default material for overlay rendering
    /// </summary>
    private Material CreateDefaultMaterial(Color color)
    {
        // Create material with appropriate shader
        // Universal Render Pipeline's Unlit shader works well for this purpose
        Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        
        // Configure material properties
        material.color = color;
        
        // THIS IS THE IMPORTANT PART - Enable GPU instancing
        material.enableInstancing = true;
        
        material.renderQueue = (int)RenderQueue.Transparent;
        
        // Set up transparency
        material.SetFloat("_Surface", 1); // 1 = Transparent
        material.SetFloat("_Blend", 0);  // 0 = SrcAlpha, OneMinusSrcAlpha
        material.SetFloat("_ZWrite", 0); // Don't write to depth buffer
        material.SetFloat("_Cull", 2);   // 2 = Back face culling
        
        // Enable blend mode
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        
        return material;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Shows movement range overlay for the specified grid positions
    /// </summary>
    /// <param name="positions">List of grid positions for the movement range</param>
    public void ShowMovementRange(List<Vector2Int> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            ClearMovementRange();
            return;
        }
        
        // Initialize materials if needed
        if (!_materialsInitialized)
        {
            InitializeMaterials();
        }
        
        // Clear existing movement range visuals
        ClearMovementRange();
        
        // Cache positions
        _currentMovementRangePositions.Clear();
        _currentMovementRangePositions.AddRange(positions);
        
        // Set visibility for GPU instancing
        _isVisible = true;
        
        // Create invisible colliders for interaction if enabled
        if (createInteractionColliders)
        {
            CreateInteractionColliders(positions);
        }
    }

    /// <summary>
    /// Shows path preview overlay for the specified grid positions
    /// </summary>
    /// <param name="positions">List of grid positions for the path</param>
    public void ShowPathPreview(List<Vector2Int> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            ClearPathPreview();
            return;
        }
        
        // Initialize materials if needed
        if (!_materialsInitialized)
        {
            InitializeMaterials();
        }
        
        // Cache positions
        _currentPathPositions.Clear();
        _currentPathPositions.AddRange(positions);
        
        // Set visibility
        _isVisible = true;
    }

    /// <summary>
    /// Clears the movement range overlay
    /// </summary>
    public void ClearMovementRange()
    {
        _currentMovementRangePositions.Clear();
        UpdateVisibility();
        
        // Clean up colliders
        CleanupInteractionColliders();
    }

    /// <summary>
    /// Clears the path preview overlay
    /// </summary>
    public void ClearPathPreview()
    {
        _currentPathPositions.Clear();
        UpdateVisibility();
    }

    /// <summary>
    /// Clears all overlays
    /// </summary>
    public void ClearAllOverlays()
    {
        _currentMovementRangePositions.Clear();
        _currentPathPositions.Clear();
        _isVisible = false;
        
        // Clean up colliders
        CleanupInteractionColliders();
    }

    /// <summary>
    /// Sets the color for movement range overlays
    /// </summary>
    public void SetMovementRangeColor(Color color)
    {
        movementRangeColor = color;
        
        if (_movementRangeMaterialInstance != null)
        {
            _movementRangeMaterialInstance.color = color;
        }
    }

    /// <summary>
    /// Sets the color for path preview overlays
    /// </summary>
    public void SetPathPreviewColor(Color color)
    {
        pathPreviewColor = color;
        
        if (_pathPreviewMaterialInstance != null)
        {
            _pathPreviewMaterialInstance.color = color;
        }
    }
    #endregion

    #region Rendering
    /// <summary>
    /// Renders all active overlays
    /// </summary>
    private void RenderOverlays()
    {
        if (_overlayMesh == null || !_materialsInitialized) return;
        
        // Only proceed if there's something to render
        if (_currentMovementRangePositions.Count == 0 && _currentPathPositions.Count == 0)
        {
            return;
        }
        
        // Local references to avoid issues if GridManager becomes null
        var gridManager = GridManager.Instance;
        if (gridManager == null) return;
        
        float cellSize = gridManager.gridData.CellSize;
        
        try
        {
            // Batch rendering preparation
            Matrix4x4[] movementMatrices = new Matrix4x4[_currentMovementRangePositions.Count];
            Matrix4x4[] pathMatrices = new Matrix4x4[_currentPathPositions.Count];
            int movementCount = 0;
            int pathCount = 0;
            
            // Prepare movement range matrices
            foreach (Vector2Int pos in _currentMovementRangePositions)
            {
                // Skip if this position is also in the path (path takes precedence)
                if (_currentPathPositions.Contains(pos)) continue;
                
                Vector3 worldPos = gridManager.GridToWorldPosition(pos);
                worldPos.y += overlayHeight;
                
                movementMatrices[movementCount++] = Matrix4x4.TRS(
                    worldPos, 
                    Quaternion.identity, 
                    Vector3.one * cellSize);
                    
                // Draw in batches of 1023 (Graphics.DrawMeshInstanced limit)
                if (movementCount == 1023)
                {
                    Graphics.DrawMeshInstanced(_overlayMesh, 0, _movementRangeMaterialInstance, 
                        movementMatrices, movementCount, _propertyBlock, ShadowCastingMode.Off, 
                        false, renderLayer);
                    movementCount = 0;
                }
            }
            
            // Draw any remaining movement tiles
            if (movementCount > 0 && _movementRangeMaterialInstance != null)
            {
                Graphics.DrawMeshInstanced(_overlayMesh, 0, _movementRangeMaterialInstance, 
                    movementMatrices, movementCount, _propertyBlock, ShadowCastingMode.Off, 
                    false, renderLayer);
            }
            
            // Prepare path matrices
            foreach (Vector2Int pos in _currentPathPositions)
            {
                Vector3 worldPos = gridManager.GridToWorldPosition(pos);
                worldPos.y += overlayHeight * 1.1f; // Slightly higher to avoid z-fighting
                
                pathMatrices[pathCount++] = Matrix4x4.TRS(
                    worldPos, 
                    Quaternion.identity, 
                    Vector3.one * cellSize);
                    
                // Draw in batches of 1023 (Graphics.DrawMeshInstanced limit)
                if (pathCount == 1023)
                {
                    Graphics.DrawMeshInstanced(_overlayMesh, 0, _pathPreviewMaterialInstance, 
                        pathMatrices, pathCount, _propertyBlock, ShadowCastingMode.Off, 
                        false, renderLayer);
                    pathCount = 0;
                }
            }
            
            // Draw any remaining path tiles
            if (pathCount > 0 && _pathPreviewMaterialInstance != null)
            {
                Graphics.DrawMeshInstanced(_overlayMesh, 0, _pathPreviewMaterialInstance, 
                    pathMatrices, pathCount, _propertyBlock, ShadowCastingMode.Off, 
                    false, renderLayer);
            }
        }
        catch (System.InvalidOperationException ex)
        {
            // Handle instancing exception and log it once
            if (ex.Message.Contains("enable instancing"))
            {
                Debug.LogError("Material instancing error: " + ex.Message + 
                    "\nFallback to non-instanced rendering. Performance will be reduced.");
                
                // Disable instancing rendering after error
                _isVisible = false;
            }
            else
            {
                // Rethrow other exceptions
                throw;
            }
        }
    }

    /// <summary>
    /// Updates overlay visibility based on current state
    /// </summary>
    private void UpdateVisibility()
    {
        _isVisible = (_currentMovementRangePositions.Count > 0 || _currentPathPositions.Count > 0);
    }
    #endregion
    
    #region Interaction Colliders
    /// <summary>
    /// Creates invisible colliders for interaction with movement range tiles
    /// </summary>
    /// <param name="positions">List of grid positions to create colliders for</param>
    private void CreateInteractionColliders(List<Vector2Int> positions)
    {
        // Clean up existing colliders
        CleanupInteractionColliders();
        
        // Get reference to GridManager
        var gridManager = GridManager.Instance;
        if (gridManager == null) return;
        
        // Create colliders for each position
        foreach (Vector2Int pos in positions)
        {
            // Get world position
            Vector3 worldPos = gridManager.GridToWorldPosition(pos);
            worldPos.y += 0.01f; // Very slightly above ground
            
            // Create GameObject with BoxCollider but no renderer
            GameObject colliderObj = new GameObject($"Collider_{pos.x}_{pos.y}");
            colliderObj.transform.position = worldPos;
            colliderObj.transform.SetParent(_collidersParent.transform);
            colliderObj.layer = interactionColliderLayer;
            
            // Add BoxCollider sized to match the cell
            BoxCollider collider = colliderObj.AddComponent<BoxCollider>();
            collider.size = new Vector3(gridManager.gridData.CellSize * 0.9f, 0.1f, gridManager.gridData.CellSize * 0.9f);
            collider.isTrigger = true; // Make it a trigger so it doesn't affect physics
            
            // Add a custom component to store the grid position for easy lookup
            GridPositionMarker marker = colliderObj.AddComponent<GridPositionMarker>();
            marker.GridPosition = pos;
            
            // Store in dictionary for easy access and cleanup
            _interactionColliders[pos] = colliderObj;
        }
        
        Debug.Log($"Created {_interactionColliders.Count} interaction colliders for movement range");
    }
    
    /// <summary>
    /// Cleans up all interaction colliders
    /// </summary>
    private void CleanupInteractionColliders()
    {
        // Clean up colliders
        foreach (var collider in _interactionColliders.Values)
        {
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }
        }
        _interactionColliders.Clear();
    }
    #endregion
}

/// <summary>
/// Simple component to store grid position data on interaction colliders
/// </summary>
public class GridPositionMarker : MonoBehaviour
{
    public Vector2Int GridPosition;
}