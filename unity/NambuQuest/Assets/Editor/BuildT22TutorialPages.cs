#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// T2-2: TutorialScene を3ページ構成（同一シーン内パネル切替）へ再構築する一時スクリプト。
/// </summary>
public static class BuildT22TutorialPages
{
    private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
    private const string FontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/HiraginoSans JP SDF.asset";
    private const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

    private static readonly Color DeepSea = new Color(0.04f, 0.12f, 0.24f, 1f);
    private static readonly Color CreamPanel = new Color(0.95f, 0.90f, 0.78f, 1f);
    private static readonly Color RedBorder = new Color(0.72f, 0.23f, 0.18f, 1f);
    private static readonly Color TextOnCream = new Color(0.24f, 0.16f, 0.11f, 1f);

    private const string Page1Heading = "南部もぐりの仕事を体験しよう";
    private const string Page1Body = "限られた空気の中で探索し、\n安全に船へ帰るゲームです。";
    private const string Page2Heading = "空気を残して帰ろう";
    private const string Page2Body = "空気は100から始まります。\n安全に帰るため、15は残しましょう。";
    private const string Page3Heading = "記録を選んで持ち帰ろう";
    private const string Page3Body = "探索で見つけた記録のうち、\n持ち帰れるのは最大2件です。";

    public static void Run()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            throw new System.Exception("日本語 Font Asset が見つかりません: " + FontAssetPath);
        }

        EnsureFontCharacters(font);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateGlobalLight2D();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);

        GameObject page1 = CreatePage(
            canvas.transform, "Page1", Page1Heading, Page1Body, "次へ", font, out Button page1Next);
        GameObject page2 = CreatePage(
            canvas.transform, "Page2", Page2Heading, Page2Body, "次へ", font, out Button page2Next);
        GameObject page3 = CreatePage(
            canvas.transform, "Page3", Page3Heading, Page3Body, "わかった", font, out Button understood);

        page2.SetActive(false);
        page3.SetActive(false);

        TutorialController controller = canvas.gameObject.AddComponent<TutorialController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("page1").objectReferenceValue = page1;
        so.FindProperty("page2").objectReferenceValue = page2;
        so.FindProperty("page3").objectReferenceValue = page3;
        so.FindProperty("page1NextButton").objectReferenceValue = page1Next;
        so.FindProperty("page2NextButton").objectReferenceValue = page2Next;
        so.FindProperty("understoodButton").objectReferenceValue = understood;
        so.ApplyModifiedPropertiesWithoutUndo();

        CreateEventSystem();

        if (!EditorSceneManager.SaveScene(scene, TutorialScenePath))
        {
            throw new System.Exception("TutorialScene の保存に失敗しました。");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("BuildT22TutorialPages: 完了");
    }

    private static void EnsureFontCharacters(TMP_FontAsset font)
    {
        string extra =
            Page1Heading + Page1Body +
            Page2Heading + Page2Body +
            Page3Heading + Page3Body +
            "次へわかった0123456789";

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.TryAddCharacters(extra, out string missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning("BuildT22TutorialPages: 追加できなかった文字: " + missing);
        }

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
    }

    private static void CreateCamera()
    {
        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = DeepSea;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraGo.AddComponent<AudioListener>();
    }

    private static void CreateGlobalLight2D()
    {
        System.Type lightType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (lightType == null)
        {
            return;
        }

        GameObject lightGo = new GameObject("Global Light 2D");
        Component light = lightGo.AddComponent(lightType);
        var lightTypeProp = lightType.GetProperty("lightType");
        if (lightTypeProp == null)
        {
            return;
        }

        foreach (object value in System.Enum.GetValues(lightTypeProp.PropertyType))
        {
            if (value.ToString() == "Global")
            {
                lightTypeProp.SetValue(light, value);
                break;
            }
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasGo = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject(
            "Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(parent, false);
        bg.transform.SetAsFirstSibling();

        RectTransform rect = bg.GetComponent<RectTransform>();
        StretchFull(rect);

        Image image = bg.GetComponent<Image>();
        image.color = DeepSea;
        image.raycastTarget = false;
    }

    private static GameObject CreatePage(
        Transform parent,
        string name,
        string heading,
        string body,
        string buttonLabel,
        TMP_FontAsset font,
        out Button button)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        StretchFull(page.GetComponent<RectTransform>());

        // 資料風パネル：赤枠＋クリーム
        GameObject border = new GameObject(
            "PanelBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        border.transform.SetParent(page.transform, false);

        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.anchoredPosition = new Vector2(0f, 40f);
        borderRect.sizeDelta = new Vector2(1240f, 640f);

        Image borderImage = border.GetComponent<Image>();
        borderImage.color = RedBorder;
        borderImage.raycastTarget = false;

        GameObject inner = new GameObject(
            "InnerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inner.transform.SetParent(border.transform, false);

        RectTransform innerRect = inner.GetComponent<RectTransform>();
        StretchFull(innerRect);
        innerRect.offsetMin = new Vector2(8f, 8f);
        innerRect.offsetMax = new Vector2(-8f, -8f);

        Image innerImage = inner.GetComponent<Image>();
        innerImage.color = CreamPanel;
        innerImage.raycastTarget = false;

        CreateText(
            inner.transform, "Heading", heading,
            new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1100f, 100f),
            52f, font);

        CreateText(
            inner.transform, "Body", body,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1100f, 280f),
            40f, font);

        button = CreateStyledButton(inner.transform, "ActionButton", buttonLabel, font);

        return page;
    }

    private static void CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        TMP_FontAsset font)
    {
        GameObject go = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = font;
        if (font.material != null)
        {
            tmp.fontSharedMaterial = font.material;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = TextOnCream;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private static Button CreateStyledButton(
        Transform parent, string name, string label, TMP_FontAsset font)
    {
        GameObject buttonGo = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 40f);
        buttonRect.sizeDelta = new Vector2(360f, 100f);

        Image borderImage = buttonGo.GetComponent<Image>();
        borderImage.color = RedBorder;
        borderImage.raycastTarget = true;

        GameObject inner = new GameObject(
            "InnerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inner.transform.SetParent(buttonGo.transform, false);

        RectTransform innerRect = inner.GetComponent<RectTransform>();
        StretchFull(innerRect);
        innerRect.offsetMin = new Vector2(8f, 8f);
        innerRect.offsetMax = new Vector2(-8f, -8f);

        Image innerImage = inner.GetComponent<Image>();
        innerImage.color = new Color(0.99f, 0.96f, 0.88f, 1f);
        innerImage.raycastTarget = false;

        GameObject labelGo = new GameObject(
            "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(inner.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());

        TextMeshProUGUI labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.font = font;
        if (font.material != null)
        {
            labelTmp.fontSharedMaterial = font.material;
        }

        labelTmp.text = label;
        labelTmp.fontSize = 40f;
        labelTmp.color = TextOnCream;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = borderImage;
        return button;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        InputSystemUIInputModule module = eventSystemGo.AddComponent<InputSystemUIInputModule>();

        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (asset == null)
        {
            module.AssignDefaultActions();
            return;
        }

        module.actionsAsset = asset;
        module.point = InputActionReference.Create(asset.FindAction("UI/Point"));
        module.leftClick = InputActionReference.Create(asset.FindAction("UI/Click"));
        module.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick"));
        module.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick"));
        module.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel"));
        module.move = InputActionReference.Create(asset.FindAction("UI/Navigate"));
        module.submit = InputActionReference.Create(asset.FindAction("UI/Submit"));
        module.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel"));
        module.trackedDevicePosition = InputActionReference.Create(
            asset.FindAction("UI/TrackedDevicePosition"));
        module.trackedDeviceOrientation = InputActionReference.Create(
            asset.FindAction("UI/TrackedDeviceOrientation"));
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
#endif
