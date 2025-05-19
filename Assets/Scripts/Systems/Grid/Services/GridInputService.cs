using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Implementation of the grid input service
    /// </summary>
    public class GridInputService : IGridInputService
    {
        private readonly IGridService _gridService;
        private readonly IUnitGridService _unitService;
        private readonly IPathfindingService _pathfindingService;
        private readonly IGridVisualizationService _visualizationService;

        // Reference to the main camera
        private Camera _mainCamera;

        // Input enabled flag
        private bool _inputEnabled = true;

        // Reference to the unit selection controller
        private UnitSelectionController _unitSelectionController;

        // Cache for hover pathfinding performance optimization
        private Vector2Int _lastHoveredGridPos = new Vector2Int(-1, -1); // Invalid position to force first calculation
        private bool _lastHoverWasValid = false;

        // Throttling for hover processing - only process hover every few frames
        private int _hoverFrameSkip = 5; // Process hover every 5 frames
        private int _currentFrame = 0;
        private Unit _lastPathUnit = null;
        private Vector2Int _lastHoveredPosition = new Vector2Int(-1, -1);

        // Debug settings
        private bool _showDetailedDebugLogs = false;
        private bool _suppressGroundHitLogs = true; // Added to suppress common ground hit logs

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gridService">The grid service</param>
        /// <param name="unitService">The unit service</param>
        /// <param name="pathfindingService">The pathfinding service</param>
        /// <param name="visualizationService">The visualization service</param>
        public GridInputService(
            IGridService gridService,
            IUnitGridService unitService,
            IPathfindingService pathfindingService,
            IGridVisualizationService visualizationService)
        {
            _gridService = gridService;
            _unitService = unitService;
            _pathfindingService = pathfindingService;
            _visualizationService = visualizationService;
        }

        /// <summary>
        /// Initialize the input service
        /// </summary>
        public void Initialize()
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
            {
                Debug.LogWarning("No main camera found! GridInputService requires a camera tagged as 'MainCamera'.");
            }

            _unitSelectionController = Object.FindFirstObjectByType<UnitSelectionController>();

            if (_unitSelectionController == null)
            {
                Debug.LogWarning("UnitSelectionController not found! Some input functionality may not work.");
            }

            Debug.Log("GridInputService initialized");
        }

        /// <summary>
        /// Enable grid input handling
        /// </summary>
        public void EnableInput()
        {
            _inputEnabled = true;
            // Reset hover cache to force recalculation on next hover
            _lastHoveredGridPos = new Vector2Int(-1, -1);
            DebugLog("Grid input enabled");
        }

        /// <summary>
        /// Disable grid input handling
        /// </summary>
        public void DisableInput()
        {
            _inputEnabled = false;
            DebugLog("Grid input disabled");
        }

        /// <summary>
        /// Process input for grid interaction
        /// </summary>
        public void ProcessInput()
        {
            if (!_inputEnabled || _mainCamera == null)
                return;

            // Check if it's the player's turn
            if (GameManager.Instance != null && !GameManager.Instance.IsPlayerTurn())
            {
                // Not player's turn, clear any path
                _visualizationService?.ClearPathVisualization();
                return;
            }

            // Get current mouse position
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            // Cast ray from mouse position
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

            // Check for mouse button clicks
            bool leftMousePressed = Mouse.current.leftButton.wasPressedThisFrame;
            bool rightMousePressed = Mouse.current.rightButton.wasPressedThisFrame;

            if (rightMousePressed)
            {
                HandleRightMouseClick(ray);
            }
            else if (!leftMousePressed && !Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed)
            {
                // For hover, implement frame throttling
                _currentFrame++;
                if (_currentFrame >= _hoverFrameSkip)
                {
                    _currentFrame = 0;
                    HandleMouseHover(ray);
                }
            }
        }

        /// <summary>
        /// Handle right mouse click for unit movement
        /// </summary>
        /// <param name="ray">Ray from mouse position</param>
        private void HandleRightMouseClick(Ray ray)
        {
            // Get the currently selected unit
            Unit selectedUnit = Unit.SelectedUnit;

            if (selectedUnit == null)
                return;

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                DebugLog($"Right click detected - hit: {hit.collider.gameObject.name}");

                // Check if we hit a movement range collider
                GridPositionMarker marker = hit.collider.GetComponent<GridPositionMarker>();
                if (marker != null)
                {
                    Vector2Int gridPos = marker.GridPosition;

                    // Check if tile is occupied
                    if (_gridService.IsTileOccupied(gridPos.x, gridPos.y))
                    {
                        Debug.LogWarning($"Cannot move to position {gridPos} - tile is occupied");
                        return;
                    }

                    // Move the unit
                    _unitService.MoveUnitToPosition(selectedUnit, gridPos);
                }
                else
                {
                    // Try to convert hit point to grid position as fallback
                    if (_gridService.WorldToGridPosition(hit.point, out Vector2Int gridPos))
                    {
                        // Check if tile is occupied
                        if (_gridService.IsTileOccupied(gridPos.x, gridPos.y))
                        {
                            Debug.LogWarning($"Cannot move to position {gridPos} - tile is occupied");
                            return;
                        }

                        // Get the unit's movement range from the visualization service
                        var unitGridPos = Vector2Int.zero;
                        _unitService.GetUnitGridPosition(selectedUnit, out unitGridPos);

                        // Calculate movement range
                        List<Vector2Int> movementRange = _pathfindingService.CalculateMovementRange(unitGridPos, selectedUnit.CurrentMovementPoints);

                        // Check if this is a valid movement tile
                        if (movementRange.Contains(gridPos))
                        {
                            DebugLog($"Moving unit to grid position: {gridPos}");

                            // Move the unit
                            _unitService.MoveUnitToPosition(selectedUnit, gridPos);
                        }
                        else
                        {
                            DebugLog($"Position {gridPos} is not in movement range");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handle mouse hover for path visualization
        /// </summary>
        private void HandleMouseHover(Ray ray)
        {
            // Get the currently active unit, not just the selected one
            Unit activeUnit = GameManager.Instance?.ActiveUnit ?? Unit.SelectedUnit;

            if (activeUnit == null)
            {
                // No active unit, clear path
                _visualizationService.ClearPathVisualization();
                _lastPathUnit = null;
                _lastHoveredPosition = new Vector2Int(-1, -1);
                return;
            }

            // Check if the unit has changed
            if (_lastPathUnit != activeUnit)
            {
                // Unit changed, clear path
                _visualizationService.ClearPathVisualization();
                _lastPathUnit = activeUnit;
                _lastHoveredPosition = new Vector2Int(-1, -1);
            }

            bool foundValidPosition = false;
            Vector2Int hoveredGridPos = Vector2Int.zero;

            // Cast ray
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Only log if it's not a ground hit or if we're not suppressing ground logs
                string hitObjectName = hit.collider.gameObject.name;
                bool isGroundHit = hitObjectName.Contains("Ground") || hitObjectName.Contains("Terrain") || hitObjectName.Contains("Floor");

                if (!isGroundHit || !_suppressGroundHitLogs)
                {
                    DebugLog($"Mouse hover hit: {hitObjectName}");
                }

                // Check if we hit a GridPositionMarker
                GridPositionMarker marker = hit.collider.GetComponent<GridPositionMarker>();
                if (marker != null)
                {
                    hoveredGridPos = marker.GridPosition;
                    foundValidPosition = true;
                    DebugLog($"Found grid marker at position {hoveredGridPos}");
                }
                else if (_gridService.WorldToGridPosition(hit.point, out hoveredGridPos))
                {
                    foundValidPosition = true;
                    DebugLog($"Converted hit point to grid position {hoveredGridPos}");
                }
            }

            // Only update path if we have a valid position and it's different from the last one
            if (foundValidPosition && !hoveredGridPos.Equals(_lastHoveredPosition))
            {
                _lastHoveredPosition = hoveredGridPos;

                // Get the unit's current position
                if (_unitService.GetUnitGridPosition(activeUnit, out Vector2Int unitPos))
                {
                    // Only proceed if the positions are different
                    if (!unitPos.Equals(hoveredGridPos))
                    {
                        // Get the movement range tiles
                        var movementRangeTiles = _visualizationService.GetCurrentMovementRange();

                        // Only visualize path if target is in movement range
                        if (movementRangeTiles.Contains(hoveredGridPos))
                        {
                            // Find path using ignoreOccupied = true
                            List<Vector2Int> path = _pathfindingService.FindPath(unitPos, hoveredGridPos, true);

                            if (path != null && path.Count > 1)
                            {
                                // Show the path
                                _visualizationService.VisualizePath(path);
                                return;
                            }
                        }
                    }
                }

                // If we got here, clear the path visualization
                _visualizationService.ClearPathVisualization();
            }
            else if (!foundValidPosition)
            {
                // No valid position, clear path
                _visualizationService.ClearPathVisualization();
                _lastHoveredPosition = new Vector2Int(-1, -1);
            }
        }

        /// <summary>
        /// Log debug messages only if detailed logging is enabled
        /// </summary>
        private void DebugLog(string message)
        {
            if (_showDetailedDebugLogs)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Enable or disable detailed debug logs
        /// </summary>
        public void SetDetailedLogging(bool enable)
        {
            _showDetailedDebugLogs = enable;
        }

        /// <summary>
        /// Enable or disable ground hit logs
        /// </summary>
        public void SetGroundHitLogging(bool enable)
        {
            _suppressGroundHitLogs = !enable;
        }
    }
}