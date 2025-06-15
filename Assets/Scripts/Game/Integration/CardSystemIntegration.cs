using System.Collections.Generic;
using UnityEngine;
using DarkProtocol.Cards;
using DarkProtocol.UI;

namespace DarkProtocol.Integration
{
    /// <summary>
    /// Integration helper to connect the enhanced card UI system with the existing Dark Protocol architecture
    /// Handles initialization, event routing, and system coordination
    /// </summary>
    public class CardSystemIntegrator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CardToolbar cardToolbar;
        [SerializeField] private GameObject enhancedCardUIPrefab;
        [SerializeField] private Canvas mainCanvas;

        [Header("Integration Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool replaceExistingCardUI = true;
        [SerializeField] private LayerMask cardTargetingLayers = -1;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // System references
        private CardSystem _cardSystem;
        private GameManager _gameManager;
        private Dictionary<Card, EnhancedCardUI> _cardUIMapping = new Dictionary<Card, EnhancedCardUI>();

        #region Unity Lifecycle

        private void Awake()
        {
            // Find system references
            _cardSystem = CardSystem.Instance;
            _gameManager = GameManager.Instance;

            // Find UI references if not assigned
            if (cardToolbar == null)
                cardToolbar = FindFirstObjectByType<CardToolbar>();

            if (mainCanvas == null)
                mainCanvas = FindFirstObjectByType<Canvas>();
        }

        private void Start()
        {
            if (autoInitialize)
            {
                InitializeIntegration();
            }
        }

        private void OnDestroy()
        {
            CleanupIntegration();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the integration between enhanced UI and card system
        /// </summary>
        public void InitializeIntegration()
        {
            DebugLog("Initializing Card System Integration");

            // Replace existing card UI if requested
            if (replaceExistingCardUI)
            {
                DisableExistingCardUI();
            }

            // Subscribe to card system events
            if (_cardSystem != null)
            {
                _cardSystem.OnHandDrawn += HandleHandDrawn;
                _cardSystem.OnCardSelected += HandleCardSelected;
                _cardSystem.OnCardPlayed += HandleCardPlayed;
                _cardSystem.OnCardDiscarded += HandleCardDiscarded;
            }

            // Subscribe to game manager events
            if (_gameManager != null)
            {
                _gameManager.OnTurnChanged += HandleTurnChanged;
                _gameManager.OnUnitActivated += HandleUnitActivated;
                _gameManager.OnUnitDeactivated += HandleUnitDeactivated;
            }

            // Subscribe to toolbar events
            if (cardToolbar != null)
            {
                // cardToolbar.OnCardSelected += HandleToolbarCardSelected; // Removed: CardToolbar does not define OnCardSelected
                cardToolbar.OnCardPlayed += (card) =>
                {
                    // Find the EnhancedCardUI for this card, if available
                    EnhancedCardUI cardUI = GetCardUI(card);
                    if (cardUI != null)
                    {
                        // Use Vector3.zero as a placeholder for targetPosition if not available
                        HandleToolbarCardPlayed(cardUI, Vector3.zero);
                    }
                };
                // cardToolbar.OnEndTurnPressed += HandleEndTurnPressed; // Removed: CardToolbar does not define OnEndTurnPressed
            }

            DebugLog("Integration initialized successfully");
        }

        /// <summary>
        /// Clean up the integration
        /// </summary>
        public void CleanupIntegration()
        {
            // Unsubscribe from events
            if (_cardSystem != null)
            {
                _cardSystem.OnHandDrawn -= HandleHandDrawn;
                _cardSystem.OnCardSelected -= HandleCardSelected;
                _cardSystem.OnCardPlayed -= HandleCardPlayed;
                _cardSystem.OnCardDiscarded -= HandleCardDiscarded;
            }

            if (_gameManager != null)
            {
                _gameManager.OnTurnChanged -= HandleTurnChanged;
                _gameManager.OnUnitActivated -= HandleUnitActivated;
                _gameManager.OnUnitDeactivated -= HandleUnitDeactivated;
            }

            if (cardToolbar != null)
            {
                // cardToolbar.OnCardSelected -= HandleToolbarCardSelected; // Removed: CardToolbar does not define OnCardSelected
                // Unsubscribe using a lambda or compatible method if one was used for subscription
                // cardToolbar.OnCardPlayed -= (card) => HandleToolbarCardPlayed(...); // Uncomment and match the subscription if needed
                // cardToolbar.OnEndTurnPressed -= HandleEndTurnPressed; // Removed: CardToolbar does not define OnEndTurnPressed
            }

            // Clear mappings
            _cardUIMapping.Clear();
        }

        /// <summary>
        /// Manually trigger a card draw animation
        /// </summary>
        public void DrawCardsForUnit(Unit unit)
        {
            if (_cardSystem != null && unit != null)
            {
                _cardSystem.DrawHandForUnit(unit);
            }
        }

        #endregion

        #region Event Handlers - Card System

        private void HandleHandDrawn(List<Card> hand)
        {
            DebugLog($"Hand drawn with {hand.Count} cards");

            // Clear existing toolbar cards
            if (cardToolbar != null)
            {
                cardToolbar.ClearCards();
            }

            // Clear mappings
            _cardUIMapping.Clear();

            // Add cards to toolbar
            foreach (var card in hand)
            {
                if (cardToolbar != null)
                {
                    cardToolbar.AddCard(card);
                }
            }
        }

        private void HandleCardSelected(Card card)
        {
            DebugLog($"Card selected: {card?.CardName ?? "null"}");

            // Find the corresponding UI element
            if (card != null && _cardUIMapping.TryGetValue(card, out EnhancedCardUI cardUI))
            {
                // The toolbar will handle the visual selection
                // This is just for system state tracking
            }
        }

        private void HandleCardPlayed(Card card)
        {
            DebugLog($"Card played: {card?.CardName ?? "null"}");

            // Remove from toolbar
            if (cardToolbar != null)
            {
                cardToolbar.RemoveCard(card);
            }

            // Remove from mapping
            _cardUIMapping.Remove(card);
        }

        private void HandleCardDiscarded(Card card)
        {
            DebugLog($"Card discarded: {card?.CardName ?? "null"}");

            // Remove from toolbar
            if (cardToolbar != null)
            {
                cardToolbar.RemoveCard(card);
            }

            // Remove from mapping
            _cardUIMapping.Remove(card);
        }

        #endregion

        #region Event Handlers - Game Manager

        private void HandleTurnChanged(GameManager.TurnState newState)
        {
            DebugLog($"Turn changed to: {newState}");

            // The toolbar handles its own visibility based on turn state
            // This is for any additional integration logic needed
        }

        private void HandleUnitActivated(Unit unit)
        {
            DebugLog($"Unit activated: {unit?.UnitName ?? "null"}");

            // Update toolbar with the new unit
            if (cardToolbar != null)
            {
                // Clear and redraw cards for the new unit
                cardToolbar.ClearCards();
                if (_cardSystem != null && unit != null)
                {
                    var hand = _cardSystem.GetHandForUnit(unit);
                    if (hand != null)
                    {
                        foreach (var card in hand)
                        {
                            cardToolbar.AddCard(card);
                        }
                    }
                }
            }
        }

        private void HandleUnitDeactivated(Unit unit)
        {
            DebugLog($"Unit deactivated: {unit?.UnitName ?? "null"}");

            // Clear toolbar if this was the active unit
            if (cardToolbar != null)
            {
                cardToolbar.ClearCards();
            }
        }

        #endregion

        #region Event Handlers - Toolbar

        private void HandleToolbarCardSelected(EnhancedCardUI cardUI)
        {
            if (cardUI == null) return;

            Card card = cardUI.GetCardInstance();
            if (card != null && _cardSystem != null)
            {
                // Update card system selection
                // Note: This would need to be added to your CardSystem
                // For now, we'll just track it locally
                DebugLog($"Toolbar card selected: {card.CardName}");
            }
        }

        private void HandleToolbarCardPlayed(EnhancedCardUI cardUI, Vector3 targetPosition)
        {
            if (cardUI == null) return;

            Card card = cardUI.GetCardInstance();
            if (card == null || _cardSystem == null) return;

            // Determine if we need a target
            CardData cardData = card.CardData;

            if (cardData.RequiresTarget)
            {
                // Try to find a unit at the target position
                Unit targetUnit = null;

                // Raycast to find target
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, cardTargetingLayers))
                {
                    targetUnit = hit.collider.GetComponent<Unit>();
                }

                // Play the card
                if (targetUnit != null || !cardData.RequiresTarget)
                {
                    bool success = _cardSystem.PlayCard(card, targetUnit);
                    DebugLog($"Card play {(success ? "succeeded" : "failed")}: {card.CardName}");
                }
            }
            else
            {
                // No target required, just play the card
                bool success = _cardSystem.PlayCard(card);
                DebugLog($"Card play {(success ? "succeeded" : "failed")}: {card.CardName}");
            }
        }

        private void HandleEndTurnPressed()
        {
            DebugLog("End turn pressed");

            // Notify game manager
            if (_gameManager != null)
            {
                _gameManager.EndPlayerTurn();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Disable existing card UI components to avoid conflicts
        /// </summary>
        private void DisableExistingCardUI()
        {
            // Find and disable CardHandUI
            CardHandUI existingCardUI = FindFirstObjectByType<CardHandUI>();
            if (existingCardUI != null)
            {
                existingCardUI.gameObject.SetActive(false);
                DebugLog("Disabled existing CardHandUI");
            }

            // Find and disable any Card components
            Card[] existingCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
            foreach (var card in existingCards)
            {
                card.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Create a mapping between Card instances and their UI representations
        /// </summary>
        public void RegisterCardUI(Card card, EnhancedCardUI cardUI)
        {
            if (card != null && cardUI != null)
            {
                _cardUIMapping[card] = cardUI;
            }
        }

        /// <summary>
        /// Get the UI representation for a card
        /// </summary>
        public EnhancedCardUI GetCardUI(Card card)
        {
            if (card != null && _cardUIMapping.TryGetValue(card, out EnhancedCardUI cardUI))
            {
                return cardUI;
            }
            return null;
        }

        /// <summary>
        /// Debug logging
        /// </summary>
        private void DebugLog(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[CardSystemIntegrator] {message}");
            }
        }

        #endregion
    }
}