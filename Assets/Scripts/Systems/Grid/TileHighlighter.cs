using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Highlights tiles under the mouse cursor with smooth, subtle effects.
/// Modified to work with the corner-based tile visualization.
/// </summary>
public class TileHighlighter : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Highlighting")]
    [Tooltip("How subtle should the highlight effect be (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float highlightIntensity = 0.7f;
    
    [Tooltip("Which layers should be checked for tiles")]
    [SerializeField] private LayerMask tileLayerMask = Physics.DefaultRaycastLayers;
    
    [Tooltip("Maximum distance for the raycast")]
    [SerializeField] private float maxHighlightDistance = 100f;
    
    [Header("Animation")]
    [Tooltip("Delay before highlighting a tile (seconds)")]
    [SerializeField] private float highlightDelay = 0.05f;
    
    [Tooltip("How long the fade-in animation takes (seconds)")]
    [SerializeField] private float fadeInDuration = 0.1f;
    
    [Tooltip("How long the fade-out animation takes (seconds)")]
    [SerializeField] private float fadeOutDuration = 0.2f;
    
    [Tooltip("How much to elevate the highlighted tile (units)")]
    [SerializeField] private float elevationAmount = 0.05f;
    
    [Tooltip("How fast the elevation animation is (seconds)")]
    [SerializeField] private float elevationDuration = 0.1f;
    
    [Tooltip("Should tiles pulse slightly when highlighted?")]
    [SerializeField] private bool usePulseEffect = true;
    
    [Tooltip("Optional highlight material - if null, will just modify corner colors")]
    [SerializeField] private Material highlightMaterial;
    
    #endregion

    #region Private Variables
    
    private Camera _mainCamera;
    private Tile _currentHighlightedTile;
    private Tile _lastHoveredTile;
    
    // Track original properties to restore later
    private Dictionary<Tile, Vector3> _originalPositions = new Dictionary<Tile, Vector3>();
    
    // Coroutines for animations
    private Coroutine _highlightDelayCoroutine;
    private Dictionary<Tile, Coroutine> _elevationCoroutines = new Dictionary<Tile, Coroutine>();
    private Dictionary<Tile, Coroutine> _pulseCoroutines = new Dictionary<Tile, Coroutine>();
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (_mainCamera == null)
        {
            Debug.LogError("No main camera found! TileHighlighter requires a camera tagged as 'MainCamera'.");
            enabled = false;
        }
    }
    
    private void Update()
    {
        HandleMouseHover();
    }
    
    private void OnDisable()
    {
        // Reset all highlighted tiles
        if (_currentHighlightedTile != null)
        {
            ResetTileImmediately(_currentHighlightedTile);
            _currentHighlightedTile = null;
        }
        
        if (_lastHoveredTile != null)
        {
            ResetTileImmediately(_lastHoveredTile);
            _lastHoveredTile = null;
        }
    }
    
    #endregion

    #region Tile Highlighting
    
    /// <summary>
    /// Handles mouse hover over tiles
    /// </summary>
    private void HandleMouseHover()
    {
        // Get mouse position using the new Input System
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // Cast ray from mouse position
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        
        Tile hoveredTile = null;
        
        if (Physics.Raycast(ray, out hit, maxHighlightDistance, tileLayerMask))
        {
            // Check if we hit a tile
            hoveredTile = hit.collider.GetComponent<Tile>();
        }
        
        // If we moved to a different tile (or off a tile)
        if (hoveredTile != _lastHoveredTile)
        {
            // If we were hovering over a tile before, fade it out
            if (_lastHoveredTile != null)
            {
                // Cancel any pending highlight
                if (_highlightDelayCoroutine != null)
                {
                    StopCoroutine(_highlightDelayCoroutine);
                    _highlightDelayCoroutine = null;
                }
                
                // Fade out the last tile immediately
                FadeTileOut(_lastHoveredTile);
            }
            
            // If we're now over a new tile, start highlighting it
            if (hoveredTile != null)
            {
                // Start the highlight delay
                if (_highlightDelayCoroutine != null)
                {
                    StopCoroutine(_highlightDelayCoroutine);
                }
                
                _highlightDelayCoroutine = StartCoroutine(DelayedHighlight(hoveredTile));
            }
            
            _lastHoveredTile = hoveredTile;
        }
    }
    
    /// <summary>
    /// Delays highlighting until the mouse has hovered for the specified time
    /// </summary>
    private IEnumerator DelayedHighlight(Tile tile)
    {
        yield return new WaitForSeconds(highlightDelay);
        
        // If tile is still valid
        if (tile != null)
        {
            _currentHighlightedTile = tile;
            
            // Start highlight effects
            HighlightTile(tile);
        }
        
        _highlightDelayCoroutine = null;
    }
    
    /// <summary>
    /// Highlights a tile with multiple effects
    /// </summary>
    private void HighlightTile(Tile tile)
    {
        if (tile == null) return;
        
        // Set the tile's highlight state
        tile.SetHighlighted(true);
        
        // Add subtle elevation
        if (elevationAmount > 0)
        {
            ElevateTile(tile);
        }
        
        // Add pulse effect if enabled
        if (usePulseEffect)
        {
            StartPulseEffect(tile);
        }
    }
    
    /// <summary>
    /// Fades out the highlight for a tile
    /// </summary>
    private void FadeTileOut(Tile tile)
    {
        if (tile == null) return;
        
        // Remove highlight state
        tile.SetHighlighted(false);
        
        // Lower the tile
        LowerTile(tile);
        
        // Stop pulse effect
        StopPulseEffect(tile);
        
        // Remove as current highlighted tile if it is
        if (_currentHighlightedTile == tile)
        {
            _currentHighlightedTile = null;
        }
    }
    
    /// <summary>
    /// Elevates a tile for emphasis
    /// </summary>
    private void ElevateTile(Tile tile)
    {
        if (tile == null || elevationAmount <= 0) return;
        
        // Stop existing elevation if any
        if (_elevationCoroutines.TryGetValue(tile, out Coroutine existingElevation) && existingElevation != null)
        {
            StopCoroutine(existingElevation);
        }
        
        // Store original position if not already stored
        if (!_originalPositions.ContainsKey(tile))
        {
            _originalPositions[tile] = tile.transform.position;
        }
        
        // Start new elevation
        _elevationCoroutines[tile] = StartCoroutine(AnimateElevation(tile, elevationAmount, elevationDuration));
    }
    
    /// <summary>
    /// Lowers a tile back to its original position
    /// </summary>
    private void LowerTile(Tile tile)
    {
        if (tile == null) return;
        
        // Stop existing elevation if any
        if (_elevationCoroutines.TryGetValue(tile, out Coroutine existingElevation) && existingElevation != null)
        {
            StopCoroutine(existingElevation);
        }
        
        // Get original position
        Vector3 originalPos = tile.transform.position;
        if (_originalPositions.TryGetValue(tile, out Vector3 storedPos))
        {
            originalPos = storedPos;
        }
        
        // Start new animation
        _elevationCoroutines[tile] = StartCoroutine(AnimatePosition(tile, originalPos, elevationDuration));
    }
    
    /// <summary>
    /// Starts a subtle pulse effect on the tile
    /// </summary>
    private void StartPulseEffect(Tile tile)
    {
        if (tile == null) return;
        
        // Stop existing pulse if any
        StopPulseEffect(tile);
        
        // Start new pulse
        _pulseCoroutines[tile] = StartCoroutine(PulseEffect(tile));
    }
    
    /// <summary>
    /// Stops the pulse effect on a tile
    /// </summary>
    private void StopPulseEffect(Tile tile)
    {
        if (tile == null) return;
        
        // Stop existing pulse if any
        if (_pulseCoroutines.TryGetValue(tile, out Coroutine existingPulse) && existingPulse != null)
        {
            StopCoroutine(existingPulse);
            _pulseCoroutines.Remove(tile);
        }
    }
    
    /// <summary>
    /// Immediately resets a tile's appearance without animation
    /// </summary>
    private void ResetTileImmediately(Tile tile)
    {
        if (tile == null) return;
        
        // Remove highlight state
        tile.SetHighlighted(false);
        
        // Stop all animations
        if (_elevationCoroutines.TryGetValue(tile, out Coroutine elevCo) && elevCo != null)
        {
            StopCoroutine(elevCo);
            _elevationCoroutines.Remove(tile);
        }
        
        StopPulseEffect(tile);
        
        // Reset position
        if (_originalPositions.TryGetValue(tile, out Vector3 originalPos))
        {
            tile.transform.position = originalPos;
            _originalPositions.Remove(tile);
        }
    }
    
    #endregion

    #region Animation Coroutines
    
    /// <summary>
    /// Smoothly elevates a tile
    /// </summary>
    private IEnumerator AnimateElevation(Tile tile, float elevation, float duration)
    {
        if (tile == null) yield break;
        
        // Store original position if this is the first elevation
        if (!_originalPositions.ContainsKey(tile))
        {
            _originalPositions[tile] = tile.transform.position;
        }
        
        Vector3 startPos = tile.transform.position;
        Vector3 targetPos = _originalPositions[tile] + Vector3.up * elevation;
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            if (tile == null) yield break;
            
            float t = (Time.time - startTime) / duration;
            // Use ease-out for smoother motion
            t = 1.0f - (1.0f - t) * (1.0f - t);
            tile.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        // Ensure we reach target position exactly
        if (tile != null)
        {
            tile.transform.position = targetPos;
        }
        
        // Clean up reference once animation is complete
        if (_elevationCoroutines.ContainsKey(tile))
        {
            _elevationCoroutines.Remove(tile);
        }
    }
    
    /// <summary>
    /// Smoothly moves a tile to a position
    /// </summary>
    private IEnumerator AnimatePosition(Tile tile, Vector3 targetPos, float duration)
    {
        if (tile == null) yield break;
        
        Vector3 startPos = tile.transform.position;
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            if (tile == null) yield break;
            
            float t = (Time.time - startTime) / duration;
            // Use ease-in for smoother motion
            t = t * t;
            tile.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        // Ensure we reach target position exactly
        if (tile != null)
        {
            tile.transform.position = targetPos;
        }
        
        // Clean up reference once animation is complete
        if (_elevationCoroutines.ContainsKey(tile))
        {
            _elevationCoroutines.Remove(tile);
        }
        
        // If this was a move back to original position, clean up
        if (targetPos == _originalPositions.GetValueOrDefault(tile, Vector3.zero))
        {
            _originalPositions.Remove(tile);
        }
    }
    
    /// <summary>
    /// Creates a subtle pulse effect on the tile's corners
    /// </summary>
    private IEnumerator PulseEffect(Tile tile)
    {
        if (tile == null) yield break;
        
        // Very subtle scale pulsing for corners
        float minScale = 0.95f;
        float maxScale = 1.05f;
        float pulseDuration = 1.0f;
        
        // Find all corner objects - look for any child objects that have renderers
        List<Transform> cornerObjects = new List<Transform>();
        for (int i = 0; i < tile.transform.childCount; i++)
        {
            Transform child = tile.transform.GetChild(i);
            if (child.GetComponentInChildren<Renderer>() != null)
            {
                cornerObjects.Add(child);
            }
        }
        
        if (cornerObjects.Count == 0) yield break;
        
        // Store original scales
        Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
        foreach (Transform corner in cornerObjects)
        {
            originalScales[corner] = corner.localScale;
        }
        
        // Pulse indefinitely until stopped
        while (true)
        {
            // Pulse up
            float startTime = Time.time;
            while (Time.time < startTime + pulseDuration * 0.5f)
            {
                if (tile == null) yield break;
                
                float t = (Time.time - startTime) / (pulseDuration * 0.5f);
                t = Mathf.SmoothStep(0, 1, t); // Smooth easing
                
                float scaleMultiplier = Mathf.Lerp(1.0f, maxScale, t);
                
                foreach (Transform corner in cornerObjects)
                {
                    if (corner == null) continue;
                    corner.localScale = originalScales[corner] * scaleMultiplier;
                }
                
                yield return null;
            }
            
            // Pulse down
            startTime = Time.time;
            while (Time.time < startTime + pulseDuration * 0.5f)
            {
                if (tile == null) yield break;
                
                float t = (Time.time - startTime) / (pulseDuration * 0.5f);
                t = Mathf.SmoothStep(0, 1, t); // Smooth easing
                
                float scaleMultiplier = Mathf.Lerp(maxScale, minScale, t);
                
                foreach (Transform corner in cornerObjects)
                {
                    if (corner == null) continue;
                    corner.localScale = originalScales[corner] * scaleMultiplier;
                }
                
                yield return null;
            }
            
            // Pulse back to normal
            startTime = Time.time;
            while (Time.time < startTime + pulseDuration * 0.5f)
            {
                if (tile == null) yield break;
                
                float t = (Time.time - startTime) / (pulseDuration * 0.5f);
                t = Mathf.SmoothStep(0, 1, t); // Smooth easing
                
                float scaleMultiplier = Mathf.Lerp(minScale, 1.0f, t);
                
                foreach (Transform corner in cornerObjects)
                {
                    if (corner == null) continue;
                    corner.localScale = originalScales[corner] * scaleMultiplier;
                }
                
                yield return null;
            }
            
            // Ensure we reset to the original scale exactly
            foreach (Transform corner in cornerObjects)
            {
                if (corner == null) continue;
                corner.localScale = originalScales[corner];
            }
            
            // Small pause between pulses
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    #endregion
}