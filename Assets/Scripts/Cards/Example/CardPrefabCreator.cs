#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.UI;

namespace DarkProtocol.Editor
{
    /// <summary>
    /// Editor script to create polished card and toolbar prefabs
    /// Menu: Dark Protocol > Create Card System Prefabs
    /// </summary>
    public class CardPrefabCreator : EditorWindow
    {
        [MenuItem("Dark Protocol/Create Card System Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<CardPrefabCreator>("Card Prefab Creator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Card System Prefab Creator", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Enhanced Card Prefab", GUILayout.Height(40)))
            {
                CreateEnhancedCardPrefab();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Card Toolbar Prefab", GUILayout.Height(40)))
            {
                CreateCardToolbarPrefab();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Both Prefabs", GUILayout.Height(40)))
            {
                CreateEnhancedCardPrefab();
                CreateCardToolbarPrefab();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This will create modern, gameplay-ready prefabs for your card system.", MessageType.Info);
        }

        private static void CreateEnhancedCardPrefab()
        {
            // Create main card object
            GameObject card = new GameObject("EnhancedCardUI");
            card.AddComponent<RectTransform>();
            
            // Add the enhanced card UI component
            EnhancedCardUI cardUI = card.AddComponent<EnhancedCardUI>();
            
            // Set up rect transform
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(200, 280);
            
            // Add canvas group for fade effects
            CanvasGroup canvasGroup = card.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(card.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            bgImage.raycastTarget = true;
            
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Create frame/border
            GameObject frame = new GameObject("Frame");
            frame.transform.SetParent(card.transform, false);
            Image frameImage = frame.AddComponent<Image>();
            frameImage.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            frameImage.raycastTarget = false;
            
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(-3, -3);
            frameRect.offsetMax = new Vector2(3, 3);
            
            // Create inner frame to make border effect
            GameObject innerFrame = new GameObject("InnerFrame");
            innerFrame.transform.SetParent(frame.transform, false);
            Image innerFrameImage = innerFrame.AddComponent<Image>();
            innerFrameImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            innerFrameImage.raycastTarget = false;
            
            RectTransform innerFrameRect = innerFrame.GetComponent<RectTransform>();
            innerFrameRect.anchorMin = Vector2.zero;
            innerFrameRect.anchorMax = Vector2.one;
            innerFrameRect.offsetMin = new Vector2(3, 3);
            innerFrameRect.offsetMax = new Vector2(-3, -3);
            
            // Create card art area
            GameObject artArea = new GameObject("ArtArea");
            artArea.transform.SetParent(card.transform, false);
            Image artImage = artArea.AddComponent<Image>();
            artImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            artImage.raycastTarget = false;
            
            RectTransform artRect = artArea.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.1f, 0.6f);
            artRect.anchorMax = new Vector2(0.9f, 0.9f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            
            // Create card icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(artArea.transform, false);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.2f, 0.2f);
            iconRect.anchorMax = new Vector2(0.8f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            // Create name text
            GameObject nameObj = CreateText(card.transform, "NameText", "Card Name", 16, TextAlignmentOptions.Center);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.1f, 0.5f);
            nameRect.anchorMax = new Vector2(0.9f, 0.6f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            nameObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            
            // Create description text
            GameObject descObj = CreateText(card.transform, "DescriptionText", "Card description text goes here.", 12, TextAlignmentOptions.Top);
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.1f, 0.25f);
            descRect.anchorMax = new Vector2(0.9f, 0.5f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;
            
            // Create stats area
            GameObject statsArea = new GameObject("StatsArea");
            statsArea.transform.SetParent(card.transform, false);
            RectTransform statsRect = statsArea.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.1f, 0.1f);
            statsRect.anchorMax = new Vector2(0.9f, 0.25f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            
            HorizontalLayoutGroup statsLayout = statsArea.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 10;
            statsLayout.childAlignment = TextAnchor.MiddleCenter;
            statsLayout.childControlHeight = false;
            statsLayout.childControlWidth = false;
            statsLayout.childForceExpandHeight = false;
            statsLayout.childForceExpandWidth = true;
            
            // Damage stat
            GameObject damageObj = CreateStatBadge(statsArea.transform, "DamageText", "5", new Color(0.8f, 0.2f, 0.2f));
            
            // Range stat
            GameObject rangeObj = CreateStatBadge(statsArea.transform, "RangeText", "3", new Color(0.2f, 0.8f, 0.2f));
            
            // Create AP cost badge
            GameObject apBadge = new GameObject("APCostBadge");
            apBadge.transform.SetParent(card.transform, false);
            Image apBadgeImage = apBadge.AddComponent<Image>();
            apBadgeImage.color = new Color(0.2f, 0.7f, 1f, 0.9f);
            apBadgeImage.raycastTarget = false;
            
            RectTransform apBadgeRect = apBadge.GetComponent<RectTransform>();
            apBadgeRect.anchorMin = new Vector2(0.8f, 0.85f);
            apBadgeRect.anchorMax = new Vector2(1.1f, 1.05f);
            apBadgeRect.offsetMin = Vector2.zero;
            apBadgeRect.offsetMax = Vector2.zero;
            
            GameObject apText = CreateText(apBadge.transform, "APCostText", "2", 18, TextAlignmentOptions.Center);
            RectTransform apTextRect = apText.GetComponent<RectTransform>();
            apTextRect.anchorMin = Vector2.zero;
            apTextRect.anchorMax = Vector2.one;
            apTextRect.offsetMin = Vector2.zero;
            apTextRect.offsetMax = Vector2.zero;
            apText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            apText.GetComponent<TextMeshProUGUI>().color = Color.white;
            
            // Create unavailable overlay
            GameObject overlay = new GameObject("UnavailableOverlay");
            overlay.transform.SetParent(card.transform, false);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            overlayImage.raycastTarget = false;
            overlay.SetActive(false);
            
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            
            // Create selected glow
            GameObject glow = new GameObject("SelectedGlow");
            glow.transform.SetParent(card.transform, false);
            Image glowImage = glow.AddComponent<Image>();
            glowImage.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            glowImage.raycastTarget = false;
            glow.SetActive(false);
            
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-5, -5);
            glowRect.offsetMax = new Vector2(5, 5);
            
            // Create tags container
            GameObject tagsContainer = new GameObject("TagsContainer");
            tagsContainer.transform.SetParent(card.transform, false);
            RectTransform tagsRect = tagsContainer.AddComponent<RectTransform>();
            tagsRect.anchorMin = new Vector2(0.05f, 0.02f);
            tagsRect.anchorMax = new Vector2(0.95f, 0.1f);
            tagsRect.offsetMin = Vector2.zero;
            tagsRect.offsetMax = Vector2.zero;
            
            HorizontalLayoutGroup tagsLayout = tagsContainer.AddComponent<HorizontalLayoutGroup>();
            tagsLayout.spacing = 5;
            tagsLayout.childAlignment = TextAnchor.MiddleLeft;
            
            // Link all components to the card UI
            var cardUIType = typeof(EnhancedCardUI);
            var fields = cardUIType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                switch (field.Name)
                {
                    case "cardBackground":
                        field.SetValue(cardUI, bgImage);
                        break;
                    case "cardFrame":
                        field.SetValue(cardUI, frameImage);
                        break;
                    case "cardIcon":
                        field.SetValue(cardUI, iconImage);
                        break;
                    case "cardArt":
                        field.SetValue(cardUI, artImage);
                        break;
                    case "cardNameText":
                        field.SetValue(cardUI, nameObj.GetComponent<TextMeshProUGUI>());
                        break;
                    case "cardDescriptionText":
                        field.SetValue(cardUI, descObj.GetComponent<TextMeshProUGUI>());
                        break;
                    case "apCostText":
                        field.SetValue(cardUI, apText.GetComponent<TextMeshProUGUI>());
                        break;
                    case "damageText":
                        field.SetValue(cardUI, damageObj.GetComponentInChildren<TextMeshProUGUI>());
                        break;
                    case "rangeText":
                        field.SetValue(cardUI, rangeObj.GetComponentInChildren<TextMeshProUGUI>());
                        break;
                    case "apCostBadge":
                        field.SetValue(cardUI, apBadge);
                        break;
                    case "unavailableOverlay":
                        field.SetValue(cardUI, overlay);
                        break;
                    case "selectedGlow":
                        field.SetValue(cardUI, glow);
                        break;
                    case "tagsContainer":
                        field.SetValue(cardUI, tagsContainer.transform);
                        break;
                }
            }
            
            SavePrefab(card, "EnhancedCardUI");
        }

        private static void CreateCardToolbarPrefab()
        {
            // Create main toolbar object
            GameObject toolbar = new GameObject("CardToolbar");
            RectTransform toolbarRect = toolbar.AddComponent<RectTransform>();
            toolbarRect.sizeDelta = new Vector2(1200, 300);
            
            // Add the toolbar component
            CardToolbar cardToolbar = toolbar.AddComponent<CardToolbar>();
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toolbar.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            bgImage.raycastTarget = false;
            
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Create top border
            GameObject border = new GameObject("TopBorder");
            border.transform.SetParent(toolbar.transform, false);
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            borderImage.raycastTarget = false;
            
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0, 0.95f);
            borderRect.anchorMax = new Vector2(1, 1);
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            
            // Create card container
            GameObject cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(toolbar.transform, false);
            RectTransform cardContainerRect = cardContainer.AddComponent<RectTransform>();
            cardContainerRect.anchorMin = new Vector2(0.05f, 0.15f);
            cardContainerRect.anchorMax = new Vector2(0.95f, 0.9f);
            cardContainerRect.offsetMin = Vector2.zero;
            cardContainerRect.offsetMax = Vector2.zero;
            
            // Create info panel
            GameObject infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(toolbar.transform, false);
            RectTransform infoPanelRect = infoPanel.AddComponent<RectTransform>();
            infoPanelRect.anchorMin = new Vector2(0, 0);
            infoPanelRect.anchorMax = new Vector2(1, 0.15f);
            infoPanelRect.offsetMin = Vector2.zero;
            infoPanelRect.offsetMax = Vector2.zero;
            
            HorizontalLayoutGroup infoLayout = infoPanel.AddComponent<HorizontalLayoutGroup>();
            infoLayout.spacing = 20;
            infoLayout.padding = new RectOffset(20, 20, 5, 5);
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            // Card count display
            GameObject cardCountObj = CreateText(infoPanel.transform, "CardCount", "Cards: 0", 14, TextAlignmentOptions.Left);
            cardCountObj.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.8f, 0.8f);
            
            // AP display
            GameObject apContainer = new GameObject("APContainer");
            apContainer.transform.SetParent(infoPanel.transform, false);
            
            HorizontalLayoutGroup apLayout = apContainer.AddComponent<HorizontalLayoutGroup>();
            apLayout.spacing = 10;
            apLayout.childAlignment = TextAnchor.MiddleLeft;
            
            GameObject apLabel = CreateText(apContainer.transform, "APLabel", "AP:", 14, TextAlignmentOptions.Left);
            apLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.8f, 0.8f);
            
