using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Manages the UI for displaying and interacting with cards in the player's hand
    /// </summary>
    public class CardHandUI : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Card Layout")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private float cardSpacing = 30f;
        [SerializeField] private float cardElevationSteps = 15f;
        [SerializeField] private float cardFanAngle = 5f;
        [SerializeField] private float selectedCardElevation = 50f;
        [SerializeField] private float cardAnimationSpeed = 0.3f;
        
        [Header("Interaction")]
        [SerializeField] private bool allowDragging = true;
        [SerializeField] private float dragThreshold = 10f;
        [SerializeField] private LayerMask targetingLayers;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject playEffectPrefab;
        [SerializeField] private GameObject targetingLinePrefab;
        [SerializeField] private Color validTargetColor = Color.green;
        [SerializeField] private Color invalidTargetColor = Color.red;
        
        [Header("UI References")]
        [SerializeField] private GameObject cardInfoPanel;
        [SerializeField] private TMPro.TextMeshProUGUI cardNameText;
        [SerializeField] private TMPro.TextMeshProUGUI cardDescriptionText;
        [SerializeField] private Image cardArtworkImage;
        [SerializeField] private Button cancelButton;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        #endregion

        #region Private Fields
        // Current hand
        private List<Card> _currentHand = new List<Card>();
        
        // Selected card
        private Card _selectedCard = null;
        private int _selectedCardIndex = -1;
        
        // Targeting
        private bool _isTargeting = false;
        private GameObject _targetingLine = null;
        private Unit _currentTarget = null;
        
        // Card positioning
        private Vector2[] _cardPositions;
        private Quaternion[] _cardRotations;
        private Vector3[] _cardScales;
        
        // Card animation
        private bool _animatingCards = false;
        
        // Card system reference
        private CardSystem _cardSystem;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get card system reference
            _cardSystem = CardSystem.Instance;
            
            if (_cardSystem == null)
            {
                Debug.LogError("No CardSystem found in scene! Make sure it exists.");
            }
            else
            {
                // Subscribe to card system events
                _cardSystem.OnHandDrawn += HandleHandDrawn;
                _cardSystem.OnCardSelected += HandleCardSelected;
                _cardSystem.OnCardPlayed += HandleCardPlayed;
                _cardSystem.OnCardDiscarded += HandleCardDiscarded;
            }
            
            // Set up cancel button
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(CancelTargeting);
                cancelButton.gameObject.SetActive(false);
            }
            
            // Hide card info panel by default
            if (cardInfoPanel != null)
            {
                cardInfoPanel.SetActive(false);
            }
        }
        
        private void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from card system events
            if (_cardSystem != null)
            {
                _cardSystem.OnHandDrawn -= HandleHandDrawn;
                _cardSystem.OnCardSelected -= HandleCardSelected;
                _cardSystem.OnCardPlayed -= HandleCardPlayed;
                _cardSystem.OnCardDiscarded -= HandleCardDiscarded;
            }
            
            // Unsubscribe from GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
            
            // Clean up targeting line
            if (_targetingLine != null)
            {
                Destroy(_targetingLine);
                _targetingLine = null;
            }
        }
        
        private void Update()
        {
            // Handle targeting
            if (_isTargeting && _selectedCard != null)
            {
                UpdateTargeting();
            }
        }
        #endregion

        #region Card Layout Management
        /// <summary>
        /// Update the card hand based on current hand
        /// </summary>
        public void UpdateHand(List<Card> cards)
        {
            // Clear the current hand
            ClearHand();
            
            // Set the new hand
            _currentHand = new List<Card>(cards);
            
            // Arrange the cards in the hand
            ArrangeCards();
        }
        
        /// <summary>
        /// Clear the current hand
        /// </summary>
        private void ClearHand()
        {
            _currentHand.Clear();
            _selectedCard = null;
            _selectedCardIndex = -1;
            
            // Clean up UI state
            _isTargeting = false;
            
            if (_targetingLine != null)
            {
                Destroy(_targetingLine);
                _targetingLine = null;
            }
            
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }
            
            if (cardInfoPanel != null)
            {
                cardInfoPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Arrange cards in a fan layout
        /// </summary>
        private void ArrangeCards()
        {
            if (_currentHand.Count == 0 || cardContainer == null)
                return;
                
            // Calculate positions
            CalculateCardPositions();
            
            // Animate cards to position
            StartCoroutine(AnimateCardsToPosition());
        }
        
        /// <summary>
        /// Calculate card positions in the layout
        /// </summary>
        private void CalculateCardPositions()
        {
            int cardCount = _currentHand.Count;
            
            // Initialize arrays
            _cardPositions = new Vector2[cardCount];
            _cardRotations = new Quaternion[cardCount];
            _cardScales = new Vector3[cardCount];
            
            // Calculate card positions
            if (cardCount == 1)
            {
                // Single card centered
                _cardPositions[0] = Vector2.zero;
                _cardRotations[0] = Quaternion.identity;
                _cardScales[0] = Vector3.one;
            }
            else
            {
                // Fan layout
                float totalWidth = (cardCount - 1) * cardSpacing;
                float startX = -totalWidth / 2f;
                
                for (int i = 0; i < cardCount; i++)
                {
                    float x = startX + i * cardSpacing;
                    float y = Mathf.Abs(x) * 0.1f; // Slight arc
                    
                    _cardPositions[i] = new Vector2(x, y);
                    
                    // Fan rotation
                    float angle = Mathf.Lerp(-cardFanAngle, cardFanAngle, (float)i / (cardCount - 1));
                    _cardRotations[i] = Quaternion.Euler(0, 0, angle);
                    
                    // Card scale
                    _cardScales[i] = Vector3.one;
                }
            }
            
            // Adjust for selected card
            if (_selectedCardIndex >= 0 && _selectedCardIndex < cardCount)
            {
                // Elevate selected card
                Vector2 pos = _cardPositions[_selectedCardIndex];
                pos.y += selectedCardElevation;
                _cardPositions[_selectedCardIndex] = pos;
            }
        }
        
        /// <summary>
        /// Animate cards to their calculated positions
        /// </summary>
        private System.Collections.IEnumerator AnimateCardsToPosition()
        {
            _animatingCards = true;
            
            // Place cards instantly if none exist
            bool instantPlacement = true;
            
            foreach (Card card in _currentHand)
            {
                if (card.gameObject.activeSelf)
                {
                    instantPlacement = false;
                    break;
                }
            }
            
            // Animate cards to position
            if (instantPlacement)
            {
                // Place cards instantly
                for (int i = 0; i < _currentHand.Count; i++)
                {
                    Card card = _currentHand[i];
                    RectTransform cardRect = card.GetComponent<RectTransform>();
                    
                    if (cardRect != null)
                    {
                        cardRect.anchoredPosition = _cardPositions[i];
                        cardRect.localRotation = _cardRotations[i];
                        cardRect.localScale = _cardScales[i];
                    }
                    
                    // Make the card active
                    card.gameObject.SetActive(true);
                }
            }
            else
            {
                // Animate cards to position
                float animationTime = 0f;
                
                while (animationTime < 1f)
                {
                    animationTime += Time.deltaTime / cardAnimationSpeed;
                    float t = Mathf.SmoothStep(0f, 1f, animationTime);
                    
                    for (int i = 0; i < _currentHand.Count; i++)
                    {
                        Card card = _currentHand[i];
                        RectTransform cardRect = card.GetComponent<RectTransform>();
                        
                        if (cardRect != null)
                        {
                            // Calculate intermediary positions
                            Vector2 targetPos = _cardPositions[i];
                            Quaternion targetRot = _cardRotations[i];
                            Vector3 targetScale = _cardScales[i];
                            
                            // Apply position with animation
                            cardRect.anchoredPosition = Vector2.Lerp(cardRect.anchoredPosition, targetPos, t);
                            cardRect.localRotation = Quaternion.Slerp(cardRect.localRotation, targetRot, t);
                            cardRect.localScale = Vector3.Lerp(cardRect.localScale, targetScale, t);
                        }
                        
                        // Make sure the card is active
                        card.gameObject.SetActive(true);
                    }
                    
                    yield return null;
                }
                
                // Ensure final positions
                for (int i = 0; i < _currentHand.Count; i++)
                {
                    Card card = _currentHand[i];
                    RectTransform cardRect = card.GetComponent<RectTransform>();
                    
                    if (cardRect != null)
                    {
                        cardRect.anchoredPosition = _cardPositions[i];
                        cardRect.localRotation = _cardRotations[i];
                        cardRect.localScale = _cardScales[i];
                    }
                }
            }
            
            _animatingCards = false;
        }
        #endregion

        #region Card Selection and Targeting
        /// <summary>
        /// Select a card in the hand
        /// </summary>
        public void SelectCard(Card card)
        {
            // Deselect current card if it's the same one
            if (_selectedCard == card)
            {
                DeselectCard();
                return;
            }
            
            // Deselect current card
            if (_selectedCard != null)
            {
                _selectedCard.SetSelected(false);
            }
            
            // Select new card
            _selectedCard = card;
            
            if (_selectedCard != null)
            {
                // Get card index
                _selectedCardIndex = _currentHand.IndexOf(_selectedCard);
                
                // Set selected state
                _selectedCard.SetSelected(true);
                
                // Show card info
                ShowCardInfo(_selectedCard);
                
                // Check if card requires targeting
                if (_selectedCard.CardData.RequiresTarget)
                {
                    StartTargeting();
                }
                else
                {
                    _isTargeting = false;
                }
                
                // Rearrange cards
                ArrangeCards();
                
                DebugLog($"Selected card: {_selectedCard.CardName}");
            }
            else
            {
                _selectedCardIndex = -1;
                HideCardInfo();
            }
        }
        
        /// <summary>
        /// Deselect the current card
        /// </summary>
        public void DeselectCard()
        {
            if (_selectedCard != null)
            {
                // Deselect in card system
                if (_cardSystem != null)
                {
                    _cardSystem.DeselectCard();
                }
                
                // End targeting
                EndTargeting();
                
                // Deselect card
                _selectedCard.SetSelected(false);
                _selectedCard = null;
                _selectedCardIndex = -1;
                
                // Hide card info
                HideCardInfo();
                
                // Rearrange cards
                ArrangeCards();
                
                DebugLog("Card deselected");
            }
        }
        
        /// <summary>
        /// Start targeting mode for the selected card
        /// </summary>
        private void StartTargeting()
        {
            if (_selectedCard == null)
                return;
                
            _isTargeting = true;
            
            // Create targeting line if needed
            if (_targetingLine == null && targetingLinePrefab != null)
            {
                _targetingLine = Instantiate(targetingLinePrefab, transform);
                _targetingLine.SetActive(true);
            }
            
            // Show cancel button
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }
            
            DebugLog("Started targeting mode");
        }
        
        /// <summary>
        /// End targeting mode
        /// </summary>
        private void EndTargeting()
        {
            _isTargeting = false;
            _currentTarget = null;
            
            // Hide targeting line
            if (_targetingLine != null)
            {
                _targetingLine.SetActive(false);
            }
            
            // Hide cancel button
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }
            
            DebugLog("Ended targeting mode");
        }
        
        /// <summary>
        /// Update targeting visuals and logic
        /// </summary>
        private void UpdateTargeting()
        {
            if (!_isTargeting || _selectedCard == null)
                return;
                
            // Get mouse position
            Vector2 mousePosition = Input.mousePosition;
            
            // Create ray from mouse position
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            
            // Check if mouse is over a valid target
            if (Physics.Raycast(ray, out hit, 100f, targetingLayers))
            {
                // Check if hit object has a Unit component
                Unit unit = hit.collider.GetComponent<Unit>();
                
                if (unit != null)
                {
                    // Check if unit is a valid target for the card
                    bool isValidTarget = IsValidTarget(unit);
                    
                    // Update targeting line
                    UpdateTargetingLine(unit.transform.position, isValidTarget);
                    
                    // Store current target
                    _currentTarget = isValidTarget ? unit : null;
                    
                    // Check for mouse click to play card
                    if (Input.GetMouseButtonDown(0) && isValidTarget)
                    {
                        PlaySelectedCard(_currentTarget);
                    }
                }
                else
                {
                    // Not a unit, clear targeting
                    UpdateTargetingLine(hit.point, false);
                    _currentTarget = null;
                }
            }
            else
            {
                // No hit, update targeting line to mouse world position
                Vector3 targetPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10f));
                UpdateTargetingLine(targetPos, false);
                _currentTarget = null;
            }
            
            // Check for right click to cancel targeting
            if (Input.GetMouseButtonDown(1))
            {
                CancelTargeting();
            }
        }
        
        /// <summary>
        /// Update the targeting line appearance
        /// </summary>
        private void UpdateTargetingLine(Vector3 targetPosition, bool isValidTarget)
        {
            if (_targetingLine == null || _selectedCard == null)
                return;
                
            // Get line renderer
            LineRenderer lineRenderer = _targetingLine.GetComponent<LineRenderer>();
            if (lineRenderer == null)
                return;
                
            // Get card position in world space
            Vector3 cardPosition = Camera.main.ScreenToWorldPoint(
                new Vector3(
                    _selectedCard.transform.position.x,
                    _selectedCard.transform.position.y,
                    10f
                )
            );
            
            // Set line positions
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, cardPosition);
            lineRenderer.SetPosition(1, targetPosition);
            
            // Set line color based on target validity
            lineRenderer.startColor = lineRenderer.endColor = isValidTarget ? validTargetColor : invalidTargetColor;
        }
        
        /// <summary>
        /// Cancel targeting mode
        /// </summary>
        private void CancelTargeting()
        {
            // Deselect the card
            DeselectCard();
        }
        
        /// <summary>
        /// Check if a unit is a valid target for the selected card
        /// </summary>
        private bool IsValidTarget(Unit unit)
        {
            if (_selectedCard == null || unit == null)
                return false;
                
            CardData cardData = _selectedCard.CardData;
            Unit owner = _selectedCard.Owner;
            
            // Check targeting restrictions
            bool canTarget = false;
            
            // Self targeting
            if (unit == owner && cardData.CanTargetSelf)
            {
                canTarget = true;
            }
            
            // Ally targeting
            if (unit != owner && unit.Team == owner.Team && cardData.CanTargetAllies)
            {
                canTarget = true;
            }
            
            // Enemy targeting
            if (unit.Team != owner.Team && cardData.CanTargetEnemies)
            {
                canTarget = true;
            }
            
            return canTarget;
        }
        
        /// <summary>
        /// Play the selected card on a target
        /// </summary>
        private void PlaySelectedCard(Unit target)
        {
            if (_selectedCard == null)
                return;
                
            if (_cardSystem != null)
            {
                // Play the card
                bool success = _cardSystem.PlayCard(_selectedCard, target);
                
                if (success)
                {
                    // Show play effect
                    if (playEffectPrefab != null && target != null)
                    {
                        GameObject effect = Instantiate(playEffectPrefab, target.transform.position, Quaternion.identity);
                        Destroy(effect, 2f);
                    }
                    
                    // End targeting
                    EndTargeting();
                    
                    // Card will be removed from hand by the card system via the HandleCardPlayed event
                    DebugLog($"Played card {_selectedCard.CardName} on {target.UnitName}");
                }
                else
                {
                    DebugLog($"Failed to play card {_selectedCard.CardName}");
                }
            }
        }
        #endregion

        #region Card Info Panel
        /// <summary>
        /// Show the card info panel for a card
        /// </summary>
        private void ShowCardInfo(Card card)
        {
            if (cardInfoPanel == null || card == null)
                return;
                
            // Set card info
            if (cardNameText != null)
            {
                cardNameText.text = card.CardName;
            }
            
            if (cardDescriptionText != null)
            {
                cardDescriptionText.text = card.CardDescription;
            }
            
            if (cardArtworkImage != null && card.CardData.CardArtwork != null)
            {
                cardArtworkImage.sprite = card.CardData.CardArtwork;
                cardArtworkImage.enabled = true;
            }
            
            // Show the panel
            cardInfoPanel.SetActive(true);
        }
        
        /// <summary>
        /// Hide the card info panel
        /// </summary>
        private void HideCardInfo()
        {
            if (cardInfoPanel == null)
                return;
                
            cardInfoPanel.SetActive(false);
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
                    // Clear hand and UI
                    ClearHand();
                    break;
            }
        }
        
        /// <summary>
        /// Handle hand drawn event from CardSystem
        /// </summary>
        private void HandleHandDrawn(List<Card> hand)
        {
            // Update the hand UI
            UpdateHand(hand);
        }
        
        /// <summary>
        /// Handle card selected event from CardSystem
        /// </summary>
        private void HandleCardSelected(Card card)
        {
            // Select the card in the UI if it's not already selected
            if (_selectedCard != card)
            {
                SelectCard(card);
            }
        }
        
        /// <summary>
        /// Handle card played event from CardSystem
        /// </summary>
        private void HandleCardPlayed(Card card)
        {
            // The card will be removed from hand by the CardSystem
            // We just need to update the hand UI
            _currentHand.Remove(card);
            
            // Deselect if this was the selected card
            if (_selectedCard == card)
            {
                _selectedCard = null;
                _selectedCardIndex = -1;
                HideCardInfo();
            }
            
            // Rearrange remaining cards
            ArrangeCards();
        }
        
        /// <summary>
        /// Handle card discarded event from CardSystem
        /// </summary>
        private void HandleCardDiscarded(Card card)
        {
            // Same as card played for now
            _currentHand.Remove(card);
            
            // Deselect if this was the selected card
            if (_selectedCard == card)
            {
                _selectedCard = null;
                _selectedCardIndex = -1;
                HideCardInfo();
            }
            
            // Rearrange remaining cards
            ArrangeCards();
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Debug logging with prefix
        /// </summary>
        private void DebugLog(string message)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[CardHandUI] {message}");
            }
        }
        #endregion
    }
}