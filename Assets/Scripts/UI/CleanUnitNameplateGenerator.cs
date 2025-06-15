// Assets/Scripts/UI/CleanUnitNameplateGenerator.cs
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
    /// Editor utility to generate a clean, XCOM-style unit nameplate prefab.
    /// </summary>
    public static class CleanUnitNameplateGenerator
    {
#if UNITY_EDITOR
        // Modern color scheme
        private static readonly Color BACKGROUND_COLOR = new Color(0.1f, 0.1f, 0.12f, 0.75f);
        private static readonly Color BORDER_COLOR = new Color(0.3f, 0.3f, 0.35f, 0.5f);
        private static readonly Color HEALTH_COLOR = new Color(0.2f, 0.85f, 0.3f);
        private static readonly Color AP_COLOR = new Color(0.2f, 0.65f, 1f);
        private static readonly Color TEXT_COLOR = new Color(0.95f, 0.95f, 0.95f);

        [MenuItem("Dark Protocol/Create UI Prefabs/Clean Unit Nameplate")]
        public static void CreateCleanNameplatePrefab()
        {
            // --- Root GameObject with Billboard Component ---
            GameObject root = new GameObject("CleanNameplatePrefab");

            // --- World-space Canvas ---
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            Camera sceneCam = SceneView.lastActiveSceneView?.camera ?? Camera.main;
            canvas.worldCamera = sceneCam;
            canvas.sortingOrder = 50;

            // Get RectTransform and configure
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(180, 40); // Smaller, more compact
            rootRt.pivot = new Vector2(0.5f, 0f); // Pivot at bottom center

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12; // Higher DPI for sharper text

            root.AddComponent<GraphicRaycaster>();

            // Set to a reasonable world scale - slightly larger for better visibility
            root.transform.localScale = Vector3.one * 0.01f;

            // --- Main Container with Rounded Corners ---
            GameObject container = new GameObject("Container");
            container.transform.SetParent(root.transform, false);
            RectTransform containerRt = container.AddComponent<RectTransform>();
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.offsetMin = Vector2.zero;
            containerRt.offsetMax = Vector2.zero;

            // --- Background with Border ---
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(container.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = BACKGROUND_COLOR;
            bgImg.raycastTarget = false;

            // Use a rounded rectangle sprite if available
            string[] guids = AssetDatabase.FindAssets("t:Sprite RoundedRect");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite roundedRect = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (roundedRect != null)
                {
                    bgImg.sprite = roundedRect;
                    bgImg.type = Image.Type.Sliced;
                    bgImg.pixelsPerUnitMultiplier = 3f;
                }
            }

            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // --- Border ---
            GameObject border = new GameObject("Border");
            border.transform.SetParent(container.transform, false);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = BORDER_COLOR;
            borderImg.raycastTarget = false;

            // Use same rounded rectangle sprite for border
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite roundedRect = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (roundedRect != null)
                {
                    borderImg.sprite = roundedRect;
                    borderImg.type = Image.Type.Sliced;
                    borderImg.pixelsPerUnitMultiplier = 3f;
                }
            }

            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1, -1);
            borderRt.offsetMax = new Vector2(1, 1);

            // --- Team Color Indicator ---
            GameObject teamIndicator = new GameObject("TeamIndicator");
            teamIndicator.transform.SetParent(container.transform, false);
            Image teamImg = teamIndicator.AddComponent<Image>();
            teamImg.color = HEALTH_COLOR; // Default to player color

            RectTransform teamRt = teamIndicator.GetComponent<RectTransform>();
            teamRt.anchorMin = new Vector2(0, 0);
            teamRt.anchorMax = new Vector2(0.02f, 1);
            teamRt.offsetMin = Vector2.zero;
            teamRt.offsetMax = Vector2.zero;

            // --- Name Text with Shadow ---
            GameObject nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(container.transform, false);
            TextMeshProUGUI nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
            nameTxt.text = "UNIT NAME";
            nameTxt.fontSize = 15;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Left;

            // Fix: Use textWrappingMode instead of enableWordWrapping
            nameTxt.textWrappingMode = TextWrappingModes.NoWrap;
            nameTxt.overflowMode = TextOverflowModes.Ellipsis;

            // Add shadow effect
            Shadow shadow = nameGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(1, -1);

            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.04f, 0.55f);
            nameRt.anchorMax = new Vector2(0.96f, 0.95f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            // --- Health Bar - Cleaner, Slimmer Design ---
            GameObject healthBarContainer = new GameObject("HealthBarContainer");
            healthBarContainer.transform.SetParent(container.transform, false);
            RectTransform healthBarContainerRt = healthBarContainer.AddComponent<RectTransform>();
            healthBarContainerRt.anchorMin = new Vector2(0.04f, 0.3f);
            healthBarContainerRt.anchorMax = new Vector2(0.96f, 0.45f);
            healthBarContainerRt.offsetMin = Vector2.zero;
            healthBarContainerRt.offsetMax = Vector2.zero;

            // Health background
            GameObject healthBg = new GameObject("Background");
            healthBg.transform.SetParent(healthBarContainer.transform, false);
            Image healthBgImg = healthBg.AddComponent<Image>();
            healthBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            RectTransform healthBgRt = healthBg.GetComponent<RectTransform>();
            healthBgRt.anchorMin = Vector2.zero;
            healthBgRt.anchorMax = Vector2.one;
            healthBgRt.offsetMin = Vector2.zero;
            healthBgRt.offsetMax = Vector2.zero;

            // Health fill
            GameObject healthFill = new GameObject("Fill");
            healthFill.transform.SetParent(healthBg.transform, false);
            Image healthFillImg = healthFill.AddComponent<Image>();
            healthFillImg.color = HEALTH_COLOR;
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            healthFillImg.fillAmount = 1f;

            RectTransform healthFillRt = healthFill.GetComponent<RectTransform>();
            healthFillRt.anchorMin = Vector2.zero;
            healthFillRt.anchorMax = Vector2.one;
            healthFillRt.offsetMin = Vector2.zero;
            healthFillRt.offsetMax = Vector2.zero;

            // --- AP Bar - Only for Player Units ---
            GameObject apBarContainer = new GameObject("APBarContainer");
            apBarContainer.transform.SetParent(container.transform, false);
            RectTransform apBarContainerRt = apBarContainer.AddComponent<RectTransform>();
            apBarContainerRt.anchorMin = new Vector2(0.04f, 0.1f);
            apBarContainerRt.anchorMax = new Vector2(0.96f, 0.25f);
            apBarContainerRt.offsetMin = Vector2.zero;
            apBarContainerRt.offsetMax = Vector2.zero;

            // AP background
            GameObject apBg = new GameObject("Background");
            apBg.transform.SetParent(apBarContainer.transform, false);
            Image apBgImg = apBg.AddComponent<Image>();
            apBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            RectTransform apBgRt = apBg.GetComponent<RectTransform>();
            apBgRt.anchorMin = Vector2.zero;
            apBgRt.anchorMax = Vector2.one;
            apBgRt.offsetMin = Vector2.zero;
            apBgRt.offsetMax = Vector2.zero;

            // AP fill
            GameObject apFill = new GameObject("Fill");
            apFill.transform.SetParent(apBg.transform, false);
            Image apFillImg = apFill.AddComponent<Image>();
            apFillImg.color = AP_COLOR;
            apFillImg.type = Image.Type.Filled;
            apFillImg.fillMethod = Image.FillMethod.Horizontal;
            apFillImg.fillAmount = 1f;

            RectTransform apFillRt = apFill.GetComponent<RectTransform>();
            apFillRt.anchorMin = Vector2.zero;
            apFillRt.anchorMax = Vector2.one;
            apFillRt.offsetMin = Vector2.zero;
            apFillRt.offsetMax = Vector2.zero;

            // --- Status Icons Container ---
            GameObject statusContainer = new GameObject("StatusIconContainer");
            statusContainer.transform.SetParent(container.transform, false);

            HorizontalLayoutGroup statusLayout = statusContainer.AddComponent<HorizontalLayoutGroup>();
            statusLayout.spacing = 2f;
            statusLayout.childAlignment = TextAnchor.MiddleRight;
            statusLayout.childControlWidth = false;
            statusLayout.childControlHeight = false;
            statusLayout.childForceExpandWidth = false;
            statusLayout.childForceExpandHeight = false;

            RectTransform statusRt = statusContainer.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0, -0.5f);
            statusRt.anchorMax = new Vector2(1, -0.1f);
            statusRt.offsetMin = new Vector2(0, 0);
            statusRt.offsetMax = new Vector2(0, 0);

            // --- Attach UnitNameplate Script ---
            UnitNameplate plateScript = root.AddComponent<UnitNameplate>();
            plateScript.nameplateCanvas = canvas;
            plateScript.canvasRect = rootRt;
            plateScript.nameText = nameTxt;
            plateScript.healthBar = healthFillImg;
            plateScript.healthBarBackground = healthBgImg;
            plateScript.apBar = apFillImg;
            plateScript.apContainer = apBarContainer;
            plateScript.statusIconContainer = statusContainer.transform;

            // Configure display settings
            plateScript.heightOffset = 1.2f; // Lower height offset to position closer to unit
            plateScript.scaleWithDistance = true;
            plateScript.minScale = 0.8f;
            plateScript.maxScale = 1.2f;
            plateScript.scaleDistance = 20f;
            plateScript.faceCamera = true;
            plateScript.hideWhenOffscreen = true;

            // Team colors
            plateScript.playerHealthColor = HEALTH_COLOR;
            plateScript.enemyHealthColor = new Color(0.9f, 0.2f, 0.2f);
            plateScript.neutralHealthColor = new Color(0.9f, 0.9f, 0.2f);

            // Animation settings
            plateScript.damageFlashDuration = 0.25f;
            plateScript.healFlashDuration = 0.25f;

            // --- Create directories if needed ---
            const string rootDir = "Assets/Prefabs";
            const string uiDir = rootDir + "/UI";
            if (!AssetDatabase.IsValidFolder(rootDir))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(uiDir))
                AssetDatabase.CreateFolder(rootDir, "UI");

            // --- Save Prefab ---
            string prefabPath = uiDir + "/CleanNameplatePrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("✅ Created clean nameplate prefab at: " + prefabPath);

            Object.DestroyImmediate(root);
        }
#endif
    }
}