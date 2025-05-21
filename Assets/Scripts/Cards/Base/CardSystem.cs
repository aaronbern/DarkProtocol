using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Cards
{
    /// <summary>
    /// Core card system for Dark Protocol
    /// Manages card decks, hands, and card actions
    /// </summary>
    public class CardSystem : MonoBehaviour
    {
        #region Singleton
        public static CardSystem Instance { get; private set; }

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
            
            // Initialize the card library
            InitializeCardLibrary();
        }
        #endregion

        #region Properties and Fields
        [Header("Card Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private int defaultHandSize = 5;
        [SerializeField] private int maxCardsInHand = 8;
        
        [Header("Card References")]
        [SerializeField] private List<CardData> allCardsList = new List<CardData>();
        [SerializeField] private List<CardData> coreTeamDeck = new List<CardData>();
        
        [Header("Card Prefabs")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform cardContainer;
        
        // Current active cards
        private Dictionary<Unit, List<Card>> _activeHandsByUnit = new Dictionary<Unit, List<Card>>();
        
        // Available cards in unit decks
        private Dictionary<Unit, List<CardData>> _unitDecks = new Dictionary<Unit, List<CardData>>();
        
        // Card library (all available cards)
        private Dictionary<string, CardData> _cardLibrary = new Dictionary<string, CardData>();
        
        // Currently selected card
        private Card _selectedCard = null;
        public Card SelectedCard => _selectedCard;
        
        // Currently active unit
        private Unit _activeUnit = null;
        
        // Events
        public event Action<Card> OnCardSelected;
        public event Action<Card> OnCardPlayed;
        public event Action<Card> OnCardDiscarded;
        public event Action<List<Card>> OnHandDrawn;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Subscribe to GameManager events if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged += HandleTurnChanged;
                GameManager.Instance.OnUnitActivated += HandleUnitActivated;
                GameManager.Instance.OnUnitDeactivated += HandleUnitDeactivated;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
                GameManager.Instance.OnUnitActivated -= HandleUnitActivated;
                GameManager.Instance.OnUnitDeactivated -= HandleUnitDeactivated;
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the card library from all available cards
        /// </summary>
        private void InitializeCardLibrary()
        {
            _cardLibrary.Clear();
            
            // Add all cards to the library
            foreach (CardData card in allCardsList)
            {
                if (card != null && !string.IsNullOrEmpty(card.CardID))
                {
                    _cardLibrary[card.CardID] = card;
                }
                else
                {
                    Debug.LogWarning("Invalid card data found. Cards must have a valid CardID.");
                }
            }
            
            DebugLog($"Card library initialized with {_cardLibrary.Count} cards");
        }
        
        /// <summary>
        /// Initialize a unit with its deck
        /// </summary>
        public void InitializeUnitDeck(Unit unit, UnitCardDeck deckConfig)
        {
            if (unit == null || deckConfig == null)
                return;
                
            List<CardData> unitDeck = new List<CardData>();
            
            // Add the unit's specialized cards
            foreach (CardData card in deckConfig.SpecializedCards)
            {
                if (card != null)
                {
                    unitDeck.Add(card);
                }
            }
            
            // Add core team cards
            foreach (CardData card in coreTeamDeck)
            {
                if (card != null)
                {
                    // Add multiple copies of common cards if specified
                    int copies = card.IsCommon ? deckConfig.CommonCardCopies : 1;
                    for (int i = 0; i < copies; i++)
                    {
                        unitDeck.Add(card);
                    }
                }
            }
            
            // Store the deck
            _unitDecks[unit] = unitDeck;
            
            DebugLog($"Initialized deck for {unit.UnitName} with {unitDeck.Count} cards");
        }
        
        /// <summary>
        /// Reset all hands and prepare for a new game
        /// </summary>
        public void ResetCardSystem()
        {
            // Clear all hands
            foreach (var handList in _activeHandsByUnit.Values)
            {
                foreach (Card card in handList)
                {
                    Destroy(card.gameObject);
                }
            }
            
            _activeHandsByUnit.Clear();
            _selectedCard = null;
            _activeUnit = null;
            
            DebugLog("Card system reset");
        }
        #endregion

        #region Hand Management
        /// <summary>
        /// Draw a new hand for a unit
        /// </summary>
        public void DrawHandForUnit(Unit unit)
        {
            if (unit == null) return;
            
            // Clear existing hand if any
            if (_activeHandsByUnit.TryGetValue(unit, out List<Card> existingHand))
            {
                DiscardHand(unit);
            }
            
            // Check if the unit has a deck
            if (!_unitDecks.TryGetValue(unit, out List<CardData> unitDeck) || unitDeck.Count == 0)
            {
                Debug.LogWarning($"No deck found for unit {unit.UnitName}. Create a deck first with InitializeUnitDeck().");
                return;
            }
            
            // Create a copy of the deck to draw from
            List<CardData> drawPile = new List<CardData>(unitDeck);
            
            // Shuffle the draw pile
            ShuffleList(drawPile);
            
            // Draw cards up to hand size
            List<Card> newHand = new List<Card>();
            int cardsToDraw = Math.Min(defaultHandSize, drawPile.Count);
            
            for (int i = 0; i < cardsToDraw; i++)
            {
                // Get card data from the pile
                CardData cardData = drawPile[i];
                
                // Instantiate card
                Card newCard = CreateCardInstance(cardData, unit);
                
                if (newCard != null)
                {
                    newHand.Add(newCard);
                }
            }
            
            // Store the hand
            _activeHandsByUnit[unit] = newHand;
            
            // Notify listeners
            OnHandDrawn?.Invoke(newHand);
            
            DebugLog($"Drew {newHand.Count} cards for {unit.UnitName}");
            
            // Make this the active unit
            _activeUnit = unit;
        }
        
        /// <summary>
        /// Create a card instance from card data
        /// </summary>
        private Card CreateCardInstance(CardData cardData, Unit owner)
        {
            if (cardPrefab == null || cardContainer == null)
            {
                Debug.LogError("Card prefab or card container not assigned!");
                return null;
            }
            
            // Create the card game object
            GameObject cardObj = Instantiate(cardPrefab, cardContainer);
            
            // Get the Card component
            Card card = cardObj.GetComponent<Card>();
            if (card == null)
            {
                Debug.LogError("Card prefab must have a Card component!");
                Destroy(cardObj);
                return null;
            }
            
            // Initialize the card
            card.Initialize(cardData, owner);
            
            // Subscribe to card events
            card.OnCardClicked += HandleCardClicked;
            
            return card;
        }
        
        /// <summary>
        /// Discard the hand for a unit
        /// </summary>
        public void DiscardHand(Unit unit)
        {
            if (unit == null) return;
            
            if (_activeHandsByUnit.TryGetValue(unit, out List<Card> hand))
            {
                // Destroy all card game objects
                foreach (Card card in hand)
                {
                    if (card != null)
                    {
                        // Unsubscribe from events
                        card.OnCardClicked -= HandleCardClicked;
                        
                        // Destroy the card
                        Destroy(card.gameObject);
                        
                        // Notify listeners
                        OnCardDiscarded?.Invoke(card);
                    }
                }
                
                // Clear the hand
                hand.Clear();
                
                DebugLog($"Discarded hand for {unit.UnitName}");
            }
            
            // Reset selected card if it belonged to this unit
            if (_selectedCard != null && _selectedCard.Owner == unit)
            {
                _selectedCard = null;
            }
        }
        
        /// <summary>
        /// Get the current hand for a unit
        /// </summary>
        public List<Card> GetHandForUnit(Unit unit)
        {
            if (unit == null) return new List<Card>();
            
            if (_activeHandsByUnit.TryGetValue(unit, out List<Card> hand))
            {
                return hand;
            }
            
            return new List<Card>();
        }
        
        /// <summary>
        /// Play a card from a unit's hand
        /// </summary>
        public bool PlayCard(Card card, Unit target = null)
        {
            if (card == null || card.Owner == null) return false;
            
            Unit owner = card.Owner;
            
            // Check if the card is in the owner's hand
            if (!_activeHandsByUnit.TryGetValue(owner, out List<Card> hand) || !hand.Contains(card))
            {
                Debug.LogWarning("Attempted to play a card that's not in the owner's hand");
                return false;
            }
            
            // Check if the owner has enough action points
            if (owner.CurrentActionPoints < card.ActionPointCost)
            {
                Debug.LogWarning($"Not enough action points! Needed {card.ActionPointCost}, has {owner.CurrentActionPoints}");
                return false;
            }
            
            // Execute the card effect
            bool effectSuccess = card.ExecuteEffect(target);
            
            if (effectSuccess)
            {
                // Spend action points
                owner.SpendActionPoints(card.ActionPointCost);
                
                // Remove from hand
                hand.Remove(card);
                
                // Notify listeners
                OnCardPlayed?.Invoke(card);
                
                // Unsubscribe from events
                card.OnCardClicked -= HandleCardClicked;
                
                // Destroy the card object
                Destroy(card.gameObject);
                
                // Reset selected card if this was the selected one
                if (_selectedCard == card)
                {
                    _selectedCard = null;
                }
                
                DebugLog($"{owner.UnitName} played {card.CardName} for {card.ActionPointCost} AP");
                
                return true;
            }
            
            Debug.LogWarning($"Failed to execute card effect for {card.CardName}");
            return false;
        }
        #endregion

        #region Card Selection
        /// <summary>
        /// Handle card click events
        /// </summary>
        private void HandleCardClicked(Card card)
        {
            // Only allow selection during proper turn
            if (GameManager.Instance != null)
            {
                if (card.Owner.Team == Unit.TeamType.Player && !GameManager.Instance.IsPlayerTurn())
                {
                    Debug.LogWarning("Cannot select cards during enemy turn");
                    return;
                }
                else if (card.Owner.Team == Unit.TeamType.Enemy && !GameManager.Instance.IsEnemyTurn())
                {
                    Debug.LogWarning("Cannot select enemy cards during player turn");
                    return;
                }
            }
            
            // Select the card
            _selectedCard = card;
            
            // Notify listeners
            OnCardSelected?.Invoke(card);
            
            DebugLog($"Selected card: {card.CardName}");
        }
        
        /// <summary>
        /// Deselect current card
        /// </summary>
        public void DeselectCard()
        {
            if (_selectedCard != null)
            {
                Card previousCard = _selectedCard;
                _selectedCard = null;
                
                // Notify listeners
                OnCardSelected?.Invoke(null);
                
                DebugLog($"Deselected card: {previousCard.CardName}");
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle turn changes from GameManager
        /// </summary>
        private void HandleTurnChanged(GameManager.TurnState newState)
        {
            switch (newState)
            {
                case GameManager.TurnState.PlayerTurnEnd:
                case GameManager.TurnState.EnemyTurnEnd:
                    // Discard all hands at the end of a turn
                    foreach (Unit unit in _activeHandsByUnit.Keys)
                    {
                        DiscardHand(unit);
                    }
                    _activeHandsByUnit.Clear();
                    _selectedCard = null;
                    _activeUnit = null;
                    break;
            }
        }

        /// <summary>
        /// Handle unit activation
        /// </summary>
        private void HandleUnitActivated(Unit unit)
        {
            if (unit == null) return;

            // CHANGED: Comment out automatic card drawing
            // DrawHandForUnit(unit);  <-- Remove or comment out this line

            // Store as active unit
            _activeUnit = unit;

            DebugLog($"Unit {unit.UnitName} activated - cards will be drawn when requested");
        }
        /// <summary>
        /// Handle unit deactivation
        /// </summary>
        private void HandleUnitDeactivated(Unit unit)
        {
            if (unit == null) return;
            
            // Discard the hand for the deactivated unit
            DiscardHand(unit);
            
            // Clear active unit if this was the one
            if (_activeUnit == unit)
            {
                _activeUnit = null;
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Shuffle a list
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            System.Random random = new System.Random();
            int n = list.Count;
            
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
        
        /// <summary>
        /// Get a card from the card library by ID
        /// </summary>
        public CardData GetCardById(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;
                
            if (_cardLibrary.TryGetValue(cardId, out CardData card))
            {
                return card;
            }
            
            Debug.LogWarning($"Card with ID '{cardId}' not found in library");
            return null;
        }
        
        /// <summary>
        /// Debug logging with prefix
        /// </summary>
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[CardSystem] {message}");
            }
        }
        #endregion
    }
}