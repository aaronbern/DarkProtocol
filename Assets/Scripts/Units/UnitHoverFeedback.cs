using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DarkProtocol.Cards;

namespace DarkProtocol.Units
{
    /// <summary>
    /// Manages hover feedback for units in Dark Protocol.
    /// Detects when the mouse hovers over units and provides visual feedback accordingly.
    /// Integrates with card system for target highlighting during card play.
    /// </summary>
    [AddComponentMenu("Dark Protocol/Units/Unit Hover Feedback")]
    public class UnitHoverFeedback : MonoBehaviour
    {
        #region Singleton
        private static UnitHoverFeedback _instance;
        public static UnitHoverFeedback Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UnitHoverFeedback>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("UnitHoverFeedback");
                        _instance = obj.AddComponent<UnitHoverFeedback>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        #endregion

        #region Inspector Settings
        [Header("Raycasting Settings")]
        [Tooltip("Layers for unit detection")]
        [SerializeField] private LayerMask unitLayerMask = Physics.DefaultRaycastLayers;
        
        [Tooltip("Maximum raycast distance")]
        [SerializeField] private float maxRaycastDistance = 100f;
        
        [Tooltip("How frequently to update hover detection (in seconds)")]
        [SerializeField] private float updateFrequency = 0.05f;
        
        [Header("Hover Effects")]
        [Tooltip("Default hover effect prefab")]
        [SerializeField] private GameObject defaultHoverEffectPrefab;
        
        [Tooltip("Player unit hover effect prefab")]
        [SerializeField] private GameObject playerUnitEffectPrefab;
        
        [Tooltip("Enemy unit hover effect prefab")]
        [SerializeField] private GameObject enemyUnitEffectPrefab;
        
        [Tooltip("Should hover effect only be shown for interactable units in the current game state")]
        [SerializeField] private bool onlyShowForInteractableUnits = true;
        
        [Header("Card Targeting")]
        [Tooltip("Valid target highlight color")]
        [SerializeField] private Color validTargetColor = new Color(0.2f, 1f, 0.2f, 0.8f);
        
        [Tooltip("Invalid target highlight color")]
        [SerializeField] private Color invalidTargetColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        
        [Tooltip("Pulse intensity for valid targets")]
        [SerializeField] private float targetPulseIntensity = 0.3f;
        
        [Header("Debug")]
        [Tooltip("Enable debug logs")]
        [SerializeField] private bool enableDebugLogs = false;
        #endregion

        #region Private Variables
        private Camera _mainCamera;
        private float _lastUpdateTime;
        private Unit _currentHoveredUnit;
        private Dictionary<Unit, UnitHoverEffect> _activeEffects = new Dictionary<Unit, UnitHoverEffect>();
        private Dictionary<Unit.TeamType, GameObject> _teamEffectPrefabs = new Dictionary<Unit.TeamType, GameObject>();
        
        // Targeting state
        private bool _isInTargetingMode = false;
        private List<Unit> _validTargets = new List<Unit>();
        private CardData _currentTargetingCard;
        
        // Game state tracking
        private bool _isHoverEnabled = true;
        private GameManager.TurnState _lastTurnState = GameManager.TurnState.None;
        #endregion

        #region Events
        // Event raised when a unit is hovered
        public event Action<Unit> OnUnitHoverEnter;
        
        // Event raised when a unit is no longer hovered
        public event Action<Unit> OnUnitHoverExit;
        
        // Event raised when a valid target is clicked during targeting
        public event Action<Unit> OnTargetSelected;
        #endregion

        #region Initialization and Lifecycle
        private void Initialize()
        {
            // Reference the main camera
            _mainCamera = Camera.main;
            
            if (_mainCamera == null)
            {
                Debug.LogError("No main camera found! UnitHoverFeedback requires a camera tagged as 'MainCamera'.");
            }
            
            // Set up team effect prefabs
            if (playerUnitEffectPrefab != null)
            {
                _teamEffectPrefabs[Unit.TeamType.Player] = playerUnitEffectPrefab;
            }
            
            if (enemyUnitEffectPrefab != null)
            {
                _teamEffectPrefabs[Unit.TeamType.Enemy] = enemyUnitEffectPrefab;
            }
            
            // Subscribe to game manager events if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged += HandleTurnChanged;
                GameManager.Instance.OnUnitActivated += HandleUnitActivated;
                GameManager.Instance.OnUnitDeactivated += HandleUnitDeactivated;
            }
            
