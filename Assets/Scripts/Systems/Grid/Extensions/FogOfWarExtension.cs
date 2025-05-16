using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid.Extensions
{
    /// <summary>
    /// Interface for a Fog of War system as a grid extension
    /// </summary>
    public interface IFogOfWarExtension : IGridExtension
    {
        /// <summary>
        /// Check if a tile is visible to the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>True if the tile is visible</returns>
        bool IsTileVisible(int x, int z);
        
        /// <summary>
        /// Check if a tile has been explored by the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>True if the tile has been explored</returns>
        bool IsTileExplored(int x, int z);
        
        /// <summary>
        /// Update visibility based on current unit positions
        /// </summary>
        void UpdateVisibility();
        
        /// <summary>
        /// Reveal a tile for the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        void RevealTile(int x, int z);
        
        /// <summary>
        /// Hide a tile from the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        void HideTile(int x, int z);
        
        /// <summary>
        /// Add a vision source at the specified position
        /// </summary>
        /// <param name="position">Grid position</param>
        /// <param name="visionRange">Vision range</param>
        void AddVisionSource(Vector2Int position, int visionRange);
        
        /// <summary>
        /// Remove a vision source from the specified position
        /// </summary>
        /// <param name="position">Grid position</param>
        void RemoveVisionSource(Vector2Int position);
        
        /// <summary>
        /// Toggle fog of war visualization
        /// </summary>
        /// <param name="enabled">Enabled state</param>
        void ToggleFogOfWar(bool enabled);
    }
    
    /// <summary>
    /// Implementation of a Fog of War system as a grid extension
    /// </summary>
    public class FogOfWarExtension : IFogOfWarExtension
    {
        // The name of the extension
        public string Name => "FogOfWar";
        
        // Reference to the grid service
        private IGridService _gridService;
        
        // Dictionaries to track visibility and exploration state
        private Dictionary<Vector2Int, bool> _visibilityMap = new Dictionary<Vector2Int, bool>();
        private Dictionary<Vector2Int, bool> _explorationMap = new Dictionary<Vector2Int, bool>();
        
        // List of vision sources
        private Dictionary<Vector2Int, int> _visionSources = new Dictionary<Vector2Int, int>();
        
        // Whether fog of war is enabled
        private bool _fogOfWarEnabled = true;
        
        // Material for fog of war visualization
        private Material _fogOfWarMaterial;
        
        // GameObject for fog of war visualization
        private GameObject _fogOfWarObject;
        
        /// <summary>
        /// Initialize the extension with the grid service
        /// </summary>
        /// <param name="gridService">The grid service</param>
        public void Initialize(IGridService gridService)
        {
            _gridService = gridService;
            
            // Initialize visibility and exploration maps
            if (_gridService.GridData != null)
            {
                for (int x = 0; x < _gridService.GridData.Width; x++)
                {
                    for (int z = 0; z < _gridService.GridData.Height; z++)
                    {
                        Vector2Int pos = new Vector2Int(x, z);
                        _visibilityMap[pos] = false;
                        _explorationMap[pos] = false;
                    }
                }
            }
            
            // Create fog of war visualization
            CreateFogOfWarVisualization();
            
            Debug.Log("Fog of War extension initialized");
        }
        
        /// <summary>
        /// Create fog of war visualization
        /// </summary>
        private void CreateFogOfWarVisualization()
        {
            // Create material for fog of war
            _fogOfWarMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _fogOfWarMaterial.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Create game object for fog of war
            _fogOfWarObject = new GameObject("FogOfWar");
            _fogOfWarObject.transform.position = Vector3.zero;
            
            // Add mesh renderer and filter
            MeshRenderer meshRenderer = _fogOfWarObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = _fogOfWarObject.AddComponent<MeshFilter>();
            
            // Set material
            meshRenderer.material = _fogOfWarMaterial;
            
            // Create mesh
            UpdateFogOfWarMesh();
        }
        
        /// <summary>
        /// Update the fog of war mesh based on visibility
        /// </summary>
        private void UpdateFogOfWarMesh()
        {
            if (_gridService.GridData == null || _fogOfWarObject == null)
                return;
                
            // Create the mesh
            Mesh mesh = new Mesh();
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Color> colors = new List<Color>();
            
            float cellSize = _gridService.GridData.CellSize;
            float fogHeight = 0.1f; // Slightly above ground
            
            // Create quads for unexplored or non-visible tiles
            for (int x = 0; x < _gridService.GridData.Width; x++)
            {
                for (int z = 0; z < _gridService.GridData.Height; z++)
                {
                    Vector2Int pos = new Vector2Int(x, z);
                    
                    // Skip if tile is visible
                    if (IsTileVisible(x, z))
                        continue;
                        
                    // Get world position
                    Vector3 worldPos = _gridService.GridToWorldPosition(pos);
                    
                    // Add vertices for a quad
                    int vertexIndex = vertices.Count;
                    
                    // Add vertices (clockwise from bottom-left)
                    vertices.Add(new Vector3(worldPos.x - cellSize/2, worldPos.y + fogHeight, worldPos.z - cellSize/2));
                    vertices.Add(new Vector3(worldPos.x + cellSize/2, worldPos.y + fogHeight, worldPos.z - cellSize/2));
                    vertices.Add(new Vector3(worldPos.x + cellSize/2, worldPos.y + fogHeight, worldPos.z + cellSize/2));
                    vertices.Add(new Vector3(worldPos.x - cellSize/2, worldPos.y + fogHeight, worldPos.z + cellSize/2));
                    
                    // Add triangles (two triangles per quad)
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 3);
                    
                    // Add UVs
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(0, 1));
                    
                    // Add colors (different for explored but not visible, and unexplored)
                    Color fogColor = IsTileExplored(x, z) 
                        ? new Color(0.2f, 0.2f, 0.2f, 0.5f) // Semi-transparent for explored
                        : new Color(0.1f, 0.1f, 0.1f, 0.9f); // Opaque for unexplored
                        
                    for (int i = 0; i < 4; i++)
                    {
                        colors.Add(fogColor);
                    }
                }
            }
            
            // Set mesh data
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.colors = colors.ToArray();
            
            // Recalculate normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            // Assign to mesh filter
            _fogOfWarObject.GetComponent<MeshFilter>().mesh = mesh;
        }
        
        /// <summary>
        /// Check if a tile is visible to the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>True if the tile is visible</returns>
        public bool IsTileVisible(int x, int z)
        {
            if (!_fogOfWarEnabled)
                return true;
                
            Vector2Int pos = new Vector2Int(x, z);
            return _visibilityMap.TryGetValue(pos, out bool visible) && visible;
        }
        
        /// <summary>
        /// Check if a tile has been explored by the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>True if the tile has been explored</returns>
        public bool IsTileExplored(int x, int z)
        {
            if (!_fogOfWarEnabled)
                return true;
                
            Vector2Int pos = new Vector2Int(x, z);
            return _explorationMap.TryGetValue(pos, out bool explored) && explored;
        }
        
        /// <summary>
        /// Update visibility based on current vision sources
        /// </summary>
        public void UpdateVisibility()
        {
            if (_gridService.GridData == null)
                return;
                
            // Reset visibility
            foreach (Vector2Int pos in _visibilityMap.Keys)
            {
                _visibilityMap[pos] = false;
            }
            
            // Update visibility from each vision source
            foreach (var kvp in _visionSources)
            {
                Vector2Int sourcePos = kvp.Key;
                int visionRange = kvp.Value;
                
                // Calculate visible area
                for (int x = sourcePos.x - visionRange; x <= sourcePos.x + visionRange; x++)
                {
                    for (int z = sourcePos.y - visionRange; z <= sourcePos.y + visionRange; z++)
                    {
                        // Skip if outside grid
                        if (!_gridService.IsValidPosition(x, z))
                            continue;
                            
                        // Check if within range
                        int distanceSquared = (x - sourcePos.x) * (x - sourcePos.x) + (z - sourcePos.y) * (z - sourcePos.y);
                        if (distanceSquared <= visionRange * visionRange)
                        {
                            // Check line of sight
                            if (HasLineOfSight(sourcePos, new Vector2Int(x, z)))
                            {
                                // Tile is visible
                                Vector2Int pos = new Vector2Int(x, z);
                                _visibilityMap[pos] = true;
                                _explorationMap[pos] = true;
                            }
                        }
                    }
                }
            }
            
            // Update visualization
            UpdateFogOfWarMesh();
        }
        
        /// <summary>
        /// Check if there is line of sight between two positions
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <returns>True if there is line of sight</returns>
        private bool HasLineOfSight(Vector2Int start, Vector2Int end)
        {
            // Simple Bresenham's line algorithm for line of sight
            int x = start.x;
            int y = start.y;
            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.y - start.y);
            int sx = start.x < end.x ? 1 : -1;
            int sy = start.y < end.y ? 1 : -1;
            int err = dx - dy;
            
            while (x != end.x || y != end.y)
            {
                // Skip the starting position
                if (x != start.x || y != start.y)
                {
                    // Check if this position blocks line of sight
                    if (_gridService.IsValidPosition(x, y))
                    {
                        TileData tile = _gridService.GridData.GetTileData(x, y);
                        
                        // Check if tile blocks vision (e.g., walls)
                        if (tile != null && !tile.IsWalkable)
                        {
                            return false;
                        }
                    }
                }
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Reveal a tile for the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        public void RevealTile(int x, int z)
        {
            Vector2Int pos = new Vector2Int(x, z);
            _visibilityMap[pos] = true;
            _explorationMap[pos] = true;
            
            // Update visualization
            UpdateFogOfWarMesh();
        }
        
        /// <summary>
        /// Hide a tile from the player
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        public void HideTile(int x, int z)
        {
            Vector2Int pos = new Vector2Int(x, z);
            _visibilityMap[pos] = false;
            
            // Update visualization
            UpdateFogOfWarMesh();
        }
        
        /// <summary>
        /// Add a vision source at the specified position
        /// </summary>
        /// <param name="position">Grid position</param>
        /// <param name="visionRange">Vision range</param>
        public void AddVisionSource(Vector2Int position, int visionRange)
        {
            _visionSources[position] = visionRange;
            
            // Update visibility
            UpdateVisibility();
        }
        
        /// <summary>
        /// Remove a vision source from the specified position
        /// </summary>
        /// <param name="position">Grid position</param>
        public void RemoveVisionSource(Vector2Int position)
        {
            if (_visionSources.ContainsKey(position))
            {
                _visionSources.Remove(position);
                
                // Update visibility
                UpdateVisibility();
            }
        }
        
        /// <summary>
        /// Toggle fog of war visualization
        /// </summary>
        /// <param name="enabled">Enabled state</param>
        public void ToggleFogOfWar(bool enabled)
        {
            _fogOfWarEnabled = enabled;
            
            // Show/hide fog of war object
            if (_fogOfWarObject != null)
            {
                _fogOfWarObject.SetActive(enabled);
            }
            
            // If enabling, update visibility
            if (enabled)
            {
                UpdateVisibility();
            }
        }
    }
}