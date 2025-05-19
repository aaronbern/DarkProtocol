using UnityEngine;
using System.Collections;

namespace DarkProtocol.Units
{
    /// <summary>
    /// Component that manages the visual appearance and animation of unit hover effects.
    /// Meant to be attached to hover effect game objects that are children of units.
    /// </summary>
    [AddComponentMenu("Dark Protocol/Units/Unit Hover Effect")]
    public class UnitHoverEffect : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Appearance")]
        [Tooltip("Base color of the effect")]
        [SerializeField] private Color baseColor = Color.white;

        [Tooltip("Highlight color for player units")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.6f, 1f);

        [Tooltip("Highlight color for enemy units")]
        [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);

        [Tooltip("Highlight color for neutral units")]
        [SerializeField] private Color neutralColor = new Color(0.8f, 0.8f, 0.2f);

        [Header("Animation")]
        [Tooltip("Whether to animate the effect")]
        [SerializeField] private bool animate = true;

        [Tooltip("Animation speed")]
        [SerializeField] private float animationSpeed = 1f;

        [Tooltip("Pulse intensity")]
        [SerializeField] private float pulseIntensity = 0.2f;

        [Tooltip("Rotation speed (degrees per second)")]
        [SerializeField] private float rotationSpeed = 30f;

        [Header("Fade")]
        [Tooltip("Whether to fade in/out the effect")]
        [SerializeField] private bool fadeEffect = true;

        [Tooltip("Fade duration")]
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Debug")]
        [Tooltip("Enable debug logs")]
        [SerializeField] private bool debugLogging = false;
        #endregion

        #region Private Variables
        private Unit _targetUnit;
        private bool _isVisible = false;
        private float _animationTime = 0f;
        private Color _effectColor;
        private MeshRenderer[] _renderers;
        private Coroutine _fadeCoroutine;
        #endregion

        #region Initialization and Lifecycle
        private void Awake()
        {
            // Cache renderers
            _renderers = GetComponentsInChildren<MeshRenderer>();

            if (_renderers.Length == 0)
            {
                Debug.LogWarning($"[UnitHoverEffect] No MeshRenderers found in {gameObject.name}! The effect will be invisible.");

                // Try to find any renderer type as a fallback
                Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
                if (allRenderers.Length > 0)
                {
                    Debug.Log($"[UnitHoverEffect] Found {allRenderers.Length} non-mesh renderers instead.");
                }
            }
            else
            {
                DebugLog($"Found {_renderers.Length} mesh renderers in {gameObject.name}");
            }

            // Ensure object is active before Awake completes
            if (!gameObject.activeSelf)
            {
                DebugLog($"[CRITICAL] GameObject {gameObject.name} is inactive during Awake!");
                gameObject.SetActive(true);
            }

            // Hide initially but keep GameObject active
            SetAlpha(0f);
            _isVisible = false;
        }

        /// <summary>
        /// Initialize the hover effect for a unit
        /// </summary>
        public void Initialize(Unit unit)
        {
            _targetUnit = unit;

            DebugLog($"Initializing effect for unit: {(unit != null ? unit.name : "null")}");

            // Ensure gameObject is active before proceeding
            if (!gameObject.activeSelf)
            {
                DebugLog("[CRITICAL] GameObject is inactive during Initialize. Activating it.");
                gameObject.SetActive(true);
            }

            // Determine color based on team
            DetermineEffectColor();

            // Position correctly relative to the unit
            PositionEffect();
        }

        private void Update()
        {
            if (!_isVisible || !animate)
                return;

            // Update animation time
            _animationTime += Time.deltaTime * animationSpeed;

            // Apply pulse animation
            float pulseScale = 1f + Mathf.Sin(_animationTime) * pulseIntensity;
            transform.localScale = Vector3.one * pulseScale;

            // Apply rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }
        #endregion

        #region Visual Methods
        /// <summary>
        /// Position the effect correctly relative to the unit
        /// </summary>
        private void PositionEffect()
        {
            if (_targetUnit == null)
                return;

            // Reset local position and rotation
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // Adjust position based on unit bounds
            Renderer unitRenderer = _targetUnit.GetComponentInChildren<Renderer>();
            if (unitRenderer != null)
            {
                // Position at the bottom of the unit's bounds
                Bounds bounds = unitRenderer.bounds;
                Vector3 bottomCenter = new Vector3(0, -bounds.extents.y, 0);
                transform.localPosition = bottomCenter;
                DebugLog($"Positioned effect at y={bottomCenter.y} based on unit bounds");
            }
            else
            {
                // Default position if no renderer found
                transform.localPosition = new Vector3(0, 0.1f, 0);
                DebugLog("No unit renderer found, using default position");
            }
        }

        /// <summary>
        /// Determine the effect color based on the unit's team
        /// </summary>
        public void DetermineEffectColor()
        {
            if (_targetUnit == null)
            {
                DebugLog("No target unit, using base color");
                _effectColor = baseColor;
                ApplyEffectColor(_effectColor);
                return;
            }

            // Set color based on team
            switch (_targetUnit.Team)
            {
                case Unit.TeamType.Player:
                    _effectColor = playerColor;
                    DebugLog($"Using player color for {_targetUnit.name}");
                    break;
                case Unit.TeamType.Enemy:
                    _effectColor = enemyColor;
                    DebugLog($"Using enemy color for {_targetUnit.name}");
                    break;
                case Unit.TeamType.Neutral:
                    _effectColor = neutralColor;
                    DebugLog($"Using neutral color for {_targetUnit.name}");
                    break;
                default:
                    _effectColor = baseColor;
                    DebugLog($"Using base color for {_targetUnit.name} (unknown team)");
                    break;
            }

            // Apply color to renderers
            ApplyEffectColor(_effectColor);
        }

        /// <summary>
        /// Apply the effect color to all renderers
        /// </summary>
        private void ApplyEffectColor(Color color)
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                DebugLog("Cannot apply color: No renderers available");
                return;
            }

