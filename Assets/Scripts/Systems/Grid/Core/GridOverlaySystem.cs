using System.Collections.Generic;
using DarkProtocol.Grid;
using UnityEngine;
using UnityEngine.Rendering;



namespace DarkProtocol.Grid
{

    /// <summary>
    /// Handles visual overlay rendering for the grid system in XCOM 2 style, showing movement ranges and paths.
    /// Uses line-based visualization with subtle grid outlines, green borders for movement range, and red lines for obstructions.
    /// </summary>
    public class GridOverlaySystem : MonoBehaviour
    {
        #region Inspector Fields
        [Header("References")]
        [Tooltip("Optional custom material for grid lines. If null, a default material will be created.")]
        [SerializeField] private Material gridLineMaterial;

        [Tooltip("Optional custom material for movement boundaries. If null, a default material will be created.")]
        [SerializeField] private Material boundaryMaterial;

        [Tooltip("Optional custom material for obstruction lines. If null, a default material will be created.")]
        [SerializeField] private Material obstructionMaterial;

        [Tooltip("Optional custom material for path preview. If null, a default material will be created.")]
        [SerializeField] private Material pathPreviewMaterial;

        [Header("XCOM-Style Appearance")]
        [Tooltip("Color for tile outline within movement range")]
        [SerializeField] private Color tileOutlineColor = new Color(1f, 1f, 1f, 0.3f);

        [Tooltip("Color for the movement boundary (XCOM-style green edge)")]
        [SerializeField] private Color movementRangeColor = new Color(0f, 1f, 0f, 1f);

        [Tooltip("Color for obstruction edges (walls, etc.)")]
        [SerializeField] private Color obstructionColor = new Color(1f, 0f, 0f, 1f);

        [Tooltip("Color for tiles showing the path preview")]
        [SerializeField] private Color pathPreviewColor = new Color(1f, 0.8f, 0.2f, 0.7f);

        [Tooltip("Height above ground level to render overlays")]
        [SerializeField] private float overlayHeight = 0.05f;

        [Tooltip("Width of the grid lines")]
        [SerializeField] private float lineWidth = 0.03f;

        [Tooltip("Width of the boundary lines (movement edge and obstructions)")]
        [SerializeField] private float boundaryLineWidth = 0.05f;

        [Header("Interaction Settings")]
        [Tooltip("Whether to create colliders for interaction")]
        [SerializeField] private bool createInteractionColliders = true;

        [Tooltip("Layer for the interaction colliders")]
        [SerializeField] private int interactionColliderLayer = 10;

        [Header("Legacy Settings")]
        [Tooltip("LEGACY: Enable standard overlay visualization instead of XCOM style")]
        [SerializeField] private bool useLegacyVisualization = false;
        #endregion

        #region Private Variables
        // Materials for visualization
        private Material _gridLineMaterialInstance;
        private Material _boundaryMaterialInstance;
        private Material _obstructionMaterialInstance;
        private Material _pathPreviewMaterialInstance;

        // Meshes for different types of visualization
        private Mesh _tileOutlineMesh;
        private Mesh _boundaryMesh;
        private Mesh _obstructionMesh;
        private Mesh _pathPreviewMesh;

        // Cache for overlay positions
        private List<Vector2Int> _currentMovementRangePositions = new List<Vector2Int>();
        private List<Vector2Int> _currentPathPositions = new List<Vector2Int>();

        // Flag for visibility
        private bool _isVisible = false;

        // Materials initialized flag
        private bool _materialsInitialized = false;

        // Dictionary to track interaction colliders
        private Dictionary<Vector2Int, GameObject> _interactionColliders = new Dictionary<Vector2Int, GameObject>();
        private GameObject _collidersParent;

