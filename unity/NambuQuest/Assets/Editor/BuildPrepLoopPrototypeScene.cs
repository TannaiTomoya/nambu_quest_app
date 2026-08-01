using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class BuildPrepLoopPrototypeScene
{
    private static readonly Vector2 ReferenceResolution = new Vector2(720, 1280);
    private static readonly Color Navy = new Color32(4, 28, 58, 255);
    private static readonly Color DeepNavy = new Color32(2, 18, 39, 255);
    private static readonly Color PanelBlue = new Color32(15, 58, 102, 255);
    private static readonly Color Cream = new Color32(255, 244, 219, 255);
    private static readonly Color Red = new Color32(211, 72, 52, 255);
    private static readonly Color Cyan = new Color32(83, 184, 218, 255);
    private static readonly Color Ink = new Color32(59, 35, 22, 255);
    private static readonly Color Gold = new Color32(255, 212, 87, 255);
    private static readonly Color PaleBlue = new Color32(198, 220, 246, 255);
    private static readonly Color PixelLine = new Color32(5, 13, 28, 255);
    private const string JapaneseFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/HiraginoSans JP SDF.asset";
    private const string GeneratedJapaneseFontPath = "Assets/Generated/Fonts/NambuQuestJapaneseDynamic.asset";
    private const string GeneratedJapaneseSourceFontPath = "Assets/Generated/Fonts/NambuQuestJapaneseSource.ttf";
    private const string CircleSpritePath = "Assets/Generated/Sprites/NambuQuestCircle.png";
    private const string SpriteDir = "Assets/Generated/Sprites";
    private static TMP_FontAsset japaneseFont;
    private static Sprite circleSprite;

    [MenuItem("Nambu Quest/Build Prep Loop Prototype Scene")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Nambu Quest", "Playを停止してからSceneを生成してください。", "OK");
            return;
        }

        japaneseFont = null;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "PrepLoopPrototype";

        CreateEventSystem();
        CreateCamera();
        Canvas canvas = CreateCanvas();
        PrepLoopPrototypeController controller = CreateController(canvas.transform);

        controller.introPanel = CreatePanel(canvas.transform, "IntroPanel");
        controller.prepPanel = CreatePanel(canvas.transform, "PrepPanel");
        controller.rankPanel = CreatePanel(canvas.transform, "RankPanel");
        controller.locationPanel = CreatePanel(canvas.transform, "LocationPanel");
        controller.divePanel = CreatePanel(canvas.transform, "DivePanel");
        controller.chestPanel = CreatePanel(canvas.transform, "ChestPanel");
        controller.returnPanel = CreatePanel(canvas.transform, "ReturnPanel");
        controller.resultPanel = CreatePanel(canvas.transform, "ResultPanel");

        BuildIntro(controller);
        BuildPrep(controller, canvas.transform);
        BuildRank(controller);
        BuildLocation(controller);
        BuildDive(controller);
        BuildChest(controller);
        BuildReturn(controller);
        BuildResult(controller);
        ApplyJapaneseFontToScene();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PrepLoopPrototype.unity");
        EditorUtility.DisplayDialog("Nambu Quest", "スマホ9:16版 PrepLoopPrototype Scene を生成しました。", "OK");
    }

    private static PrepLoopPrototypeController CreateController(Transform parent)
    {
        GameObject go = new GameObject("PrepLoopPrototypeController");
        go.transform.SetParent(parent, false);
        return go.AddComponent<PrepLoopPrototypeController>();
    }

    private static Camera CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        Camera camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = DeepNavy;
        camera.orthographic = true;
        go.tag = "MainCamera";
        return camera;
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("MobileCanvas_720x1280");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();

        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            go.AddComponent(inputSystemModuleType);
        }
        else
        {
            go.AddComponent<StandaloneInputModule>();
        }
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        Stretch(rect);
        Image image = go.AddComponent<Image>();
        image.color = Navy;
        AddSeaDetails(go.transform);
        return go;
    }

    private static void BuildIntro(PrepLoopPrototypeController c)
    {
        c.introTitleText = CreateText(c.introPanel.transform, "南部もぐり", 50, Color.white, new Vector2(0, 205), new Vector2(620, 80));
        c.introTitleText.fontStyle = FontStyles.Bold;
        c.introBodyText = CreateText(c.introPanel.transform, "", 28, PaleBlue, new Vector2(0, 55), new Vector2(620, 275));
        c.startButton = CreateButton(c.introPanel.transform, "装備準備を始める", 36, new Vector2(0, -515), new Vector2(620, 112), Cream, Red, Ink);
    }

    private static void BuildPrep(PrepLoopPrototypeController c, Transform canvas)
    {
        c.prepTitleText = CreateText(c.prepPanel.transform, "装備準備", 46, Color.white, new Vector2(0, 552), new Vector2(620, 70));
        c.prepTitleText.fontStyle = FontStyles.Bold;
        c.timerText = CreateText(c.prepPanel.transform, "8.0", 86, Color.white, new Vector2(0, 455), new Vector2(260, 100));
        c.timerText.fontStyle = FontStyles.Bold;

        c.prepStepButtons = new Button[3];
        c.prepStepLabels = new TMP_Text[3];
        c.prepStepButtons[0] = CreateButton(c.prepPanel.transform, "1. 潜水服を着る", 32, new Vector2(0, 328), new Vector2(620, 90), Cream, Red, Ink, out c.prepStepLabels[0]);
        c.prepStepButtons[1] = CreateButton(c.prepPanel.transform, "2. 胸当てと錘を調整する", 28, new Vector2(0, 220), new Vector2(620, 90), PanelBlue, Cyan, PaleBlue, out c.prepStepLabels[1]);
        c.prepStepButtons[2] = CreateButton(c.prepPanel.transform, "3. ヘルメットと送気を確認する", 26, new Vector2(0, 112), new Vector2(620, 90), PanelBlue, Cyan, PaleBlue, out c.prepStepLabels[2]);

        c.prepGuideText = CreateText(c.prepPanel.transform, "", 28, PaleBlue, new Vector2(0, -250), new Vector2(620, 60));
        c.prepGauge = CreateSlider(c.prepPanel.transform, new Vector2(0, -310), new Vector2(620, 30));
        c.mashButton = CreateButton(c.prepPanel.transform, "準備！", 48, new Vector2(0, -500), new Vector2(252, 252), Cream, Red, Ink);

        c.diverRoot = CreateRect("DiverRoot", canvas, new Vector2(0, -70), new Vector2(390, 360));
        c.diverStages = new GameObject[4];
        c.diverStageLabels = new TMP_Text[4];
        c.diverStages[0] = CreateDiverStage(c.diverRoot.transform, "装備なし", new Color32(188, 123, 80, 255), 0, out c.diverStageLabels[0]);
        c.diverStages[1] = CreateDiverStage(c.diverRoot.transform, "潜水服", new Color32(235, 226, 199, 255), 1, out c.diverStageLabels[1]);
        c.diverStages[2] = CreateDiverStage(c.diverRoot.transform, "胸当て・錘", new Color32(203, 197, 182, 255), 2, out c.diverStageLabels[2]);
        c.diverStages[3] = CreateDiverStage(c.diverRoot.transform, "ヘルメット・送気", new Color32(218, 151, 40, 255), 3, out c.diverStageLabels[3]);
    }

    private static void BuildRank(PrepLoopPrototypeController c)
    {
        CreateText(c.rankPanel.transform, "準備完了", 45, Color.white, new Vector2(0, 120), new Vector2(620, 70));
        c.rankText = CreateText(c.rankPanel.transform, "準備ランク 1", 66, Gold, new Vector2(0, 15), new Vector2(620, 100));
        c.rankText.fontStyle = FontStyles.Bold;
        c.rankBodyText = CreateText(c.rankPanel.transform, "", 27, PaleBlue, new Vector2(0, -90), new Vector2(620, 90));
    }

    private static void BuildLocation(PrepLoopPrototypeController c)
    {
        c.locationTitleText = CreateText(c.locationPanel.transform, "潜水地点を選ぶ", 42, Color.white, new Vector2(0, 380), new Vector2(620, 80));
        c.locationButtons = new Button[3];
        c.locationButtonLabels = new TMP_Text[3];
        c.locationButtons[0] = CreateButton(c.locationPanel.transform, "近距離の海底", 31, new Vector2(0, 160), new Vector2(560, 92), Cream, Red, Ink, out c.locationButtonLabels[0]);
        c.locationButtons[1] = CreateButton(c.locationPanel.transform, "中距離の海底", 31, new Vector2(0, 35), new Vector2(560, 92), Cream, Red, Ink, out c.locationButtonLabels[1]);
        c.locationButtons[2] = CreateButton(c.locationPanel.transform, "遠距離の海底", 31, new Vector2(0, -90), new Vector2(560, 92), Cream, Red, Ink, out c.locationButtonLabels[2]);
    }

    private static void BuildDive(PrepLoopPrototypeController c)
    {
        c.diveTitleText = CreateText(c.divePanel.transform, "潜水中", 42, Color.white, new Vector2(0, 510), new Vector2(620, 70));
        c.diveHudText = CreateText(c.divePanel.transform, "AIR 100% / DEPTH 0m / QUEST 探索中", 22, Gold, new Vector2(0, 450), new Vector2(650, 45));
        c.airGauge = CreateSlider(c.divePanel.transform, new Vector2(0, 398), new Vector2(620, 26));
        c.depthGauge = CreateSlider(c.divePanel.transform, new Vector2(0, 360), new Vector2(620, 26));

        CreateText(c.divePanel.transform, "◇\n\n~~~~~~\n\n[ 潜水士 ]", 44, Cyan, new Vector2(0, 90), new Vector2(620, 360));
        c.diveLogText = CreateText(c.divePanel.transform, "潜水士が海底へ向かっている。", 28, Cream, new Vector2(0, -410), new Vector2(620, 100));
    }

    private static void BuildChest(PrepLoopPrototypeController c)
    {
        c.chestTitleText = CreateText(c.chestPanel.transform, "海底イベント", 42, Color.white, new Vector2(0, 540), new Vector2(650, 70));
        c.chestLocationText = CreateText(c.chestPanel.transform, "", 24, Gold, new Vector2(0, 480), new Vector2(360, 54), null, true);
        c.closedChest = CreateChest(c.chestPanel.transform, false, new Vector2(0, 235));
        c.openChest = CreateChest(c.chestPanel.transform, true, new Vector2(0, 235));
        c.inspectButton = CreateButton(c.chestPanel.transform, "▶ 調べる", 34, new Vector2(0, -15), new Vector2(620, 90), Cream, Red, Ink);
        c.chestLogText = CreateText(c.chestPanel.transform, "海底に古い箱が沈んでいる。", 28, Ink, new Vector2(0, -145), new Vector2(620, 110), Cream, true);

        c.rewardCard = CreateTextCard(c.chestPanel.transform, "", 20, Cream, new Vector2(0, -410), new Vector2(620, 300), new Color32(111, 82, 13, 255));
        c.rewardTitleText = CreateText(c.rewardCard.transform, "招待券", 32, Cream, new Vector2(0, 60), new Vector2(560, 95));
        c.rewardBodyText = CreateText(c.rewardCard.transform, "体験情報への誘導", 24, Cream, new Vector2(0, -50), new Vector2(560, 120));
    }

    private static void BuildReturn(PrepLoopPrototypeController c)
    {
        CreateText(c.returnPanel.transform, "帰還", 38, Color.white, new Vector2(0, 545), new Vector2(620, 70));
        GameObject videoGo = CreateRect("ReturnVideo", c.returnPanel.transform, new Vector2(0, 0), new Vector2(520, 900));
        c.returnVideoImage = videoGo.AddComponent<RawImage>();
        c.returnVideoPlayer = videoGo.AddComponent<VideoPlayer>();
        c.returnVideoPlayer.playOnAwake = false;
        c.returnVideoPlayer.isLooping = false;
        c.returnVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        c.skipReturnButton = CreateButton(c.returnPanel.transform, "タップで進む", 22, new Vector2(0, -575), new Vector2(300, 56), PanelBlue, Cyan, Color.white);
    }

    private static void BuildResult(PrepLoopPrototypeController c)
    {
        CreateText(c.resultPanel.transform, "結果", 42, Color.white, new Vector2(0, 420), new Vector2(620, 80));
        c.resultText = CreateText(c.resultPanel.transform, "探索完了", 35, Cream, new Vector2(0, 110), new Vector2(620, 230));
        c.restartButton = CreateButton(c.resultPanel.transform, "もう一度遊ぶ", 32, new Vector2(0, -430), new Vector2(520, 92), Cream, Red, Ink);
    }

    private static GameObject CreateDiverStage(Transform parent, string label, Color bodyColor, int stageIndex, out TMP_Text labelText)
    {
        GameObject stage = CreateRect(label, parent, Vector2.zero, new Vector2(390, 360));
        CreateImage(stage.transform, "Shadow", new Vector2(0, -132), new Vector2(310, 58), new Color32(19, 91, 134, 210), true);
        CreateSpriteImage(stage.transform, "DiverSprite", GetDiverSprite(stageIndex), new Vector2(0, -10), new Vector2(300, 300));
        CreateImage(stage.transform, "BubbleLarge", new Vector2(132, 102), new Vector2(32, 32), new Color32(121, 206, 231, 160), true);
        CreateImage(stage.transform, "BubbleSmall", new Vector2(170, 62), new Vector2(18, 18), new Color32(121, 206, 231, 160), true);
        labelText = CreateText(stage.transform, label, 25, PaleBlue, new Vector2(0, -170), new Vector2(320, 44));
        return stage;
    }

    private static Slider CreateSlider(Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateRect("Slider", parent, pos, size);
        Image background = go.AddComponent<Image>();
        background.color = PanelBlue;

        GameObject fill = CreateRect("Fill", go.transform, Vector2.zero, size);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Cyan;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        return slider;
    }

    private static Button CreateButton(Transform parent, string label, int fontSize, Vector2 pos, Vector2 size, Color bg, Color border, Color text)
    {
        TMP_Text unused;
        return CreateButton(parent, label, fontSize, pos, size, bg, border, text, out unused);
    }

    private static Button CreateButton(Transform parent, string label, int fontSize, Vector2 pos, Vector2 size, Color bg, Color border, Color text, out TMP_Text labelText)
    {
        GameObject go = CreateFramedCard(parent, "Button", pos, size, border, bg, 7f);
        Image image = go.GetComponent<Image>();
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = border;
        colors.highlightedColor = Color.Lerp(border, bg, 0.25f);
        colors.pressedColor = border;
        colors.disabledColor = new Color(bg.r, bg.g, bg.b, 0.35f);
        button.colors = colors;

        labelText = CreateText(go.transform, label, fontSize, text, Vector2.zero, new Vector2(size.x - 24f, size.y - 18f));
        labelText.fontStyle = FontStyles.Bold;
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string text, int fontSize, Color color, Vector2 pos, Vector2 size, Color? background = null, bool card = false)
    {
        GameObject go = card ? CreateFramedCard(parent, "MessageWindow", pos, size, Red, background ?? Cream, 7f) : CreateRect(string.IsNullOrEmpty(text) ? "Text" : text, parent, pos, size);
        TMP_Text tmp = AddTextChild(go.transform, text, fontSize, color);
        return tmp;
    }

    private static GameObject CreateTextCard(Transform parent, string text, int fontSize, Color color, Vector2 pos, Vector2 size, Color background)
    {
        GameObject go = CreateFramedCard(parent, string.IsNullOrEmpty(text) ? "Card" : text, pos, size, Red, background, 7f);

        if (!string.IsNullOrEmpty(text))
        {
            AddTextChild(go.transform, text, fontSize, color);
        }

        return go;
    }

    private static TMP_Text AddTextChild(Transform parent, string text, int fontSize, Color color)
    {
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(parent, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        Stretch(textRect);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetJapaneseFont();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static TMP_FontAsset GetJapaneseFont()
    {
        if (japaneseFont == null)
        {
            japaneseFont = CreateDynamicJapaneseFontAsset();

            if (japaneseFont == null)
            {
                japaneseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JapaneseFontPath);
            }

            if (japaneseFont != null)
            {
                japaneseFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                EditorUtility.SetDirty(japaneseFont);
            }
        }

        return japaneseFont;
    }

    private static TMP_FontAsset CreateDynamicJapaneseFontAsset()
    {
        Font sourceFont = CreateOrLoadProjectJapaneseFont();
        if (sourceFont == null)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(GeneratedJapaneseFontPath));
        if (File.Exists(GeneratedJapaneseFontPath))
        {
            AssetDatabase.DeleteAsset(GeneratedJapaneseFontPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset == null)
        {
            Debug.LogWarning("日本語TMP Dynamicフォントの生成に失敗しました。Font Import Settings の Include Font Data を確認してください。");
            return null;
        }

        fontAsset.name = "NambuQuestJapaneseDynamic";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        AssetDatabase.CreateAsset(fontAsset, GeneratedJapaneseFontPath);
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "NambuQuestJapaneseDynamic Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(GeneratedJapaneseFontPath);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GeneratedJapaneseFontPath);
    }

    private static Font CreateOrLoadProjectJapaneseFont()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(GeneratedJapaneseSourceFontPath));

        string sourcePath = FindJapaneseFontFile();
        if (string.IsNullOrEmpty(sourcePath))
        {
            Debug.LogWarning("コピー可能な日本語フォントファイルが見つかりません。");
            return null;
        }

        FileInfo sourceInfo = new FileInfo(sourcePath);
        FileInfo projectInfo = new FileInfo(GeneratedJapaneseSourceFontPath);
        bool mustRefreshSource = !projectInfo.Exists || projectInfo.Length != sourceInfo.Length;
        if (mustRefreshSource)
        {
            File.Copy(sourcePath, GeneratedJapaneseSourceFontPath, true);
        }

        AssetDatabase.ImportAsset(GeneratedJapaneseSourceFontPath, ImportAssetOptions.ForceUpdate);
        EnsureFontDataIsIncluded(GeneratedJapaneseSourceFontPath);

        Font projectFont = AssetDatabase.LoadAssetAtPath<Font>(GeneratedJapaneseSourceFontPath);
        if (projectFont == null)
        {
            AssetDatabase.ImportAsset(GeneratedJapaneseSourceFontPath);
            projectFont = AssetDatabase.LoadAssetAtPath<Font>(GeneratedJapaneseSourceFontPath);
        }

        return projectFont;
    }

    private static void EnsureFontDataIsIncluded(string assetPath)
    {
        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null)
        {
            return;
        }

        // UnityのバージョンによってフォントImporterの型名が異なるため、
        // 共通のAssetImporterからInclude Font Dataを設定する。
        System.Reflection.PropertyInfo includeFontDataProperty = importer.GetType().GetProperty("includeFontData");
        if (includeFontDataProperty != null && includeFontDataProperty.CanWrite)
        {
            object currentValue = includeFontDataProperty.GetValue(importer, null);
            if (!(currentValue is bool) || !(bool)currentValue)
            {
                includeFontDataProperty.SetValue(importer, true, null);
                importer.SaveAndReimport();
            }
            return;
        }

        SerializedObject serializedImporter = new SerializedObject(importer);
        SerializedProperty includeFontData = serializedImporter.FindProperty("m_IncludeFontData");
        if (includeFontData != null && !includeFontData.boolValue)
        {
            includeFontData.boolValue = true;
            serializedImporter.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }
    }

    private static string FindJapaneseFontFile()
    {
        string[] preferredFiles =
        {
            "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
            "/System/Library/Fonts/Supplemental/AppleGothic.ttf",
            "/Library/Fonts/Arial Unicode.ttf",
            "/System/Library/Fonts/Hiragino Sans GB.ttc",
            "/System/Library/Fonts/CJKSymbolsFallback.ttc"
        };

        foreach (string file in preferredFiles)
        {
            if (File.Exists(file))
            {
                return file;
            }
        }

        return null;
    }

    private static void ApplyJapaneseFontToScene()
    {
        TMP_FontAsset font = GetJapaneseFont();
        if (font == null)
        {
            Debug.LogWarning("日本語TMPフォントが見つかりません。TextMesh Proの日本語フォントアセットを追加してください。");
            return;
        }

        TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (TextMeshProUGUI text in texts)
        {
            text.font = font;
            text.SetAllDirty();
        }
    }

    private static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool circle = false)
    {
        GameObject go = CreateRect(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.color = color;
        if (circle)
        {
            image.sprite = GetCircleSprite();
        }

        return image;
    }

    private static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateRect(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        return image;
    }

    private static Image CreatePixel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        return CreateImage(parent, name, pos, size, color, false);
    }

    private static GameObject CreateFramedCard(Transform parent, string name, Vector2 pos, Vector2 size, Color border, Color fill, float inset)
    {
        GameObject outer = CreateRect(name, parent, pos, size);
        Image outerImage = outer.AddComponent<Image>();
        outerImage.color = border;

        GameObject inner = CreateRect("Inner", outer.transform, Vector2.zero, new Vector2(size.x - inset * 2f, size.y - inset * 2f));
        Image innerImage = inner.AddComponent<Image>();
        innerImage.color = fill;
        inner.transform.SetAsFirstSibling();
        return outer;
    }

    private static void AddSeaDetails(Transform parent)
    {
        CreateImage(parent, "DeepBand", new Vector2(0, -485), new Vector2(720, 230), new Color32(2, 18, 39, 90));
        CreateImage(parent, "BubbleA", new Vector2(250, 338), new Vector2(28, 28), new Color32(121, 206, 231, 120), true);
        CreateImage(parent, "BubbleB", new Vector2(292, 294), new Vector2(16, 16), new Color32(121, 206, 231, 120), true);
        CreateImage(parent, "BubbleC", new Vector2(-280, -360), new Vector2(18, 18), new Color32(121, 206, 231, 80), true);
    }

    private static GameObject CreateChest(Transform parent, bool opened, Vector2 pos)
    {
        GameObject root = CreateFramedCard(parent, opened ? "OpenChestScene" : "ClosedChestScene", pos, new Vector2(620, 350), new Color32(43, 101, 159, 255), PanelBlue, 8f);
        CreateImage(root.transform, "SeaFloor", new Vector2(0, -112), new Vector2(620, 120), new Color32(20, 83, 122, 255), true);
        CreateSpriteImage(root.transform, opened ? "OpenChestSprite" : "ClosedChestSprite", GetChestSprite(opened), new Vector2(0, 24), new Vector2(230, 230));
        return root;
    }

    private static Sprite GetDiverSprite(int stage)
    {
        string path = SpriteDir + "/nambu_diver_stage_" + stage + ".png";
        return CreateOrLoadSprite(path, 96, 96, texture => DrawDiverSprite(texture, stage));
    }

    private static Sprite GetChestSprite(bool opened)
    {
        string path = SpriteDir + (opened ? "/treasure_chest_open.png" : "/treasure_chest_closed.png");
        return CreateOrLoadSprite(path, 96, 96, texture => DrawChestSprite(texture, opened));
    }

    private static Sprite CreateOrLoadSprite(string path, int width, int height, Action<Texture2D> draw)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Clear(texture);
            draw(texture);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 96f;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void DrawDiverSprite(Texture2D texture, int stage)
    {
        Color32 line = new Color32(5, 13, 28, 255);
        Color32 skin = new Color32(255, 182, 128, 255);
        Color32 casual = new Color32(188, 123, 80, 255);
        Color32 suit = new Color32(235, 226, 199, 255);
        Color32 navy = new Color32(15, 31, 56, 255);
        Color32 white = new Color32(247, 247, 241, 255);
        Color32 metal = new Color32(91, 99, 113, 255);
        Color32 brass = new Color32(218, 151, 40, 255);
        Color32 glass = new Color32(58, 154, 188, 255);
        Color32 body = stage == 0 ? casual : suit;

        Rect(texture, 35, 8, 10, 26, line);
        Rect(texture, 51, 8, 10, 26, line);
        Rect(texture, 37, 10, 6, 23, navy);
        Rect(texture, 53, 10, 6, 23, navy);
        Rect(texture, 28, 4, 18, 7, line);
        Rect(texture, 50, 4, 18, 7, line);
        Rect(texture, 30, 6, 14, 4, white);
        Rect(texture, 52, 6, 14, 4, white);

        Rect(texture, 29, 31, 38, 39, line);
        Rect(texture, 33, 34, 30, 33, body);
        Rect(texture, 38, 42, 20, 19, line);
        Rect(texture, 41, 45, 14, 14, stage >= 2 ? metal : white);

        Rect(texture, 18, 32, 10, 31, line);
        Rect(texture, 68, 32, 10, 31, line);
        Rect(texture, 21, 35, 6, 26, body);
        Rect(texture, 69, 35, 6, 26, body);

        if (stage >= 2)
        {
            Rect(texture, 13, 33, 10, 23, line);
            Rect(texture, 73, 33, 10, 23, line);
            Rect(texture, 15, 35, 6, 19, metal);
            Rect(texture, 75, 35, 6, 19, metal);
            Rect(texture, 34, 28, 28, 6, new Color32(217, 190, 126, 255));
        }

        if (stage >= 3)
        {
            Circle(texture, 48, 70, 19, line);
            Circle(texture, 48, 70, 16, brass);
            Circle(texture, 48, 70, 11, line);
            Circle(texture, 48, 70, 8, glass);
            Rect(texture, 65, 78, 7, 19, line);
            Rect(texture, 67, 80, 3, 15, new Color32(103, 157, 187, 255));
        }
        else
        {
            Circle(texture, 48, 72, 17, line);
            Circle(texture, 48, 70, 12, skin);
            Rect(texture, 42, 73, 3, 3, line);
            Rect(texture, 53, 73, 3, 3, line);
            Rect(texture, 42, 63, 12, 2, line);
        }

        if (stage == 0)
        {
            Rect(texture, 76, 42, 8, 18, line);
            Rect(texture, 78, 44, 4, 14, new Color32(83, 196, 225, 255));
        }
    }

    private static void DrawChestSprite(Texture2D texture, bool opened)
    {
        Color32 line = new Color32(5, 13, 28, 255);
        Color32 wood = new Color32(169, 96, 44, 255);
        Color32 lid = new Color32(188, 121, 54, 255);
        Color32 gold = new Color32(255, 212, 87, 255);
        Color32 cream = new Color32(255, 244, 219, 255);

        Rect(texture, 20, 25, 56, 28, line);
        Rect(texture, 24, 29, 48, 20, wood);
        Rect(texture, 41, 34, 14, 14, line);
        Rect(texture, 44, 37, 8, 8, gold);

        if (opened)
        {
            Rect(texture, 15, 53, 32, 9, line);
            Rect(texture, 49, 53, 32, 9, line);
            Rect(texture, 18, 56, 27, 5, gold);
            Rect(texture, 51, 56, 27, 5, gold);
            Star(texture, 48, 67, gold, line);
        }
        else
        {
            Rect(texture, 18, 53, 60, 12, line);
            Rect(texture, 21, 56, 54, 7, lid);
            Rect(texture, 23, 58, 25, 3, cream);
            Rect(texture, 49, 58, 25, 3, cream);
        }
    }

    private static void Star(Texture2D texture, int cx, int cy, Color32 fill, Color32 line)
    {
        Rect(texture, cx - 3, cy - 18, 6, 36, line);
        Rect(texture, cx - 18, cy - 3, 36, 6, line);
        Rect(texture, cx - 12, cy - 12, 24, 24, line);
        Rect(texture, cx - 2, cy - 15, 4, 30, fill);
        Rect(texture, cx - 15, cy - 2, 30, 4, fill);
        Rect(texture, cx - 8, cy - 8, 16, 16, fill);
    }

    private static void Clear(Texture2D texture)
    {
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }
    }

    private static void Rect(Texture2D texture, int x, int y, int width, int height, Color32 color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                if (xx >= 0 && yy >= 0 && xx < texture.width && yy < texture.height)
                {
                    texture.SetPixel(xx, yy, color);
                }
            }
        }
    }

    private static void Circle(Texture2D texture, int cx, int cy, int radius, Color32 color)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= r2 && x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        if (!File.Exists(CircleSpritePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CircleSpritePath));
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = x - 31.5f;
                    float dy = y - 31.5f;
                    texture.SetPixel(x, y, dx * dx + dy * dy <= 31.5f * 31.5f ? solid : clear);
                }
            }

            texture.Apply();
            File.WriteAllBytes(CircleSpritePath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(CircleSpritePath);
            TextureImporter importer = AssetImporter.GetAtPath(CircleSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 64f;
                importer.SaveAndReimport();
            }
        }

        circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        return circleSprite;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
