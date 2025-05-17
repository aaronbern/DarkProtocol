using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DarkProtocol.Cards
{
    /// <summary>
    /// MonoBehaviour that represents a card in the scene
    /// Handles card rendering, interaction, and effects
    /// </summary>
    public class Card : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Inspector Fields
        [Header("Card References")]
        [SerializeField] private Image cardBackgroundImage;
        [SerializeField] private Image cardArtworkImage;
        [SerializeField] private Image cardIconImage;
        [SerializeField] private Image cardRarityIndicator;
        [SerializeField] private Image cardHighlightImage;
        
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private TextMeshProUGUI actionPointCostText;
        [SerializeField] private TextMeshProUGUI movementPointCostText;
        [SerializeField] private TextMeshProUGUI effectValueText;
        
        [Header("Animation Settings")]
        [SerializeField] private float highlightScale = 1.1f;
        [SerializeField] private float highlightTransitionSpeed = 0.1f;
        [SerializeField] private float dragThreshold = 0.1f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip playSound;
        #endregion

        #region Private Fields
        // Card data and state
        private CardData _cardData;
        private Unit _owner;
        private bool _isSelected = false;
        private bool _isHighlighted = false;
        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        
        // Dragging state
        private bool _isDragging = false;
        private Vector3 _dragOffset;
        
        // Card cost display
        private bool _hasMovementCost = false;
        private bool _hasHealthCost = false;
        
        // Original colors
        private Color _originalBackgroundColor;
        
        // Audio source
        private AudioSource _audioSource;
        #endregion

        #region Public Properties
        public CardData CardData => _cardData;
        public Unit Owner => _owner;
        public string CardName => _cardData?.CardName ?? "Unknown Card";
        public string CardDescription => _cardData?.CardDescription ?? "";
        public int ActionPointCost => _cardData?.ActionPointCost ?? 0;
        public int MovementPointCost => _cardData?.MovementPointCost ?? 0;
        public int HealthCost => _cardData?.HealthCost ?? 0;
        public bool IsSelected => _isSelected;
        
        // Events
        public event Action<Card> OnCardClicked;
        public event Action<Card> OnCardHoverEnter;
        public event Action<Card> OnCardHoverExit;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Cache the original scale
            _originalScale = transform.localScale;
            _originalPosition = transform.position;
            
            // Get or add audio source
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0f; // 2D sound
            }
            
            // Cache original colors
            if (cardBackgroundImage != null)
            {
                _originalBackgroundColor = cardBackgroundImage.color;
            }
        }
        
        private void Update()
        {
            // Handle card highlighting (hover effect)
            if (_isHighlighted && !_isSelected)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _originalScale * highlightScale, Time.deltaTime * highlightTransitionSpeed);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _originalScale, Time.deltaTime * highlightTransitionSpeed);
            }
            
            // Handle card dragging
            if (_isDragging)
            {
                // Update position based on mouse
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = -Camera.main.transform.position.z;
                transform.position = Camera.main.ScreenToWorldPoint(mousePosition) + _dragOffset;
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the card with data and owner
        /// </summary>
        public void Initialize(CardData cardData, Unit owner)
        {
            _cardData = cardData;
            _owner = owner;
            
            if (_cardData == null)
            {
                Debug.LogError("Cannot initialize card with null CardData!");
                return;
            }
            
            // Set up card visuals
            SetupCardVisuals();
            
            // Set up card costs
            SetupCardCosts();
            
            // Set up card descriptions
            SetupCardText();
        }
        
        /// <summary>
        /// Set up card visual elements
        /// </summary>
        private void SetupCardVisuals()
        {
            // Set card background
            if (cardBackgroundImage != null && _cardData.CardBackground != null)
            {
                cardBackgroundImage.sprite = _cardData.CardBackground;
                cardBackgroundImage.color = _cardData.CardColor;
            }
            
            // Set card artwork
            if (cardArtworkImage != null && _cardData.CardArtwork != null)
            {
                cardArtworkImage.sprite = _cardData.CardArtwork;
                cardArtworkImage.enabled = true;
            }
            
            // Set card icon
            if (cardIconImage != null && _cardData.CardIcon != null)
            {
                cardIconImage.sprite = _cardData.CardIcon;
                cardIconImage.enabled = true;
            }
            
            // Set card rarity indicator
            if (cardRarityIndicator != null)
            {
                // Set color based on rarity
                Color rarityColor = GetRarityColor(_cardData.Rarity);
                cardRarityIndicator.color = rarityColor;
                cardRarityIndicator.enabled = true;
            }
            
            // Disable highlight by default
            if (cardHighlightImage != null)
            {
                cardHighlightImage.enabled = false;
            }
        }
        
        /// <summary>
        /// Set up card cost displays
        /// </summary>
        private void SetupCardCosts()
        {
            // Action points
            if (actionPointCostText != null)
            {
                actionPointCostText.text = _cardData.ActionPointCost.ToString();
                actionPointCostText.gameObject.SetActive(_cardData.ActionPointCost > 0);
            }
            
            // Movement points
            _hasMovementCost = _cardData.MovementPointCost > 0;
            if (movementPointCostText != null)
            {
                movementPointCostText.text = _cardData.MovementPointCost.ToString();
                movementPointCostText.gameObject.SetActive(_hasMovementCost);
            }
            
            // Health cost (not shown in UI by default, could add if needed)
            _hasHealthCost = _cardData.HealthCost > 0;
        }
        
        /// <summary>
        /// Set up card text elements
        /// </summary>
        private void SetupCardText()
        {
            // Card name
            if (cardNameText != null)
            {
                cardNameText.text = _cardData.CardName;
            }
            
            // Card description
            if (cardDescriptionText != null)
            {
                // Replace placeholder values with actual values
                string description = _cardData.CardDescription;
                
                // Replace damage and healing values
                description = description.Replace("{damage}", _cardData.BaseDamage.ToString());
                description = description.Replace("{healing}", _cardData.BaseHealing.ToString());
                description = description.Replace("{range}", _cardData.EffectRange.ToString());
                
                // Apply any card-specific formatting
                
                cardDescriptionText.text = description;
            }
            
            // Effect value (e.g., damage or healing amount)
            if (effectValueText != null)
            {
                string valueText = "";
                
                // Determine what value to show based on card type
                switch (_cardData.EffectType)
                {
                    case CardEffectType.Damage:
                        valueText = _cardData.BaseDamage.ToString();
                        break;
                    case CardEffectType.Healing:
                        valueText = _cardData.BaseHealing.ToString();
                        break;
                }
                
                effectValueText.text = valueText;
                effectValueText.gameObject.SetActive(!string.IsNullOrEmpty(valueText));
            }
        }
        #endregion

        #region Interaction Handlers
        /// <summary>
        /// Handle pointer click
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
                
            SetSelected(!_isSelected);
            
            // Notify listeners
            OnCardClicked?.Invoke(this);
            
            // Play sound
            PlaySound(clickSound);
        }
        
        /// <summary>
        /// Handle pointer enter
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHighlighted = true;
            
            // Show highlight
            if (cardHighlightImage != null)
            {
                cardHighlightImage.enabled = true;
            }
            
            // Notify listeners
            OnCardHoverEnter?.Invoke(this);
            
            // Play sound
            PlaySound(hoverSound);
        }
        
        /// <summary>
        /// Handle pointer exit
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHighlighted = false;
            
            // Hide highlight
            if (cardHighlightImage != null)
            {
                cardHighlightImage.enabled = false;
            }
            
            // Notify listeners
            OnCardHoverExit?.Invoke(this);
        }
        
        /// <summary>
        /// Handle card selection
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            // Update visual state
            if (cardHighlightImage != null)
            {
                cardHighlightImage.enabled = selected;
            }
            
            // Update card appearance
            UpdateCardAppearance();
        }
        
        /// <summary>
        /// Update card appearance based on current state
        /// </summary>
        private void UpdateCardAppearance()
        {
            // Playability checks
            bool canPlay = true;
            
            // Check if owner has enough AP
            if (_owner != null && _owner.CurrentActionPoints < _cardData.ActionPointCost)
            {
                canPlay = false;
            }
            
            // Check if owner has enough MP (if card costs MP)
            if (_hasMovementCost && _owner != null && _owner.CurrentMovementPoints < _cardData.MovementPointCost)
            {
                canPlay = false;
            }
            
            // Check if owner has enough HP (if card costs HP)
            if (_hasHealthCost && _owner != null && _owner.CurrentHealth <= _cardData.HealthCost)
            {
                canPlay = false;
            }
            
            // Update card visuals based on playability
            if (cardBackgroundImage != null)
            {
                if (!canPlay)
                {
                    // Dim the card if it can't be played
                    cardBackgroundImage.color = new Color(
                        _originalBackgroundColor.r * 0.6f,
                        _originalBackgroundColor.g * 0.6f,
                        _originalBackgroundColor.b * 0.6f,
                        _originalBackgroundColor.a
                    );
                }
                else
                {
                    // Normal color
                    cardBackgroundImage.color = _originalBackgroundColor;
                }
            }
        }
        #endregion

        #region Card Actions
        /// <summary>
        /// Execute the card effect
        /// </summary>
        public bool ExecuteEffect(Unit target = null)
        {
            if (_cardData == null || _owner == null)
                return false;
                
            // Check if the owner has enough action points
            if (_owner.CurrentActionPoints < _cardData.ActionPointCost)
            {
                Debug.LogWarning($"Not enough action points to play {_cardData.CardName}");
                return false;
            }
            
            // Check for movement point cost
            if (_hasMovementCost && _owner.CurrentMovementPoints < _cardData.MovementPointCost)
            {
                Debug.LogWarning($"Not enough movement points to play {_cardData.CardName}");
                return false;
            }
            
            // Check for health cost
            if (_hasHealthCost && _owner.CurrentHealth <= _cardData.HealthCost)
            {
                Debug.LogWarning($"Not enough health to play {_cardData.CardName}");
                return false;
            }
            
            // Play card sound
            PlaySound(playSound);
            
            // Execute the card effect
            return _cardData.ExecuteEffect(_owner, target);
        }
        
        /// <summary>
        /// Play a sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Get color based on card rarity
        /// </summary>
        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return new Color(0.7f, 0.7f, 0.7f); // Gray
                case CardRarity.Uncommon:
                    return new Color(0.0f, 0.7f, 0.0f); // Green
                case CardRarity.Rare:
                    return new Color(0.0f, 0.4f, 0.8f); // Blue
                case CardRarity.Epic:
                    return new Color(0.7f, 0.2f, 0.8f); // Purple
                case CardRarity.Legendary:
                    return new Color(1.0f, 0.5f, 0.0f); // Orange
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Convert a string to rich text colored by rarity
        /// </summary>
        private string ColorTextByRarity(string text, CardRarity rarity)
        {
            string hexColor = "";
            
            switch (rarity)
            {
                case CardRarity.Common:
                    hexColor = "#B8B8B8"; // Gray
                    break;
                case CardRarity.Uncommon:
                    hexColor = "#00CC00"; // Green
                    break;
                case CardRarity.Rare:
                    hexColor = "#0080FF"; // Blue
                    break;
                case CardRarity.Epic:
                    hexColor = "#CC33FF"; // Purple
                    break;
                case CardRarity.Legendary:
                    hexColor = "#FF9900"; // Orange
                    break;
                default:
                    return text;
            }
            
            return $"<color={hexColor}>{text}</color>";
        }
        #endregion
    }
}