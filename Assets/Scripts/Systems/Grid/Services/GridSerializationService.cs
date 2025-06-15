using System;
using System.IO;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Implementation of the grid serialization service
    /// </summary>
    public class GridSerializationService : IGridSerializationService
    {
        private readonly IGridService _gridService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gridService">The grid service</param>
        public GridSerializationService(IGridService gridService)
        {
            _gridService = gridService;
        }

        /// <summary>
        /// Save grid data to a file
        /// </summary>
        /// <param name="filePath">File path</param>
        public void SaveToFile(string filePath)
        {
            if (_gridService == null || _gridService.GridData == null)
            {
                Debug.LogWarning("Cannot save grid data: Grid service or GridData is null");
                return;
            }

            try
            {
                // Forward to the grid data
                _gridService.GridData.SaveToFile(filePath);
                Debug.Log($"Grid data saved to {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save grid data: {e.Message}");
            }
        }

        /// <summary>
        /// Load grid data from a file
        /// </summary>
        /// <param name="filePath">File path</param>
        public void LoadFromFile(string filePath)
        {
            if (_gridService == null || _gridService.GridData == null)
            {
                Debug.LogWarning("Cannot load grid data: Grid service or GridData is null");
                return;
            }

            try
            {
                // Forward to the grid data
                _gridService.GridData.LoadFromFile(filePath);
                Debug.Log($"Grid data loaded from {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load grid data: {e.Message}");
            }
        }

        /// <summary>
        /// Export grid data to a different format
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="format">Export format</param>
        public void ExportGridData(string filePath, GridExportFormat format)
        {
            if (_gridService == null || _gridService.GridData == null)
            {
                Debug.LogWarning("Cannot export grid data: Grid service or GridData is null");
                return;
            }

            try
            {
                switch (format)
                {
                    case GridExportFormat.JSON:
                        ExportToJson(filePath);
                        break;
                    case GridExportFormat.CSV:
                        ExportToCsv(filePath);
                        break;
                    default:
                        Debug.LogWarning($"Unsupported export format: {format}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export grid data: {e.Message}");
            }
        }

        /// <summary>
        /// Export grid data to JSON format
        /// </summary>
        /// <param name="filePath">File path</param>
        private void ExportToJson(string filePath)
        {
            // Create a serializable representation of the grid
            var gridData = _gridService.GridData;
            var exportData = new GridExportData
            {
                Width = gridData.Width,
                Height = gridData.Height,
                CellSize = gridData.CellSize,
                MapOrigin = new Vector3Serializable(gridData.MapOrigin),
                Tiles = new TileExportData[gridData.Width * gridData.Height]
            };

            // Export tile data
            for (int x = 0; x < gridData.Width; x++)
            {
                for (int z = 0; z < gridData.Height; z++)
                {
                    int index = z * gridData.Width + x;
                    TileData tile = gridData.GetTileData(x, z);

                    exportData.Tiles[index] = new TileExportData
                    {
                        X = x,
                        Z = z,
                        TerrainType = (int)tile.TerrainType,
                        MovementCost = tile.MovementCost,
                        IsWalkable = tile.IsWalkable,
                        IsOccupied = tile.IsOccupied,
                        Elevation = tile.Elevation,
                        CoverType = (int)tile.CoverType
                    };
                }
            }

            // Serialize to JSON
            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(filePath, json);

            Debug.Log($"Grid data exported to JSON: {filePath}");
        }

        /// <summary>
        /// Export grid data to CSV format
        /// </summary>
        /// <param name="filePath">File path</param>
        private void ExportToCsv(string filePath)
        {
            var gridData = _gridService.GridData;

            // Create CSV header
            string header = "X,Z,TerrainType,MovementCost,IsWalkable,IsOccupied,Elevation,CoverType";

            // Create CSV rows
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(header);

            for (int x = 0; x < gridData.Width; x++)
            {
                for (int z = 0; z < gridData.Height; z++)
                {
                    TileData tile = gridData.GetTileData(x, z);

                    sb.AppendLine($"{x},{z},{(int)tile.TerrainType},{tile.MovementCost},{tile.IsWalkable},{tile.IsOccupied},{tile.Elevation},{(int)tile.CoverType}");
                }
            }

            // Write to file
            File.WriteAllText(filePath, sb.ToString());

            Debug.Log($"Grid data exported to CSV: {filePath}");
        }
    }

    /// <summary>
    /// Export format for grid data
    /// </summary>
    public enum GridExportFormat
    {
        JSON,
        CSV
    }

    /// <summary>
    /// Serializable data for grid export
    /// </summary>
    [Serializable]
    public class GridExportData
    {
        public int Width;
        public int Height;
        public float CellSize;
        public Vector3Serializable MapOrigin;
        public TileExportData[] Tiles;
    }

    /// <summary>
    /// Serializable data for tile export
    /// </summary>
    [Serializable]
    public class TileExportData
    {
        public int X;
        public int Z;
        public int TerrainType;
        public float MovementCost;
        public bool IsWalkable;
        public bool IsOccupied;
        public float Elevation;
        public int CoverType;
    }

    /// <summary>
    /// Serializable Vector3 for JSON serialization
    /// </summary>
    [Serializable]
    public class Vector3Serializable
    {
        public float X, Y, Z;

        public Vector3Serializable(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }
}