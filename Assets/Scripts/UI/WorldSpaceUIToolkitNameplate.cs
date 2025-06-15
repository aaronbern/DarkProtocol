using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Modern world-space nameplate using UI Toolkit
    /// Location: Assets/Scripts/UI/WorldSpaceUIToolkitNameplate.cs
    /// Much cleaner than prefab generation, easier to style
    /// </summary>
    public class WorldSpaceUIToolkitNameplate : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float heightOffset = 1.8f;
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private float minScale = 0.7f;
        [SerializeField] private float maxScale = 1.0f;
        [SerializeField] private float scaleDistance = 25f;

        // UI Elements
        private UIDocument _uiDocument;
        private VisualElement _root;
        private Label _nameLabel;
        private VisualElement _healthContainer;
        private VisualElement _apContainer;
        private VisualElement _statusContainer;

        // Runtime data
        private Unit _unit;
        private Camera _mainCamera;
        private List<VisualElement> _healthSegments = new List<VisualElement>();
        private List<VisualElement> _apSegments = new List<VisualElement>();

        private void Awake()
        {
            _mainCamera = Camera.main;
            CreateWorldSpaceUI();
        }

        private void CreateWorldSpaceUI()
        {
            // Create UI Document with world-space rendering
            _uiDocument = gameObject.AddComponent<UIDocument>();
            
            // Configure for world-space
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.targetTexture = null;
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.scale = 1.0f;
            
            _uiDocument.panelSettings = panelSettings;

            // Create the nameplate structure
            CreateNameplateElements();
        }

        private void CreateNameplateElements()
        {
            _root = new VisualElement();
            _root.name = "nameplate-root";
            
            // Modern XCOM-style container
            _root.style.position = Position.Absolute;
            _root.style.width = 120;
            _root.style.height = 32;
            _root.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);
            _root.style.borderTopWidth = _root.style.borderBottomWidth = 
                _root.style.borderLeftWidth = _root.style.borderRightWidth = 1;
            _root.style.borderTopColor = _root.style.borderBottomColor = 
                _root.style.borderLeftColor = _root.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 0.4f);
            _root.style.borderTopLeftRadius = _root.style.borderTopRightRadius = 
                _root.style.borderBottomLeftRadius = _root.style.borderBottomRightRadius = 2;

            // Unit name
            _nameLabel = new Label("OPERATIVE");
            _nameLabel.style.fontSize = 10;
            _nameLabel.style.color = new Color(0.95f, 0.95f, 0.98f, 0.95f);
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _nameLabel.style.height = 14;
            _nameLabel.style.marginTop = 2;
            _root.Add(_nameLabel);

            // Health bar container
            _healthContainer = new VisualElement();
            _healthContainer.style.flexDirection = FlexDirection.Row;
            _healthContainer.style.height = 4;
            _healthContainer.style.marginTop = 2;
            _healthContainer.style.marginLeft = _healthContainer.style.marginRight = 6;
            _root.Add(_healthContainer);

            // AP bar container (only for player units)
            _apContainer = new VisualElement();
            _apContainer.style.flexDirection = FlexDirection.Row;
            _apContainer.style.height = 4;
            _apContainer.style.marginTop = 1;
            _apContainer.style.marginLeft = _apContainer.style.marginRight = 6;
            _apContainer.style.marginBottom = 3;
            _root.Add(_apContainer);

            // Status icons container
            _statusContainer = new VisualElement();
            _statusContainer.style.position = Position.Absolute;
            _statusContainer.style.bottom = -16;
            _statusContainer.style.left = 0;
            _statusContainer.style.right = 0;
            _statusContainer.style.height = 14;
            _statusContainer.style.flexDirection = FlexDirection.Row;
            _statusContainer.style.justifyContent = Justify.Center;
            _root.Add(_statusContainer);

            // Team indicator
            var teamIndicator = new VisualElement();
            teamIndicator.style.position = Position.Absolute;
            teamIndicator.style.left = -2;
            teamIndicator.style.top = Length.Percent(25);
            teamIndicator.style.width = 2;
            teamIndicator.style.height = Length.Percent(50);
            teamIndicator.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 1f);
            teamIndicator.style.borderTopLeftRadius = teamIndicator.style.borderBottomLeftRadius = 1;
            _root.Add(teamIndicator);

            _uiDocument.rootVisualElement.Add(_root);
        }

        public void Initialize(Unit unit)
        {
            _unit = unit;
            if (_unit == null) return;

            // Subscribe to events
            _unit.OnHealthChanged += HandleHealthChanged;
            _unit.OnActionPointsChanged += HandleActionPointsChanged;

            // Initialize display
            UpdateName();
            CreateHealthSegments();
            CreateAPSegments();
            RefreshDisplay();
        }

        private void CreateHealthSegments()
        {
            _healthContainer.Clear();
            _healthSegments.Clear();

            // Create segments based on max health (up to 10 segments)
            int segmentCount = Mathf.Min(_unit.MaxHealth, 10);
            
            for (int i = 0; i < segmentCount; i++)
            {
                var segment = new VisualElement();
                segment.style.flexGrow = 1;
                segment.style.height = Length.Percent(100);
                segment.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 0.9f);
                segment.style.marginRight = i < segmentCount - 1 ? 1 : 0;
                segment.style.borderTopLeftRadius = segment.style.borderTopRightRadius = 
                    segment.style.borderBottomLeftRadius = segment.style.borderBottomRightRadius = 1;
                
                _healthContainer.Add(segment);
                _healthSegments.Add(segment);
            }
        }

        private void CreateAPSegments()
        {
            if (_unit.Team != Unit.TeamType.Player)
            {
                _apContainer.style.display = DisplayStyle.None;
                return;
            }

            _apContainer.Clear();
            _apSegments.Clear();

            for (int i = 0; i < _unit.MaxActionPoints; i++)
            {
                var segment = new VisualElement();
                segment.style.width = Length.Percent(20);
                segment.style.height = Length.Percent(100);
                segment.style.backgroundColor = new Color(0.3f, 0.7f, 1f, 0.9f);
                segment.style.marginRight = i < _unit.MaxActionPoints - 1 ? 2 : 0;
                segment.style.borderTopLeftRadius = segment.style.borderTopRightRadius = 
                    segment.style.borderBottomLeftRadius = segment.style.borderBottomRightRadius = 1;
                
                _apContainer.Add(segment);
                _apSegments.Add(segment);
            }
        }

        private void UpdateName()
        {
            if (_nameLabel != null && _unit != null)
            {
                _nameLabel.text = _unit.UnitName.ToUpper();
            }
        }

        private void RefreshDisplay()
        {
            UpdateHealthSegments();
            UpdateAPSegments();
        }

        private void UpdateHealthSegments()
        {
            if (_unit == null || _healthSegments.Count == 0) return;

            int currentHealth = _unit.CurrentHealth;
            int segmentCount = _healthSegments.Count;
            int healthPerSegment = Mathf.CeilToInt((float)_unit.MaxHealth / segmentCount);

            for (int i = 0; i < segmentCount; i++)
            {
                var segment = _healthSegments[i];
                int segmentStartHealth = i * healthPerSegment;

                if (currentHealth > segmentStartHealth)
                {
                    // Active segment
                    if (currentHealth < _unit.MaxHealth * 0.3f)
                    {
                        segment.style.backgroundColor = new Color(1f, 0.25f, 0.25f, 0.9f); // Critical
                    }
                    else
                    {
                        segment.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 0.9f); // Healthy
                    }
                }
                else
                {
                    // Damaged segment
                    segment.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.6f);
                }
            }
        }

        private void UpdateAPSegments()
        {
            if (_unit == null || _apSegments.Count == 0) return;

            for (int i = 0; i < _apSegments.Count; i++)
            {
                var segment = _apSegments[i];
                
                if (i < _unit.CurrentActionPoints)
                {
                    segment.style.backgroundColor = new Color(0.3f, 0.7f, 1f, 0.9f);
                    segment.style.scale = new Scale(Vector3.one);
                }
                else
                {
                    segment.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.4f);
                    segment.style.scale = new Scale(Vector3.one * 0.8f);
                }
            }
        }

        public void UpdateStatusIcons(List<DarkProtocol.Cards.ActiveStatusEffect> activeEffects)
        {
            if (_statusContainer == null) return;

            // Clear existing icons
            _statusContainer.Clear();

            // Add new status icons
            foreach (var effect in activeEffects)
            {
                if (effect.EffectData.EffectIcon != null)
                {
                    var statusIcon = CreateStatusIcon(effect);
                    _statusContainer.Add(statusIcon);
                }
            }
        }

        private VisualElement CreateStatusIcon(DarkProtocol.Cards.ActiveStatusEffect effect)
        {
            var iconRoot = new VisualElement();
            iconRoot.style.width = 14;
            iconRoot.style.height = 14;
            iconRoot.style.marginLeft = iconRoot.style.marginRight = 1;

            // Icon background
            iconRoot.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            iconRoot.style.borderTopLeftRadius = iconRoot.style.borderTopRightRadius = 
                iconRoot.style.borderBottomLeftRadius = iconRoot.style.borderBottomRightRadius = 1;

            // Icon image
            iconRoot.style.backgroundImage = new StyleBackground(effect.EffectData.EffectIcon);

            // Stack count if more than 1
            if (effect.StackCount > 1)
            {
                var stackLabel = new Label(effect.StackCount.ToString());
                stackLabel.style.position = Position.Absolute;
                stackLabel.style.bottom = -2;
                stackLabel.style.right = -2;
                stackLabel.style.fontSize = 8;
                stackLabel.style.color = new Color(1f, 1f, 1f, 0.9f);
                stackLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                iconRoot.Add(stackLabel);
            }

            return iconRoot;
        }

        private void Update()
        {
            if (_unit == null || _mainCamera == null) return;

            UpdatePosition();
            UpdateRotation();
            UpdateScale();
        }

        private void UpdatePosition()
        {
            var unitRenderer = _unit.GetComponentInChildren<Renderer>();
            float unitHeight = unitRenderer?.bounds.size.y ?? 1.8f;

            Vector3 worldPosition = _unit.transform.position;
            worldPosition.y += unitHeight + heightOffset;

            transform.position = worldPosition;
        }

        private void UpdateRotation()
        {
            if (faceCamera && _mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward);
            }
        }

        private void UpdateScale()
        {
            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float scaleFactor = Mathf.Clamp01(1f - (distance / scaleDistance));
            scaleFactor = Mathf.Lerp(minScale, maxScale, scaleFactor);

            transform.localScale = Vector3.one * scaleFactor;
        }

        // Event handlers
        private void HandleHealthChanged(int newHealth, int oldHealth)
        {
            UpdateHealthSegments();

            // Flash effect
            if (newHealth < oldHealth)
            {
                _root.experimental.animation.Start(new Color(1f, 0.3f, 0.3f, 0.8f), Color.clear, 300,
                    (element, color) => element.style.backgroundColor = 
                        Color.Lerp(new Color(0.08f, 0.08f, 0.12f, 0.85f), color, color.a));
            }
        }

        private void HandleActionPointsChanged(int newAP, int oldAP)
        {
            UpdateAPSegments();
        }

        private void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.OnHealthChanged -= HandleHealthChanged;
                _unit.OnActionPointsChanged -= HandleActionPointsChanged;
            }
        }
    }
}