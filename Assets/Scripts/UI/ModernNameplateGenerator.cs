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
    /// Modern, minimalist XCOM-inspired unit nameplate generator
    /// Creates clean, professional nameplates with subtle animations and perfect readability
    /// </summary>
    public static class ModernNameplateGenerator
    {
#if UNITY_EDITOR
        // Modern XCOM-inspired color palette - muted but readable
        private static readonly Color BACKGROUND_PRIMARY = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        private static readonly Color BACKGROUND_SECONDARY = new Color(0.12f, 0.12f, 0.18f, 0.75f);
        private static readonly Color ACCENT_BLUE = new Color(0.3f, 0.7f, 1f, 1f);
        private static readonly Color TEXT_PRIMARY = new Color(0.95f, 0.95f, 0.98f, 1f);
        private static readonly Color TEXT_SECONDARY = new Color(0.75f, 0.75f, 0.82f, 1f);

        // Team colors - more sophisticated than basic RGB
        private static readonly Color PLAYER_COLOR = new Color(0.2f, 0.8f, 0.4f, 1f);
        private static readonly Color ENEMY_COLOR = new Color(0.9f, 0.25f, 0.2f, 1f);
        private static readonly Color NEUTRAL_COLOR = new Color(0.85f, 0.75f, 0.3f, 1f);

        [MenuItem("Dark Protocol/Create UI Prefabs/Modern XCOM Nameplate")]
        public static void CreateModernNameplate()
        {
            // === ROOT OBJECT ===
            GameObject nameplate = new GameObject("ModernNameplatePrefab");

            // World-space canvas optimized for performance
            Canvas canvas = nameplate.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // Render above most UI

            RectTransform canvasRect = nameplate.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(120, 32); // Compact dimensions
            canvasRect.pivot = new Vector2(0.5f, 0f); // Bottom-center pivot

            // High-quality scaling for crisp text
            CanvasScaler scaler = nameplate.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.dynamicPixelsPerUnit = 16f;

            // Disable raycast blocking
            GraphicRaycaster raycaster = nameplate.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            // World scale - smaller and less intrusive
            nameplate.transform.localScale = Vector3.one * 0.008f;

            // === MAIN CONTAINER ===
            GameObject container = CreateContainer(nameplate.transform);

            // === BACKGROUND SYSTEM ===
            CreateModernBackground(container.transform);

            // === CONTENT LAYOUT ===
            GameObject content = CreateContentArea(container.transform);

            // === NAME SECTION ===
            TextMeshProUGUI nameText = CreateNameText(content.transform);

            // === HEALTH SYSTEM ===
            var (healthContainer, healthFill, healthBg) = CreateModernHealthBar(content.transform);

            // === AP SYSTEM ===
            var (apContainer, apFill, apBackground) = CreateAPSegments(content.transform);

            // === STATUS ICONS ===
            Transform statusContainer = CreateStatusIconArea(container.transform);

            // === TEAM INDICATOR ===
            Image teamIndicator = CreateTeamIndicator(container.transform);

            // === ATTACH COMPONENT ===
            UnitNameplate nameplateComponent = nameplate.AddComponent<UnitNameplate>();
            ConfigureNameplateComponent(nameplateComponent, canvas, canvasRect, nameText,
                healthFill, healthBg, apContainer, statusContainer, teamIndicator);

            // === SAVE PREFAB ===
            SavePrefab(nameplate, "ModernNameplatePrefab");

            Debug.Log("✅ Created Modern XCOM Nameplate - Sleek, minimal, and performance-optimized");
        }

        private static GameObject CreateContainer(Transform parent)
        {
            GameObject container = new GameObject("Container");
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return container;
        }

        private static void CreateModernBackground(Transform parent)
        {
            // Primary background - main panel
            GameObject primaryBg = new GameObject("BackgroundPrimary");
            primaryBg.transform.SetParent(parent, false);

            Image primaryImg = primaryBg.AddComponent<Image>();
            primaryImg.color = BACKGROUND_PRIMARY;
            primaryImg.raycastTarget = false;

            RectTransform primaryRect = primaryBg.GetComponent<RectTransform>();
            primaryRect.anchorMin = Vector2.zero;
            primaryRect.anchorMax = Vector2.one;
            primaryRect.offsetMin = Vector2.zero;
            primaryRect.offsetMax = Vector2.zero;

            // Subtle gradient overlay for depth
            GameObject overlay = new GameObject("GradientOverlay");
            overlay.transform.SetParent(primaryBg.transform, false);

            Image overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(1f, 1f, 1f, 0.02f);
            overlayImg.raycastTarget = false;

            // Create subtle gradient effect
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(0, 0.6f);
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateContentArea(Transform parent)
        {
            GameObject content = new GameObject("ContentArea");
            content.transform.SetParent(parent, false);

            RectTransform rect = content.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6, 3); // Minimal padding
            rect.offsetMax = new Vector2(-6, -3);

            return content;
        }

        private static TextMeshProUGUI CreateNameText(Transform parent)
        {
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(parent, false);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "OPERATIVE";
            nameText.fontSize = 11f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = TEXT_PRIMARY;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // Subtle outline for readability
            Outline outline = nameObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(0.5f, -0.5f);

            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.65f);
            nameRect.anchorMax = new Vector2(1, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            return nameText;
        }

        private static (GameObject container, Image fill, Image background) CreateModernHealthBar(Transform parent)
        {
            GameObject healthContainer = new GameObject("HealthBarContainer");
            healthContainer.transform.SetParent(parent, false);

            RectTransform containerRect = healthContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.35f);
            containerRect.anchorMax = new Vector2(1, 0.55f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Background
            GameObject healthBg = new GameObject("Background");
            healthBg.transform.SetParent(healthContainer.transform, false);

            Image bgImage = healthBg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            bgImage.raycastTarget = false;

            RectTransform bgRect = healthBg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Health fill with smooth edges
            GameObject healthFill = new GameObject("HealthFill");
            healthFill.transform.SetParent(healthBg.transform, false);

            Image fillImage = healthFill.AddComponent<Image>();
            fillImage.color = PLAYER_COLOR;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;

            RectTransform fillRect = healthFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1, 1); // Slight inset for clean look
            fillRect.offsetMax = new Vector2(-1, -1);

            return (healthContainer, fillImage, bgImage);
        }

        private static (GameObject container, Image apFill, Image apBackground) CreateAPSegments(Transform parent)
        {
            GameObject apContainer = new GameObject("APContainer");
            apContainer.transform.SetParent(parent, false);

            RectTransform containerRect = apContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.1f);
            containerRect.anchorMax = new Vector2(1, 0.25f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Create individual AP segments manually for better control
            HorizontalLayoutGroup layout = apContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Create 4 AP segments (max expected AP) - extras will be hidden
            for (int i = 0; i < 4; i++)
            {
                GameObject segment = new GameObject($"APSegment_{i}");
                segment.transform.SetParent(apContainer.transform, false);

                Image segmentImage = segment.AddComponent<Image>();
                segmentImage.color = ACCENT_BLUE;
                segmentImage.raycastTarget = false;

                RectTransform segmentRect = segment.GetComponent<RectTransform>();
                segmentRect.sizeDelta = new Vector2(20, 0); // Fixed width, flexible height

                // Hide extra segments by default
                if (i >= 3) segment.SetActive(false);
            }

            // Return dummy references for compatibility - actual AP display will be handled by UnitNameplate
            return (apContainer, null, null);
        }

        private static Transform CreateStatusIconArea(Transform parent)
        {
            GameObject statusArea = new GameObject("StatusIconArea");
            statusArea.transform.SetParent(parent, false);

            RectTransform statusRect = statusArea.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, -0.4f);
            statusRect.anchorMax = new Vector2(1, -0.05f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = statusArea.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 1f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return statusArea.transform;
        }

        private static Image CreateTeamIndicator(Transform parent)
        {
            GameObject teamIndicator = new GameObject("TeamIndicator");
            teamIndicator.transform.SetParent(parent, false);

            Image teamImage = teamIndicator.AddComponent<Image>();
            teamImage.color = PLAYER_COLOR;
            teamImage.raycastTarget = false;

            RectTransform teamRect = teamIndicator.GetComponent<RectTransform>();
            teamRect.anchorMin = new Vector2(0, 0.25f);
            teamRect.anchorMax = new Vector2(0.015f, 0.75f);
            teamRect.offsetMin = Vector2.zero;
            teamRect.offsetMax = Vector2.zero;

            return teamImage;
        }

        private static void ConfigureNameplateComponent(UnitNameplate component, Canvas canvas,
            RectTransform canvasRect, TextMeshProUGUI nameText, Image healthFill, Image healthBg,
            GameObject apContainer, Transform statusContainer, Image teamIndicator)
        {
            // Basic references
            component.nameplateCanvas = canvas;
            component.canvasRect = canvasRect;
            component.nameText = nameText;
            component.healthBar = healthFill;
            component.healthBarBackground = healthBg;
            component.apContainer = apContainer;
            component.statusIconContainer = statusContainer;

            // Display settings - minimal and unobtrusive
            component.heightOffset = 1.8f;
            component.scaleWithDistance = true;
            component.minScale = 0.7f;
            component.maxScale = 1.0f;
            component.scaleDistance = 25f;
            component.faceCamera = true;
            component.hideWhenOffscreen = true;

            // Modern team colors
            component.playerHealthColor = PLAYER_COLOR;
            component.enemyHealthColor = ENEMY_COLOR;
            component.neutralHealthColor = NEUTRAL_COLOR;

            // Smooth animations
            component.damageFlashDuration = 0.2f;
            component.healFlashDuration = 0.15f;
            component.damageFlashColor = new Color(1f, 0.3f, 0.3f, 0.8f);
            component.healFlashColor = new Color(0.3f, 1f, 0.5f, 0.8f);
        }

        private static void SavePrefab(GameObject prefab, string fileName)
        {
            // Ensure directories exist
            const string rootDir = "Assets/Prefabs";
            const string uiDir = rootDir + "/UI";

            if (!AssetDatabase.IsValidFolder(rootDir))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(uiDir))
                AssetDatabase.CreateFolder(rootDir, "UI");

            // Save prefab
            string prefabPath = $"{uiDir}/{fileName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);

            Debug.Log($"✅ Modern nameplate saved to: {prefabPath}");

            // Clean up scene object
            Object.DestroyImmediate(prefab);

            // Refresh assets
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // === ADDITIONAL UTILITY METHODS ===

        [MenuItem("Dark Protocol/Create UI Prefabs/Status Icon (Modern)")]
        public static void CreateModernStatusIcon()
        {
            GameObject icon = new GameObject("ModernStatusIconPrefab");

            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(16, 16); // Smaller, less intrusive

            // Main icon
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            // Subtle background for contrast
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(icon.transform, false);
            bg.transform.SetAsFirstSibling();

            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.4f);
            bgImage.raycastTarget = false;

            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-2, -2);
            bgRect.offsetMax = new Vector2(2, 2);

            // Stack count
            GameObject stackObj = new GameObject("StackCount");
            stackObj.transform.SetParent(icon.transform, false);

            TextMeshProUGUI stackText = stackObj.AddComponent<TextMeshProUGUI>();
            stackText.text = "";
            stackText.fontSize = 8f;
            stackText.fontStyle = FontStyles.Bold;
            stackText.color = TEXT_PRIMARY;
            stackText.alignment = TextAlignmentOptions.BottomRight;

            Outline stackOutline = stackObj.AddComponent<Outline>();
            stackOutline.effectColor = Color.black;
            stackOutline.effectDistance = new Vector2(0.5f, -0.5f);

            RectTransform stackRect = stackObj.GetComponent<RectTransform>();
            stackRect.anchorMin = new Vector2(0.6f, 0);
            stackRect.anchorMax = new Vector2(1.2f, 0.5f);
            stackRect.offsetMin = Vector2.zero;
            stackRect.offsetMax = Vector2.zero;

            SavePrefab(icon, "ModernStatusIconPrefab");
        }
#endif
    }
}