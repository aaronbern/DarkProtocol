using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Controls camera movement, rotation, and zoom for tactical grid-based gameplay.
/// Works in conjunction with MapBounds to ensure camera stays within valid play area.
/// </summary>
public class TacticalCameraController : MonoBehaviour
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("Reference to MapBounds component (optional, will auto-find if not set)")]
    [SerializeField] private MapBounds mapBounds;

    [Header("Movement Settings")]
    [Tooltip("Camera movement speed")]
    [SerializeField] private float moveSpeed = 20f;

    [Tooltip("Camera movement acceleration")]
    [SerializeField] private float moveAcceleration = 8f;

    [Tooltip("Camera movement deceleration")]
    [SerializeField] private float moveDeceleration = 12f;

    [Tooltip("Key sensitivity multiplier for movement")]
    [SerializeField] private float keyboardSensitivity = 1f;

    [Tooltip("Movement speed multiplier when shift is held")]
    [SerializeField] private float fastMoveFactor = 2.5f;

    [Header("Zoom Settings")]
    [Tooltip("Camera zoom speed")]
    [SerializeField] private float zoomSpeed = 0.5f;

    [Tooltip("Zoom acceleration")]
    [SerializeField] private float zoomAcceleration = 15f;

    [Tooltip("Minimum zoom distance (closest to ground)")]
    [SerializeField] private float minZoomDistance = 5f;

    [Tooltip("Maximum zoom distance (furthest from ground)")]
    [SerializeField] private float maxZoomDistance = 30f;

    [Tooltip("Height of the ground plane")]
    [SerializeField] private float groundHeight = 0f;

    [Tooltip("Whether to zoom along camera forward axis or directly up/down")]
    [SerializeField] private bool zoomAlongForwardAxis = true;

    [Header("Rotation Settings")]
    [Tooltip("Camera rotation speed (degrees per second)")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Rotation acceleration")]
    [SerializeField] private float rotationAcceleration = 15f;

    [Tooltip("Rotation deceleration")]
    [SerializeField] private float rotationDeceleration = 20f;

    [Tooltip("Whether to rotate around a focus point or camera position")]
    [SerializeField] private bool rotateAroundFocusPoint = true;

    [Tooltip("Focus point distance from camera for rotation")]
    [SerializeField] private float focusDistance = 10f;

    [Header("Debug")]
    [Tooltip("Show debug information in the console")]
    [SerializeField] private bool showDebugInfo = false;

    #endregion

    #region Private Variables

    private Transform cameraTransform;
    private Vector3 currentVelocity = Vector3.zero;
    private float currentZoomVelocity = 0f;
    private float currentRotationVelocity = 0f;
    private float targetZoomDistance;
    private Vector3 focusPoint = Vector3.zero;
    
    // Input values
    private Vector3 moveInput = Vector3.zero;
    private float zoomInput = 0f;
    private float rotationInput = 0f;
    
    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        cameraTransform = transform;
        
        // Find map bounds if not manually assigned
        if (mapBounds == null)
        {
            mapBounds = FindFirstObjectByType<MapBounds>();
            if (mapBounds == null && showDebugInfo)
            {
                Debug.LogWarning("MapBounds component not found! Camera bounds will not be enforced.");
            }
        }
        
        // Set initial zoom distance
        targetZoomDistance = Mathf.Clamp(
            cameraTransform.position.y - groundHeight,
            minZoomDistance,
            maxZoomDistance);
    }

    private void Update()
    {
        GetInputs();
        ProcessMovement();
        ProcessZoom();
        ProcessRotation();
        
        // Update focus point for rotation around target
        if (rotateAroundFocusPoint)
        {
            UpdateFocusPoint();
        }
    }

    #endregion

    #region Input Handling
    
    /// <summary>
    /// Gathers all input for camera control
    /// </summary>
    private void GetInputs()
    {
        // Get keyboard movement input
        Vector2 keyboardInput = Vector2.zero;
        
        // Use Keyboard.current for direct key checks
        if (Keyboard.current != null)
        {
            // Horizontal movement (A/D or Left/Right)
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                keyboardInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                keyboardInput.x += 1f;
                
            // Vertical movement (W/S or Up/Down)
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                keyboardInput.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                keyboardInput.y -= 1f;
                
            // Normalize if we're moving diagonally
            if (keyboardInput.sqrMagnitude > 1f)
                keyboardInput.Normalize();
                
            // Apply sensitivity
            keyboardInput *= keyboardSensitivity;
            
            // Fast movement with shift
            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                keyboardInput *= fastMoveFactor;
                
            // Set the move input
            moveInput = new Vector3(keyboardInput.x, 0f, keyboardInput.y);
        }
        
        // Zoom input from mouse wheel
        if (Mouse.current != null)
        {
            // Read mouse scroll value and scale it down significantly
            float scrollValue = Mouse.current.scroll.y.ReadValue() * 0.0001f;
            
            // Apply additional dampening and invert direction (negative becomes positive)
            zoomInput = -scrollValue; // Inverted so scroll up = zoom in, scroll down = zoom out
            
            if (showDebugInfo && Mathf.Abs(zoomInput) > 0.00001f)
            {
                Debug.Log($"Zoom input: {zoomInput}");
            }
        }
        
        // Rotation input from Q/E keys
        rotationInput = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.isPressed)
                rotationInput += 1f;
            if (Keyboard.current.eKey.isPressed)
                rotationInput -= 1f;
        }
    }
    
    #endregion

    #region Camera Movement
    
    /// <summary>
    /// Processes camera movement based on input
    /// </summary>
    private void ProcessMovement()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // Transform input to camera space (ignoring Y)
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
            
            // Calculate movement direction in world space
            Vector3 targetVelocity = (right * moveInput.x + forward * moveInput.z) * moveSpeed;
            
            // Apply acceleration
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, moveAcceleration * Time.deltaTime);
        }
        else
        {
            // Apply deceleration when no input
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, moveDeceleration * Time.deltaTime);
        }
        
        // Apply movement if we have any velocity
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            // Calculate new position
            Vector3 newPosition = cameraTransform.position + currentVelocity * Time.deltaTime;
            
            // Check if new position is within bounds
            if (mapBounds == null || mapBounds.IsPositionInBounds(newPosition))
            {
                cameraTransform.position = newPosition;
            }
            else
            {
                // If we're out of bounds, try to move only in the valid direction
                Vector3 xOnlyMove = cameraTransform.position + new Vector3(currentVelocity.x, 0, 0) * Time.deltaTime;
                Vector3 zOnlyMove = cameraTransform.position + new Vector3(0, 0, currentVelocity.z) * Time.deltaTime;
                
                if (mapBounds == null || mapBounds.IsPositionInBounds(xOnlyMove))
                {
                    cameraTransform.position = xOnlyMove;
                }
                
                if (mapBounds == null || mapBounds.IsPositionInBounds(zOnlyMove))
                {
                    cameraTransform.position = zOnlyMove;
                }
                
                // Zero out velocity to prevent oscillation
                currentVelocity = Vector3.zero;
            }
        }
    }
    
    #endregion

    #region Camera Zoom
    
    /// <summary>
    /// Processes camera zoom based on input
    /// </summary>
    private void ProcessZoom()
    {
        if (Mathf.Abs(zoomInput) > 0.00001f)
        {
            // Calculate target zoom
            float zoomAmount = zoomInput * zoomSpeed * 100f;
            float newTargetZoom = Mathf.Clamp(targetZoomDistance - zoomAmount, minZoomDistance, maxZoomDistance);
            
            if (showDebugInfo)
            {
                Debug.Log($"Zoom: input={zoomInput}, amount={zoomAmount}, new target={newTargetZoom}, current={targetZoomDistance}");
            }
            
            // Apply only if within bounds
            Vector3 zoomDirection = zoomAlongForwardAxis ? cameraTransform.forward : Vector3.up;
            Vector3 newPosition = cameraTransform.position + zoomDirection * (newTargetZoom - targetZoomDistance);
            
            if (mapBounds == null || mapBounds.IsPositionInBounds(newPosition)) 
            {
                targetZoomDistance = newTargetZoom;
                
                // Simple direct application for mouse wheel (smoother)
                if (zoomAlongForwardAxis)
                {
                    cameraTransform.position = newPosition;
                }
                else
                {
                    // Vertical-only zoom
                    newPosition = cameraTransform.position;
                    newPosition.y = groundHeight + targetZoomDistance;
                    
                    // Make sure we don't go below ground
                    if (newPosition.y >= groundHeight + minZoomDistance)
                    {
                        cameraTransform.position = newPosition;
                    }
                }
            }
            
            // Reset input after processing to prevent continuous zooming
            zoomInput = 0f;
        }
    }
    
    #endregion

    #region Camera Rotation
    
    /// <summary>
    /// Updates the focus point for rotation
    /// </summary>
    private void UpdateFocusPoint()
    {
        // Cast a ray downward to find the ground
        if (Physics.Raycast(cameraTransform.position, Vector3.down, out RaycastHit hit))
        {
            // Use hit point as focus
            focusPoint = hit.point;
        }
        else
        {
            // Fallback to a point in front of the camera
            focusPoint = cameraTransform.position + cameraTransform.forward * focusDistance;
            focusPoint.y = groundHeight;
        }
    }
    
    /// <summary>
    /// Processes camera rotation based on input
    /// </summary>
    private void ProcessRotation()
    {
        if (Mathf.Abs(rotationInput) > 0.01f)
        {
            // Calculate target rotation velocity
            float targetRotationVelocity = rotationInput * rotationSpeed;
            
            // Apply acceleration
            currentRotationVelocity = Mathf.Lerp(
                currentRotationVelocity, 
                targetRotationVelocity, 
                rotationAcceleration * Time.deltaTime);
        }
        else
        {
            // Apply deceleration when no input
            currentRotationVelocity = Mathf.Lerp(
                currentRotationVelocity, 
                0f, 
                rotationDeceleration * Time.deltaTime);
        }
        
        // Apply rotation if we have any velocity
        if (Mathf.Abs(currentRotationVelocity) > 0.01f)
        {
            float rotationAmount = currentRotationVelocity * Time.deltaTime;
            
            if (rotateAroundFocusPoint)
            {
                // Rotate around focus point
                cameraTransform.RotateAround(focusPoint, Vector3.up, rotationAmount);
            }
            else
            {
                // Rotate around self
                cameraTransform.Rotate(Vector3.up, rotationAmount, Space.World);
            }
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Smoothly transitions the camera to a new position
    /// </summary>
    /// <param name="targetPosition">Position to move to</param>
    /// <param name="duration">Time in seconds for the transition</param>
    public void MoveCameraTo(Vector3 targetPosition, float duration = 1.0f)
    {
        StartCoroutine(MoveCameraToCoroutine(targetPosition, duration));
    }
    
    /// <summary>
    /// Coroutine to smoothly move camera to a target position
    /// </summary>
    private IEnumerator MoveCameraToCoroutine(Vector3 targetPosition, float duration)
    {
        // Ensure target is within bounds
        if (mapBounds != null && !mapBounds.IsPositionInBounds(targetPosition))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("Target position is outside camera bounds!");
            }
            yield break;
        }
        
        Vector3 startPosition = cameraTransform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Calculate interpolation factor
            float t = elapsed / duration;
            
            // Smooth step for easing
            t = t * t * (3f - 2f * t);
            
            // Update position
            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Update time
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we arrive exactly at the target
        cameraTransform.position = targetPosition;
    }
    
    /// <summary>
    /// Sets the camera zoom to a specific distance
    /// </summary>
    /// <param name="zoomDistance">Target zoom distance</param>
    public void SetZoomDistance(float zoomDistance)
    {
        targetZoomDistance = Mathf.Clamp(zoomDistance, minZoomDistance, maxZoomDistance);
    }
    
    /// <summary>
    /// Sets the camera's rotation to face a specific point
    /// </summary>
    /// <param name="lookAtPosition">The position to look at</param>
    public void LookAtPosition(Vector3 lookAtPosition)
    {
        // Calculate direction to target (only XZ plane)
        Vector3 direction = lookAtPosition - cameraTransform.position;
        
        // If direction is too small, do nothing
        if (direction.sqrMagnitude < 0.001f)
            return;
        
        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        // Preserve camera's existing pitch (X rotation)
        float currentPitch = cameraTransform.rotation.eulerAngles.x;
        Vector3 targetEuler = targetRotation.eulerAngles;
        targetEuler.x = currentPitch;
        targetRotation = Quaternion.Euler(targetEuler);
        
        // Apply rotation
        cameraTransform.rotation = targetRotation;
        
        // Update focus point if needed
        if (rotateAroundFocusPoint)
        {
            UpdateFocusPoint();
        }
    }
    
    #endregion
}