using System;
using System.Collections.Generic;
using DarkProtocol.Grid;
using UnityEngine;

/// <summary>
/// Base class for all combat units in Dark Protocol.
/// Handles core stats, action points, and team affiliation.
/// </summary>
public class Unit : MonoBehaviour
{
    #region Enums
    
    /// <summary>
    /// Represents the team affiliation of a unit
    /// </summary>
    public enum TeamType
    {
        Player,
        Enemy,
        Neutral // For potential future use (civilians, etc.)
    }
    
    #endregion

    #region Static Selection Management
    
    // Static collection of all units
    private static List<Unit> allUnits = new List<Unit>();
    
    // Currently selected unit
    private static Unit selectedUnit;
    
    // Last selected unit on each team
    private static Dictionary<TeamType, Unit> lastSelectedByTeam = new Dictionary<TeamType, Unit>();
    
    /// <summary>
    /// Gets the currently selected unit
    /// </summary>
    public static Unit SelectedUnit => selectedUnit;
    
    /// <summary>
    /// Gets all units of a specific team
    /// </summary>
    public static List<Unit> GetUnitsOfTeam(TeamType team)
    {
        List<Unit> result = new List<Unit>();
        
        foreach (Unit unit in allUnits)
        {
            if (unit.Team == team && unit.IsAlive)
            {
                result.Add(unit);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Selects a unit
    /// </summary>
    public static void SelectUnit(Unit unit)
    {
        // Deselect the current unit
        if (selectedUnit != null)
        {
            selectedUnit.OnDeselected();
        }
        
        // Update the selected unit
        selectedUnit = unit;
        
        // If a unit was selected, call its OnSelected method
        if (selectedUnit != null)
        {
            // Remember this unit as the last selected for its team
            lastSelectedByTeam[selectedUnit.Team] = selectedUnit;
            
            // Notify the unit it was selected
            selectedUnit.OnSelected();
        }
    }
    
    /// <summary>
    /// Auto-selects the appropriate unit for the current turn
    /// </summary>
    public static void AutoSelectUnitForCurrentTurn()
    {
        if (GameManager.Instance == null) return;
        
        TeamType teamToSelect = TeamType.Player;
        
        // Determine which team should be selected based on current turn
        if (GameManager.Instance.CurrentTurnState == GameManager.TurnState.PlayerTurn)
        {
            teamToSelect = TeamType.Player;
        }
        else if (GameManager.Instance.CurrentTurnState == GameManager.TurnState.EnemyTurn)
        {
            teamToSelect = TeamType.Enemy;
        }
        else
        {
            return; // Don't select during transition states
        }
        
        // Get all active units of the team
        List<Unit> teamUnits = GetUnitsOfTeam(teamToSelect);
        
        // No units available to select
        if (teamUnits.Count == 0)
        {
            Debug.LogWarning($"No active {teamToSelect} units available to select");
            SelectUnit(null);
            return;
        }
        
        // If there's a last selected unit of this team that's still alive, select it
        if (lastSelectedByTeam.TryGetValue(teamToSelect, out Unit lastUnit) && 
            lastUnit != null && 
            lastUnit.IsAlive && 
            teamUnits.Contains(lastUnit))
        {
            SelectUnit(lastUnit);
            return;
        }
        
        // Otherwise, select the first available unit of the team
        SelectUnit(teamUnits[0]);
    }
    
    #endregion

    #region Stats Properties
    
    [Header("Unit Identity")]
    [SerializeField] private string unitName = "Unnamed Unit";
    [SerializeField] private TeamType team = TeamType.Player;
    
    [Header("Core Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxActionPoints = 3;
    [SerializeField] private int maxMovementPoints = 5;
    
    [Header("Selection")]
    [SerializeField] private GameObject selectionIndicator;
    
    // Current state properties
    private int currentHealth;
    private int currentActionPoints;
    private int currentMovementPoints;
    private bool isSelected = false;
    
    // Read-only public accessors
    public string UnitName => unitName;
    public TeamType Team => team;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int MaxActionPoints => maxActionPoints;
    public int CurrentActionPoints => currentActionPoints;
    public int MaxMovementPoints => maxMovementPoints;
    public int CurrentMovementPoints => currentMovementPoints;
    public bool IsAlive => currentHealth > 0;
    public bool HasActionPoints => currentActionPoints > 0;
    public bool HasMovementPoints => currentMovementPoints > 0;
    public bool IsSelected => isSelected;

    // Add this property to Unit.cs in the Stats Properties region
    public bool HasStartedTurn { get; private set; } = false;

    #endregion

    #region Events

    // Events for other systems to subscribe to
    public event Action<int, int> OnHealthChanged; // (newHealth, oldHealth)
    public event Action<int, int> OnActionPointsChanged; // (newAP, oldAP)
    public event Action<int, int> OnMovementPointsChanged; // (newMP, oldMP)
    public event Action OnUnitDeath;
    public event Action OnTurnStarted;
    public event Action OnTurnEnded;
    public event Action OnUnitSelected;
    public event Action OnUnitDeselected;
    
    #endregion

    #region Unity Lifecycle
    
    protected virtual void Awake()
    {
        // Initialize current values to maximum values
        ResetHealth();
        ResetActionPoints();
        ResetMovementPoints();
        
        // Hide selection indicator initially
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }
    
    protected virtual void OnEnable()
    {
        // Add this unit to the global list
        if (!allUnits.Contains(this))
        {
            allUnits.Add(this);
        }
        
        // Subscribe to game manager events if needed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
        }
    }
    
    protected virtual void OnDisable()
    {
        // Remove this unit from the global list
        allUnits.Remove(this);
        
        // If this unit was selected, deselect it
        if (selectedUnit == this)
        {
            SelectUnit(null);
        }
        
        // Unsubscribe from game manager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
        }
    }
    
    #endregion

    #region Turn Management
    
    /// <summary>
    /// Called when the GameManager changes turns
    /// </summary>
    private void HandleTurnChanged(GameManager.TurnState newState)
    {
        bool isMyTurn = false;
        
        // Determine if it's this unit's turn based on team
        if (team == TeamType.Player && newState == GameManager.TurnState.PlayerTurn)
        {
            isMyTurn = true;
        }
        else if (team == TeamType.Enemy && newState == GameManager.TurnState.EnemyTurn)
        {
            isMyTurn = true;
        }
        
        // If it's this unit's turn, start it
        if (isMyTurn)
        {
            StartTurn();
        }
        
        // Auto-select a unit at the start of each turn
        if ((newState == GameManager.TurnState.PlayerTurn || newState == GameManager.TurnState.EnemyTurn) && 
            selectedUnit == null)
        {
            AutoSelectUnitForCurrentTurn();
        }
    }

    /// <summary>
    /// Prepares the unit for a new turn
    /// </summary>
    public virtual void StartTurn()
    {
        if (!IsAlive)
        {
            Debug.LogWarning($"{unitName} cannot start a turn because they are not alive.");
            return;
        }

        // Reset points for the new turn
        ResetActionPoints();
        ResetMovementPoints();

        // Set the flag that this unit has started its turn
        HasStartedTurn = true;

        // Notify subscribers that the turn has started
        OnTurnStarted?.Invoke();

        Debug.Log($"{unitName} ({team}) is starting their turn with {currentActionPoints} AP and {currentMovementPoints} movement.");
    }

    /// <summary>
    /// Ends this unit's turn
    /// </summary>
    public virtual void EndTurn()
    {
        // Reset the turn flag
        HasStartedTurn = false;

        // Notify subscribers that the turn has ended
        OnTurnEnded?.Invoke();

        Debug.Log($"{unitName} ({team}) has ended their turn.");
    }

    #endregion

    #region Selection Management

    /// <summary>
    /// Called when this unit is selected
    /// </summary>
    protected virtual void OnSelected()
    {
        isSelected = true;
        
        // Show selection indicator if available
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
        
        // Notify subscribers
        OnUnitSelected?.Invoke();
        
        Debug.Log($"{unitName} is now selected");
    }
    
    /// <summary>
    /// Called when this unit is deselected
    /// </summary>
    protected virtual void OnDeselected()
    {
        isSelected = false;
        
        // Hide selection indicator if available
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
        
        // Notify subscribers
        OnUnitDeselected?.Invoke();
        
        Debug.Log($"{unitName} is now deselected");
    }
    
    /// <summary>
    /// Selects this unit
    /// </summary>
    public void Select()
    {
        if (!IsAlive)
        {
            Debug.LogWarning($"Cannot select {unitName} because they are not alive");
            return;
        }
        
        // Use the static method to handle selection
        SelectUnit(this);
    }
    
    /// <summary>
    /// Deselects this unit if it's currently selected
    /// </summary>
    public void Deselect()
    {
        if (selectedUnit == this)
        {
            SelectUnit(null);
        }
    }
    
    #endregion

    #region Action Points Management
    
    /// <summary>
    /// Resets action points to the maximum value
    /// </summary>
    public virtual void ResetActionPoints()
    {
        SetActionPoints(maxActionPoints);
    }
    
    /// <summary>
    /// Sets the action points to a specific value
    /// </summary>
    public virtual void SetActionPoints(int value)
    {
        int oldAP = currentActionPoints;
        currentActionPoints = Mathf.Clamp(value, 0, maxActionPoints);
        
        if (oldAP != currentActionPoints)
        {
            OnActionPointsChanged?.Invoke(currentActionPoints, oldAP);
        }
    }
    
    /// <summary>
    /// Attempts to spend the specified amount of action points
    /// </summary>
    /// <param name="amount">Amount of AP to spend</param>
    /// <returns>True if successful, false if not enough AP</returns>
    public virtual bool SpendActionPoints(int amount)
    {
        // Verify the unit can spend AP
        if (!IsAlive)
        {
            Debug.LogWarning($"{unitName} cannot spend AP because they are not alive.");
            return false;
        }
        
        if (amount <= 0)
        {
            Debug.LogWarning($"Attempted to spend invalid amount of AP: {amount}");
            return false;
        }
        
        // Check if enough AP are available
        if (currentActionPoints < amount)
        {
            Debug.Log($"{unitName} doesn't have enough AP. Current: {currentActionPoints}, Required: {amount}");
            return false;
        }
        
        // Spend the AP
        SetActionPoints(currentActionPoints - amount);
        return true;
    }
    
    /// <summary>
    /// Gain additional action points
    /// </summary>
    public virtual void GainActionPoints(int amount)
    {
        if (amount <= 0) return;
        
        SetActionPoints(currentActionPoints + amount);
    }
    
    #endregion
    
    #region Movement Points Management
    
    /// <summary>
    /// Resets movement points to the maximum value
    /// </summary>
    public virtual void ResetMovementPoints()
    {
        SetMovementPoints(maxMovementPoints);
    }
    
    /// <summary>
    /// Sets the movement points to a specific value
    /// </summary>
    public virtual void SetMovementPoints(int value)
    {
        int oldMP = currentMovementPoints;
        currentMovementPoints = Mathf.Clamp(value, 0, maxMovementPoints);
        
        if (oldMP != currentMovementPoints)
        {
            OnMovementPointsChanged?.Invoke(currentMovementPoints, oldMP);
        }
    }
    
    /// <summary>
    /// Attempts to spend the specified amount of movement points
    /// </summary>
    /// <param name="amount">Amount of movement points to spend</param>
    /// <returns>True if successful, false if not enough movement points</returns>
    public virtual bool SpendMovementPoints(int amount)
    {
        // Verify the unit can move
        if (!IsAlive)
        {
            Debug.LogWarning($"{unitName} cannot move because they are not alive.");
            return false;
        }
        
        if (amount <= 0)
        {
            Debug.LogWarning($"Attempted to spend invalid amount of movement points: {amount}");
            return false;
        }
        
        // Check if enough movement points are available
        if (currentMovementPoints < amount)
        {
            Debug.Log($"{unitName} doesn't have enough movement points. Current: {currentMovementPoints}, Required: {amount}");
            return false;
        }
        
        // Spend the movement points
        SetMovementPoints(currentMovementPoints - amount);
        return true;
    }
    
    /// <summary>
    /// Gain additional movement points
    /// </summary>
    public virtual void GainMovementPoints(int amount)
    {
        if (amount <= 0) return;
        
        SetMovementPoints(currentMovementPoints + amount);
    }
    
    #endregion

    #region Health Management
    
    /// <summary>
    /// Resets health to the maximum value
    /// </summary>
    public virtual void ResetHealth()
    {
        SetHealth(maxHealth);
    }
    
    /// <summary>
    /// Sets the health to a specific value
    /// </summary>
    public virtual void SetHealth(int value)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        
        if (oldHealth != currentHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, oldHealth);
            
            // Check for death
            if (oldHealth > 0 && currentHealth <= 0)
            {
                HandleDeath();
            }
        }
    }
    
    /// <summary>
    /// Take damage, reducing health by the specified amount
    /// </summary>
    /// <param name="amount">Amount of damage to take</param>
    /// <param name="source">Optional source of the damage</param>
    public virtual void TakeDamage(int amount, Unit source = null)
    {
        if (amount <= 0) return;
        
        Debug.Log($"{unitName} takes {amount} damage" + (source != null ? $" from {source.UnitName}" : ""));
        SetHealth(currentHealth - amount);
    }
    
    /// <summary>
    /// Heal, increasing health by the specified amount
    /// </summary>
    /// <param name="amount">Amount to heal</param>
    /// <param name="source">Optional source of the healing</param>
    public virtual void Heal(int amount, Unit source = null)
    {
        if (amount <= 0 || !IsAlive) return;
        
        Debug.Log($"{unitName} heals {amount} health" + (source != null ? $" from {source.UnitName}" : ""));
        SetHealth(currentHealth + amount);
    }
    
    /// <summary>
    /// Handle the unit's death
    /// </summary>
    protected virtual void HandleDeath()
    {
        Debug.Log($"{unitName} has been defeated!");
        
        // If this unit was selected, deselect it and auto-select another unit
        if (selectedUnit == this)
        {
            Deselect();
            AutoSelectUnitForCurrentTurn();
        }
        
        // Notify subscribers
        OnUnitDeath?.Invoke();
        
        // Additional death logic can be implemented in derived classes
        // This could include death animations, dropping items, etc.
    }
    
    #endregion

    #region Card System Hooks
    
    /// <summary>
    /// Called when a card is played on or by this unit
    /// </summary>
    /// <param name="cardData">Data about the card being played</param>
    /// <param name="source">The unit playing the card</param>
    /// <returns>True if the card effect was applied successfully</returns>
    public virtual bool OnCardPlayed(object cardData, Unit source)
    {
        // This is a stub for future card system implementation
        // Override in derived classes to handle specific card effects
        Debug.Log($"Card played on {unitName} by {source?.UnitName ?? "unknown source"}");
        return true;
    }
    
    #endregion

    #region Movement Hooks
    
    /// <summary>
    /// Move the unit to a target position
    /// </summary>
    /// <param name="targetPosition">The position to move to</param>
    /// <param name="movementCost">The movement point cost for this movement</param>
    /// <returns>True if movement succeeded</returns>
    public virtual bool Move(Vector3 targetPosition, int movementCost = 1)
    {
        // Verify the unit can move
        if (!IsAlive || !SpendMovementPoints(movementCost))
            return false;

        // Use UnitGridAdapter if present
        UnitGridAdapter adapter = GetComponent<UnitGridAdapter>();
        if (adapter != null)
        {
            return adapter.HandleMoveRequest(targetPosition, movementCost);
        }

        // fallback (editor testing / emergency)
        transform.position = targetPosition;
        Debug.LogWarning($"{unitName} moved directly to {targetPosition} (adapter missing!)");

        return true;
    }

    
    #endregion
}