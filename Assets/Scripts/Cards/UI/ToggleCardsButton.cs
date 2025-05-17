using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DarkProtocol.UI;

/// <summary>
/// Manages the toggle button for showing/hiding the player's card hand
/// </summary>
public class ToggleCardsButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Inspector Fields
    [Header("References")]
    [Tooltip("Reference to the CardHandUI component")]
    [SerializeField] private CardHandUI cardHandUI;
    
    [Tooltip("The toggle button component")]
    [SerializeField] private Button toggleButton;
    
    [Header("Visuals")]
    [Tooltip("Icon to show when cards are visible")]
    [SerializeField] private Sprite cardsVisibleIcon;
    
    [Tooltip("Icon to show when cards are hidden")]
    [SerializeField] private Sprite cardsHiddenIcon;
    
    [Tooltip("Image component for the button icon")]
    [SerializeField] private Image buttonIcon;
    
    [Tooltip("Text label for the button")]
    [SerializeField] private TextMeshProUGUI buttonLabel;
    
    [Header("Animation")]
    [Tooltip("Whether to animate the button on hover")]
    [SerializeField] private bool animateOnHover = true;
    
    [Tooltip("Scale multiplier for hover animation")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;
    
    [Tooltip("Duration of hover animation")]
    [SerializeField] private float hoverAnimationDuration = 0.1f;
    
    [Tooltip("Enable pulse animation when cards are hidden")]
    [SerializeField] private bool enablePulseAnimation = true;
    
    [Tooltip("Pulse animation speed")]
    [SerializeField] private float pulseSpeed = 1.5f;
    
    [Tooltip("Pulse animation intensity")]
    [SerializeField] private float pulseIntensity = 0.2f;
    
    [Header("Tooltips")]
    [Tooltip("Tooltip text when cards are visible")]
    [SerializeField] private string visibleTooltip = "Hide Cards";
    
    [Tooltip("Tooltip text when cards are hidden")]
    [SerializeField] private string hiddenTooltip = "Show Cards";
    
    [Tooltip("Tooltip UI element")]
    [SerializeField] private GameObject tooltipObject;
    
    [Tooltip("Tooltip text component")]
    [SerializeField] private TextMeshProUGUI tooltipText;
    
    [Header("Audio")]
    [Tooltip("Audio source for button sounds")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("Sound played when toggling cards")]
    [SerializeField] private AudioClip toggleSound;
    
    [Tooltip("Sound played on button hover")]
    [SerializeField] private AudioClip hoverSound;
    #endregion

    #region Private Variables
    // Original button scale for animations
    private Vector3 _originalScale;
    
    // Whether cards are currently visible
    private bool _cardsVisible = true;
    
    // Animation coroutines
    private Coroutine _hoverAnimationCoroutine;
    private Coroutine _pulseAnimationCoroutine;
    
    // Pulse animation state
    private bool _isPulsing = false;
    
    // Initial button position
    private Vector2 _originalPosition;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _originalScale = transform.localScale;
        
        // Create audio source if needed
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Find CardHandUI if not assigned
        if (cardHandUI == null)
        {
            cardHandUI = FindFirstObjectByType<CardHandUI>();
            if (cardHandUI == null)
            {
                Debug.LogError("No CardHandUI found! ToggleCardsButton requires a CardHandUI component.");
            }
        }
        
        // Set up button
        if (toggleButton == null)
        {
            toggleButton = GetComponent<Button>();
        }
        
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnButtonClicked);
        }
        
        // Hide tooltip by default
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
        
        // Store initial position for animations
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            _originalPosition = rectTransform.anchoredPosition;
        }
    }
    
    private void Start()
    {
        // Initialize state based on CardHandUI
        if (cardHandUI != null)
        {
            _cardsVisible = cardHandUI.IsCardHandVisible();
            UpdateButtonVisuals();
        }
        
        // Start pulse animation if enabled and cards are hidden
        if (enablePulseAnimation && !_cardsVisible)
        {
            StartPulseAnimation();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up button listener
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(OnButtonClicked);
        }
        
        // Stop animations
        if (_hoverAnimationCoroutine != null)
        {
            StopCoroutine(_hoverAnimationCoroutine);
        }
        
        if (_pulseAnimationCoroutine != null)
        {
            StopCoroutine(_pulseAnimationCoroutine);
        }
    }
    #endregion

    #region Button Logic
    /// <summary>
    /// Handle button click
    /// </summary>
    private void OnButtonClicked()
    {
        // Toggle card hand visibility
        if (cardHandUI != null)
        {
            cardHandUI.ToggleCardHandVisibility();
        }
        
        // Update local state
        _cardsVisible = !_cardsVisible;
        
        // Update button visuals
        UpdateButtonVisuals();
        
        // Play sound
        PlaySound(toggleSound);
        
        // Manage pulse animation
        if (enablePulseAnimation)
        {
            if (_cardsVisible)
            {
                StopPulseAnimation();
            }
            else
            {
                StartPulseAnimation();
            }
        }
    }
    
    /// <summary>
    /// Update button visuals based on current state
    /// </summary>
    private void UpdateButtonVisuals()
    {
        // Update icon
        if (buttonIcon != null)
        {
            buttonIcon.sprite = _cardsVisible ? cardsVisibleIcon : cardsHiddenIcon;
        }
        
        // Update label
        if (buttonLabel != null)
        {
            buttonLabel.text = _cardsVisible ? "Hide Cards" : "Show Cards";
        }
        
        // Update tooltip text
        if (tooltipText != null)
        {
            tooltipText.text = _cardsVisible ? visibleTooltip : hiddenTooltip;
        }
    }
    
    /// <summary>
    /// Play a sound
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    #endregion

    #region Animations
    /// <summary>
    /// Start pulse animation
    /// </summary>
    private void StartPulseAnimation()
    {
        if (_isPulsing)
            return;
            
        _isPulsing = true;
        _pulseAnimationCoroutine = StartCoroutine(PulseAnimationCoroutine());
    }
    
    /// <summary>
    /// Stop pulse animation
    /// </summary>
    private void StopPulseAnimation()
    {
        if (!_isPulsing)
            return;
            
        _isPulsing = false;
        
        if (_pulseAnimationCoroutine != null)
        {
            StopCoroutine(_pulseAnimationCoroutine);
            _pulseAnimationCoroutine = null;
        }
        
        // Reset scale
        transform.localScale = _originalScale;
    }
    
    /// <summary>
    /// Pulse animation coroutine
    /// </summary>
    private IEnumerator PulseAnimationCoroutine()
    {
        float time = 0;
        
        while (_isPulsing)
        {
            time += Time.deltaTime * pulseSpeed;
            
            // Calculate pulse scale
            float pulse = 1 + Mathf.Sin(time) * pulseIntensity;
            transform.localScale = _originalScale * pulse;
            
            yield return null;
        }
        
        // Reset scale
        transform.localScale = _originalScale;
    }
    
    /// <summary>
    /// Hover animation
    /// </summary>
    private IEnumerator HoverAnimationCoroutine(bool isHovering)
    {
        Vector3 targetScale = isHovering ? _originalScale * hoverScaleMultiplier : _originalScale;
        Vector3 startScale = transform.localScale;
        float time = 0;
        
        while (time < hoverAnimationDuration)
        {
            time += Time.deltaTime;
            float t = time / hoverAnimationDuration;
            t = Mathf.SmoothStep(0, 1, t); // Smoothstep for more natural animation
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            yield return null;
        }
        
        transform.localScale = targetScale;
        _hoverAnimationCoroutine = null;
    }
    #endregion

    #region Pointer Events
    /// <summary>
    /// Handle pointer enter
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show tooltip
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(true);
        }
        
        // Animate on hover
        if (animateOnHover)
        {
            // Stop any existing animation
            if (_hoverAnimationCoroutine != null)
            {
                StopCoroutine(_hoverAnimationCoroutine);
            }
            
            // Start hover animation
            _hoverAnimationCoroutine = StartCoroutine(HoverAnimationCoroutine(true));
        }
        
        // Play hover sound
        PlaySound(hoverSound);
    }
    
    /// <summary>
    /// Handle pointer exit
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide tooltip
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
        
        // Animate on hover
        if (animateOnHover)
        {
            // Stop any existing animation
            if (_hoverAnimationCoroutine != null)
            {
                StopCoroutine(_hoverAnimationCoroutine);
            }
            
            // Start hover out animation
            _hoverAnimationCoroutine = StartCoroutine(HoverAnimationCoroutine(false));
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Force update of button state based on CardHandUI
    /// </summary>
    public void UpdateState()
    {
        if (cardHandUI != null)
        {
            _cardsVisible = cardHandUI.IsCardHandVisible();
            UpdateButtonVisuals();
            
            // Manage pulse animation
            if (enablePulseAnimation)
            {
                if (_cardsVisible)
                {
                    StopPulseAnimation();
                }
                else
                {
                    StartPulseAnimation();
                }
            }
        }
    }
    
    /// <summary>
    /// Set cards visible/hidden state
    /// </summary>
    public void SetCardsVisible(bool visible)
    {
        if (_cardsVisible != visible)
        {
            OnButtonClicked();
        }
    }
    #endregion
}