            // Subscribe to card system events if available
            CardSystem cardSystem = FindFirstObjectByType<CardSystem>();
            if (cardSystem != null)
            {
                cardSystem.OnCardSelected += HandleCardSelected;
                cardSystem.OnCardPlayed += HandleCardPlayed;
            }
            
            DebugLog("UnitHoverFeedback initialized");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from game manager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
                GameManager.Instance.OnUnitActivated -= HandleUnitActivated;
                GameManager.Instance.OnUnitDeactivated -= HandleUnitDeactivated;
            }
            
            // Unsubscribe from card system events
            CardSystem cardSystem = FindFirstObjectByType<CardSystem>();
            if (cardSystem != null)
            {
                cardSystem.OnCardSelected -= HandleCardSelected;
                cardSystem.OnCardPlayed -= HandleCardPlayed;
            }
            
            // Clean up any active effects
            foreach (var effect in _activeEffects.Values)
            {
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            }
            
            _activeEffects.Clear();
        }
        
        private void Update()
        {
            // Only process hover detection if enabled
            if (!_isHoverEnabled)
                return;
                
            // Update at the specified frequency
            if (Time.time - _lastUpdateTime >= updateFrequency)
            {
                _lastUpdateTime = Time.time;
                DetectHoveredUnit();
            }
            
            // Check for mouse click in targeting mode
            if (_isInTargetingMode && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleTargetingClick();
            }
        }
        #endregion

        #region Hover Detection
        /// <summary>
        /// Detects which unit (if any) is currently under the mouse cursor
        /// </summary>
        private void DetectHoveredUnit()
        {
            // Skip if no camera
            if (_mainCamera == null)
                return;
                
            // Get mouse position
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            // Create a ray from the camera through the mouse position
            Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
            
            // Cast the ray and check for hits
            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, unitLayerMask))
            {
                // Check if we hit a unit
                Unit hitUnit = hit.collider.GetComponentInParent<Unit>();
                
                // If we hit a unit different from the currently hovered one
                if (hitUnit != null && hitUnit != _currentHoveredUnit)
                {
                    // Check if we should show hover effect for this unit
                    if (ShouldShowHoverEffect(hitUnit))
                    {
                        // Exit hover on the previous unit if any
                        if (_currentHoveredUnit != null)
                        {
                            ExitHover(_currentHoveredUnit);
                        }
                        
                        // Enter hover on the new unit
                        EnterHover(hitUnit);
                        
                        // Update current hovered unit
                        _currentHoveredUnit = hitUnit;
                    }
                }
            }
            else if (_currentHoveredUnit != null)
            {
                // No unit hit, exit hover on the current unit
                ExitHover(_currentHoveredUnit);
                _currentHoveredUnit = null;
            }
        }
        
        /// <summary>
        /// Determines if hover effect should be shown for a unit based on game state
        /// </summary>
        private bool ShouldShowHoverEffect(Unit unit)
        {
            // Always show if not restricted to interactable units
            if (!onlyShowForInteractableUnits)
                return true;
                
            // Don't show effect for dead units
            if (!unit.IsAlive)
                return false;
                
            // Check if in targeting mode
            if (_isInTargetingMode && _currentTargetingCard != null)
            {
                // If we're in targeting mode, only show hover for valid targets
                bool canTargetSelf = _currentTargetingCard.CanTargetSelf;
                bool canTargetAllies = _currentTargetingCard.CanTargetAllies;
                bool canTargetEnemies = _currentTargetingCard.CanTargetEnemies;
                
                // Get active unit
                Unit activeUnit = GameManager.Instance?.ActiveUnit;
                
                if (activeUnit != null)
                {
                    // Self targeting check
                    if (unit == activeUnit && canTargetSelf)
                        return true;
                        
                    // Ally targeting check
                    if (unit != activeUnit && unit.Team == activeUnit.Team && canTargetAllies)
                        return true;
                        
                    // Enemy targeting check
                    if (unit.Team != activeUnit.Team && canTargetEnemies)
                        return true;
                }
                
                // Not a valid target for this card
                return false;
            }
            
            // Check game state
            if (GameManager.Instance != null)
            {
                GameManager.TurnState currentTurnState = GameManager.Instance.CurrentTurnState;
                
                // Only show hover effect for player units during player turn
                if (currentTurnState == GameManager.TurnState.PlayerTurn && unit.Team == Unit.TeamType.Player)
                {
                    // Check if unit has already acted this turn
                    if (UnitSelectionController.Instance != null && 
                        UnitSelectionController.Instance.HasUnitActed(unit))
                    {
                        return false;
                    }
                    
                    return true;
                }
                
                // Only show hover effect for enemy units during targeting
                if (currentTurnState == GameManager.TurnState.PlayerTurn && 
                    unit.Team == Unit.TeamType.Enemy)
                {
                    // Check if we're in targeting mode
                    if (CardSystem.Instance != null && 
                        CardSystem.Instance.SelectedCard != null)
                    {
                        // Check if the selected card can target enemies
                        return CardSystem.Instance.SelectedCard.CardData.CanTargetEnemies;
                    }
                }
            }
            
            // Default to false if none of the conditions are met
            return false;
        }
        
        /// <summary>
        /// Handles unit hover enter
        /// </summary>
        private void EnterHover(Unit unit)
        {
            if (unit == null)
                return;
                
            DebugLog($"Hover Enter: {unit.UnitName}");
            
            // Create hover effect if not already active
            if (!_activeEffects.ContainsKey(unit))
            {
                // Get appropriate effect prefab based on team
                GameObject effectPrefab = GetEffectPrefabForTeam(unit.Team);
                
                // Create effect instance
                if (effectPrefab != null)
                {
                    GameObject effectObject = Instantiate(effectPrefab, unit.transform);
                    UnitHoverEffect effect = effectObject.GetComponent<UnitHoverEffect>();
                    
                    if (effect == null)
                    {
                        // Add the component if it doesn't exist
                        effect = effectObject.AddComponent<UnitHoverEffect>();
                    }
                    
                    // Initialize the effect
                    effect.Initialize(unit);
                    
                    // Apply targeting color if in targeting mode
                    if (_isInTargetingMode)
                    {
                        bool isValidTarget = _validTargets.Contains(unit);
                        effect.UpdateColor(isValidTarget ? validTargetColor : invalidTargetColor);
                    }
                    
                    // Store in active effects
                    _activeEffects[unit] = effect;
                }
            }
            else
            {
                // Show existing effect
                _activeEffects[unit].Show();
                
                // Apply targeting color if in targeting mode
                if (_isInTargetingMode)
                {
                    bool isValidTarget = _validTargets.Contains(unit);
                    _activeEffects[unit].UpdateColor(isValidTarget ? validTargetColor : invalidTargetColor);
                }
            }
            
            // Raise event
            OnUnitHoverEnter?.Invoke(unit);
        }
        
        /// <summary>
        /// Handles unit hover exit
        /// </summary>
        private void ExitHover(Unit unit)
        {
            if (unit == null)
                return;
                
            DebugLog($"Hover Exit: {unit.UnitName}");
            
            // Hide hover effect if active
            if (_activeEffects.TryGetValue(unit, out UnitHoverEffect effect) && effect != null)
            {
                effect.Hide();
            }
            
            // Raise event
            OnUnitHoverExit?.Invoke(unit);
        }
        
        /// <summary>
        /// Gets the appropriate effect prefab for a team
        /// </summary>
        private GameObject GetEffectPrefabForTeam(Unit.TeamType team)
        {
            // Use team-specific prefab if available
            if (_teamEffectPrefabs.TryGetValue(team, out GameObject prefab) && prefab != null)
            {
                return prefab;
            }
            
            // Fall back to default prefab
            return defaultHoverEffectPrefab;
        }
        
        /// <summary>
        /// Handles clicks during targeting mode
        /// </summary>
        private void HandleTargetingClick()
        {
            if (_currentHoveredUnit != null && _validTargets.Contains(_currentHoveredUnit))
            {
                // Valid target clicked
                DebugLog($"Target selected: {_currentHoveredUnit.UnitName}");
                
                // Notify listeners
                OnTargetSelected?.Invoke(_currentHoveredUnit);
                
                // End targeting mode
                EndTargetingMode();
            }
        }
        #endregion

        #region Card Targeting
        /// <summary>
        /// Start targeting mode for a specific card
        /// </summary>
        public void StartTargetingMode(CardData cardData, Unit sourceUnit)
        {
            if (cardData == null || sourceUnit == null)
                return;
                
            _isInTargetingMode = true;
            _currentTargetingCard = cardData;
            
            // Determine valid targets
            _validTargets.Clear();
            
            bool canTargetSelf = cardData.CanTargetSelf;
            bool canTargetAllies = cardData.CanTargetAllies;
            bool canTargetEnemies = cardData.CanTargetEnemies;
            
            // Find all units in the scene
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            
            foreach (Unit unit in allUnits)
            {
                if (!unit.IsAlive)
                    continue;
                    
                // Self targeting
                if (unit == sourceUnit && canTargetSelf)
                {
                    _validTargets.Add(unit);
                    continue;
                }
                
                // Ally targeting
                if (unit != sourceUnit && unit.Team == sourceUnit.Team && canTargetAllies)
                {
                    _validTargets.Add(unit);
                    continue;
                }
                
                // Enemy targeting
                if (unit.Team != sourceUnit.Team && canTargetEnemies)
                {
                    _validTargets.Add(unit);
                    continue;
                }
            }
            
            // Check range restrictions
            if (cardData.EffectRange > 0)
            {
                Vector3 sourcePos = sourceUnit.transform.position;
                List<Unit> inRangeUnits = new List<Unit>();
                
                foreach (Unit unit in _validTargets)
                {
                    float distance = Vector3.Distance(sourcePos, unit.transform.position);
                    float maxRange = cardData.EffectRange * 1f; // Assuming 1 unit = 1 tile
                    
                    if (distance <= maxRange)
                    {
                        inRangeUnits.Add(unit);
                    }
                }
                
                _validTargets = inRangeUnits;
            }
            
            // Update hover effects for valid targets
            foreach (Unit unit in _validTargets)
            {
                if (_activeEffects.TryGetValue(unit, out UnitHoverEffect effect) && effect != null)
                {
                    effect.UpdateColor(validTargetColor);
                    effect.Show();
                }
                else
                {
                    // Create effect if it doesn't exist
                    GameObject effectPrefab = GetEffectPrefabForTeam(unit.Team);
                    
                    if (effectPrefab != null)
                    {
                        GameObject effectObject = Instantiate(effectPrefab, unit.transform);
                        UnitHoverEffect newEffect = effectObject.GetComponent<UnitHoverEffect>();
                        
                        if (newEffect == null)
                        {
                            newEffect = effectObject.AddComponent<UnitHoverEffect>();
                        }
                        
                        newEffect.Initialize(unit);
                        newEffect.UpdateColor(validTargetColor);
                        
                        _activeEffects[unit] = newEffect;
                    }
                }
            }
            
            DebugLog($"Started targeting mode: {cardData.CardName}, {_validTargets.Count} valid targets");
        }
        
        /// <summary>
        /// End targeting mode
        /// </summary>
        public void EndTargetingMode()
        {
            if (!_isInTargetingMode)
                return;
                
            _isInTargetingMode = false;
            _currentTargetingCard = null;
            
            // Hide all targeting effects except for currently hovered unit
            foreach (var pair in _activeEffects)
            {
                Unit unit = pair.Key;
                UnitHoverEffect effect = pair.Value;
                
                if (unit != _currentHoveredUnit)
                {
                    effect.Hide();
                }
                else
                {
                    // Reset color for hovered unit
                    effect.DetermineEffectColor();
                }
            }
            
            _validTargets.Clear();
            
            DebugLog("Ended targeting mode");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Register a team-specific effect prefab
        /// </summary>
        public void RegisterTeamEffectPrefab(Unit.TeamType team, GameObject effectPrefab)
        {
            if (effectPrefab != null)
            {
                _teamEffectPrefabs[team] = effectPrefab;
                DebugLog($"Registered effect prefab for team {team}");
            }
        }
        
        /// <summary>
        /// Enable or disable hover feedback
        /// </summary>
        public void SetHoverEnabled(bool enabled)
        {
            if (_isHoverEnabled == enabled)
                return;
                
            _isHoverEnabled = enabled;
            
            // Clear any active hover if disabled
            if (!enabled && _currentHoveredUnit != null)
            {
                ExitHover(_currentHoveredUnit);
                _currentHoveredUnit = null;
                
                // End targeting mode if active
                if (_isInTargetingMode)
                {
                    EndTargetingMode();
                }
            }
            
            DebugLog($"Hover feedback {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Refresh hover state for all units (useful after game state changes)
        /// </summary>
        public void RefreshHoverStates()
        {
            // Clear current hover
            if (_currentHoveredUnit != null)
            {
                ExitHover(_currentHoveredUnit);
                _currentHoveredUnit = null;
            }
            
            // Clear all active effects for units that shouldn't show them
            List<Unit> unitsToRemove = new List<Unit>();
            
            foreach (var pair in _activeEffects)
            {
                Unit unit = pair.Key;
                UnitHoverEffect effect = pair.Value;
                
                if (unit != null && !ShouldShowHoverEffect(unit))
                {
                    // Hide the effect
                    if (effect != null)
                    {
                        effect.Hide();
                    }
                    
                    unitsToRemove.Add(unit);
                }
            }
            
            // Remove units from active effects
            foreach (Unit unit in unitsToRemove)
            {
                _activeEffects.Remove(unit);
            }
        }
        
        /// <summary>
        /// Show hover effect for a specific unit (useful for scripted events)
        /// </summary>
        public void HighlightUnit(Unit unit, Color? customColor = null)
        {
            if (unit == null)
                return;
                
            // Create hover effect if not already active
            if (!_activeEffects.ContainsKey(unit))
            {
                // Get appropriate effect prefab based on team
                GameObject effectPrefab = GetEffectPrefabForTeam(unit.Team);
                
                // Create effect instance
                if (effectPrefab != null)
                {
                    GameObject effectObject = Instantiate(effectPrefab, unit.transform);
                    UnitHoverEffect effect = effectObject.GetComponent<UnitHoverEffect>();
                    
                    if (effect == null)
                    {
                        effect = effectObject.AddComponent<UnitHoverEffect>();
                    }
                    
                    // Initialize the effect
                    effect.Initialize(unit);
                    
                    // Apply custom color if provided
                    if (customColor.HasValue)
                    {
                        effect.UpdateColor(customColor.Value);
                    }
                    
                    // Store in active effects
                    _activeEffects[unit] = effect;
                }
            }
            else
            {
                // Show existing effect
                _activeEffects[unit].Show();
                
                // Apply custom color if provided
                if (customColor.HasValue)
                {
                    _activeEffects[unit].UpdateColor(customColor.Value);
                }
                else
                {
                    // Reset to default color
                    _activeEffects[unit].DetermineEffectColor();
                }
            }
            
            DebugLog($"Highlighted unit: {unit.UnitName}");
        }
        
        /// <summary>
        /// Remove highlight from a specific unit
        /// </summary>
        public void RemoveHighlight(Unit unit)
        {
            if (unit == null)
                return;
                
            // Hide hover effect if active
            if (_activeEffects.TryGetValue(unit, out UnitHoverEffect effect) && effect != null)
            {
                effect.Hide();
            }
            
            DebugLog($"Removed highlight from unit: {unit.UnitName}");
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle turn state changes from GameManager
        /// </summary>
        private void HandleTurnChanged(GameManager.TurnState newState)
        {
            // Update game state tracking
            _lastTurnState = newState;
            
            // Enable hover only during player turn
            SetHoverEnabled(newState == GameManager.TurnState.PlayerTurn);
            
            // Refresh hover states
            RefreshHoverStates();
            
            // End targeting mode if active
            if (_isInTargetingMode)
            {
                EndTargetingMode();
            }
        }
        
        /// <summary>
        /// Handle unit activation
        /// </summary>
        private void HandleUnitActivated(Unit unit)
        {
            // Refresh hover states when a new unit is activated
            RefreshHoverStates();
        }
        
        /// <summary>
        /// Handle unit deactivation
        /// </summary>
        private void HandleUnitDeactivated(Unit unit)
        {
            // End targeting mode if active
            if (_isInTargetingMode)
            {
                EndTargetingMode();
            }
            
            // Remove any active effects for this unit
            if (_activeEffects.TryGetValue(unit, out UnitHoverEffect effect) && effect != null)
            {
                effect.Hide();
            }
        }
        
        /// <summary>
        /// Handle card selection
        /// </summary>
        private void HandleCardSelected(Card card)
        {
            // End targeting mode if active
            if (_isInTargetingMode)
            {
                EndTargetingMode();
            }
            
            // If a targeting card is selected, start targeting mode
            if (card != null && card.CardData.RequiresTarget)
            {
                StartTargetingMode(card.CardData, card.Owner);
            }
        }
        
        /// <summary>
        /// Handle card played
        /// </summary>
        private void HandleCardPlayed(Card card)
        {
            // End targeting mode if active
            if (_isInTargetingMode)
            {
                EndTargetingMode();
            }
        }
        #endregion

        #region Utility Methods
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[UnitHoverFeedback] {message}");
            }
        }
        #endregion
    }
}