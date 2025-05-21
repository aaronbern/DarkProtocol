using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the "Start Unit Turn" button functionality
/// </summary>
public class StartUnitTurnButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button startTurnButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Animation")]
    [SerializeField] private bool animateButton = true;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseIntensity = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    private Unit _targetUnit;
    private Vector3 _originalScale;
    private Coroutine _pulseCoroutine;

    private void Awake()
    {
        // Cache original scale
        _originalScale = transform.localScale;

        // Add click listener
        if (startTurnButton != null)
        {
            startTurnButton.onClick.AddListener(OnButtonClicked);
        }

        // Create audio source if needed
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Hide button by default
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up
        if (startTurnButton != null)
        {
            startTurnButton.onClick.RemoveListener(OnButtonClicked);
        }

        StopPulseAnimation();
    }

    /// <summary>
    /// Set up the button for a specific unit
    /// </summary>
    public void SetupForUnit(Unit unit)
    {
        _targetUnit = unit;

        if (unit != null && buttonText != null)
        {
            buttonText.text = $"Start {unit.UnitName}'s Turn";
        }

        // Show button
        gameObject.SetActive(unit != null);

        // Start pulse animation
        if (animateButton && unit != null)
        {
            StartPulseAnimation();
        }
    }

    /// <summary>
    /// Handle button click
    /// </summary>
    private void OnButtonClicked()
    {
        if (_targetUnit == null || GameManager.Instance == null)
            return;

        // Play sound
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Start the unit's turn
        GameManager.Instance.StartUnitTurn(_targetUnit);

        // Hide the button
        gameObject.SetActive(false);

        // Stop animation
        StopPulseAnimation();
    }

    /// <summary>
    /// Start pulse animation
    /// </summary>
    private void StartPulseAnimation()
    {
        // Stop any existing animation
        StopPulseAnimation();

        // Start new animation
        _pulseCoroutine = StartCoroutine(PulseAnimationCoroutine());
    }

    /// <summary>
    /// Stop pulse animation
    /// </summary>
    private void StopPulseAnimation()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        // Reset scale
        transform.localScale = _originalScale;
    }

    /// <summary>
    /// Pulse animation coroutine
    /// </summary>
    private System.Collections.IEnumerator PulseAnimationCoroutine()
    {
        float time = 0;

        while (true)
        {
            time += Time.deltaTime * pulseSpeed;

            // Calculate pulse scale
            float pulse = 1 + Mathf.Sin(time) * pulseIntensity;
            transform.localScale = _originalScale * pulse;

            yield return null;
        }
    }
}