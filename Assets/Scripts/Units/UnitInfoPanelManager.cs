using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the unit info panel and unit turn state
/// </summary>
public class UnitInfoPanelManager : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private TextMeshProUGUI movementPointsText;
    [SerializeField] private Image healthBar;
    [SerializeField] private Image actionPointsBar;

    [Header("Start Turn Button")]
    [SerializeField] private StartUnitTurnButton startTurnButton;

    private Unit _currentUnit;

    private void OnEnable()
    {
        if (_currentUnit != null)
        {
            SubscribeToUnitEvents(_currentUnit);
        }
    }

    private void OnDisable()
    {
        if (_currentUnit != null)
        {
            UnsubscribeFromUnitEvents(_currentUnit);
        }
    }

    /// <summary>
    /// Update the panel with a unit's information
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

            // Setup start turn button
            SetupStartTurnButton();
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
    /// Set up the start turn button
    /// </summary>
    private void SetupStartTurnButton()
    {
        if (startTurnButton == null || _currentUnit == null)
            return;

        // Determine if button should be shown
        bool canStartTurn = GameManager.Instance != null &&
                          GameManager.Instance.IsPlayerTurn() &&
                          _currentUnit.Team == Unit.TeamType.Player &&
                          Unit.SelectedUnit == _currentUnit &&
                          !_currentUnit.HasStartedTurn &&
                          UnitSelectionController.Instance != null &&
                          !UnitSelectionController.Instance.HasUnitActed(_currentUnit);

        if (canStartTurn)
        {
            startTurnButton.SetupForUnit(_currentUnit);
        }
        else
        {
            startTurnButton.gameObject.SetActive(false);
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
        unit.OnTurnStarted += HandleTurnStarted;
        unit.OnTurnEnded += HandleTurnEnded;
    }

    /// <summary>
    /// Unsubscribe from unit events
    /// </summary>
    private void UnsubscribeFromUnitEvents(Unit unit)
    {
        unit.OnHealthChanged -= HandleHealthChanged;
        unit.OnActionPointsChanged -= HandleActionPointsChanged;
        unit.OnMovementPointsChanged -= HandleMovementPointsChanged;
        unit.OnTurnStarted -= HandleTurnStarted;
        unit.OnTurnEnded -= HandleTurnEnded;
    }

    // Event handlers
    private void HandleHealthChanged(int newHealth, int oldHealth) => RefreshUI();
    private void HandleActionPointsChanged(int newAP, int oldAP) => RefreshUI();
    private void HandleMovementPointsChanged(int newMP, int oldMP) => RefreshUI();

    private void HandleTurnStarted()
    {
        RefreshUI();
        SetupStartTurnButton();
    }

    private void HandleTurnEnded()
    {
        RefreshUI();
        SetupStartTurnButton();
    }
}