            GameObject apSliderObj = new GameObject("APSlider");
            apSliderObj.transform.SetParent(apContainer.transform, false);
            Slider apSlider = apSliderObj.AddComponent<Slider>();
            apSlider.minValue = 0;
            apSlider.maxValue = 3;
            apSlider.value = 3;
            apSlider.interactable = false;
            
            RectTransform apSliderRect = apSlider.GetComponent<RectTransform>();
            apSliderRect.sizeDelta = new Vector2(100, 20);
            
            // Create slider background
            GameObject sliderBg = new GameObject("Background");
            sliderBg.transform.SetParent(apSliderObj.transform, false);
            Image sliderBgImage = sliderBg.AddComponent<Image>();
            sliderBgImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            
            RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
            sliderBgRect.anchorMin = Vector2.zero;
            sliderBgRect.anchorMax = Vector2.one;
            sliderBgRect.offsetMin = Vector2.zero;
            sliderBgRect.offsetMax = Vector2.zero;
            
            // Create slider fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(apSliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // Create slider fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.7f, 1f, 0.9f);
            
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            apSlider.fillRect = fillImage.rectTransform;
            
            GameObject apText = CreateText(apContainer.transform, "APText", "3/3", 14, TextAlignmentOptions.Left);
            apText.GetComponent<TextMeshProUGUI>().color = new Color(0.2f, 0.7f, 1f);
            
