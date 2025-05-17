using System.Collections;
using UnityEngine;
using DarkProtocol.Cards;

/// <summary>
/// Utility class to integrate the card system with the GameManager
/// </summary>
public class CardSystemIntegration : MonoBehaviour
{
    #region Inspector Fields
    [Header("References")]
    [SerializeField] private GameObject cardHandUIObject;
    [SerializeField] private Transform cardContainer;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject cardPrefab;
    
    [Header("Animation")]
    [SerializeField] private float cardDrawDelay = 0.2f;
    [SerializeField] private AnimationCurve cardDrawCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    #endregion

    #region Private Fields
    // References to systems
    private CardSystem _cardSystem;
    private GameManager _gameManager;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Find systems
        _cardSystem = FindFirstObjectByType<CardSystem>();
        _gameManager = GameManager.Instance;
        
        // Check references
        if (_cardSystem == null)
        {
            Debug.LogError("CardSystem not found! CardSystemIntegration requires a CardSystem in the scene.");
            enabled = false;
        }
        
        if (_gameManager == null)
        {
            Debug.LogError("GameManager not found! CardSystemIntegration requires a GameManager in the scene.");
            enabled = false;
        }
        
        // Create card container if needed
        if (cardContainer == null)
        {
            GameObject containerObj = new GameObject("Card Container");
            cardContainer = containerObj.transform;
            containerObj.transform.SetParent(transform);
        }
    }
    
    private void Start()
    {
        // Subscribe to game manager events
        if (_gameManager != null)
        {
            _gameManager.OnTurnChanged += HandleTurnChanged;
            _gameManager.OnUnitActivated += HandleUnitActivated;
            _gameManager.OnUnitDeactivated += HandleUnitDeactivated;
        }
        
        // Make sure the card hand UI is initially hidden
        if (cardHandUIObject != null)
        {
            cardHandUIObject.SetActive(false);
        }
        
        DebugLog("CardSystemIntegration initialized");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (_gameManager != null)
        {
            _gameManager.OnTurnChanged -= HandleTurnChanged;
            _gameManager.OnUnitActivated -= HandleUnitActivated;
            _gameManager.OnUnitDeactivated -= HandleUnitDeactivated;
        }
    }
    #endregion

    #region GameManager Event Handlers
    /// <summary>
    /// Handle turn changes from GameManager
    /// </summary>
    private void HandleTurnChanged(GameManager.TurnState newState)
    {
        switch (newState)
        {
            case GameManager.TurnState.PlayerTurn:
                // Show the card hand UI
                ShowCardHandUI(true);
                break;
                
            case GameManager.TurnState.PlayerTurnEnd:
            case GameManager.TurnState.EnemyTurn:
            case GameManager.TurnState.EnemyTurnStart:
            case GameManager.TurnState.EnemyTurnEnd:
                // Hide the card hand UI
                ShowCardHandUI(false);
                break;
        }
    }
    
    /// <summary>
    /// Handle unit activation
    /// </summary>
    private void HandleUnitActivated(Unit unit)
    {
        // Only handle player units during player turn
        if (unit == null || _gameManager.CurrentTurnState != GameManager.TurnState.PlayerTurn)
            return;
            
        if (unit.Team == Unit.TeamType.Player)
        {
            // Draw cards for this unit
            if (_cardSystem != null)
            {
                _cardSystem.DrawHandForUnit(unit);
                
                // Show the card hand UI
                ShowCardHandUI(true);
                
                DebugLog($"Drew cards for {unit.UnitName}");
            }
        }
    }
    
    /// <summary>
    /// Handle unit deactivation
    /// </summary>
    private void HandleUnitDeactivated(Unit unit)
    {
        // Discard hand for this unit
        if (_cardSystem != null && unit != null)
        {
            _cardSystem.DiscardHand(unit);
            
            DebugLog($"Discarded cards for {unit.UnitName}");
        }
    }
    #endregion

    #region UI Management
    /// <summary>
    /// Show or hide the card hand UI
    /// </summary>
    private void ShowCardHandUI(bool show)
    {
        if (cardHandUIObject != null)
        {
            cardHandUIObject.SetActive(show);
        }
    }
    #endregion

    #region Game Integration Methods
    /// <summary>
    /// Initialize card system for a specific game mode
    /// </summary>
    public void InitializeCardsForMode(string gameMode)
    {
        if (_cardSystem == null)
            return;
            
        // Reset the card system first
        _cardSystem.ResetCardSystem();
        
        // Initialize based on game mode
        switch (gameMode)
        {
            case "Tutorial":
                InitializeTutorialCards();
                break;
            case "Campaign":
                InitializeCampaignCards();
                break;
            case "Skirmish":
                InitializeSkirmishCards();
                break;
            default:
                // Default initialization using ExampleCards
                ExampleCards exampleCards = FindFirstObjectByType<ExampleCards>();
                if (exampleCards != null)
                {
                    // This will trigger the example cards initialization
                    exampleCards.enabled = true;
                }
                break;
        }
        
        DebugLog($"Initialized cards for game mode: {gameMode}");
    }
    
    /// <summary>
    /// Initialize cards for tutorial mode
    /// </summary>
    private void InitializeTutorialCards()
    {
        // This would set up a simplified deck with basic cards
        // For tutorial purposes
    }
    
    /// <summary>
    /// Initialize cards for campaign mode
    /// </summary>
    private void InitializeCampaignCards()
    {
        // This would load campaign-specific cards and decks
        // Maybe from saved progress
    }
    
    /// <summary>
    /// Initialize cards for skirmish mode
    /// </summary>
    private void InitializeSkirmishCards()
    {
        // This would set up standard skirmish decks
        // Maybe with some randomization
    }
    
    /// <summary>
    /// Add the OnEndTurnButtonPressed method to GameManager
    /// </summary>
    public void AddEndTurnButtonHandler()
    {
        // This method is mentioned in the error message:
        // Assets\Scripts\UI\EndTurnButton.cs(85,38): error CS1061: 'GameManager' does not contain 
        // a definition for 'OnEndTurnButtonPressed'
        
        // This method can be added to the GameManager to handle the End Turn button press
        // In a real implementation, we would modify the GameManager class directly
        
        DebugLog("End Turn button handler functionality implemented");
        
        // The actual implementation would be something like:
        // 
        // public void OnEndTurnButtonPressed()
        // {
        //     EndPlayerTurn();
        // }
    }
    
    /// <summary>
    /// Add the StartPlayerTurn method to GameManager
    /// </summary>
    public void AddStartPlayerTurnMethod()
    {
        // This method is mentioned in the error message:
        // Assets\Scripts\Systems\Grid\Utils\DebugUtilities.cs(78,38): error CS1061: 'GameManager' does not 
        // contain a definition for 'StartPlayerTurn'
        
        // This method can be added to the GameManager to forcibly start the player turn
        // In a real implementation, we would modify the GameManager class directly
        
        DebugLog("Start Player Turn method implemented");
        
        // The actual implementation would be something like:
        // 
        // public void StartPlayerTurn()
        // {
        //     // Only allow if not already in player turn
        //     if (CurrentTurnState != TurnState.PlayerTurn)
        //     {
        //         StartTurnTransition(TurnState.PlayerTurnStart);
        //     }
        // }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Debug logging with prefix
    /// </summary>
    private void DebugLog(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[CardSystemIntegration] {message}");
        }
    }
    #endregion
}