using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Controls unit selection for Dark Protocol's turn-based system.
/// Manages which unit is selected for the current player round.
/// Uses the new Input System.
/// </summary>
public class UnitSelectionController : MonoBehaviour
{
    #region Singleton Pattern
    
    private static UnitSelectionController _instance;
    
    /// <summary>
    /// Singleton instance of the UnitSelectionController
    /// </summary>
    public static UnitSelectionController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<UnitSelectionController>();
                
                if (_instance == null)
                {
                    Debug.LogError("No UnitSelectionController found in scene. Please add one.");
                }
            }
            
            return _instance;
        }
    }
    
    private void Awake()
    {
        // Ensure singleton pattern
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Multiple UnitSelectionControllers found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Get reference to the tab action if assigned
        if (tabActionReference != null)
        {
            _tabAction = tabActionReference.action;
        }
    }
    
    #endregion

    #region Inspector Fields
    
    [Header("Selection Settings")]
    [Tooltip("Which layers should be checked for units")]
    [SerializeField] private LayerMask unitLayerMask = Physics.DefaultRaycastLayers;
    
    [Tooltip("If true, will automatically select the only available unit")]
    [SerializeField] private bool autoSelectSingleUnit = true;
    
    [Header("Input")]
    [Tooltip("Reference to the Tab Key input action")]
    [SerializeField] private InputActionReference tabActionReference;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Events")]
    [SerializeField] private UnityEvent<Unit> OnUnitSelected;
    [SerializeField] private UnityEvent OnNoUnitsAvailable;
    
    [Header("UI References")]
    [Tooltip("Optional prefab for selection visuals")]
    [SerializeField] private GameObject selectionIndicatorPrefab = null;
    
    #endregion

    #region State Variables
    
    private Camera _mainCamera;
    private List<Unit> _availableUnits = new List<Unit>();
    private bool _selectionEnabled = false;
    private List<Unit> _unitsActedThisRound = new List<Unit>();
    private InputAction _tabAction;
    
    // Read-only property for the currently selected unit
    public Unit CurrentlySelectedUnit => Unit.SelectedUnit;
    
    #endregion

    #region Unity Lifecycle
    
    private void OnEnable()
    {
        // Enable the tab action if assigned
        if (_tabAction != null)
        {
            _tabAction.performed += OnTabKeyPressed;
            _tabAction.Enable();
        }
    }
    
    private void OnDisable()
    {
        // Disable the tab action if assigned
        if (_tabAction != null)
        {
            _tabAction.performed -= OnTabKeyPressed;
            _tabAction.Disable();
        }
    }
    
    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (_mainCamera == null)
        {
            Debug.LogError("No main camera found! UnitSelectionController requires a camera tagged as 'MainCamera'.");
        }
        
        // Subscribe to game manager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from game manager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
        }
    }
    
    #endregion

    #region Input Handling
    
    /// <summary>
    /// Called when the Tab key is pressed
    /// </summary>
    private void OnTabKeyPressed(InputAction.CallbackContext context)
    {
        if (showDebugInfo && _selectionEnabled)
        {
            Debug.Log("Tab key pressed, cycling to next unit");
            CycleToNextAvailableUnit();
        }
    }
    
    #endregion

    #region Turn Management
    
    /// <summary>
    /// Handles turn state changes from the GameManager
    /// </summary>
    private void HandleTurnChanged(GameManager.TurnState newState)
    {
        if (newState == GameManager.TurnState.PlayerTurn)
        {
            // Start of player round
            BeginUnitSelection();
        }
        else
        {
            // Any other turn state (enemy turn or transitioning)
            // Make sure selection is disabled
            _selectionEnabled = false;
        }
    }
    
    /// <summary>
    /// Called at the start of the player round to begin unit selection
    /// </summary>
    public void BeginUnitSelection()
    {
        // First, clear units that acted this round at the start of a new player turn
        _unitsActedThisRound.Clear();
        
        // Get all available player units
        RefreshAvailableUnits();
        
        // Enable selection
        _selectionEnabled = true;
        
        if (_availableUnits.Count == 0)
        {
            Debug.LogWarning("No available units to select!");
            OnNoUnitsAvailable?.Invoke();
            return;
        }
        
        // Auto-select if there's only one unit and auto-selection is enabled
        if (_availableUnits.Count == 1 && autoSelectSingleUnit)
        {
            SelectUnit(_availableUnits[0]);
        }
        else
        {
            // Show UI or wait for input to select a unit
            Debug.Log($"Please select a unit. {_availableUnits.Count} units available.");
            
            // If UI is implemented, we would activate it here
            // For now, we'll rely on the player clicking units
        }
    }
    
    /// <summary>
    /// Called when a unit has completed its turn
    /// </summary>
    public void OnUnitFinishedTurn(Unit unit)
    {
        if (unit != null)
        {
            // Add to the list of units that have acted this round
            if (!_unitsActedThisRound.Contains(unit))
            {
                _unitsActedThisRound.Add(unit);
            }
            
            // Deselect the unit
            if (Unit.SelectedUnit == unit)
            {
                Unit.SelectUnit(null);
            }
        }
        
        // Check if there are more units to act this round
        RefreshAvailableUnits();
        
        if (_availableUnits.Count > 0)
        {
            // More units can act this round
            _selectionEnabled = true;
            
            // Auto-select if there's only one unit and auto-selection is enabled
            if (_availableUnits.Count == 1 && autoSelectSingleUnit)
            {
                SelectUnit(_availableUnits[0]);
            }
            else
            {
                // Show UI or wait for input to select another unit
                Debug.Log($"Please select another unit. {_availableUnits.Count} units remaining.");
            }
        }
        else
        {
            // No more units can act this round, suggest ending the turn
            Debug.Log("All units have acted this round. Consider ending your turn.");
            
            // If there's a UI for ending the turn, we would highlight it here
        }
    }
    
    #endregion

    #region Selection Logic
    
    /// <summary>
    /// Refreshes the list of available units that can be selected
    /// </summary>
    public void RefreshAvailableUnits()
    {
        _availableUnits.Clear();
        
        // Get all player units
        List<Unit> playerUnits = Unit.GetUnitsOfTeam(Unit.TeamType.Player);
        
        // Filter out units that have already acted this round
        foreach (Unit unit in playerUnits)
        {
            if (!_unitsActedThisRound.Contains(unit))
            {
                _availableUnits.Add(unit);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Available units: {_availableUnits.Count}");
        }
    }
    
    /// <summary>
    /// Selects a unit for the current player turn
    /// </summary>
    public void SelectUnit(Unit unit)
    {
        // Make sure the unit is valid
        if (unit == null || !unit.IsAlive || unit.Team != Unit.TeamType.Player)
        {
            Debug.LogWarning("Invalid unit selection!");
            return;
        }
        
        // Make sure the unit hasn't already acted this round
        if (_unitsActedThisRound.Contains(unit))
        {
            Debug.LogWarning($"{unit.UnitName} has already acted this round!");
            return;
        }
        
        // Use the Unit's static selection method
        Unit.SelectUnit(unit);
        
        // Disable further selection until this unit's turn is complete
        _selectionEnabled = false;
        
        // Start the unit's turn
        unit.StartTurn();
        
        // Notify any listeners
        OnUnitSelected?.Invoke(unit);
        
        Debug.Log($"Selected {unit.UnitName} for this turn.");
    }
    
    /// <summary>
    /// Helper method for debug/testing - cycles to the next available unit
    /// </summary>
    private void CycleToNextAvailableUnit()
    {
        if (_availableUnits.Count == 0) return;
        
        // Find the current unit's index
        int currentIndex = -1;
        if (Unit.SelectedUnit != null)
        {
            currentIndex = _availableUnits.IndexOf(Unit.SelectedUnit);
        }
        
        // Select the next unit in the list
        int nextIndex = (currentIndex + 1) % _availableUnits.Count;
        SelectUnit(_availableUnits[nextIndex]);
    }
    
    /// <summary>
    /// Handles mouse clicks for unit selection (only enabled during selection phase)
    /// </summary>
    public void HandleUnitClicked(Unit clickedUnit)
    {
        Debug.Log($"HandleUnitClicked called for {clickedUnit?.UnitName ?? "null"}. Selection enabled: {_selectionEnabled}");
        
        // Only process clicks when selection is enabled
        if (!_selectionEnabled) return;
        
        // Verify the unit is valid and available
        if (clickedUnit != null && clickedUnit.Team == Unit.TeamType.Player && 
            clickedUnit.IsAlive && !_unitsActedThisRound.Contains(clickedUnit))
        {
            Debug.Log($"Unit {clickedUnit.UnitName} is valid for selection");
            SelectUnit(clickedUnit);
        }
        else
        {
            if (clickedUnit == null)
            {
                Debug.Log("Clicked unit is null");
            }
            else if (clickedUnit.Team != Unit.TeamType.Player)
            {
                Debug.Log($"Unit {clickedUnit.UnitName} is not on Player team");
            }
            else if (!clickedUnit.IsAlive)
            {
                Debug.Log($"Unit {clickedUnit.UnitName} is not alive");
            }
            else if (_unitsActedThisRound.Contains(clickedUnit))
            {
                Debug.Log($"Unit {clickedUnit.UnitName} has already acted this round");
            }
        }
    }
    
    #endregion
}