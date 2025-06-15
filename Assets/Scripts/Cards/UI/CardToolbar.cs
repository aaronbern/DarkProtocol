using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Simplified card toolbar that stays at the bottom and properly displays cards
    /// </summary>
    public class CardToolbar : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private float baseCardWidth = 120f;
        [SerializeField] private float cardOverlapPercent = 0.3f; // How much cards overlap (0.3 = 30%)
        [SerializeField] private float maxHandSpreadWidth = 800f;
        [SerializeField] private float cardYOffset = 20f; // How much to raise hovered card
        [SerializeField] private AnimationCurve fanCurve = AnimationCurve.Linear(0, -10, 1, 10);
        [SerializeField] private float fanIntensity = 1f;

        [Header("Card Sizing")]
        [SerializeField] private bool maintainCardAspectRatio = true;
        [SerializeField] private float cardAspectRatio = 0.714f; // Standard card ratio (5:7)
        [SerializeField] private float maxCardHeight = 200f;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI apDisplayText;
        [SerializeField] private Image apBar;
        [SerializeField] private Button endTurnButton;

        [Header("Animation")]
        [SerializeField] private float cardMoveSpeed = 10f;
        [SerializeField] private float hoverAnimationSpeed = 15f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip cardDrawSound;
        [SerializeField] private AudioClip cardPlaySound;

        // State
        private List<EnhancedCardUI> _cards = new List<EnhancedCardUI>();
        private Dictionary<EnhancedCardUI, CardPosition> _cardPositions = new Dictionary<EnhancedCardUI, CardPosition>();
        private Unit _currentUnit;
        private EnhancedCardUI _hoveredCard;
        private bool _isDraggingCard;

        // Events
        public event Action<Card, Action<Unit>> OnCardNeedsTarget;
        public event Action<Card> OnCardPlayed;

        private class CardPosition
        {
            public Vector2 restPosition;
            public float restRotation;
            public int sortOrder;
        }

        #region Unity Lifecycle

        private void Awake()
        {
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Set up end turn button
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnClicked);

            // Make sure we're anchored to bottom
            RectTransform rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 0);
        }

        private void Update()
        {
            UpdateCardPositions();
            UpdateAPDisplay();
        }

        #endregion

        #region Public Methods

        public void SetUnit(Unit unit)
        {
            _currentUnit = unit;
            UpdateAPDisplay();
        }

        public void AddCard(Card card)
        {
            if (cardUIPrefab == null || card == null) return;

            // Create card UI
            GameObject cardObj = Instantiate(cardUIPrefab, cardContainer);
            EnhancedCardUI cardUI = cardObj.GetComponent<EnhancedCardUI>();

            if (cardUI == null)
            {
                cardUI = cardObj.AddComponent<EnhancedCardUI>();
            }

            // Initialize with proper data
            cardUI.Initialize(card.CardData, card);

            // Set up events
            cardUI.OnCardHovered += HandleCardHovered;
            cardUI.OnCardUnhovered += HandleCardUnhovered;
            cardUI.OnCardPlayed += HandleCardPlayed;
            cardUI.OnCardDragStarted += HandleCardDragStarted;
            cardUI.OnCardDragEnded += HandleCardDragEnded;

            // Add to list
            _cards.Add(cardUI);

            // Calculate positions for all cards
            CalculateCardPositions();

            // Set initial position off-screen for animation
            RectTransform cardRect = cardUI.GetComponent<RectTransform>();
            cardRect.anchoredPosition = new Vector2(0, -300);

            // Play sound
            if (audioSource && cardDrawSound)
                audioSource.PlayOneShot(cardDrawSound);
        }

        public void RemoveCard(Card card)
        {
            EnhancedCardUI cardToRemove = null;

            foreach (var cardUI in _cards)
            {
                if (cardUI.GetCardInstance() == card)
                {
                    cardToRemove = cardUI;
                    break;
                }
            }

            if (cardToRemove != null)
            {
                _cards.Remove(cardToRemove);
                _cardPositions.Remove(cardToRemove);

                // Clean up events
                cardToRemove.OnCardHovered -= HandleCardHovered;
                cardToRemove.OnCardUnhovered -= HandleCardUnhovered;
                cardToRemove.OnCardPlayed -= HandleCardPlayed;
                cardToRemove.OnCardDragStarted -= HandleCardDragStarted;
                cardToRemove.OnCardDragEnded -= HandleCardDragEnded;

                Destroy(cardToRemove.gameObject);

                // Recalculate positions
                CalculateCardPositions();
            }
        }

        public void ClearCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    card.OnCardHovered -= HandleCardHovered;
                    card.OnCardUnhovered -= HandleCardUnhovered;
                    card.OnCardPlayed -= HandleCardPlayed;
                    card.OnCardDragStarted -= HandleCardDragStarted;
                    card.OnCardDragEnded -= HandleCardDragEnded;

                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
            _cardPositions.Clear();
        }

        #endregion

        #region Card Positioning

        private void CalculateCardPositions()
        {
            if (_cards.Count == 0) return;

            _cardPositions.Clear();

            // Calculate card dimensions
            float cardHeight = Mathf.Min(maxCardHeight, cardContainer.rect.height * 0.9f);
            float cardWidth = maintainCardAspectRatio ? cardHeight * cardAspectRatio : baseCardWidth;

            // Calculate total spread
            float totalWidth = cardWidth + ((_cards.Count - 1) * cardWidth * (1f - cardOverlapPercent));
            totalWidth = Mathf.Min(totalWidth, maxHandSpreadWidth);

            // Adjust card spacing based on available space
            float actualSpacing = _cards.Count > 1 ?
                (totalWidth - cardWidth) / (_cards.Count - 1) : 0;

            // Starting position
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;

            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                var pos = new CardPosition();

                // Calculate X position
                float x = startX + (i * actualSpacing);

                // Calculate Y position using fan curve
                float normalizedPos = _cards.Count > 1 ? (float)i / (_cards.Count - 1) : 0.5f;
                float y = fanCurve.Evaluate(normalizedPos) * fanIntensity;

                // Calculate rotation
                float rotation = Mathf.Lerp(-5f, 5f, normalizedPos);

                pos.restPosition = new Vector2(x, y);
                pos.restRotation = rotation;
                pos.sortOrder = i;

                _cardPositions[card] = pos;

                // Set card size
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
            }
        }

        private void UpdateCardPositions()
        {
            foreach (var kvp in _cardPositions)
            {
                var card = kvp.Key;
                var targetPos = kvp.Value;

                if (card == null) continue;

                RectTransform rect = card.GetComponent<RectTransform>();

                // Skip if being dragged
                if (_isDraggingCard && card == _hoveredCard) continue;

                // Calculate target position
                Vector2 finalPos = targetPos.restPosition;
                float finalRot = targetPos.restRotation;
                int sortOrder = targetPos.sortOrder;

                // Apply hover offset
                if (_hoveredCard == card && !_isDraggingCard)
                {
                    finalPos.y += cardYOffset;
                    finalRot = 0; // Straighten hovered card
                    sortOrder = 1000; // Bring to front
                }

                // Animate to position
                rect.anchoredPosition = Vector2.Lerp(
                    rect.anchoredPosition,
                    finalPos,
                    Time.deltaTime * (_hoveredCard == card ? hoverAnimationSpeed : cardMoveSpeed)
                );

                rect.localRotation = Quaternion.Lerp(
                    rect.localRotation,
                    Quaternion.Euler(0, 0, finalRot),
                    Time.deltaTime * cardMoveSpeed
                );

                // Update sort order
                rect.SetSiblingIndex(sortOrder);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleCardHovered(EnhancedCardUI card)
        {
            _hoveredCard = card;
        }

        private void HandleCardUnhovered(EnhancedCardUI card)
        {
            if (_hoveredCard == card)
                _hoveredCard = null;
        }

        private void HandleCardDragStarted(EnhancedCardUI card)
        {
            _isDraggingCard = true;
            _hoveredCard = card;
        }

        private void HandleCardDragEnded(EnhancedCardUI card)
        {
            _isDraggingCard = false;
        }

        private void HandleCardPlayed(EnhancedCardUI cardUI, Vector3 worldPos)
        {
            Card card = cardUI.GetCardInstance();
            if (card == null) return;

            // Play sound
            if (audioSource && cardPlaySound)
                audioSource.PlayOneShot(cardPlaySound);

            // Check if card needs a target
            if (card.CardData.RequiresTarget)
            {
                // Start targeting mode
                StartTargetingMode(card);
            }
            else
            {
                // Play card immediately
                PlayCardWithoutTarget(card);
            }
        }

        private void StartTargetingMode(Card card)
        {
            // Show targeting UI
            Debug.Log($"Select target for {card.CardName}");

            // Notify listeners that we need a target
            OnCardNeedsTarget?.Invoke(card, (target) =>
            {
                // This callback is called when a target is selected
                if (target != null)
                {
                    PlayCardWithTarget(card, target);
                }
            });
        }

        private void PlayCardWithTarget(Card card, Unit target)
        {
            if (CardSystem.Instance != null)
            {
                bool success = CardSystem.Instance.PlayCard(card, target);
                if (success)
                {
                    OnCardPlayed?.Invoke(card);
                }
            }
        }

        private void PlayCardWithoutTarget(Card card)
        {
            if (CardSystem.Instance != null)
            {
                bool success = CardSystem.Instance.PlayCard(card);
                if (success)
                {
                    OnCardPlayed?.Invoke(card);
                }
            }
        }

        private void OnEndTurnClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndPlayerTurn();
            }
        }

        #endregion

        #region UI Updates

        private void UpdateAPDisplay()
        {
            if (_currentUnit == null) return;

            if (apDisplayText != null)
            {
                apDisplayText.text = $"{_currentUnit.CurrentActionPoints}/{_currentUnit.MaxActionPoints}";
            }

            if (apBar != null)
            {
                apBar.fillAmount = (float)_currentUnit.CurrentActionPoints / _currentUnit.MaxActionPoints;
            }
        }

        #endregion
    }
}