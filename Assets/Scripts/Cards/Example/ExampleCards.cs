using UnityEngine;
using DarkProtocol.Cards;

/// <summary>
/// This class contains example card implementations to showcase the card system functionality
/// </summary>
public class ExampleCards : MonoBehaviour
{
    #region Inspector Fields
    [Header("Basic Cards")]
    [SerializeField] private CardData basicAttackCard;
    [SerializeField] private CardData basicHealCard;
    [SerializeField] private CardData basicMoveCard;
    [SerializeField] private CardData overchargeCard;
    
    [Header("Special Cards")]
    [SerializeField] private CardData grenadeCard;
    [SerializeField] private CardData shieldCard;
    [SerializeField] private CardData stealthCard;
    [SerializeField] private CardData coverFireCard;
    
    [Header("Unit Decks")]
    [SerializeField] private UnitCardDeck assaultDeck;
    [SerializeField] private UnitCardDeck supportDeck;
    [SerializeField] private UnitCardDeck reconDeck;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // Initialize the card system if it exists
        if (DarkProtocol.Cards.CardSystem.Instance != null)
        {
            // Initialize unit decks
            InitializeUnitDecks();
        }
        else
        {
            Debug.LogError("CardSystem not found in scene! Cannot initialize example cards.");
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initialize unit decks for testing
    /// </summary>
    private void InitializeUnitDecks()
    {
        // Find player units
        Unit[] playerUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        if (playerUnits.Length > 0)
        {
            // For testing, assign decks to player units based on index
            for (int i = 0; i < playerUnits.Length; i++)
            {
                // Only initialize for player team units
                if (playerUnits[i].Team != Unit.TeamType.Player)
                    continue;
                    
                // Assign a deck based on index
                UnitCardDeck deckToAssign = null;
                
                switch (i % 3)
                {
                    case 0:
                        deckToAssign = assaultDeck;
                        break;
                    case 1:
                        deckToAssign = supportDeck;
                        break;
                    case 2:
                        deckToAssign = reconDeck;
                        break;
                }
                
                // Initialize if deck is valid
                if (deckToAssign != null)
                {
                    DarkProtocol.Cards.CardSystem.Instance.InitializeUnitDeck(playerUnits[i], deckToAssign);
                    Debug.Log($"Initialized {deckToAssign.DeckName} for {playerUnits[i].UnitName}");
                }
            }
        }
    }
    #endregion

    #region Custom Card Implementations
    // The following are example implementations of custom card effects using the ICardEffect interface
    
    /// <summary>
    /// Grenade card effect implementation
    /// </summary>
    public class GrenadeCardEffect : ICardEffect
    {
        public bool ExecuteEffect(CardData card, Unit caster, Unit target, Vector3? targetPosition)
        {
            if (targetPosition == null)
                return false;
                
            Debug.Log($"Executing Grenade card effect at {targetPosition}");
            
            // Get all units in the area
            Collider[] colliders = Physics.OverlapSphere(targetPosition.Value, card.AreaOfEffect);
            bool hitAny = false;
            
            // Apply damage to each unit in the area
            foreach (Collider collider in colliders)
            {
                Unit unit = collider.GetComponent<Unit>();
                
                if (unit != null && unit.IsAlive)
                {
                    // Determine damage falloff based on distance
                    float distance = Vector3.Distance(targetPosition.Value, unit.transform.position);
                    float falloff = 1f - Mathf.Clamp01(distance / card.AreaOfEffect);
                    int damageAmount = Mathf.RoundToInt(card.BaseDamage * falloff);
                    
                    if (damageAmount > 0)
                    {
                        unit.TakeDamage(damageAmount, caster);
                        hitAny = true;
                        
                        Debug.Log($"Grenade dealt {damageAmount} damage to {unit.UnitName}");
                        
                        // Create impact effect
                        CreateImpactEffect(unit.transform.position);
                    }
                }
            }
            
            // Create explosion effect at target position
            CreateExplosionEffect(targetPosition.Value);
            
            return hitAny;
        }
        
        /// <summary>
        /// Create impact effect on a unit
        /// </summary>
        private void CreateImpactEffect(Vector3 position)
        {
            // This would instantiate a particle effect for damage
            // For example:
            // GameObject effect = Instantiate(Resources.Load<GameObject>("Effects/DamageImpact"), position, Quaternion.identity);
            // Destroy(effect, 2f);
        }
        
        /// <summary>
        /// Create explosion effect at target position
        /// </summary>
        private void CreateExplosionEffect(Vector3 position)
        {
            // This would instantiate a particle effect for the explosion
            // For example:
            // GameObject effect = Instantiate(Resources.Load<GameObject>("Effects/Explosion"), position, Quaternion.identity);
            // Destroy(effect, 3f);
        }
    }
    
    /// <summary>
    /// Shield card effect implementation
    /// </summary>
    public class ShieldCardEffect : ICardEffect
    {
        public bool ExecuteEffect(CardData card, Unit caster, Unit target, Vector3? targetPosition)
        {
            if (target == null)
                return false;
                
            Debug.Log($"Executing Shield card effect on {target.UnitName}");
            
            // Apply shield status effect
            StatusEffectManager statusManager = target.GetComponent<StatusEffectManager>();
            if (statusManager != null)
            {
                // Create shield status effect data
                StatusEffectData shieldEffect = ScriptableObject.CreateInstance<StatusEffectData>();
                
                // In a real implementation, this would be a reference to an actual StatusEffectData asset
                // For this example, we're creating one on the fly
                
                // Apply shield effect for the duration specified in the card
                statusManager.ApplyStatusEffect(shieldEffect, caster, card.EffectDuration);
                
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Stealth card effect implementation
    /// </summary>
    public class StealthCardEffect : ICardEffect
    {
        public bool ExecuteEffect(CardData card, Unit caster, Unit target, Vector3? targetPosition)
        {
            if (target == null)
                return false;
                
            Debug.Log($"Executing Stealth card effect on {target.UnitName}");
            
            // Apply stealth status effect
            StatusEffectManager statusManager = target.GetComponent<StatusEffectManager>();
            if (statusManager != null)
            {
                // Create stealth status effect data
                StatusEffectData stealthEffect = ScriptableObject.CreateInstance<StatusEffectData>();
                
                // Apply stealth effect for the duration specified in the card
                statusManager.ApplyStatusEffect(stealthEffect, caster, card.EffectDuration);
                
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Cover Fire card effect implementation
    /// </summary>
    public class CoverFireCardEffect : ICardEffect
    {
        public bool ExecuteEffect(CardData card, Unit caster, Unit target, Vector3? targetPosition)
        {
            if (target == null)
                return false;
                
            Debug.Log($"Executing Cover Fire card effect on {target.UnitName}");
            
            // Deal damage to the target
            target.TakeDamage(card.BaseDamage, caster);
            
            // Apply suppression effect (reduces AP)
            StatusEffectManager statusManager = target.GetComponent<StatusEffectManager>();
            if (statusManager != null)
            {
                // Create suppression effect data
                StatusEffectData suppressionEffect = ScriptableObject.CreateInstance<StatusEffectData>();
                
                // Apply suppression effect for 1 turn
                statusManager.ApplyStatusEffect(suppressionEffect, caster, 1);
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Overcharge card effect implementation
    /// </summary>
    public class OverchargeCardEffect : ICardEffect
    {
        public bool ExecuteEffect(CardData card, Unit caster, Unit target, Vector3? targetPosition)
        {
            if (caster == null)
                return false;
                
            Debug.Log($"Executing Overcharge card effect on {caster.UnitName}");
            
            // Increase AP at the cost of health
            if (caster.CurrentHealth > card.HealthCost)
            {
                // Take health cost
                caster.TakeDamage(card.HealthCost, caster);
                
                // Grant additional AP
                // This assumes Unit has a method to gain AP, which it might not have
                // We could add this method to Unit class, for example:
                caster.GainActionPoints(2);
                
                return true;
            }
            
            return false;
        }
    }
    #endregion
}

/// <summary>
/// Example ScriptableObject assets for cards
/// In a real project, these would be created using the Unity editor
/// </summary>
#region Card Asset Templates

/*
 * Basic Attack Card Template
 * 
   {
       "name": "BasicAttackCard",
       "cardID": "basic_attack",
       "cardName": "Basic Attack",
       "cardDescription": "Deal {damage} damage to target enemy.",
       "category": "Attack",
       "rarity": "Common",
       "isCommon": true,
       "actionPointCost": 2,
       "movementPointCost": 0,
       "healthCost": 0,
       "effectType": "Damage",
       "baseDamage": 15,
       "effectRange": 5,
       "areaOfEffect": 0,
       "requiresTarget": true,
       "canTargetSelf": false,
       "canTargetAllies": false,
       "canTargetEnemies": true
   }
 */

/*
 * Basic Heal Card Template
 * 
   {
       "name": "BasicHealCard",
       "cardID": "basic_heal",
       "cardName": "Basic Heal",
       "cardDescription": "Heal target ally for {healing} health.",
       "category": "Support",
       "rarity": "Common",
       "isCommon": true,
       "actionPointCost": 2,
       "movementPointCost": 0,
       "healthCost": 0,
       "effectType": "Healing",
       "baseHealing": 12,
       "effectRange": 3,
       "areaOfEffect": 0,
       "requiresTarget": true,
       "canTargetSelf": true,
       "canTargetAllies": true,
       "canTargetEnemies": false
   }
 */

/*
 * Basic Move Card Template
 * 
   {
       "name": "BasicMoveCard",
       "cardID": "basic_move",
       "cardName": "Tactical Move",
       "cardDescription": "Move to a position up to {range} tiles away.",
       "category": "Movement",
       "rarity": "Common",
       "isCommon": true,
       "actionPointCost": 1,
       "movementPointCost": 0,
       "healthCost": 0,
       "effectType": "Movement",
       "effectRange": 4,
       "areaOfEffect": 0,
       "requiresTarget": false,
       "canTargetSelf": false,
       "canTargetAllies": false,
       "canTargetEnemies": false
   }
 */

/*
 * Grenade Card Template
 * 
   {
       "name": "GrenadeCard",
       "cardID": "grenade",
       "cardName": "Frag Grenade",
       "cardDescription": "Deal {damage} damage to all units in a {range} tile radius.",
       "category": "Attack",
       "rarity": "Uncommon",
       "isCommon": false,
       "actionPointCost": 3,
       "movementPointCost": 0,
       "healthCost": 0,
       "effectType": "AreaEffect",
       "baseDamage": 20,
       "effectRange": 4,
       "areaOfEffect": 2,
       "requiresTarget": true,
       "canTargetSelf": false,
       "canTargetAllies": false,
       "canTargetEnemies": true,
       "customEffectClass": "ExampleCards+GrenadeCardEffect"
   }
 */

/*
 * Shield Card Template
 * 
   {
       "name": "ShieldCard",
       "cardID": "shield",
       "cardName": "Energy Shield",
       "cardDescription": "Apply a shield to target ally that reduces incoming damage by 50% for {duration} turns.",
       "category": "Support",
       "rarity": "Uncommon",
       "isCommon": false,
       "actionPointCost": 2,
       "movementPointCost": 0,
       "healthCost": 0,
       "effectType": "Buff",
       "effectRange": 3,
       "effectDuration": 2,
       "requiresTarget": true,
       "canTargetSelf": true,
       "canTargetAllies": true,
       "canTargetEnemies": false,
       "customEffectClass": "ExampleCards+ShieldCardEffect"
   }
 */

/*
 * Stealth Card Template
 * 
   {
       "name": "StealthCard",
       "cardID": "stealth",
       "cardName": "Tactical Stealth",
       "cardDescription": "Target ally becomes invisible to enemies for {duration} turns.",
       "category": "Utility",
       "rarity": "Rare",
       "isCommon": false,
       "actionPointCost": 2,
       "movementPointCost": 0,
       "healthCost": 0,
       "effectType": "Buff",
       "effectRange": 2,
       "effectDuration": 2,
       "requiresTarget": true,
       "canTargetSelf": true,
       "canTargetAllies": true,
       "canTargetEnemies": false,
       "customEffectClass": "ExampleCards+StealthCardEffect"
   }
 */

/*
 * Cover Fire Card Template
 * 
   {
       "name": "CoverFireCard",
       "cardID": "cover_fire",
       "cardName": "Suppressive Fire",
       "cardDescription": "Deal {damage} damage to target enemy and reduce their AP by 1 for 1 turn.",
       "category": "Attack",
       "rarity": "Uncommon",
       "isCommon": false,
       "actionPointCost": 2,
       "movementPointCost": 0,
       "healthCost": 0,
       "effectType": "Damage",
       "baseDamage": 10,
       "effectRange": 6,
       "areaOfEffect": 0,
       "requiresTarget": true,
       "canTargetSelf": false,
       "canTargetAllies": false,
       "canTargetEnemies": true,
       "customEffectClass": "ExampleCards+CoverFireCardEffect"
   }
 */

/*
 * Overcharge Card Template
 * 
   {
       "name": "OverchargeCard",
       "cardID": "overcharge",
       "cardName": "Tactical Overcharge",
       "cardDescription": "Sacrifice {healthCost} health to gain 2 Action Points.",
       "category": "Utility",
       "rarity": "Uncommon",
       "isCommon": false,
       "actionPointCost": 0,
       "movementPointCost": 0,
       "healthCost": 10,
       "effectType": "Special",
       "requiresTarget": false,
       "canTargetSelf": true,
       "canTargetAllies": false,
       "canTargetEnemies": false,
       "customEffectClass": "ExampleCards+OverchargeCardEffect"
   }
 */

#endregion

/// <summary>
/// Example of Unit Card Deck configuration
/// In a real project, these would be created using the Unity editor
/// </summary>
#region Deck Templates

/*
 * Assault Deck Template
 *
   {
       "name": "AssaultDeck",
       "deckName": "Assault Operative",
       "deckDescription": "Focused on direct damage and offensive capabilities.",
       "specializedCards": [
           "grenadeCard",
           "coverFireCard",
           "overchargeCard"
       ],
       "commonCardCopies": 2,
       "maxSpecializedCards": 5
   }
 */

/*
 * Support Deck Template
 *
   {
       "name": "SupportDeck",
       "deckName": "Support Operative",
       "deckDescription": "Focused on healing and buffing allies.",
       "specializedCards": [
           "shieldCard",
           "basicHealCard"
       ],
       "commonCardCopies": 2,
       "maxSpecializedCards": 5
   }
 */

/*
 * Recon Deck Template
 *
   {
       "name": "ReconDeck",
       "deckName": "Recon Operative",
       "deckDescription": "Focused on mobility and stealth.",
       "specializedCards": [
           "stealthCard",
           "basicMoveCard"
       ],
       "commonCardCopies": 1,
       "maxSpecializedCards": 5
   }
 */

#endregion