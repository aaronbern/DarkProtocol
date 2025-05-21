using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using DarkProtocol.Cards;
using DarkProtocol.Grid;

/// <summary>
/// Controls unit selection for Dark Protocol's turn-based system.
/// Manages which unit is selected for the current player round.
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
                _instance = FindFirstObjectByType<UnitSelectionController>();
                
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
    
    // Reference to the Card System
    private CardSystem _cardSystem;
    
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
        
        // Get Card System reference
                    _cardSystem = FindFirstObjectByType<CardSystem>();
        
        if (_cardSystem == null)
        {
            Debug.LogWarning("CardSystem not found! Card functionality will be limited.");
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
        if (_selectionEnabled)
        {
            if (showDebugInfo)
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
            // Make sure selection is disabled and cleanup any active unit
            _selectionEnabled = false;
            
            // Clear any card hand if a unit is selected
            if (CurrentlySelectedUnit != null && _cardSystem != null)
            {
                _cardSystem.DiscardHand(CurrentlySelectedUnit);
                
                // Deselect the unit
                Unit.SelectUnit(null);
            }
            
            // Clear the list of units that acted this round
            _unitsActedThisRound.Clear();
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
            if (showDebugInfo)
                Debug.LogWarning("No available units to select!");
                
            OnNoUnitsAvailable?.Invoke();
            
            // Since no units are available, suggest ending the turn
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndPlayerTurn();
            }
            
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
            if (showDebugInfo)
                Debug.Log($"Please select a unit. {_availableUnits.Count} units available.");
        }
    }
    
    /// <summary>
    /// Called when a unit has completed its turn
    /// </summary>
    public void OnUnitFinishedTurn(Unit unit)
    {
        if (unit != null)
        {
            if (showDebugInfo)
                Debug.Log($"Unit {unit.UnitName} finished turn");
                
            // Add to the list of units that have acted this round
            if (!_unitsActedThisRound.Contains(unit))
            {
                _unitsActedThisRound.Add(unit);
            }
            
            // Clear any card hand for this unit
            if (_cardSystem != null)
            {
                _cardSystem.DiscardHand(unit);
            }
            
            // Deselect the unit if it's the currently selected one
            if (CurrentlySelectedUnit == unit)
            {
                Unit.SelectUnit(null);
                
                // Notify the grid about unit deselection
                if (GridManager.Instance != null)
                {
                    // Passing null to clear selection state
                    GridManager.Instance.OnUnitSelected(null);
                }
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
                if (showDebugInfo)
                    Debug.Log($"Please select another unit. {_availableUnits.Count} units remaining.");
            }
        }
        else
        {
            // No more units can act this round, suggest ending the turn
            if (showDebugInfo)
                Debug.Log("All units have acted this round. Ending player turn.");
                
            // Automatically end the turn since all units have acted
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndPlayerTurn();
            }
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
            if (!_unitsActedThisRound.Contains(unit) && unit.IsAlive)
            {
                _availableUnits.Add(unit);
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"Available units: {_availableUnits.Count}");
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

        // Make sure selection is enabled
        if (!_selectionEnabled)
        {
            Debug.LogWarning("Unit selection is currently disabled!");
            return;
        }

        // If there's currently a selected unit, make sure to discard its hand first
        if (CurrentlySelectedUnit != null && _cardSystem != null)
        {
            _cardSystem.DiscardHand(CurrentlySelectedUnit);
        }

        // Use the Unit's static selection method
        Unit.SelectUnit(unit);

        // Notify any listeners
        OnUnitSelected?.Invoke(unit);

        // Also notify the grid manager
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnUnitSelected(unit);
        }

        if (showDebugInfo)
            Debug.Log($"Selected {unit.UnitName} for this turn.");

        // REMOVED: Start the unit's turn
        // unit.StartTurn();

        // REMOVED: Draw cards for this unit
        // if (_cardSystem != null)
        // {
        //     _cardSystem.DrawHandForUnit(unit);
        // }
    }

    /// <summary>
    /// Helper method for debug/testing - cycles to the next available unit
    /// </summary>
    private void CycleToNextAvailableUnit()
    {
        if (_availableUnits.Count == 0) return;
        
        // Find the current unit's index
        int currentIndex = -1;
        if (CurrentlySelectedUnit != null)
        {
            currentIndex = _availableUnits.IndexOf(CurrentlySelectedUnit);
        }
        
        // Select the next unit in the list
        int nextIndex = (currentIndex + 1) % _availableUnits.Count;
        SelectUnit(_availableUnits[nextIndex]);
    }

    /// <summary>
    /// Handles unit clicks for selection (only enabled during selection phase)
    /// </summary>
    public void HandleUnitClicked(Unit clickedUnit)
    {
        if (showDebugInfo)
            Debug.Log($"HandleUnitClicked called for {clickedUnit?.UnitName ?? "null"}");

        // Validate that the unit can be selected
        if (!_selectionEnabled)
        {
            Debug.Log("Selection is disabled - unit is currently active or it's not player turn");
            return;
        }

        // Verify the unit is valid and available
        if (clickedUnit != null && clickedUnit.Team == Unit.TeamType.Player &&
            clickedUnit.IsAlive && !_unitsActedThisRound.Contains(clickedUnit))
        {
            if (showDebugInfo)
                Debug.Log($"Unit {clickedUnit.UnitName} is valid for selection");

            // Select the unit via the Unit system
            SelectUnit(clickedUnit);
        }
        else
        {
            // Debug why the unit can't be selected
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

    /// <summary>
    /// Mark a unit as ready to act again (useful for abilities that grant extra actions)
    /// </summary>
    public void ResetUnitAction(Unit unit)
    {
        if (unit != null && _unitsActedThisRound.Contains(unit))
        {
            _unitsActedThisRound.Remove(unit);
            RefreshAvailableUnits();
            
            if (showDebugInfo)
                Debug.Log($"Reset action for {unit.UnitName} - they can act again this round");
        }
    }
    
    /// <summary>
    /// End the active unit's turn programmatically (for UI buttons, etc.)
    /// </summary>
    public void EndActiveUnitTurn()
    {
        if (CurrentlySelectedUnit != null)
        {
            // End the unit's turn
            CurrentlySelectedUnit.EndTurn();
            
            // Process the end of turn
            OnUnitFinishedTurn(CurrentlySelectedUnit);
        }
    }
    
    #endregion

    #region Unit Management
    
    /// <summary>
    /// Get all units that have acted this round
    /// </summary>
    public List<Unit> GetUnitsActedThisRound()
    {
        return new List<Unit>(_unitsActedThisRound);
    }
    
    /// <summary>
    /// Get all units that haven't acted this round yet
    /// </summary>
    public List<Unit> GetAvailableUnits()
    {
        return new List<Unit>(_availableUnits);
    }
    
    /// <summary>
    /// Check if a specific unit has already acted this round
    /// </summary>
    public bool HasUnitActed(Unit unit)
    {
        return unit != null && _unitsActedThisRound.Contains(unit);
    }
    
    /// <summary>
    /// Check if all player units have acted this round
    /// </summary>
    public bool HaveAllUnitsActed()
    {
        List<Unit> playerUnits = Unit.GetUnitsOfTeam(Unit.TeamType.Player);
        
        // Check if all living player units have acted
        foreach (Unit unit in playerUnits)
        {
            if (unit.IsAlive && !_unitsActedThisRound.Contains(unit))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Check if selection is currently enabled
    /// </summary>
    public bool IsSelectionEnabled()
    {
        return _selectionEnabled;
    }
    
    #endregion
}