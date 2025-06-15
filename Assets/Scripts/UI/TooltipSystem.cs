using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Advanced tooltip system for displaying contextual information
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        #region Singleton
        public static TooltipSystem _instance;
        public static TooltipSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TooltipSystem>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("TooltipSystem");
                        _instance = obj.AddComponent<TooltipSystem>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("Tooltip Prefabs")]
        [SerializeField] public GameObject simpleTooltipPrefab;
        [SerializeField] public GameObject cardTooltipPrefab;
        [SerializeField] public GameObject unitTooltipPrefab;
        [SerializeField] public GameObject statusEffectTooltipPrefab;

        [Header("Settings")]
        [SerializeField] public float showDelay = 0.5f;
        [SerializeField] public float hideDelay = 0f;
        [SerializeField] public Vector2 offset = new Vector2(10, 10);
        [SerializeField] public bool followMouse = true;
        [SerializeField] public float fadeInDuration = 0.2f;
        [SerializeField] public float fadeOutDuration = 0.1f;

        public GameObject _currentTooltip;
        public Coroutine _showCoroutine;
        public Coroutine _hideCoroutine;
        public RectTransform _tooltipRect;
        public CanvasGroup _tooltipCanvasGroup;
        public Canvas _parentCanvas;

        public void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Find or create canvas
            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        _parentCanvas = canvas;
                        break;
                    }
                }
            }
        }

        #region Public Methods

        /// <summary>
        /// Show a simple text tooltip
        /// </summary>
        public static void ShowSimple(string text, Vector2? position = null)
        {
            if (Instance == null) return;
            Instance.ShowSimpleTooltip(text, position);
        }

        /// <summary>
        /// Show a card tooltip
        /// </summary>
        public static void ShowCard(CardData cardData, Vector2? position = null)
        {
            if (Instance == null) return;
            Instance.ShowCardTooltip(cardData, position);
        }

        /// <summary>
        /// Show a unit tooltip
        /// </summary>
        public static void ShowUnit(Unit unit, Vector2? position = null)
        {
            if (Instance == null) return;
            Instance.ShowUnitTooltip(unit, position);
        }

        /// <summary>
        /// Show a status effect tooltip
        /// </summary>
        public static void ShowStatusEffect(StatusEffectData effectData, Vector2? position = null)
        {
            if (Instance == null) return;
            Instance.ShowStatusEffectTooltip(effectData, position);
        }

        /// <summary>
        /// Hide the current tooltip
        /// </summary>
        public static void Hide()
        {
            if (Instance == null) return;
            Instance.HideTooltip();
        }

        #endregion

        #region Tooltip Display Methods

        public void ShowSimpleTooltip(string text, Vector2? position)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Cancel any pending operations
            CancelPendingOperations();

            _showCoroutine = StartCoroutine(ShowTooltipDelayed(() =>
            {
                if (simpleTooltipPrefab == null)
                    return;

                // Create tooltip
                _currentTooltip = Instantiate(simpleTooltipPrefab, _parentCanvas.transform);

                // Set text
                TextMeshProUGUI tooltipText = _currentTooltip.GetComponentInChildren<TextMeshProUGUI>();
                if (tooltipText != null)
                {
                    tooltipText.text = text;
                }

                // Position and show
                SetupTooltip(position);
            }));
        }

        public void ShowCardTooltip(CardData cardData, Vector2? position)
        {
            if (cardData == null)
                return;

            // Cancel any pending operations
            CancelPendingOperations();

            _showCoroutine = StartCoroutine(ShowTooltipDelayed(() =>
            {
                if (cardTooltipPrefab == null)
                    return;

                // Create tooltip
                _currentTooltip = Instantiate(cardTooltipPrefab, _parentCanvas.transform);

                // Get tooltip component
                CardTooltip cardTooltip = _currentTooltip.GetComponent<CardTooltip>();
                if (cardTooltip != null)
                {
                    cardTooltip.SetCardData(cardData);
                }

                // Position and show
                SetupTooltip(position);
            }));
        }

        public void ShowUnitTooltip(Unit unit, Vector2? position)
        {
            if (unit == null)
                return;

            // Cancel any pending operations
            CancelPendingOperations();

            _showCoroutine = StartCoroutine(ShowTooltipDelayed(() =>
            {
                if (unitTooltipPrefab == null)
                    return;

                // Create tooltip
                _currentTooltip = Instantiate(unitTooltipPrefab, _parentCanvas.transform);

                // Get tooltip component
                UnitTooltip unitTooltip = _currentTooltip.GetComponent<UnitTooltip>();
                if (unitTooltip != null)
                {
                    unitTooltip.SetUnit(unit);
                }

                // Position and show
                SetupTooltip(position);
            }));
        }

        public void ShowStatusEffectTooltip(StatusEffectData effectData, Vector2? position)
        {
            if (effectData == null)
                return;

            // Cancel any pending operations
            CancelPendingOperations();

            _showCoroutine = StartCoroutine(ShowTooltipDelayed(() =>
            {
                if (statusEffectTooltipPrefab == null)
                    return;

                // Create tooltip
                _currentTooltip = Instantiate(statusEffectTooltipPrefab, _parentCanvas.transform);

                // Get tooltip component
                StatusEffectTooltip effectTooltip = _currentTooltip.GetComponent<StatusEffectTooltip>();
                if (effectTooltip != null)
                {
                    effectTooltip.SetEffectData(effectData);
                }

                // Position and show
                SetupTooltip(position);
            }));
        }

        public void HideTooltip()
        {
            CancelPendingOperations();

            if (_currentTooltip != null)
            {
                _hideCoroutine = StartCoroutine(HideTooltipDelayed());
            }
        }

        #endregion

        #region Helper Methods

        public void SetupTooltip(Vector2? position)
        {
            if (_currentTooltip == null)
                return;

            // Get components
            _tooltipRect = _currentTooltip.GetComponent<RectTransform>();
            _tooltipCanvasGroup = _currentTooltip.GetComponent<CanvasGroup>();

            if (_tooltipCanvasGroup == null)
            {
                _tooltipCanvasGroup = _currentTooltip.AddComponent<CanvasGroup>();
            }

            // Set initial alpha
            _tooltipCanvasGroup.alpha = 0;

            // Position tooltip
            if (position.HasValue)
            {
                _tooltipRect.position = position.Value;
            }
            else
            {
                PositionTooltipAtMouse();
            }

            // Fade in
            StartCoroutine(FadeIn());
        }

        public void PositionTooltipAtMouse()
        {
            if (_tooltipRect == null)
                return;

            Vector2 mousePosition = Input.mousePosition;
            _tooltipRect.position = mousePosition + offset;

            // Keep tooltip on screen
            KeepTooltipOnScreen();
        }

        public void KeepTooltipOnScreen()
        {
            if (_tooltipRect == null || _parentCanvas == null)
                return;

            Vector3[] corners = new Vector3[4];
            _tooltipRect.GetWorldCorners(corners);

            RectTransform canvasRect = _parentCanvas.GetComponent<RectTransform>();

            // Check if tooltip goes off screen
            float minX = corners[0].x;
            float maxX = corners[2].x;
            float minY = corners[0].y;
            float maxY = corners[1].y;

            Vector2 adjustment = Vector2.zero;

            // Adjust X
            if (maxX > Screen.width)
            {
                adjustment.x = Screen.width - maxX - 10;
            }
            else if (minX < 0)
            {
                adjustment.x = -minX + 10;
            }

            // Adjust Y
            if (maxY > Screen.height)
            {
                adjustment.y = Screen.height - maxY - 10;
            }
            else if (minY < 0)
            {
                adjustment.y = -minY + 10;
            }

            // Apply adjustment
            if (adjustment != Vector2.zero)
            {
                _tooltipRect.position += (Vector3)adjustment;
            }
        }

        public void Update()
        {
            // Follow mouse if enabled
            if (followMouse && _currentTooltip != null && _tooltipRect != null)
            {
                PositionTooltipAtMouse();
            }
        }

        public void CancelPendingOperations()
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
        }

        #endregion

        #region Coroutines

        public IEnumerator ShowTooltipDelayed(System.Action createTooltip)
        {
            yield return new WaitForSeconds(showDelay);

            createTooltip?.Invoke();

            _showCoroutine = null;
        }

        public IEnumerator HideTooltipDelayed()
        {
            yield return new WaitForSeconds(hideDelay);

            // Fade out
            yield return StartCoroutine(FadeOut());

            // Destroy
            if (_currentTooltip != null)
            {
                Destroy(_currentTooltip);
                _currentTooltip = null;
            }

            _hideCoroutine = null;
        }

        public IEnumerator FadeIn()
        {
            if (_tooltipCanvasGroup == null)
                yield break;

            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                _tooltipCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            _tooltipCanvasGroup.alpha = 1;
        }

        public IEnumerator FadeOut()
        {
            if (_tooltipCanvasGroup == null)
                yield break;

            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _tooltipCanvasGroup.alpha = 1 - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }

            _tooltipCanvasGroup.alpha = 0;
        }

        #endregion
    }
}