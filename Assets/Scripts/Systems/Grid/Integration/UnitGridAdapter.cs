using UnityEngine;
using System.Collections.Generic;
using System;

namespace DarkProtocol.Grid
{
    /// <summary>
    /// Adapter component to integrate existing units with the new data-driven grid system
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public class UnitGridAdapter : MonoBehaviour
    {
        #region Inspector Fields
        [Tooltip("Whether the unit should automatically register with the grid system on start")]
        [SerializeField] private bool autoRegisterWithGrid = true;
        
        [Tooltip("Offset from the grid cell center when positioning the unit")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0, 0.01f, 0);
        
        [Tooltip("Whether to snap movement to grid cells")]
        [SerializeField] private bool snapMovementToGrid = true;
        
        [Header("Movement")]
        [Tooltip("Movement speed between grid cells")]
        [SerializeField] private float movementSpeed = 5f;
        
        [Tooltip("Height of the jump arc during movement")]
        [SerializeField] private float jumpHeight = 0.5f;
        
        [Tooltip("Whether to use curved paths for movement")]
        [SerializeField] private bool useCurvedPaths = true;
        #endregion

        #region Private Variables
        private Unit _unit;
        private Vector2Int _currentGridPosition;
        private bool _isMoving = false;
        private List<Vector3> _movementPath = new List<Vector3>();
        private int _currentPathIndex = 0;
        private float _movementStartTime;
        private float _movementDuration;
        private Vector3 _movementStartPosition;
        private Vector3 _movementTargetPosition;
        private bool _isRegisteredWithGrid = false;
        #endregion

        #region Properties
        /// <summary>
        /// The current grid position of the unit
        /// </summary>
        public Vector2Int CurrentGridPosition => _currentGridPosition;
        
        /// <summary>
        /// Whether the unit is currently moving
        /// </summary>
        public bool IsMoving => _isMoving;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _unit = GetComponent<Unit>();
        }
        
        private void Start()
        {
            if (autoRegisterWithGrid)
            {
                RegisterWithGrid();
            }
        }
        
