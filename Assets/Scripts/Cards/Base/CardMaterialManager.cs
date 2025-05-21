using System.Collections.Generic;
using UnityEngine;
using DarkProtocol.Cards;

namespace DarkProtocol.Materials
{
    /// <summary>
    /// Manages card materials and visual effects for Dark Protocol cards
    /// Works with Standard rendering pipeline rather than URP
    /// </summary>
    [RequireComponent(typeof(Card))]
    public class CardMaterialManager : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Material References")]
        [SerializeField] private Renderer cardFaceRenderer;
        [SerializeField] private Renderer cardBackRenderer;
        [SerializeField] private Transform cardPivot;

        [Header("Material Assets")]
        [SerializeField] private Material cardFaceMaterial;
        [SerializeField] private Material cardBackMaterial;
        [SerializeField] private Material cardEffectMaterial;

        [Header("Texture Assets")]
        [SerializeField] private Texture2D defaultCardMask;
        [SerializeField] private Texture2D defaultCardNormalMap;
        [SerializeField] private Texture2D noiseTexture;
        [SerializeField] private Texture2D flowMapTexture;

        [Header("Card Visual Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 1f);
        [SerializeField] private float highlightIntensity = 0.6f;
        [SerializeField] private Color rimLightColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private float rimLightPower = 3f;
        [SerializeField] private float cardAnimationSpeed = 5f;

        [Header("Card Effect Settings")]
        [SerializeField] private ParticleSystem cardPlayParticles;
        [SerializeField] private GameObject cardEffectObject;
        [SerializeField] private float effectDuration = 1.5f;
        [SerializeField] private Light cardLight;

        [Header("Animation Settings")]
        [SerializeField] private bool animateCardBack = true;
        [SerializeField] private float backPatternSpeed = 0.5f;
        [SerializeField] private float backPatternIntensity = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        #endregion

        #region Private Fields
        // Component references
        private Card _card;
        private MaterialPropertyBlock _facePropertyBlock;
        private MaterialPropertyBlock _backPropertyBlock;
        private MaterialPropertyBlock _effectPropertyBlock;

        // Card state
        private bool _isSelected = false;
        private bool _isHovering = false;
        private bool _isPlayable = true;
        private float _currentHighlightIntensity = 0f;

        // Effect state
        private bool _effectActive = false;
        private float _effectTimer = 0f;

        // Material instances
        private Material _instantiatedFaceMaterial;
        private Material _instantiatedBackMaterial;
        private Material _instantiatedEffectMaterial;

        // Static property IDs (for performance)
        private static class Props
        {
            // Card face properties
            public static readonly int MainTex = Shader.PropertyToID("_MainTex");
            public static readonly int MaskTex = Shader.PropertyToID("_MaskTex");
            public static readonly int NormalMap = Shader.PropertyToID("_NormalMap");
            public static readonly int CardColor = Shader.PropertyToID("_CardColor");
            public static readonly int HighlightColor = Shader.PropertyToID("_HighlightColor");
            public static readonly int HighlightIntensity = Shader.PropertyToID("_HighlightIntensity");
            public static readonly int RimColor = Shader.PropertyToID("_RimColor");
            public static readonly int RimPower = Shader.PropertyToID("_RimPower");
            public static readonly int RarityColor = Shader.PropertyToID("_RarityColor");
            public static readonly int RarityIntensity = Shader.PropertyToID("_RarityIntensity");
            public static readonly int RarityPulse = Shader.PropertyToID("_RarityPulse");
            public static readonly int APCost = Shader.PropertyToID("_APCost");
            public static readonly int MPCost = Shader.PropertyToID("_MPCost");
            public static readonly int DamageAmount = Shader.PropertyToID("_DamageAmount");
            public static readonly int HealAmount = Shader.PropertyToID("_HealAmount");
            public static readonly int Selected = Shader.PropertyToID("_Selected");
            public static readonly int Playable = Shader.PropertyToID("_Playable");
            public static readonly int Hovering = Shader.PropertyToID("_Hovering");

            // Card back properties
            public static readonly int TeamColor = Shader.PropertyToID("_TeamColor");
            public static readonly int PatternSpeed = Shader.PropertyToID("_PatternSpeed");
            public static readonly int PatternIntensity = Shader.PropertyToID("_PatternIntensity");
            public static readonly int AnimateBack = Shader.PropertyToID("_AnimateBack");

            // Effect properties
            public static readonly int Color = Shader.PropertyToID("_Color");
            public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
            public static readonly int EmissionIntensity = Shader.PropertyToID("_EmissionIntensity");
            public static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");
            public static readonly int DissolveEdgeWidth = Shader.PropertyToID("_DissolveEdgeWidth");
            public static readonly int DissolveEdgeColor = Shader.PropertyToID("_DissolveEdgeColor");
            public static readonly int FlowSpeed = Shader.PropertyToID("_FlowSpeed");
            public static readonly int FlowIntensity = Shader.PropertyToID("_FlowIntensity");
            public static readonly int EffectType = Shader.PropertyToID("_EffectType");
            public static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");
            public static readonly int FlowMap = Shader.PropertyToID("_FlowMap");
            public static readonly int UseWorldCoords = Shader.PropertyToID("_UseWorldCoords");
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get component references
            _card = GetComponent<Card>();

            // Create property blocks
            _facePropertyBlock = new MaterialPropertyBlock();
            _backPropertyBlock = new MaterialPropertyBlock();
            _effectPropertyBlock = new MaterialPropertyBlock();

            // Ensure we have renderers
            if (cardFaceRenderer == null)
            {
                cardFaceRenderer = GetComponentInChildren<Renderer>();
                DebugLog("Card face renderer not assigned, trying to find one in children");
            }

            if (cardBackRenderer == null && cardFaceRenderer != null)
            {
                // Might be the same renderer with multiple materials
                cardBackRenderer = cardFaceRenderer;
                DebugLog("Card back renderer not assigned, using face renderer");
            }

            // Initialize card pivot if not assigned
            if (cardPivot == null)
            {
                cardPivot = transform;
                DebugLog("Card pivot not assigned, using card transform");
            }

            // Initialize effect object
            if (cardEffectObject != null)
            {
                cardEffectObject.SetActive(false);
            }

            // Initialize light
            if (cardLight != null)
            {
                cardLight.enabled = false;
            }
        }

        private void Start()
        {
            // Initial setup of card materials
            InitializeCardMaterials();

            // Subscribe to card events if card component is available
            if (_card != null)
            {
                _card.OnCardHoverEnter += HandleCardHoverEnter;
                _card.OnCardHoverExit += HandleCardHoverExit;
                _card.OnCardClicked += HandleCardClicked;
            }

            // Update material properties initially
            UpdateMaterialProperties();
        }

        private void OnDestroy()
        {
            // Unsubscribe from card events
            if (_card != null)
            {
                _card.OnCardHoverEnter -= HandleCardHoverEnter;
                _card.OnCardHoverExit -= HandleCardHoverExit;
                _card.OnCardClicked -= HandleCardClicked;
            }

            // Clean up instantiated materials
            if (_instantiatedFaceMaterial != null)
            {
                Destroy(_instantiatedFaceMaterial);
            }

            if (_instantiatedBackMaterial != null)
            {
                Destroy(_instantiatedBackMaterial);
            }

            if (_instantiatedEffectMaterial != null)
            {
                Destroy(_instantiatedEffectMaterial);
            }
        }

        private void Update()
        {
            // Smoothly update highlight intensity
            float targetHighlight = _isSelected ? highlightIntensity : (_isHovering ? highlightIntensity * 0.5f : 0f);
            _currentHighlightIntensity = Mathf.Lerp(_currentHighlightIntensity, targetHighlight, Time.deltaTime * cardAnimationSpeed);

            // Update material properties if changed
            if (Mathf.Abs(_currentHighlightIntensity - targetHighlight) > 0.01f)
            {
                UpdateMaterialProperties();
            }

            // Handle active effects
            if (_effectActive)
            {
                _effectTimer -= Time.deltaTime;

                // Update effect properties
                if (cardEffectObject != null && cardEffectObject.activeSelf)
                {
                    Renderer effectRenderer = cardEffectObject.GetComponent<Renderer>();
                    if (effectRenderer != null)
                    {
                        // Get property block
                        effectRenderer.GetPropertyBlock(_effectPropertyBlock);

                        // Update dissolve or other time-based properties
                        float normalizedTime = 1f - (_effectTimer / effectDuration);
                        _effectPropertyBlock.SetFloat(Props.DissolveAmount, normalizedTime);

                        // Apply property block
                        effectRenderer.SetPropertyBlock(_effectPropertyBlock);
                    }
                }

                // End effect when timer expires
                if (_effectTimer <= 0f)
                {
                    EndCardEffect();
                }
            }
        }
        #endregion

        #region Material Setup
        /// <summary>
        /// Initializes and sets up card materials
        /// </summary>
        private void InitializeCardMaterials()
        {
            // Create and setup card face material
            if (cardFaceRenderer != null)
            {
                // If a material is assigned, use it. Otherwise, look for the shader and create one
                if (cardFaceMaterial != null)
                {
                    _instantiatedFaceMaterial = new Material(cardFaceMaterial);
                }
                else
                {
                    Shader cardFaceShader = Shader.Find("DarkProtocol/CardStandard");
                    if (cardFaceShader != null)
                    {
                        _instantiatedFaceMaterial = new Material(cardFaceShader);
                        DebugLog("Created card face material from shader");
                    }
                    else
                    {
                        // Fallback to standard shader
                        _instantiatedFaceMaterial = new Material(Shader.Find("Standard"));
                        DebugLog("WARNING: DarkProtocol/CardStandard shader not found, using Standard shader");
                    }
                }

                // Apply the material
                cardFaceRenderer.material = _instantiatedFaceMaterial;

                // If card has multiple materials, ensure we only replace the first one
                if (cardFaceRenderer.materials.Length > 1)
                {
                    Material[] materials = cardFaceRenderer.materials;
                    materials[0] = _instantiatedFaceMaterial;
                    cardFaceRenderer.materials = materials;
                }
            }

            // Create and setup card back material (if it's a different renderer)
            if (cardBackRenderer != null && cardBackRenderer != cardFaceRenderer)
            {
                // If a material is assigned, use it. Otherwise, look for the shader and create one
                if (cardBackMaterial != null)
                {
                    _instantiatedBackMaterial = new Material(cardBackMaterial);
                }
                else
                {
                    Shader cardBackShader = Shader.Find("DarkProtocol/CardBack");
                    if (cardBackShader != null)
                    {
                        _instantiatedBackMaterial = new Material(cardBackShader);
                        DebugLog("Created card back material from shader");
                    }
                    else
                    {
                        // Fallback to standard shader
                        _instantiatedBackMaterial = new Material(Shader.Find("Standard"));
                        DebugLog("WARNING: DarkProtocol/CardBack shader not found, using Standard shader");
                    }
                }

                // Apply the material
                cardBackRenderer.material = _instantiatedBackMaterial;
            }
            // If same renderer but multiple materials, set up the back material as the second material
            else if (cardBackRenderer != null && cardBackRenderer.materials.Length > 1)
            {
                if (cardBackMaterial != null)
                {
                    _instantiatedBackMaterial = new Material(cardBackMaterial);
                }
                else
                {
                    Shader cardBackShader = Shader.Find("DarkProtocol/CardBack");
                    if (cardBackShader != null)
                    {
                        _instantiatedBackMaterial = new Material(cardBackShader);
                        DebugLog("Created card back material from shader");
                    }
                    else
                    {
                        // Fallback to standard shader
                        _instantiatedBackMaterial = new Material(Shader.Find("Standard"));
                        DebugLog("WARNING: DarkProtocol/CardBack shader not found, using Standard shader");
                    }
                }

                // Apply as second material
                Material[] materials = cardBackRenderer.materials;
                materials[1] = _instantiatedBackMaterial;
                cardBackRenderer.materials = materials;
            }

            // Setup card effect material if needed
            if (cardEffectObject != null)
            {
                Renderer effectRenderer = cardEffectObject.GetComponent<Renderer>();
                if (effectRenderer != null)
                {
                    // If a material is assigned, use it. Otherwise, look for the shader and create one
                    if (cardEffectMaterial != null)
                    {
                        _instantiatedEffectMaterial = new Material(cardEffectMaterial);
                    }
                    else
                    {
                        Shader effectShader = Shader.Find("DarkProtocol/CardEffect");
                        if (effectShader != null)
                        {
                            _instantiatedEffectMaterial = new Material(effectShader);
                            DebugLog("Created card effect material from shader");
                        }
                        else
                        {
                            // Fallback to particle standard
                            _instantiatedEffectMaterial = new Material(Shader.Find("Standard"));
                            DebugLog("WARNING: DarkProtocol/CardEffect shader not found, using Standard shader");
                        }
                    }

                    // Initialize effect properties
                    _instantiatedEffectMaterial.SetTexture(Props.NoiseTex, noiseTexture);
                    _instantiatedEffectMaterial.SetTexture(Props.FlowMap, flowMapTexture);

                    // Apply the material
                    effectRenderer.material = _instantiatedEffectMaterial;
                }
            }

            // Set initial texture assets if they exist
            if (_card != null && _card.CardData != null)
            {
                UpdateCardTextures();
            }
        }

        /// <summary>
        /// Updates card textures based on card data
        /// </summary>
        private void UpdateCardTextures()
        {
            if (_card == null || _card.CardData == null)
                return;

            CardData cardData = _card.CardData;

            // Update card face renderer properties
            if (cardFaceRenderer != null)
            {
                // Get the property block
                cardFaceRenderer.GetPropertyBlock(_facePropertyBlock);

                // Set textures
                if (cardData.CardArtwork != null)
                {
                    Texture2D artwork = cardData.CardArtwork.texture;
                    _facePropertyBlock.SetTexture(Props.MainTex, artwork);
                }

                if (defaultCardMask != null)
                {
                    _facePropertyBlock.SetTexture(Props.MaskTex, defaultCardMask);
                }

                if (defaultCardNormalMap != null)
                {
                    _facePropertyBlock.SetTexture(Props.NormalMap, defaultCardNormalMap);
                }

                // Set card color
                _facePropertyBlock.SetColor(Props.CardColor, cardData.CardColor);

                // Set card stats
                _facePropertyBlock.SetFloat(Props.APCost, cardData.ActionPointCost);
                _facePropertyBlock.SetFloat(Props.MPCost, cardData.MovementPointCost);
                _facePropertyBlock.SetFloat(Props.DamageAmount, cardData.BaseDamage);
                _facePropertyBlock.SetFloat(Props.HealAmount, cardData.BaseHealing);

                // Set rarity properties
                Color rarityColor = GetRarityColor(cardData.Rarity);
                _facePropertyBlock.SetColor(Props.RarityColor, rarityColor);
                _facePropertyBlock.SetFloat(Props.RarityIntensity, GetRarityIntensity(cardData.Rarity));
                _facePropertyBlock.SetFloat(Props.RarityPulse, IsLegendaryOrHigher(cardData.Rarity) ? 1.0f : 0.0f);

                // Apply the property block
                cardFaceRenderer.SetPropertyBlock(_facePropertyBlock);
            }

            // Update card back renderer properties (if it's a separate renderer)
            if (cardBackRenderer != null && cardBackRenderer != cardFaceRenderer)
            {
                // Get the property block
                cardBackRenderer.GetPropertyBlock(_backPropertyBlock);

                // Set team color based on card owner
                Color teamColor = _card.Owner != null ?
                    (_card.Owner.Team == Unit.TeamType.Player ? new Color(0, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f)) :
                    new Color(0.7f, 0.7f, 0.7f);

                _backPropertyBlock.SetColor(Props.TeamColor, teamColor);
                _backPropertyBlock.SetColor(Props.CardColor, cardData.CardColor);

                // Set animation properties
                _backPropertyBlock.SetFloat(Props.AnimateBack, animateCardBack ? 1.0f : 0.0f);
                _backPropertyBlock.SetFloat(Props.PatternSpeed, backPatternSpeed);
                _backPropertyBlock.SetFloat(Props.PatternIntensity, backPatternIntensity);

                // Apply the property block
                cardBackRenderer.SetPropertyBlock(_backPropertyBlock);
            }
            // If same renderer with multiple materials, we can't use property blocks for the second material
            else if (cardBackRenderer != null && cardBackRenderer.materials.Length > 1 && _instantiatedBackMaterial != null)
            {
                // Set team color directly on material
                Color teamColor = _card.Owner != null ?
                    (_card.Owner.Team == Unit.TeamType.Player ? new Color(0, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f)) :
                    new Color(0.7f, 0.7f, 0.7f);

                _instantiatedBackMaterial.SetColor(Props.TeamColor, teamColor);
                _instantiatedBackMaterial.SetColor(Props.CardColor, cardData.CardColor);
                _instantiatedBackMaterial.SetFloat(Props.AnimateBack, animateCardBack ? 1.0f : 0.0f);
                _instantiatedBackMaterial.SetFloat(Props.PatternSpeed, backPatternSpeed);
                _instantiatedBackMaterial.SetFloat(Props.PatternIntensity, backPatternIntensity);
            }
        }

        /// <summary>
        /// Updates material properties based on card state
        /// </summary>
        private void UpdateMaterialProperties()
        {
            // Update card face renderer properties
            if (cardFaceRenderer != null)
            {
                // Get the property block
                cardFaceRenderer.GetPropertyBlock(_facePropertyBlock);

                // Set highlight properties
                _facePropertyBlock.SetColor(Props.HighlightColor, highlightColor);
                _facePropertyBlock.SetFloat(Props.HighlightIntensity, _currentHighlightIntensity);

                // Set rim light properties
                _facePropertyBlock.SetColor(Props.RimColor, rimLightColor);
                _facePropertyBlock.SetFloat(Props.RimPower, rimLightPower);

                // Set card state properties
                _facePropertyBlock.SetFloat(Props.Selected, _isSelected ? 1.0f : 0.0f);
                _facePropertyBlock.SetFloat(Props.Hovering, _isHovering ? 1.0f : 0.0f);
                _facePropertyBlock.SetFloat(Props.Playable, _isPlayable ? 1.0f : 0.0f);

                // Apply the property block
                cardFaceRenderer.SetPropertyBlock(_facePropertyBlock);
            }
        }
        #endregion

        #region Card Effects
        /// <summary>
        /// Play a card play effect
        /// </summary>
        public void PlayCardEffect(int effectType = 0)
        {
            // Start effect timer
            _effectActive = true;
            _effectTimer = effectDuration;

            // Show effect object if available
            if (cardEffectObject != null)
            {
                cardEffectObject.SetActive(true);

                // Configure effect
                Renderer effectRenderer = cardEffectObject.GetComponent<Renderer>();
                if (effectRenderer != null && _instantiatedEffectMaterial != null)
                {
                    // Set effect color based on card data
                    Color effectColor = Color.white;
                    Color emissionColor = highlightColor;

                    if (_card != null && _card.CardData != null)
                    {
                        // Use rarity color for emission
                        emissionColor = GetRarityColor(_card.CardData.Rarity);

                        // Use card color for main color
                        effectColor = _card.CardData.CardColor;
                    }

                    // Create property block
                    effectRenderer.GetPropertyBlock(_effectPropertyBlock);

                    // Set basic properties
                    _effectPropertyBlock.SetColor(Props.Color, effectColor);
                    _effectPropertyBlock.SetColor(Props.EmissionColor, emissionColor);
                    _effectPropertyBlock.SetFloat(Props.EmissionIntensity, 1.5f);

                    // Set effect-specific properties
                    _effectPropertyBlock.SetFloat(Props.EffectType, effectType);

                    // Set dissolve properties
                    _effectPropertyBlock.SetFloat(Props.DissolveAmount, 0.0f);
                    _effectPropertyBlock.SetFloat(Props.DissolveEdgeWidth, 0.05f);
                    _effectPropertyBlock.SetColor(Props.DissolveEdgeColor, emissionColor);

                    // Set flow properties
                    _effectPropertyBlock.SetFloat(Props.FlowSpeed, 1.0f);
                    _effectPropertyBlock.SetFloat(Props.FlowIntensity, 0.5f);

                    // Apply property block
                    effectRenderer.SetPropertyBlock(_effectPropertyBlock);
                }
            }

            // Play particles if available
            if (cardPlayParticles != null)
            {
                // Set particle color based on card data
                ParticleSystem.MainModule main = cardPlayParticles.main;

                if (_card != null && _card.CardData != null)
                {
                    // Use rarity color for particles
                    main.startColor = GetRarityColor(_card.CardData.Rarity);
                }

                // Play particle system
                cardPlayParticles.Play();
            }

            // Enable light if available
            if (cardLight != null)
            {
                // Set light color based on card data
                if (_card != null && _card.CardData != null)
                {
                    // Use rarity color for light
                    cardLight.color = GetRarityColor(_card.CardData.Rarity);
                }

                // Enable light
                cardLight.enabled = true;
            }
        }

        /// <summary>
        /// End card effect
        /// </summary>
        private void EndCardEffect()
        {
            _effectActive = false;

            // Hide effect object
            if (cardEffectObject != null)
            {
                cardEffectObject.SetActive(false);
            }

            // Stop particles
            if (cardPlayParticles != null)
            {
                cardPlayParticles.Stop();
            }

            // Disable light
            if (cardLight != null)
            {
                cardLight.enabled = false;
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle card hover enter
        /// </summary>
        private void HandleCardHoverEnter(Card card)
        {
            _isHovering = true;
            UpdateMaterialProperties();
        }

        /// <summary>
        /// Handle card hover exit
        /// </summary>
        private void HandleCardHoverExit(Card card)
        {
            _isHovering = false;
            UpdateMaterialProperties();
        }

        /// <summary>
        /// Handle card clicked
        /// </summary>
        private void HandleCardClicked(Card card)
        {
            // Toggle selected state
            _isSelected = !_isSelected;

            // Update card visuals
            UpdateMaterialProperties();
        }

        /// <summary>
        /// External method to set card selected state
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected != selected)
            {
                _isSelected = selected;
                UpdateMaterialProperties();
            }
        }

        /// <summary>
        /// External method to set card playable state
        /// </summary>
        public void SetPlayable(bool playable)
        {
            if (_isPlayable != playable)
            {
                _isPlayable = playable;
                UpdateMaterialProperties();
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Get color for card rarity
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
        /// Get intensity value for card rarity
        /// </summary>
        private float GetRarityIntensity(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return 0.1f;
                case CardRarity.Uncommon:
                    return 0.2f;
                case CardRarity.Rare:
                    return 0.3f;
                case CardRarity.Epic:
                    return 0.4f;
                case CardRarity.Legendary:
                    return 0.5f;
                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Check if card is legendary or higher
        /// </summary>
        private bool IsLegendaryOrHigher(CardRarity rarity)
        {
            return rarity >= CardRarity.Legendary;
        }

        /// <summary>
        /// Debug log with prefix
        /// </summary>
        private void DebugLog(string message)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[CardMaterialManager] {message}");
            }
        }
        #endregion
    }
}