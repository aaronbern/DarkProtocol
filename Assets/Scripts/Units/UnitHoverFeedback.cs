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

        [Tooltip("Filter out common ground hit logs")]
        [SerializeField] private bool suppressGroundHitLogs = true;
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
                // Check if prefab is active
                if (!playerUnitEffectPrefab.activeSelf)
                {
                    Debug.LogWarning("[UnitHoverFeedback][CRITICAL] Player unit effect prefab is INACTIVE in prefab. This will cause coroutine errors!");
                    Debug.LogWarning("[UnitHoverFeedback][FIX] Select the prefab in your Project window and check the 'Active' checkbox at the top of the Inspector.");
                }
                _teamEffectPrefabs[Unit.TeamType.Player] = playerUnitEffectPrefab;
            }

            if (enemyUnitEffectPrefab != null)
            {
                // Check if prefab is active
                if (!enemyUnitEffectPrefab.activeSelf)
                {
                    Debug.LogWarning("[UnitHoverFeedback][CRITICAL] Enemy unit effect prefab is INACTIVE in prefab. This will cause coroutine errors!");
                    Debug.LogWarning("[UnitHoverFeedback][FIX] Select the prefab in your Project window and check the 'Active' checkbox at the top of the Inspector.");
                }
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
                    DebugLog($"Ray hit unit: {hitUnit.UnitName} [Team: {hitUnit.Team}]");

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
                    else
                    {
                        DebugLog($"Not showing hover effect for {hitUnit.UnitName} as it doesn't meet criteria");
                    }
                }
                else if (hitUnit == null && suppressGroundHitLogs)
                {
                    // Don't log anything for terrain/ground hits to avoid log spam
                }
            }
            else if (_currentHoveredUnit != null)
            {
                // No unit hit, exit hover on the current unit
                DebugLog($"No unit hit by ray, exiting hover on {_currentHoveredUnit.UnitName}");
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
            {
                DebugLog($"ShouldShowHoverEffect: TRUE for {unit.UnitName} - not restricted to interactable units");
                return true;
            }

            // Don't show effect for dead units
            if (!unit.IsAlive)
            {
                DebugLog($"ShouldShowHoverEffect: FALSE for {unit.UnitName} - unit is not alive");
                return false;
            }

            // Check if in targeting mode
            if (_isInTargetingMode && _currentTargetingCard != null)
            {
                // If we're in targeting mode, only show hover for valid targets
                bool canTargetSelf = _currentTargetingCard.CanTargetSelf;
                bool canTargetAllies = _currentTargetingCard.CanTargetAllies;
                bool canTargetEnemies = _currentTargetingCard.CanTargetEnemies;

                DebugLog($"Targeting mode active with card: {_currentTargetingCard.CardName}");
                DebugLog($" - Can target self: {canTargetSelf}, allies: {canTargetAllies}, enemies: {canTargetEnemies}");

                // Get active unit
                Unit activeUnit = GameManager.Instance?.ActiveUnit;

                if (activeUnit != null)
                {
                    // Self targeting check
                    if (unit == activeUnit && canTargetSelf)
                    {
                        DebugLog($"ShouldShowHoverEffect: TRUE for {unit.UnitName} - is active unit and can target self");
                        return true;
                    }

                    // Ally targeting check
                    if (unit != activeUnit && unit.Team == activeUnit.Team && canTargetAllies)
                    {
                        DebugLog($"ShouldShowHoverEffect: TRUE for {unit.UnitName} - is ally of active unit and can target allies");
                        return true;
                    }

                    // Enemy targeting check
                    if (unit.Team != activeUnit.Team && canTargetEnemies)
                    {
                        DebugLog($"ShouldShowHoverEffect: TRUE for {unit.UnitName} - is enemy of active unit and can target enemies");
                        return true;
                    }
                }
                else
                {
                    DebugLog($"WARNING: No active unit found while in targeting mode");
                }

                // Not a valid target for this card
                DebugLog($"ShouldShowHoverEffect: FALSE for {unit.UnitName} - not a valid target for the card");
                return false;
            }

            // Check game state
            if (GameManager.Instance != null)
            {
                GameManager.TurnState currentTurnState = GameManager.Instance.CurrentTurnState;
                DebugLog($"Current turn state: {currentTurnState}, Unit team: {unit.Team}");

                // Only show hover effect for player units during player turn
                if (currentTurnState == GameManager.TurnState.PlayerTurn && unit.Team == Unit.TeamType.Player)
                {
                    // Check if unit has already acted this turn
                    if (UnitSelectionController.Instance != null &&
                        UnitSelectionController.Instance.HasUnitActed(unit))
                    {
                        DebugLog($"ShouldShowHoverEffect: FALSE for {unit.UnitName} - player unit has already acted this turn");
                        return false;
                    }

                    DebugLog($"ShouldShowHoverEffect: TRUE for {unit.UnitName} - player unit during player turn that hasn't acted");
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
                        bool canTargetEnemies = CardSystem.Instance.SelectedCard.CardData.CanTargetEnemies;
                        DebugLog($"ShouldShowHoverEffect: {canTargetEnemies} for {unit.UnitName} - enemy unit with card selected that {(canTargetEnemies ? "can" : "cannot")} target enemies");
                        return canTargetEnemies;
                    }
                }
            }
            else
            {
                DebugLog($"WARNING: GameManager.Instance is null in ShouldShowHoverEffect");
            }

            // Default to false if none of the conditions are met
            DebugLog($"ShouldShowHoverEffect: FALSE for {unit.UnitName} - no matching conditions");
            return false;
        }

        /// <summary>
        /// Handles unit hover enter
        /// </summary>
        private void EnterHover(Unit unit)
        {
            if (unit == null)
                return;

            DebugLog($"Hover Enter: {unit.UnitName} [Team: {unit.Team}]");

            // Create hover effect if not already active
            if (!_activeEffects.ContainsKey(unit))
            {
                // Get appropriate effect prefab based on team
                GameObject effectPrefab = GetEffectPrefabForTeam(unit.Team);

                // Create effect instance
                if (effectPrefab != null)
                {
                    DebugLog($"Creating new hover effect for {unit.UnitName} using prefab {effectPrefab.name}");

                    // First check if prefab is active
                    if (!effectPrefab.activeSelf)
                    {
                        Debug.LogWarning($"[UnitHoverFeedback][CRITICAL] Prefab {effectPrefab.name} for team {unit.Team} is INACTIVE! This will cause coroutine errors.");
                        Debug.LogWarning($"[UnitHoverFeedback][FIX] Select prefab '{effectPrefab.name}' in Project view, check 'Active' checkbox at top of Inspector, and save prefab");
                    }

                    GameObject effectObject = Instantiate(effectPrefab, unit.transform);

                    // CRITICAL FIX: Make sure effect is active BEFORE trying to use it
                    effectObject.SetActive(true);

                    // Check if the prefab was inactive by default
                    bool wasInactive = !effectObject.activeSelf;
                    if (wasInactive)
                    {
                        Debug.LogWarning($"[UnitHoverFeedback][CRITICAL] Effect prefab was inactive by default for {unit.UnitName}. This is the cause of the coroutine error!");
                    }

                    DebugLog($"Activated GameObject for {unit.UnitName}, active state: {effectObject.activeSelf}");

                    UnitHoverEffect effect = effectObject.GetComponent<UnitHoverEffect>();

                    if (effect == null)
                    {
                        // Add the component if it doesn't exist
                        DebugLog($"No UnitHoverEffect component found on prefab for {unit.UnitName}, adding one");
                        effect = effectObject.AddComponent<UnitHoverEffect>();
                    }

                    try
                    {
                        // Initialize the effect
                        effect.Initialize(unit);

                        // MODIFICATION: Call Show() AFTER Initialize instead of relying on Initialize to do it
                        effect.Show();

                        // Apply targeting color if in targeting mode
                        if (_isInTargetingMode)
                        {
                            bool isValidTarget = _validTargets.Contains(unit);
                            DebugLog($"In targeting mode, unit {unit.UnitName} is a {(isValidTarget ? "valid" : "invalid")} target");
                            effect.UpdateColor(isValidTarget ? validTargetColor : invalidTargetColor);
                        }

                        // Store in active effects
                        _activeEffects[unit] = effect;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UnitHoverFeedback][CRITICAL] Error initializing hover effect for {unit.UnitName}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogError($"[UnitHoverFeedback][CRITICAL] No effect prefab found for {unit.UnitName} with team {unit.Team}");
                }
            }
            else
            {
                DebugLog($"Reusing existing hover effect for {unit.UnitName}");

                // FIX: Make sure the GameObject is active before showing
                if (_activeEffects[unit] != null && _activeEffects[unit].gameObject != null)
                {
                    bool wasInactive = !_activeEffects[unit].gameObject.activeSelf;
                    if (wasInactive)
                    {
                        DebugLog($"WARNING: Existing effect GameObject was inactive for {unit.UnitName}, activating it");
                    }

                    _activeEffects[unit].gameObject.SetActive(true);
                    DebugLog($"GameObject active state for {unit.UnitName}: {_activeEffects[unit].gameObject.activeSelf}");
                }
                else
                {
                    DebugLog($"ERROR: Effect exists in dictionary for {unit.UnitName} but the component or GameObject is null");
                }

                try
                {
                    // Show existing effect
                    DebugLog($"Calling Show() for {unit.UnitName}");
                    _activeEffects[unit].Show();

                    // Apply targeting color if in targeting mode
                    if (_isInTargetingMode)
                    {
                        bool isValidTarget = _validTargets.Contains(unit);
                        DebugLog($"In targeting mode, unit {unit.UnitName} is a {(isValidTarget ? "valid" : "invalid")} target");
                        _activeEffects[unit].UpdateColor(isValidTarget ? validTargetColor : invalidTargetColor);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnitHoverFeedback][CRITICAL] Error showing hover effect for {unit.UnitName}: {ex.Message}\n{ex.StackTrace}");
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

            DebugLog($"Hover Exit: {unit.UnitName} [Team: {unit.Team}]");

            // Hide hover effect if active
            if (_activeEffects.TryGetValue(unit, out UnitHoverEffect effect) && effect != null)
            {
                // FIX: Check if gameObject is active before calling Hide()
                if (effect.gameObject != null)
                {
                    DebugLog($"Found effect for {unit.UnitName}, GameObject active state: {effect.gameObject.activeSelf}");

                    if (effect.gameObject.activeInHierarchy)
                    {
                        DebugLog($"Calling Hide() for {unit.UnitName}");
                        effect.Hide();
                    }
                    else
                    {
                        DebugLog($"WARNING: Cannot call Hide() for {unit.UnitName} because GameObject is inactive");
                    }
                }
                else
                {
                    DebugLog($"ERROR: Effect for {unit.UnitName} has null GameObject");
                }
            }
            else
            {
                DebugLog($"No active effect found for {unit.UnitName} in ExitHover");
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

            DebugLog($"===== Starting Targeting Mode =====");
            DebugLog($"Card: {cardData.CardName}, Source Unit: {sourceUnit.UnitName}");
            DebugLog($"Can target - Self: {cardData.CanTargetSelf}, Allies: {cardData.CanTargetAllies}, Enemies: {cardData.CanTargetEnemies}");

            // Determine valid targets
            _validTargets.Clear();

            bool canTargetSelf = cardData.CanTargetSelf;
            bool canTargetAllies = cardData.CanTargetAllies;
            bool canTargetEnemies = cardData.CanTargetEnemies;

            // Find all units in the scene
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            DebugLog($"Found {allUnits.Length} total units in scene");

            foreach (Unit unit in allUnits)
            {
                if (!unit.IsAlive)
                {
                    DebugLog($"Skipping {unit.UnitName} - unit is not alive");
                    continue;
                }

                // Self targeting
                if (unit == sourceUnit && canTargetSelf)
                {
                    _validTargets.Add(unit);
                    DebugLog($"Added {unit.UnitName} as valid target (self)");
                    continue;
                }

                // Ally targeting
                if (unit != sourceUnit && unit.Team == sourceUnit.Team && canTargetAllies)
                {
                    _validTargets.Add(unit);
                    DebugLog($"Added {unit.UnitName} as valid target (ally)");
                    continue;
                }

                // Enemy targeting
                if (unit.Team != sourceUnit.Team && canTargetEnemies)
                {
                    _validTargets.Add(unit);
                    DebugLog($"Added {unit.UnitName} as valid target (enemy)");
                    continue;
                }

                DebugLog($"{unit.UnitName} is not a valid target for this card");
            }

            // Check range restrictions
            if (cardData.EffectRange > 0)
            {
                DebugLog($"Applying range restrictions. Max range: {cardData.EffectRange}");
                Vector3 sourcePos = sourceUnit.transform.position;
                List<Unit> inRangeUnits = new List<Unit>();

                foreach (Unit unit in _validTargets)
                {
                    float distance = Vector3.Distance(sourcePos, unit.transform.position);
                    float maxRange = cardData.EffectRange * 1f; // Assuming 1 unit = 1 tile

                    if (distance <= maxRange)
                    {
                        inRangeUnits.Add(unit);
                        DebugLog($"{unit.UnitName} is in range (distance: {distance:F2})");
                    }
                    else
                    {
                        DebugLog($"{unit.UnitName} is OUT of range (distance: {distance:F2})");
                    }
                }

                _validTargets = inRangeUnits;
                DebugLog($"After range check: {_validTargets.Count} valid targets");
            }

            // Update hover effects for valid targets
            DebugLog($"Updating hover effects for valid targets");
            foreach (Unit unit in _validTargets)
            {
                if (_activeEffects.TryGetValue(unit, out UnitHoverEffect effect) && effect != null)
                {
                    // FIX: Make sure the GameObject is active before showing
                    bool wasInactive = !effect.gameObject.activeSelf;
                    if (wasInactive)
                    {
                        DebugLog($"WARNING: Effect GameObject for {unit.UnitName} was inactive, activating it");
                    }

                    effect.gameObject.SetActive(true);
                    DebugLog($"Found existing effect for {unit.UnitName}, updating color to valid target");

                    effect.UpdateColor(validTargetColor);
                    effect.Show();
                }
                else
                {
                    // Create effect if it doesn't exist
                    GameObject effectPrefab = GetEffectPrefabForTeam(unit.Team);

                    if (effectPrefab != null)
                    {
                        DebugLog($"Creating new effect for valid target {unit.UnitName}");
                        GameObject effectObject = Instantiate(effectPrefab, unit.transform);

                        // FIX: Make sure the GameObject is active before proceeding
                        bool wasInactive = !effectObject.activeSelf;
                        if (wasInactive)
                        {
                            DebugLog($"WARNING: New effect GameObject for {unit.UnitName} was inactive by default, activating it");
                        }

                        effectObject.SetActive(true);

                        UnitHoverEffect newEffect = effectObject.GetComponent<UnitHoverEffect>();

                        if (newEffect == null)
                        {
                            DebugLog($"No UnitHoverEffect component found on prefab for {unit.UnitName}, adding one");
                            newEffect = effectObject.AddComponent<UnitHoverEffect>();
                        }

                        newEffect.Initialize(unit);
                        newEffect.UpdateColor(validTargetColor);

                        _activeEffects[unit] = newEffect;
                    }
                    else
                    {
                        DebugLog($"ERROR: No effect prefab found for team {unit.Team}");
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

            DebugLog($"===== Ending Targeting Mode =====");

            _isInTargetingMode = false;
            _currentTargetingCard = null;

            // Hide all targeting effects except for currently hovered unit
            foreach (var pair in _activeEffects)
            {
                Unit unit = pair.Key;
                UnitHoverEffect effect = pair.Value;

                if (unit != _currentHoveredUnit)
                {
                    // FIX: Check if gameObject is active before calling Hide()
                    if (effect != null && effect.gameObject != null)
                    {
                        if (effect.gameObject.activeInHierarchy)
                        {
                            DebugLog($"Hiding effect for {unit.UnitName} (not currently hovered)");
                            effect.Hide();
                        }
                        else
                        {
                            DebugLog($"Cannot hide effect for {unit.UnitName} - GameObject is inactive");
                        }
                    }
                    else
                    {
                        DebugLog($"ERROR: Effect or GameObject is null for {unit.UnitName}");
                    }
                }
                else
                {
                    // Reset color for hovered unit
                    if (effect != null && effect.gameObject != null && effect.gameObject.activeInHierarchy)
                    {
                        DebugLog($"Resetting color for currently hovered unit: {unit.UnitName}");
                        effect.DetermineEffectColor();
                    }
                    else
                    {
                        DebugLog($"Cannot reset color for hovered unit {unit.UnitName} - effect or GameObject issue");
                    }
                }
            }

            DebugLog($"Clearing {_validTargets.Count} valid targets");
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
                    if (effect != null && effect.gameObject.activeInHierarchy)
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

                    // FIX: Make sure the GameObject is active before proceeding
                    effectObject.SetActive(true);

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
                // FIX: Make sure the GameObject is active before showing
                if (_activeEffects[unit] != null && _activeEffects[unit].gameObject != null)
                {
                    _activeEffects[unit].gameObject.SetActive(true);
                }

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
                // FIX: Check if gameObject is active before calling Hide()
                if (effect.gameObject.activeInHierarchy)
                {
                    effect.Hide();
                }
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
                // FIX: Check if gameObject is active before calling Hide()
                if (effect.gameObject.activeInHierarchy)
                {
                    effect.Hide();
                }
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