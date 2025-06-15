using System.Collections.Generic;
using UnityEngine;
using DarkProtocol.Grid.Extensions;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Refactored GridManager that maintains compatibility with existing code
    /// while using the new service-based architecture.
    /// Acts as a facade for the new services.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
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

        #region Services
        // Services (private, only accessible through interface methods)
        private IGridService _gridService;
        private IPathfindingService _pathfindingService;
        private IUnitGridService _unitGridService;
        private IGridVisualizationService _visualizationService;
        private IGridInputService _inputService;
        private IGridSerializationService _serializationService;
        private IGridExtensionService _extensionService;
        
        // MonoBehaviour for processing input
        private GridInputProcessor _inputProcessor;
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the grid manager
        /// </summary>
        private void Initialize()
        {
            Debug.Log("Initializing GridManager with new service-based architecture");
            
            // Create services
            CreateServices();
            
            // Initialize grid
            if (gridData == null && createNewGridOnStart)
            {
                CreateNewGrid();
            }
            else if (gridData != null)
            {
                // Update Grid service with the grid data
                ((GridService)_gridService).GridData = gridData;
                _gridService.Initialize();
            }
            
            // Create input processor
            _inputProcessor = gameObject.AddComponent<GridInputProcessor>();
            _inputProcessor.Initialize(_inputService);
            
            // Initialize grid visualization
            if (gridParent == null)
            {
                gridParent = new GameObject("Grid").transform;
                gridParent.SetParent(transform);
            }
            
            // Generate chunk renderers
            if (gridData != null)
            {
                gridData.GenerateChunkRenderers(gridParent);
                
                // Find main camera if not set
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }
            }
            
            // Register for turn changed events from GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
            
            Debug.Log("GridManager initialization complete");
        }
        
        /// <summary>
        /// Create and register all grid services
        /// </summary>
        private void CreateServices()
        {
            // Create the service locator instance
            var serviceLocator = GridServiceLocator.Instance;
            
            // Create and register the grid service
            _gridService = new GridService(gridData);
            serviceLocator.RegisterService<IGridService>(_gridService);
            
            // Set default grid settings
            GridService gridServiceImpl = _gridService as GridService;
            if (gridServiceImpl != null)
            {
                gridServiceImpl.DefaultWidth = defaultWidth;
                gridServiceImpl.DefaultHeight = defaultHeight;
                gridServiceImpl.DefaultCellSize = defaultCellSize;
            }
            
            // Create and register the pathfinding service
            _pathfindingService = new PathfindingService(_gridService);
            serviceLocator.RegisterService<IPathfindingService>(_pathfindingService);
            
            // Create and register the visualization service
            _visualizationService = new GridVisualizationService(_gridService, _pathfindingService, gridOverlaySystem);
            serviceLocator.RegisterService<IGridVisualizationService>(_visualizationService);
            
            // Configure visualization colors
            ((GridVisualizationService)_visualizationService).SetMovementRangeColor(movementRangeColor);
            ((GridVisualizationService)_visualizationService).SetPathPreviewColor(pathPreviewColor);
            
            // Create and register the unit grid service
            _unitGridService = new UnitGridService(_gridService, _pathfindingService, _visualizationService);
            serviceLocator.RegisterService<IUnitGridService>(_unitGridService);
            
            // Create and register the input service
            _inputService = new GridInputService(_gridService, _unitGridService, _pathfindingService, _visualizationService);
            serviceLocator.RegisterService<IGridInputService>(_inputService);
            
            // Create and register the serialization service
            _serializationService = new GridSerializationService(_gridService);
            serviceLocator.RegisterService<IGridSerializationService>(_serializationService);
            
            // Create and register the extension service
            _extensionService = new GridExtensionService(_gridService);
            serviceLocator.RegisterService<IGridExtensionService>(_extensionService);
            
            Debug.Log("Grid services created and registered");
        }
        
        /// <summary>
        /// Handle turn state changes from the GameManager
        /// </summary>
        private void HandleTurnChanged(GameManager.TurnState newState)
        {
            if (newState == GameManager.TurnState.PlayerTurn)
            {
                // Enable input during player turn
                _inputService.EnableInput();
            }
            else
            {
                // Disable input during enemy turn or transitions
                _inputService.DisableInput();
                
                // Clear visualizations
                _visualizationService.ClearMovementRange();
                _visualizationService.ClearPathVisualization();
            }
        }
        #endregion
        
        #region Unity Lifecycle
        private void Update()
        {
            if (mainCamera != null && gridData != null)
            {
                // Update chunk visibility based on camera position
                Vector3 cameraPosition = mainCamera.transform.position;
                gridData.UpdateChunkVisibility(cameraPosition);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
            
            // Clean up services if needed
            var serviceLocator = GridServiceLocator.Instance;
            if (serviceLocator != null)
            {
                serviceLocator.ClearServices();
            }
        }
        #endregion

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Only draw if showGridInEditor is enabled
            if (!showGridInEditor || gridData == null) 
                return;
            
            // Set up the gizmo color and matrix
            Gizmos.color = gridLineColor;
            
            // Get grid dimensions
            int width = gridData.Width;
            int height = gridData.Height;
            float cellSize = gridData.CellSize;
            Vector3 origin = gridData.MapOrigin;
            
            // Draw horizontal lines (along Z axis)
            for (int z = 0; z <= height; z++)
            {
                Vector3 start = origin + new Vector3(0, 0, z * cellSize);
                Vector3 end = origin + new Vector3(width * cellSize, 0, z * cellSize);
                Gizmos.DrawLine(start, end);
            }
            
            // Draw vertical lines (along X axis)
            for (int x = 0; x <= width; x++)
            {
                Vector3 start = origin + new Vector3(x * cellSize, 0, 0);
                Vector3 end = origin + new Vector3(x * cellSize, 0, height * cellSize);
                Gizmos.DrawLine(start, end);
            }
        }
