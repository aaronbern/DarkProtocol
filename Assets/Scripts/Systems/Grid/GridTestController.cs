using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple test controller for demonstrating and testing the grid system.
/// </summary>
public class GridTestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitTilePlacer unitPlacer;
    
    [Header("Input")]
    [Tooltip("Reference to the place unit action")]
    [SerializeField] private InputActionReference placeUnitActionReference;
    
    [Header("Test Settings")]
    [SerializeField] private bool placePlayerUnitOnStart = true;
    [SerializeField] private bool placeEnemyUnitOnStart = true;
    [SerializeField] private Vector2Int playerStartPosition = new Vector2Int(2, 2);
    [SerializeField] private Vector2Int enemyStartPosition = new Vector2Int(7, 7);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Input action for placing units
    private InputAction placeUnitAction;
    
    private void Awake()
    {
        // Setup input action if reference is provided
        if (placeUnitActionReference != null)
        {
            placeUnitAction = placeUnitActionReference.action;
        }
        else
        {
            // Create a fallback action if no reference is provided
            placeUnitAction = new InputAction("PlaceUnit", InputActionType.Button);
            placeUnitAction.AddBinding("<Keyboard>/p");
            
            if (showDebugInfo)
            {
                Debug.Log("No place unit action reference provided. Using default 'P' key binding.");
            }
        }
    }
    
    private void OnEnable()
    {
        // Enable the input action and subscribe to it
        if (placeUnitAction != null)
        {
            placeUnitAction.performed += OnPlaceUnitAction;
            placeUnitAction.Enable();
        }
    }
    
    private void OnDisable()
    {
        // Disable the input action and unsubscribe
        if (placeUnitAction != null)
        {
            placeUnitAction.performed -= OnPlaceUnitAction;
            placeUnitAction.Disable();
        }
    }
    
    private void Start()
    {
        // Find references if not assigned
        if (gridManager == null)
        {
            gridManager = GetComponent<GridManager>();
            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<GridManager>();
            }
        }
        
        if (unitPlacer == null)
        {
            unitPlacer = GetComponent<UnitTilePlacer>();
            if (unitPlacer == null)
            {
                unitPlacer = FindAnyObjectByType<UnitTilePlacer>();
            }
        }
        
        // Check for missing components
        if (gridManager == null)
        {
            Debug.LogError("GridManager reference is missing. Please assign it in the inspector or add it to this GameObject.");
            enabled = false;
            return;
        }
        
        if (unitPlacer == null)
        {
            Debug.LogWarning("UnitTilePlacer reference is missing. Unit placement will not work.");
        }
        
        // Delay initial unit placement to ensure grid is initialized
        Invoke("PlaceInitialUnits", 0.2f);
    }
    
    /// <summary>
    /// Callback for the place unit input action
    /// </summary>
    private void OnPlaceUnitAction(InputAction.CallbackContext context)
    {
        if (unitPlacer != null)
        {
            PlaceUnitAtMousePosition();
        }
    }
    
    /// <summary>
    /// Places initial test units on the grid
    /// </summary>
    private void PlaceInitialUnits()
    {
        if (unitPlacer == null)
            return;
            
        if (placePlayerUnitOnStart)
        {
            Unit playerUnit = unitPlacer.PlaceUnitAt(playerStartPosition.x, playerStartPosition.y);
            if (playerUnit != null && showDebugInfo)
            {
                Debug.Log($"Placed player unit at ({playerStartPosition.x}, {playerStartPosition.y})");
            }
        }
        
        if (placeEnemyUnitOnStart)
        {
            Unit enemyUnit = unitPlacer.PlaceUnitAt(enemyStartPosition.x, enemyStartPosition.y);
            
            if (enemyUnit != null)
            {
                // Note: If you have a method to set the team, uncomment this
                // enemyUnit.SetTeam(Unit.TeamType.Enemy);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Placed enemy unit at ({enemyStartPosition.x}, {enemyStartPosition.y})");
                }
            }
        }
    }
    
    /// <summary>
    /// Places a unit at the current mouse position
    /// </summary>
    private void PlaceUnitAtMousePosition()
    {
        // Get the current mouse position
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        
        // Create a ray from the camera through the mouse position
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Unit placedUnit = unitPlacer.PlaceUnitAtPosition(hit.point);
            
            if (placedUnit != null && showDebugInfo)
            {
                // Get grid coordinates for better logging
                if (gridManager.WorldToGridPosition(hit.point, out int x, out int y))
                {
                    Debug.Log($"Placed unit at grid coordinates ({x}, {y})");
                }
                else
                {
                    Debug.Log($"Placed unit at world position {hit.point}");
                }
            }
        }
    }
}