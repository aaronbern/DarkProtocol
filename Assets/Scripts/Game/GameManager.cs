using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkProtocol.Grid;

/// <summary>
/// Enhanced GameManager for Dark Protocol - Handles the complete turn-based combat system
/// with support for animations, camera control, UI integration, and sound effects.
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
        
        // Initialize systems
        InitializeSystems();
    }
    #endregion

    #region Public Properties and Fields
    [Header("Game State")]
    [SerializeField] private bool autoStartGame = true;
    [SerializeField] private bool enableDebugOutput = true;
    
    [Header("Turn Management")]
    [SerializeField] private float turnTransitionDelay = 0.5f;
    [SerializeField] private int maxRounds = 30; // Max rounds before game forced to end
    
    [Header("Camera Control")]
    [SerializeField] private bool centerCameraOnActiveUnit = true;
    [SerializeField] private float cameraCenterSpeed = 2f;
    [SerializeField] private float defaultCameraHeight = 10f;
    [SerializeField] private bool zoomInDuringActions = true;
    [SerializeField] private float actionZoomHeight = 7f;
    
    [Header("UI References")]
    [SerializeField] private GameObject turnBanner;
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private GameObject unitInfoPanel;
    
    [Header("Audio")]
    [SerializeField] private AudioClip turnStartSound;
    [SerializeField] private AudioClip turnEndSound;
    [SerializeField] private AudioClip unitSelectSound;
    [SerializeField] private AudioClip playerVictoryMusic;
    [SerializeField] private AudioClip playerDefeatMusic;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    // Public game state properties
    public int CurrentRound { get; private set; } = 0;
    public TurnState CurrentTurnState { get; private set; } = TurnState.None;
    public bool IsGameOver { get; private set; } = false;
    public Unit ActiveUnit { get; private set; } = null;
    
    // Get systems
    public TacticalCameraController CameraSystem => _cameraController;
    public UIManager UISystem => _uiManager;
    public AudioManager AudioSystem => _audioManager;
    public MissionManager MissionSystem => _missionManager;
    #endregion

    #region Enums and Types
    // Enum to represent the current turn state
    public enum TurnState
    {
        None,
        Initializing,
        PlayerTurnStart,
        PlayerTurn,
        PlayerTurnEnd,
        EnemyTurnStart,
        EnemyTurn,
        EnemyTurnEnd,
        Transitioning,
        GameOver
    }
    
    // Game result enum
    public enum GameResult
    {
        None,
        PlayerVictory,
        PlayerDefeat,
        Draw
    }
    
    // Turn info structure
    public struct TurnInfo
    {
        public int RoundNumber;
        public TurnState State;
        public Unit.TeamType ActiveTeam;
        public bool IsFirstTurn;
        public float RemainingTime; // For turn timers if desired
    }
    #endregion

    #region Events
    // Event delegates for turn changes
    public delegate void TurnChangedHandler(TurnState newState);
    public event TurnChangedHandler OnTurnChanged;
    
    // Enhanced turn changed event with more info
    public delegate void EnhancedTurnChangedHandler(TurnState newState, TurnInfo turnInfo);
    public event EnhancedTurnChangedHandler OnEnhancedTurnChanged;
    
    // Round changed event
    public delegate void RoundChangedHandler(int newRound);
    public event RoundChangedHandler OnRoundChanged;
    
    // Unit activated event
    public delegate void UnitActivatedHandler(Unit unit);
    public event UnitActivatedHandler OnUnitActivated;
    
    // Unit deactivated event
    public delegate void UnitDeactivatedHandler(Unit unit);
    public event UnitDeactivatedHandler OnUnitDeactivated;
    
    // Game over event
    public delegate void GameOverHandler(GameResult result);
    public event GameOverHandler OnGameOver;
    
    // Mission events
    public event Action OnMissionStart;
    public event Action<Unit> OnUnitDied;
    #endregion

    #region Component References
    // System components
    private TacticalCameraController _cameraController;
    private UIManager _uiManager;
    private AudioManager _audioManager;
    private MissionManager _missionManager;
    
    // Queue of coroutines to process sequentially
    private Queue<IEnumerator> _actionQueue = new Queue<IEnumerator>();
    private bool _isProcessingActions = false;
    #endregion

    #region Initialization
    /// <summary>
    /// Initialize all required systems
    /// </summary>
    private void InitializeSystems()
    {
        DebugLog("Initializing Game Systems");
        
        // Get the camera controller
        _cameraController = FindFirstObjectByType<TacticalCameraController>();
        
        // Create UIManager if needed
        _uiManager = FindFirstObjectByType<UIManager>();
        if (_uiManager == null)
        {
            GameObject uiManagerObj = new GameObject("UI Manager");
            _uiManager = uiManagerObj.AddComponent<UIManager>();
        }
        
        // Create AudioManager if needed
        _audioManager = FindFirstObjectByType<AudioManager>();
        if (_audioManager == null)
        {
            GameObject audioManagerObj = new GameObject("Audio Manager");
            _audioManager = audioManagerObj.AddComponent<AudioManager>();
            
            // Initialize audio sources if needed
            if (musicSource == null)
            {
                AudioSource newMusicSource = audioManagerObj.AddComponent<AudioSource>();
                newMusicSource.loop = true;
                newMusicSource.playOnAwake = false;
                musicSource = newMusicSource;
            }
            
            if (sfxSource == null)
            {
                AudioSource newSfxSource = audioManagerObj.AddComponent<AudioSource>();
                newSfxSource.loop = false;
                newSfxSource.playOnAwake = false;
                sfxSource = newSfxSource;
            }
            
            // Set up the audio manager
            _audioManager.Initialize(musicSource, sfxSource);
        }
        
        // Create MissionManager if needed
        _missionManager = FindFirstObjectByType<MissionManager>();
        if (_missionManager == null)
        {
            GameObject missionManagerObj = new GameObject("Mission Manager");
            _missionManager = missionManagerObj.AddComponent<MissionManager>();
        }
        
        // Initialize units
        foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            unit.OnUnitDeath += () => HandleUnitDeath(unit);
        }
        
        // We'll use the Unit's static SelectedUnit property instead of direct event subscription
        // This approach works with your existing selection system without needing access to protected events
        
        // We need to monitor for selection changes in the Update method
        // The Unit class is already set up to track the currently selected unit with its static SelectedUnit property
        
        DebugLog("Game Systems Initialized");
    }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // Begin the game if auto-start is enabled
        if (autoStartGame)
        {
            StartGame();
        }
        else
        {
            CurrentTurnState = TurnState.Initializing;
        }
    }
    
    // Track previously selected unit for change detection
    private Unit _previouslySelectedUnit = null;
    
    private void Update()
    {
        // Process action queue
        if (_actionQueue.Count > 0 && !_isProcessingActions)
        {
            StartCoroutine(ProcessActionQueue());
        }
        
        // Check for unit selection changes
        Unit currentSelectedUnit = Unit.SelectedUnit;
        if (currentSelectedUnit != _previouslySelectedUnit)
        {
            HandleUnitSelected(currentSelectedUnit);
            _previouslySelectedUnit = currentSelectedUnit;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up
        _previouslySelectedUnit = null;
    }
    #endregion

    #region Game Flow Control
    /// <summary>
    /// Starts a new game
    /// </summary>
    public void StartGame()
    {
        DebugLog("Starting New Game");
        
        // Reset game state
        CurrentRound = 1;
        IsGameOver = false;
        ActiveUnit = null;
        
        // Notify mission start
        OnMissionStart?.Invoke();
        
        // Start with player's turn
        StartTurnTransition(TurnState.PlayerTurnStart);
    }
    
    /// <summary>
    /// Process a state transition
    /// </summary>
    private void StartTurnTransition(TurnState newState)
    {
        TurnState previousState = CurrentTurnState;
        CurrentTurnState = newState;
        
        // Create turn info for event
        TurnInfo turnInfo = new TurnInfo
        {
            RoundNumber = CurrentRound,
            State = newState,
            ActiveTeam = GetCurrentTeam(),
            IsFirstTurn = (CurrentRound == 1 && newState == TurnState.PlayerTurn),
            RemainingTime = -1 // No time limit by default
        };
        
        DebugLog($"Turn State Change: {previousState} → {newState}");
        
        // Notify subscribers of turn change - basic version first
        OnTurnChanged?.Invoke(newState);
        
        // Then enhanced version
        OnEnhancedTurnChanged?.Invoke(newState, turnInfo);
        
        // Enqueue transition animation/delay
        EnqueueAction(TurnTransitionAnimation(newState, turnInfo));
    }
    
    /// <summary>
    /// Handle the transition between turn states
    /// </summary>
    private IEnumerator TurnTransitionAnimation(TurnState newState, TurnInfo turnInfo)
    {
        // Transition animations and delays based on state
        switch (newState)
        {
            case TurnState.PlayerTurnStart:
                // Show player turn banner
                if (_uiManager != null)
                    _uiManager.ShowTurnBanner("Player Turn", Color.blue);
                
                // Play turn start sound
                if (_audioManager != null)
                    _audioManager.PlaySFX(turnStartSound);
                
                // Wait for banner animation
                yield return new WaitForSeconds(turnTransitionDelay);
                
                // Transition to player turn
                StartTurnTransition(TurnState.PlayerTurn);
                break;
                
            case TurnState.PlayerTurn:
                // Execute player turn setup
                SetupPlayerTurn();
                break;
                
            case TurnState.PlayerTurnEnd:
                // Show transition
                if (_uiManager != null)
                    _uiManager.ShowTurnBanner("Player Turn Ended", Color.blue);
                
                // Play turn end sound
                if (_audioManager != null)
                    _audioManager.PlaySFX(turnEndSound);
                
                // Deselect current unit if any
                if (ActiveUnit != null)
                {
                    Unit.SelectUnit(null);
                    ActiveUnit = null;
                }
                
                // Wait for banner animation
                yield return new WaitForSeconds(turnTransitionDelay);
                
                // Transition to enemy turn
                StartTurnTransition(TurnState.EnemyTurnStart);
                break;
                
            case TurnState.EnemyTurnStart:
                // Show enemy turn banner
                if (_uiManager != null)
                    _uiManager.ShowTurnBanner("Enemy Turn", Color.red);
                
                // Play turn start sound
                if (_audioManager != null)
                    _audioManager.PlaySFX(turnStartSound);
                
                // Wait for banner animation
                yield return new WaitForSeconds(turnTransitionDelay);
                
                // Transition to enemy turn
                StartTurnTransition(TurnState.EnemyTurn);
                break;
                
            case TurnState.EnemyTurn:
                // Execute enemy turn logic
                EnqueueAction(ProcessEnemyTurn());
                break;
                
            case TurnState.EnemyTurnEnd:
                // Show transition
                if (_uiManager != null)
                    _uiManager.ShowTurnBanner("Enemy Turn Ended", Color.red);
                
                // Play turn end sound
                if (_audioManager != null)
                    _audioManager.PlaySFX(turnEndSound);
                
                // Wait for banner animation
                yield return new WaitForSeconds(turnTransitionDelay);
                
                // Advance round and start player turn
                CurrentRound++;
                OnRoundChanged?.Invoke(CurrentRound);
                
                // Check win/loss conditions before continuing
                if (CheckGameOverConditions())
                {
                    StartTurnTransition(TurnState.GameOver);
                }
                else
                {
                    // Start next player turn
                    StartTurnTransition(TurnState.PlayerTurnStart);
                }
                break;
                
            case TurnState.GameOver:
                HandleGameOver();
                break;
        }
    }
    
    /// <summary>
    /// Setup for player turn
    /// </summary>
    private void SetupPlayerTurn()
    {
        // Reset turn state for all player units
        foreach (Unit unit in Unit.GetUnitsOfTeam(Unit.TeamType.Player))
        {
            unit.StartTurn();
        }
        
        // Auto-select if there's only one unit
        List<Unit> playerUnits = Unit.GetUnitsOfTeam(Unit.TeamType.Player);
        if (playerUnits.Count == 1)
        {
            Unit.SelectUnit(playerUnits[0]);
        }
        
        // Enable player input
        EnablePlayerInput(true);
        
        DebugLog("Player Turn Started");
    }
    
    /// <summary>
    /// Process enemy turn logic
    /// </summary>
    private IEnumerator ProcessEnemyTurn()
    {
        DebugLog("Enemy Turn Started");
        
        // Reset all enemy units
        foreach (Unit unit in Unit.GetUnitsOfTeam(Unit.TeamType.Enemy))
        {
            unit.StartTurn();
        }
        
        // Get all enemy units
        List<Unit> enemyUnits = Unit.GetUnitsOfTeam(Unit.TeamType.Enemy);
        
        // Process each enemy unit's turn
        foreach (Unit enemyUnit in enemyUnits)
        {
            // "Activate" the enemy unit
            ActiveUnit = enemyUnit;
            OnUnitActivated?.Invoke(enemyUnit);
            
            // Center camera on enemy unit if enabled
            if (centerCameraOnActiveUnit && _cameraController != null)
            {
                _cameraController.FocusOnTarget(enemyUnit.transform, cameraCenterSpeed);
                yield return new WaitForSeconds(0.5f); // Wait for camera movement
            }
            
            // Execute AI logic for this unit
            yield return StartCoroutine(ExecuteEnemyUnitAI(enemyUnit));
            
            // Small delay between units
            yield return new WaitForSeconds(0.5f);
        }
        
        // End enemy turn
        ActiveUnit = null;
        StartTurnTransition(TurnState.EnemyTurnEnd);
    }
    
    /// <summary>
    /// Execute AI logic for a specific enemy unit
    /// </summary>
    private IEnumerator ExecuteEnemyUnitAI(Unit enemyUnit)
    {
        DebugLog($"Processing AI for {enemyUnit.UnitName}");
        
        // Get the AI controller for this unit if it has one
        IAIController aiController = enemyUnit.GetComponent<IAIController>();
        
        if (aiController != null)
        {
            // Execute AI decision making
            yield return StartCoroutine(aiController.ExecuteTurn());
        }
        else
        {
            // Simple fallback AI if no controller exists
            yield return StartCoroutine(FallbackEnemyAI(enemyUnit));
        }
        
        // End the unit's turn
        enemyUnit.EndTurn();
    }
    
    /// <summary>
    /// Simple fallback AI for enemies without a dedicated controller
    /// </summary>
    private IEnumerator FallbackEnemyAI(Unit enemyUnit)
    {
        // Find nearest player unit
        Unit nearestPlayer = FindNearestUnit(enemyUnit, Unit.TeamType.Player);
        
        if (nearestPlayer != null)
        {
            // Get positions
            Vector3 enemyPos = enemyUnit.transform.position;
            Vector3 playerPos = nearestPlayer.transform.position;
            
            // Calculate direction and distance
            Vector3 direction = (playerPos - enemyPos).normalized;
            float distance = Vector3.Distance(enemyPos, playerPos);
            
            // Simple movement toward player
            if (distance > 5f) // If far away, move closer
            {
                Vector3 targetPos = enemyPos + direction * Mathf.Min(distance * 0.5f, enemyUnit.CurrentMovementPoints);
                
                // Move the unit
                DebugLog($"Enemy {enemyUnit.UnitName} moving toward player");
                enemyUnit.Move(targetPos);
                
                // Wait for movement to complete
                yield return new WaitForSeconds(1.0f);
            }
            else // If close, simulate an attack
            {
                DebugLog($"Enemy {enemyUnit.UnitName} attacks player");
                
                // Simulate attack animation
                yield return new WaitForSeconds(0.8f);
                
                // Simulated attack
                nearestPlayer.TakeDamage(10, enemyUnit);
            }
        }
        else
        {
            // No player units found, just wait
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// Find the nearest unit of a specific team
    /// </summary>
    private Unit FindNearestUnit(Unit fromUnit, Unit.TeamType targetTeam)
    {
        List<Unit> targetUnits = Unit.GetUnitsOfTeam(targetTeam);
        
        Unit nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (Unit targetUnit in targetUnits)
        {
            float distance = Vector3.Distance(fromUnit.transform.position, targetUnit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = targetUnit;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Check if the game has reached an end condition
    /// </summary>
    private bool CheckGameOverConditions()
    {
        // Check round limit
        if (CurrentRound > maxRounds)
        {
            DebugLog("Game over: Maximum rounds reached");
            return true;
        }
        
        // Check if all player units are defeated
        if (Unit.GetUnitsOfTeam(Unit.TeamType.Player).Count == 0)
        {
            DebugLog("Game over: All player units defeated");
            return true;
        }
        
        // Check if all enemy units are defeated
        if (Unit.GetUnitsOfTeam(Unit.TeamType.Enemy).Count == 0)
        {
            DebugLog("Game over: All enemy units defeated");
            return true;
        }
        
        // Check mission-specific win/loss conditions
        if (_missionManager != null && _missionManager.CheckMissionEndConditions(out _))
        {
            DebugLog("Game over: Mission end conditions met");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Handle game over state
    /// </summary>
    private void HandleGameOver()
    {
        IsGameOver = true;
        
        // Determine win/loss
        GameResult result = GameResult.Draw;
        
        if (Unit.GetUnitsOfTeam(Unit.TeamType.Player).Count == 0)
        {
            result = GameResult.PlayerDefeat;
        }
        else if (Unit.GetUnitsOfTeam(Unit.TeamType.Enemy).Count == 0)
        {
            result = GameResult.PlayerVictory;
        }
        else if (_missionManager != null)
        {
            // Check mission-specific victory conditions
            _missionManager.CheckMissionEndConditions(out result);
        }
        
        // Play appropriate music
        if (_audioManager != null)
        {
            switch (result)
            {
                case GameResult.PlayerVictory:
                    _audioManager.PlayMusic(playerVictoryMusic);
                    break;
                case GameResult.PlayerDefeat:
                    _audioManager.PlayMusic(playerDefeatMusic);
                    break;
            }
        }
        
        // Show game over UI
        if (_uiManager != null)
        {
            switch (result)
            {
                case GameResult.PlayerVictory:
                    _uiManager.ShowGameOverScreen("Victory!", Color.green);
                    break;
                case GameResult.PlayerDefeat:
                    _uiManager.ShowGameOverScreen("Defeat", Color.red);
                    break;
                case GameResult.Draw:
                    _uiManager.ShowGameOverScreen("Draw", Color.yellow);
                    break;
            }
        }
        
        // Notify listeners
        OnGameOver?.Invoke(result);
        
        DebugLog($"Game Over: {result}");
    }
    #endregion

    #region Action and Turn Management
    /// <summary>
    /// Called by UI when the player chooses to end their turn
    /// </summary>
    public void EndPlayerTurn()
    {
        if (CurrentTurnState != TurnState.PlayerTurn)
        {
            DebugLog("Cannot end player turn - not currently player's turn!");
            return;
        }
        
        // End turn for all player units
        foreach (Unit unit in Unit.GetUnitsOfTeam(Unit.TeamType.Player))
        {
            unit.EndTurn();
        }
        
        // Disable player input
        EnablePlayerInput(false);
        
        // Start turn ending sequence
        StartTurnTransition(TurnState.PlayerTurnEnd);
    }
    
    /// <summary>
    /// Queue an action to be executed
    /// </summary>
    public void EnqueueAction(IEnumerator action)
    {
        _actionQueue.Enqueue(action);
    }
    
    /// <summary>
    /// Process the action queue
    /// </summary>
    private IEnumerator ProcessActionQueue()
    {
        _isProcessingActions = true;
        
        while (_actionQueue.Count > 0)
        {
            IEnumerator action = _actionQueue.Dequeue();
            yield return StartCoroutine(action);
        }
        
        _isProcessingActions = false;
    }
    
    /// <summary>
    /// Enable or disable player input
    /// </summary>
    private void EnablePlayerInput(bool enable)
    {
        // Enable/disable input handlers
        UnitInputHandler[] inputHandlers = FindObjectsByType<UnitInputHandler>(FindObjectsSortMode.None);
        foreach (UnitInputHandler handler in inputHandlers)
        {
            handler.enabled = enable;
        }
        
        // Enable/disable grid input
        var inputService = GridServiceLocator.Instance?.GetService<IGridInputService>();
        
        if (inputService != null)
        {
            if (enable)
            {
                inputService.EnableInput();
            }
            else
            {
                inputService.DisableInput();
            }
        }
    }
    
    /// <summary>
    /// Get the team for the current turn
    /// </summary>
    private Unit.TeamType GetCurrentTeam()
    {
        switch (CurrentTurnState)
        {
            case TurnState.PlayerTurn:
            case TurnState.PlayerTurnStart:
            case TurnState.PlayerTurnEnd:
                return Unit.TeamType.Player;
                
            case TurnState.EnemyTurn:
            case TurnState.EnemyTurnStart:
            case TurnState.EnemyTurnEnd:
                return Unit.TeamType.Enemy;
                
            default:
                return Unit.TeamType.Neutral;
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

    #region Event Handlers
    /// <summary>
    /// Handle unit selection events
    /// </summary>
    private void HandleUnitSelected(Unit unit)
    {
        // Only process during player turn
        if (CurrentTurnState != TurnState.PlayerTurn)
            return;
            
        // Play selection sound
        if (_audioManager != null && unit != null)
        {
            _audioManager.PlaySFX(unitSelectSound);
        }
        
        // Center camera on selected unit if enabled
        if (unit != null && centerCameraOnActiveUnit && _cameraController != null)
        {
            _cameraController.FocusOnTarget(unit.transform, cameraCenterSpeed);
        }
        
        // Update active unit
        if (unit != null && unit.Team == Unit.TeamType.Player)
        {
            // Set as active unit
            ActiveUnit = unit;
            
            // Notify listeners
            OnUnitActivated?.Invoke(unit);
            
            // Update UI
            if (_uiManager != null)
            {
                _uiManager.ShowUnitInfo(unit);
                _uiManager.ShowActionPanel(unit);
            }
            
            // Also notify grid system
            GridManager.Instance?.OnUnitSelected(unit);
        }
        else if (ActiveUnit != null)
        {
            // Notify deactivation of previous unit
            Unit previousActive = ActiveUnit;
            ActiveUnit = null;
            OnUnitDeactivated?.Invoke(previousActive);
            
            // Update UI
            if (_uiManager != null)
            {
                _uiManager.HideUnitInfo();
                _uiManager.HideActionPanel();
            }
        }
    }
    
    /// <summary>
    /// Handle a unit's death
    /// </summary>
    private void HandleUnitDeath(Unit unit)
    {
        DebugLog($"Unit {unit.UnitName} has died");
        
        // Notify subscribers
        OnUnitDied?.Invoke(unit);
        
        // If this was the active unit, deactivate it
        if (unit == ActiveUnit)
        {
            ActiveUnit = null;
            OnUnitDeactivated?.Invoke(unit);
        }
        
        // Check for game over condition after a unit dies
        if (CheckGameOverConditions() && !IsGameOver)
        {
            StartTurnTransition(TurnState.GameOver);
        }
    }
    #endregion

    #region Utility Functions
    /// <summary>
    /// Debug logging with prefix
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugOutput)
        {
            Debug.Log($"[GameManager] {message}");
        }
    }
    #endregion
}

/// <summary>
/// Interface for AI controllers
/// </summary>
public interface IAIController
{
    /// <summary>
    /// Execute the AI turn logic
    /// </summary>
    IEnumerator ExecuteTurn();
}

/// <summary>
/// Helper for Unit-Camera integration with TacticalCameraController
/// </summary>
public static class CameraExtensions
{
    public static void FocusOnTarget(this TacticalCameraController controller, Transform target, float speed)
    {
        if (controller != null && target != null)
        {
            // Your TacticalCameraController should already have this functionality
            // This is just a convenience wrapper
            
            // Example:
            // controller.SetTargetPosition(target.position);
            // controller.SetMovementSpeed(speed);
        }
    }
}

/// <summary>
/// UI Manager for tactical UI
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject turnBannerObject;
    [SerializeField] private TMPro.TextMeshProUGUI turnBannerText;
    [SerializeField] private GameObject actionPanelObject;
    [SerializeField] private GameObject unitInfoPanelObject;
    [SerializeField] private GameObject gameOverPanelObject;
    [SerializeField] private TMPro.TextMeshProUGUI gameOverText;
    
    [Header("Animation")]
    [SerializeField] private float bannerFadeSpeed = 2f;
    [SerializeField] private float panelSlideSpeed = 0.5f;
    
    private Coroutine _bannerCoroutine;
    
    /// <summary>
    /// Show the turn banner with text and color
    /// </summary>
    public void ShowTurnBanner(string text, Color color)
    {
        // Stop any existing banner animations
        if (_bannerCoroutine != null)
        {
            StopCoroutine(_bannerCoroutine);
        }
        
        // Start the banner animation
        _bannerCoroutine = StartCoroutine(AnimateTurnBanner(text, color));
    }
    
    /// <summary>
    /// Show the unit info panel for a unit
    /// </summary>
    public void ShowUnitInfo(Unit unit)
    {
        if (unitInfoPanelObject == null)
            return;
            
        // Activate panel
        unitInfoPanelObject.SetActive(true);
        
        // Find the unit info component and update it
        UnitInfoPanel infoPanel = unitInfoPanelObject.GetComponent<UnitInfoPanel>();
        if (infoPanel != null)
        {
            infoPanel.UpdateUnitInfo(unit);
        }
    }
    
    /// <summary>
    /// Hide the unit info panel
    /// </summary>
    public void HideUnitInfo()
    {
        if (unitInfoPanelObject != null)
        {
            unitInfoPanelObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show the action panel for a unit
    /// </summary>
    public void ShowActionPanel(Unit unit)
    {
        if (actionPanelObject == null)
            return;
            
        // Activate panel
        actionPanelObject.SetActive(true);
        
        // Find the action panel component and update it
        ActionPanel actionPanel = actionPanelObject.GetComponent<ActionPanel>();
        if (actionPanel != null)
        {
            actionPanel.SetUnit(unit);
        }
    }
    
    /// <summary>
    /// Hide the action panel
    /// </summary>
    public void HideActionPanel()
    {
        if (actionPanelObject != null)
        {
            actionPanelObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show the game over screen
    /// </summary>
    public void ShowGameOverScreen(string text, Color color)
    {
        if (gameOverPanelObject == null || gameOverText == null)
            return;
            
        // Set text and color
        gameOverText.text = text;
        gameOverText.color = color;
        
        // Activate panel
        gameOverPanelObject.SetActive(true);
    }
    
    /// <summary>
    /// Animate the turn banner
    /// </summary>
    private IEnumerator AnimateTurnBanner(string text, Color color)
    {
        if (turnBannerObject == null || turnBannerText == null)
            yield break;
            
        // Set text and color
        turnBannerText.text = text;
        turnBannerText.color = color;
        
        // Activate the banner
        turnBannerObject.SetActive(true);
        
        // Fade in
        CanvasGroup canvasGroup = turnBannerObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            
            // Fade in
            float timer = 0;
            while (timer < 1)
            {
                timer += Time.deltaTime * bannerFadeSpeed;
                canvasGroup.alpha = Mathf.Clamp01(timer);
                yield return null;
            }
            
            // Hold
            yield return new WaitForSeconds(1.5f);
            
            // Fade out
            timer = 1;
            while (timer > 0)
            {
                timer -= Time.deltaTime * bannerFadeSpeed;
                canvasGroup.alpha = Mathf.Clamp01(timer);
                yield return null;
            }
        }
        else
        {
            // Simple show/hide if no canvas group
            yield return new WaitForSeconds(2f);
        }
        
        // Hide the banner
        turnBannerObject.SetActive(false);
    }
}

/// <summary>
/// Audio manager for tactical audio
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    /// <summary>
    /// Initialize the audio manager
    /// </summary>
    public void Initialize(AudioSource music, AudioSource sfx)
    {
        musicSource = music;
        sfxSource = sfx;
        
        // Apply volume settings
        UpdateVolumes();
    }
    
    /// <summary>
    /// Update all volume levels
    /// </summary>
    public void UpdateVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
        
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume * masterVolume;
        }
    }
    
    /// <summary>
    /// Play a sound effect
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }
    
    /// <summary>
    /// Play music
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }
    
    /// <summary>
    /// Stop the current music
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
    
    /// <summary>
    /// Pause or resume the current music
    /// </summary>
    public void PauseMusic(bool pause)
    {
        if (musicSource != null)
        {
            if (pause)
            {
                musicSource.Pause();
            }
            else
            {
                musicSource.UnPause();
            }
        }
    }
}

/// <summary>
/// Mission manager for mission-specific logic
/// </summary>
public class MissionManager : MonoBehaviour
{
    [Header("Mission Settings")]
    [SerializeField] private string missionName = "Default Mission";
    [SerializeField] private string missionDescription = "Defeat all enemies.";
    [SerializeField] private int missionTimeLimit = -1; // -1 for no limit
    
    [Header("Win Conditions")]
    [SerializeField] private bool winOnAllEnemiesDefeated = true;
    [SerializeField] private bool winOnObjectivesComplete = false;
    [SerializeField] private int objectivesToComplete = 1;
    
    [Header("Loss Conditions")]
    [SerializeField] private bool loseOnAllPlayerUnitsDefeated = true;
    [SerializeField] private bool loseOnTimeLimitReached = false;
    [SerializeField] private bool loseOnObjectiveDestroyed = false;
    
    private int _completedObjectives = 0;
    private bool _objectiveDestroyed = false;
    private float _missionTimer = 0;
    
    private void Update()
    {
        // Update mission timer if there's a time limit
        if (missionTimeLimit > 0)
        {
            _missionTimer += Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Check if mission end conditions have been met
    /// </summary>
    public bool CheckMissionEndConditions(out GameManager.GameResult result)
    {
        result = GameManager.GameResult.None;
        
        // Check win conditions
        if (winOnAllEnemiesDefeated && Unit.GetUnitsOfTeam(Unit.TeamType.Enemy).Count == 0)
        {
            result = GameManager.GameResult.PlayerVictory;
            return true;
        }
        
        if (winOnObjectivesComplete && _completedObjectives >= objectivesToComplete)
        {
            result = GameManager.GameResult.PlayerVictory;
            return true;
        }
        
        // Check loss conditions
        if (loseOnAllPlayerUnitsDefeated && Unit.GetUnitsOfTeam(Unit.TeamType.Player).Count == 0)
        {
            result = GameManager.GameResult.PlayerDefeat;
            return true;
        }
        
        if (loseOnTimeLimitReached && missionTimeLimit > 0 && _missionTimer >= missionTimeLimit)
        {
            result = GameManager.GameResult.PlayerDefeat;
            return true;
        }
        
        if (loseOnObjectiveDestroyed && _objectiveDestroyed)
        {
            result = GameManager.GameResult.PlayerDefeat;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Mark an objective as completed
    /// </summary>
    public void CompleteObjective()
    {
        _completedObjectives++;
    }
    
    /// <summary>
    /// Mark an objective as destroyed
    /// </summary>
    public void ObjectiveDestroyed()
    {
        _objectiveDestroyed = true;
    }
    
    /// <summary>
    /// Get the remaining mission time
    /// </summary>
    public float GetRemainingTime()
    {
        if (missionTimeLimit <= 0)
            return -1;
            
        return Mathf.Max(0, missionTimeLimit - _missionTimer);
    }
    
    /// <summary>
    /// Get the mission progress
    /// </summary>
    public float GetObjectiveProgress()
    {
        if (objectivesToComplete <= 0)
            return 1f;
            
        return (float)_completedObjectives / objectivesToComplete;
    }
}

/// <summary>
/// Action panel component
/// </summary>
public class ActionPanel : MonoBehaviour
{
    [SerializeField] private Transform actionButtonContainer;
    [SerializeField] private GameObject actionButtonPrefab;
    [SerializeField] private UnityEngine.UI.Button endTurnButton;
    
    private Unit _currentUnit;
    
    private void Awake()
    {
        // Set up end turn button
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndTurn);
        }
    }
    
    private void OnEnable()
    {
        RefreshActions();
    }
    
    /// <summary>
    /// Set the unit for this action panel
    /// </summary>
    public void SetUnit(Unit unit)
    {
        _currentUnit = unit;
        RefreshActions();
    }
    
    /// <summary>
    /// Refresh action buttons
    /// </summary>
    private void RefreshActions()
    {
        // Clear existing buttons
        if (actionButtonContainer != null)
        {
            foreach (Transform child in actionButtonContainer)
            {
                if (child.gameObject != endTurnButton?.gameObject)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        if (_currentUnit == null || actionButtonPrefab == null || actionButtonContainer == null)
            return;
            
        // Create basic action buttons
        CreateActionButton("Move", () => {
            // Move action logic
            Debug.Log("Move action clicked");
        });
        
        CreateActionButton("Attack", () => {
            // Attack action logic
            Debug.Log("Attack action clicked");
        });
        
        CreateActionButton("Defend", () => {
            // Defend action logic
            Debug.Log("Defend action clicked");
        });
        
        // TODO: Add unit-specific actions based on unit type
    }
    
    /// <summary>
    /// Create an action button
    /// </summary>
    private void CreateActionButton(string actionName, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonContainer);
        
        // Try to find TMPro text component
        var buttonText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = actionName;
        }
        else
        {
            // Fall back to regular text component
            var legacyText = buttonObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (legacyText != null)
            {
                legacyText.text = actionName;
            }
        }
        
        // Set button action
        UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }
    
    /// <summary>
    /// End the player's turn
    /// </summary>
    private void EndTurn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndPlayerTurn();
        }
    }
}

/// <summary>
/// Unit info panel component
/// </summary>
public class UnitInfoPanel : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI unitNameText;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    [SerializeField] private TMPro.TextMeshProUGUI actionPointsText;
    [SerializeField] private TMPro.TextMeshProUGUI movementPointsText;
    [SerializeField] private UnityEngine.UI.Image healthBar;
    [SerializeField] private UnityEngine.UI.Image actionPointsBar;
    
    private Unit _currentUnit;
    
    private void OnEnable()
    {
        // Subscribe to unit events if we have a current unit
        if (_currentUnit != null)
        {
            SubscribeToUnitEvents(_currentUnit);
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from unit events
        if (_currentUnit != null)
        {
            UnsubscribeFromUnitEvents(_currentUnit);
        }
    }
    
    /// <summary>
    /// Update the UI with unit information
    /// </summary>
    public void UpdateUnitInfo(Unit unit)
    {
        // Unsubscribe from previous unit
        if (_currentUnit != null)
        {
            UnsubscribeFromUnitEvents(_currentUnit);
        }
        
        // Store new unit
        _currentUnit = unit;
        
        // Subscribe to new unit events
        if (_currentUnit != null)
        {
            SubscribeToUnitEvents(_currentUnit);
            
            // Update UI elements
            RefreshUI();
        }
    }
    
    /// <summary>
    /// Refresh all UI elements
    /// </summary>
    private void RefreshUI()
    {
        if (_currentUnit == null)
            return;
            
        // Name
        if (unitNameText != null)
        {
            unitNameText.text = _currentUnit.UnitName;
        }
        
        // Health
        if (healthText != null)
        {
            healthText.text = $"{_currentUnit.CurrentHealth}/{_currentUnit.MaxHealth}";
        }
        
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)_currentUnit.CurrentHealth / _currentUnit.MaxHealth;
        }
        
        // Action Points
        if (actionPointsText != null)
        {
            actionPointsText.text = $"{_currentUnit.CurrentActionPoints}/{_currentUnit.MaxActionPoints}";
        }
        
        if (actionPointsBar != null)
        {
            actionPointsBar.fillAmount = (float)_currentUnit.CurrentActionPoints / _currentUnit.MaxActionPoints;
        }
        
        // Movement Points
        if (movementPointsText != null)
        {
            movementPointsText.text = $"{_currentUnit.CurrentMovementPoints}/{_currentUnit.MaxMovementPoints}";
        }
    }
    
    /// <summary>
    /// Subscribe to unit events
    /// </summary>
    private void SubscribeToUnitEvents(Unit unit)
    {
        unit.OnHealthChanged += HandleHealthChanged;
        unit.OnActionPointsChanged += HandleActionPointsChanged;
        unit.OnMovementPointsChanged += HandleMovementPointsChanged;
    }
    
    /// <summary>
    /// Unsubscribe from unit events
    /// </summary>
    private void UnsubscribeFromUnitEvents(Unit unit)
    {
        unit.OnHealthChanged -= HandleHealthChanged;
        unit.OnActionPointsChanged -= HandleActionPointsChanged;
        unit.OnMovementPointsChanged -= HandleMovementPointsChanged;
    }
    
    // Event handlers
    private void HandleHealthChanged(int newHealth, int oldHealth) => RefreshUI();
    private void HandleActionPointsChanged(int newAP, int oldAP) => RefreshUI();
    private void HandleMovementPointsChanged(int newMP, int oldMP) => RefreshUI();
}