using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid.Extensions
{
    /// <summary>
    /// Interface for a Cover System extension
    /// </summary>
    public interface ICoverSystemExtension : IGridExtension
    {
        /// <summary>
        /// Check if a position provides cover from another position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <param name="fromPosition">The position to check cover from</param>
        /// <returns>Type of cover provided</returns>
        CoverType GetCoverType(Vector2Int position, Vector2Int fromPosition);
        
        /// <summary>
        /// Get all positions that provide cover against a position
        /// </summary>
        /// <param name="fromPosition">The position to check cover from</param>
        /// <returns>Dictionary of positions and cover types</returns>
        Dictionary<Vector2Int, CoverType> GetCoverPositions(Vector2Int fromPosition);
        
        /// <summary>
        /// Set the cover type for a specific tile
        /// </summary>
        /// <param name="position">The position</param>
        /// <param name="coverType">The cover type</param>
        void SetCoverType(Vector2Int position, CoverType coverType);
        
        /// <summary>
        /// Calculate the hit chance modification based on cover
        /// </summary>
        /// <param name="attackerPosition">The attacker's position</param>
        /// <param name="defenderPosition">The defender's position</param>
        /// <returns>Hit chance modifier (0-1)</returns>
        float CalculateHitChanceModifier(Vector2Int attackerPosition, Vector2Int defenderPosition);
        
        /// <summary>
        /// Show cover visualization for a unit
        /// </summary>
        /// <param name="unit">The unit</param>
        void ShowCoverVisualization(Unit unit);
        
        /// <summary>
        /// Clear cover visualization
        /// </summary>
        void ClearCoverVisualization();
    }
    
    /// <summary>
    /// Implementation of a Cover System extension
    /// </summary>
    public class CoverSystemExtension : ICoverSystemExtension
    {
        // The name of the extension
        public string Name => "CoverSystem";
        
        // Reference to the grid service
        private IGridService _gridService;
        
        // Dictionary of manual cover type overrides
        private Dictionary<Vector2Int, CoverType> _coverTypeOverrides = new Dictionary<Vector2Int, CoverType>();
        
        // Currently active cover visualization
        private GameObject _coverVisualizationObject;
        
        // Materials for different cover types
        private Dictionary<CoverType, Material> _coverMaterials = new Dictionary<CoverType, Material>();
        
        // Hit chance modifiers for different cover types
        private Dictionary<CoverType, float> _coverHitModifiers = new Dictionary<CoverType, float>
        {
            { CoverType.None, 1.0f },        // No cover = 100% normal hit chance
            { CoverType.Half, 0.65f },       // Half cover = 65% normal hit chance
            { CoverType.Full, 0.35f },       // Full cover = 35% normal hit chance
            { CoverType.Destructible, 0.5f } // Destructible cover = 50% normal hit chance
        };
        
        /// <summary>
        /// Initialize the extension with the grid service
        /// </summary>
        /// <param name="gridService">The grid service</param>
        public void Initialize(IGridService gridService)
        {
            _gridService = gridService;
            
            // Initialize cover materials
            InitializeCoverMaterials();
            
            Debug.Log("Cover System extension initialized");
        }
        
        /// <summary>
        /// Initialize materials for cover visualization
        /// </summary>
        private void InitializeCoverMaterials()
        {
            // Create materials for different cover types
            _coverMaterials[CoverType.None] = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                color = new Color(1.0f, 0.3f, 0.3f, 0.3f) // Red = Exposed
            };
            
            _coverMaterials[CoverType.Half] = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                color = new Color(1.0f, 1.0f, 0.3f, 0.5f) // Yellow = Half cover
            };
            
            _coverMaterials[CoverType.Full] = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                color = new Color(0.3f, 1.0f, 0.3f, 0.5f) // Green = Full cover
            };
            
            _coverMaterials[CoverType.Destructible] = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                color = new Color(0.3f, 0.7f, 1.0f, 0.5f) // Blue = Destructible cover
            };
            
            // Set up transparency
            foreach (var material in _coverMaterials.Values)
            {
                material.SetFloat("_Surface", 1); // 1 = Transparent
                material.SetFloat("_Blend", 0);  // 0 = SrcAlpha, OneMinusSrcAlpha
                material.SetFloat("_ZWrite", 0); // Don't write to depth buffer
                material.renderQueue = 3000;
            }
        }
        
        /// <summary>
        /// Check if a position provides cover from another position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <param name="fromPosition">The position to check cover from</param>
        /// <returns>Type of cover provided</returns>
        public CoverType GetCoverType(Vector2Int position, Vector2Int fromPosition)
        {
            // Check for manually overridden cover types
            if (_coverTypeOverrides.TryGetValue(position, out CoverType overrideCover))
            {
                return overrideCover;
            }
            
            // Get tile data
            TileData tileData = _gridService.GridData.GetTileData(position);
            if (tileData == null)
            {
                return CoverType.None;
            }
            
            // If the tile has a cover type, return it
            if (tileData.CoverType != CoverType.None)
            {
                return tileData.CoverType;
            }
            
            // Check adjacent tiles for cover
            CoverType bestCover = CoverType.None;
            
            // Get direction from attacker to defender
            Vector2 direction = new Vector2(position.x - fromPosition.x, position.y - fromPosition.y).normalized;
            
            // Check tiles in the likely cover directions
            List<Vector2Int> potentialCoverPositions = GetPotentialCoverPositions(position, direction);
            
            foreach (Vector2Int coverPos in potentialCoverPositions)
            {
                // Skip if outside grid
                if (!_gridService.IsValidPosition(coverPos.x, coverPos.y))
                    continue;
                    
                // Get cover from tile
                TileData coverTile = _gridService.GridData.GetTileData(coverPos);
                
                // Skip walkable tiles (they don't provide cover)
                if (coverTile.IsWalkable)
                    continue;
                    
                // Check if this tile provides cover
                CoverType coverProvided = DetermineCoverFromTile(coverTile, direction);
                
                // Track best cover
                if (coverProvided > bestCover)
                {
                    bestCover = coverProvided;
                    
                    // If we found full cover, we can stop looking
                    if (bestCover == CoverType.Full)
                        break;
                }
            }
            
            return bestCover;
        }
        
        /// <summary>
        /// Get potential cover positions based on direction
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <param name="direction">The direction from attacker to defender</param>
        /// <returns>List of potential cover positions</returns>
        private List<Vector2Int> GetPotentialCoverPositions(Vector2Int position, Vector2 direction)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            
            // Round direction to cardinal or diagonal
            int dx = Mathf.Abs(direction.x) > 0.3f ? (direction.x > 0 ? 1 : -1) : 0;
            int dy = Mathf.Abs(direction.y) > 0.3f ? (direction.y > 0 ? 1 : -1) : 0;
            
            // Get positions adjacent to defender in the cover directions
            if (dx != 0 || dy != 0)
            {
                // Add the tile directly between attacker and defender
                result.Add(new Vector2Int(position.x - dx, position.y - dy));
                
                // Add "flanking" tiles that could provide partial cover
                if (dx != 0 && dy != 0) // Diagonal
                {
                    result.Add(new Vector2Int(position.x - dx, position.y));
                    result.Add(new Vector2Int(position.x, position.y - dy));
                }
                else if (dx != 0) // Horizontal
                {
                    result.Add(new Vector2Int(position.x - dx, position.y + 1));
                    result.Add(new Vector2Int(position.x - dx, position.y - 1));
                }
                else // Vertical
                {
                    result.Add(new Vector2Int(position.x + 1, position.y - dy));
                    result.Add(new Vector2Int(position.x - 1, position.y - dy));
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Determine cover type from a tile
        /// </summary>
        /// <param name="tile">The tile</param>
        /// <param name="direction">The direction from attacker to defender</param>
        /// <returns>Cover type provided</returns>
        private CoverType DetermineCoverFromTile(TileData tile, Vector2 direction)
        {
            // If the tile already has a cover type, use that
            if (tile.CoverType != CoverType.None)
                return tile.CoverType;
                
            // Default cover determination based on terrain type
            switch (tile.TerrainType)
            {
                case TerrainType.Rocks:
                    return CoverType.Full;
                    
                case TerrainType.Road:
                case TerrainType.Sand:
                    return CoverType.Half;
                    
                case TerrainType.Metal:
                    return CoverType.Full;
                    
                default:
                    return CoverType.None;
            }
        }
        
        /// <summary>
        /// Get all positions that provide cover against a position
        /// </summary>
        /// <param name="fromPosition">The position to check cover from</param>
        /// <returns>Dictionary of positions and cover types</returns>
        public Dictionary<Vector2Int, CoverType> GetCoverPositions(Vector2Int fromPosition)
        {
            Dictionary<Vector2Int, CoverType> coverPositions = new Dictionary<Vector2Int, CoverType>();
            
            if (_gridService == null || _gridService.GridData == null)
                return coverPositions;
                
            // Check all grid positions
            for (int x = 0; x < _gridService.GridData.Width; x++)
            {
                for (int z = 0; z < _gridService.GridData.Height; z++)
                {
                    Vector2Int position = new Vector2Int(x, z);
                    
                    // Skip the "from" position
                    if (position == fromPosition)
                        continue;
                        
                    // Get cover type
                    CoverType coverType = GetCoverType(position, fromPosition);
                    
                    // Add to dictionary if there's cover
                    if (coverType != CoverType.None)
                    {
                        coverPositions[position] = coverType;
                    }
                }
            }
            
            return coverPositions;
        }
        
        /// <summary>
        /// Set the cover type for a specific tile
        /// </summary>
        /// <param name="position">The position</param>
        /// <param name="coverType">The cover type</param>
        public void SetCoverType(Vector2Int position, CoverType coverType)
        {
            // Store in overrides dictionary
            _coverTypeOverrides[position] = coverType;
            
            // Also update the tile data if available
            if (_gridService != null && _gridService.GridData != null)
            {
                _gridService.GridData.SetTileCover(position.x, position.y, coverType);
            }
        }
        
        /// <summary>
        /// Calculate the hit chance modification based on cover
        /// </summary>
        /// <param name="attackerPosition">The attacker's position</param>
        /// <param name="defenderPosition">The defender's position</param>
        /// <returns>Hit chance modifier (0-1)</returns>
        public float CalculateHitChanceModifier(Vector2Int attackerPosition, Vector2Int defenderPosition)
        {
            // Get cover type
            CoverType coverType = GetCoverType(defenderPosition, attackerPosition);
            
            // Get hit chance modifier based on cover type
            if (_coverHitModifiers.TryGetValue(coverType, out float modifier))
            {
                return modifier;
            }
            
            // Default to no cover
            return 1.0f;
        }
        
        /// <summary>
        /// Show cover visualization for a unit
        /// </summary>
        /// <param name="unit">The unit</param>
        public void ShowCoverVisualization(Unit unit)
        {
            // Clear any existing visualization
            ClearCoverVisualization();
            
            if (unit == null || _gridService == null)
                return;
                
            // Get unit position
            if (!_gridService.WorldToGridPosition(unit.transform.position, out Vector2Int unitPos))
                return;
                
            // Create visualization object
            _coverVisualizationObject = new GameObject("CoverVisualization");
            
            // Get cover positions
            Dictionary<Vector2Int, CoverType> coverPositions = GetCoverPositions(unitPos);
            
            // Create visualization for each cover position
            foreach (var kvp in coverPositions)
            {
                Vector2Int position = kvp.Key;
                CoverType coverType = kvp.Value;
                
                // Get world position
                Vector3 worldPos = _gridService.GridToWorldPosition(position);
                
                // Create tile visualization
                CreateCoverTileVisualization(worldPos, coverType);
            }
        }
        
        /// <summary>
        /// Create a visualization for a cover tile
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="coverType">Cover type</param>
        private void CreateCoverTileVisualization(Vector3 position, CoverType coverType)
        {
            if (_coverVisualizationObject == null)
                return;
                
            // Create a quad for visualization
            GameObject tileObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tileObj.transform.SetParent(_coverVisualizationObject.transform);
            
            // Position and orient the quad
            tileObj.transform.position = position + Vector3.up * 0.05f; // Slightly above ground
            tileObj.transform.rotation = Quaternion.Euler(90, 0, 0); // Face up
            
            // Scale the quad
            float cellSize = _gridService.GridData.CellSize;
            tileObj.transform.localScale = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1);
            
            // Remove collider
            Object.Destroy(tileObj.GetComponent<Collider>());
            
            // Set material based on cover type
            if (_coverMaterials.TryGetValue(coverType, out Material material))
            {
                tileObj.GetComponent<Renderer>().material = material;
            }
        }
        
        /// <summary>
        /// Clear cover visualization
        /// </summary>
        public void ClearCoverVisualization()
        {
            if (_coverVisualizationObject != null)
            {
                Object.Destroy(_coverVisualizationObject);
                _coverVisualizationObject = null;
            }
        }
    }
}