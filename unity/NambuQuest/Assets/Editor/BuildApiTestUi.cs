#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 【一時スクリプト】用途: ExplorationScene に API 疎通テスト用の最小 UI
/// （ApiTestPanel: 見出し・状態表示・送信ボタン + ApiTestController）を追加する。
///
/// 再実行の安全性: 既存の「ApiTestPanel」を削除してから作り直すため、
/// 何度実行しても重複しない（ApiTestPanel 以外のオブジェクトには触れない）。
///
/// 実行方法（Unity を閉じた状態で）:
///   Unity -batchmode -quit -projectPath unity/NambuQuest \
///     -executeMethod BuildApiTestUi.Run
///
/// 疎通確認が完了し、テスト UI を撤去したら本スクリプトも削除してよい。
/// </summary>
public static class BuildApiTestUi
{
    private const string ScenePath = "Assets/Scenes/ExplorationScene.unity";
    private const string FontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/HiraginoSans JP SDF.asset";

    private const string PanelName = "ApiTestPanel";
    private const string TitleLabel = "API通信テスト";
    private const string SendLabel = "送信";
    private const string InitialStatus = "状態：未送信";

    private static readonly Color PanelBg = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color RedBorder = new Color(0.72f, 0.23f, 0.18f, 1f);
    private static readonly Color CreamInner = new Color(0.99f, 0.96f, 0.88f, 1f);
    private static readonly Color TextOnCream = new Color(0.24f, 0.16f, 0.11f, 1f);

    public static void Run()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            throw new System.Exception("日本語 Font Asset が見つかりません: " + FontAssetPath);
        }

        EnsureFontCharacters(font);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            throw new System.Exception("ExplorationScene に Canvas が見つかりません。");
        }

        // 再実行時の重複防止：既存パネルを削除してから作り直す
        Transform existing = canvas.transform.Find(PanelName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject panel = CreatePanel(canvas.transform);
        TextMeshProUGUI statusText = CreateText(
            panel.transform, "StatusText", InitialStatus,
            new Vector2(0f, -40f), new Vector2(480f, 140f), 32f, font);
        CreateText(
            panel.transform, "TitleText", TitleLabel,
            new Vector2(0f, 90f), new Vector2(480f, 60f), 36f, font);
        Button sendButton = CreateSendButton(panel.transform, font);

        ApiTestController controller = panel.AddComponent<ApiTestController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("sendButton").objectReferenceValue = sendButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new System.Exception("ExplorationScene の保存に失敗しました。");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("BuildApiTestUi: 完了");
    }

    private static void EnsureFontCharacters(TMP_FontAsset font)
    {
        string extra =
            TitleLabel + SendLabel + InitialStatus +
            "通信中通信成功通信失敗UnityとFastAPIが接続されましたなし";

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.TryAddCharacters(extra, out string missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning("BuildApiTestUi: 追加できなかった文字: " + missing);
        }

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject(
            PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(40f, -40f);
        rect.sizeDelta = new Vector2(520f, 380f);

        Image image = panel.GetComponent<Image>();
        image.color = PanelBg;
        image.raycastTarget = false;

        return panel;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        TMP_FontAsset font)
    {
        GameObject go = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
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
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static Button CreateSendButton(Transform parent, TMP_FontAsset font)
    {
        GameObject buttonGo = new GameObject(
            "SendButton", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 30f);
        rect.sizeDelta = new Vector2(240f, 80f);

        Image borderImage = buttonGo.GetComponent<Image>();
        borderImage.color = RedBorder;
        borderImage.raycastTarget = true;

        GameObject inner = new GameObject(
            "InnerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inner.transform.SetParent(buttonGo.transform, false);

        RectTransform innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(6f, 6f);
        innerRect.offsetMax = new Vector2(-6f, -6f);

        Image innerImage = inner.GetComponent<Image>();
        innerImage.color = CreamInner;
        innerImage.raycastTarget = false;

        GameObject labelGo = new GameObject(
            "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(inner.transform, false);

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.font = font;
        if (font.material != null)
        {
            labelTmp.fontSharedMaterial = font.material;
        }

        labelTmp.text = SendLabel;
        labelTmp.fontSize = 34f;
        labelTmp.color = TextOnCream;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = borderImage;
        return button;
    }
}
#endif