#endif
        #region Public Methods (Compatibility API)
        /// <summary>
        /// Create a new grid with default settings
        /// </summary>
        public void CreateNewGrid()
        {
            // Destroy old grid visuals
            if (gridParent != null)
            {
                for (int i = gridParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(gridParent.GetChild(i).gameObject);
                }
            }

            if (_gridService != null)
            {
                ((GridService)_gridService).CreateNewGrid();
                gridData = ((GridService)_gridService).GridData;

                // Generate new chunk renderers
                if (gridData != null)
                {
                    gridData.GenerateChunkRenderers(gridParent);
                }
            }
        }


        /// <summary>
        /// Check if a position is valid on the grid
        /// </summary>
        public bool IsValidPosition(int x, int z)
        {
            return _gridService != null && _gridService.IsValidPosition(x, z);
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
            return _gridService?.GridToWorldPosition(x, z) ?? Vector3.zero;
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return _gridService?.GridToWorldPosition(gridPosition) ?? Vector3.zero;
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public bool WorldToGridPosition(Vector3 worldPosition, out int x, out int z)
        {
            if (_gridService != null)
            {
                return _gridService.WorldToGridPosition(worldPosition, out x, out z);
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
            if (_gridService != null)
            {
                return _gridService.WorldToGridPosition(worldPosition, out gridPosition);
            }
            
            gridPosition = Vector2Int.zero;
            return false;
        }
        
        /// <summary>
        /// Get the terrain type at a specific position
        /// </summary>
        public TerrainType GetTerrainType(int x, int z)
        {
            return _gridService?.GetTerrainType(x, z) ?? TerrainType.Ground;
        }
        
        /// <summary>
        /// Set the terrain type at a specific position
        /// </summary>
        public void SetTerrainType(int x, int z, TerrainType terrainType, float movementCost = 1f)
        {
            _gridService?.SetTerrainType(x, z, terrainType, movementCost);
        }
        
        /// <summary>
        /// Check if a tile is occupied
        /// </summary>
        public bool IsTileOccupied(int x, int z)
        {
            return _gridService != null && _gridService.IsTileOccupied(x, z);
        }
        
        /// <summary>
        /// Set whether a tile is occupied
        /// </summary>
        public void SetTileOccupied(int x, int z, bool occupied, GameObject occupant = null)
        {
            _gridService?.SetTileOccupied(x, z, occupied, occupant);
        }
        
        /// <summary>
        /// Find a path between two points
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool ignoreOccupied = false)
        {
            return _pathfindingService?.FindPath(start, end, ignoreOccupied);
        }
        
        /// <summary>
        /// Calculate and show the movement range for a unit
        /// </summary>
        public List<Vector2Int> ShowMovementRange(Unit unit, int movementPoints)
        {
            return _visualizationService?.ShowMovementRange(unit, movementPoints) ?? new List<Vector2Int>();
        }
        
        /// <summary>
        /// Clear the movement range visualization
        /// </summary>
        public void ClearMovementRange()
        {
            _visualizationService?.ClearMovementRange();
        }
        
        /// <summary>
        /// Visualize a path between points for planning movement
        /// </summary>
        public void VisualizePath(List<Vector2Int> path)
        {
            _visualizationService?.VisualizePath(path);
        }
        
        /// <summary>
        /// Clear the path visualization
        /// </summary>
        public void ClearPathVisualization()
        {
            _visualizationService?.ClearPathVisualization();
        }
        
        /// <summary>
        /// Move a unit to a grid position
        /// </summary>
        public bool MoveUnitToPosition(Unit unit, Vector2Int targetPos)
        {
            return _unitGridService != null && _unitGridService.MoveUnitToPosition(unit, targetPos);
        }
        
        /// <summary>
        /// Get the grid position of a unit
        /// </summary>
        public bool GetUnitGridPosition(Unit unit, out Vector2Int position)
        {
            if (_unitGridService != null)
            {
                return _unitGridService.GetUnitGridPosition(unit, out position);
            }
            
            position = Vector2Int.zero;
            return false;
        }
        
        /// <summary>
        /// Register a unit at its current position
        /// </summary>
        public void RegisterUnitAtPosition(Unit unit)
        {
            _unitGridService?.RegisterUnitAtPosition(unit);
        }
        
        /// <summary>
        /// Get a list of all units of a specific team
        /// </summary>
        public List<Unit> GetUnitsOfTeam(Unit.TeamType team)
        {
            // This is an example of how to maintain compatibility with the existing system
            return Unit.GetUnitsOfTeam(team);
        }
        
        /// <summary>
        /// Handle unit selection
        /// </summary>
        public void OnUnitSelected(Unit unit)
        {
            _unitGridService?.OnUnitSelected(unit);
        }
        
        /// <summary>
        /// Save the grid data to a file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            _serializationService?.SaveToFile(filePath);
        }
        
        /// <summary>
        /// Load the grid data from a file
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            _serializationService?.LoadFromFile(filePath);
        }
        
        /// <summary>
        /// Register a grid extension
        /// </summary>
        public void RegisterExtension(IGridExtension extension)
        {
            _extensionService?.RegisterExtension(extension);
        }
        
        /// <summary>
        /// Get an extension of a specific type
        /// </summary>
        public T GetExtension<T>() where T : class, IGridExtension
        {
            return _extensionService?.GetExtension<T>();
        }
        #endregion
    }
    
    /// <summary>
    /// A simple MonoBehaviour to process input from the grid input service
    /// </summary>
    public class GridInputProcessor : MonoBehaviour
    {
        private IGridInputService _inputService;
        
        /// <summary>
        /// Initialize the input processor
        /// </summary>
        /// <param name="inputService">The input service</param>
        public void Initialize(IGridInputService inputService)
        {
            _inputService = inputService;
            _inputService.Initialize();
        }
        
        private void Update()
        {
            _inputService?.ProcessInput();
        }
    }
}