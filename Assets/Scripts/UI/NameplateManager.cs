using System.Collections.Generic;
using DarkProtocol.Cards;
using UnityEngine;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Manages all unit nameplates in the scene
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
        [SerializeField] private GameObject nameplatePrefab;
        [SerializeField] private Transform nameplateContainer;
        [SerializeField] private bool showPlayerNameplates = true;
        [SerializeField] private bool showEnemyNameplates = true;
        [SerializeField] private bool showNeutralNameplates = true;
        [SerializeField] private bool onlyShowNameplatesInCombat = false;

        [Header("Visibility")]
        [SerializeField] private float nameplateVisibilityDistance = 30f;
        [SerializeField] private bool fadeNameplatesWithDistance = true;
        [SerializeField] private AnimationCurve distanceFadeCurve;

        private Dictionary<Unit, UnitNameplate> _nameplates = new Dictionary<Unit, UnitNameplate>();
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

            // Create container if needed
            if (nameplateContainer == null)
            {
                GameObject container = new GameObject("Nameplates");
                container.transform.SetParent(transform);
                nameplateContainer = container.transform;
            }
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

            if (nameplatePrefab != null)
            {
                // Instantiate in world space, not as UI child
                GameObject nameplateObj = Instantiate(nameplatePrefab);

                // Position above the unit
                Vector3 unitPosition = unit.transform.position;
                nameplateObj.transform.position = unitPosition + Vector3.up * 2.5f; // Adjust height as needed

                UnitNameplate nameplate = nameplateObj.GetComponent<UnitNameplate>();
                if (nameplate != null)
                {
                    nameplate.Initialize(unit);
                    _nameplates[unit] = nameplate;
                }
            }
        }

        /// <summary>
        /// Remove a nameplate for a unit
        /// </summary>
        public void RemoveNameplateForUnit(Unit unit)
        {
            if (unit == null || !_nameplates.ContainsKey(unit))
                return;

            UnitNameplate nameplate = _nameplates[unit];
            _nameplates.Remove(unit);

            if (nameplate != null)
            {
                Destroy(nameplate.gameObject);
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
                UnitNameplate nameplate = kvp.Value;

                if (unit == null || nameplate == null)
                    continue;

                // Check team filter
                if (teamFilter.HasValue && unit.Team != teamFilter.Value)
                    continue;

                // Update visibility
                bool shouldShow = ShouldShowNameplate(unit);
                nameplate.gameObject.SetActive(shouldShow);
            }
        }

        /// <summary>
        /// Update status effects for a specific unit
        /// </summary>
        public void UpdateUnitStatusIcons(Unit unit)
        {
            if (unit == null || !_nameplates.ContainsKey(unit))
                return;

            StatusEffectManager statusManager = unit.GetComponent<StatusEffectManager>();
            if (statusManager != null)
            {
                _nameplates[unit].UpdateStatusIcons(statusManager.GetAllActiveEffects());
            }
        }

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
            foreach (var nameplate in _nameplates.Values)
            {
                if (nameplate != null)
                {
                    Destroy(nameplate.gameObject);
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