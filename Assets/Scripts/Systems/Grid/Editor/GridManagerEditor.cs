#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Custom editor for the GridManager to provide level design tools
    /// </summary>
    [CustomEditor(typeof(GridManager))]
    public class GridManagerEditor : UnityEditor.Editor // Fix: Use fully qualified name UnityEditor.Editor
    {
        // Serialized properties
        private SerializedProperty gridDataProp;
        private SerializedProperty createNewGridOnStartProp;
        private SerializedProperty defaultWidthProp;
        private SerializedProperty defaultHeightProp;
        private SerializedProperty defaultCellSizeProp;
        private SerializedProperty showGridInEditorProp;
        private SerializedProperty showGridInGameProp;
        private SerializedProperty gridLineColorProp;
        private SerializedProperty gridLineWidthProp;
        private SerializedProperty movementRangeMaterialProp;
        private SerializedProperty movementRangeColorProp;
        private SerializedProperty movementRangeHeightProp;
        private SerializedProperty gridParentProp;
        private SerializedProperty mainCameraProp;
        private SerializedProperty enableDebugViewProp;
        private SerializedProperty showPathfindingResultsProp;

        // Editor state
        private bool showEditingTools = true;
        private bool showRuntimeSettings = true;
        private bool showTerrainPainting = false;
        private bool showObstaclePlacement = false;
        private bool showTileCover = false;
        private bool showTerrainDetails = false;

        // Terrain painting tool
        private TerrainType selectedTerrainType = TerrainType.Ground;
        private float terrainMovementCost = 1.0f;
        private bool continuousPainting = false;
        private int brushSize = 1;
        private bool showBrushPreview = true;

        // Obstacle placement tool
        private bool isPlacingObstacle = false;
        private bool isRemovingObstacles = false;

        // Cover placement tool
        private CoverType selectedCoverType = CoverType.Half;

        // Grid visualization
        private Color[] terrainColors = new Color[]
        {
            new Color(0.5f, 0.5f, 0.5f), // Ground
            new Color(0.2f, 0.4f, 0.8f), // Water
            new Color(0.4f, 0.3f, 0.2f), // Mud
            new Color(0.9f, 0.8f, 0.6f), // Sand
            new Color(0.3f, 0.3f, 0.3f), // Road
            new Color(0.5f, 0.5f, 0.5f), // Rocks
            new Color(0.7f, 0.7f, 0.7f), // Metal
            new Color(0.3f, 0.7f, 0.3f), // Grass
            new Color(0.9f, 0.9f, 0.9f), // Snow
            new Color(0.8f, 0.9f, 1.0f), // Ice
            new Color(0.9f, 0.3f, 0.1f)  // Lava
        };

        // Scene view tools
        private bool isEditingInSceneView = false;
        private Vector3 lastMousePosition;
        private bool isDragging = false;

        private void OnEnable()
        {
            // Get serialized properties safely
            try
            {
                gridDataProp = serializedObject.FindProperty("gridData");
                createNewGridOnStartProp = serializedObject.FindProperty("createNewGridOnStart");
                defaultWidthProp = serializedObject.FindProperty("defaultWidth");
                defaultHeightProp = serializedObject.FindProperty("defaultHeight");
                defaultCellSizeProp = serializedObject.FindProperty("defaultCellSize");
                showGridInEditorProp = serializedObject.FindProperty("showGridInEditor");
                showGridInGameProp = serializedObject.FindProperty("showGridInGame");
                gridLineColorProp = serializedObject.FindProperty("gridLineColor");
                gridLineWidthProp = serializedObject.FindProperty("gridLineWidth");
                movementRangeMaterialProp = serializedObject.FindProperty("movementRangeMaterial");
                movementRangeColorProp = serializedObject.FindProperty("movementRangeColor");
                movementRangeHeightProp = serializedObject.FindProperty("movementRangeHeight");
                gridParentProp = serializedObject.FindProperty("gridParent");
                mainCameraProp = serializedObject.FindProperty("mainCamera");
                enableDebugViewProp = serializedObject.FindProperty("enableDebugView");
                showPathfindingResultsProp = serializedObject.FindProperty("showPathfindingResults");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to find some serialized properties: {e.Message}");
            }

            // Register for scene view events
            SceneView.duringSceneGui += OnSceneViewGUI;
        }

        private void OnDisable()
        {
            // Unregister from scene view events
            SceneView.duringSceneGui -= OnSceneViewGUI;
        }

        // Helper method to safely display a property field
        private void SafePropertyField(SerializedProperty property)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GridManager gridManager = (GridManager)target;

            // General grid settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Data", EditorStyles.boldLabel);
            SafePropertyField(gridDataProp);

            // New grid settings
            SafePropertyField(createNewGridOnStartProp);
            if (createNewGridOnStartProp != null && createNewGridOnStartProp.boolValue)
            {
                EditorGUI.indentLevel++;
                SafePropertyField(defaultWidthProp);
                SafePropertyField(defaultHeightProp);
                SafePropertyField(defaultCellSizeProp);
                EditorGUI.indentLevel--;
            }

            // Create new grid button
            EditorGUILayout.Space();
            if (GUILayout.Button("Create New Grid"))
            {
                if (EditorUtility.DisplayDialog("Create New Grid",
                    "Are you sure you want to create a new grid? This will replace the current grid data.",
                    "Create", "Cancel"))
                {
                    // Create new grid
                    Undo.RecordObject(gridManager, "Create New Grid");
                    gridManager.CreateNewGrid();
                }
            }

            // Visualization settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visualization Settings", EditorStyles.boldLabel);
            SafePropertyField(showGridInEditorProp);
            SafePropertyField(showGridInGameProp);
            SafePropertyField(gridLineColorProp);
            SafePropertyField(gridLineWidthProp);

            // Movement range visualization
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement Range Visualization", EditorStyles.boldLabel);
            SafePropertyField(movementRangeMaterialProp);
            SafePropertyField(movementRangeColorProp);
            SafePropertyField(movementRangeHeightProp);

            // Object references
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object References", EditorStyles.boldLabel);
            SafePropertyField(gridParentProp);
            SafePropertyField(mainCameraProp);

            // Debug settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            SafePropertyField(enableDebugViewProp);
            SafePropertyField(showPathfindingResultsProp);

            // Grid editing tools
            EditorGUILayout.Space();
            showEditingTools = EditorGUILayout.Foldout(showEditingTools, "Grid Editing Tools", true, EditorStyles.foldoutHeader);
            if (showEditingTools && gridManager.gridData != null)
            {
                EditorGUILayout.Space();

                // Terrain painting
                showTerrainPainting = EditorGUILayout.Foldout(showTerrainPainting, "Terrain Painting", true);
                if (showTerrainPainting)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;

                    // Terrain type selection
                    selectedTerrainType = (TerrainType)EditorGUILayout.EnumPopup("Terrain Type", selectedTerrainType);

                    // Display color preview
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Terrain Color");
                    Rect colorRect = GUILayoutUtility.GetRect(64, 16);
                    EditorGUI.DrawRect(colorRect, terrainColors[(int)selectedTerrainType]);
                    EditorGUILayout.EndHorizontal();

                    // Movement cost
                    terrainMovementCost = EditorGUILayout.FloatField("Movement Cost", terrainMovementCost);

                    // Brush settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
                    brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 5);
                    continuousPainting = EditorGUILayout.Toggle("Continuous Painting", continuousPainting);
                    showBrushPreview = EditorGUILayout.Toggle("Show Brush Preview", showBrushPreview);

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Start Terrain Painting"))
                    {
                        isEditingInSceneView = true;
                        isPlacingObstacle = false;
                        isRemovingObstacles = false;

                        // Focus the scene view
                        SceneView view = SceneView.lastActiveSceneView;
                        if (view != null)
                        {
                            view.Focus();
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                // Obstacle placement
                EditorGUILayout.Space();
                showObstaclePlacement = EditorGUILayout.Foldout(showObstaclePlacement, "Obstacle Placement", true);
                if (showObstaclePlacement)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;

                    EditorGUILayout.LabelField("Place unwalkable obstacles on the grid", EditorStyles.wordWrappedLabel);

                    // Brush settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
                    brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 5);
                    continuousPainting = EditorGUILayout.Toggle("Continuous Painting", continuousPainting);

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Place Obstacles"))
                    {
                        isEditingInSceneView = true;
                        isPlacingObstacle = true;
                        isRemovingObstacles = false;

                        // Focus the scene view
                        SceneView view = SceneView.lastActiveSceneView;
                        if (view != null)
                        {
                            view.Focus();
                        }
                    }

                    if (GUILayout.Button("Remove Obstacles"))
                    {
                        isEditingInSceneView = true;
                        isPlacingObstacle = false;
                        isRemovingObstacles = true;

                        // Focus the scene view
                        SceneView view = SceneView.lastActiveSceneView;
                        if (view != null)
                        {
                            view.Focus();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }

                // Cover placement
                EditorGUILayout.Space();
                showTileCover = EditorGUILayout.Foldout(showTileCover, "Cover Placement", true);
                if (showTileCover)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;

                    // Cover type selection
                    selectedCoverType = (CoverType)EditorGUILayout.EnumPopup("Cover Type", selectedCoverType);

                    // Brush settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
                    brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 3);
                    continuousPainting = EditorGUILayout.Toggle("Continuous Painting", continuousPainting);

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Place Cover"))
                    {
                        // TODO: Implement cover placement
                    }

                    EditorGUI.indentLevel--;
                }

                // Terrain details
                EditorGUILayout.Space();
                showTerrainDetails = EditorGUILayout.Foldout(showTerrainDetails, "Terrain Details", true);
                if (showTerrainDetails)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;

                    // TODO: Implement terrain details editor
                    EditorGUILayout.HelpBox("Terrain details editor coming soon!", MessageType.Info);

                    EditorGUI.indentLevel--;
                }

                // Stop editing button
                EditorGUILayout.Space();
                if (isEditingInSceneView)
                {
                    if (GUILayout.Button("Stop Editing"))
                    {
                        isEditingInSceneView = false;
                        isPlacingObstacle = false;
                        isRemovingObstacles = false;
                    }
                }
            }

            // Runtime settings
            EditorGUILayout.Space();
            showRuntimeSettings = EditorGUILayout.Foldout(showRuntimeSettings, "Runtime Settings", true, EditorStyles.foldoutHeader);
            if (showRuntimeSettings)
            {
                EditorGUILayout.HelpBox("These settings affect the grid system during gameplay.", MessageType.Info);

                // TODO: Add runtime settings
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Fixed: Changed method signature to match Unity's expected callback
        private void OnSceneViewGUI(SceneView sceneView)
        {
            GridManager gridManager = (GridManager)target;

            if (!isEditingInSceneView || gridManager == null || gridManager.gridData == null)
                return;

            // Handle mouse input
            Event e = Event.current;

            // Get mouse position in world space
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool validHit = false;
            Vector3 hitPoint = Vector3.zero;

            // Try to hit the plane at y=0
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                hitPoint = ray.GetPoint(distance);
                validHit = true;
            }

            // If we have a valid hit, use it
            if (validHit)
            {
                // Convert to grid position
                int gridX, gridZ;
                bool validGridPos = gridManager.WorldToGridPosition(hitPoint, out gridX, out gridZ);

                if (validGridPos)
                {
                    // Draw brush preview
                    if (showBrushPreview && brushSize > 0)
                    {
                        // Cache handles color
                        Color originalColor = Handles.color;

                        // Set color based on tool
                        if (isPlacingObstacle)
                            Handles.color = new Color(0.8f, 0.2f, 0.2f, 0.5f);
                        else if (isRemovingObstacles)
                            Handles.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
                        else
                            Handles.color = terrainColors[(int)selectedTerrainType];

                        // Draw brush preview for each affected tile
                        for (int x = gridX - brushSize + 1; x < gridX + brushSize; x++)
                        {
                            for (int z = gridZ - brushSize + 1; z < gridZ + brushSize; z++)
                            {
                                // Skip if outside brush radius
                                if (brushSize > 1 && Vector2.Distance(new Vector2(gridX, gridZ), new Vector2(x, z)) >= brushSize)
                                    continue;

                                // Skip if outside grid
                                if (!gridManager.IsValidPosition(x, z))
                                    continue;

                                // Get world position of this tile
                                Vector3 tileCenter = gridManager.GridToWorldPosition(x, z);

                                // Draw square
                                Vector3 size = Vector3.one * gridManager.gridData.CellSize * 0.9f;
                                size.y = 0.01f; // Thin volume
                                Handles.DrawWireCube(tileCenter, size);
                            }
                        }

                        // Restore original color
                        Handles.color = originalColor;
                    }

                    // Handle editing
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        // Start dragging
                        isDragging = true;
                        lastMousePosition = hitPoint;

                        // Process the edit
                        ProcessGridEdit(gridManager, gridX, gridZ);

                        // Use the event
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
                    {
                        // Only process if the mouse has moved to a new tile
                        Vector3 currentPos = hitPoint;
                        if (Vector3.Distance(lastMousePosition, currentPos) > gridManager.gridData.CellSize * 0.5f || continuousPainting)
                        {
                            lastMousePosition = currentPos;

                            // Process the edit
                            ProcessGridEdit(gridManager, gridX, gridZ);
                        }

                        // Use the event
                        e.Use();
                    }
                    else if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        // Stop dragging
                        isDragging = false;

                        // Use the event
                        e.Use();
                    }
                }
            }

            // Force repaint of the scene view to show brush preview
            if (showBrushPreview && isEditingInSceneView)
            {
                sceneView.Repaint();
            }
        }

        private void ProcessGridEdit(GridManager gridManager, int gridX, int gridZ)
        {
            // Apply the edit based on the current tool
            if (isPlacingObstacle)
            {
                // Record undo
                Undo.RecordObject(gridManager, "Place Obstacles");

                // Apply to each affected tile
                for (int x = gridX - brushSize + 1; x < gridX + brushSize; x++)
                {
                    for (int z = gridZ - brushSize + 1; z < gridZ + brushSize; z++)
                    {
                        // Skip if outside brush radius
                        if (brushSize > 1 && Vector2.Distance(new Vector2(gridX, gridZ), new Vector2(x, z)) >= brushSize)
                            continue;

                        // Skip if outside grid
                        if (!gridManager.IsValidPosition(x, z))
                            continue;

                        // Set as unwalkable
                        gridManager.gridData.SetTileWalkable(x, z, false);
                    }
                }
            }
            else if (isRemovingObstacles)
            {
                // Record undo
                Undo.RecordObject(gridManager, "Remove Obstacles");

                // Apply to each affected tile
                for (int x = gridX - brushSize + 1; x < gridX + brushSize; x++)
                {
                    for (int z = gridZ - brushSize + 1; z < gridZ + brushSize; z++)
                    {
                        // Skip if outside brush radius
                        if (brushSize > 1 && Vector2.Distance(new Vector2(gridX, gridZ), new Vector2(x, z)) >= brushSize)
                            continue;

                        // Skip if outside grid
                        if (!gridManager.IsValidPosition(x, z))
                            continue;

                        // Set as walkable
                        gridManager.gridData.SetTileWalkable(x, z, true);
                    }
                }
            }
            else // Terrain painting
            {
                // Record undo
                Undo.RecordObject(gridManager, "Paint Terrain");

                // Apply to each affected tile
                for (int x = gridX - brushSize + 1; x < gridX + brushSize; x++)
                {
                    for (int z = gridZ - brushSize + 1; z < gridZ + brushSize; z++)
                    {
                        // Skip if outside brush radius
                        if (brushSize > 1 && Vector2.Distance(new Vector2(gridX, gridZ), new Vector2(x, z)) >= brushSize)
                            continue;

                        // Skip if outside grid
                        if (!gridManager.IsValidPosition(x, z))
                            continue;

                        // Set terrain type
                        gridManager.gridData.SetTileTerrain(x, z, selectedTerrainType, terrainMovementCost);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Custom editor for the GridData ScriptableObject
    /// </summary>
    [CustomEditor(typeof(GridData))]
    public class GridDataEditor : UnityEditor.Editor // Fix: Use fully qualified name UnityEditor.Editor
    {
        // SerializedProperties
        private SerializedProperty widthProp;
        private SerializedProperty heightProp;
        private SerializedProperty cellSizeProp;
        private SerializedProperty mapOriginProp;
        private SerializedProperty chunkSizeProp;
        private SerializedProperty maxVisibleDistanceProp;
        private SerializedProperty updateBudgetPerFrameProp;

        // Foldouts
        private bool showImportExport = true;
        private bool showGridSettings = true;
        private bool showOptimizationSettings = true;

        private void OnEnable()
        {
            // Find properties
            try
            {
                widthProp = serializedObject.FindProperty("width");
                heightProp = serializedObject.FindProperty("height");
                cellSizeProp = serializedObject.FindProperty("cellSize");
                mapOriginProp = serializedObject.FindProperty("mapOrigin");
                chunkSizeProp = serializedObject.FindProperty("chunkSize");
                maxVisibleDistanceProp = serializedObject.FindProperty("maxVisibleDistance");
                updateBudgetPerFrameProp = serializedObject.FindProperty("updateBudgetPerFrame");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to find some serialized properties: {e.Message}");
            }
        }

        private void SafePropertyField(SerializedProperty property)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GridData gridData = (GridData)target;

            // Title
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Data Asset", EditorStyles.boldLabel);

            // Grid settings
            EditorGUILayout.Space();
            showGridSettings = EditorGUILayout.Foldout(showGridSettings, "Grid Settings", true, EditorStyles.foldoutHeader);
            if (showGridSettings)
            {
                SafePropertyField(widthProp);
                SafePropertyField(heightProp);
                SafePropertyField(cellSizeProp);
                SafePropertyField(mapOriginProp);

                EditorGUILayout.Space();
                if (widthProp != null && heightProp != null)
                {
                    EditorGUILayout.LabelField($"Total Tiles: {widthProp.intValue * heightProp.intValue}");
                }
            }

            // Optimization settings
            EditorGUILayout.Space();
            showOptimizationSettings = EditorGUILayout.Foldout(showOptimizationSettings, "Optimization Settings", true, EditorStyles.foldoutHeader);
            if (showOptimizationSettings)
            {
                SafePropertyField(chunkSizeProp);
                SafePropertyField(maxVisibleDistanceProp);
                SafePropertyField(updateBudgetPerFrameProp);

                EditorGUILayout.Space();
                if (widthProp != null && heightProp != null && chunkSizeProp != null)
                {
                    int chunksX = Mathf.CeilToInt((float)widthProp.intValue / chunkSizeProp.intValue);
                    int chunksZ = Mathf.CeilToInt((float)heightProp.intValue / chunkSizeProp.intValue);
                    EditorGUILayout.LabelField($"Chunks: {chunksX}x{chunksZ} = {chunksX * chunksZ} total");
                }
            }

            // Import/Export
            EditorGUILayout.Space();
            showImportExport = EditorGUILayout.Foldout(showImportExport, "Import/Export", true, EditorStyles.foldoutHeader);
            if (showImportExport)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Export to JSON"))
                {
                    string path = EditorUtility.SaveFilePanel(
                        "Export Grid Data",
                        Application.dataPath,
                        "GridData.json",
                        "json");

                    if (!string.IsNullOrEmpty(path))
                    {
                        gridData.SaveToFile(path);
                    }
                }

                if (GUILayout.Button("Import from JSON"))
                {
                    string path = EditorUtility.OpenFilePanel(
                        "Import Grid Data",
                        Application.dataPath,
                        "json");

                    if (!string.IsNullOrEmpty(path))
                    {
                        Undo.RecordObject(gridData, "Import Grid Data");
                        gridData.LoadFromFile(path);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            // Apply modified properties
            serializedObject.ApplyModifiedProperties();

            // Initialize button
            EditorGUILayout.Space();
            if (GUILayout.Button("Initialize Grid"))
            {
                Undo.RecordObject(gridData, "Initialize Grid");
                gridData.Initialize();
            }
        }
    }
}
#endif