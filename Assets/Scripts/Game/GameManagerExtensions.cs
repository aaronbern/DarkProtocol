using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkProtocol.Grid;
using DarkProtocol.Cards;

/// <summary>
/// Enhanced GameManager extensions for Dark Protocol
/// Adds methods needed to fix compilation errors and integrate with the card system
/// </summary>
public static class GameManagerExtensions
{
    /// <summary>
    /// Adds the missing OnEndTurnButtonPressed method to GameManager
    /// Referenced in Assets\Scripts\UI\EndTurnButton.cs(85,38)
    /// </summary>
    /// <param name="gameManager">The game manager instance</param>
    public static void OnEndTurnButtonPressed(this GameManager gameManager)
    {
        // Call the existing EndPlayerTurn method
        gameManager.EndPlayerTurn();
        
        Debug.Log("[GameManagerExtensions] End turn button pressed");
    }
    
    /// <summary>
    /// Adds the missing StartPlayerTurn method to GameManager
    /// Referenced in Assets\Scripts\Systems\Grid\Utils\DebugUtilities.cs(78,38)
    /// </summary>
    /// <param name="gameManager">The game manager instance</param>
    public static void StartPlayerTurn(this GameManager gameManager)
    {
        // Only make the change if not already in player turn
        if (gameManager.CurrentTurnState != GameManager.TurnState.PlayerTurn)
        {
            // Force a transition to player turn start
            // We can use reflection to call the private StartTurnTransition method
            // This is a bit of a hack, but it works for demonstration purposes
            
            Type gameManagerType = typeof(GameManager);
            var method = gameManagerType.GetMethod("StartTurnTransition", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (method != null)
            {
                method.Invoke(gameManager, new object[] { GameManager.TurnState.PlayerTurnStart });
                Debug.Log("[GameManagerExtensions] Forced transition to player turn");
            }
            else
            {
                Debug.LogError("[GameManagerExtensions] Failed to find StartTurnTransition method via reflection");
                
                // Alternative approach: set the CurrentTurnState directly
                // This is even more hacky but could work in a pinch
                var field = gameManagerType.GetField("CurrentTurnState", 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                    
                if (field != null)
                {
                    // Set the current state to PlayerTurn
                    field.SetValue(gameManager, GameManager.TurnState.PlayerTurn);
                    
                    // Call any event handlers manually
                    var eventField = gameManagerType.GetField("OnTurnChanged", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                        
                    if (eventField != null)
                    {
                        var eventDelegate = eventField.GetValue(gameManager) as GameManager.TurnChangedHandler;
                        eventDelegate?.Invoke(GameManager.TurnState.PlayerTurn);
                    }
                    
                    Debug.Log("[GameManagerExtensions] Directly set player turn state");
                }
                else
                {
                    Debug.LogError("[GameManagerExtensions] Could not find a way to force player turn");
                }
            }
        }
        else
        {
            Debug.Log("[GameManagerExtensions] Already in player turn");
        }
    }
    
    /// <summary>
    /// Helper method to integrate with the card system
    /// </summary>
    /// <param name="gameManager">The game manager instance</param>
    /// <param name="unit">The unit to draw cards for</param>
    public static void DrawCardsForUnit(this GameManager gameManager, Unit unit)
    {
        if (unit == null)
            return;
            
        // Find the card system
        CardSystem cardSystem = UnityEngine.Object.FindFirstObjectByType<DarkProtocol.Cards.CardSystem>();
        
        if (cardSystem != null)
        {
            // Draw cards for the unit
            cardSystem.DrawHandForUnit(unit);
            Debug.Log($"[GameManagerExtensions] Drew cards for {unit.UnitName}");
        }
    }
    
    /// <summary>
    /// Helper method to end the current player's card actions
    /// </summary>
    /// <param name="gameManager">The game manager instance</param>
    public static void EndCardActions(this GameManager gameManager)
    {
        // Find the card system
        CardSystem cardSystem = UnityEngine.Object.FindFirstObjectByType<DarkProtocol.Cards.CardSystem>();
        
        if (cardSystem != null)
        {
            // Deselect any selected card
            cardSystem.DeselectCard();
            
            Debug.Log("[GameManagerExtensions] Ended card actions");
        }
    }
    
    /// <summary>
    /// Helper method to play a card programmatically
    /// </summary>
    /// <param name="gameManager">The game manager instance</param>
    /// <param name="cardIndex">The index of the card in the active unit's hand</param>
    /// <param name="target">The target unit for the card</param>
    public static bool PlayCard(this GameManager gameManager, int cardIndex, Unit target = null)
    {
        // Find the card system
        CardSystem cardSystem = UnityEngine.Object.FindFirstObjectByType<DarkProtocol.Cards.CardSystem>();
        
        if (cardSystem != null && gameManager.ActiveUnit != null)
        {
            // Get the hand for the active unit
            List<DarkProtocol.Cards.Card> hand = cardSystem.GetHandForUnit(gameManager.ActiveUnit);
            
            // Check if the index is valid
            if (cardIndex >= 0 && cardIndex < hand.Count)
            {
                // Get the card
                DarkProtocol.Cards.Card card = hand[cardIndex];
                
                // Play the card
                bool success = cardSystem.PlayCard(card, target);
                
                Debug.Log($"[GameManagerExtensions] {(success ? "Successfully played" : "Failed to play")} card at index {cardIndex}");
                
                return success;
            }
            else
            {
                Debug.LogError($"[GameManagerExtensions] Invalid card index: {cardIndex}. Hand has {hand.Count} cards.");
            }
        }
        
        return false;
    }
}