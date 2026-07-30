#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TitleScene の最低限の見た目改善（外部画像なし）。
/// </summary>
public static class PolishTitleSceneVisual
{
    private const string ScenePath = "Assets/Scenes/TitleScene.unity";

    // 深海・資料風パネル（発表資料を完成イメージとした配色。画像は使わない）
    private static readonly Color DeepSeaTop = new Color(0.04f, 0.12f, 0.24f, 1f);
    private static readonly Color DeepSeaBottom = new Color(0.06f, 0.22f, 0.38f, 1f);
    private static readonly Color CreamPanel = new Color(0.95f, 0.90f, 0.78f, 1f);
    private static readonly Color RedBorder = new Color(0.72f, 0.23f, 0.18f, 1f);
    private static readonly Color TitleColor = new Color(0.97f, 0.94f, 0.86f, 1f);
    private static readonly Color LabelColor = new Color(0.24f, 0.16f, 0.11f, 1f);

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera mainCamera = Object.FindAnyObjectByType<Camera>();
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = DeepSeaTop;
            EditorUtility.SetDirty(mainCamera);
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            throw new System.Exception("Canvas が見つかりません。");
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        EnsureBackground(canvasRect);
        StyleTitle(canvasRect);
        StyleStartButton(canvasRect);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
        {
            throw new System.Exception("TitleScene の保存に失敗しました。");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("PolishTitleSceneVisual: 完了");
    }

    private static void EnsureBackground(RectTransform canvasRect)
    {
        Transform existing = canvasRect.Find("Background");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("Background", typeof(RectTransform));
        root.transform.SetParent(canvasRect, false);
        root.transform.SetAsFirstSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);

        CreateGradientBand(rootRect, "BgTop", new Vector2(0f, 0.45f), new Vector2(1f, 1f), DeepSeaTop);
        CreateGradientBand(rootRect, "BgBottom", new Vector2(0f, 0f), new Vector2(1f, 0.55f), DeepSeaBottom);
    }

    private static void CreateGradientBand(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject band = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        band.transform.SetParent(parent, false);

        RectTransform rect = band.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = band.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void StyleTitle(RectTransform canvasRect)
    {
        Transform titleTransform = canvasRect.Find("TitleText");
        if (titleTransform == null)
        {
            throw new System.Exception("TitleText が見つかりません。");
        }

        RectTransform rect = titleTransform.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -140f);
        rect.sizeDelta = new Vector2(1200f, 160f);

        TextMeshProUGUI title = titleTransform.GetComponent<TextMeshProUGUI>();
        title.text = "南部もぐりRPG";
        title.fontSize = 84f;
        title.color = TitleColor;
        title.alignment = TextAlignmentOptions.Center;
        title.horizontalAlignment = HorizontalAlignmentOptions.Center;
        title.verticalAlignment = VerticalAlignmentOptions.Middle;
        EditorUtility.SetDirty(title);
        EditorUtility.SetDirty(rect);
    }

    private static void StyleStartButton(RectTransform canvasRect)
    {
        Transform buttonTransform = canvasRect.Find("StartButton");
        if (buttonTransform == null)
        {
            throw new System.Exception("StartButton が見つかりません。");
        }

        RectTransform buttonRect = buttonTransform.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 180f);
        buttonRect.sizeDelta = new Vector2(440f, 120f);

        // 外側＝赤枠、内側＝明るいクリームパネル
        Image outerImage = buttonTransform.GetComponent<Image>();
        if (outerImage == null)
        {
            outerImage = buttonTransform.gameObject.AddComponent<Image>();
        }

        outerImage.color = RedBorder;
        outerImage.raycastTarget = true;

        Transform inner = buttonTransform.Find("InnerPanel");
        if (inner == null)
        {
            GameObject innerGo = new GameObject(
                "InnerPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            innerGo.transform.SetParent(buttonTransform, false);
            inner = innerGo.transform;
        }

        RectTransform innerRect = inner.GetComponent<RectTransform>();
        StretchFull(innerRect);
        innerRect.offsetMin = new Vector2(8f, 8f);
        innerRect.offsetMax = new Vector2(-8f, -8f);

        Image innerImage = inner.GetComponent<Image>();
        innerImage.color = CreamPanel;
        innerImage.raycastTarget = false;

        Transform labelTransform = buttonTransform.Find("Label");
        if (labelTransform == null)
        {
            labelTransform = inner.Find("Label");
        }

        if (labelTransform == null)
        {
            throw new System.Exception("Label が見つかりません。");
        }

        labelTransform.SetParent(inner, false);
        RectTransform labelRect = labelTransform.GetComponent<RectTransform>();
        StretchFull(labelRect);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelTransform.GetComponent<TextMeshProUGUI>();
        label.text = "ゲーム開始";
        label.fontSize = 42f;
        label.color = LabelColor;
        label.alignment = TextAlignmentOptions.Center;
        label.horizontalAlignment = HorizontalAlignmentOptions.Center;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;

        Button button = buttonTransform.GetComponent<Button>();
        if (button != null)
        {
            button.targetGraphic = outerImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            EditorUtility.SetDirty(button);
        }

        // Label を最前面に
        labelTransform.SetAsLastSibling();

        EditorUtility.SetDirty(outerImage);
        EditorUtility.SetDirty(innerImage);
        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(buttonRect);
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
#endif
