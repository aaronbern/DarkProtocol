using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Nameplate that hovers above units showing their status
    /// </summary>
    public class UnitNameplate : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] public Canvas nameplateCanvas;
        [SerializeField] public RectTransform canvasRect;
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] public Image healthBar;
        [SerializeField] public Image healthBarBackground;
        [SerializeField] public Image apBar; // Legacy - still supported for old prefabs
        [SerializeField] public GameObject apContainer;
        [SerializeField] public GameObject shieldIcon;
        [SerializeField] public GameObject stunIcon;
        [SerializeField] public Transform statusIconContainer;
        [SerializeField] public GameObject statusIconPrefab;

        [Header("Settings")]
        [SerializeField] public float heightOffset = 2f;
        [SerializeField] public bool scaleWithDistance = true;
        [SerializeField] public float minScale = 0.5f;
        [SerializeField] public float maxScale = 1.2f;
        [SerializeField] public float scaleDistance = 20f;
        [SerializeField] public bool faceCamera = true;
        [SerializeField] public bool hideWhenOffscreen = true;

        [Header("Animation")]
        [SerializeField] public float damageFlashDuration = 0.3f;
        [SerializeField] public Color damageFlashColor = Color.red;
        [SerializeField] public float healFlashDuration = 0.3f;
        [SerializeField] public Color healFlashColor = Color.green;
        [SerializeField] public AnimationCurve damagePulseCurve;

        [SerializeField] public Image backgroundGradient;
        [SerializeField] public bool adaptiveScaling = true;
        [SerializeField] public bool dynamicRotation = true;

        [Header("Team Colors")]
        [SerializeField] public Color playerHealthColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] public Color enemyHealthColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] public Color neutralHealthColor = new Color(0.8f, 0.8f, 0.2f);

        // Private fields
        public Unit _unit;
        public Camera _mainCamera;
        public Dictionary<string, GameObject> _statusIcons = new Dictionary<string, GameObject>();
        public Coroutine _flashCoroutine;
        public Vector3 _originalScale;
        public bool _isVisible = true;

        // AP segment management
        private List<Image> _apSegments = new List<Image>();
        private bool _apSegmentsInitialized = false;

        public void Awake()
        {
            _mainCamera = Camera.main;
            _originalScale = transform.localScale;

            if (nameplateCanvas == null)
            {
                nameplateCanvas = GetComponentInChildren<Canvas>();
            }

            if (nameplateCanvas != null)
            {
                nameplateCanvas.renderMode = RenderMode.WorldSpace;
                nameplateCanvas.worldCamera = _mainCamera;
            }
        }

        public void Initialize(Unit unit)
        {
            _unit = unit;

            if (_unit == null) return;

            InitializeAPSegments();

            _unit.OnHealthChanged += HandleHealthChanged;
            _unit.OnActionPointsChanged += HandleActionPointsChanged;
            _unit.OnUnitDeath += HandleUnitDeath;
            _unit.OnUnitSelected += HandleUnitSelected;
            _unit.OnUnitDeselected += HandleUnitDeselected;

            UpdateName();
            UpdateHealthBar();
            UpdateAPBar();
            SetTeamColor();

            StatusEffectManager statusManager = _unit.GetComponent<StatusEffectManager>();
            if (statusManager != null)
            {
                UpdateStatusIcons(statusManager.GetAllActiveEffects());
            }
        }

        private void InitializeAPSegments()
        {
            if (_apSegmentsInitialized || apContainer == null) return;

            _apSegments.Clear();

            Image[] segmentImages = apContainer.GetComponentsInChildren<Image>();
            foreach (Image img in segmentImages)
            {
                if (img.gameObject.name.StartsWith("APSegment_"))
                {
                    _apSegments.Add(img);
                }
            }

            _apSegments.Sort((a, b) => string.Compare(a.gameObject.name, b.gameObject.name));
            _apSegmentsInitialized = true;

            Debug.Log($"[UnitNameplate] Initialized {_apSegments.Count} AP segments for {(_unit?.UnitName ?? "unknown unit")}");
        }

        public void Update()
        {
            if (_unit == null || _mainCamera == null) return;

            UpdatePosition();

            if (faceCamera)
            {
                transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward);
            }

            if (scaleWithDistance)
            {
                UpdateScale();
            }

            if (hideWhenOffscreen)
            {
                UpdateVisibility();
            }
        }

        private void UpdatePosition()
        {
            if (_unit == null) return;

            Renderer unitRenderer = _unit.GetComponentInChildren<Renderer>();
            float unitHeight = 1.8f;

            if (unitRenderer != null)
            {
                unitHeight = unitRenderer.bounds.size.y;
            }

            Vector3 targetPosition = _unit.transform.position;
            targetPosition.y += unitHeight + heightOffset;

            transform.position = targetPosition;

            if (nameplateCanvas != null && nameplateCanvas.worldCamera == null)
            {
                nameplateCanvas.worldCamera = _mainCamera;
            }
        }

        public void UpdateScale()
        {
            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float scaleFactor = Mathf.Clamp01(1f - (distance / scaleDistance));
            scaleFactor = Mathf.Lerp(minScale, maxScale, scaleFactor);

            transform.localScale = _originalScale * scaleFactor;
        }

        public void UpdateVisibility()
        {
            Vector3 viewportPoint = _mainCamera.WorldToViewportPoint(transform.position);
            bool shouldBeVisible = viewportPoint.z > 0 &&
                                 viewportPoint.x > 0 && viewportPoint.x < 1 &&
                                 viewportPoint.y > 0 && viewportPoint.y < 1;

            if (shouldBeVisible != _isVisible)
            {
                _isVisible = shouldBeVisible;
                nameplateCanvas.enabled = _isVisible;
            }
        }

        public void UpdateName()
        {
            if (nameText != null && _unit != null)
            {
                nameText.text = _unit.UnitName;
            }
        }

        public void UpdateHealthBar()
        {
            if (healthBar != null && _unit != null)
            {
                float healthPercent = (float)_unit.CurrentHealth / _unit.MaxHealth;
                healthBar.fillAmount = healthPercent;

                Color healthColor = GetTeamColor();
                if (healthPercent < 0.3f)
                {
                    healthColor = Color.Lerp(healthColor, Color.red, 0.5f);
                }

                healthBar.color = healthColor;
            }
        }

        public void UpdateAPBar()
        {
            if (_unit == null) return;

            if (apContainer != null)
            {
                apContainer.SetActive(_unit.Team == Unit.TeamType.Player);
            }

            if (_unit.Team != Unit.TeamType.Player) return;

            if (_apSegments.Count > 0)
            {
                UpdateAPSegments();
            }
            else if (apBar != null)
            {
                float apPercent = (float)_unit.CurrentActionPoints / _unit.MaxActionPoints;
                apBar.fillAmount = apPercent;
            }
        }

        private void UpdateAPSegments()
        {
            if (_unit == null || _apSegments.Count == 0) return;

            int currentAP = _unit.CurrentActionPoints;
            int maxAP = _unit.MaxActionPoints;

            if (maxAP > _apSegments.Count)
            {
                Debug.LogWarning($"[UnitNameplate] Unit {_unit.UnitName} has {maxAP} max AP but only {_apSegments.Count} segments available.");
                maxAP = _apSegments.Count;
            }

            for (int i = 0; i < _apSegments.Count; i++)
            {
                if (i < maxAP)
                {
                    _apSegments[i].gameObject.SetActive(true);

                    if (i < currentAP)
                    {
                        _apSegments[i].color = new Color(0.3f, 0.7f, 1f, 1f);
                        _apSegments[i].transform.localScale = Vector3.one;
                    }
                    else
                    {
                        _apSegments[i].color = new Color(0.1f, 0.1f, 0.15f, 0.6f);
                        _apSegments[i].transform.localScale = Vector3.one * 0.8f;
                    }
                }
                else
                {
                    _apSegments[i].gameObject.SetActive(false);
                }
            }

            Debug.Log($"[UnitNameplate] Updated AP segments for {_unit.UnitName}: {currentAP}/{maxAP} AP, showing {Mathf.Min(maxAP, _apSegments.Count)} of {_apSegments.Count} available segments");
        }

        public void SetTeamColor()
        {
            Color teamColor = GetTeamColor();

            if (healthBar != null)
            {
                healthBar.color = teamColor;
            }
        }

        public Color GetTeamColor()
        {
            if (_unit == null) return Color.white;

            switch (_unit.Team)
            {
                case Unit.TeamType.Player:
                    return playerHealthColor;
                case Unit.TeamType.Enemy:
                    return enemyHealthColor;
                case Unit.TeamType.Neutral:
                    return neutralHealthColor;
                default:
                    return Color.white;
            }
        }

        public void UpdateStatusIcons(List<DarkProtocol.Cards.ActiveStatusEffect> activeEffects)
        {
            foreach (var icon in _statusIcons.Values)
            {
                Destroy(icon);
            }
            _statusIcons.Clear();

            foreach (var effect in activeEffects)
            {
                if (effect.EffectData.EffectIcon != null && statusIconPrefab != null)
                {
                    GameObject iconObj = Instantiate(statusIconPrefab, statusIconContainer);
                    Image iconImage = iconObj.GetComponent<Image>();

                    if (iconImage != null)
                    {
                        iconImage.sprite = effect.EffectData.EffectIcon;

                        if (effect.StackCount > 1)
                        {
                            TextMeshProUGUI stackText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
                            if (stackText != null)
                            {
                                stackText.text = effect.StackCount.ToString();
                            }
                        }
                    }

                    _statusIcons[effect.EffectData.EffectID] = iconObj;
                }
            }
        }

        #region Event Handlers

        public void HandleHealthChanged(int newHealth, int oldHealth)
        {
            UpdateHealthBar();

            if (newHealth < oldHealth)
            {
                FlashNameplate(damageFlashColor, damageFlashDuration);
            }
            else if (newHealth > oldHealth)
            {
                FlashNameplate(healFlashColor, healFlashDuration);
            }
        }

        public void HandleActionPointsChanged(int newAP, int oldAP)
        {
            UpdateAPBar();
            Debug.Log($"[UnitNameplate] AP changed for {(_unit?.UnitName ?? "unknown")}: {oldAP} -> {newAP}");
        }

        public void HandleUnitDeath()
        {
            StartCoroutine(FadeOutAndDestroy());
        }

        public void HandleUnitSelected()
        {
            if (healthBarBackground != null)
            {
                healthBarBackground.color = Color.yellow;
            }
        }

        public void HandleUnitDeselected()
        {
            if (healthBarBackground != null)
            {
                healthBarBackground.color = Color.black;
            }
        }

        #endregion

        #region Animation Methods

        public void FlashNameplate(Color flashColor, float duration)
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(FlashEffect(flashColor, duration));
        }

        public IEnumerator FlashEffect(Color flashColor, float duration)
        {
            Color originalColor = healthBar.color;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (damagePulseCurve != null)
                {
                    float scaleMod = damagePulseCurve.Evaluate(t);
                    transform.localScale = _originalScale * scaleMod;
                }

                healthBar.color = Color.Lerp(flashColor, originalColor, t);

                yield return null;
            }

            healthBar.color = originalColor;
            transform.localScale = _originalScale;
            _flashCoroutine = null;
        }

        public IEnumerator FadeOutAndDestroy()
        {
            CanvasGroup canvasGroup = nameplateCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = nameplateCanvas.gameObject.AddComponent<CanvasGroup>();
            }

            float fadeTime = 1f;
            float elapsed = 0;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeTime);
                yield return null;
            }

            Destroy(gameObject);
        }

        #endregion

        public void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.OnHealthChanged -= HandleHealthChanged;
                _unit.OnActionPointsChanged -= HandleActionPointsChanged;
                _unit.OnUnitDeath -= HandleUnitDeath;
                _unit.OnUnitSelected -= HandleUnitSelected;
                _unit.OnUnitDeselected -= HandleUnitDeselected;
            }
        }
    }
}