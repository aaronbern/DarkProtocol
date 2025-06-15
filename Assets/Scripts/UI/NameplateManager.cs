using System.Collections.Generic;
using DarkProtocol.Cards;
using UnityEngine;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Manages all unit nameplates in the scene - Updated for modern UI Toolkit nameplates
    /// Location: Assets/Scripts/UI/NameplateManager.cs
    /// </summary>
    public class NameplateManager : MonoBehaviour
    {
        #region Singleton
        private static NameplateManager _instance;
        public static NameplateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NameplateManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("NameplateManager");
                        _instance = obj.AddComponent<NameplateManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("Nameplate Settings")]
        [SerializeField] private bool useModernNameplates = true;
        [SerializeField] private bool showPlayerNameplates = true;
        [SerializeField] private bool showEnemyNameplates = true;
        [SerializeField] private bool showNeutralNameplates = true;
        [SerializeField] private bool onlyShowNameplatesInCombat = false;

        [Header("Visibility")]
        [SerializeField] private float nameplateVisibilityDistance = 30f;
        [SerializeField] private bool fadeNameplatesWithDistance = true;
        [SerializeField] private AnimationCurve distanceFadeCurve;

        // Fixed: Use GameObject instead of MonoBehaviour to avoid type errors
        private Dictionary<Unit, GameObject> _nameplates = new Dictionary<Unit, GameObject>();
        private Camera _mainCamera;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            // Find all existing units and create nameplates
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            foreach (Unit unit in allUnits)
            {
                CreateNameplateForUnit(unit);
            }

            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
        }

        /// <summary>
        /// Create a nameplate for a unit
        /// </summary>
        public void CreateNameplateForUnit(Unit unit)
        {
            if (unit == null || _nameplates.ContainsKey(unit))
                return;

            if (!ShouldShowNameplate(unit))
                return;

            GameObject nameplateObj = null;

            if (useModernNameplates)
            {
                // Create modern UI Toolkit nameplate
                nameplateObj = new GameObject($"ModernNameplate_{unit.UnitName}");
                
                // Position above the unit
                Vector3 unitPosition = unit.transform.position;
                Renderer unitRenderer = unit.GetComponentInChildren<Renderer>();
                float unitHeight = unitRenderer?.bounds.size.y ?? 1.8f;
                nameplateObj.transform.position = unitPosition + Vector3.up * (unitHeight + 0.5f);
                
                // Add the modern nameplate component
                var modernNameplate = nameplateObj.AddComponent<WorldSpaceUIToolkitNameplate>();
                modernNameplate.Initialize(unit);
            }
            else
            {
                // Create simple fallback nameplate
                nameplateObj = new GameObject($"SimpleNameplate_{unit.UnitName}");
                
                // Position above the unit  
                Vector3 unitPosition = unit.transform.position;
                nameplateObj.transform.position = unitPosition + Vector3.up * 2.5f;
                
                Debug.LogWarning($"Using simple nameplate for {unit.UnitName} - enable 'Use Modern Nameplates' for better visuals");
            }

            if (nameplateObj != null)
            {
                _nameplates[unit] = nameplateObj;
            }
        }

        /// <summary>
        /// Remove a nameplate for a unit
        /// </summary>
        public void RemoveNameplateForUnit(Unit unit)
        {
            if (unit == null || !_nameplates.ContainsKey(unit))
                return;

            GameObject nameplateObj = _nameplates[unit];
            _nameplates.Remove(unit);

            if (nameplateObj != null)
            {
                Destroy(nameplateObj);
            }
        }

        /// <summary>
        /// Update nameplate visibility settings
        /// </summary>
        public void UpdateNameplateVisibility(Unit.TeamType? teamFilter = null)
        {
            foreach (var kvp in _nameplates)
            {
                Unit unit = kvp.Key;
                GameObject nameplateObj = kvp.Value;

                if (unit == null || nameplateObj == null)
                    continue;

                // Check team filter
                if (teamFilter.HasValue && unit.Team != teamFilter.Value)
                    continue;

                // Update visibility
                bool shouldShow = ShouldShowNameplate(unit);
                nameplateObj.SetActive(shouldShow);
            }
        }

        /// <summary>
        /// Update status effects for a specific unit
        /// </summary>
        public void UpdateUnitStatusIcons(Unit unit)
        {
            if (unit == null || !_nameplates.ContainsKey(unit))
                return;

            var nameplateObj = _nameplates[unit];
            
            // Try to update modern nameplate
            var modernNameplate = nameplateObj.GetComponent<WorldSpaceUIToolkitNameplate>();
            if (modernNameplate != null)
            {
                StatusEffectManager statusManager = unit.GetComponent<StatusEffectManager>();
                if (statusManager != null)
                {
                    modernNameplate.UpdateStatusIcons(statusManager.GetAllActiveEffects());
                }
            }
            
            // Try to update legacy nameplate (if you still have any)
            var legacyNameplate = nameplateObj.GetComponent<UnitNameplate>();
            if (legacyNameplate != null)
            {
                StatusEffectManager statusManager = unit.GetComponent<StatusEffectManager>();
                if (statusManager != null)
                {
                    legacyNameplate.UpdateStatusIcons(statusManager.GetAllActiveEffects());
                }
            }
        }

        /// <summary>
        /// Determines if a nameplate should be shown for a unit
        /// </summary>
        private bool ShouldShowNameplate(Unit unit)
        {
            if (unit == null)
                return false;

            // Check team visibility settings
            switch (unit.Team)
            {
                case Unit.TeamType.Player:
                    if (!showPlayerNameplates) return false;
                    break;
                case Unit.TeamType.Enemy:
                    if (!showEnemyNameplates) return false;
                    break;
                case Unit.TeamType.Neutral:
                    if (!showNeutralNameplates) return false;
                    break;
            }

            // Check combat state
            if (onlyShowNameplatesInCombat && GameManager.Instance != null)
            {
                bool inCombat = GameManager.Instance.CurrentTurnState == GameManager.TurnState.PlayerTurn ||
                               GameManager.Instance.CurrentTurnState == GameManager.TurnState.EnemyTurn;

                if (!inCombat)
                    return false;
            }

            // Check distance
            if (_mainCamera != null && nameplateVisibilityDistance > 0)
            {
                float distance = Vector3.Distance(_mainCamera.transform.position, unit.transform.position);
                if (distance > nameplateVisibilityDistance)
                    return false;
            }

            return true;
        }

        private void HandleTurnChanged(GameManager.TurnState newState)
        {
            // Update visibility if we're only showing in combat
            if (onlyShowNameplatesInCombat)
            {
                UpdateNameplateVisibility();
            }
        }

        private void OnDestroy()
        {
            // Clean up
            foreach (var nameplateObj in _nameplates.Values)
            {
                if (nameplateObj != null)
                {
                    Destroy(nameplateObj);
                }
            }

            _nameplates.Clear();

            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
        }
    }
}