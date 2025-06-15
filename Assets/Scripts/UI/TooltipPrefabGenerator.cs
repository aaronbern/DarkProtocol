using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DarkProtocol.UI
{
    /// <summary>
    /// Generates modern, clean tooltip prefabs for Dark Protocol
    /// Run from menu: Dark Protocol > Create UI Prefabs
    /// </summary>
    public class TooltipPrefabGenerator : MonoBehaviour
    {
        // Modern dark theme colors
        private static readonly Color BACKGROUND_COLOR = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        private static readonly Color BORDER_COLOR = new Color(0.2f, 0.8f, 1f, 0.8f);
        private static readonly Color TEXT_COLOR = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color SECONDARY_TEXT_COLOR = new Color(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color ACCENT_COLOR = new Color(0.2f, 0.8f, 1f, 1f);

#if UNITY_EDITOR
        [MenuItem("Dark Protocol/Create UI Prefabs/Create All Tooltip Prefabs")]
        public static void CreateAllTooltipPrefabs()
        {
            CreateSimpleTooltipPrefab();
            CreateCardTooltipPrefab();
            CreateUnitTooltipPrefab();
            CreateStatusEffectTooltipPrefab();
            CreateUnitNameplatePrefab();
            CreateStatusIconPrefab();

            Debug.Log("All Dark Protocol UI prefabs created successfully!");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Dark Protocol/Create UI Prefabs/Simple Tooltip")]
        public static void CreateSimpleTooltipPrefab()
        {
            GameObject tooltip = new GameObject("SimpleTooltipPrefab");

            // Add base components
            RectTransform rect = tooltip.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 100);

            CanvasGroup canvasGroup = tooltip.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Background with modern styling
            Image background = tooltip.AddComponent<Image>();
            background.color = BACKGROUND_COLOR;
            background.raycastTarget = false;

            // Add modern border
            GameObject border = CreateBorder(tooltip.transform);

            // Content container with padding
            GameObject content = new GameObject("Content");
            content.transform.SetParent(tooltip.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(15, 15);
            contentRect.offsetMax = new Vector2(-15, -15);

            // Main text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(content.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            text.text = "Tooltip Text";
            text.fontSize = 14;
            text.color = TEXT_COLOR;
            text.alignment = TextAlignmentOptions.Center;
            text.font = GetDefaultFont();

            // Add subtle shadow
            AddShadow(tooltip);

            SaveAsPrefab(tooltip, "SimpleTooltipPrefab");
        }

        [MenuItem("Dark Protocol/Create UI Prefabs/Card Tooltip")]
        public static void CreateCardTooltipPrefab()
        {
            GameObject tooltip = new GameObject("CardTooltipPrefab");

            // Base setup
            RectTransform rect = tooltip.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(350, 450);

            CanvasGroup canvasGroup = tooltip.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Image background = tooltip.AddComponent<Image>();
            background.color = BACKGROUND_COLOR;
            background.raycastTarget = false;

            // Add CardTooltip component
            CardTooltip cardTooltip = tooltip.AddComponent<CardTooltip>();

            // Border
            GameObject border = CreateBorder(tooltip.transform);

            // Content with padding
            GameObject content = new GameObject("Content");
            content.transform.SetParent(tooltip.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -20);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // Header section
            GameObject header = new GameObject("Header");
            header.transform.SetParent(content.transform, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 60);

            // Card name
            GameObject nameObj = CreateText(header.transform, "CardName", "Card Name", 24,
                TextAlignmentOptions.Left, new Vector2(0, 30), new Vector2(-80, 0));
            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = ACCENT_COLOR;

            // Card type
            GameObject typeObj = CreateText(header.transform, "CardType", "Attack", 16,
                TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(-80, 30));
            TextMeshProUGUI typeText = typeObj.GetComponent<TextMeshProUGUI>();
            typeText.color = SECONDARY_TEXT_COLOR;

            // AP Cost badge
            GameObject apBadge = CreateAPCostBadge(header.transform);

            // Divider
            CreateDivider(content.transform);

            // Stats section
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(content.transform, false);
            RectTransform statsRect = stats.AddComponent<RectTransform>();
            statsRect.sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup statsLayout = stats.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 20;
            statsLayout.childControlHeight = true;
            statsLayout.childControlWidth = false;
            statsLayout.childForceExpandHeight = true;
            statsLayout.childForceExpandWidth = false;

            // Damage stat
            GameObject damageObj = CreateStatDisplay(stats.transform, "DamageText", "Damage: 5", Color.red);

            // Range stat
            GameObject rangeObj = CreateStatDisplay(stats.transform, "RangeText", "Range: 3", Color.cyan);

            // Description section
            GameObject descBg = new GameObject("DescriptionBackground");
            descBg.transform.SetParent(content.transform, false);
            Image descBgImage = descBg.AddComponent<Image>();
            descBgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);

            RectTransform descBgRect = descBg.GetComponent<RectTransform>();
            LayoutElement descLayout = descBg.AddComponent<LayoutElement>();
            descLayout.preferredHeight = 200;
            descLayout.flexibleHeight = 1;

            GameObject descObj = CreateText(descBg.transform, "Description",
                "Card description goes here. This text will explain what the card does.",
                14, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.zero);
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.offsetMin = new Vector2(10, 10);
            descRect.offsetMax = new Vector2(-10, -10);

            // Tag container (for future synergy tags)
            GameObject tagContainer = new GameObject("TagContainer");
            tagContainer.transform.SetParent(content.transform, false);
            RectTransform tagRect = tagContainer.AddComponent<RectTransform>();
            tagRect.sizeDelta = new Vector2(0, 40);
            HorizontalLayoutGroup tagLayout = tagContainer.AddComponent<HorizontalLayoutGroup>();
            tagLayout.spacing = 10;
            tagLayout.childAlignment = TextAnchor.MiddleCenter;

            // Rarity border (colored accent)
            GameObject rarityBorder = new GameObject("RarityBorder");
            rarityBorder.transform.SetParent(tooltip.transform, false);
            Image rarityImage = rarityBorder.AddComponent<Image>();
            rarityImage.color = ACCENT_COLOR;
            rarityImage.raycastTarget = false;

            RectTransform rarityRect = rarityBorder.GetComponent<RectTransform>();
            rarityRect.anchorMin = new Vector2(0, 0.95f);
            rarityRect.anchorMax = new Vector2(1, 1);
            rarityRect.offsetMin = Vector2.zero;
            rarityRect.offsetMax = Vector2.zero;

            // Shadow
            AddShadow(tooltip);

            // Link references
            cardTooltip.cardNameText = nameText;
            cardTooltip.cardTypeText = typeText;
            cardTooltip.cardDescriptionText = descObj.GetComponent<TextMeshProUGUI>();
            cardTooltip.apCostText = apBadge.GetComponentInChildren<TextMeshProUGUI>();
            cardTooltip.damageText = damageObj.GetComponent<TextMeshProUGUI>();
            cardTooltip.rangeText = rangeObj.GetComponent<TextMeshProUGUI>();
            cardTooltip.rarityBorder = rarityImage;
            cardTooltip.tagContainer = tagContainer.transform;

            SaveAsPrefab(tooltip, "CardTooltipPrefab");
        }

        [MenuItem("Dark Protocol/Create UI Prefabs/Unit Tooltip")]
        public static void CreateUnitTooltipPrefab()
        {
            GameObject tooltip = new GameObject("UnitTooltipPrefab");

            // Base setup
            RectTransform rect = tooltip.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 250);

            CanvasGroup canvasGroup = tooltip.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Image background = tooltip.AddComponent<Image>();
            background.color = BACKGROUND_COLOR;
            background.raycastTarget = false;

            // Add UnitTooltip component
            UnitTooltip unitTooltip = tooltip.AddComponent<UnitTooltip>();

            // Border
            CreateBorder(tooltip.transform);

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(tooltip.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -20);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            // Header
            GameObject header = new GameObject("Header");
            header.transform.SetParent(content.transform, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 50);

            GameObject nameObj = CreateText(header.transform, "UnitName", "Unit Name", 20,
                TextAlignmentOptions.TopLeft, new Vector2(0, 25), Vector2.zero);
            nameObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            GameObject classObj = CreateText(header.transform, "UnitClass", "Soldier", 14,
                TextAlignmentOptions.TopLeft, new Vector2(0, 0), new Vector2(0, 25));
            classObj.GetComponent<TextMeshProUGUI>().color = SECONDARY_TEXT_COLOR;

            // Stats
            var (healthBar, healthText, healthFill) = CreateStatBar(content.transform, "Health", Color.green);
            var (apBar, apText, apFill) = CreateStatBar(content.transform, "AP", ACCENT_COLOR);
            var (mpBar, mpText, mpFill) = CreateStatBar(content.transform, "MP", Color.yellow);

            // Status effects
            GameObject statusContainer = new GameObject("StatusEffectContainer");
            statusContainer.transform.SetParent(content.transform, false);
            RectTransform statusRect = statusContainer.AddComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(0, 40);

            HorizontalLayoutGroup statusLayout = statusContainer.AddComponent<HorizontalLayoutGroup>();
            statusLayout.spacing = 5;
            statusLayout.childAlignment = TextAnchor.MiddleLeft;

            // Shadow
            AddShadow(tooltip);

            // Link references
            unitTooltip.unitNameText = nameObj.GetComponent<TextMeshProUGUI>();
            unitTooltip.unitClassText = classObj.GetComponent<TextMeshProUGUI>();
            unitTooltip.healthText = healthText;
            unitTooltip.apText = apText;
            unitTooltip.mpText = mpText;
            unitTooltip.healthBar = healthFill;
            unitTooltip.apBar = apFill;
            unitTooltip.mpBar = mpFill;
            unitTooltip.statusEffectContainer = statusContainer.transform;

            SaveAsPrefab(tooltip, "UnitTooltipPrefab");
        }

        [MenuItem("Dark Protocol/Create UI Prefabs/Status Effect Tooltip")]
        public static void CreateStatusEffectTooltipPrefab()
        {
            GameObject tooltip = new GameObject("StatusEffectTooltipPrefab");

            // Base setup
            RectTransform rect = tooltip.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 200);

            CanvasGroup canvasGroup = tooltip.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Image background = tooltip.AddComponent<Image>();
            background.color = BACKGROUND_COLOR;
            background.raycastTarget = false;

            // Add StatusEffectTooltip component
            StatusEffectTooltip effectTooltip = tooltip.AddComponent<StatusEffectTooltip>();

            // Border
            GameObject border = CreateBorder(tooltip.transform);

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(tooltip.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -20);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;

            // Header with icon
            GameObject header = new GameObject("Header");
            header.transform.SetParent(content.transform, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 40);

            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 10;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;

            // Icon
            GameObject iconObj = new GameObject("EffectIcon");
            iconObj.transform.SetParent(header.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.color = Color.white;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(40, 40);

            // Name and type
            GameObject textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(header.transform, false);
            VerticalLayoutGroup textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2;

            GameObject nameObj = CreateText(textContainer.transform, "EffectName", "Effect Name",
                16, TextAlignmentOptions.Left, Vector2.zero, Vector2.zero);
            nameObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            GameObject typeObj = CreateText(textContainer.transform, "EffectType", "Buff",
                12, TextAlignmentOptions.Left, Vector2.zero, Vector2.zero);

            // Description
            GameObject descObj = CreateText(content.transform, "Description",
                "Effect description goes here.", 14, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.zero);

            // Duration
            GameObject durationObj = CreateText(content.transform, "Duration",
                "Duration: 3 turns", 14, TextAlignmentOptions.Left, Vector2.zero, Vector2.zero);
            durationObj.GetComponent<TextMeshProUGUI>().color = SECONDARY_TEXT_COLOR;

            // Stackable indicator
            GameObject stackable = CreateText(content.transform, "StackableIndicator",
                "Max Stacks: 3", 12, TextAlignmentOptions.Left, Vector2.zero, Vector2.zero);
            stackable.GetComponent<TextMeshProUGUI>().color = ACCENT_COLOR;

            // Shadow
            AddShadow(tooltip);

            // Link references
            effectTooltip.effectNameText = nameObj.GetComponent<TextMeshProUGUI>();
            effectTooltip.effectTypeText = typeObj.GetComponent<TextMeshProUGUI>();
            effectTooltip.effectDescriptionText = descObj.GetComponent<TextMeshProUGUI>();
            effectTooltip.durationText = durationObj.GetComponent<TextMeshProUGUI>();
            effectTooltip.effectIcon = icon;
            effectTooltip.rarityBorder = border.GetComponent<Image>();
            effectTooltip.stackableIndicator = stackable;
            effectTooltip.stackText = stackable.GetComponent<TextMeshProUGUI>();

            SaveAsPrefab(tooltip, "StatusEffectTooltipPrefab");
        }

        [MenuItem("Dark Protocol/Create UI Prefabs/Unit Nameplate")]
        public static void CreateUnitNameplatePrefab()
        {
            GameObject nameplate = new GameObject("UnitNameplatePrefab");

            // World space canvas with advanced settings
            Canvas canvas = nameplate.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10; // Render above other UI elements

            RectTransform canvasRect = nameplate.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(3f, 1.2f); // Larger, more detailed space

            // Scale and positioning
            nameplate.transform.localScale = Vector3.one * 0.015f; // Slight increase in base scale

            CanvasScaler scaler = nameplate.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            GraphicRaycaster raycaster = nameplate.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            // Add UnitNameplate component
            UnitNameplate unitNameplate = nameplate.AddComponent<UnitNameplate>();

            // Holographic background with gradient and scanline effect
            GameObject bgGradient = new GameObject("BackgroundGradient");
            bgGradient.transform.SetParent(nameplate.transform, false);
            Image bgGradientImage = bgGradient.AddComponent<Image>();
            bgGradientImage.color = new Color(0.05f, 0.05f, 0.1f, 0.8f);
            bgGradientImage.raycastTarget = false;

            RectTransform bgGradientRect = bgGradient.GetComponent<RectTransform>();
            bgGradientRect.anchorMin = Vector2.zero;
            bgGradientRect.anchorMax = Vector2.one;
            bgGradientRect.offsetMin = Vector2.zero;
            bgGradientRect.offsetMax = Vector2.zero;

            // Add a subtle border for holographic effect
            GameObject border = new GameObject("HolographicBorder");
            border.transform.SetParent(nameplate.transform, false);
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.2f, 0.8f, 1f, 0.5f); // Cyan-blue holographic border
            borderImage.type = Image.Type.Sliced;

            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-5, -5);
            borderRect.offsetMax = new Vector2(5, 5);

            // Futuristic name display
            GameObject nameObj = CreateText(nameplate.transform, "NameText", "UNIT-7492",
                16, TextAlignmentOptions.Center, new Vector2(0, 0.85f), new Vector2(1, 1));
            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = new Color(0.2f, 0.8f, 1f, 0.9f); // Cyan-blue holographic text

            // Advanced health bar with background and fill
            GameObject healthContainer = new GameObject("HealthContainer");
            healthContainer.transform.SetParent(nameplate.transform, false);
            RectTransform healthContainerRect = healthContainer.AddComponent<RectTransform>();
            healthContainerRect.anchorMin = new Vector2(0.1f, 0.4f);
            healthContainerRect.anchorMax = new Vector2(0.9f, 0.6f);

            // Health bar background (grid-like)
            GameObject healthBg = new GameObject("HealthBackground");
            healthBg.transform.SetParent(healthContainer.transform, false);
            Image healthBgImage = healthBg.AddComponent<Image>();
            healthBgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.6f);
            healthBgImage.raycastTarget = false;

            RectTransform healthBgRect = healthBg.GetComponent<RectTransform>();
            healthBgRect.anchorMin = Vector2.zero;
            healthBgRect.anchorMax = Vector2.one;

            // Health bar fill with gradient
            GameObject healthFill = new GameObject("HealthBar");
            healthFill.transform.SetParent(healthBg.transform, false);
            Image healthImage = healthFill.AddComponent<Image>();
            healthImage.color = new Color(0.2f, 1f, 0.2f, 0.9f); // Bright green
            healthImage.type = Image.Type.Filled;
            healthImage.fillMethod = Image.FillMethod.Horizontal;
            healthImage.fillOrigin = 0;
            healthImage.raycastTarget = false;

            RectTransform healthFillRect = healthFill.GetComponent<RectTransform>();
            healthFillRect.anchorMin = Vector2.zero;
            healthFillRect.anchorMax = Vector2.one;

            // Action Points container
            GameObject apContainer = new GameObject("APContainer");
            apContainer.transform.SetParent(nameplate.transform, false);
            RectTransform apContainerRect = apContainer.AddComponent<RectTransform>();
            apContainerRect.anchorMin = new Vector2(0.1f, 0.2f);
            apContainerRect.anchorMax = new Vector2(0.9f, 0.3f);

            // AP bar background
            GameObject apBg = new GameObject("APBackground");
            apBg.transform.SetParent(apContainer.transform, false);
            Image apBgImage = apBg.AddComponent<Image>();
            apBgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.6f);
            apBgImage.raycastTarget = false;

            RectTransform apBgRect = apBg.GetComponent<RectTransform>();
            apBgRect.anchorMin = Vector2.zero;
            apBgRect.anchorMax = Vector2.one;

            // AP bar fill
            GameObject apFill = new GameObject("APBar");
            apFill.transform.SetParent(apBg.transform, false);
            Image apImage = apFill.AddComponent<Image>();
            apImage.color = new Color(0.2f, 0.8f, 1f, 0.9f); // Cyan-blue
            apImage.type = Image.Type.Filled;
            apImage.fillMethod = Image.FillMethod.Horizontal;
            apImage.fillOrigin = 0;
            apImage.raycastTarget = false;

            RectTransform apFillRect = apFill.GetComponent<RectTransform>();
            apFillRect.anchorMin = Vector2.zero;
            apFillRect.anchorMax = Vector2.one;

            // Status effect container
            GameObject statusContainer = new GameObject("StatusContainer");
            statusContainer.transform.SetParent(nameplate.transform, false);
            RectTransform statusRect = statusContainer.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, -0.2f);
            statusRect.anchorMax = new Vector2(1, 0);

            HorizontalLayoutGroup statusLayout = statusContainer.AddComponent<HorizontalLayoutGroup>();
            statusLayout.spacing = 5;
            statusLayout.childAlignment = TextAnchor.MiddleCenter;
            statusLayout.childControlHeight = false;
            statusLayout.childControlWidth = false;

            // Link references to UnitNameplate component
            unitNameplate.nameplateCanvas = canvas;
            unitNameplate.canvasRect = canvasRect;
            unitNameplate.nameText = nameText;
            unitNameplate.backgroundGradient = bgGradientImage;
            unitNameplate.healthBarBackground = healthBgImage;
            unitNameplate.healthBar = healthImage;
            unitNameplate.apBar = apImage;
            unitNameplate.apContainer = apContainer;
            unitNameplate.statusIconContainer = statusContainer.transform;

            // Advanced nameplate settings
            unitNameplate.heightOffset = 1.8f;
            unitNameplate.adaptiveScaling = true;
            unitNameplate.dynamicRotation = true;

            SaveAsPrefab(nameplate, "UnitNameplatePrefab");
        }

        [MenuItem("Dark Protocol/Create UI Prefabs/Status Icon")]
        public static void CreateStatusIconPrefab()
        {
            GameObject icon = new GameObject("StatusIconPrefab");

            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(32, 32);

            // Icon image
            Image image = icon.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;

            // Stack count text
            GameObject stackObj = new GameObject("StackCount");
            stackObj.transform.SetParent(icon.transform, false);

            TextMeshProUGUI stackText = stackObj.AddComponent<TextMeshProUGUI>();
            stackText.text = "1";
            stackText.fontSize = 12;
            stackText.fontStyle = FontStyles.Bold;
            stackText.color = Color.white;
            stackText.alignment = TextAlignmentOptions.BottomRight;
            stackText.font = GetDefaultFont();

            RectTransform stackRect = stackObj.GetComponent<RectTransform>();
            stackRect.anchorMin = new Vector2(0.5f, 0);
            stackRect.anchorMax = new Vector2(1, 0.5f);
            stackRect.offsetMin = Vector2.zero;
            stackRect.offsetMax = Vector2.zero;

            // Outline for visibility
            Outline outline = stackObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            SaveAsPrefab(icon, "StatusIconPrefab");
        }

        // Helper methods
        private static GameObject CreateBorder(Transform parent)
        {
            GameObject border = new GameObject("Border");
            border.transform.SetParent(parent, false);

            Image borderImage = border.AddComponent<Image>();
            borderImage.color = BORDER_COLOR;
            borderImage.raycastTarget = false;

            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            // Inner mask to create border effect
            GameObject mask = new GameObject("InnerMask");
            mask.transform.SetParent(border.transform, false);

            Image maskImage = mask.AddComponent<Image>();
            maskImage.color = BACKGROUND_COLOR;
            maskImage.raycastTarget = false;

            RectTransform maskRect = mask.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = new Vector2(2, 2);
            maskRect.offsetMax = new Vector2(-2, -2);

            return border;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TEXT_COLOR;
            tmp.alignment = alignment;
            tmp.font = GetDefaultFont();

            RectTransform rect = textObj.GetComponent<RectTransform>();
            if (anchorMin != Vector2.zero || anchorMax != Vector2.zero)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
            }
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return textObj;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);

            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            RectTransform dividerRect = divider.GetComponent<RectTransform>();
            dividerRect.sizeDelta = new Vector2(0, 1);

            LayoutElement layout = divider.AddComponent<LayoutElement>();
            layout.preferredHeight = 1;
        }

        private static (GameObject container, TextMeshProUGUI valueText, Image fillImage) CreateStatBar(Transform parent, string statName, Color barColor)
        {
            GameObject container = new GameObject(statName + "Container");
            container.transform.SetParent(parent, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 30);

            LayoutElement layout = container.AddComponent<LayoutElement>();
            layout.preferredHeight = 30;

            // Label
            GameObject label = CreateText(container.transform, "Label", statName + ":",
                12, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(0.3f, 1));

            // Value text
            GameObject value = CreateText(container.transform, "Value", "10/10",
                12, TextAlignmentOptions.Right, new Vector2(0.7f, 0), new Vector2(1, 1));
            TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();

            // Bar background
            GameObject barBg = new GameObject("BarBackground");
            barBg.transform.SetParent(container.transform, false);
            Image barBgImage = barBg.AddComponent<Image>();
            barBgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            RectTransform barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0, 0.2f);
            barBgRect.anchorMax = new Vector2(1, 0.5f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;

            // Bar fill
            GameObject barFill = new GameObject("Fill");
            barFill.transform.SetParent(barBg.transform, false);
            Image fillImage = barFill.AddComponent<Image>();
            fillImage.color = barColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            RectTransform fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            return (container, valueText, fillImage);
        }

        private static GameObject CreateAPCostBadge(Transform parent)
        {
            GameObject badge = new GameObject("APCostBadge");
            badge.transform.SetParent(parent, false);

            Image badgeImage = badge.AddComponent<Image>();
            badgeImage.color = ACCENT_COLOR;

            RectTransform badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1, 0.5f);
            badgeRect.anchorMax = new Vector2(1, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-30, 0);
            badgeRect.sizeDelta = new Vector2(50, 50);

            // AP text
            GameObject apText = CreateText(badge.transform, "APText", "2",
                20, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            apText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            return badge;
        }

        private static GameObject CreateStatDisplay(Transform parent, string name, string text, Color accentColor)
        {
            GameObject stat = new GameObject(name);
            stat.transform.SetParent(parent, false);

            TextMeshProUGUI statText = stat.AddComponent<TextMeshProUGUI>();
            statText.text = text;
            statText.fontSize = 14;
            statText.color = TEXT_COLOR;
            statText.font = GetDefaultFont();

            // Color the number
            statText.text = text.Replace(":", ":</color> <color=#" + ColorUtility.ToHtmlStringRGB(accentColor) + ">");

            LayoutElement layout = stat.AddComponent<LayoutElement>();
            layout.preferredWidth = 80;

            return stat;
        }

        private static void AddShadow(GameObject obj)
        {
            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(obj.transform.parent, false);
            shadow.transform.SetSiblingIndex(0);

            Image shadowImage = shadow.AddComponent<Image>();
            shadowImage.color = new Color(0, 0, 0, 0.5f);
            shadowImage.raycastTarget = false;

            RectTransform shadowRect = shadow.GetComponent<RectTransform>();
            shadowRect.anchorMin = obj.GetComponent<RectTransform>().anchorMin;
            shadowRect.anchorMax = obj.GetComponent<RectTransform>().anchorMax;
            shadowRect.sizeDelta = obj.GetComponent<RectTransform>().sizeDelta;
            shadowRect.anchoredPosition = obj.GetComponent<RectTransform>().anchoredPosition + new Vector2(5, -5);
        }

        private static TMP_FontAsset GetDefaultFont()
        {
            // Try to find a default TextMeshPro font
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts.Length > 0)
            {
                // Prefer fonts with "SDF" in the name
                foreach (var font in fonts)
                {
                    if (font.name.Contains("SDF"))
                        return font;
                }
                return fonts[0];
            }
            return null;
        }

        private static void SaveAsPrefab(GameObject obj, string prefabName)
        {
            string path = "Assets/Prefabs/UI/" + prefabName + ".prefab";

            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Debug.Log($"Created prefab: {path}");

            // Clean up scene object
            DestroyImmediate(obj);
        }
#endif
    }
}