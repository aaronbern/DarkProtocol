using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player input for clicking on units.
/// Separate from the UnitSelectionController for clean architecture.
/// Uses the new Input System.
/// </summary>
public class UnitInputHandler : MonoBehaviour
{
    #region Inspector Fields
    
    [Tooltip("Which layers should be checked for units")]
    [SerializeField] private LayerMask unitLayerMask = Physics.DefaultRaycastLayers;
    
    [Tooltip("Maximum distance for the raycast")]
    [SerializeField] private float maxSelectionDistance = 100f;
    
    [Tooltip("Reference to the Input Action Reference for clicking")]
    [SerializeField] private InputActionReference clickActionReference;
    
    #endregion

    #region Private Variables
    
    private Camera _mainCamera;
    private InputAction _clickAction;
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        // Get reference to the click action
        if (clickActionReference != null)
        {
            _clickAction = clickActionReference.action;
        }
        else
        {
            Debug.LogError("Click Action Reference not assigned in inspector!");
        }
    }
    
    private void OnEnable()
    {
        if (_clickAction != null)
        {
            _clickAction.performed += OnMouseClick;
            _clickAction.Enable();
        }
    }
    
    private void OnDisable()
    {
        if (_clickAction != null)
        {
            _clickAction.performed -= OnMouseClick;
            _clickAction.Disable();
        }
    }
    
    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (_mainCamera == null)
        {
            Debug.LogError("No main camera found! UnitInputHandler requires a camera tagged as 'MainCamera'.");
        }
    }
    
    #endregion

    #region Input Handling
    
    /// <summary>
    /// Called when the mouse is clicked
    /// </summary>
    private void OnMouseClick(InputAction.CallbackContext context)
    {
        // Only process input during player turn
        if (GameManager.Instance != null && !GameManager.Instance.IsPlayerTurn())
        {
            return;
        }
        
        Debug.Log("Mouse click detected via Input System");
        HandleMouseClick();
    }
    
    /// <summary>
    /// Processes mouse clicks to detect unit selection
    /// </summary>
    private void HandleMouseClick()
    {
        // Get mouse position from the Input System
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        
        Debug.Log($"Casting ray from mouse position: {mousePosition}");
        
        if (Physics.Raycast(ray, out hit, maxSelectionDistance, unitLayerMask))
        {
            Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
            
            // Check if we hit a unit
            Unit clickedUnit = hit.collider.GetComponent<Unit>();
            
            if (clickedUnit != null)
            {
                Debug.Log($"Clicked on unit: {clickedUnit.UnitName}");
                 
                 
                // Forward the click to the UnitSelectionController
                if (UnitSelectionController.Instance != null)
                {
                    UnitSelectionController.Instance.HandleUnitClicked(clickedUnit);
                    
                    // ADDED: Also notify GridManager directly
                    if (DarkProtocol.Grid.GridManager.Instance != null)
                    {
                        Debug.Log($"Directly notifying GridManager about unit selection: {clickedUnit.UnitName}");
                        DarkProtocol.Grid.GridManager.Instance.OnUnitSelected(clickedUnit);
                    }
                    else
                    {
                        Debug.LogError("GridManager.Instance is null! Make sure it exists in the scene.");
                    }
                }
                else
                {
                    Debug.LogError("UnitSelectionController.Instance is null!");
                }
            }
            else
            {
                Debug.Log("No Unit component on clicked object");
            }
        }
        else
        {
            Debug.Log("Raycast did not hit anything");
        }
    }
    
    #endregion
}