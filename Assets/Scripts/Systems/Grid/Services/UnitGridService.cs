using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Implementation of the unit grid service
    /// </summary>
    public class UnitGridService : IUnitGridService
    {
        private readonly IGridService _gridService;
        private readonly IPathfindingService _pathfindingService;
        private readonly IGridVisualizationService _visualizationService;
        
        // Currently selected unit
        private Unit _selectedUnit;
        
        // Current movement range
        private List<Vector2Int> _currentMovementRange = new List<Vector2Int>();

        // Debug flag
        private bool _showDetailedDebugLogs = false;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gridService">The grid service</param>
        /// <param name="pathfindingService">The pathfinding service</param>
        /// <param name="visualizationService">The visualization service</param>
        public UnitGridService(
            IGridService gridService, 
            IPathfindingService pathfindingService,
            IGridVisualizationService visualizationService)
        {
            _gridService = gridService;
            _pathfindingService = pathfindingService;
            _visualizationService = visualizationService;
        }
        
        /// <summary>
        /// Register a unit at its current position on the grid
        /// </summary>
        /// <param name="unit">The unit to register</param>
        public void RegisterUnitAtPosition(Unit unit)
        {
            if (unit == null || _gridService == null)
                return;
                
            // Get unit grid position
            if (GetUnitGridPosition(unit, out Vector2Int pos))
            {
                // Set tile as occupied
                _gridService.SetTileOccupied(pos.x, pos.y, true, unit.gameObject);
                
                if (_showDetailedDebugLogs)
                    Debug.Log($"Unit {GetUnitDisplayName(unit)} registered at grid position ({pos.x}, {pos.y})");
            }
        }
        
        /// <summary>
        /// Get the grid position of a unit
        /// </summary>
        /// <param name="unit">The unit</param>
        /// <param name="position">Output grid position</param>
        /// <returns>True if the position was found</returns>
        public bool GetUnitGridPosition(Unit unit, out Vector2Int position)
        {
            position = Vector2Int.zero;
            
            if (unit == null || _gridService == null)
                return false;
                
            // Get unit world position
            Vector3 worldPos = unit.transform.position;
            
            // Convert to grid position
            if (_gridService.WorldToGridPosition(worldPos, out position))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Move a unit to a grid position
        /// </summary>
        /// <param name="unit">The unit to move</param>
        /// <param name="targetPos">Target grid position</param>
        /// <returns>True if movement was successful</returns>
        public bool MoveUnitToPosition(Unit unit, Vector2Int targetPos)
        {
            if (_showDetailedDebugLogs)
                Debug.Log($"Attempting to move {unit.name} to position {targetPos}");
            
            if (_gridService == null || unit == null)
            {
                Debug.LogError("Cannot move unit: Grid service or unit is null");
                return false;
            }
                
            // Get current position
            if (!GetUnitGridPosition(unit, out Vector2Int currentPos))
            {
                Debug.LogError($"Could not get current grid position for {unit.name}");
                return false;
            }
            
            if (_showDetailedDebugLogs)
                Debug.Log($"Current position: {currentPos}, Target position: {targetPos}, Movement range count: {_currentMovementRange.Count}");
            
            // Check if this is a valid movement
            bool isValid = _currentMovementRange.Contains(targetPos);
            
            if (!isValid)
            {
                Debug.LogWarning($"Cannot move unit to position {targetPos} - not in movement range");
                
                if (_showDetailedDebugLogs)
                    Debug.Log($"Available positions in range: {string.Join(", ", _currentMovementRange)}");
                    
                return false;
            }
            
            // Find path to the target
            List<Vector2Int> path = _pathfindingService.FindPath(currentPos, targetPos);
            if (path == null || path.Count <= 1)
            {
                Debug.LogWarning($"Cannot find path to position {targetPos}");
                
                // Debug the pathfinding issue
                (_pathfindingService as PathfindingService)?.DebugPathfindingIssue(currentPos, targetPos);
                
                return false;
            }
            
            if (_showDetailedDebugLogs)
                Debug.Log($"Path found with {path.Count} tiles: {string.Join(" -> ", path)}");
            
            // Calculate movement cost
            float totalCost = _pathfindingService.CalculatePathCost(currentPos, targetPos);
            Debug.Log($"Movement cost: {totalCost}, Available points: {unit.CurrentMovementPoints}");
            
            // Check if unit has enough movement points
            if (unit.CurrentMovementPoints < totalCost)
            {
                Debug.LogWarning($"Unit doesn't have enough movement points. Needs {totalCost}, has {unit.CurrentMovementPoints}");
                return false;
            }
            
            // *** CRITICAL FIX: Update tile occupancy BEFORE moving ***
            // Clear the start position occupancy
            _gridService.SetTileOccupied(currentPos.x, currentPos.y, false);
            
            // Move the unit visually
            Vector3 targetWorldPos = _gridService.GridToWorldPosition(targetPos);
            Debug.Log($"Moving unit to world position: {targetWorldPos}, Cost: {Mathf.RoundToInt(totalCost)}");
            
            // Try to move the unit
            bool moveResult = unit.Move(targetWorldPos, Mathf.RoundToInt(totalCost));
            Debug.Log($"Unit.Move result: {moveResult}");
            
            // Update occupancy based on movement result
            if (moveResult)
            {
                // Move was successful, mark destination as occupied
                _gridService.SetTileOccupied(targetPos.x, targetPos.y, true, unit.gameObject);
            }
            else
            {
                // Move failed, restore original position occupancy
                _gridService.SetTileOccupied(currentPos.x, currentPos.y, true, unit.gameObject);
            }
            
            // Clear ranges and paths after movement
            if (_visualizationService != null)
            {
                _visualizationService.ClearMovementRange();
                _visualizationService.ClearPathVisualization();
            }
            
            _currentMovementRange.Clear();
            
            return moveResult;
        }
        
        /// <summary>
        /// Handle unit selection
        /// </summary>
        /// <param name="unit">The selected unit</param>
        public void OnUnitSelected(Unit unit)
        {
            Debug.Log($"Unit selected: {(unit != null ? GetUnitDisplayName(unit) : "None")}");
            
            // Clear any existing movement range
            if (_visualizationService != null)
            {
                _visualizationService.ClearMovementRange();
                _visualizationService.ClearPathVisualization();
            }
            
            _currentMovementRange.Clear();
            _selectedUnit = unit;
            
            // Show movement range for the selected unit
            if (unit != null && _visualizationService != null)
            {
                Debug.Log($"Selected unit {GetUnitDisplayName(unit)} has {unit.CurrentMovementPoints} movement points");
                _currentMovementRange = _visualizationService.ShowMovementRange(unit, unit.CurrentMovementPoints);
                Debug.Log($"Movement range contains {_currentMovementRange.Count} tiles");
            }
        }
        
        /// <summary>
        /// Get a display name for the unit, safely handling if UnitName doesn't exist
        /// </summary>
        private string GetUnitDisplayName(Unit unit)
        {
            if (unit == null)
                return "Unknown Unit";
                
            // Try to get UnitName property
            var nameProperty = unit.GetType().GetProperty("UnitName");
            if (nameProperty != null)
            {
                return nameProperty.GetValue(unit, null)?.ToString() ?? "Unnamed Unit";
            }
            
            // Fallback to GameObject name
            return unit.gameObject.name;
        }
    }
}