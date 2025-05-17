using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Cards
{
    /// <summary>
    /// ScriptableObject that defines a card's properties and effects
    /// </summary>
    [CreateAssetMenu(fileName = "New Card", menuName = "Dark Protocol/Cards/Card Data")]
    public class CardData : ScriptableObject
    {
        #region Card Properties
        [Header("Card Identity")]
        [Tooltip("Unique identifier for this card")]
        [SerializeField] private string cardID;
        [Tooltip("Display name of the card")]
        [SerializeField] private string cardName;
        [Tooltip("Description of the card effect")]
        [TextArea(3, 5)]
        [SerializeField] private string cardDescription;
        
        [Header("Card Classification")]
        [Tooltip("Card category (Attack, Defense, Support, etc.)")]
        [SerializeField] private CardCategory category;
        [Tooltip("Card rarity")]
        [SerializeField] private CardRarity rarity;
        [Tooltip("If true, this is a common card that can be used by any unit")]
        [SerializeField] private bool isCommon = false;
        [Tooltip("If true, this is a special advanced card")]
        [SerializeField] private bool isAdvanced = false;
        
        [Header("Card Costs")]
        [Tooltip("Action Points cost to play this card")]
        [SerializeField] private int actionPointCost = 1;
        [Tooltip("Additional Movement Points cost, if any")]
        [SerializeField] private int movementPointCost = 0;
        [Tooltip("Health cost to play this card, if any")]
        [SerializeField] private int healthCost = 0;
        
        [Header("Card Visuals")]
        [Tooltip("Card artwork")]
        [SerializeField] private Sprite cardArtwork;
        [Tooltip("Card background/frame")]
        [SerializeField] private Sprite cardBackground;
        [Tooltip("Card icon (shown in the corner)")]
        [SerializeField] private Sprite cardIcon;
        [Tooltip("Card color for tinting")]
        [SerializeField] private Color cardColor = Color.white;
        
        [Header("Card Effects")]
        [Tooltip("Type of effect when played")]
        [SerializeField] private CardEffectType effectType;
        [Tooltip("Base damage amount for attack cards")]
        [SerializeField] private int baseDamage = 0;
        [Tooltip("Base healing amount for healing cards")]
        [SerializeField] private int baseHealing = 0;
        [Tooltip("Range of the card effect in tiles")]
        [SerializeField] private int effectRange = 1;
        [Tooltip("Area of effect radius (0 for single target)")]
        [SerializeField] private int areaOfEffect = 0;
        [Tooltip("Card effect duration in turns (0 for instant effect)")]
        [SerializeField] private int effectDuration = 0;
        [Tooltip("Status effects applied by this card")]
        [SerializeField] private List<StatusEffectData> statusEffects = new List<StatusEffectData>();
        [Tooltip("Whether the card requires a target")]
        [SerializeField] private bool requiresTarget = false;
        [Tooltip("Can target self?")]
        [SerializeField] private bool canTargetSelf = false;
        [Tooltip("Can target allies?")]
        [SerializeField] private bool canTargetAllies = false;
        [Tooltip("Can target enemies?")]
        [SerializeField] private bool canTargetEnemies = true;
        [Tooltip("True if this card has environmental effects (destructible cover, etc)")]
        [SerializeField] private bool affectsEnvironment = false;
        
        [Header("Card Gameplay Rules")]
        [Tooltip("If true, this card can only be played once per battle")]
        [SerializeField] private bool singleUsePerBattle = false;
        [Tooltip("If true, this card can only be played once per turn")]
        [SerializeField] private bool singleUsePerTurn = false;
        [Tooltip("Special gameplay conditions required to play this card")]
        [SerializeField] private CardCondition playCondition = CardCondition.None;
        [Tooltip("Cards that this card can combo with for enhanced effects")]
        [SerializeField] private List<CardData> comboCards = new List<CardData>();
        
        [Header("Advanced Settings")]
        [Tooltip("Optional custom logic for this card")]
        [SerializeField] private string customEffectClass;
        #endregion

        #region Public Properties
        // Public accessors for card data
        public string CardID => cardID;
        public string CardName => cardName;
        public string CardDescription => cardDescription;
        public CardCategory Category => category;
        public CardRarity Rarity => rarity;
        public bool IsCommon => isCommon;
        public bool IsAdvanced => isAdvanced;
        public int ActionPointCost => actionPointCost;
        public int MovementPointCost => movementPointCost;
        public int HealthCost => healthCost;
        public Sprite CardArtwork => cardArtwork;
        public Sprite CardBackground => cardBackground;
        public Sprite CardIcon => cardIcon;
        public Color CardColor => cardColor;
        public CardEffectType EffectType => effectType;
        public int BaseDamage => baseDamage;
        public int BaseHealing => baseHealing;
        public int EffectRange => effectRange;
        public int AreaOfEffect => areaOfEffect;
        public int EffectDuration => effectDuration;
        public List<StatusEffectData> StatusEffects => statusEffects;
        public bool RequiresTarget => requiresTarget;
        public bool CanTargetSelf => canTargetSelf;
        public bool CanTargetAllies => canTargetAllies;
        public bool CanTargetEnemies => canTargetEnemies;
        public bool AffectsEnvironment => affectsEnvironment;
        public bool SingleUsePerBattle => singleUsePerBattle;
        public bool SingleUsePerTurn => singleUsePerTurn;
        public CardCondition PlayCondition => playCondition;
        public List<CardData> ComboCards => comboCards;
        public string CustomEffectClass => customEffectClass;
        #endregion

        #region Card Execution
        /// <summary>
        /// Execute the card effect on a target
        /// </summary>
        /// <param name="caster">The unit playing the card</param>
        /// <param name="target">The target unit (if any)</param>
        /// <param name="targetPosition">Optional target position for area/environmental effects</param>
        /// <returns>True if the effect was executed successfully</returns>
        public bool ExecuteEffect(Unit caster, Unit target = null, Vector3? targetPosition = null)
        {
            if (caster == null)
                return false;
                
            // Check if the card requires a target
            if (requiresTarget && target == null)
            {
                Debug.LogWarning($"Card {cardName} requires a target but none was provided");
                return false;
            }
            
            // Check targeting restrictions
            if (target != null)
            {
                bool validTarget = true;
                
                // Self targeting check
                if (target == caster && !canTargetSelf)
                {
                    Debug.LogWarning($"Card {cardName} cannot target self");
                    validTarget = false;
                }
                
                // Ally targeting check
                if (target != caster && target.Team == caster.Team && !canTargetAllies)
                {
                    Debug.LogWarning($"Card {cardName} cannot target allies");
                    validTarget = false;
                }
                
                // Enemy targeting check
                if (target.Team != caster.Team && !canTargetEnemies)
                {
                    Debug.LogWarning($"Card {cardName} cannot target enemies");
                    validTarget = false;
                }
                
                if (!validTarget)
                    return false;
            }
            
            // Check for custom effect implementation
            if (!string.IsNullOrEmpty(customEffectClass))
            {
                // Try to get a custom effect handler
                Type effectType = Type.GetType(customEffectClass);
                if (effectType != null && typeof(ICardEffect).IsAssignableFrom(effectType))
                {
                    ICardEffect customEffect = Activator.CreateInstance(effectType) as ICardEffect;
                    if (customEffect != null)
                    {
                        return customEffect.ExecuteEffect(this, caster, target, targetPosition);
                    }
                }
                
                Debug.LogWarning($"Failed to create custom effect handler for {cardName}: {customEffectClass}");
            }
            
            // Execute standard effect based on effect type
            switch (effectType)
            {
                case CardEffectType.Damage:
                    return ExecuteDamageEffect(caster, target);
                case CardEffectType.Healing:
                    return ExecuteHealingEffect(caster, target);
                case CardEffectType.Buff:
                    return ExecuteBuffEffect(caster, target);
                case CardEffectType.Debuff:
                    return ExecuteDebuffEffect(caster, target);
                case CardEffectType.Movement:
                    return ExecuteMovementEffect(caster, targetPosition);
                case CardEffectType.AreaEffect:
                    return ExecuteAreaEffect(caster, targetPosition);
                case CardEffectType.Environmental:
                    return ExecuteEnvironmentalEffect(caster, targetPosition);
                case CardEffectType.Special:
                    // Special cards should have custom implementations
                    Debug.LogWarning($"Card {cardName} has Special effect type but no custom implementation");
                    return false;
                default:
                    Debug.LogWarning($"Unknown effect type for card {cardName}");
                    return false;
            }
        }
        
        /// <summary>
        /// Execute damage effect
        /// </summary>
        private bool ExecuteDamageEffect(Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive)
                return false;
                
            // Calculate final damage
            int finalDamage = baseDamage;
            
            // Apply any modifier effects based on caster stats
            // This would integrate with deeper stat and combat systems
            
            // Apply damage to target
            target.TakeDamage(finalDamage, caster);
            
            // Apply any status effects
            ApplyStatusEffects(caster, target);
            
            // Notify the target that a card was played on them
            target.OnCardPlayed(this, caster);
            
            return true;
        }
        
        /// <summary>
        /// Execute healing effect
        /// </summary>
        private bool ExecuteHealingEffect(Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive)
                return false;
                
            // Calculate final healing
            int finalHealing = baseHealing;
            
            // Apply any modifier effects
            
            // Apply healing to target
            target.Heal(finalHealing, caster);
            
            // Apply any status effects
            ApplyStatusEffects(caster, target);
            
            // Notify the target that a card was played on them
            target.OnCardPlayed(this, caster);
            
            return true;
        }
        
        /// <summary>
        /// Execute buff effect
        /// </summary>
        private bool ExecuteBuffEffect(Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive)
                return false;
                
            // Apply status effects
            ApplyStatusEffects(caster, target);
            
            // Notify the target that a card was played on them
            target.OnCardPlayed(this, caster);
            
            return true;
        }
        
        /// <summary>
        /// Execute debuff effect
        /// </summary>
        private bool ExecuteDebuffEffect(Unit caster, Unit target)
        {
            if (target == null || !target.IsAlive)
                return false;
                
            // Apply status effects
            ApplyStatusEffects(caster, target);
            
            // Notify the target that a card was played on them
            target.OnCardPlayed(this, caster);
            
            return true;
        }
        
        /// <summary>
        /// Execute movement effect
        /// </summary>
        private bool ExecuteMovementEffect(Unit caster, Vector3? targetPosition)
        {
            if (targetPosition == null)
                return false;
                
            // Move the caster to the target position
            return caster.Move(targetPosition.Value, movementPointCost);
        }
        
        /// <summary>
        /// Execute area effect
        /// </summary>
        private bool ExecuteAreaEffect(Unit caster, Vector3? targetPosition)
        {
            if (targetPosition == null)
                return false;
                
            // Implement area effect depending on game mechanics
            // For now, we'll use a simple approach of finding units in range
            
            // Get all units in the area
            List<Unit> unitsInArea = GetUnitsInArea(targetPosition.Value);
            bool anyEffectsApplied = false;
            
            // Apply effects to each valid target
            foreach (Unit unit in unitsInArea)
            {
                // Check if this is a valid target
                bool isValidTarget = false;
                
                if (unit == caster && canTargetSelf)
                    isValidTarget = true;
                else if (unit.Team == caster.Team && canTargetAllies)
                    isValidTarget = true;
                else if (unit.Team != caster.Team && canTargetEnemies)
                    isValidTarget = true;
                    
                if (isValidTarget)
                {
                    // Apply appropriate effect based on card type
                    switch (effectType)
                    {
                        case CardEffectType.Damage:
                            unit.TakeDamage(baseDamage, caster);
                            anyEffectsApplied = true;
                            break;
                        case CardEffectType.Healing:
                            unit.Heal(baseHealing, caster);
                            anyEffectsApplied = true;
                            break;
                    }
                    
                    // Apply status effects
                    ApplyStatusEffects(caster, unit);
                    
                    // Notify the target that a card was played on them
                    unit.OnCardPlayed(this, caster);
                }
            }
            
            return anyEffectsApplied;
        }
        
        /// <summary>
        /// Execute environmental effect
        /// </summary>
        private bool ExecuteEnvironmentalEffect(Unit caster, Vector3? targetPosition)
        {
            if (targetPosition == null || !affectsEnvironment)
                return false;
                
            // Implement environmental effects
            // For example, destroying cover or creating obstacles
            
            // This would integrate with the grid system
            if (DarkProtocol.Grid.GridManager.Instance != null)
            {
                // Get grid position
                if (DarkProtocol.Grid.GridManager.Instance.WorldToGridPosition(targetPosition.Value, out int x, out int z))
                {
                    // Modify the grid based on the effect
                    // This is a placeholder - actual implementation would depend on the specific card
                    
                    // For example, destroying cover:
                    // GridManager.Instance.SetTileOccupied(x, z, false);
                    
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Apply status effects to a target
        /// </summary>
        private void ApplyStatusEffects(Unit caster, Unit target)
        {
            if (statusEffects.Count == 0 || target == null)
                return;
                
            // Get the status effect manager
            StatusEffectManager statusManager = target.GetComponent<StatusEffectManager>();
            if (statusManager == null)
            {
                Debug.LogWarning($"Target {target.UnitName} has no StatusEffectManager component");
                return;
            }
            
            // Apply each status effect
            foreach (StatusEffectData effectData in statusEffects)
            {
                statusManager.ApplyStatusEffect(effectData, caster, effectDuration);
            }
        }
        
        /// <summary>
        /// Get all units in an area around a position
        /// </summary>
        private List<Unit> GetUnitsInArea(Vector3 center)
        {
            List<Unit> unitsInArea = new List<Unit>();
            
            // Find all units in the scene
            Unit[] allUnits = GameObject.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            
            // Check each unit's distance
            foreach (Unit unit in allUnits)
            {
                float distance = Vector3.Distance(center, unit.transform.position);
                
                // Check if within area of effect radius
                if (distance <= areaOfEffect * (DarkProtocol.Grid.GridManager.Instance?.gridData.CellSize ?? 1f))
                {
                    unitsInArea.Add(unit);
                }
            }
            
            return unitsInArea;
        }
        #endregion

        #region Editor Validation
        private void OnValidate()
        {
            // Ensure the card has a unique ID
            if (string.IsNullOrEmpty(cardID))
            {
                cardID = System.Guid.NewGuid().ToString();
            }
        }
        #endregion
    }

    /// <summary>
    /// Card categories
    /// </summary>
    public enum CardCategory
    {
        Attack,
        Defense,
        Support,
        Movement,
        Utility,
        Special
    }
    
    /// <summary>
    /// Card rarity levels
    /// </summary>
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    /// <summary>
    /// Types of card effects
    /// </summary>
    public enum CardEffectType
    {
        Damage,
        Healing,
        Buff,
        Debuff,
        Movement,
        AreaEffect,
        Environmental,
        Special
    }
    
    /// <summary>
    /// Special conditions for playing a card
    /// </summary>
    public enum CardCondition
    {
        None,
        LowHealth,       // Unit has low health
        HighGround,      // Unit is on elevated terrain
        InCover,         // Unit is in cover
        ExposedTarget,   // Target is not in cover
        ComboAvailable,  // A combo card was played previously
        FirstAction,     // Must be the first action of the turn
        LastAction       // Must be the last action of the turn
    }
    
    /// <summary>
    /// Interface for custom card effects
    /// </summary>
    public interface ICardEffect
    {
        bool ExecuteEffect(CardData card, Unit caster, Unit target, Vector3? targetPosition);
    }
}