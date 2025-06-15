using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DarkProtocol.UI
{
    /// <summary>
    /// A reusable segmented bar component that can be used for health, AP, or any other resource.
    /// Provides XCOM-style segmented display for any numeric value.
    /// </summary>
    public class SegmentedBarComponent : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color inactiveColor = Color.gray;
        [SerializeField] private float inactiveAlpha = 0.3f;
        [SerializeField] private float segmentSpacing = 0.1f; // Spacing between segments as percentage of segment width

        [Header("Configuration")]
        [SerializeField] private int segmentCount = 8;
        [SerializeField] private bool showInactiveSegments = true;
        [SerializeField] private bool animateChanges = true;
        [SerializeField] private float animationSpeed = 5f;

        [Header("Runtime Values")]
        [SerializeField] private float currentValue = 5f;
        [SerializeField] private float maxValue = 10f;

        // List of segment image components
        private List<Image> _segments = new List<Image>();
        private float _targetFillAmount = 0f;
        private int _targetActiveSegments = 0;

        private void Awake()
        {
            // Initialize if we have no segments yet
            if (_segments.Count == 0)
            {
                CreateSegments();
            }
        }

        private void Update()
        {
            // Only update if animation is enabled
            if (animateChanges)
            {
                UpdateSegmentAnimation();
            }
        }

        /// <summary>
        /// Create the segments dynamically based on the segment count
        /// </summary>
        public void CreateSegments()
        {
            // Clear existing segments
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            _segments.Clear();

            // Create new segments
            float segmentWidth = 1f / segmentCount;
            float effectiveSegmentWidth = segmentWidth * (1f - segmentSpacing);
            float halfSpacing = segmentWidth * segmentSpacing * 0.5f;

            for (int i = 0; i < segmentCount; i++)
            {
                GameObject segment = new GameObject($"Segment_{i}");
                segment.transform.SetParent(transform, false);

                // Add image component
                Image segmentImage = segment.AddComponent<Image>();
                segmentImage.color = activeColor;
                segmentImage.raycastTarget = false;

                // Configure RectTransform
                RectTransform segmentRect = segment.GetComponent<RectTransform>();
                float startX = i * segmentWidth + halfSpacing;
                float endX = (i + 1) * segmentWidth - halfSpacing;

                segmentRect.anchorMin = new Vector2(startX, 0);
                segmentRect.anchorMax = new Vector2(endX, 1);
                segmentRect.offsetMin = Vector2.zero;
                segmentRect.offsetMax = Vector2.zero;

                // Add to list
                _segments.Add(segmentImage);
            }

            // Initial update
            SetValue(currentValue, maxValue);
        }

        /// <summary>
        /// Sets the number of segments in the bar
        /// </summary>
        public void SetSegmentCount(int count)
        {
            if (count <= 0) return;
            segmentCount = count;
            CreateSegments();
        }

        /// <summary>
        /// Sets the value and max value of the bar
        /// </summary>
        public void SetValue(float value, float max)
        {
            currentValue = Mathf.Clamp(value, 0, max);
            maxValue = Mathf.Max(max, 0.01f); // Prevent division by zero

            float fillPercentage = currentValue / maxValue;
            _targetFillAmount = fillPercentage;

            // Calculate active segments
            float valuePerSegment = maxValue / segmentCount;
            _targetActiveSegments = Mathf.CeilToInt(currentValue / valuePerSegment);

            // If not animating, update immediately
            if (!animateChanges)
            {
                UpdateSegmentDisplay();
            }
        }

        /// <summary>
        /// Sets the colors for active and inactive segments
        /// </summary>
        public void SetColors(Color active, Color? inactive = null)
        {
            activeColor = active;
            if (inactive.HasValue)
            {
                inactiveColor = inactive.Value;
            }
            else
            {
                // Default to a darkened version of the active color
                inactiveColor = new Color(
                    activeColor.r * 0.5f,
                    activeColor.g * 0.5f,
                    activeColor.b * 0.5f,
                    activeColor.a * inactiveAlpha
                );
            }

            // Update all segments
            UpdateSegmentDisplay();
        }

        /// <summary>
        /// Animates segment display to target values
        /// </summary>
        private void UpdateSegmentAnimation()
        {
            UpdateSegmentDisplay();
        }

        /// <summary>
        /// Updates the segment display based on current values
        /// </summary>
        private void UpdateSegmentDisplay()
        {
            if (_segments.Count == 0) return;

            // Calculate value per segment
            float valuePerSegment = maxValue / segmentCount;

            // Update each segment
            for (int i = 0; i < _segments.Count; i++)
            {
                Image segment = _segments[i];

                if (i < _targetActiveSegments)
                {
                    // Active segment
                    segment.enabled = true;

                    // Full color for active segments
                    segment.color = activeColor;
                }
                else
                {
                    // Inactive segment
                    if (showInactiveSegments)
                    {
                        segment.enabled = true;

                        // Fade color for inactive segments
                        segment.color = new Color(
                            inactiveColor.r,
                            inactiveColor.g,
                            inactiveColor.b,
                            inactiveColor.a * inactiveAlpha
                        );
                    }
                    else
                    {
                        segment.enabled = false;
                    }
                }
            }
        }
    }
}