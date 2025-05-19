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

            // Hide initially
            SetVisible(false, false);
        }

        /// <summary>
        /// Initialize the hover effect for a unit
        /// </summary>
        public void Initialize(Unit unit)
        {
            _targetUnit = unit;

            // Determine color based on team
            DetermineEffectColor();

            // Position correctly relative to the unit
            PositionEffect();

            // Show the effect
            Show();
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
            }
            else
            {
                // Default position if no renderer found
                transform.localPosition = new Vector3(0, 0.1f, 0);
            }
        }

        /// <summary>
        /// Determine the effect color based on the unit's team
        /// </summary>
        public void DetermineEffectColor()
        {
            if (_targetUnit == null)
                return;

            // Set color based on team
            switch (_targetUnit.Team)
            {
                case Unit.TeamType.Player:
                    _effectColor = playerColor;
                    break;
                case Unit.TeamType.Enemy:
                    _effectColor = enemyColor;
                    break;
                case Unit.TeamType.Neutral:
                    _effectColor = neutralColor;
                    break;
                default:
                    _effectColor = baseColor;
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
            if (_renderers == null)
                return;

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
            }
        }

        /// <summary>
        /// Set visibility with optional fade
        /// </summary>
        private void SetVisible(bool visible, bool fade = true)
        {
            // Skip if already in the desired state
            if (_isVisible == visible)
                return;

            _isVisible = visible;

            // Stop any active fade
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            // Apply fade or immediate change
            if (fade && fadeEffect)
            {
                _fadeCoroutine = StartCoroutine(FadeEffect(visible));
            }
            else
            {
                // Immediate change
                SetAlpha(visible ? 1f : 0f);
                gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Fade the effect in or out
        /// </summary>
        private IEnumerator FadeEffect(bool fadeIn)
        {
            // Ensure object is active for fade in
            if (fadeIn)
            {
                gameObject.SetActive(true);
            }

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

            // Deactivate object if faded out
            if (!fadeIn)
            {
                gameObject.SetActive(false);
            }

            _fadeCoroutine = null;
        }

        /// <summary>
        /// Set alpha value for all renderers
        /// </summary>
        private void SetAlpha(float alpha)
        {
            if (_renderers == null)
                return;

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
            SetVisible(true);
        }

        /// <summary>
        /// Hide the hover effect
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>
        /// Update the color of the effect
        /// </summary>
        public void UpdateColor(Color color)
        {
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
    }
}