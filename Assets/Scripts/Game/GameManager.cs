using System.Collections;
using UnityEngine;

/// <summary>
/// GameManager for Dark Protocol - Handles the turn-based combat system
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Turn State Management
    // Enum to represent the current turn state
    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn,
        Transitioning // For potential animations/transitions between turns
    }

    // Property to get the current turn state
    public TurnState CurrentTurnState { get; private set; }

    // Event delegates for turn changes (for other systems to hook into)
    public delegate void TurnChangedHandler(TurnState newState);
    public event TurnChangedHandler OnTurnChanged;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // Begin the mission with player's turn
        StartPlayerTurn();
    }
    #endregion

    #region Turn Management Methods
    /// <summary>
    /// Begins the player's turn
    /// </summary>
    public void StartPlayerTurn()
    {
        CurrentTurnState = TurnState.PlayerTurn;
        Debug.Log("<color=green>Player Turn Started</color> - Select a soldier and play cards");
        
        // Notify subscribers of turn change
        OnTurnChanged?.Invoke(CurrentTurnState);
        
        // Additional setup for player turn could go here
        // - Refresh action points
        // - Update UI
        // - Enable player input
        // - Select default soldier (or last selected)
    }

    /// <summary>
    /// Called when the player chooses to end their turn
    /// </summary>
    public void EndPlayerTurn()
    {
        if (CurrentTurnState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("Attempted to end player turn when it wasn't player's turn!");
            return;
        }

        CurrentTurnState = TurnState.Transitioning;
        Debug.Log("<color=yellow>Transitioning to Enemy Turn</color>");
        
        // Start enemy turn processing
        StartCoroutine(ProcessEnemyTurn());
    }

    /// <summary>
    /// Simulates the enemy AI taking their turn
    /// </summary>
    private IEnumerator ProcessEnemyTurn()
    {
        // Short delay before enemy turn starts (for UI/feedback purposes)
        yield return new WaitForSeconds(0.5f);
        
        CurrentTurnState = TurnState.EnemyTurn;
        Debug.Log("<color=red>Enemy Turn Started</color> - AI controlling enemies");
        
        // Notify subscribers of turn change
        OnTurnChanged?.Invoke(CurrentTurnState);
        
        // Simulate enemy AI taking time to make decisions
        // This will be replaced with actual AI logic later
        yield return new WaitForSeconds(2f);
        
        // Enemy turn is complete
        EndEnemyTurn();
    }

    /// <summary>
    /// Ends the enemy turn and returns to player turn
    /// </summary>
    private void EndEnemyTurn()
    {
        if (CurrentTurnState != TurnState.EnemyTurn)
        {
            Debug.LogWarning("Attempted to end enemy turn when it wasn't enemy's turn!");
            return;
        }

        CurrentTurnState = TurnState.Transitioning;
        Debug.Log("<color=yellow>Transitioning to Player Turn</color>");
        
        // Short delay before returning to player turn
        StartCoroutine(TransitionToPlayerTurn());
    }

    /// <summary>
    /// Handles the transition back to player turn
    /// </summary>
    private IEnumerator TransitionToPlayerTurn()
    {
        // Short delay for feedback purposes
        yield return new WaitForSeconds(0.5f);
        
        // Start the next player turn
        StartPlayerTurn();
    }
    #endregion

    #region Public Interface Methods
    /// <summary>
    /// Public method to manually end the player's turn (called by UI button)
    /// </summary>
    public void OnEndTurnButtonPressed()
    {
        if (CurrentTurnState == TurnState.PlayerTurn)
        {
            EndPlayerTurn();
        }
    }

    /// <summary>
    /// Check if it's currently the player's turn
    /// </summary>
    public bool IsPlayerTurn()
    {
        return CurrentTurnState == TurnState.PlayerTurn;
    }

    /// <summary>
    /// Check if it's currently the enemy's turn
    /// </summary>
    public bool IsEnemyTurn()
    {
        return CurrentTurnState == TurnState.EnemyTurn;
    }
    #endregion
}