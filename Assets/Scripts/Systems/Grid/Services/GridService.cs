using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Implementation of the grid service that coordinates all grid-related functionality.
    /// </summary>
    public class GridService : IGridService
    {
        #region Properties
        
        /// <summary>
        /// The grid data containing tile information
        /// </summary>
        public GridData GridData { get; set; }
        
        /// <summary>
        /// Default grid width when creating a new grid
        /// </summary>
        public int DefaultWidth { get; set; } = 20;
        
        /// <summary>
        /// Default grid height when creating a new grid
        /// </summary>
        public int DefaultHeight { get; set; } = 20;
        
        /// <summary>
        /// Default cell size when creating a new grid
        /// </summary>
        public float DefaultCellSize { get; set; } = 1f;
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gridData">The grid data to use, or null to create a new one</param>
        public GridService(GridData gridData = null)
        {
            GridData = gridData;
        }
        
        /// <summary>
        /// Initialize the grid system
        /// </summary>
        public void Initialize()
        {
            if (GridData == null)
            {
                CreateNewGrid();
            }
            else
            {
                GridData.Initialize();
            }
            
            Debug.Log($"Grid Service initialized with grid: {GridData.Width}x{GridData.Height}");
        }
        
        /// <summary>
        /// Create a new grid with default settings
        /// </summary>
        public void CreateNewGrid()
        {
            // Create a new grid data asset
            GridData = ScriptableObject.CreateInstance<GridData>();
            
            // Set the properties explicitly using reflection (since they're serialized fields)
            var widthField = GridData.GetType().GetField("width", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var heightField = GridData.GetType().GetField("height", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var cellSizeField = GridData.GetType().GetField("cellSize", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
            if (widthField != null) widthField.SetValue(GridData, DefaultWidth);
            if (heightField != null) heightField.SetValue(GridData, DefaultHeight);
            if (cellSizeField != null) cellSizeField.SetValue(GridData, DefaultCellSize);
            
            // Center the grid on the origin
            CenterGridOnOrigin();
            
            // Initialize the grid
            GridData.Initialize();
            
            Debug.Log($"Created new grid: {GridData.Width}x{GridData.Height}");
        }
        
        /// <summary>
        /// Centers the grid on the world origin
        /// </summary>
        private void CenterGridOnOrigin()
        {
            if (GridData == null)
            {
                Debug.LogWarning("Cannot center grid: GridData is null");
                return;
            }
            
            // Calculate what the offset should be to center the grid
            float width = GridData.Width * GridData.CellSize;
            float height = GridData.Height * GridData.CellSize;
            
            // Center the grid by setting the origin to negative half of dimensions
            Vector3 centeredOrigin = new Vector3(-width/2f, 0, -height/2f);
            
            // Update the origin in the grid data
            var originField = GridData.GetType().GetField("mapOrigin", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                
            if (originField != null)
            {
                originField.SetValue(GridData, centeredOrigin);
                Debug.Log($"Centered grid origin at {centeredOrigin}");
            }
            else
            {
                Debug.LogWarning("Could not find mapOrigin field in GridData");
            }
        }
        
        #endregion

        #region Grid Position Methods
        
        /// <summary>
        /// Check if a position is valid on the grid
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>True if the position is within grid bounds</returns>
        public bool IsValidPosition(int x, int z)
        {
            return GridData != null && GridData.IsValidPosition(x, z);
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>World position</returns>
        public Vector3 GridToWorldPosition(int x, int z)
        {
            return GridData?.GridToWorldPosition(x, z) ?? Vector3.zero;
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>World position</returns>
        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return GridData?.GridToWorldPosition(gridPosition) ?? Vector3.zero;
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <param name="x">Output X grid coordinate</param>
        /// <param name="z">Output Z grid coordinate</param>
        /// <returns>True if conversion was successful</returns>
        public bool WorldToGridPosition(Vector3 worldPosition, out int x, out int z)
        {
            if (GridData != null)
            {
                return GridData.WorldToGridPosition(worldPosition, out x, out z);
            }
            
            x = 0;
            z = 0;
            return false;
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <param name="gridPosition">Output grid position</param>
        /// <returns>True if conversion was successful</returns>
        public bool WorldToGridPosition(Vector3 worldPosition, out Vector2Int gridPosition)
        {
            if (GridData != null)
            {
                return GridData.WorldToGridPosition(worldPosition, out gridPosition);
            }
            
            gridPosition = Vector2Int.zero;
            return false;
        }
        
        #endregion

        #region Tile Methods
        
        /// <summary>
        /// Get the terrain type at the specified position
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>Terrain type</returns>
        public TerrainType GetTerrainType(int x, int z)
        {
            return GridData?.GetTileData(x, z)?.TerrainType ?? TerrainType.Ground;
        }
        
        /// <summary>
        /// Set the terrain type at the specified position
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <param name="terrainType">Terrain type to set</param>
        /// <param name="movementCost">Movement cost for this terrain</param>
        public void SetTerrainType(int x, int z, TerrainType terrainType, float movementCost = 1f)
        {
            GridData?.SetTileTerrain(x, z, terrainType, movementCost);
        }
        
        /// <summary>
        /// Check if a tile is occupied
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>True if the tile is occupied</returns>
        public bool IsTileOccupied(int x, int z)
        {
            return GridData?.GetTileData(x, z)?.IsOccupied ?? false;
        }
        
        /// <summary>
        /// Set whether a tile is occupied
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <param name="occupied">Occupied state</param>
        /// <param name="occupant">GameObject occupying the tile (optional)</param>
        public void SetTileOccupied(int x, int z, bool occupied, GameObject occupant = null)
        {
            GridData?.SetTileOccupancy(x, z, occupied, occupant);
        }
        
        #endregion
    }
}