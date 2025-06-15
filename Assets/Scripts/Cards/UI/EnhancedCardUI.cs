using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Enhanced card UI component with modern interactions and visual feedback
    /// Designed for fast prototyping and gameplay development
    /// </summary>
    public class EnhancedCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image cardIcon;
        [SerializeField] private Image cardArt;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private TextMeshProUGUI apCostText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI rangeText;
        [SerializeField] private GameObject apCostBadge;
        [SerializeField] private GameObject unavailableOverlay;
        [SerializeField] private GameObject selectedGlow;
        [SerializeField] private Transform tagsContainer;
        [SerializeField] private GameObject tagPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float hoverElevation = 30f;
        [SerializeField] private float animationSpeed = 8f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve elevationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Interaction")]
        [SerializeField] private bool allowDragging = true;
        [SerializeField] private float dragThreshold = 10f;
        [SerializeField] private LayerMask targetingLayers = -1;

        [Header("Visual States")]
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color unavailableColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 1.2f);

        // Card data and state
        private CardData _cardData;
        private Card _cardInstance;
        private bool _isHovered = false;
        private bool _isSelected = false;
        private bool _isDragging = false;
        private bool _canPlay = true;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private int _originalSiblingIndex;
        private Canvas _parentCanvas;
        private GraphicRaycaster _graphicRaycaster;

        // Animation
        private Coroutine _animationCoroutine;

        // Events
        public event Action<EnhancedCardUI> OnCardClicked;
        public event Action<EnhancedCardUI> OnCardHovered;
        public event Action<EnhancedCardUI> OnCardUnhovered;
        public event Action<EnhancedCardUI, Vector3> OnCardPlayed;
        public event Action<EnhancedCardUI> OnCardDragStarted;
        public event Action<EnhancedCardUI> OnCardDragEnded;

        #region Unity Lifecycle

        private void Awake()
        {
            _originalPosition = transform.localPosition;
            _originalScale = transform.localScale;
            _originalSiblingIndex = transform.GetSiblingIndex();
            
            _parentCanvas = GetComponentInParent<Canvas>();
            _graphicRaycaster = GetComponentInParent<GraphicRaycaster>();

            // Initialize visual states
            if (unavailableOverlay != null)
                unavailableOverlay.SetActive(false);
            
            if (selectedGlow != null)
                selectedGlow.SetActive(false);
        }

        private void Start()
        {
            UpdateVisualState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the card with data
        /// </summary>
        public void Initialize(CardData cardData, Card cardInstance = null)
        {
            _cardData = cardData;
            _cardInstance = cardInstance;
            
            if (_cardData == null)
            {
                Debug.LogWarning("Card initialized with null CardData!");
                return;
            }

            UpdateCardDisplay();
            UpdatePlayability();
        }

        /// <summary>
        /// Set the selected state
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected != selected)
            {
                _isSelected = selected;
                UpdateVisualState();
                
                if (selectedGlow != null)
                    selectedGlow.SetActive(selected);
            }
        }

        /// <summary>
        /// Set whether the card can be played
        /// </summary>
        public void SetPlayable(bool canPlay)
        {
            if (_canPlay != canPlay)
            {
                _canPlay = canPlay;
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Get the card data
        /// </summary>
        public CardData GetCardData() => _cardData;

        /// <summary>
        /// Get the card instance
        /// </summary>
        public Card GetCardInstance() => _cardInstance;

        /// <summary>
        /// Check if card is currently playable
        /// </summary>
        public bool IsPlayable() => _canPlay && _cardData != null;

        #endregion

        #region Display Updates

        private void UpdateCardDisplay()
        {
            if (_cardData == null) return;

            // Basic info
            if (cardNameText != null)
                cardNameText.text = _cardData.CardName;

            if (cardDescriptionText != null)
            {
                string description = _cardData.CardDescription;
                // Replace placeholders
                description = description.Replace("{damage}", _cardData.BaseDamage.ToString());
                description = description.Replace("{healing}", _cardData.BaseHealing.ToString());
                description = description.Replace("{range}", _cardData.EffectRange.ToString());
                cardDescriptionText.text = description;
            }

            // Costs and stats
            if (apCostText != null)
                apCostText.text = _cardData.ActionPointCost.ToString();

            if (damageText != null)
            {
                if (_cardData.BaseDamage > 0)
                {
                    damageText.text = _cardData.BaseDamage.ToString();
                    damageText.gameObject.SetActive(true);
                }
                else
                {
                    damageText.gameObject.SetActive(false);
                }
            }

            if (rangeText != null)
            {
                if (_cardData.EffectRange > 0)
                {
                    rangeText.text = _cardData.EffectRange.ToString();
                    rangeText.gameObject.SetActive(true);
                }
                else
                {
                    rangeText.gameObject.SetActive(false);
                }
            }

            // Visual elements
            if (cardIcon != null && _cardData.CardIcon != null)
                cardIcon.sprite = _cardData.CardIcon;

            if (cardArt != null && _cardData.CardArtwork != null)
                cardArt.sprite = _cardData.CardArtwork;

            // Category-based coloring
            UpdateCategoryVisuals();

            // Tags
            UpdateTagDisplay();
        }

        private void UpdateCategoryVisuals()
        {
            if (cardFrame == null || _cardData == null) return;

            Color frameColor = GetCategoryColor(_cardData.Category);
            cardFrame.color = frameColor;

            if (apCostBadge != null)
            {
                Image badgeImage = apCostBadge.GetComponent<Image>();
                if (badgeImage != null)
                    badgeImage.color = frameColor;
            }
        }

        private Color GetCategoryColor(CardCategory category)
        {
            switch (category)
            {
                case CardCategory.Attack: return new Color(0.8f, 0.2f, 0.2f); // Red
                case CardCategory.Defense: return new Color(0.2f, 0.4f, 0.8f); // Blue
                case CardCategory.Support: return new Color(0.2f, 0.8f, 0.4f); // Green
                case CardCategory.Movement: return new Color(0.8f, 0.8f, 0.2f); // Yellow
                case CardCategory.Utility: return new Color(0.6f, 0.4f, 0.8f); // Purple
                case CardCategory.Special: return new Color(0.8f, 0.4f, 0.2f); // Orange
                default: return Color.gray;
            }
        }

        private void UpdateTagDisplay()
        {
            if (tagsContainer == null || tagPrefab == null) return;

            // Clear existing tags
            foreach (Transform child in tagsContainer)
            {
                Destroy(child.gameObject);
            }

            // TODO: Add tag system when implemented
            // This would display synergy tags like [Reaction], [Control], etc.
        }

        private void UpdatePlayability()
        {
            if (_cardInstance == null) return;

            // Check if the unit can afford to play this card
            Unit owner = _cardInstance.Owner;
            if (owner != null)
            {
                bool canAfford = owner.CurrentActionPoints >= _cardData.ActionPointCost;
                SetPlayable(canAfford);
            }
        }

        private void UpdateVisualState()
        {
            if (cardBackground == null) return;

            Color targetColor;
            
            if (!_canPlay)
            {
                targetColor = unavailableColor;
                if (unavailableOverlay != null)
                    unavailableOverlay.SetActive(true);
            }
            else if (_isSelected)
            {
                targetColor = selectedColor;
                if (unavailableOverlay != null)
                    unavailableOverlay.SetActive(false);
            }
            else if (_isHovered)
            {
                targetColor = hoverColor;
                if (unavailableOverlay != null)
                    unavailableOverlay.SetActive(false);
            }
            else
            {
                targetColor = availableColor;
                if (unavailableOverlay != null)
                    unavailableOverlay.SetActive(false);
            }

            cardBackground.color = targetColor;
        }

        #endregion

        #region Animation

        private void AnimateToState(bool hover)
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);

            _animationCoroutine = StartCoroutine(AnimateToStateCoroutine(hover));
        }

        private IEnumerator AnimateToStateCoroutine(bool hover)
        {
            Vector3 targetPosition = _originalPosition;
            Vector3 targetScale = _originalScale;

            if (hover && _canPlay)
            {
                targetPosition += Vector3.up * hoverElevation;
                targetScale *= hoverScale;
                
                // Bring to front while hovering
                transform.SetAsLastSibling();
            }
            else
            {
                // Return to original position in hierarchy
                transform.SetSiblingIndex(_originalSiblingIndex);
            }

            Vector3 startPosition = transform.localPosition;
            Vector3 startScale = transform.localScale;
            
            float elapsed = 0f;
            float duration = 1f / animationSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Apply curves
                float scaleT = scaleCurve.Evaluate(t);
                float positionT = elevationCurve.Evaluate(t);

                transform.localPosition = Vector3.Lerp(startPosition, targetPosition, positionT);
                transform.localScale = Vector3.Lerp(startScale, targetScale, scaleT);

                yield return null;
            }

            transform.localPosition = targetPosition;
            transform.localScale = targetScale;
            
            _animationCoroutine = null;
        }

        #endregion

        #region Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging) return;

            _isHovered = true;
            UpdateVisualState();
            AnimateToState(true);

            OnCardHovered?.Invoke(this);

            // Show tooltip if available
            if (_cardData != null)
            {
                TooltipSystem.ShowCard(_cardData);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging) return;

            _isHovered = false;
            UpdateVisualState();
            AnimateToState(false);

            OnCardUnhovered?.Invoke(this);

            // Hide tooltip
            TooltipSystem.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging || !_canPlay) return;

            OnCardClicked?.Invoke(this);

            // Toggle selection
            SetSelected(!_isSelected);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!allowDragging || !_canPlay) return;

            _isDragging = true;
            _isHovered = false;
            
            // Hide tooltip
            TooltipSystem.Hide();
            
            OnCardDragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            // Move card with mouse
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            
            // Reset position and scale
            transform.localPosition = _originalPosition;
            transform.localScale = _originalScale;
            transform.SetSiblingIndex(_originalSiblingIndex);

            // Check if we're over a valid target
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 10f));
            
            // Raycast to find target
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, targetingLayers))
            {
                OnCardPlayed?.Invoke(this, hit.point);
            }

            OnCardDragEnded?.Invoke(this);
        }

        #endregion
    }
}