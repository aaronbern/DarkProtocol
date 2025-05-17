using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Core interface for the grid service that coordinates all grid-related functionality.
    /// Serves as the main entry point for other systems to interact with the grid.
    /// </summary>
    public interface IGridService
    {
        /// <summary>
        /// Get the grid data containing tile information
        /// </summary>
        GridData GridData { get; }
        
        /// <summary>
        /// Initialize the grid system
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Check if a position is valid on the grid
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>True if the position is within grid bounds</returns>
        bool IsValidPosition(int x, int z);
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>World position</returns>
        Vector3 GridToWorldPosition(int x, int z);
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>World position</returns>
        Vector3 GridToWorldPosition(Vector2Int gridPosition);
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <param name="x">Output X grid coordinate</param>
        /// <param name="z">Output Z grid coordinate</param>
        /// <returns>True if conversion was successful</returns>
        bool WorldToGridPosition(Vector3 worldPosition, out int x, out int z);
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <param name="gridPosition">Output grid position</param>
        /// <returns>True if conversion was successful</returns>
        bool WorldToGridPosition(Vector3 worldPosition, out Vector2Int gridPosition);
        
        /// <summary>
        /// Get the terrain type at the specified position
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>Terrain type</returns>
        TerrainType GetTerrainType(int x, int z);
        
        /// <summary>
        /// Set the terrain type at the specified position
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <param name="terrainType">Terrain type to set</param>
        /// <param name="movementCost">Movement cost for this terrain</param>
        void SetTerrainType(int x, int z, TerrainType terrainType, float movementCost = 1f);
        
        /// <summary>
        /// Check if a tile is occupied
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>True if the tile is occupied</returns>
        bool IsTileOccupied(int x, int z);
        
        /// <summary>
        /// Set whether a tile is occupied
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <param name="occupied">Occupied state</param>
        /// <param name="occupant">GameObject occupying the tile (optional)</param>
        void SetTileOccupied(int x, int z, bool occupied, GameObject occupant = null);
    }

    /// <summary>
    /// Interface for pathfinding functionality
    /// </summary>
    public interface IPathfindingService
    {
        /// <summary>
        /// Find a path between two points on the grid
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <param name="ignoreOccupied">Whether to ignore occupied tiles</param>
        /// <returns>List of positions forming the path, or null if no path found</returns>
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, bool ignoreOccupied = false);
        
        /// <summary>
        /// Calculate the movement range for a unit from a starting position
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="movementPoints">Available movement points</param>
        /// <returns>List of positions the unit can reach</returns>
        List<Vector2Int> CalculateMovementRange(Vector2Int start, int movementPoints);
        
        /// <summary>
        /// Check if a path exists between two points
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <returns>True if a path exists</returns>
        bool HasPath(Vector2Int start, Vector2Int end);
        
        /// <summary>
        /// Calculate the movement cost between two positions
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <returns>Movement cost, or float.MaxValue if no path exists</returns>
        float CalculatePathCost(Vector2Int start, Vector2Int end);
    }

    /// <summary>
    /// Interface for unit/object management on the grid
    /// </summary>
    public interface IUnitGridService
    {
        /// <summary>
        /// Register a unit at its current position on the grid
        /// </summary>
        /// <param name="unit">The unit to register</param>
        void RegisterUnitAtPosition(Unit unit);

        /// <summary>
        /// Get the grid position of a unit
        /// </summary>
        /// <param name="unit">The unit</param>
        /// <param name="position">Output grid position</param>
        /// <returns>True if the position was found</returns>
        bool GetUnitGridPosition(Unit unit, out Vector2Int position);

        /// <summary>
        /// Move a unit to a grid position
        /// </summary>
        /// <param name="unit">The unit to move</param>
        /// <param name="targetPos">Target grid position</param>
        /// <returns>True if movement was successful</returns>
        bool MoveUnitToPosition(Unit unit, Vector2Int targetPos);

        /// <summary>
        /// Handle unit selection
        /// </summary>
        /// <param name="unit">The selected unit</param>
        void OnUnitSelected(Unit unit);

        /// <summary>
        /// Get the current movement range for the selected unit
        /// </summary>
        /// <returns>List of positions in the movement range</returns>
        List<Vector2Int> GetCurrentMovementRange();
    }

    /// <summary>
    /// Interface for grid visualization/rendering
    /// </summary>
    public interface IGridVisualizationService
    {
        /// <summary>
        /// Show the movement range for a unit
        /// </summary>
        /// <param name="unit">The unit</param>
        /// <param name="movementPoints">Available movement points</param>
        /// <returns>List of positions in the movement range</returns>
        List<Vector2Int> ShowMovementRange(Unit unit, int movementPoints);

        /// <summary>
        /// Clear the movement range visualization
        /// </summary>
        void ClearMovementRange();

        /// <summary>
        /// Visualize a path between points
        /// </summary>
        /// <param name="path">List of positions forming the path</param>
        void VisualizePath(List<Vector2Int> path);

        /// <summary>
        /// Clear the path visualization
        /// </summary>
        void ClearPathVisualization();

        /// <summary>
        /// Get the current movement range
        /// </summary>
        /// <returns>List of positions in the current movement range</returns>
        List<Vector2Int> GetCurrentMovementRange();
    }

    /// <summary>
    /// Interface for grid input handling
    /// </summary>
    public interface IGridInputService
    {
        /// <summary>
        /// Initialize the input service
        /// </summary>
        void Initialize();

        /// <summary>
        /// Enable grid input handling
        /// </summary>
        void EnableInput();

        /// <summary>
        /// Disable grid input handling
        /// </summary>
        void DisableInput();

        /// <summary>
        /// Process input for grid interaction
        /// </summary>
        void ProcessInput();
        
    }
    
    /// <summary>
    /// Interface for serialization (save/load) of grid data
    /// </summary>
    public interface IGridSerializationService
    {
        /// <summary>
        /// Save grid data to a file
        /// </summary>
        /// <param name="filePath">File path</param>
        void SaveToFile(string filePath);
        
        /// <summary>
        /// Load grid data from a file
        /// </summary>
        /// <param name="filePath">File path</param>
        void LoadFromFile(string filePath);
    }
    
    /// <summary>
    /// Interface for managing extension points like Fog of War, Cover System, etc.
    /// </summary>
    public interface IGridExtensionService
    {
        /// <summary>
        /// Register an extension to the grid system
        /// </summary>
        /// <param name="extension">The extension to register</param>
        void RegisterExtension(IGridExtension extension);
        
        /// <summary>
        /// Unregister an extension from the grid system
        /// </summary>
        /// <param name="extension">The extension to unregister</param>
        void UnregisterExtension(IGridExtension extension);
        
        /// <summary>
        /// Get an extension of a specific type
        /// </summary>
        /// <typeparam name="T">The type of extension</typeparam>
        /// <returns>The extension, or null if not found</returns>
        T GetExtension<T>() where T : class, IGridExtension;
    }
    
    /// <summary>
    /// Base interface for all grid extensions (Fog of War, Cover System, etc.)
    /// </summary>
    public interface IGridExtension
    {
        /// <summary>
        /// Get the name of the extension
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Initialize the extension with the grid service
        /// </summary>
        /// <param name="gridService">The grid service</param>
        void Initialize(IGridService gridService);
    }
}