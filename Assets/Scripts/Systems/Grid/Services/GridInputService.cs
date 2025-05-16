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
        
        // Debug setting
        private bool _showDetailedDebugLogs = false;
        
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
            Debug.Log("Grid input enabled");
        }
        
        /// <summary>
        /// Disable grid input handling
        /// </summary>
        public void DisableInput()
        {
            _inputEnabled = false;
            Debug.Log("Grid input disabled");
        }
        
        /// <summary>
        /// Process input for grid interaction
        /// Call this from Update() in a MonoBehaviour
        /// </summary>
        public void ProcessInput()
        {
            if (!_inputEnabled || _mainCamera == null)
                return;
                
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
                // Reset hover cache after a click to force recalculation
                _lastHoveredGridPos = new Vector2Int(-1, -1);
            }
            else if (!leftMousePressed && !Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed)
            {
                // Implement frame skipping for hover processing
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
                if (_showDetailedDebugLogs)
                    Debug.Log($"Right click detected - hit: {hit.collider.gameObject.name}");
                
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
                            if (_showDetailedDebugLogs)
                                Debug.Log($"Moving unit to grid position: {gridPos}");
                                
                            // Move the unit
                            _unitService.MoveUnitToPosition(selectedUnit, gridPos);
                        }
                        else
                        {
                            if (_showDetailedDebugLogs)
                                Debug.Log($"Position {gridPos} is not in movement range");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Handle mouse hover for path visualization
        /// </summary>
        /// <param name="ray">Ray from mouse position</param>
        private void HandleMouseHover(Ray ray)
        {
            // Get the currently selected unit
            Unit selectedUnit = Unit.SelectedUnit;
            
            // CRITICAL FIX: Only process hovering if there's a selected unit
            if (selectedUnit == null)
            {
                // No unit selected, make sure we clear any visualizations
                _visualizationService.ClearPathVisualization();
                return;
            }
            
            bool foundValidPosition = false;
            Vector2Int hoveredGridPos = Vector2Int.zero;
            
            // Cast ray
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Check if we hit a Unit - we don't want to calculate paths when hovering over units
                Unit hitUnit = hit.collider.GetComponent<Unit>();
                if (hitUnit != null)
                {
                    // We're hovering over a unit, clear path visualization and skip further processing
                    _visualizationService.ClearPathVisualization();
                    return;
                }
                
                // Check if we hit a GridPositionMarker
                GridPositionMarker marker = hit.collider.GetComponent<GridPositionMarker>();
                if (marker != null)
                {
                    hoveredGridPos = marker.GridPosition;
                    foundValidPosition = true;
                    
                    if (_showDetailedDebugLogs)
                        Debug.Log($"Found grid marker at position {hoveredGridPos}");
                }
                else if (_gridService.WorldToGridPosition(hit.point, out hoveredGridPos))
                {
                    foundValidPosition = true;
                    
                    if (_showDetailedDebugLogs)
                        Debug.Log($"Converted hit point to grid position {hoveredGridPos}");
                }
            }
            
            // If nothing changed, don't recalculate
            if (foundValidPosition == _lastHoverWasValid && hoveredGridPos == _lastHoveredGridPos)
            {
                return; // Skip recalculation - nothing changed
            }
            
            // Save current state for next frame comparison
            _lastHoverWasValid = foundValidPosition;
            _lastHoveredGridPos = hoveredGridPos;
            
            if (foundValidPosition)
            {
                // Check for occupancy here
                if (_gridService.IsTileOccupied(hoveredGridPos.x, hoveredGridPos.y))
                {
                    // If hovering over an occupied tile, clear path
                    _visualizationService.ClearPathVisualization();
                    return;
                }
                
                // Get the unit's current position
                if (_unitService.GetUnitGridPosition(selectedUnit, out Vector2Int unitPos))
                {
                    // Only try to path if the positions are different
                    if (unitPos != hoveredGridPos)
                    {
                        // Important: Try with ignoreOccupied = true to get a path
                        List<Vector2Int> path = _pathfindingService.FindPath(unitPos, hoveredGridPos, true);
                        
                        if (path != null && path.Count > 1)
                        {
                            // Show the path
                            _visualizationService.VisualizePath(path);
                            
                            if (_showDetailedDebugLogs)
                                Debug.Log($"Found path from {unitPos} to {hoveredGridPos} with {path.Count} points");
                        }
                        else
                        {
                            // Clear any existing path
                            _visualizationService.ClearPathVisualization();
                            
                            if (_showDetailedDebugLogs)
                                Debug.Log($"Could not find path from {unitPos} to {hoveredGridPos}");
                        }
                    }
                    else
                    {
                        // Clear path if hovering over current position
                        _visualizationService.ClearPathVisualization();
                    }
                }
            }
            else
            {
                // No hit, clear path
                _visualizationService.ClearPathVisualization();
            }
        }
    }
}