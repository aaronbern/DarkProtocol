using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Implementation of the grid visualization service
    /// </summary>
    public class GridVisualizationService : IGridVisualizationService
    {
        private readonly IGridService _gridService;
        private readonly IPathfindingService _pathfindingService;
        
        // Reference to the grid overlay system
        private GridOverlaySystem _gridOverlaySystem;
        
        // Current movement range and path
        private List<Vector2Int> _currentMovementRange = new List<Vector2Int>();
        private List<Vector2Int> _currentPath = new List<Vector2Int>();
        
        // Visualization settings
        private Color _movementRangeColor = new Color(0.2f, 0.8f, 0.4f, 0.5f);
        private Color _pathPreviewColor = new Color(1f, 0.8f, 0.2f, 0.7f);
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gridService">The grid service</param>
        /// <param name="pathfindingService">The pathfinding service</param>
        /// <param name="gridOverlaySystem">The grid overlay system</param>
        public GridVisualizationService(
            IGridService gridService, 
            IPathfindingService pathfindingService,
            GridOverlaySystem gridOverlaySystem = null)
        {
            _gridService = gridService;
            _pathfindingService = pathfindingService;
            _gridOverlaySystem = gridOverlaySystem;
            
            // If grid overlay system not provided, try to find it
            if (_gridOverlaySystem == null)
            {
                _gridOverlaySystem = Object.FindFirstObjectByType<GridOverlaySystem>();
                
                if (_gridOverlaySystem == null)
                {
                    Debug.LogWarning("GridOverlaySystem not found! Visualization will not work properly.");
                }
            }
            
            // Configure the overlay system
            SetupGridOverlaySystem();
        }
        
        /// <summary>
        /// Set up the grid overlay system
        /// </summary>
        private void SetupGridOverlaySystem()
        {
            if (_gridOverlaySystem != null)
            {
                _gridOverlaySystem.SetMovementRangeColor(_movementRangeColor);
                _gridOverlaySystem.SetPathPreviewColor(_pathPreviewColor);
            }
        }
        
        /// <summary>
        /// Show the movement range for a unit
        /// </summary>
        /// <param name="unit">The unit</param>
        /// <param name="movementPoints">Available movement points</param>
        /// <returns>List of positions in the movement range</returns>
        public List<Vector2Int> ShowMovementRange(Unit unit, int movementPoints)
        {
            Debug.Log("USING GRID VISUALIZATION SERVICE");

            // Clear any existing range visualization
            ClearMovementRange();
            
            if (_gridService == null || unit == null)
                return new List<Vector2Int>();
                
            // Get unit position
            if (!_gridService.WorldToGridPosition(unit.transform.position, out Vector2Int unitPos))
                return new List<Vector2Int>();
                
            // Calculate movement range
            _currentMovementRange = _pathfindingService.CalculateMovementRange(unitPos, movementPoints);
            
            // Show visualization using grid overlay system
            if (_gridOverlaySystem != null)
            {
                _gridOverlaySystem.ShowMovementRange(_currentMovementRange);
                Debug.Log($"Showing movement range for {unit.UnitName}: {_currentMovementRange.Count} tiles");
            }
            else
            {
                Debug.LogWarning("GridOverlaySystem not found! Cannot show movement range.");
                
                // Try to find or create it
                _gridOverlaySystem = Object.FindFirstObjectByType<GridOverlaySystem>();
                
                // Try again if we got it
                if (_gridOverlaySystem != null)
                {
                    SetupGridOverlaySystem();
                    _gridOverlaySystem.ShowMovementRange(_currentMovementRange);
                }
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
            if (_gridOverlaySystem != null)
            {
                _gridOverlaySystem.ClearMovementRange();
            }
        }
        
        /// <summary>
        /// Visualize a path between points
        /// </summary>
        /// <param name="path">List of positions forming the path</param>
        public void VisualizePath(List<Vector2Int> path)
        {
            // Clear existing path visualization
            ClearPathVisualization();
            
            if (path == null || path.Count < 2)
            {
                Debug.Log("Path is null or too short to visualize");
                return;
            }
            
            // Debug the EXACT path we're trying to visualize
            Debug.Log($"Visualizing path: Start={path[0]}, End={path[path.Count-1]}, via: {string.Join(" → ", path)}");
            
            // Store the current path
            _currentPath = new List<Vector2Int>(path);
            
            // Show visualization using grid overlay system
            if (_gridOverlaySystem != null)
            {
                _gridOverlaySystem.ShowPathPreview(_currentPath);
            }
            else
            {
                // Try to find grid overlay system
                _gridOverlaySystem = Object.FindFirstObjectByType<GridOverlaySystem>();
                
                // Try again if we got it
                if (_gridOverlaySystem != null)
                {
                    SetupGridOverlaySystem();
                    _gridOverlaySystem.ShowPathPreview(_currentPath);
                }
            }
        }
        /// <summary>
        /// Get the current movement range
        /// </summary>
        /// <returns>List of positions in the current movement range</returns>
        public List<Vector2Int> GetCurrentMovementRange()
        {
            if (_gridOverlaySystem != null)
            {
                return _gridOverlaySystem.GetCurrentMovementRange();
            }

            return new List<Vector2Int>();
        }
        /// <summary>
        /// Clear the path visualization
        /// </summary>
        public void ClearPathVisualization()
        {
            _currentPath.Clear();
            
            // Clear visualization using grid overlay system
            if (_gridOverlaySystem != null)
            {
                _gridOverlaySystem.ClearPathPreview();
            }
        }
        
        /// <summary>
        /// Set the color for movement range visualization
        /// </summary>
        /// <param name="color">The color</param>
        public void SetMovementRangeColor(Color color)
        {
            _movementRangeColor = color;
            
            if (_gridOverlaySystem != null)
            {
                _gridOverlaySystem.SetMovementRangeColor(color);
            }
        }
        
        /// <summary>
        /// Set the color for path preview visualization
        /// </summary>
        /// <param name="color">The color</param>
        public void SetPathPreviewColor(Color color)
        {
            _pathPreviewColor = color;
            
            if (_gridOverlaySystem != null)
            {
                _gridOverlaySystem.SetPathPreviewColor(color);
            }
        }
    }
}