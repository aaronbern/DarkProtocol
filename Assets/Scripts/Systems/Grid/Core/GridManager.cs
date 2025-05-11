using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Core manager for the grid system. Handles initialization, interactions, and integration with game systems.
    /// Replaces the old GameObject-based grid system with a more efficient data-driven approach.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        #region Singleton
        public static GridManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        #endregion

        #region Inspector Fields
        [Header("Grid Data")]
        [SerializeField] public GridData gridData;
        [SerializeField] private bool createNewGridOnStart = false;
        [SerializeField] private int defaultWidth = 20;
        [SerializeField] private int defaultHeight = 20;
        [SerializeField] private float defaultCellSize = 1f;
        
        [Header("Visualization")]
        [SerializeField] private bool showGridInEditor = true;
        [SerializeField] private bool showGridInGame = true;
        [SerializeField] private Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        [SerializeField] private float gridLineWidth = 0.02f;
        
        [Header("Movement Range Visualization")]
        [SerializeField] private Color movementRangeColor = new Color(0.2f, 0.8f, 0.4f, 0.5f);
        [SerializeField] private Color pathPreviewColor = new Color(1f, 0.8f, 0.2f, 0.7f);
        
        [Header("Object References")]
        [SerializeField] private Transform gridParent;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GridOverlaySystem gridOverlaySystem;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugView = false;
        [SerializeField] private bool showPathfindingResults = true;
        #endregion

        #region Private Variables
        // Currently selected unit and its state
        private Unit _selectedUnit;
        private List<Vector2Int> _currentMovementRange = new List<Vector2Int>();
        private List<Vector2Int> _currentPath = new List<Vector2Int>();
        
        // For editor visualization
        #if UNITY_EDITOR
        private bool _hasShownGrid = false;
        #endif
        
        // Last known camera position for LOD
        private Vector3 _lastCameraPosition;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Initialize on Start in case any components need to be available first
            if (gridData == null && createNewGridOnStart)
            {
                CreateNewGrid();
            }
            
            // Initialize grid visualization
            if (gridData != null)
            {
                // Create chunk renderers if needed
                if (gridParent == null)
                {
                    gridParent = new GameObject("Grid").transform;
                    gridParent.SetParent(transform);
                }
                
                gridData.GenerateChunkRenderers(gridParent);
                
                // Register for unit selection events from other systems
                UnitSelectionController unitSelectionController = FindFirstObjectByType<UnitSelectionController>();

                if (unitSelectionController != null)
                {
                    // Use reflection to add our method to their event if it exists
                    var eventField = unitSelectionController.GetType().GetField("OnUnitSelected", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (eventField != null)
                    {
                        UnityEngine.Events.UnityEvent<Unit> onUnitSelected = 
                            (UnityEngine.Events.UnityEvent<Unit>)eventField.GetValue(unitSelectionController);
                        
                        if (onUnitSelected != null)
                        {
                            onUnitSelected.AddListener(OnUnitSelected);
                        }
                    }
                }
            }
            
            // Find main camera if not set
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // Store initial camera position
            if (mainCamera != null)
            {
                _lastCameraPosition = mainCamera.transform.position;
            }
            
            // Get reference to GridOverlaySystem if not already set
            if (gridOverlaySystem == null)
            {
                gridOverlaySystem = GetComponent<GridOverlaySystem>();
                
                if (gridOverlaySystem == null)
                {
                    gridOverlaySystem = FindFirstObjectByType<GridOverlaySystem>();
                    
                    if (gridOverlaySystem == null && Application.isPlaying)
                    {
                        // Create the overlay system component if it doesn't exist
                        gridOverlaySystem = gameObject.AddComponent<GridOverlaySystem>();
                    }
                }
            }
            
            // Set colors on the grid overlay system
            if (gridOverlaySystem != null)
            {
                gridOverlaySystem.SetMovementRangeColor(movementRangeColor);
                gridOverlaySystem.SetPathPreviewColor(pathPreviewColor);
            }
        }
        
        private void Update()
        {
            if (gridData == null) return;
            
            // Update camera position for chunk visibility
            if (mainCamera != null)
            {
                Vector3 cameraPosition = mainCamera.transform.position;
                
                // Only update if the camera has moved significantly
                if (Vector3.Distance(cameraPosition, _lastCameraPosition) > 1.0f)
                {
                    gridData.UpdateChunkVisibility(cameraPosition);
                    _lastCameraPosition = cameraPosition;
                }
            }
            
            // Handle unit movement input if a unit is selected
            if (_selectedUnit != null && _currentMovementRange.Count > 0)
            {
                HandleUnitMovementInput();
            }
        }
        
        // Draw grid lines in the editor
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (gridData == null || !showGridInEditor) return;
                
            if (!Application.isPlaying || !_hasShownGrid)
            {
                DrawGridGizmos();
                _hasShownGrid = true;
            }
        }
        #endif
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the grid system
        /// </summary>
        private void Initialize()
        {
            if (gridData != null)
            {
                // Initialize the grid data
                gridData.Initialize();
                
                Debug.Log($"Grid Manager initialized with grid: {gridData.Width}x{gridData.Height}");
            }
        }
        
        /// <summary>
        /// Create a new grid with default settings
        /// </summary>
        public void CreateNewGrid()
        {
            // Create a new grid data asset
            gridData = ScriptableObject.CreateInstance<GridData>();
            
            // Set default properties
            // Normally these would come from a saved asset or settings menu
            #if UNITY_EDITOR
            // Create a proper asset in the editor
            string path = "Assets/Resources/GridData/DefaultGrid.asset";
            UnityEditor.AssetDatabase.CreateAsset(gridData, path);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"Created new grid asset at {path}");
            #endif
            
            // Initialize with default values
            gridData.Initialize();
            
            Debug.Log($"Created new grid: {gridData.Width}x{gridData.Height}");
        }
        #endregion

        #region Public Interface Methods
        /// <summary>
        /// Check if a position is valid on the grid
        /// </summary>
        public bool IsValidPosition(int x, int z)
        {
            return gridData != null && gridData.IsValidPosition(x, z);
        }
        
        /// <summary>
        /// Get the width of the grid
        /// </summary>
        public int GetGridWidth()
        {
            return gridData?.Width ?? 0;
        }
        
        /// <summary>
        /// Get the height of the grid
        /// </summary>
        public int GetGridHeight()
        {
            return gridData?.Height ?? 0;
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int z)
        {
            return gridData?.GridToWorldPosition(x, z) ?? Vector3.zero;
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return gridData?.GridToWorldPosition(gridPosition) ?? Vector3.zero;
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public bool WorldToGridPosition(Vector3 worldPosition, out int x, out int z)
        {
            if (gridData != null)
            {
                return gridData.WorldToGridPosition(worldPosition, out x, out z);
            }
            
            x = 0;
            z = 0;
            return false;
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public bool WorldToGridPosition(Vector3 worldPosition, out Vector2Int gridPosition)
        {
            if (gridData != null)
            {
                return gridData.WorldToGridPosition(worldPosition, out gridPosition);
            }
            
            gridPosition = Vector2Int.zero;
            return false;
        }
        
        /// <summary>
        /// Get the terrain type at a specific position
        /// </summary>
        public TerrainType GetTerrainType(int x, int z)
        {
            return gridData?.GetTileData(x, z)?.TerrainType ?? TerrainType.Ground;
        }
        
        /// <summary>
        /// Centers the grid on the world origin
        /// </summary>
        public void CenterGridOnOrigin()
        {
            if (gridData == null)
            {
                Debug.LogWarning("Cannot center grid: GridData is null");
                return;
            }
            
            // Calculate what the offset should be to center the grid
            float width = gridData.Width * gridData.CellSize;
            float height = gridData.Height * gridData.CellSize;
            
            // Center the grid by setting the origin to negative half of dimensions
            Vector3 centeredOrigin = new Vector3(-width/2f, 0, -height/2f);
            
            // Update the origin in the grid data
            // This assumes you have a way to set the MapOrigin property
            var originField = gridData.GetType().GetField("mapOrigin", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
            if (originField != null)
            {
                originField.SetValue(gridData, centeredOrigin);
                Debug.Log($"Centered grid origin at {centeredOrigin}");
            }
            else
            {
                Debug.LogWarning("Could not find mapOrigin field in GridData");
            }
            
            // If grid is already created, you'll need to regenerate it
            if (gridData.Width > 0 && gridData.Height > 0)
            {
                // Regenerate the grid with the new origin
                gridData.GenerateChunkRenderers(transform);
            }
        }

        /// <summary>
        /// Creates a new grid using default settings from the inspector
        /// </summary>
        public void CreateGridWithDefaultSettings()
        {
            // Get the default values from your inspector fields
            int defaultWidth = this.defaultWidth;
            int defaultHeight = this.defaultHeight;
            float defaultCellSize = this.defaultCellSize;
            
            // Create a new grid data instance if needed
            if (gridData == null)
            {
                gridData = ScriptableObject.CreateInstance<GridData>();
            }
            
            // Set the properties explicitly
            var widthField = gridData.GetType().GetField("width", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var heightField = gridData.GetType().GetField("height", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var cellSizeField = gridData.GetType().GetField("cellSize", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
            if (widthField != null) widthField.SetValue(gridData, defaultWidth);
            if (heightField != null) heightField.SetValue(gridData, defaultHeight);
            if (cellSizeField != null) cellSizeField.SetValue(gridData, defaultCellSize);
            
            // Center the grid on the origin
            CenterGridOnOrigin();
            
            // Initialize the grid
            gridData.Initialize();
            
            // Generate chunk renderers
            gridData.GenerateChunkRenderers(transform);
            
            Debug.Log($"Created grid with size {defaultWidth}x{defaultHeight}, cell size {defaultCellSize}, centered on origin");
        }
        
        /// <summary>
        /// Set the terrain type at a specific position
        /// </summary>
        public void SetTerrainType(int x, int z, TerrainType terrainType, float movementCost = 1f)
        {
            gridData?.SetTileTerrain(x, z, terrainType, movementCost);
        }
        
        /// <summary>
        /// Check if a tile is occupied
        /// </summary>
        public bool IsTileOccupied(int x, int z)
        {
            return gridData?.GetTileData(x, z)?.IsOccupied ?? false;
        }
        
        /// <summary>
        /// Set whether a tile is occupied
        /// </summary>
        public void SetTileOccupied(int x, int z, bool occupied, GameObject occupant = null)
        {
            gridData?.SetTileOccupancy(x, z, occupied, occupant);
        }
        
        /// <summary>
        /// Find a path between two points
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool ignoreOccupied = false)
        {
            return gridData?.FindPath(start, end, ignoreOccupied);
        }
        
        /// <summary>
        /// Calculate and show the movement range for a unit
        /// </summary>
        public List<Vector2Int> ShowMovementRange(Unit unit, int movementPoints)
        {
            // Clear any existing range visualization
            ClearMovementRange();
            
            if (gridData == null || unit == null)
                return new List<Vector2Int>();
                
            // Get unit position
            if (!GetUnitGridPosition(unit, out Vector2Int unitPos))
                return new List<Vector2Int>();
                
            // Calculate movement range
            _currentMovementRange = gridData.CalculateMovementRange(unitPos, movementPoints);
            
            // Show visualization using grid overlay system
            if (gridOverlaySystem != null)
            {
                gridOverlaySystem.ShowMovementRange(_currentMovementRange);
            }
            
            return _currentMovementRange;
        }
        
        /// <summary>
        /// Clear the movement range visualization
        /// </summary>
        public void ClearMovementRange()
        {
            _currentMovementRange.Clear();
            
            // Clear visualization using grid overlay system
            if (gridOverlaySystem != null)
            {
                gridOverlaySystem.ClearMovementRange();
            }
        }
        
        /// <summary>
        /// Visualize a path between points for planning movement
        /// </summary>
        public void VisualizePath(List<Vector2Int> path)
        {
            // Clear existing path visualization
            ClearPathVisualization();
            
            if (path == null || path.Count < 2 || !showPathfindingResults)
                return;
                
            // Store the current path
            _currentPath = new List<Vector2Int>(path);
            
            // Show visualization using grid overlay system
            if (gridOverlaySystem != null)
            {
                gridOverlaySystem.ShowPathPreview(_currentPath);
            }
        }
        
        /// <summary>
        /// Clear the path visualization
        /// </summary>
        public void ClearPathVisualization()
        {
            _currentPath.Clear();
            
            // Clear visualization using grid overlay system
            if (gridOverlaySystem != null)
            {
                gridOverlaySystem.ClearPathPreview();
            }
        }
        
        /// <summary>
        /// Move a unit to a grid position
        /// </summary>
        public bool MoveUnitToPosition(Unit unit, Vector2Int targetPos)
        {
            if (gridData == null || unit == null)
                return false;
                
            // Get current position
            if (!GetUnitGridPosition(unit, out Vector2Int currentPos))
                return false;
                
            // Check if this is a valid movement
            bool isValid = _currentMovementRange.Contains(targetPos);
            
            if (!isValid)
            {
                Debug.LogWarning($"Cannot move unit to position {targetPos} - not in movement range");
                return false;
            }
            
            // Find path to the target
            List<Vector2Int> path = gridData.FindPath(currentPos, targetPos);
            
            if (path == null || path.Count <= 1)
            {
                Debug.LogWarning($"Cannot find path to position {targetPos}");
                return false;
            }
            
            // Calculate movement cost (sum of tile costs along path)
            float totalCost = 0;
            for (int i = 1; i < path.Count; i++) // Start from 1 to skip the starting tile
            {
                TileData tileData = gridData.GetTileData(path[i]);
                totalCost += tileData.MovementCost;
            }
            
            // Check if unit has enough movement points
            if (unit.CurrentMovementPoints < totalCost)
            {
                Debug.LogWarning($"Unit doesn't have enough movement points. Needs {totalCost}, has {unit.CurrentMovementPoints}");
                return false;
            }
            
            // Update occupancy
            SetTileOccupied(currentPos.x, currentPos.y, false);
            SetTileOccupied(targetPos.x, targetPos.y, true, unit.gameObject);
            
            // Move the unit visually
            Vector3 targetWorldPos = GridToWorldPosition(targetPos);
            unit.Move(targetWorldPos, Mathf.RoundToInt(totalCost));
            
            // Clear ranges and paths after movement
            ClearMovementRange();
            ClearPathVisualization();
            
            return true;
        }
        
        /// <summary>
        /// Get the grid position of a unit
        /// </summary>
        public bool GetUnitGridPosition(Unit unit, out Vector2Int position)
        {
            position = Vector2Int.zero;
            
            if (unit == null || gridData == null)
                return false;
                
            // Get unit world position
            Vector3 worldPos = unit.transform.position;
            
            // Convert to grid position
            if (gridData.WorldToGridPosition(worldPos, out position))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Register a unit at its current position
        /// </summary>
        public void RegisterUnitAtPosition(Unit unit)
        {
            if (unit == null || gridData == null)
                return;
                
            // Get unit grid position
            if (GetUnitGridPosition(unit, out Vector2Int pos))
            {
                // Set tile as occupied
                SetTileOccupied(pos.x, pos.y, true, unit.gameObject);
            }
        }
        
        /// <summary>
        /// Get a list of all units of a specific team
        /// </summary>
        public List<Unit> GetUnitsOfTeam(Unit.TeamType team)
        {
            // This is an example of how to maintain compatibility with the existing system
            return Unit.GetUnitsOfTeam(team);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle unit selection
        /// </summary>
        public void OnUnitSelected(Unit unit)
        {
            // Clear any existing movement range
            ClearMovementRange();
            ClearPathVisualization();
            
            _selectedUnit = unit;
            
            // Show movement range for the selected unit
            if (unit != null)
            {
                ShowMovementRange(unit, unit.CurrentMovementPoints);
            }
        }
        
        /// <summary>
        /// Handle unit movement input
        /// </summary>
        private void HandleUnitMovementInput()
        {
            if (_selectedUnit == null)
                return;
                
            // Check for mouse input
            if (Input.GetMouseButtonDown(0))
            {
                // Cast ray from mouse position
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Convert hit point to grid position
                    if (WorldToGridPosition(hit.point, out Vector2Int gridPos))
                    {
                        // Check if this is a valid movement tile
                        if (_currentMovementRange.Contains(gridPos))
                        {
                            // Move the unit
                            MoveUnitToPosition(_selectedUnit, gridPos);
                        }
                    }
                }
            }
            
            // If mouse is hovering over a tile, show the path to it
            if (Input.GetMouseButton(0) == false) // Only when not clicking
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Convert hit point to grid position
                    if (WorldToGridPosition(hit.point, out Vector2Int hoverPos))
                    {
                        // Check if this is in the movement range
                        if (_currentMovementRange.Contains(hoverPos))
                        {
                            // Calculate path
                            if (GetUnitGridPosition(_selectedUnit, out Vector2Int unitPos))
                            {
                                List<Vector2Int> path = gridData.FindPath(unitPos, hoverPos);
                                if (path != null && path.Count > 0)
                                {
                                    VisualizePath(path);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Visualization Methods
        /// <summary>
        /// Draw grid gizmos in the editor
        /// </summary>
        private void DrawGridGizmos()
        {
            #if UNITY_EDITOR
            if (gridData == null) return;
            
            // Set gizmo color
            Gizmos.color = gridLineColor;
            
            // Get grid properties
            int width = gridData.Width;
            int height = gridData.Height;
            float cellSize = gridData.CellSize;
            Vector3 origin = gridData.MapOrigin;
            
            // Draw horizontal lines
            for (int z = 0; z <= height; z++)
            {
                Vector3 start = origin + new Vector3(0, 0, z * cellSize);
                Vector3 end = origin + new Vector3(width * cellSize, 0, z * cellSize);
                Gizmos.DrawLine(start, end);
            }
            
            // Draw vertical lines
            for (int x = 0; x <= width; x++)
            {
                Vector3 start = origin + new Vector3(x * cellSize, 0, 0);
                Vector3 end = origin + new Vector3(x * cellSize, 0, height * cellSize);
                Gizmos.DrawLine(start, end);
            }
            #endif
        }
        #endregion
    }
}