            DebugLog($"Applying color: {color} to {_renderers.Length} renderers");

            foreach (MeshRenderer renderer in _renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Check if material has emission
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.SetColor("_EmissionColor", color * 0.8f);
                        renderer.material.EnableKeyword("_EMISSION");
                    }

                    // Set main color
                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = color;
                    }
                }
                else if (renderer == null)
                {
                    DebugLog("Null renderer found in array!");
                }
                else if (renderer.material == null)
                {
                    DebugLog($"Null material on renderer {renderer.name}!");
                }
            }
        }

        /// <summary>
        /// Set visibility with optional fade
        /// </summary>
        private void SetVisible(bool visible, bool fade = true)
        {
            // Skip if already in the desired state
            if (_isVisible == visible)
            {
                DebugLog($"Already in desired visibility state: {visible}. Skipping.");
                return;
            }

            DebugLog($"Setting visibility to {visible} with fade={fade}");

            // Ensure gameObject is active for any visibility operations
            if (!gameObject.activeSelf)
            {
                DebugLog("[CRITICAL] GameObject inactive when trying to change visibility! Activating it.");
                gameObject.SetActive(true);
            }

            _isVisible = visible;

            // Stop any active fade
            if (_fadeCoroutine != null)
            {
                DebugLog("Stopping existing fade coroutine");
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            // Apply fade or immediate change
            if (fade && fadeEffect && gameObject.activeInHierarchy)
            {
                try
                {
                    DebugLog("Starting fade coroutine");
                    _fadeCoroutine = StartCoroutine(FadeEffect(visible));
                }
                catch (System.Exception ex)
                {
                    // If coroutine fails, fall back to immediate change
                    Debug.LogError($"[UnitHoverEffect] Error starting fade coroutine: {ex.Message}. Using immediate change instead.");
                    SetAlpha(visible ? 1f : 0f);
                }
            }
            else
            {
                // Immediate change
                DebugLog("Using immediate visibility change");
                SetAlpha(visible ? 1f : 0f);

                // IMPORTANT: Keep the GameObject active even when not visible
                // Only change the alpha, not the active state
                // This prevents the coroutine error
            }
        }

        /// <summary>
        /// Fade the effect in or out
        /// </summary>
        private IEnumerator FadeEffect(bool fadeIn)
        {
            DebugLog($"FadeEffect started: fadeIn={fadeIn}");

            // Ensure object stays active for the entire fade
            gameObject.SetActive(true);

            float startAlpha = fadeIn ? 0f : 1f;
            float targetAlpha = fadeIn ? 1f : 0f;
            float time = 0f;

            // Set initial alpha
            SetAlpha(startAlpha);

            // Fade over time
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(time / fadeDuration);
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);

                SetAlpha(currentAlpha);

                yield return null;
            }

            // Set final alpha
            SetAlpha(targetAlpha);

            // IMPORTANT: Keep the GameObject active even when not visible
            // Only change the alpha, not the active state
            // This prevents the coroutine error

            _fadeCoroutine = null;
            DebugLog("FadeEffect completed");
        }

        /// <summary>
        /// Set alpha value for all renderers
        /// </summary>
        private void SetAlpha(float alpha)
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                DebugLog("Cannot set alpha: No renderers available");
                return;
            }

            DebugLog($"Setting alpha to {alpha} on {_renderers.Length} renderers");

            foreach (MeshRenderer renderer in _renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Get current color
                    Color color = renderer.material.color;

                    // Set alpha
                    color.a = alpha;

                    // Apply color
                    renderer.material.color = color;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Show the hover effect
        /// </summary>
        public void Show()
        {
            DebugLog("Show called");

            // Ensure gameObject is active
            if (!gameObject.activeSelf)
            {
                DebugLog("[CRITICAL] GameObject is inactive when Show was called! Activating it.");
                gameObject.SetActive(true);
            }

            SetVisible(true);
        }

        /// <summary>
        /// Hide the hover effect
        /// </summary>
        public void Hide()
        {
            DebugLog("Hide called");

            // Check if GameObject is active before trying to hide
            if (!gameObject.activeSelf)
            {
                DebugLog("[CRITICAL] GameObject is already inactive when Hide was called!");
                return;
            }

            SetVisible(false);
        }

        /// <summary>
        /// Update the color of the effect
        /// </summary>
        public void UpdateColor(Color color)
        {
            DebugLog($"UpdateColor called with {color}");
            _effectColor = color;
            ApplyEffectColor(color);
        }

        /// <summary>
        /// Set whether the effect should animate
        /// </summary>
        public void SetAnimationEnabled(bool enabled)
        {
            animate = enabled;
        }
        #endregion

        #region Debug
        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[UnitHoverEffect] {message}");
            }
        }
        #endregion
    }
}