        private void Update()
        {
            // Handle movement
            if (_isMoving)
            {
                UpdateMovement();
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from grid when destroyed
            if (_isRegisteredWithGrid && GridManager.Instance != null)
            {
                GridManager.Instance.SetTileOccupied(_currentGridPosition.x, _currentGridPosition.y, false);
            }
        }
        #endregion

        #region Grid Registration
        /// <summary>
        /// Register the unit with the grid system
        /// </summary>
        public void RegisterWithGrid()
        {
            if (GridManager.Instance == null)
            {
                Debug.LogWarning($"Cannot register unit {GetUnitDisplayName()} with grid - GridManager not found");
                return;
            }
            
            // Get current grid position
            Vector3 unitPosition = transform.position;
            if (GridManager.Instance.WorldToGridPosition(unitPosition, out int x, out int z))
            {
                _currentGridPosition = new Vector2Int(x, z);
                
                // Set tile as occupied
                GridManager.Instance.SetTileOccupied(x, z, true, gameObject);
                
                // Snap position to grid (with offset)
                if (snapMovementToGrid)
                {
                    Vector3 gridPos = GridManager.Instance.GridToWorldPosition(x, z);
                    transform.position = gridPos + positionOffset;
                }
                
                _isRegisteredWithGrid = true;
                
                Debug.Log($"Unit {GetUnitDisplayName()} registered at grid position ({x}, {z})");
            }
            else
            {
                Debug.LogWarning($"Unit {GetUnitDisplayName()} is not on a valid grid position");
            }
        }
        
        /// <summary>
        /// Unregister the unit from the grid system
        /// </summary>
        public void UnregisterFromGrid()
        {
            if (GridManager.Instance == null || !_isRegisteredWithGrid)
                return;
                
            // Clear tile occupancy
            GridManager.Instance.SetTileOccupied(_currentGridPosition.x, _currentGridPosition.y, false);
            
            _isRegisteredWithGrid = false;
        }
        #endregion

        #region Movement
        /// <summary>
        /// Move to a specific grid position
        /// </summary>
        public bool MoveToGridPosition(Vector2Int targetPosition)
        {
            if (_isMoving)
            {
                Debug.LogWarning($"Unit {GetUnitDisplayName()} is already moving");
                return false;
            }
            
            if (GridManager.Instance == null)
            {
                Debug.LogWarning($"Cannot move unit {GetUnitDisplayName()} - GridManager not found");
                return false;
            }
            
            // Check if the target position is valid
            if (!GridManager.Instance.IsValidPosition(targetPosition.x, targetPosition.y))
            {
                Debug.LogWarning($"Invalid target position: ({targetPosition.x}, {targetPosition.y})");
                return false;
            }
            
            // Find path to the target
            List<Vector2Int> path = GridManager.Instance.FindPath(_currentGridPosition, targetPosition);
            
            if (path == null || path.Count <= 1)
            {
                Debug.LogWarning($"No valid path to target position");
                return false;
            }
            
            // Convert path to world positions
            _movementPath.Clear();
            foreach (Vector2Int pos in path)
            {
                Vector3 worldPos = GridManager.Instance.GridToWorldPosition(pos);
                worldPos += positionOffset; // Add offset
                _movementPath.Add(worldPos);
            }
            
            // Initiate movement
            StartMovement();
            
            return true;
        }
        
        /// <summary>
        /// Start movement along the calculated path
        /// </summary>
        private void StartMovement()
        {
            if (_movementPath.Count < 2)
                return;
                
            // Set initial movement params
            _isMoving = true;
            _currentPathIndex = 1; // Start moving to the second point (index 1)
            _movementStartPosition = transform.position;
            _movementTargetPosition = _movementPath[_currentPathIndex];
            _movementStartTime = Time.time;
            
            // Calculate duration based on distance and speed
            float distance = Vector3.Distance(_movementStartPosition, _movementTargetPosition);
            _movementDuration = distance / movementSpeed;
            
            // Update grid occupancy
            if (_isRegisteredWithGrid && GridManager.Instance != null)
            {
                // Remove occupancy from current position
                GridManager.Instance.SetTileOccupied(_currentGridPosition.x, _currentGridPosition.y, false);
                
                // We'll set the new occupancy when the movement is complete
            }
        }
        
        /// <summary>
        /// Update movement along the path
        /// </summary>
        private void UpdateMovement()
        {
            // Calculate progress
            float elapsedTime = Time.time - _movementStartTime;
            float progress = Mathf.Clamp01(elapsedTime / _movementDuration);
            
            // Move towards target
            if (useCurvedPaths)
            {
                // Use a jump arc for more natural movement
                Vector3 position = Vector3.Lerp(_movementStartPosition, _movementTargetPosition, progress);
                
                // Add a vertical arc
                float jumpProgress = Mathf.Sin(progress * Mathf.PI);
                position.y += jumpHeight * jumpProgress;
                
                transform.position = position;
                
                // Rotate to face movement direction
                if (progress < 0.9f)
                {
                    Vector3 direction = _movementTargetPosition - _movementStartPosition;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        direction.y = 0; // Keep upright
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                    }
                }
            }
            else
            {
                // Simple linear movement
                transform.position = Vector3.Lerp(_movementStartPosition, _movementTargetPosition, progress);
            }
            
            // Check if we've reached the target
            if (progress >= 1.0f)
            {
                // Move to next point in path
                _currentPathIndex++;
                
                if (_currentPathIndex < _movementPath.Count)
                {
                    // Continue to next point
                    _movementStartPosition = transform.position;
                    _movementTargetPosition = _movementPath[_currentPathIndex];
                    _movementStartTime = Time.time;
                    
                    // Calculate new duration
                    float distance = Vector3.Distance(_movementStartPosition, _movementTargetPosition);
                    _movementDuration = distance / movementSpeed;
                }
                else
                {
                    // Reached the end of the path
                    CompleteMovement();
                }
            }
        }
        
        /// <summary>
        /// Complete the movement
        /// </summary>
        private void CompleteMovement()
        {
            _isMoving = false;
            
            // Update grid position
            if (GridManager.Instance != null)
            {
                // Get the new grid position
                GridManager.Instance.WorldToGridPosition(transform.position, out int x, out int z);
                _currentGridPosition = new Vector2Int(x, z);
                
                // Set the new occupancy
                GridManager.Instance.SetTileOccupied(x, z, true, gameObject);
            }
            
            // Trigger any movement completion events
            // For example, notify the Unit that movement is complete
            if (_unit != null)
            {
                // We could call a method on the Unit to indicate movement is complete
                // This would be useful for animation transitions, etc.
            }
        }
        #endregion

