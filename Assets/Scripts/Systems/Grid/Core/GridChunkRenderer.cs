using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Handles efficient rendering of a chunk of the grid using a single mesh
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridChunkRenderer : MonoBehaviour
    {
        #region Private Fields
        private GridData _gridData;
        private RectInt _chunkBounds;
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private bool _isDirty = true;
        
        // Cached mesh data
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector2> _uvs = new List<Vector2>();
        private List<Color> _colors = new List<Color>();
        
        // Visualization settings (could be moved to a SO for configuration)
        private Dictionary<TerrainType, Color> _terrainColors = new Dictionary<TerrainType, Color>
        {
            { TerrainType.Ground, new Color(0.5f, 0.5f, 0.5f) },
            { TerrainType.Water, new Color(0.2f, 0.4f, 0.8f) },
            { TerrainType.Mud, new Color(0.4f, 0.3f, 0.2f) },
            { TerrainType.Sand, new Color(0.9f, 0.8f, 0.6f) },
            { TerrainType.Road, new Color(0.3f, 0.3f, 0.3f) },
            { TerrainType.Rocks, new Color(0.5f, 0.5f, 0.5f) },
            { TerrainType.Metal, new Color(0.7f, 0.7f, 0.7f) },
            { TerrainType.Grass, new Color(0.3f, 0.7f, 0.3f) },
            { TerrainType.Snow, new Color(0.9f, 0.9f, 0.9f) },
            { TerrainType.Ice, new Color(0.8f, 0.9f, 1.0f) },
            { TerrainType.Lava, new Color(0.9f, 0.3f, 0.1f) }
        };
        
        // Highlight settings
        private Color _occupiedColor = new Color(1f, 0.5f, 0.5f, 0.8f);
        private Color _unwalkableColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        private Color _highlightColor = new Color(1f, 1f, 0.5f, 0.8f);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get components
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            
            // Create mesh
            _mesh = new Mesh();
            _mesh.name = $"GridChunk_{gameObject.name}";
            _meshFilter.sharedMesh = _mesh;
        }
        
        private void OnEnable()
        {
            // Mark dirty to rebuild on enable
            _isDirty = true;
        }
        
        private void Update()
        {
            // Rebuild mesh if dirty
            if (_isDirty)
            {
                RebuildMesh();
                _isDirty = false;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up mesh
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the chunk renderer
        /// </summary>
        /// <param name="gridData">The grid data</param>
        /// <param name="chunkBounds">The bounds of this chunk</param>
        public void Initialize(GridData gridData, RectInt chunkBounds)
        {
            _gridData = gridData;
            _chunkBounds = chunkBounds;
            
            // Set material 
            // This should be assigned from a factory or settings object in a real implementation
            _meshRenderer.material = Resources.Load<Material>("Materials/GridTile");
            
            // First-time build
            RebuildMesh();
        }
        
        /// <summary>
        /// Check if this chunk contains a grid position
        /// </summary>
        public bool ContainsPosition(int x, int z)
        {
            return x >= _chunkBounds.x && x < _chunkBounds.x + _chunkBounds.width &&
                   z >= _chunkBounds.y && z < _chunkBounds.y + _chunkBounds.height;
        }
        
        /// <summary>
        /// Mark the mesh as dirty to trigger a rebuild
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }
        #endregion

        #region Mesh Building
        /// <summary>
        /// Rebuild the entire mesh
        /// </summary>
        private void RebuildMesh()
        {
            if (_gridData == null)
                return;
                
            // Clear lists
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _colors.Clear();
            
            // Get cell size
            float cellSize = _gridData.CellSize;
            
            // Iterate over each tile in the chunk
            for (int x = _chunkBounds.x; x < _chunkBounds.x + _chunkBounds.width; x++)
            {
                for (int z = _chunkBounds.y; z < _chunkBounds.y + _chunkBounds.height; z++)
                {
                    if (!_gridData.IsValidPosition(x, z))
                        continue;
                        
                    // Get tile data
                    TileData tileData = _gridData.GetTileData(x, z);
                    
                    // Calculate local position (relative to chunk)
                    float localX = (x - _chunkBounds.x) * cellSize;
                    float localZ = (z - _chunkBounds.y) * cellSize;
                    
                    // Add a quad for this tile
                    AddTileQuad(localX, localZ, cellSize, tileData);
                }
            }
            
            // Update the mesh
            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            
            // Recalculate normals and bounds
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
        
        /// <summary>
        /// Add a quad for a single tile
        /// </summary>
        private void AddTileQuad(float x, float z, float size, TileData tileData)
        {
            // Get base vertex index
            int vertIndex = _vertices.Count;
            
            // Calculate y position based on elevation
            float y = tileData.Elevation + 0.01f; // Small offset to avoid z-fighting
            
            // Add vertices (clockwise from bottom-left)
            _vertices.Add(new Vector3(x, y, z));                     // Bottom-left
            _vertices.Add(new Vector3(x + size, y, z));              // Bottom-right
            _vertices.Add(new Vector3(x + size, y, z + size));       // Top-right
            _vertices.Add(new Vector3(x, y, z + size));              // Top-left
            
            // Add triangles (two triangles per quad)
            _triangles.Add(vertIndex);
            _triangles.Add(vertIndex + 1);
            _triangles.Add(vertIndex + 2);
            
            _triangles.Add(vertIndex);
            _triangles.Add(vertIndex + 2);
            _triangles.Add(vertIndex + 3);
            
            // Add UVs
            float uvScale = 1.0f;
            _uvs.Add(new Vector2(0, 0) * uvScale);
            _uvs.Add(new Vector2(1, 0) * uvScale);
            _uvs.Add(new Vector2(1, 1) * uvScale);
            _uvs.Add(new Vector2(0, 1) * uvScale);
            
            // Determine color based on tile state
            Color tileColor = GetTileColor(tileData);
            
            // Add colors (same color for all vertices of the quad)
            for (int i = 0; i < 4; i++)
            {
                _colors.Add(tileColor);
            }
        }
        
        /// <summary>
        /// Get the appropriate color for a tile based on its state
        /// </summary>
        private Color GetTileColor(TileData tileData)
        {
            // Priority order for visualization
            if (!tileData.IsWalkable)
                return _unwalkableColor;
                
            if (tileData.IsOccupied)
                return _occupiedColor;
                
            // Get base color from terrain type
            if (_terrainColors.TryGetValue(tileData.TerrainType, out Color terrainColor))
                return terrainColor;
                
            // Default color
            return Color.gray;
        }
        #endregion
    }
}