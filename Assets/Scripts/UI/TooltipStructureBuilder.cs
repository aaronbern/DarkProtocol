#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class TooltipStructureBuilder : MonoBehaviour
{
    [ContextMenu("Build Tooltip Structure")]
    public void BuildTooltipStructure()
    {
        // Get this tooltip object
        GameObject tooltip = gameObject;
        
        // Create background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(tooltip.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Create content container
        GameObject content = new GameObject("Content");
        content.transform.SetParent(tooltip.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(10, 10);
        contentRect.offsetMax = new Vector2(-10, -10);
        
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(5, 5, 5, 5);
        
        // Card Name
        GameObject cardName = CreateText(content.transform, "CardNameText", "Card Name", 18, true);
        
        // Card Type
        GameObject cardType = CreateText(content.transform, "CardTypeText", "Attack", 14, false);
        
        // Card Description
        GameObject cardDesc = CreateText(content.transform, "CardDescriptionText", "Card description goes here", 12, false);
        
        // Stats Container
        GameObject statsContainer = new GameObject("StatsContainer");
        statsContainer.transform.SetParent(content.transform, false);
        RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
        HorizontalLayoutGroup statsLayout = statsContainer.AddComponent<HorizontalLayoutGroup>();
        statsLayout.spacing = 10;
        
        // AP Cost
        GameObject apCost = CreateText(statsContainer.transform, "APCostText", "AP: 2", 12, false);
        
        // Damage
        GameObject damage = CreateText(statsContainer.transform, "DamageText", "Damage: 10", 12, false);
        
        // Range
        GameObject range = CreateText(statsContainer.transform, "RangeText", "Range: 3", 12, false);
        
        // Rarity
        GameObject rarity = CreateText(content.transform, "RarityText", "Common", 12, false);
        
        // Rarity Border (frame around tooltip)
        GameObject rarityBorder = new GameObject("RarityBorder");
        rarityBorder.transform.SetParent(tooltip.transform, false);
        Image borderImage = rarityBorder.AddComponent<Image>();
        borderImage.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        borderImage.raycastTarget = false;
        borderImage.fillCenter = false; // Makes it a border only
        RectTransform borderRect = rarityBorder.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
        
        // Tag Container
        GameObject tagContainer = new GameObject("TagContainer");
        tagContainer.transform.SetParent(content.transform, false);
        RectTransform tagRect = tagContainer.AddComponent<RectTransform>();
        HorizontalLayoutGroup tagLayout = tagContainer.AddComponent<HorizontalLayoutGroup>();
        tagLayout.spacing = 5;
        
        // Tag Prefab (create a simple one)
        GameObject tagPrefab = new GameObject("TagPrefab");
        tagPrefab.AddComponent<RectTransform>().sizeDelta = new Vector2(50, 20);
        Image tagBg = tagPrefab.AddComponent<Image>();
        tagBg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        GameObject tagText = CreateText(tagPrefab.transform, "Text", "Tag", 10, false);
        
        // Status Effect Container
        GameObject statusContainer = new GameObject("StatusEffectContainer");
        statusContainer.transform.SetParent(content.transform, false);
        RectTransform statusRect = statusContainer.AddComponent<RectTransform>();
        HorizontalLayoutGroup statusLayout = statusContainer.AddComponent<HorizontalLayoutGroup>();
        statusLayout.spacing = 5;
        
        // Status Effect Prefab
        GameObject statusPrefab = new GameObject("StatusEffectPrefab");
        statusPrefab.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 20);
        Image statusBg = statusPrefab.AddComponent<Image>();
        statusBg.color = new Color(0.4f, 0.2f, 0.2f, 0.8f);
        GameObject statusText = CreateText(statusPrefab.transform, "Text", "Effect", 10, false);
        
        // Make prefabs inactive (they're just templates)
        tagPrefab.SetActive(false);
        statusPrefab.SetActive(false);
        
        Debug.Log("Tooltip structure built! Now assign the references in the Inspector.");
    }
    
    private GameObject CreateText(Transform parent, string name, string text, int fontSize, bool bold)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        
        return textObj;
    }
}
#endif