        // Helper for neighbor checking
        private static readonly Vector2Int[] _directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),  // East
            new Vector2Int(0, 1),  // North
            new Vector2Int(-1, 0), // West
            new Vector2Int(0, -1)  // South
        };

        // Legacy material instances (for compatibility)
        private Material _legacyMovementRangeMaterialInstance;
        private Material _legacyPathPreviewMaterialInstance;
        private Mesh _legacyOverlayMesh;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Log("USING GRID OVERLAY VERSION: XCOM MESH LINES");

            // Initialize materials
            InitializeMaterials();

            // Create parent for colliders
            _collidersParent = new GameObject("MovementRangeColliders");
            _collidersParent.transform.SetParent(transform);

            // Create legacy overlay mesh if needed
            if (useLegacyVisualization)
            {
                CreateLegacyOverlayMesh();
            }

            // Log creation
            Debug.Log($"GridOverlaySystem initialized in {(useLegacyVisualization ? "Legacy" : "XCOM")} mode");
        }

        private void Start()
        {
            // Perform a test visualization to ensure everything is working
            if (!Application.isEditor)
            {
                List<Vector2Int> testPositions = new List<Vector2Int>();
                for (int x = 0; x < 3; x++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        testPositions.Add(new Vector2Int(x, z));
                    }
                }

                ShowMovementRange(testPositions);
                ClearMovementRange();
            }
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

        private void OnDestroy()
        {
            // Clean up created materials
            CleanupMaterial(ref _gridLineMaterialInstance);
            CleanupMaterial(ref _boundaryMaterialInstance);
            CleanupMaterial(ref _obstructionMaterialInstance);
            CleanupMaterial(ref _pathPreviewMaterialInstance);
            CleanupMaterial(ref _legacyMovementRangeMaterialInstance);
            CleanupMaterial(ref _legacyPathPreviewMaterialInstance);

            // Clean up meshes
            CleanupMesh(ref _tileOutlineMesh);
            CleanupMesh(ref _boundaryMesh);
            CleanupMesh(ref _obstructionMesh);
            CleanupMesh(ref _pathPreviewMesh);
            CleanupMesh(ref _legacyOverlayMesh);

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

            if (useLegacyVisualization)
            {
                RenderLegacyOverlays();
            }
            else
            {
                RenderXCOMStyleOverlays();
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes materials for overlay rendering
        /// </summary>
        private void InitializeMaterials()
        {
            if (_materialsInitialized) return;

            Debug.Log("Initializing GridOverlaySystem materials for XCOM-style visualization");

            if (useLegacyVisualization)
            {
                InitializeLegacyMaterials();
            }
            else
            {
                // Create grid line material for subtle white tile outlines
                if (gridLineMaterial != null)
                {
                    _gridLineMaterialInstance = new Material(gridLineMaterial);
                }
                else
                {
                    _gridLineMaterialInstance = CreateLineMaterial(tileOutlineColor, lineWidth);
                }
                _gridLineMaterialInstance.color = tileOutlineColor;

                // Create boundary material for green movement range edge
                if (boundaryMaterial != null)
                {
                    _boundaryMaterialInstance = new Material(boundaryMaterial);
                }
                else
                {
                    _boundaryMaterialInstance = CreateLineMaterial(movementRangeColor, boundaryLineWidth);
                }
                _boundaryMaterialInstance.color = movementRangeColor;

                // Create obstruction material for red walls/obstacles
                if (obstructionMaterial != null)
                {
                    _obstructionMaterialInstance = new Material(obstructionMaterial);
                }
                else
                {
                    _obstructionMaterialInstance = CreateLineMaterial(obstructionColor, boundaryLineWidth);
                }
                _obstructionMaterialInstance.color = obstructionColor;

                // Create path preview material
                if (pathPreviewMaterial != null)
                {
                    _pathPreviewMaterialInstance = new Material(pathPreviewMaterial);
                }
                else
                {
                    _pathPreviewMaterialInstance = CreateLineMaterial(pathPreviewColor, lineWidth);
                }
                _pathPreviewMaterialInstance.color = pathPreviewColor;

                // Debug info
                Debug.Log($"XCOM-style materials created: " +
                          $"GridLine={_gridLineMaterialInstance != null}, " +
                          $"Boundary={_boundaryMaterialInstance != null}, " +
                          $"Obstruction={_obstructionMaterialInstance != null}, " +
                          $"PathPreview={_pathPreviewMaterialInstance != null}");
            }

            _materialsInitialized = true;
        }

        /// <summary>
        /// Initialize legacy materials for backward compatibility
        /// </summary>
        private void InitializeLegacyMaterials()
        {
            // Create movement range material
            _legacyMovementRangeMaterialInstance = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _legacyMovementRangeMaterialInstance.color = movementRangeColor;
            _legacyMovementRangeMaterialInstance.enableInstancing = true;
            _legacyMovementRangeMaterialInstance.SetFloat("_Surface", 1); // 1 = Transparent
            _legacyMovementRangeMaterialInstance.SetFloat("_Blend", 0);  // 0 = SrcAlpha, OneMinusSrcAlpha
            _legacyMovementRangeMaterialInstance.SetFloat("_ZWrite", 0); // Don't write to depth buffer
            _legacyMovementRangeMaterialInstance.renderQueue = (int)RenderQueue.Transparent;

            // Create path preview material
            _legacyPathPreviewMaterialInstance = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _legacyPathPreviewMaterialInstance.color = pathPreviewColor;
            _legacyPathPreviewMaterialInstance.enableInstancing = true;
            _legacyPathPreviewMaterialInstance.SetFloat("_Surface", 1);
            _legacyPathPreviewMaterialInstance.SetFloat("_Blend", 0);
            _legacyPathPreviewMaterialInstance.SetFloat("_ZWrite", 0);
            _legacyPathPreviewMaterialInstance.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>
        /// Creates a material suitable for line rendering
        /// </summary>
        private Material CreateLineMaterial(Color color, float width)
        {
            Debug.Log($"Creating line material with color {color}");

            // Check if we have a base material to use
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                // Try other shader variants
                shader = Shader.Find("Unlit/Color");

                if (shader == null)
                {
                    // Last resort fallback
                    shader = Shader.Find("Standard");
                    Debug.LogWarning("Using Standard shader as fallback for grid lines - this may cause visual issues");
                }
            }

            Material material = new Material(shader);
            material.color = color;

            // Try to find appropriate shader properties
            int colorProperty = Shader.PropertyToID("_BaseColor");
            if (material.HasProperty(colorProperty))
            {
                material.SetColor(colorProperty, color);
            }
            else
            {
                colorProperty = Shader.PropertyToID("_Color");
                if (material.HasProperty(colorProperty))
                {
                    material.SetColor(colorProperty, color);
                }
            }

            // Setup for transparency
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1); // 1 = Transparent
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0);   // 0 = SrcAlpha, OneMinusSrcAlpha
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0);  // Don't write to depth buffer
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0); // No alpha clipping
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0); // No culling for lines
            }

            // Set rendering mode
            material.renderQueue = (int)RenderQueue.Transparent + 100; // Push to front of transparent queue
            material.enableInstancing = true;

            return material;
        }

        /// <summary>
        /// Creates a simple quad mesh for legacy overlay rendering
        /// </summary>
        private void CreateLegacyOverlayMesh()
        {
            _legacyOverlayMesh = new Mesh();
            _legacyOverlayMesh.name = "GridOverlayQuad";

            // Create a simple quad centered on origin
            float halfSize = 0.5f * 0.9f; // 90% of tile size

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

            _legacyOverlayMesh.vertices = vertices;
            _legacyOverlayMesh.triangles = triangles;
            _legacyOverlayMesh.uv = uv;
            _legacyOverlayMesh.RecalculateNormals();
            _legacyOverlayMesh.RecalculateBounds();
        }

        /// <summary>
        /// Helper method to clean up materials
        /// </summary>
        private void CleanupMaterial(ref Material material)
        {
            if (material != null)
            {
                if (Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);

                material = null;
            }
        }

        /// <summary>
        /// Helper method to clean up meshes
        /// </summary>
        private void CleanupMesh(ref Mesh mesh)
        {
            if (mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(mesh);
                else
                    DestroyImmediate(mesh);

                mesh = null;
            }
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

            // Generate visualization based on the chosen style
            if (useLegacyVisualization)
            {
                // No special generation needed for legacy style
            }
            else
            {
                GenerateXCOMStyleVisualization(positions);
            }

            // Set visibility
            _isVisible = true;

            // Create interaction colliders if enabled
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

            // Generate path preview based on style
            if (!useLegacyVisualization)
            {
                GeneratePathPreviewMesh(positions);
            }

            // Set visibility
            _isVisible = true;
        }

        /// <summary>
        /// Clears the movement range overlay
        /// </summary>
        public void ClearMovementRange()
        {
            _currentMovementRangePositions.Clear();

            // Clean up meshes
            CleanupMesh(ref _tileOutlineMesh);
            CleanupMesh(ref _boundaryMesh);
            CleanupMesh(ref _obstructionMesh);

            // Clean up colliders
            CleanupInteractionColliders();

            UpdateVisibility();
        }

        /// <summary>
        /// Clears the path preview overlay
        /// </summary>
        public void ClearPathPreview()
        {
            _currentPathPositions.Clear();
            CleanupMesh(ref _pathPreviewMesh);
            UpdateVisibility();
        }

        /// <summary>
        /// Clears all overlays
        /// </summary>
        public void ClearAllOverlays()
        {
            ClearMovementRange();
            ClearPathPreview();
            _isVisible = false;
        }

        /// <summary>
        /// Get the current movement range
        /// </summary>
        /// <returns>List of positions in the current movement range</returns>
        public List<Vector2Int> GetCurrentMovementRange()
        {
            return new List<Vector2Int>(_currentMovementRangePositions);
        }

        /// <summary>
        /// Sets the color for movement range overlays
        /// </summary>
        public void SetMovementRangeColor(Color color)
        {
            movementRangeColor = color;

            if (_boundaryMaterialInstance != null)
            {
                _boundaryMaterialInstance.color = color;
            }

            if (_legacyMovementRangeMaterialInstance != null)
            {
                _legacyMovementRangeMaterialInstance.color = color;
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

            if (_legacyPathPreviewMaterialInstance != null)
            {
                _legacyPathPreviewMaterialInstance.color = color;
            }
        }
        #endregion

        #region XCOM-Style Visualization
        /// <summary>
        /// Generates XCOM 2-style movement range visualization with:
        /// 1. Subtle grid outlines for reachable tiles
        /// 2. Green boundary lines around the outer perimeter of movement range
        /// 3. Red boundary lines where movement is blocked by obstacles
        /// </summary>
        /// <param name="positions">List of reachable grid positions</param>
        // Modified version of the GenerateXCOMStyleVisualization method in the GridOverlaySystem class

        private void GenerateXCOMStyleVisualization(List<Vector2Int> positions)
        {
            var gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogError("GridManager.Instance is null. Cannot generate visualization.");
                return;
            }

            // Convert to HashSet for faster lookups
            HashSet<Vector2Int> reachableTiles = new HashSet<Vector2Int>(positions);

            // Debug info
            Debug.Log($"Generating XCOM-style visualization for {reachableTiles.Count} tiles");

            // Line vertices and indices
            List<Vector3> tileOutlineVertices = new List<Vector3>();
            List<int> tileOutlineIndices = new List<int>();

            List<Vector3> boundaryVertices = new List<Vector3>();
            List<int> boundaryIndices = new List<int>();

            List<Vector3> obstructionVertices = new List<Vector3>();
            List<int> obstructionIndices = new List<int>();

            int tileOutlineVertexCount = 0;
            int boundaryVertexCount = 0;
            int obstructionVertexCount = 0;

            float cellSize = gridManager.gridData.CellSize;
            float halfSize = cellSize / 2f;

            // Track which edges are boundaries or obstructions to avoid drawing grid lines on them
            Dictionary<(Vector2Int, int), bool> edgeBoundaryMap = new Dictionary<(Vector2Int, int), bool>();

            // First pass: identify all boundaries/obstructions
            foreach (Vector2Int tile in reachableTiles)
            {
                // Check each neighbor for boundary/obstruction lines
                for (int d = 0; d < _directions.Length; d++)
                {
                    Vector2Int neighbor = tile + _directions[d];

                    // Skip if the neighbor is also in the reachable set
                    if (reachableTiles.Contains(neighbor)) continue;

                    // This edge is a boundary or obstruction - mark in our map
                    // The key is (tile position, direction)
                    edgeBoundaryMap[(tile, d)] = true;
                }
            }

            // Second pass: Process each reachable tile
            foreach (Vector2Int tile in reachableTiles)
            {
                Vector3 center = gridManager.GridToWorldPosition(tile);
                center.y += overlayHeight;

                // Calculate corner positions
                Vector3 topLeft = center + new Vector3(-halfSize, 0, halfSize);
                Vector3 topRight = center + new Vector3(halfSize, 0, halfSize);
                Vector3 bottomRight = center + new Vector3(halfSize, 0, -halfSize);
                Vector3 bottomLeft = center + new Vector3(-halfSize, 0, -halfSize);

                // Add subtle grid outlines for each reachable tile (white lines)
                // Only if they're not boundary edges
                if (!edgeBoundaryMap.ContainsKey((tile, 1)) && !edgeBoundaryMap.ContainsKey((new Vector2Int(tile.x, tile.y + 1), 3)))
                {
                    // Top edge (North)
                    AddLine(tileOutlineVertices, tileOutlineIndices, ref tileOutlineVertexCount,
                            topLeft, topRight, lineWidth);
                }

                if (!edgeBoundaryMap.ContainsKey((tile, 0)) && !edgeBoundaryMap.ContainsKey((new Vector2Int(tile.x + 1, tile.y), 2)))
                {
                    // Right edge (East)
                    AddLine(tileOutlineVertices, tileOutlineIndices, ref tileOutlineVertexCount,
                            topRight, bottomRight, lineWidth);
                }

                if (!edgeBoundaryMap.ContainsKey((tile, 3)) && !edgeBoundaryMap.ContainsKey((new Vector2Int(tile.x, tile.y - 1), 1)))
                {
                    // Bottom edge (South)
                    AddLine(tileOutlineVertices, tileOutlineIndices, ref tileOutlineVertexCount,
                            bottomRight, bottomLeft, lineWidth);
                }

                if (!edgeBoundaryMap.ContainsKey((tile, 2)) && !edgeBoundaryMap.ContainsKey((new Vector2Int(tile.x - 1, tile.y), 0)))
                {
                    // Left edge (West)
                    AddLine(tileOutlineVertices, tileOutlineIndices, ref tileOutlineVertexCount,
                            bottomLeft, topLeft, lineWidth);
                }

                // Check each neighbor for boundary/obstruction lines
                for (int d = 0; d < _directions.Length; d++)
                {
                    Vector2Int neighbor = tile + _directions[d];

                    // Skip if the neighbor is also in the reachable set
                    if (reachableTiles.Contains(neighbor)) continue;

                    // Get the vertices for this edge based on direction
                    Vector3 start, end;
                    switch (d)
                    {
                        case 0: // East
                            start = topRight;
                            end = bottomRight;
                            break;
                        case 1: // North
                            start = topLeft;
                            end = topRight;
                            break;
                        case 2: // West
                            start = bottomLeft;
                            end = topLeft;
                            break;
                        default: // South
                            start = bottomRight;
                            end = bottomLeft;
                            break;
                    }

                    // Check if the neighbor position is valid and walkable
                    bool isOutOfBounds = !gridManager.IsValidPosition(neighbor.x, neighbor.y);
                    bool isBlocked = false;

                    if (!isOutOfBounds)
                    {
                        // Check if the tile is occupied or otherwise blocked
                        isBlocked = gridManager.IsTileOccupied(neighbor.x, neighbor.y);

                        // If we have a TileData getter, also check terrain type
                        TerrainType terrainType = gridManager.GetTerrainType(neighbor.x, neighbor.y);
                        if (terrainType == TerrainType.Wall || terrainType == TerrainType.Obstacle)
                        {
                            isBlocked = true;
                        }
                    }

                    // If out of bounds or blocked, add red obstruction line
                    // Otherwise, add green boundary line
                    if (isOutOfBounds || isBlocked)
                    {
                        // Red obstruction line for walls/obstacles/out of bounds
                        AddLine(obstructionVertices, obstructionIndices, ref obstructionVertexCount,
                                start, end, boundaryLineWidth);
                    }
                    else
                    {
                        // Green boundary line for edge of movement range
                        AddLine(boundaryVertices, boundaryIndices, ref boundaryVertexCount,
                                start, end, boundaryLineWidth);
                    }
                }
            }

            // Debug info
            Debug.Log($"Created mesh elements: Outlines={tileOutlineVertexCount / 4}, Boundaries={boundaryVertexCount / 4}, Obstructions={obstructionVertexCount / 4}");

            // Create and assign meshes
            _tileOutlineMesh = CreateMeshFromLines(tileOutlineVertices, tileOutlineIndices, "TileOutlineMesh");
            _boundaryMesh = CreateMeshFromLines(boundaryVertices, boundaryIndices, "BoundaryMesh");
            _obstructionMesh = CreateMeshFromLines(obstructionVertices, obstructionIndices, "ObstructionMesh");
        }

        /// <summary>
        /// Creates a path preview mesh
        /// </summary>
        private void GeneratePathPreviewMesh(List<Vector2Int> pathPositions)
        {
            var gridManager = GridManager.Instance;
            if (gridManager == null) return;

            // Clean up existing mesh
            CleanupMesh(ref _pathPreviewMesh);

            // Line vertices and indices
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            int vertexCount = 0;

            float cellSize = gridManager.gridData.CellSize;
            float halfSize = cellSize / 2f;

            // Process each path position
            for (int i = 0; i < pathPositions.Count; i++)
            {
                Vector3 center = gridManager.GridToWorldPosition(pathPositions[i]);
                center.y += overlayHeight * 1.1f; // Slightly higher to avoid z-fighting

                // Calculate corner positions
                Vector3 topLeft = center + new Vector3(-halfSize, 0, halfSize);
                Vector3 topRight = center + new Vector3(halfSize, 0, halfSize);
                Vector3 bottomRight = center + new Vector3(halfSize, 0, -halfSize);
                Vector3 bottomLeft = center + new Vector3(-halfSize, 0, -halfSize);

                // Add lines for tile outline
                AddLine(vertices, indices, ref vertexCount, topLeft, topRight, lineWidth * 1.5f);
                AddLine(vertices, indices, ref vertexCount, topRight, bottomRight, lineWidth * 1.5f);
                AddLine(vertices, indices, ref vertexCount, bottomRight, bottomLeft, lineWidth * 1.5f);
                AddLine(vertices, indices, ref vertexCount, bottomLeft, topLeft, lineWidth * 1.5f);
            }

            // Create path preview mesh
            _pathPreviewMesh = CreateMeshFromLines(vertices, indices, "PathPreviewMesh");
        }

        /// <summary>
        /// Helper method to add a line to vertex and index lists
        /// </summary>
        private void AddLine(List<Vector3> vertices, List<int> indices, ref int vertexCount,
                         Vector3 start, Vector3 end, float width)
        {
            // Line direction
            Vector3 lineDir = (end - start).normalized;

            // Perpendicular direction in horizontal plane
            Vector3 perpDir = new Vector3(lineDir.z, 0, -lineDir.x).normalized;

            // Calculate corners of the quad
            Vector3 v0 = start + perpDir * width / 2;
            Vector3 v1 = start - perpDir * width / 2;
            Vector3 v2 = end - perpDir * width / 2;
            Vector3 v3 = end + perpDir * width / 2;

            // Add vertices
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            // Add indices for two triangles
            indices.Add(vertexCount);
            indices.Add(vertexCount + 2);
            indices.Add(vertexCount + 1);

            indices.Add(vertexCount);
            indices.Add(vertexCount + 3);
            indices.Add(vertexCount + 2);

            // Update vertex count
            vertexCount += 4;
        }

        /// <summary>
        /// Creates a mesh from line vertices and indices
        /// </summary>
        private Mesh CreateMeshFromLines(List<Vector3> vertices, List<int> indices, string meshName)
        {
            if (vertices == null || indices == null)
            {
                Debug.LogError($"Cannot create mesh {meshName}: vertices or indices is null");
                return null;
            }

            if (vertices.Count == 0 || indices.Count == 0)
            {
                Debug.LogWarning($"Cannot create mesh {meshName}: vertices or indices is empty");
                return null;
            }

            // Ensure we don't exceed vertex limits
            if (vertices.Count > 65000)
            {
                Debug.LogError($"Mesh {meshName} exceeds vertex limit: {vertices.Count}/65000. Truncating.");
                vertices = vertices.GetRange(0, 65000);
                // Adjust indices to match
                int lastValidIndex = 65000 - 1;
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] > lastValidIndex)
                    {
                        indices.RemoveRange(i, indices.Count - i);
                        break;
                    }
                }
            }

            try
            {
                Mesh mesh = new Mesh();
                mesh.name = meshName;

                mesh.SetVertices(vertices);
                mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                Debug.Log($"Successfully created mesh {meshName} with {vertices.Count} vertices and {indices.Count} indices");

                return mesh;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error creating mesh {meshName}: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Rendering
        /// <summary>
        /// Renders XCOM-style overlays
        /// </summary>
        private void RenderXCOMStyleOverlays()
        {
            // Debug info to help troubleshooting
            if (_tileOutlineMesh == null || _boundaryMesh == null || _obstructionMesh == null)
            {
                Debug.LogWarning($"[GridOverlaySystem] Missing meshes: TileOutline={_tileOutlineMesh != null}, Boundary={_boundaryMesh != null}, Obstruction={_obstructionMesh != null}");
            }

            // Ensure materials are initialized
            if (!_materialsInitialized || _gridLineMaterialInstance == null || _boundaryMaterialInstance == null || _obstructionMaterialInstance == null)
            {
                InitializeMaterials();
            }

            // Render tile outlines - these are the subtle white grid lines
            if (_tileOutlineMesh != null && _gridLineMaterialInstance != null)
            {
                Graphics.DrawMesh(_tileOutlineMesh, Matrix4x4.identity, _gridLineMaterialInstance,
                    0, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            }

            // Render movement boundary (green edges)
            if (_boundaryMesh != null && _boundaryMaterialInstance != null)
            {
                Graphics.DrawMesh(_boundaryMesh, Matrix4x4.identity, _boundaryMaterialInstance,
                    0, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            }

            // Render obstructions (red edges)
            if (_obstructionMesh != null && _obstructionMaterialInstance != null)
            {
                Graphics.DrawMesh(_obstructionMesh, Matrix4x4.identity, _obstructionMaterialInstance,
                    0, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            }

            // Render path preview
            if (_pathPreviewMesh != null && _pathPreviewMaterialInstance != null)
            {
                Graphics.DrawMesh(_pathPreviewMesh, Matrix4x4.identity, _pathPreviewMaterialInstance,
                    0, null, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            }
        }

        /// <summary>
        /// Renders legacy style overlays
        /// </summary>
        private void RenderLegacyOverlays()
        {
            if (_legacyOverlayMesh == null) return;

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
                        Graphics.DrawMeshInstanced(_legacyOverlayMesh, 0, _legacyMovementRangeMaterialInstance,
                            movementMatrices, movementCount, null, ShadowCastingMode.Off,
                            false, 0);
                        movementCount = 0;
                    }
                }

                // Draw any remaining movement tiles
                if (movementCount > 0 && _legacyMovementRangeMaterialInstance != null)
                {
                    Graphics.DrawMeshInstanced(_legacyOverlayMesh, 0, _legacyMovementRangeMaterialInstance,
                        movementMatrices, movementCount, null, ShadowCastingMode.Off,
                        false, 0);
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
                        Graphics.DrawMeshInstanced(_legacyOverlayMesh, 0, _legacyPathPreviewMaterialInstance,
                            pathMatrices, pathCount, null, ShadowCastingMode.Off,
                            false, 0);
                        pathCount = 0;
                    }
                }

                // Draw any remaining path tiles
                if (pathCount > 0 && _legacyPathPreviewMaterialInstance != null)
                {
                    Graphics.DrawMeshInstanced(_legacyOverlayMesh, 0, _legacyPathPreviewMaterialInstance,
                        pathMatrices, pathCount, null, ShadowCastingMode.Off,
                        false, 0);
                }
            }
            catch (System.InvalidOperationException ex)
            {
                // Handle instancing exception
                Debug.LogError("Material instancing error: " + ex.Message);
                _isVisible = false;
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
                OverlayGridPositionMarker marker = colliderObj.AddComponent<OverlayGridPositionMarker>();
                marker.GridPosition = pos;

                // Store in dictionary for easy access and cleanup
                _interactionColliders[pos] = colliderObj;
            }
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
    public class OverlayGridPositionMarker : MonoBehaviour
    {
        public Vector2Int GridPosition;
    }
}