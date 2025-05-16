using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Implementation of the pathfinding service
    /// </summary>
    public class PathfindingService : IPathfindingService
    {
        private readonly IGridService _gridService;
        
        /// <summary>
        /// Cache of recently calculated paths for performance
        /// </summary>
        private Dictionary<string, List<Vector2Int>> _pathCache = new Dictionary<string, List<Vector2Int>>();
        
        /// <summary>
        /// Maximum size of the path cache
        /// </summary>
        private const int MaxCacheSize = 100;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gridService">The grid service</param>
        public PathfindingService(IGridService gridService)
        {
            _gridService = gridService;
        }

        /// <summary>
        /// Find a path between two points on the grid
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <param name="ignoreOccupied">Whether to ignore occupied tiles</param>
        /// <returns>List of positions forming the path, or null if no path found</returns>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool ignoreOccupied = false)
        {
            // Check if the grid service is valid
            if (_gridService == null || _gridService.GridData == null)
            {
                Debug.LogError("Cannot find path: Grid service or GridData is null");
                return null;
            }
            
            // Validate positions
            if (!_gridService.IsValidPosition(start.x, start.y) || !_gridService.IsValidPosition(end.x, end.y))
            {
                Debug.LogWarning($"Invalid positions for pathfinding: Start={start}, End={end}");
                return null;
            }
            
            // Check if end is walkable (if not, there's no way to path to it)
            TileData endTile = _gridService.GridData.GetTileData(end);
            if (endTile != null && !endTile.IsWalkable && !ignoreOccupied)
            {
                Debug.LogWarning($"End position is not walkable: {end}");
                return null;
            }
            
            // Special case: If start and end are the same, return just the start
            if (start == end)
            {
                return new List<Vector2Int> { start };
            }
            
            // Check cache for identical path request
            string cacheKey = $"{start.x},{start.y}_{end.x},{end.y}_{ignoreOccupied}";
            if (_pathCache.TryGetValue(cacheKey, out List<Vector2Int> cachedPath))
            {
                return new List<Vector2Int>(cachedPath); // Return copy to prevent modifications
            }
            
            // Create a direct path for testing
            bool isHorizontal = start.y == end.y;
            bool isVertical = start.x == end.x;
            bool isDiagonal = Mathf.Abs(start.x - end.x) == Mathf.Abs(start.y - end.y);
            
            // For direct paths (horizontal, vertical, or perfect diagonal), try a simple path first
            if (isHorizontal || isVertical || isDiagonal)
            {
                List<Vector2Int> directPath = TryDirectPath(start, end, ignoreOccupied);
                if (directPath != null)
                {
                    // Cache the result
                    _pathCache[cacheKey] = new List<Vector2Int>(directPath);
                    return directPath;
                }
            }
            
            // Forward to the grid data's more complex A* algorithm if direct path doesn't work
            List<Vector2Int> path = _gridService.GridData.FindPath(start, end, ignoreOccupied);
            
            // If we still don't have a path, try a more lenient approach
            if (path == null || path.Count <= 1)
            {
                Debug.LogWarning($"Standard A* could not find path from {start} to {end}. Trying alternative pathfinding...");
                path = FindAlternativePath(start, end, ignoreOccupied);
            }
            
            // Cache the result if successful
            if (path != null && path.Count > 1)
            {
                _pathCache[cacheKey] = new List<Vector2Int>(path);
            }
            
            return path;
        }
        
        /// <summary>
        /// Calculate the movement range for a unit from a starting position
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="movementPoints">Available movement points</param>
        /// <returns>List of positions the unit can reach</returns>
        public List<Vector2Int> CalculateMovementRange(Vector2Int start, int movementPoints)
        {
            // Check if the grid service is valid
            if (_gridService == null || _gridService.GridData == null)
            {
                Debug.LogWarning("Cannot calculate movement range: Grid service or GridData is null");
                return new List<Vector2Int>();
            }
            
            // Forward to the grid data
            return _gridService.GridData.CalculateMovementRange(start, movementPoints);
        }
        
        /// <summary>
        /// Check if a path exists between two points
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <returns>True if a path exists</returns>
        public bool HasPath(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = FindPath(start, end);
            return path != null && path.Count > 1;
        }
        
        /// <summary>
        /// Calculate the movement cost between two positions
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <returns>Movement cost, or float.MaxValue if no path exists</returns>
        public float CalculatePathCost(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = FindPath(start, end);
            
            if (path == null || path.Count <= 1)
            {
                return float.MaxValue;
            }
            
            float totalCost = 0;
            
            // Calculate movement cost (sum of tile costs along path)
            for (int i = 1; i < path.Count; i++) // Start from 1 to skip the starting tile
            {
                TileData tileData = _gridService.GridData.GetTileData(path[i]);
                totalCost += tileData.MovementCost;
            }
            
            return totalCost;
        }
        
        /// <summary>
        /// Clear the path cache
        /// </summary>
        public void ClearPathCache()
        {
            _pathCache.Clear();
            Debug.Log("Path cache cleared");
        }
        
        // Try to find a direct path (horizontal, vertical, or diagonal)
        private List<Vector2Int> TryDirectPath(Vector2Int start, Vector2Int end, bool ignoreOccupied)
        {
            List<Vector2Int> path = new List<Vector2Int> { start };
            Vector2Int current = start;
            
            // Check if direct path is possible by testing each tile in between
            while (current != end)
            {
                Vector2Int next = GetNextDirectStep(current, end);
                
                // Check if the next tile is walkable
                if (_gridService.IsValidPosition(next.x, next.y))
                {
                    TileData tile = _gridService.GridData.GetTileData(next);
                    if (tile != null && (tile.IsWalkable || ignoreOccupied || next == end))
                    {
                        path.Add(next);
                        current = next;
                    }
                    else
                    {
                        // Path is blocked
                        return null;
                    }
                }
                else
                {
                    // Invalid position
                    return null;
                }
            }
            
            return path;
        }
        
        // Get the next step in a direct path
        private Vector2Int GetNextDirectStep(Vector2Int current, Vector2Int target)
        {
            Vector2Int step = current;
            
            // Move one step closer in both directions if needed
            if (current.x < target.x) step.x++;
            else if (current.x > target.x) step.x--;
            
            if (current.y < target.y) step.y++;
            else if (current.y > target.y) step.y--;
            
            return step;
        }
        
        // Alternative pathfinding when regular A* fails
        private List<Vector2Int> FindAlternativePath(Vector2Int start, Vector2Int end, bool ignoreOccupied)
        {
            // This is a simplified Manhattan-style path approach
            List<Vector2Int> path = new List<Vector2Int> { start };
            Vector2Int current = start;
            
            // First try moving horizontally as much as possible
            while (current.x != end.x)
            {
                Vector2Int next = current;
                if (current.x < end.x) next.x++;
                else next.x--;
                
                // Check if we can move to this tile
                if (_gridService.IsValidPosition(next.x, next.y))
                {
                    TileData tile = _gridService.GridData.GetTileData(next);
                    if (tile != null && (tile.IsWalkable || ignoreOccupied || (next.x == end.x && next.y == end.y)))
                    {
                        path.Add(next);
                        current = next;
                    }
                    else
                    {
                        // Try to go around vertically first
                        Vector2Int alternate = current;
                        alternate.y += (end.y > current.y) ? 1 : -1;
                        
                        if (_gridService.IsValidPosition(alternate.x, alternate.y))
                        {
                            TileData altTile = _gridService.GridData.GetTileData(alternate);
                            if (altTile != null && (altTile.IsWalkable || ignoreOccupied))
                            {
                                path.Add(alternate);
                                current = alternate;
                                continue;
                            }
                        }
                        
                        // If we can't find any path, return null
                        Debug.LogWarning($"Alternative pathfinding failed at position {current}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"Alternative pathfinding reached invalid position {next}");
                    return null;
                }
            }
            
            // Then move vertically to reach the target
            while (current.y != end.y)
            {
                Vector2Int next = current;
                if (current.y < end.y) next.y++;
                else next.y--;
                
                // Check if we can move to this tile
                if (_gridService.IsValidPosition(next.x, next.y))
                {
                    TileData tile = _gridService.GridData.GetTileData(next);
                    if (tile != null && (tile.IsWalkable || ignoreOccupied || (next.x == end.x && next.y == end.y)))
                    {
                        path.Add(next);
                        current = next;
                    }
                    else
                    {
                        Debug.LogWarning($"Alternative pathfinding blocked at position {next}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"Alternative pathfinding reached invalid position {next}");
                    return null;
                }
            }
            
            return path;
        }
        
        public void DebugPathfindingIssue(Vector2Int start, Vector2Int end)
        {
            Debug.Log($"Debugging pathfinding from {start} to {end}");
            
            // Check if positions are valid
            bool startValid = _gridService.IsValidPosition(start.x, start.y);
            bool endValid = _gridService.IsValidPosition(end.x, end.y);
            Debug.Log($"Start position valid: {startValid}, End position valid: {endValid}");
            
            // Check walkability
            bool startWalkable = false;
            bool endWalkable = false;
            bool startOccupied = false;
            bool endOccupied = false;
            
            if (startValid)
            {
                TileData startTile = _gridService.GridData.GetTileData(start);
                startWalkable = startTile?.IsWalkable ?? false;
                startOccupied = startTile?.IsOccupied ?? false;
            }
            
            if (endValid)
            {
                TileData endTile = _gridService.GridData.GetTileData(end);
                endWalkable = endTile?.IsWalkable ?? false;
                endOccupied = endTile?.IsOccupied ?? false;
            }
            
            Debug.Log($"Start tile: Walkable={startWalkable}, Occupied={startOccupied}");
            Debug.Log($"End tile: Walkable={endWalkable}, Occupied={endOccupied}");
            
            // If end is occupied, that could be the issue
            if (endOccupied)
            {
                Debug.LogWarning("End position is occupied! This is likely why no path can be found.");
                
                // Check who is occupying it
                var occupant = _gridService.GridData.GetOccupant(end.x, end.y);
                if (occupant != null)
                {
                    Debug.LogWarning($"Tile is occupied by: {occupant.name}");
                }
            }
            
            // Try getting neighbors to see if they're walkable
            Debug.Log("Checking neighbors of start position:");
            CheckNeighbors(start);
            
            Debug.Log("Checking manhattan path from start to end:");
            // Check the direct line of tiles between start and end
            Vector2Int current = start;
            while (current != end)
            {
                // Move horizontally
                if (current.x < end.x) current.x++;
                else if (current.x > end.x) current.x--;
                
                // Check this position
                if (_gridService.IsValidPosition(current.x, current.y))
                {
                    TileData tile = _gridService.GridData.GetTileData(current);
                    Debug.Log($"Position {current}: Walkable={tile?.IsWalkable ?? false}, Occupied={tile?.IsOccupied ?? false}");
                    
                    if (tile != null && tile.IsOccupied)
                    {
                        var occupant = _gridService.GridData.GetOccupant(current.x, current.y);
                        Debug.Log($"Tile is occupied by: {occupant?.name ?? "Unknown"}");
                    }
                }
                else
                {
                    Debug.Log($"Position {current}: Invalid position");
                }
                
                // Break if we've reached the same X position
                if (current.x == end.x) break;
            }
            
            // Then move vertically
            while (current != end)
            {
                if (current.y < end.y) current.y++;
                else if (current.y > end.y) current.y--;
                
                // Check this position
                if (_gridService.IsValidPosition(current.x, current.y))
                {
                    TileData tile = _gridService.GridData.GetTileData(current);
                    Debug.Log($"Position {current}: Walkable={tile?.IsWalkable ?? false}, Occupied={tile?.IsOccupied ?? false}");
                    
                    if (tile != null && tile.IsOccupied)
                    {
                        var occupant = _gridService.GridData.GetOccupant(current.x, current.y);
                        Debug.Log($"Tile is occupied by: {occupant?.name ?? "Unknown"}");
                    }
                }
                else
                {
                    Debug.Log($"Position {current}: Invalid position");
                }
            }
            
            // Suggest potential solutions
            Debug.Log("Potential solutions:");
            Debug.Log("1. Check if source and destination tiles are properly marked as walkable");
            Debug.Log("2. Make sure occupied tiles are correctly marked as occupied");
            Debug.Log("3. Verify that the grid data is properly initialized");
            Debug.Log("4. Check if tiles are marked as unwalkable when they should be walkable");
        }

        private void CheckNeighbors(Vector2Int position)
        {
            // Check all neighbors
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(0, 1),   // Up
                new Vector2Int(0, -1),  // Down
            };
            
            foreach (var dir in directions)
            {
                Vector2Int neighbor = position + dir;
                if (_gridService.IsValidPosition(neighbor.x, neighbor.y))
                {
                    TileData tile = _gridService.GridData.GetTileData(neighbor);
                    Debug.Log($"Neighbor {neighbor} ({GetDirectionName(dir)}): Walkable={tile?.IsWalkable ?? false}, Occupied={tile?.IsOccupied ?? false}");
                    
                    if (tile != null && tile.IsOccupied)
                    {
                        var occupant = _gridService.GridData.GetOccupant(neighbor.x, neighbor.y);
                        Debug.Log($"Tile is occupied by: {occupant?.name ?? "Unknown"}");
                    }
                }
                else
                {
                    Debug.Log($"Neighbor {neighbor} ({GetDirectionName(dir)}): Invalid position");
                }
            }
        }

        private string GetDirectionName(Vector2Int direction)
        {
            if (direction.x > 0) return "Right";
            if (direction.x < 0) return "Left";
            if (direction.y > 0) return "Up";
            if (direction.y < 0) return "Down";
            return "Unknown";
        }
    }
}