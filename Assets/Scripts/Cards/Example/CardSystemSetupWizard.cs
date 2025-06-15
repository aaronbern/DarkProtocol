#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DarkProtocol.UI;
using DarkProtocol.Integration;

namespace DarkProtocol.Editor
{
    public class CardSystemSetupWizard : EditorWindow
    {
        [MenuItem("Dark Protocol/Setup Card System Scene")]
        public static void ShowWindow()
        {
            GetWindow<CardSystemSetupWizard>("Card System Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Card System Scene Setup", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Setup Complete Card System", GUILayout.Height(50)))
            {
                SetupCardSystemScene();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will:\n" +
                "• Create UI Canvas if needed\n" +
                "• Add Card Toolbar\n" +
                "• Add Tooltip System\n" +
                "• Add Card System Integrator\n" +
                "• Configure all connections", 
                MessageType.Info);
        }

        private static void SetupCardSystemScene()
        {
            // Find or create canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("UI Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create toolbar
            GameObject toolbarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CardToolbar.prefab");
            if (toolbarPrefab != null)
            {
                GameObject toolbar = PrefabUtility.InstantiatePrefab(toolbarPrefab, canvas.transform) as GameObject;
                
                // Position at bottom of screen
                RectTransform toolbarRect = toolbar.GetComponent<RectTransform>();
                toolbarRect.anchorMin = new Vector2(0, 0);
                toolbarRect.anchorMax = new Vector2(1, 0);
                toolbarRect.pivot = new Vector2(0.5f, 0);
                toolbarRect.anchoredPosition = new Vector2(0, 0);
            }

            // Create tooltip
            GameObject tooltipObj = new GameObject("Card Tooltip");
            tooltipObj.transform.SetParent(canvas.transform, false);
            RectTransform tooltipRect = tooltipObj.AddComponent<RectTransform>();
            EnhancedCardTooltip tooltip = tooltipObj.AddComponent<EnhancedCardTooltip>();
            
            tooltipRect.sizeDelta = new Vector2(300, 400);

            // Create integrator
            GameObject integratorObj = new GameObject("Card System Integrator");
            CardSystemIntegrator integrator = integratorObj.AddComponent<CardSystemIntegrator>();

            Debug.Log("Card System Scene Setup Complete!");
        }
    }
}
#endif