            // Toggle button
            GameObject toggleBtn = CreateButton(infoPanel.transform, "ToggleButton", "Hide", 12);
            
            // End turn button
            GameObject endTurnBtn = CreateButton(infoPanel.transform, "EndTurnButton", "End Turn", 14);
            endTurnBtn.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.2f, 0.8f);
            
            // Empty hand message
            GameObject emptyMsg = CreateText(cardContainer.transform, "EmptyHandMessage", "No cards in hand", 16, TextAlignmentOptions.Center);
            emptyMsg.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.6f, 0.6f);
            RectTransform emptyRect = emptyMsg.GetComponent<RectTransform>();
            emptyRect.anchorMin = Vector2.zero;
            emptyRect.anchorMax = Vector2.one;
            emptyRect.offsetMin = Vector2.zero;
            emptyRect.offsetMax = Vector2.zero;
            emptyMsg.SetActive(false);
            
            // Link components using reflection
            var toolbarType = typeof(CardToolbar);
            var fields = toolbarType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                switch (field.Name)
                {
                    case "cardContainer":
                        field.SetValue(cardToolbar, cardContainerRect);
                        break;
                    case "toggleButton":
                        field.SetValue(cardToolbar, toggleBtn.GetComponent<Button>());
                        break;
                    case "cardCountText":
                        field.SetValue(cardToolbar, cardCountObj.GetComponent<TextMeshProUGUI>());
                        break;
                    case "apDisplayText":
                        field.SetValue(cardToolbar, apText.GetComponent<TextMeshProUGUI>());
                        break;
                    case "apSlider":
                        field.SetValue(cardToolbar, apSlider);
                        break;
                    case "endTurnButton":
                        field.SetValue(cardToolbar, endTurnBtn.GetComponent<Button>());
                        break;
                    case "emptyHandMessage":
                        field.SetValue(cardToolbar, emptyMsg);
                        break;
                }
            }
            
            SavePrefab(toolbar, "CardToolbar");
        }

        private static GameObject CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.9f, 0.9f, 0.9f);
            tmp.alignment = alignment;
            tmp.font = GetDefaultFont();
            
            return textObj;
        }

        private static GameObject CreateStatBadge(Transform parent, string name, string value, Color color)
        {
            GameObject badge = new GameObject(name);
            badge.transform.SetParent(parent, false);
            
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.sizeDelta = new Vector2(30, 25);
            
            Image badgeImage = badge.AddComponent<Image>();
            badgeImage.color = color;
            badgeImage.raycastTarget = false;
            
            GameObject textObj = CreateText(badge.transform, "Text", value, 12, TextAlignmentOptions.Center);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            textObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            textObj.GetComponent<TextMeshProUGUI>().color = Color.white;
            
            return badge;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, float fontSize)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent, false);
            
            RectTransform btnRect = button.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(80, 30);
            
            Image btnImage = button.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            
            Button btn = button.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            
            GameObject textObj = CreateText(button.transform, "Text", text, fontSize, TextAlignmentOptions.Center);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            textObj.GetComponent<TextMeshProUGUI>().color = Color.white;
            
            return button;
        }

        private static TMP_FontAsset GetDefaultFont()
        {
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts.Length > 0)
            {
                foreach (var font in fonts)
                {
                    if (font.name.Contains("SDF"))
                        return font;
                }
                return fonts[0];
            }
            return null;
        }

        private static void SavePrefab(GameObject obj, string prefabName)
        {
            string path = $"Assets/Prefabs/UI/{prefabName}.prefab";
            
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
    }
}
#endif