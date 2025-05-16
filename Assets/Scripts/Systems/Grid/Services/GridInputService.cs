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
        
        // Last known input position for interaction
        private Vector3 _lastMousePosition;
        
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
            }
            else if (!leftMousePressed && !Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed)
            {
                HandleMouseHover(ray);
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
                Debug.Log($"Right click detected - hit: {hit.collider.gameObject.name}");
                
                // Check if we hit a movement range collider
                GridPositionMarker marker = hit.collider.GetComponent<GridPositionMarker>();
                if (marker != null)
                {
                    Vector2Int gridPos = marker.GridPosition;
                    Debug.Log($"Clicked on movement tile at grid position: {gridPos}");
                    
                    // Move the unit
                    _unitService.MoveUnitToPosition(selectedUnit, gridPos);
                }
                else
                {
                    // Try to convert hit point to grid position as fallback
                    if (_gridService.WorldToGridPosition(hit.point, out Vector2Int gridPos))
                    {
                        // Get the unit's movement range from the visualization service
                        var unitGridPos = Vector2Int.zero;
                        _unitService.GetUnitGridPosition(selectedUnit, out unitGridPos);
                        
                        // Calculate movement range
                        List<Vector2Int> movementRange = _pathfindingService.CalculateMovementRange(unitGridPos, selectedUnit.CurrentMovementPoints);
                        
                        // Check if this is a valid movement tile
                        if (movementRange.Contains(gridPos))
                        {
                            Debug.Log($"Moving unit to grid position: {gridPos}");
                            // Move the unit
                            _unitService.MoveUnitToPosition(selectedUnit, gridPos);
                        }
                        else
                        {
                            Debug.Log($"Position {gridPos} is not in movement range");
                        }
                    }
                    else
                    {
                        Debug.Log("Could not convert hit point to valid grid position");
                    }
                }
            }
            else
            {
                Debug.Log("Right-click raycast did not hit anything");
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
            
            if (selectedUnit == null)
                return;
            
            bool foundValidPosition = false;
            Vector2Int hoveredGridPos = Vector2Int.zero;
            
            // Cast ray
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Debug.Log($"Mouse hover hit: {hit.collider.gameObject.name} at distance {hit.distance}");
                
                // Check if we hit a GridPositionMarker
                GridPositionMarker marker = hit.collider.GetComponent<GridPositionMarker>();
                if (marker != null)
                {
                    hoveredGridPos = marker.GridPosition;
                    foundValidPosition = true;
                    Debug.Log($"Found grid marker at position {hoveredGridPos}");
                }
                else if (_gridService.WorldToGridPosition(hit.point, out hoveredGridPos))
                {
                    foundValidPosition = true;
                    Debug.Log($"Converted hit point to grid position {hoveredGridPos}");
                }
            }
            
            if (foundValidPosition)
            {
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
                            // CRITICAL: Log to verify we're getting the expected path
                            Debug.Log($"Found path from {unitPos} to {hoveredGridPos} with {path.Count} points");
                            
                            // Show the path
                            _visualizationService.VisualizePath(path);
                        }
                        else
                        {
                            // Log the failure reason
                            Debug.LogWarning($"Could not find path from {unitPos} to {hoveredGridPos}");
                            
                            // Clear any existing path
                            _visualizationService.ClearPathVisualization();
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