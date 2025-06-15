using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.Cards;
using System.Collections;
using System.Collections.Generic;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Enhanced card tooltip that displays detailed card information
    /// </summary>
    public class EnhancedCardTooltip : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardTypeText;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private TextMeshProUGUI apCostText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI rangeText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Transform tagContainer;
        [SerializeField] private GameObject tagPrefab;
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private GameObject statusEffectPrefab;

        [Header("Animation")]
        [SerializeField] private float fadeInSpeed = 8f;
        [SerializeField] private float fadeOutSpeed = 12f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Positioning")]
        [SerializeField] private Vector2 offset = new Vector2(10, 10);
        [SerializeField] private bool followMouse = true;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private CardData _currentCardData;
        private Coroutine _fadeCoroutine;
        private bool _isVisible = false;

        #region Unity Lifecycle

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _rectTransform = GetComponent<RectTransform>();

            // Start hidden
            _canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_isVisible && followMouse)
            {
                UpdatePosition();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show tooltip for a card
        /// </summary>
        public void ShowCard(CardData cardData, Vector2? position = null)
        {
            if (cardData == null) return;

            _currentCardData = cardData;
            UpdateContent();

            if (position.HasValue)
            {
                _rectTransform.position = position.Value;
            }
            else
            {
                UpdatePosition();
            }

            Show();
        }

        /// <summary>
        /// Hide the tooltip
        /// </summary>
        public void Hide()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeOut());
        }

        #endregion

        #region Content Update

        private void UpdateContent()
        {
            if (_currentCardData == null) return;

            // Basic info
            if (cardNameText != null)
                cardNameText.text = _currentCardData.CardName;

            if (cardTypeText != null)
                cardTypeText.text = _currentCardData.Category.ToString();

            if (cardDescriptionText != null)
            {
                string description = _currentCardData.CardDescription;
                // Replace placeholders
                description = description.Replace("{damage}", _currentCardData.BaseDamage.ToString());
                description = description.Replace("{healing}", _currentCardData.BaseHealing.ToString());
                description = description.Replace("{range}", _currentCardData.EffectRange.ToString());
                description = description.Replace("{duration}", _currentCardData.EffectDuration.ToString());
                cardDescriptionText.text = description;
            }

            // Stats
            if (apCostText != null)
                apCostText.text = $"AP Cost: {_currentCardData.ActionPointCost}";

            if (damageText != null)
            {
                damageText.gameObject.SetActive(_currentCardData.BaseDamage > 0);
                damageText.text = $"Damage: {_currentCardData.BaseDamage}";
            }

            if (rangeText != null)
            {
                rangeText.gameObject.SetActive(_currentCardData.EffectRange > 0);
                rangeText.text = $"Range: {_currentCardData.EffectRange}";
            }

            // Rarity
            if (rarityText != null)
                rarityText.text = _currentCardData.Rarity.ToString();

            if (rarityBorder != null)
                rarityBorder.color = GetRarityColor(_currentCardData.Rarity);

            // Tags (when synergy system is implemented)
            UpdateTags();

            // Status effects
            UpdateStatusEffects();
        }

        private void UpdateTags()
        {
            if (tagContainer == null) return;

            // Clear existing tags
            foreach (Transform child in tagContainer)
            {
                Destroy(child.gameObject);
            }

            // TODO: Add tags when synergy system is implemented
        }

        private void UpdateStatusEffects()
        {
            if (statusEffectContainer == null) return;

            // Clear existing
            foreach (Transform child in statusEffectContainer)
            {
                Destroy(child.gameObject);
            }

            // Show status effects applied by this card
            if (_currentCardData.StatusEffects != null && _currentCardData.StatusEffects.Count > 0)
            {
                foreach (var effectData in _currentCardData.StatusEffects)
                {
                    if (statusEffectPrefab != null && effectData != null)
                    {
                        GameObject effectObj = Instantiate(statusEffectPrefab, statusEffectContainer);

                        // Set up the effect display
                        TextMeshProUGUI effectText = effectObj.GetComponentInChildren<TextMeshProUGUI>();
                        if (effectText != null)
                        {
                            effectText.text = effectData.EffectName;
                        }

                        Image effectIcon = effectObj.GetComponentInChildren<Image>();
                        if (effectIcon != null && effectData.EffectIcon != null)
                        {
                            effectIcon.sprite = effectData.EffectIcon;
                        }
                    }
                }
            }
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
                case CardRarity.Uncommon: return new Color(0.2f, 0.8f, 0.2f);
                case CardRarity.Rare: return new Color(0.2f, 0.4f, 0.8f);
                case CardRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
                case CardRarity.Legendary: return new Color(1f, 0.5f, 0f);
                default: return Color.white;
            }
        }

        #endregion

        #region Positioning

        private void UpdatePosition()
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector2 targetPosition = mousePosition + offset;

            // Keep tooltip on screen
            float rightEdge = targetPosition.x + _rectTransform.rect.width;
            float topEdge = targetPosition.y + _rectTransform.rect.height;

            if (rightEdge > Screen.width)
            {
                targetPosition.x = mousePosition.x - offset.x - _rectTransform.rect.width;
            }

            if (topEdge > Screen.height)
            {
                targetPosition.y = mousePosition.y - offset.y - _rectTransform.rect.height;
            }

            _rectTransform.position = targetPosition;
        }

        #endregion

        #region Animation

        private void Show()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            gameObject.SetActive(true);
            _fadeCoroutine = StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            _isVisible = true;
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * fadeInSpeed;
                float t = fadeCurve.Evaluate(elapsed);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            _isVisible = false;
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * fadeOutSpeed;
                float t = fadeCurve.Evaluate(elapsed);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        #endregion
    }
}