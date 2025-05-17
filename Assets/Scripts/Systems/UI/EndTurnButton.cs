using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the End Turn button UI element.
/// Connects the UI to the game's turn management systems.
/// </summary>
public class EndTurnButton : MonoBehaviour
{
    #region Inspector References
    
    [Tooltip("Reference to the button component")]
    [SerializeField] private Button endTurnButton;
    
    [Tooltip("Optional visual effect when turn can be ended")]
    [SerializeField] private GameObject pulseEffect;
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        if (endTurnButton == null)
        {
            endTurnButton = GetComponent<Button>();
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }
        
        // Subscribe to game manager turn events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
        }
        
        // Initialize button state
        UpdateButtonState();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnEndTurnButtonClicked);
        }
    }
    
    #endregion

    #region Button Logic
    
    /// <summary>
    /// Called when the end turn button is clicked
    /// </summary>
    private void OnEndTurnButtonClicked()
    {
        // Check if a unit is currently selected
        if (UnitSelectionController.Instance != null && 
            UnitSelectionController.Instance.CurrentlySelectedUnit != null)
        {
            // End the current unit's turn
            Unit currentUnit = UnitSelectionController.Instance.CurrentlySelectedUnit;
            currentUnit.EndTurn();
            
            // Notify the selection controller that this unit is done
            UnitSelectionController.Instance.OnUnitFinishedTurn(currentUnit);
        }
        else
        {
            // If no unit is selected, just end the player turn
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEndTurnButtonPressed();
            }
        }
    }
    
    /// <summary>
    /// Updates the button state based on game state
    /// </summary>
    private void UpdateButtonState()
    {
        bool isPlayerTurn = GameManager.Instance != null && GameManager.Instance.IsPlayerTurn();
        
        // Enable the button only during player turn
        if (endTurnButton != null)
        {
            endTurnButton.interactable = isPlayerTurn;
        }
        
        // Show/hide pulse effect
        if (pulseEffect != null)
        {
            pulseEffect.SetActive(isPlayerTurn);
        }
    }
    
    /// <summary>
    /// Handles turn state changes
    /// </summary>
    private void HandleTurnChanged(GameManager.TurnState newState)
    {
        UpdateButtonState();
    }
    
    #endregion
}