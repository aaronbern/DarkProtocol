// Assets/Editor/CleanUnitNameplateGenerator.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editor utility to generate a clean, world-space unit nameplate prefab.
/// </summary>
public static class CleanUnitNameplateGenerator
{
#if UNITY_EDITOR
    [MenuItem("Dark Protocol/Create UI Prefabs/Clean Unit Nameplate Prefab")]
    public static void CreateUnitNameplatePrefab()
    {
        // --- Root GameObject & RectTransform ---
        GameObject root = new GameObject("UnitNameplatePrefab");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.sizeDelta = new Vector2(200, 50);

        // --- World-space Canvas ---
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        Camera sceneCam = SceneView.lastActiveSceneView?.camera ?? Camera.main;
        canvas.worldCamera = sceneCam;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        root.AddComponent<GraphicRaycaster>();
        root.transform.localScale = Vector3.one * 0.01f;

        // --- Background Panel ---
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // --- Name Text ---
        GameObject nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(root.transform, false);
        TextMeshProUGUI nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
        nameTxt.text = "Unit Name";
        nameTxt.fontSize = 24;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = Color.white;
        nameTxt.textWrappingMode = TextWrappingModes.NoWrap;
        nameTxt.outlineWidth = 0.08f;
        nameTxt.outlineColor = Color.black;
        var shadow = nameGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(1, -1);
        RectTransform nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0.7f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.offsetMin = Vector2.zero;
        nameRt.offsetMax = Vector2.zero;

        // --- Health Bar ---
        CreateBar(root.transform,
                  "HealthBar",
                  new Color(0.25f, 1f, 0.25f),
                  new Vector2(0.1f, 0.4f),
                  new Vector2(0.9f, 0.6f));

        // --- AP Bar ---
        CreateBar(root.transform,
                  "APBar",
                  new Color(0.2f, 0.6f, 1f),
                  new Vector2(0.1f, 0.1f),
                  new Vector2(0.9f, 0.3f));

        // --- Attach UnitNameplate script and wire up ---
        var plateScript = root.AddComponent<DarkProtocol.UI.UnitNameplate>();
        plateScript.nameplateCanvas = canvas;
        plateScript.nameText = nameTxt;

        var healthFill = root.transform.Find("HealthBar/Background/Fill");
        if (healthFill != null)
            plateScript.healthBar = healthFill.GetComponent<Image>();

        var apFill = root.transform.Find("APBar/Background/Fill");
        if (apFill != null)
            plateScript.apBar = apFill.GetComponent<Image>();

        var apBarObj = root.transform.Find("APBar");
        if (apBarObj != null)
            plateScript.apContainer = apBarObj.gameObject;

        // -- Height offset determines how high above the model this plate sits.
        //    Lower the value to bring it closer to the head.
        plateScript.heightOffset = 1.8f; // was 2.2f, reduce to move closer
        plateScript.minScale = 0.6f;
        plateScript.maxScale = 1.2f;
        plateScript.scaleDistance = 25f;
        plateScript.faceCamera = true;
        plateScript.hideWhenOffscreen = true;

        // --- Ensure folders exist ---
        const string rootDir = "Assets/Prefabs";
        const string uiDir = rootDir + "/UI";
        if (!AssetDatabase.IsValidFolder(rootDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(uiDir))
            AssetDatabase.CreateFolder(rootDir, "UI");

        // --- Save Prefab ---
        string prefabPath = uiDir + "/UnitNameplatePrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Debug.Log("✅ Created UnitNameplatePrefab at: " + prefabPath);

        Object.DestroyImmediate(root);
    }

    private static void CreateBar(Transform parent,
                                  string name,
                                  Color fillColor,
                                  Vector2 anchorMin,
                                  Vector2 anchorMax)
    {
        GameObject barRoot = new GameObject(name);
        barRoot.transform.SetParent(parent, false);

        RectTransform rt = barRoot.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(barRoot.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.8f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
    }
#endif
}