        #region Unit Movement Override
        /// <summary>
        /// Handle movement requests from the Unit class
        /// </summary>
        public bool HandleMoveRequest(Vector3 targetPosition, int movementCost)
        {
            if (_unit == null || GridManager.Instance == null)
                return false;
            
            // Convert target world position to grid position
            if (GridManager.Instance.WorldToGridPosition(targetPosition, out Vector2Int gridPos))
            {
                // Attempt to move on the grid
                bool success = MoveToGridPosition(gridPos);
                
                if (success)
                {
                    // Try to spend the movement points (if appropriate Unit method exists)
                    TrySpendMovementPoints(movementCost);
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Attempts to call SpendMovementPoints on the Unit if it exists
        /// </summary>
        private void TrySpendMovementPoints(int cost)
        {
            // We'll use reflection to safely check if the method exists
            var method = _unit.GetType().GetMethod("SpendMovementPoints");
            if (method != null)
            {
                method.Invoke(_unit, new object[] { cost });
            }
        }
        
        /// <summary>
        /// Get a display name for the unit, safely handling if UnitName doesn't exist
        /// </summary>
        private string GetUnitDisplayName()
        {
            if (_unit == null)
                return "Unknown Unit";
                
            // Try to get UnitName property
            var nameProperty = _unit.GetType().GetProperty("UnitName");
            if (nameProperty != null)
            {
                return nameProperty.GetValue(_unit, null)?.ToString() ?? "Unnamed Unit";
            }
            
            // Fallback to GameObject name
            return gameObject.name;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Get the world position for a specific grid position
        /// </summary>
        public Vector3 GetWorldPositionForGrid(Vector2Int gridPos)
        {
            if (GridManager.Instance == null)
                return Vector3.zero;
                
            Vector3 worldPos = GridManager.Instance.GridToWorldPosition(gridPos);
            return worldPos + positionOffset;
        }
        
        /// <summary>
        /// Move instantly to a grid position (for initialization or teleportation)
        /// </summary>
        public void TeleportToGridPosition(Vector2Int gridPos)
        {
            if (GridManager.Instance == null)
                return;
                
            // Check if the position is valid
            if (!GridManager.Instance.IsValidPosition(gridPos.x, gridPos.y))
                return;
                
            // Update grid occupancy
            if (_isRegisteredWithGrid)
            {
                // Clear old position
                GridManager.Instance.SetTileOccupied(_currentGridPosition.x, _currentGridPosition.y, false);
            }
            
            // Set new position
            _currentGridPosition = gridPos;
            transform.position = GetWorldPositionForGrid(gridPos);
            
            // Update occupancy
            GridManager.Instance.SetTileOccupied(gridPos.x, gridPos.y, true, gameObject);
            _isRegisteredWithGrid = true;
        }
        #endregion
    }
    
    /// <summary>
    /// Extension to integrate with the Unit class to connect to the grid system
    /// Must be added after UnitGridAdapter since it depends on it
    /// </summary>
    [RequireComponent(typeof(UnitGridAdapter))]
    public class UnitMovementOverride : MonoBehaviour
    {
        private Unit _unit;
        private UnitGridAdapter _gridAdapter;
        
        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _gridAdapter = GetComponent<UnitGridAdapter>();
            
            // Add a hook to override the Unit.Move method
            // We'll do this via reflection since we're not sure of the exact
            // signature/properties available on the Unit class
            HookIntoMoveMethod();
        }
        
        /// <summary>
        /// Uses reflection to hook into the Unit.Move method
        /// </summary>
        private void HookIntoMoveMethod()
        {
            if (_unit == null || _gridAdapter == null)
                return;
                
            // Check if Unit has an OnMoveAttempt event we can hook into
            var eventField = _unit.GetType().GetField("OnMoveAttempt",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
            if (eventField != null)
            {
                // Create a delegate to handle the move attempt
                // The exact type of delegate depends on the Unit class definition,
                // so we'll use a common/expected signature
                try
                {
                    // Try to add our handler - this approach might need adjustment
                    // based on the actual Unit implementation
                    var eventType = eventField.FieldType;
                    var method = typeof(UnitGridAdapter).GetMethod("HandleMoveRequest");
                    
                    // Create a delegate from our method
                    var delegateInstance = Delegate.CreateDelegate(eventType, _gridAdapter, method);
                    
                    // Add the delegate to the event
                    var addMethod = eventType.GetMethod("Add");
                    addMethod.Invoke(eventField.GetValue(_unit), new object[] { delegateInstance });
                    
                    Debug.Log("Successfully hooked into Unit.Move method");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to hook into Unit.Move method: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("Unit class does not have an OnMoveAttempt event, movement override will not work");
            }
        }
        
        /// <summary>
        /// Manual override for the Unit.Move method if hooking doesn't work
        /// </summary>
        public bool Move(Vector3 targetPosition, int movementCost = 1)
        {
            if (_gridAdapter != null)
            {
                return _gridAdapter.HandleMoveRequest(targetPosition, movementCost);
            }
            
            // Fallback to standard movement if adapter not available
            transform.position = targetPosition;
            Debug.Log($"Unit moved to {targetPosition}");
            
            return true;
        }
    }
}