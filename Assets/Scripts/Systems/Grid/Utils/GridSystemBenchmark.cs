using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Debug = UnityEngine.Debug;
using System.Linq;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Utility class for benchmarking the data-driven grid system performance
    /// Provides methods to test creation, pathfinding, and rendering performance
    /// Can be used both in editor and runtime
    /// </summary>
    public class GridSystemBenchmark
    {
        #region Benchmark Results
        /// <summary>
        /// Structure to store benchmark results
        /// </summary>
        public class BenchmarkResult
        {
            public int GridSize { get; set; }
            public int ChunkSize { get; set; }
            public float CreationTime { get; set; }
            public float MemoryUsage { get; set; }
            public float PathfindingTime { get; set; }
            public float VisibilityCheckTime { get; set; }
            public float PathValidationTime { get; set; }
            public float RenderingFPS { get; set; }
            public int ActiveTileCount { get; set; }
            public string Description { get; set; }

            public BenchmarkResult()
            {
                Description = "Default";
            }

            public BenchmarkResult(int gridSize, int chunkSize)
            {
                GridSize = gridSize;
                ChunkSize = chunkSize;
                Description = $"Grid:{gridSize}x{gridSize}, Chunk:{chunkSize}";
            }

            public override string ToString()
            {
                return $"Grid: {GridSize}x{GridSize}, Chunk: {ChunkSize}, " +
                       $"Creation: {CreationTime:F3}s, Memory: {MemoryUsage:F2}MB, " +
                       $"Pathfinding: {PathfindingTime:F6}s, Rendering: {RenderingFPS:F1}FPS";
            }
        }
        #endregion

        #region Static Properties
        /// <summary>
        /// List of all benchmark results
        /// </summary>
        public static List<BenchmarkResult> Results { get; private set; } = new List<BenchmarkResult>();

        /// <summary>
        /// Is a benchmark currently running
        /// </summary>
        public static bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Callback for when benchmark is complete
        /// </summary>
        public static System.Action<List<BenchmarkResult>> OnBenchmarkComplete;

        /// <summary>
        /// Callback for benchmark progress
        /// </summary>
        public static System.Action<float, string> OnBenchmarkProgress;
        #endregion

        #region Benchmark Methods
        /// <summary>
        /// Run a full benchmark suite
        /// </summary>
        /// <param name="gridManager">The grid manager to benchmark</param>
        /// <param name="gridSizes">Array of grid sizes to test</param>
        /// <param name="chunkSizes">Array of chunk sizes to test</param>
        /// <param name="iterations">Number of iterations per test</param>
        /// <returns>Coroutine for the benchmark process</returns>
        public static IEnumerator RunBenchmarkSuite(
            GridManager gridManager,
            int[] gridSizes = null,
            int[] chunkSizes = null,
            int iterations = 3,
            float delayBetweenTests = 0.5f)
        {
            if (gridManager == null)
            {
                Debug.LogError("Cannot run benchmark: Grid Manager is null");
                yield break;
            }

            IsRunning = true;
            Results.Clear();

            // Default values if not specified
            if (gridSizes == null || gridSizes.Length == 0)
                gridSizes = new int[] { 10, 20, 50, 100 };

            if (chunkSizes == null || chunkSizes.Length == 0)
                chunkSizes = new int[] { 5, 10, 20 };

            // Calculate total tests
            int totalTests = gridSizes.Length * chunkSizes.Length;
            int currentTest = 0;

            Debug.Log("Starting Grid System Benchmark...");

            // Test different grid and chunk sizes
            for (int i = 0; i < gridSizes.Length; i++)
            {
                for (int j = 0; j < chunkSizes.Length; j++)
                {
                    int gridSize = gridSizes[i];
                    int chunkSize = chunkSizes[j];

                    // Skip combinations that don't make sense (chunk size > grid size)
                    if (chunkSize > gridSize)
                        continue;

                    // Calculate progress
                    float progress = (float)currentTest / totalTests;
                    OnBenchmarkProgress?.Invoke(progress, $"Testing Grid: {gridSize}x{gridSize}, Chunk: {chunkSize}");

                    Debug.Log($"Testing grid size: {gridSize}x{gridSize} with chunk size: {chunkSize}");

                    // Average over multiple iterations
                    BenchmarkResult averageResult = new BenchmarkResult(gridSize, chunkSize);

                    for (int iter = 0; iter < iterations; iter++)
                    {
                        BenchmarkResult iterResult = new BenchmarkResult(gridSize, chunkSize);

                        // Test grid creation
                        yield return BenchmarkGridCreation(gridManager, gridSize, chunkSize, iterResult);

                        // Test pathfinding
                        yield return BenchmarkPathfinding(gridManager, gridSize, iterResult);

                        // Test rendering (if in play mode)
                        if (Application.isPlaying)
                        {
                            yield return BenchmarkRendering(gridManager, iterResult);
                        }

                        // Accumulate results
                        averageResult.CreationTime += iterResult.CreationTime / iterations;
                        averageResult.MemoryUsage += iterResult.MemoryUsage / iterations;
                        averageResult.PathfindingTime += iterResult.PathfindingTime / iterations;
                        averageResult.PathValidationTime += iterResult.PathValidationTime / iterations;
                        averageResult.VisibilityCheckTime += iterResult.VisibilityCheckTime / iterations;
                        averageResult.RenderingFPS += iterResult.RenderingFPS / iterations;
                        averageResult.ActiveTileCount += iterResult.ActiveTileCount / iterations;

                        // Clean up between iterations
                        System.GC.Collect();
                        yield return new WaitForSeconds(delayBetweenTests);
                    }

                    // Add average result to results list
                    Results.Add(averageResult);
                    currentTest++;

                    // Report progress
                    OnBenchmarkProgress?.Invoke((float)currentTest / totalTests, $"Completed {currentTest}/{totalTests} tests");

                    // Brief delay between configurations
                    yield return new WaitForSeconds(delayBetweenTests);
                }
            }

            // Log results
            LogResults();

            // Notify completion
            IsRunning = false;
            OnBenchmarkComplete?.Invoke(Results);
        }

        /// <summary>
        /// Benchmark grid creation
        /// </summary>
        private static IEnumerator BenchmarkGridCreation(
            GridManager gridManager, 
            int gridSize, 
            int chunkSize, 
            BenchmarkResult result)
        {
            // Ensure we have a clean slate
            System.GC.Collect();
            yield return null;

            // Record starting memory
            float startMemory = System.GC.GetTotalMemory(true) / (1024f * 1024f); // MB

            // Create stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Configure the grid manager
            if (gridManager.gridData != null)
            {
                // Set properties via reflection
                var widthField = gridManager.gridData.GetType().GetField("width", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var heightField = gridManager.gridData.GetType().GetField("height", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var chunkSizeField = gridManager.gridData.GetType().GetField("chunkSize", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                if (widthField != null) widthField.SetValue(gridManager.gridData, gridSize);
                if (heightField != null) heightField.SetValue(gridManager.gridData, gridSize);
                if (chunkSizeField != null) chunkSizeField.SetValue(gridManager.gridData, chunkSize);
            }

            // Initialize grid
            gridManager.gridData.Initialize();

            // Create chunk renderers
            Transform gridParent = new GameObject("GridParent").transform;
            gridManager.gridData.GenerateChunkRenderers(gridParent);

            // Stop timing
            stopwatch.Stop();
            result.CreationTime = stopwatch.ElapsedMilliseconds / 1000f;

            // Calculate memory usage
            float endMemory = System.GC.GetTotalMemory(false) / (1024f * 1024f); // MB
            result.MemoryUsage = endMemory - startMemory;

            // Record active tile count
            if (gridManager.gridData != null)
            {
                result.ActiveTileCount = gridManager.gridData.Width * gridManager.gridData.Height;
            }

            Debug.Log($"Creation Time: {result.CreationTime:F3}s, Memory Usage: {result.MemoryUsage:F2}MB");
        }

        /// <summary>
        /// Benchmark pathfinding
        /// </summary>
        private static IEnumerator BenchmarkPathfinding(
            GridManager gridManager, 
            int gridSize, 
            BenchmarkResult result)
        {
            if (gridManager.gridData == null)
                yield break;

            Stopwatch stopwatch = new Stopwatch();

            // Setup various pathfinding tests
            Vector2Int[] startPoints = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(gridSize / 4, gridSize / 4),
                new Vector2Int(gridSize / 2, 0)
            };

            Vector2Int[] endPoints = new Vector2Int[]
            {
                new Vector2Int(gridSize - 1, gridSize - 1),
                new Vector2Int(gridSize / 2, gridSize / 2),
                new Vector2Int(gridSize - 1, 0)
            };

            int numTests = startPoints.Length;
            float totalPathfindingTime = 0f;
            float totalPathValidationTime = 0f;

            // Run pathfinding tests
            for (int i = 0; i < numTests; i++)
            {
                Vector2Int start = startPoints[i];
                Vector2Int end = endPoints[i];

                // Skip if invalid coordinates
                if (!gridManager.gridData.IsValidPosition(start) || !gridManager.gridData.IsValidPosition(end))
                    continue;

                // Measure basic pathfinding
                stopwatch.Restart();
                List<Vector2Int> path = gridManager.gridData.FindPath(start, end);
                stopwatch.Stop();

                totalPathfindingTime += stopwatch.ElapsedMilliseconds;

                // Skip if no path found
                if (path == null || path.Count == 0)
                    continue;

                // Measure path validation (movement cost calculation)
                stopwatch.Restart();
                float pathCost = 0;
                for (int j = 1; j < path.Count; j++)
                {
                    TileData tile = gridManager.gridData.GetTileData(path[j]);
                    if (tile != null)
                    {
                        pathCost += tile.MovementCost;
                    }
                }
                stopwatch.Stop();

                totalPathValidationTime += stopwatch.ElapsedMilliseconds;

                // Give the system a chance to breathe
                if (i % 3 == 0)
                    yield return null;
            }

            // Calculate average times
            if (numTests > 0)
            {
                result.PathfindingTime = totalPathfindingTime / (numTests * 1000f);
                result.PathValidationTime = totalPathValidationTime / (numTests * 1000f);
            }

            Debug.Log($"Pathfinding Time: {result.PathfindingTime:F6}s, Path Validation: {result.PathValidationTime:F6}s");
        }

        /// <summary>
        /// Benchmark rendering performance
        /// </summary>
        private static IEnumerator BenchmarkRendering(
            GridManager gridManager, 
            BenchmarkResult result)
        {
            if (!Application.isPlaying || Camera.main == null)
                yield break;

            // Position camera to see the grid
            Vector3 originalCamPos = Camera.main.transform.position;
            Quaternion originalCamRot = Camera.main.transform.rotation;

            int gridSize = result.GridSize;
            Camera.main.transform.position = new Vector3(gridSize / 2f, gridSize, gridSize / 2f);
            Camera.main.transform.rotation = Quaternion.Euler(45, 0, 0);

            // Accumulate FPS over several frames
            float fpsAccumulation = 0;
            int frameCount = 0;
            float measurementDuration = 1f; // seconds
            float startTime = Time.realtimeSinceStartup;

            // Measure FPS
            while (Time.realtimeSinceStartup - startTime < measurementDuration)
            {
                fpsAccumulation += 1f / Time.deltaTime;
                frameCount++;
                yield return null;
            }

            // Calculate average FPS
            if (frameCount > 0)
            {
                result.RenderingFPS = fpsAccumulation / frameCount;
            }

            // Measure time for visibility checks
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Call visibility update method if it exists
            if (gridManager.gridData != null)
            {
                System.Reflection.MethodInfo visibilityMethod = gridManager.gridData.GetType()
                    .GetMethod("UpdateChunkVisibility", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (visibilityMethod != null)
                {
                    visibilityMethod.Invoke(gridManager.gridData, new object[] { Camera.main.transform.position });
                }
            }

            stopwatch.Stop();
            result.VisibilityCheckTime = stopwatch.ElapsedMilliseconds / 1000f;

            // Restore camera
            Camera.main.transform.position = originalCamPos;
            Camera.main.transform.rotation = originalCamRot;

            Debug.Log($"Rendering FPS: {result.RenderingFPS:F1}, Visibility Check: {result.VisibilityCheckTime:F6}s");
        }

        /// <summary>
        /// Quick test to measure pathfinding performance
        /// </summary>
        public static float BenchmarkPathfindingSingle(GridManager gridManager, Vector2Int start, Vector2Int end, int iterations = 10)
        {
            if (gridManager == null || gridManager.gridData == null)
                return 0f;

            if (!gridManager.gridData.IsValidPosition(start) || !gridManager.gridData.IsValidPosition(end))
                return 0f;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < iterations; i++)
            {
                gridManager.gridData.FindPath(start, end);
            }

            stopwatch.Stop();
            return (stopwatch.ElapsedMilliseconds / (float)iterations) / 1000f;
        }
        #endregion

        #region Reporting Methods
        /// <summary>
        /// Log benchmark results to the console
        /// </summary>
        public static void LogResults()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========== GRID SYSTEM BENCHMARK RESULTS ==========");
            sb.AppendLine();
            sb.AppendLine("Grid Size\tChunk Size\tCreate Time (s)\tMemory (MB)\tPathfinding (s)\tFPS");
            
            foreach (var result in Results)
            {
                sb.AppendLine($"{result.GridSize}x{result.GridSize}\t{result.ChunkSize}\t{result.CreationTime:F3}\t{result.MemoryUsage:F2}\t{result.PathfindingTime:F6}\t{result.RenderingFPS:F1}");
            }
            
            sb.AppendLine();
            sb.AppendLine("==================================================");
            
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Save results to a CSV file
        /// </summary>
        public static void SaveResultsToFile(string filePath = "GridBenchmarkResults.csv")
        {
            StringBuilder sb = new StringBuilder();
            
            // Header
            sb.AppendLine("GridSize,ChunkSize,CreationTime,MemoryUsage,PathfindingTime,PathValidationTime,VisibilityCheckTime,RenderingFPS,ActiveTileCount,Description");
            
            // Data
            foreach (var result in Results)
            {
                sb.AppendLine($"{result.GridSize},{result.ChunkSize},{result.CreationTime:F6},{result.MemoryUsage:F6},{result.PathfindingTime:F6},{result.PathValidationTime:F6},{result.VisibilityCheckTime:F6},{result.RenderingFPS:F3},{result.ActiveTileCount},\"{result.Description}\"");
            }
            
            // Ensure the directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Write to file
            File.WriteAllText(filePath, sb.ToString());
            
            Debug.Log($"Benchmark results saved to {filePath}");
        }
        #endregion

        #region Visual Report Methods
        /// <summary>
        /// Generate a graph of benchmark results
        /// </summary>
        public static Texture2D GenerateResultGraph(string metric = "CreationTime", int width = 800, int height = 400)
        {
            if (Results.Count == 0)
                return null;

            // Create texture
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            Color gridColor = new Color(0.3f, 0.3f, 0.3f);
            Color[] dataColors = new Color[]
            {
                new Color(1.0f, 0.5f, 0.5f),
                new Color(0.5f, 1.0f, 0.5f),
                new Color(0.5f, 0.5f, 1.0f),
                new Color(1.0f, 1.0f, 0.5f),
                new Color(1.0f, 0.5f, 1.0f)
            };

            // Fill background
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }
            texture.SetPixels(pixels);

            // Group results by grid size
            Dictionary<int, List<BenchmarkResult>> resultsByGridSize = new Dictionary<int, List<BenchmarkResult>>();
            foreach (var result in Results)
            {
                if (!resultsByGridSize.ContainsKey(result.GridSize))
                {
                    resultsByGridSize[result.GridSize] = new List<BenchmarkResult>();
                }
                resultsByGridSize[result.GridSize].Add(result);
            }

            // Get min/max values for the selected metric
            float maxValue = float.MinValue;
            foreach (var result in Results)
            {
                float value = GetMetricValue(result, metric);
                if (value > maxValue)
                    maxValue = value;
            }

            // Draw grid lines
            int gridLinesX = 10;
            int gridLinesY = 5;
            for (int i = 0; i <= gridLinesX; i++)
            {
                int x = (int)(width * i / (float)gridLinesX);
                DrawVerticalLine(texture, x, 0, height, gridColor);
            }
            for (int i = 0; i <= gridLinesY; i++)
            {
                int y = (int)(height * i / (float)gridLinesY);
                DrawHorizontalLine(texture, 0, width, y, gridColor);
            }

            // Plot data
            int colorIndex = 0;
            int margin = 40;
            int plotWidth = width - 2 * margin;
            int plotHeight = height - 2 * margin;

            foreach (var kvp in resultsByGridSize)
            {
                int gridSize = kvp.Key;
                List<BenchmarkResult> results = kvp.Value;

                // Sort by chunk size
                results.Sort((a, b) => a.ChunkSize.CompareTo(b.ChunkSize));

                // Draw line connecting points
                for (int i = 0; i < results.Count - 1; i++)
                {
                    float x1 = margin + (float)i / (results.Count - 1) * plotWidth;
                    float y1 = margin + (1 - GetMetricValue(results[i], metric) / maxValue) * plotHeight;

                    float x2 = margin + (float)(i + 1) / (results.Count - 1) * plotWidth;
                    float y2 = margin + (1 - GetMetricValue(results[i + 1], metric) / maxValue) * plotHeight;

                    DrawLine(texture, (int)x1, (int)y1, (int)x2, (int)y2, dataColors[colorIndex % dataColors.Length]);
                }

                // Draw points
                for (int i = 0; i < results.Count; i++)
                {
                    float x = margin + (float)i / (results.Count - 1) * plotWidth;
                    float y = margin + (1 - GetMetricValue(results[i], metric) / maxValue) * plotHeight;

                    DrawCircle(texture, (int)x, (int)y, 5, dataColors[colorIndex % dataColors.Length]);
                }

                colorIndex++;
            }

            // Apply changes
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Get value of a specific metric from a result
        /// </summary>
        private static float GetMetricValue(BenchmarkResult result, string metric)
        {
            switch (metric)
            {
                case "CreationTime": return result.CreationTime;
                case "MemoryUsage": return result.MemoryUsage;
                case "PathfindingTime": return result.PathfindingTime;
                case "VisibilityCheckTime": return result.VisibilityCheckTime;
                case "PathValidationTime": return result.PathValidationTime;
                case "RenderingFPS": return result.RenderingFPS;
                default: return 0f;
            }
        }

        /// <summary>
        /// Draw a horizontal line on the texture
        /// </summary>
        private static void DrawHorizontalLine(Texture2D texture, int x1, int x2, int y, Color color)
        {
            for (int x = x1; x < x2; x++)
            {
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>
        /// Draw a vertical line on the texture
        /// </summary>
        private static void DrawVerticalLine(Texture2D texture, int x, int y1, int y2, Color color)
        {
            for (int y = y1; y < y2; y++)
            {
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>
        /// Draw a line on the texture using Bresenham's algorithm
        /// </summary>
        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < texture.width && y0 >= 0 && y0 < texture.height)
                {
                    texture.SetPixel(x0, y0, color);
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>
        /// Draw a circle on the texture
        /// </summary>
        private static void DrawCircle(Texture2D texture, int x0, int y0, int radius, Color color)
        {
            int x = radius;
            int y = 0;
            int radiusError = 1 - x;

            while (x >= y)
            {
                // Draw the 8 symmetric points
                DrawPixel(texture, x + x0, y + y0, color);
                DrawPixel(texture, y + x0, x + y0, color);
                DrawPixel(texture, -x + x0, y + y0, color);
                DrawPixel(texture, -y + x0, x + y0, color);
                DrawPixel(texture, -x + x0, -y + y0, color);
                DrawPixel(texture, -y + x0, -x + y0, color);
                DrawPixel(texture, x + x0, -y + y0, color);
                DrawPixel(texture, y + x0, -x + y0, color);

                y++;
                if (radiusError < 0)
                {
                    radiusError += 2 * y + 1;
                }
                else
                {
                    x--;
                    radiusError += 2 * (y - x) + 1;
                }
            }
        }

        /// <summary>
        /// Draw a pixel on the texture
        /// </summary>
        private static void DrawPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }
        #endregion

        #region Custom Benchmark Helpers
        /// <summary>
        /// Helper method to measure the impact of varying grid sizes on pathfinding
        /// </summary>
        public static IEnumerator BenchmarkPathfindingByDistance(GridManager gridManager, int gridSize, int iterations = 50)
        {
            if (gridManager == null || gridManager.gridData == null)
                yield break;

            // Configure grid if needed
            if (gridManager.gridData.Width != gridSize || gridManager.gridData.Height != gridSize)
            {
                // Set properties via reflection
                var widthField = gridManager.gridData.GetType().GetField("width", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var heightField = gridManager.gridData.GetType().GetField("height", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                if (widthField != null) widthField.SetValue(gridManager.gridData, gridSize);
                if (heightField != null) heightField.SetValue(gridManager.gridData, gridSize);

                // Initialize grid
                gridManager.gridData.Initialize();
            }

            // Create list of distances to test
            List<int> distances = new List<int>();
            int step = Mathf.Max(1, gridSize / 10);
            for (int d = 1; d <= gridSize; d += step)
            {
                distances.Add(d);
            }
            if (!distances.Contains(gridSize))
                distances.Add(gridSize);

            // Results for each distance
            Dictionary<int, float> pathfindingTimesByDistance = new Dictionary<int, float>();

            // Run benchmark for each distance
            foreach (int distance in distances)
            {
                Vector2Int start = new Vector2Int(0, 0);
                Vector2Int end = new Vector2Int(distance - 1, 0);

                // Skip if invalid
                if (!gridManager.gridData.IsValidPosition(start) || !gridManager.gridData.IsValidPosition(end))
                    continue;

                float totalTime = 0f;
                Stopwatch stopwatch = new Stopwatch();

                for (int i = 0; i < iterations; i++)
                {
                    stopwatch.Restart();
                    gridManager.gridData.FindPath(start, end);
                    stopwatch.Stop();
                    totalTime += stopwatch.ElapsedTicks;

                    if (i % 10 == 0)
                        yield return null;
                }

                // Convert to seconds and store
                float avgTime = (totalTime / iterations) / Stopwatch.Frequency;
                pathfindingTimesByDistance[distance] = avgTime;

                Debug.Log($"Distance: {distance}, Avg Time: {avgTime:F6}s");
                yield return null;
            }

            // Generate report
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Pathfinding by Distance Results:");
            sb.AppendLine("Distance,Time(s)");
            
            foreach (var pair in pathfindingTimesByDistance)
            {
                sb.AppendLine($"{pair.Key},{pair.Value:F8}");
            }
            
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Helper method to benchmark tile-specific operations
        /// </summary>
        public static void BenchmarkTileOperations(GridManager gridManager, int gridSize, int iterations = 1000)
        {
            if (gridManager == null || gridManager.gridData == null)
                return;

            Stopwatch stopwatch = new Stopwatch();
            Dictionary<string, float> results = new Dictionary<string, float>
            {
                { "GetTileData", 0 },
                { "SetTerrain", 0 },
                { "SetWalkable", 0 },
                { "SetOccupancy", 0 },
                { "WorldToGrid", 0 },
                { "GridToWorld", 0 }
            };

            // Run tests
            for (int op = 0; op < results.Count; op++)
            {
                string operation = results.Keys.ElementAt(op);
                stopwatch.Restart();

                for (int i = 0; i < iterations; i++)
                {
                    int x = Random.Range(0, gridSize);
                    int z = Random.Range(0, gridSize);
                    Vector2Int pos = new Vector2Int(x, z);
                    
                    switch (operation)
                    {
                        case "GetTileData":
                            gridManager.gridData.GetTileData(x, z);
                            break;
                        case "SetTerrain":
                            gridManager.gridData.SetTileTerrain(x, z, (TerrainType)(i % 10));
                            break;
                        case "SetWalkable":
                            gridManager.gridData.SetTileWalkable(x, z, i % 2 == 0);
                            break;
                        case "SetOccupancy":
                            gridManager.gridData.SetTileOccupancy(x, z, i % 2 == 0);
                            break;
                        case "WorldToGrid":
                            Vector3 worldPos = gridManager.gridData.GridToWorldPosition(x, z);
                            gridManager.gridData.WorldToGridPosition(worldPos, out int _, out int _);
                            break;
                        case "GridToWorld":
                            gridManager.gridData.GridToWorldPosition(x, z);
                            break;
                    }
                }

                stopwatch.Stop();
                results[operation] = (stopwatch.ElapsedTicks / (float)iterations) / Stopwatch.Frequency;
            }

            // Log results
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Tile Operation Performance:");
            foreach (var pair in results)
            {
                sb.AppendLine($"{pair.Key}: {pair.Value * 1000000:F2} μs per operation");
            }
            Debug.Log(sb.ToString());
        }
        #endregion
    }
}