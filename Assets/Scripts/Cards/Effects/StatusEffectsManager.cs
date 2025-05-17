using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Cards
{
    /// <summary>
    /// Status effect data for cards
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Dark Protocol/Cards/Status Effect")]
    public class StatusEffectData : ScriptableObject
    {
        #region Effect Properties
        [Header("Effect Identity")]
        [Tooltip("Unique identifier for this effect")]
        [SerializeField] private string effectID;
        [Tooltip("Display name of the effect")]
        [SerializeField] private string effectName;
        [Tooltip("Description of the effect")]
        [TextArea(3, 5)]
        [SerializeField] private string effectDescription;
        
        [Header("Effect Visuals")]
        [Tooltip("Icon representing this effect")]
        [SerializeField] private Sprite effectIcon;
        [Tooltip("Color associated with this effect")]
        [SerializeField] private Color effectColor = Color.white;
        [Tooltip("Particle effect prefab (optional)")]
        [SerializeField] private GameObject effectParticlePrefab;
        
        [Header("Effect Properties")]
        [Tooltip("Type of status effect")]
        [SerializeField] private StatusEffectType effectType;
        [Tooltip("Whether the effect is beneficial or harmful")]
        [SerializeField] private bool isBeneficial = false;
        [Tooltip("Base effect value (damage, healing, etc.)")]
        [SerializeField] private int effectValue = 0;
        [Tooltip("Effect value per turn (for damage over time, etc.)")]
        [SerializeField] private int effectValuePerTurn = 0;
        [Tooltip("If true, the effect stacks with multiple applications")]
        [SerializeField] private bool isStackable = false;
        [Tooltip("Maximum stack count")]
        [SerializeField] private int maxStackCount = 1;
        [Tooltip("If true, the effect will be removed when the unit takes damage")]
        [SerializeField] private bool removedOnDamage = false;
        
        [Header("Stat Modifiers")]
        [Tooltip("Modifies the unit's movement points")]
        [SerializeField] private int movementPointModifier = 0;
        [Tooltip("Modifies the unit's action points")]
        [SerializeField] private int actionPointModifier = 0;
        [Tooltip("Damage modifier percentage (0 = no change, -50 = half damage, 50 = 50% more damage)")]
        [Range(-100, 100)]
        [SerializeField] private int damageModifierPercentage = 0;
        [Tooltip("Healing modifier percentage")]
        [Range(-100, 100)]
        [SerializeField] private int healingModifierPercentage = 0;
        
        [Header("Advanced Settings")]
        [Tooltip("Optional custom logic for this effect")]
        [SerializeField] private string customEffectClass;
        #endregion

        #region Public Properties
        public string EffectID => effectID;
        public string EffectName => effectName;
        public string EffectDescription => effectDescription;
        public Sprite EffectIcon => effectIcon;
        public Color EffectColor => effectColor;
        public GameObject EffectParticlePrefab => effectParticlePrefab;
        public StatusEffectType EffectType => effectType;
        public bool IsBeneficial => isBeneficial;
        public int EffectValue => effectValue;
        public int EffectValuePerTurn => effectValuePerTurn;
        public bool IsStackable => isStackable;
        public int MaxStackCount => maxStackCount;
        public bool RemovedOnDamage => removedOnDamage;
        public int MovementPointModifier => movementPointModifier;
        public int ActionPointModifier => actionPointModifier;
        public int DamageModifierPercentage => damageModifierPercentage;
        public int HealingModifierPercentage => healingModifierPercentage;
        public string CustomEffectClass => customEffectClass;
        #endregion

        #region Editor Validation
        private void OnValidate()
        {
            // Ensure the effect has a unique ID
            if (string.IsNullOrEmpty(effectID))
            {
                effectID = System.Guid.NewGuid().ToString();
            }
        }
        #endregion
    }

    /// <summary>
    /// Types of status effects
    /// </summary>
    public enum StatusEffectType
    {
        DamageOverTime,
        HealOverTime,
        StatBuff,
        StatDebuff,
        Stun,
        Root,
        Stealth,
        Shield,
        Taunt,
        Confusion,
        Custom
    }

    /// <summary>
    /// Component that manages status effects on a unit
    /// </summary>
    public class StatusEffectManager : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Status Effect Settings")]
        [SerializeField] private Transform statusEffectContainer;
        [SerializeField] private GameObject statusEffectIconPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        #endregion

        #region Private Fields
        // List of active effects
        private List<ActiveStatusEffect> _activeEffects = new List<ActiveStatusEffect>();
        
        // Reference to the unit
        private Unit _unit;
        
        // Dictionary of visual indicators
        private Dictionary<string, GameObject> _effectIndicators = new Dictionary<string, GameObject>();
        
        // Dictionary of particle effects
        private Dictionary<string, GameObject> _effectParticles = new Dictionary<string, GameObject>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get the unit
            _unit = GetComponent<Unit>();
            
            if (_unit == null)
            {
                Debug.LogError("StatusEffectManager requires a Unit component!");
                enabled = false;
                return;
            }
            
            // Subscribe to unit events
            _unit.OnTurnStarted += HandleTurnStarted;
            _unit.OnTurnEnded += HandleTurnEnded;
            _unit.OnHealthChanged += HandleHealthChanged;
            _unit.OnUnitDeath += HandleUnitDeath;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from unit events
            if (_unit != null)
            {
                _unit.OnTurnStarted -= HandleTurnStarted;
                _unit.OnTurnEnded -= HandleTurnEnded;
                _unit.OnHealthChanged -= HandleHealthChanged;
                _unit.OnUnitDeath -= HandleUnitDeath;
            }
            
            // Clean up indicators
            foreach (var indicator in _effectIndicators.Values)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
            
            // Clean up particles
            foreach (var particle in _effectParticles.Values)
            {
                if (particle != null)
                {
                    Destroy(particle);
                }
            }
        }
        #endregion

        #region Status Effect Management
        /// <summary>
        /// Apply a status effect to the unit
        /// </summary>
        public void ApplyStatusEffect(StatusEffectData effectData, Unit source, int duration = 1)
        {
            if (effectData == null || _unit == null)
                return;
                
            // Check if the unit is immune to this effect
            // (Could be expanded with an immunity system)
            
            // Check if the effect already exists
            ActiveStatusEffect existingEffect = GetActiveEffect(effectData.EffectID);
            
            if (existingEffect != null)
            {
                // Effect already exists, handle based on stackability
                if (effectData.IsStackable && existingEffect.StackCount < effectData.MaxStackCount)
                {
                    // Increment stack count
                    existingEffect.StackCount++;
                    
                    // Refresh duration
                    existingEffect.RemainingDuration = Mathf.Max(existingEffect.RemainingDuration, duration);
                    
                    // Update visual
                    UpdateEffectVisual(existingEffect);
                    
                    DebugLog($"Stacked effect {effectData.EffectName} on {_unit.UnitName}. Stack count: {existingEffect.StackCount}");
                }
                else
                {
                    // Just refresh the duration for non-stacking effects
                    existingEffect.RemainingDuration = Mathf.Max(existingEffect.RemainingDuration, duration);
                    
                    DebugLog($"Refreshed effect {effectData.EffectName} on {_unit.UnitName}. Duration: {existingEffect.RemainingDuration}");
                }
            }
            else
            {
                // Create a new active effect
                ActiveStatusEffect newEffect = new ActiveStatusEffect
                {
                    EffectData = effectData,
                    Source = source,
                    RemainingDuration = duration,
                    StackCount = 1
                };
                
                // Add to active effects
                _activeEffects.Add(newEffect);
                
                // Create visual indicator
                CreateEffectVisual(newEffect);
                
                // Apply immediate effect
                ApplyEffectImmediately(newEffect);
                
                DebugLog($"Applied new effect {effectData.EffectName} to {_unit.UnitName} for {duration} turns");
            }
            
            // Check for custom effect handlers
            if (!string.IsNullOrEmpty(effectData.CustomEffectClass))
            {
                TryExecuteCustomEffect(effectData, source);
            }
        }
        
        /// <summary>
        /// Remove a status effect from the unit
        /// </summary>
        public void RemoveStatusEffect(string effectID)
        {
            ActiveStatusEffect effect = GetActiveEffect(effectID);
            
            if (effect != null)
            {
                // Remove effect
                RemoveEffect(effect);
                
                DebugLog($"Removed effect {effect.EffectData.EffectName} from {_unit.UnitName}");
            }
        }
        
        /// <summary>
        /// Remove a status effect from the unit
        /// </summary>
        public void RemoveStatusEffect(StatusEffectData effectData)
        {
            if (effectData == null)
                return;
                
            RemoveStatusEffect(effectData.EffectID);
        }
        
        /// <summary>
        /// Remove an effect and clean up its visuals
        /// </summary>
        private void RemoveEffect(ActiveStatusEffect effect)
        {
            // Remove any stat modifiers or ongoing effects
            RemoveEffectModifiers(effect);
            
            // Remove visual indicators
            RemoveEffectVisual(effect);
            
            // Remove from active effects list
            _activeEffects.Remove(effect);
        }
        
        /// <summary>
        /// Get an active effect by ID
        /// </summary>
        private ActiveStatusEffect GetActiveEffect(string effectID)
        {
            return _activeEffects.Find(e => e.EffectData.EffectID == effectID);
        }
        
        /// <summary>
        /// Check if the unit has a specific status effect
        /// </summary>
        public bool HasStatusEffect(string effectID)
        {
            return GetActiveEffect(effectID) != null;
        }
        
        /// <summary>
        /// Get the stack count for a status effect
        /// </summary>
        public int GetStatusEffectStackCount(string effectID)
        {
            ActiveStatusEffect effect = GetActiveEffect(effectID);
            return effect != null ? effect.StackCount : 0;
        }
        
        /// <summary>
        /// Apply the immediate effect of a status effect
        /// </summary>
        private void ApplyEffectImmediately(ActiveStatusEffect effect)
        {
            if (effect == null || effect.EffectData == null)
                return;
                
            // Apply stat modifiers
            ApplyStatModifiers(effect);
            
            // Apply immediate effect based on type
            switch (effect.EffectData.EffectType)
            {
                case StatusEffectType.DamageOverTime:
                    // Immediate damage on application
                    if (effect.EffectData.EffectValue > 0)
                    {
                        _unit.TakeDamage(effect.EffectData.EffectValue, effect.Source);
                    }
                    break;
                case StatusEffectType.HealOverTime:
                    // Immediate healing on application
                    if (effect.EffectData.EffectValue > 0)
                    {
                        _unit.Heal(effect.EffectData.EffectValue, effect.Source);
                    }
                    break;
                case StatusEffectType.Stun:
                    // Immediate stun effect
                    // This would need to integrate with the action system
                    break;
                case StatusEffectType.Shield:
                    // Apply shield effect
                    // This would need integration with the damage system
                    break;
            }
        }
        
        /// <summary>
        /// Apply the per-turn effect of a status effect
        /// </summary>
        private void ApplyEffectPerTurn(ActiveStatusEffect effect)
        {
            if (effect == null || effect.EffectData == null)
                return;
                
            // Apply per-turn effect based on type
            switch (effect.EffectData.EffectType)
            {
                case StatusEffectType.DamageOverTime:
                    // Damage per turn
                    if (effect.EffectData.EffectValuePerTurn > 0)
                    {
                        int totalDamage = effect.EffectData.EffectValuePerTurn * effect.StackCount;
                        _unit.TakeDamage(totalDamage, effect.Source);
                        
                        DebugLog($"{_unit.UnitName} took {totalDamage} damage from {effect.EffectData.EffectName}");
                    }
                    break;
                case StatusEffectType.HealOverTime:
                    // Healing per turn
                    if (effect.EffectData.EffectValuePerTurn > 0)
                    {
                        int totalHealing = effect.EffectData.EffectValuePerTurn * effect.StackCount;
                        _unit.Heal(totalHealing, effect.Source);
                        
                        DebugLog($"{_unit.UnitName} healed {totalHealing} from {effect.EffectData.EffectName}");
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Apply stat modifiers from a status effect
        /// </summary>
        private void ApplyStatModifiers(ActiveStatusEffect effect)
        {
            if (effect == null || effect.EffectData == null)
                return;
                
            // For more complex games, you might want to use a proper stat modification system
            // For now, we'll apply simple stat changes
            
            // Movement point modifier
            if (effect.EffectData.MovementPointModifier != 0)
            {
                // This would need a method to modify max movement points on the Unit class
                // _unit.ModifyMaxMovementPoints(effect.EffectData.MovementPointModifier * effect.StackCount);
            }
            
            // Action point modifier
            if (effect.EffectData.ActionPointModifier != 0)
            {
                // This would need a method to modify max action points on the Unit class
                // _unit.ModifyMaxActionPoints(effect.EffectData.ActionPointModifier * effect.StackCount);
            }
        }
        
        /// <summary>
        /// Remove stat modifiers from a status effect
        /// </summary>
        private void RemoveEffectModifiers(ActiveStatusEffect effect)
        {
            if (effect == null || effect.EffectData == null)
                return;
                
            // For more complex games, you might want to use a proper stat modification system
            // For now, we'll remove simple stat changes
            
            // Movement point modifier
            if (effect.EffectData.MovementPointModifier != 0)
            {
                // This would need a method to modify max movement points on the Unit class
                // _unit.ModifyMaxMovementPoints(-effect.EffectData.MovementPointModifier * effect.StackCount);
            }
            
            // Action point modifier
            if (effect.EffectData.ActionPointModifier != 0)
            {
                // This would need a method to modify max action points on the Unit class
                // _unit.ModifyMaxActionPoints(-effect.EffectData.ActionPointModifier * effect.StackCount);
            }
        }
        
        /// <summary>
        /// Try to execute custom effect logic
        /// </summary>
        private void TryExecuteCustomEffect(StatusEffectData effectData, Unit source)
        {
            if (string.IsNullOrEmpty(effectData.CustomEffectClass))
                return;
                
            // Try to get a custom effect handler
            Type effectType = Type.GetType(effectData.CustomEffectClass);
            if (effectType != null && typeof(IStatusEffect).IsAssignableFrom(effectType))
            {
                IStatusEffect customEffect = Activator.CreateInstance(effectType) as IStatusEffect;
                if (customEffect != null)
                {
                    customEffect.ApplyEffect(_unit, effectData, source);
                }
            }
        }
        #endregion

        #region Visual Management
        /// <summary>
        /// Create a visual indicator for a status effect
        /// </summary>
        private void CreateEffectVisual(ActiveStatusEffect effect)
        {
            if (effect == null || effect.EffectData == null || statusEffectContainer == null || statusEffectIconPrefab == null)
                return;
                
            // Create icon
            GameObject iconObject = Instantiate(statusEffectIconPrefab, statusEffectContainer);
            
            // Set up the icon
            StatusEffectIcon iconComponent = iconObject.GetComponent<StatusEffectIcon>();
            if (iconComponent != null)
            {
                iconComponent.Initialize(effect.EffectData, effect.StackCount, effect.RemainingDuration);
            }
            
            // Store the indicator
            _effectIndicators[effect.EffectData.EffectID] = iconObject;
            
            // Create particle effect if specified
            if (effect.EffectData.EffectParticlePrefab != null)
            {
                GameObject particleObj = Instantiate(effect.EffectData.EffectParticlePrefab, transform);
                _effectParticles[effect.EffectData.EffectID] = particleObj;
            }
        }
        
        /// <summary>
        /// Update a visual indicator for a status effect
        /// </summary>
        private void UpdateEffectVisual(ActiveStatusEffect effect)
        {
            if (effect == null || effect.EffectData == null)
                return;
                
            // Update icon if it exists
            if (_effectIndicators.TryGetValue(effect.EffectData.EffectID, out GameObject iconObject) && iconObject != null)
            {
                StatusEffectIcon iconComponent = iconObject.GetComponent<StatusEffectIcon>();
                if (iconComponent != null)
                {
                    iconComponent.UpdateStatus(effect.StackCount, effect.RemainingDuration);
                }
            }
            else
            {
                // Create if not exists
                CreateEffectVisual(effect);
            }
        }
        
        /// <summary>
        /// Remove a visual indicator for a status effect
        /// </summary>
        private void RemoveEffectVisual(ActiveStatusEffect effect)
        {
            if (effect == null || effect.EffectData == null)
                return;
                
            // Remove icon if it exists
            if (_effectIndicators.TryGetValue(effect.EffectData.EffectID, out GameObject iconObject) && iconObject != null)
            {
                Destroy(iconObject);
                _effectIndicators.Remove(effect.EffectData.EffectID);
            }
            
            // Remove particle effect if it exists
            if (_effectParticles.TryGetValue(effect.EffectData.EffectID, out GameObject particleObj) && particleObj != null)
            {
                Destroy(particleObj);
                _effectParticles.Remove(effect.EffectData.EffectID);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle unit turn start
        /// </summary>
        private void HandleTurnStarted()
        {
            DebugLog($"{_unit.UnitName} turn started with {_activeEffects.Count} active effects");
            
            // Process start-of-turn effects
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveStatusEffect effect = _activeEffects[i];
                
                // Apply per-turn effects
                ApplyEffectPerTurn(effect);
                
                // Update duration
                effect.RemainingDuration--;
                
                // Check if the effect has expired
                if (effect.RemainingDuration <= 0)
                {
                    DebugLog($"Effect {effect.EffectData.EffectName} expired");
                    
                    // Remove the effect
                    RemoveEffect(effect);
                }
                else
                {
                    // Update visual
                    UpdateEffectVisual(effect);
                }
            }
        }
        
        /// <summary>
        /// Handle unit turn end
        /// </summary>
        private void HandleTurnEnded()
        {
            // Some effects might need end-of-turn processing
            DebugLog($"{_unit.UnitName} turn ended with {_activeEffects.Count} active effects");
        }
        
        /// <summary>
        /// Handle unit health changes
        /// </summary>
        private void HandleHealthChanged(int newHealth, int oldHealth)
        {
            // Check if the unit took damage
            if (newHealth < oldHealth)
            {
                // Check for effects that are removed on damage
                for (int i = _activeEffects.Count - 1; i >= 0; i--)
                {
                    ActiveStatusEffect effect = _activeEffects[i];
                    
                    if (effect.EffectData.RemovedOnDamage)
                    {
                        DebugLog($"Effect {effect.EffectData.EffectName} removed due to damage");
                        
                        // Remove the effect
                        RemoveEffect(effect);
                    }
                }
            }
        }
        
        /// <summary>
        /// Handle unit death
        /// </summary>
        private void HandleUnitDeath()
        {
            // Clear all effects on death
            ClearAllEffects();
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Clear all active status effects
        /// </summary>
        public void ClearAllEffects()
        {
            // Remove all effects
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                RemoveEffect(_activeEffects[i]);
            }
            
            // Clear lists
            _activeEffects.Clear();
            _effectIndicators.Clear();
            _effectParticles.Clear();
            
            DebugLog($"Cleared all effects from {_unit.UnitName}");
        }
        
        /// <summary>
        /// Get all active status effects
        /// </summary>
        public List<ActiveStatusEffect> GetAllActiveEffects()
        {
            return new List<ActiveStatusEffect>(_activeEffects);
        }
        
        /// <summary>
        /// Debug logging with prefix
        /// </summary>
        private void DebugLog(string message)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[StatusEffectManager] {message}");
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents an active status effect on a unit
    /// </summary>
    public class ActiveStatusEffect
    {
        public StatusEffectData EffectData;
        public Unit Source;
        public int RemainingDuration;
        public int StackCount;
    }

    /// <summary>
    /// Interface for custom status effects
    /// </summary>
    public interface IStatusEffect
    {
        void ApplyEffect(Unit target, StatusEffectData effectData, Unit source);
        void RemoveEffect(Unit target, StatusEffectData effectData);
    }

    /// <summary>
    /// Component for displaying a status effect icon
    /// </summary>
    public class StatusEffectIcon : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image iconImage;
        [SerializeField] private TMPro.TextMeshProUGUI stackCountText;
        [SerializeField] private TMPro.TextMeshProUGUI durationText;
        
        private StatusEffectData _effectData;
        
        /// <summary>
        /// Initialize the icon with effect data
        /// </summary>
        public void Initialize(StatusEffectData effectData, int stackCount, int duration)
        {
            _effectData = effectData;
            
            // Set icon
            if (iconImage != null && effectData.EffectIcon != null)
            {
                iconImage.sprite = effectData.EffectIcon;
                iconImage.color = effectData.EffectColor;
            }
            
            // Update status
            UpdateStatus(stackCount, duration);
        }
        
        /// <summary>
        /// Update stack count and duration
        /// </summary>
        public void UpdateStatus(int stackCount, int duration)
        {
            // Show stack count if applicable
            if (stackCountText != null)
            {
                if (_effectData.IsStackable && stackCount > 1)
                {
                    stackCountText.text = stackCount.ToString();
                    stackCountText.gameObject.SetActive(true);
                }
                else
                {
                    stackCountText.gameObject.SetActive(false);
                }
            }
            
            // Show duration if applicable
            if (durationText != null)
            {
                if (duration > 0)
                {
                    durationText.text = duration.ToString();
                    durationText.gameObject.SetActive(true);
                }
                else
                {
                    durationText.gameObject.SetActive(false);
                }
            }
        }
    }
}