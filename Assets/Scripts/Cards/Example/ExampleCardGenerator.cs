using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using DarkProtocol.Cards;

public class ExampleCardGenerator : EditorWindow
{
    private string assetPath = "Assets/Scripts/Cards/Example";
    private bool createBasicCards = true;
    private bool createSpecialCards = true;
    private bool createDecks = true;

    [MenuItem("Tools/Dark Protocol/Generate Example Cards")]
    public static void ShowWindow()
    {
        GetWindow<ExampleCardGenerator>("Card Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Example Card Generator", EditorStyles.boldLabel);

        assetPath = EditorGUILayout.TextField("Asset Path", assetPath);
        createBasicCards = EditorGUILayout.Toggle("Create Basic Cards", createBasicCards);
        createSpecialCards = EditorGUILayout.Toggle("Create Special Cards", createSpecialCards);
        createDecks = EditorGUILayout.Toggle("Create Decks", createDecks);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Cards"))
        {
            GenerateCards();
        }
    }

    private void GenerateCards()
    {
        if (!System.IO.Directory.Exists(assetPath))
            System.IO.Directory.CreateDirectory(assetPath);

        List<CardData> allCards = new List<CardData>();
        List<CardData> basicCards = new List<CardData>();
        List<CardData> assaultCards = new List<CardData>();
        List<CardData> supportCards = new List<CardData>();
        List<CardData> reconCards = new List<CardData>();

        if (createBasicCards)
        {
            CardData basicAttack = CreateCardAsset("BasicAttackCard", "basic_attack", "Basic Attack",
                "Deal {damage} damage to target enemy.", CardCategory.Attack, CardRarity.Common, true);
            SetCardStats(basicAttack, 2, CardEffectType.Damage, 15, 0, 5, 0, 0, true, false, false, true, null);
            SaveAsset(basicAttack, "BasicAttackCard");
            basicCards.Add(basicAttack);
            assaultCards.Add(basicAttack);
            allCards.Add(basicAttack);

            CardData basicHeal = CreateCardAsset("BasicHealCard", "basic_heal", "Basic Heal",
                "Heal target ally for {healing} health.", CardCategory.Support, CardRarity.Common, true);
            SetCardStats(basicHeal, 2, CardEffectType.Healing, 0, 12, 3, 0, 0, true, true, true, false, null);
            SaveAsset(basicHeal, "BasicHealCard");
            basicCards.Add(basicHeal);
            supportCards.Add(basicHeal);
            allCards.Add(basicHeal);

            CardData basicMove = CreateCardAsset("BasicMoveCard", "basic_move", "Tactical Move",
                "Move to a position up to {range} tiles away.", CardCategory.Movement, CardRarity.Common, true);
            SetCardStats(basicMove, 1, CardEffectType.Movement, 0, 0, 4, 0, 0, false, false, false, false, null);
            SaveAsset(basicMove, "BasicMoveCard");
            basicCards.Add(basicMove);
            reconCards.Add(basicMove);
            allCards.Add(basicMove);
        }

        if (createSpecialCards)
        {
            CardData grenadeCard = CreateCardAsset("GrenadeCard", "grenade", "Frag Grenade",
                "Deal {damage} damage to all units in a {range} tile radius.", CardCategory.Attack, CardRarity.Uncommon, false);
            SetCardStats(grenadeCard, 3, CardEffectType.AreaEffect, 20, 0, 4, 0, 2, true, false, false, true, "ExampleCards+GrenadeCardEffect");
            SaveAsset(grenadeCard, "GrenadeCard");
            assaultCards.Add(grenadeCard);
            allCards.Add(grenadeCard);

            CardData shieldCard = CreateCardAsset("ShieldCard", "shield", "Energy Shield",
                "Apply a shield to target ally that reduces incoming damage by 50% for {duration} turns.", CardCategory.Support, CardRarity.Uncommon, false);
            SetCardStats(shieldCard, 2, CardEffectType.Buff, 0, 0, 3, 2, 0, true, true, true, false, "ExampleCards+ShieldCardEffect");
            SaveAsset(shieldCard, "ShieldCard");
            supportCards.Add(shieldCard);
            allCards.Add(shieldCard);

            CardData stealthCard = CreateCardAsset("StealthCard", "stealth", "Tactical Stealth",
                "Target ally becomes invisible to enemies for {duration} turns.", CardCategory.Utility, CardRarity.Rare, false);
            SetCardStats(stealthCard, 2, CardEffectType.Buff, 0, 0, 2, 2, 0, true, true, true, false, "ExampleCards+StealthCardEffect");
            SaveAsset(stealthCard, "StealthCard");
            reconCards.Add(stealthCard);
            allCards.Add(stealthCard);

            CardData coverFireCard = CreateCardAsset("CoverFireCard", "cover_fire", "Suppressive Fire",
                "Deal {damage} damage to target enemy and reduce their AP by 1 for 1 turn.", CardCategory.Attack, CardRarity.Uncommon, false);
            SetCardStats(coverFireCard, 2, CardEffectType.Damage, 10, 0, 6, 0, 0, true, false, false, true, "ExampleCards+CoverFireCardEffect");
            SaveAsset(coverFireCard, "CoverFireCard");
            assaultCards.Add(coverFireCard);
            allCards.Add(coverFireCard);

            CardData overchargeCard = CreateCardAsset("OverchargeCard", "overcharge", "Tactical Overcharge",
                "Sacrifice {healthCost} health to gain 2 Action Points.", CardCategory.Utility, CardRarity.Uncommon, false);
            SetCardStats(overchargeCard, 0, CardEffectType.Special, 0, 0, 0, 0, 0, false, true, false, false, "ExampleCards+OverchargeCardEffect", 10);
            SaveAsset(overchargeCard, "OverchargeCard");
            assaultCards.Add(overchargeCard);
            allCards.Add(overchargeCard);
        }

        if (createDecks)
        {
            UnitCardDeck assaultDeck = CreateDeckAsset("AssaultDeck", "Assault Operative", "Focused on direct damage and offensive capabilities.");
            SetDeckStats(assaultDeck, 2, 5);
            assaultDeck.SpecializedCards.AddRange(assaultCards);
            SaveAsset(assaultDeck, "AssaultDeck");

            UnitCardDeck supportDeck = CreateDeckAsset("SupportDeck", "Support Operative", "Focused on healing and buffing allies.");
            SetDeckStats(supportDeck, 2, 5);
            supportDeck.SpecializedCards.AddRange(supportCards);
            SaveAsset(supportDeck, "SupportDeck");

            UnitCardDeck reconDeck = CreateDeckAsset("ReconDeck", "Recon Operative", "Focused on mobility and stealth.");
            SetDeckStats(reconDeck, 1, 5);
            reconDeck.SpecializedCards.AddRange(reconCards);
            SaveAsset(reconDeck, "ReconDeck");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated {allCards.Count} cards and 3 decks at {assetPath}");
    }

    private CardData CreateCardAsset(string assetName, string cardID, string cardName, string description,
                                     CardCategory category, CardRarity rarity, bool isCommon)
    {
        CardData card = ScriptableObject.CreateInstance<CardData>();
        SetPrivate(card, "cardID", cardID);
        SetPrivate(card, "cardName", cardName);
        SetPrivate(card, "cardDescription", description);
        SetPrivate(card, "category", category);
        SetPrivate(card, "rarity", rarity);
        SetPrivate(card, "isCommon", isCommon);
        return card;
    }

    private void SetCardStats(CardData card, int apCost, CardEffectType effectType, int dmg, int heal, int range, int duration, int aoe,
                              bool requiresTarget, bool self, bool allies, bool enemies, string customEffect, int healthCost = 0)
    {
        SetPrivate(card, "actionPointCost", apCost);
        SetPrivate(card, "effectType", effectType);
        SetPrivate(card, "baseDamage", dmg);
        SetPrivate(card, "baseHealing", heal);
        SetPrivate(card, "effectRange", range);
        SetPrivate(card, "effectDuration", duration);
        SetPrivate(card, "areaOfEffect", aoe);
        SetPrivate(card, "requiresTarget", requiresTarget);
        SetPrivate(card, "canTargetSelf", self);
        SetPrivate(card, "canTargetAllies", allies);
        SetPrivate(card, "canTargetEnemies", enemies);
        SetPrivate(card, "customEffectClass", customEffect);
        SetPrivate(card, "healthCost", healthCost);
    }

    private UnitCardDeck CreateDeckAsset(string assetName, string deckName, string description)
    {
        UnitCardDeck deck = ScriptableObject.CreateInstance<UnitCardDeck>();
        SetPrivate(deck, "deckName", deckName);
        SetPrivate(deck, "deckDescription", description);
        return deck;
    }

    private void SetDeckStats(UnitCardDeck deck, int commonCopies, int maxSpecial)
    {
        SetPrivate(deck, "commonCardCopies", commonCopies);
        SetPrivate(deck, "maxSpecializedCards", maxSpecial);
    }

    private void SetPrivate(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }

    private void SaveAsset(Object asset, string assetName)
    {
        string fullPath = $"{assetPath}/{assetName}.asset";
        AssetDatabase.CreateAsset(asset, fullPath);
    }
}
