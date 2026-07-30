#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 【一時スクリプト】用途: ExplorationScene に T2-4 の探索地点3つ
/// （近・中・遠の単色円 + Trigger 接近判定）と接近表示テキストを追加する。
///
/// 実施内容:
/// 1. Player に "Player" タグを設定する（地点側の判定で使用）
/// 2. World/ExplorationPoints 配下に NearPoint / MiddlePoint / FarPoint を作成
///    （SpriteRenderer + CircleCollider2D(isTrigger) + ExplorationPoint）
/// 3. Canvas に ProximityText(TMP) と PointProximityView を追加し、
///    各地点の参照を SerializedObject で接続する
///
/// 再実行の安全性: 既存の「ExplorationPoints」「ProximityText」を削除してから
/// 作り直すため重複しない。Player・ApiTestPanel・EventSystem の構成には触れない。
///
/// 実行方法（Unity を閉じた状態で）:
///   Unity -batchmode -quit -projectPath unity/NambuQuest \
///     -executeMethod BuildT24Points.Run
///
/// T2-4 の動作確認が完了したら本スクリプトは削除してよい。
/// </summary>
public static class BuildT24Points
{
    private const string ScenePath = "Assets/Scenes/ExplorationScene.unity";
    private const string FontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/HiraginoSans JP SDF.asset";

    /// <summary>接近判定の半径（ワールド単位。localScale 適用前の値）。</summary>
    private const float TriggerRadius = 0.8f;

    private struct PointSpec
    {
        public string ObjectName;
        public string PointId;
        public string DisplayName;
        public DistanceType Distance;
        public Vector2 Position;
        public Color Color;
    }

    // Player 初期位置 (0, -2) からの距離が 近 < 中 < 遠 になる配置。
    // 移動範囲 minX=-8 / maxX=8 / minY=-4.4 / maxY=0.5 の内側に収める
    private static readonly PointSpec[] Points =
    {
        new PointSpec
        {
            ObjectName = "NearPoint",
            PointId = "near-01",
            DisplayName = "近場",
            Distance = DistanceType.Near,
            Position = new Vector2(-3f, -2.5f),
            Color = new Color(0.45f, 0.85f, 0.55f, 1f),
        },
        new PointSpec
        {
            ObjectName = "MiddlePoint",
            PointId = "middle-01",
            DisplayName = "中距離",
            Distance = DistanceType.Middle,
            Position = new Vector2(4f, -0.5f),
            Color = new Color(0.95f, 0.65f, 0.30f, 1f),
        },
        new PointSpec
        {
            ObjectName = "FarPoint",
            PointId = "far-01",
            DisplayName = "遠距離",
            Distance = DistanceType.Far,
            Position = new Vector2(7f, -3.8f),
            Color = new Color(0.90f, 0.35f, 0.35f, 1f),
        },
    };

    public static void Run()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            throw new System.Exception("日本語 Font Asset が見つかりません: " + FontAssetPath);
        }

        EnsureFontCharacters(font);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsurePlayerTag();
        PointProximityView view = CreateProximityView(font);
        CreateExplorationPoints(view);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new System.Exception("ExplorationScene の保存に失敗しました。");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("BuildT24Points: 完了");
    }

    private static void EnsureFontCharacters(TMP_FontAsset font)
    {
        string extra = "接近中：";
        foreach (PointSpec spec in Points)
        {
            extra += spec.DisplayName;
        }

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.TryAddCharacters(extra, out string missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning("BuildT24Points: 追加できなかった文字: " + missing);
        }

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
    }

    private static void EnsurePlayerTag()
    {
        GameObject player = GameObject.Find("World/Player");
        if (player == null)
        {
            throw new System.Exception("World/Player が見つかりません。T2-3 の構成を確認してください。");
        }

        player.tag = "Player";
    }

    private static PointProximityView CreateProximityView(TMP_FontAsset font)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            throw new System.Exception("ExplorationScene に Canvas が見つかりません。");
        }

        // 再実行時の重複防止
        Transform existing = canvas.transform.Find("ProximityText");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject go = new GameObject(
            "ProximityText", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(800f, 80f);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = font;
        if (font.material != null)
        {
            tmp.fontSharedMaterial = font.material;
        }

        tmp.text = string.Empty;
        tmp.fontSize = 44f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        PointProximityView view = go.AddComponent<PointProximityView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("proximityText").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }

    private static void CreateExplorationPoints(PointProximityView view)
    {
        GameObject world = GameObject.Find("World");
        if (world == null)
        {
            throw new System.Exception("World が見つかりません。T2-3 の構成を確認してください。");
        }

        // 再実行時の重複防止
        Transform existing = world.transform.Find("ExplorationPoints");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject parent = new GameObject("ExplorationPoints");
        parent.transform.SetParent(world.transform, false);

        foreach (PointSpec spec in Points)
        {
            CreatePoint(parent.transform, spec, view);
        }
    }

    private static void CreatePoint(Transform parent, PointSpec spec, PointProximityView view)
    {
        GameObject go = new GameObject(spec.ObjectName);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(spec.Position.x, spec.Position.y, 0f);
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        renderer.color = spec.Color;

        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = TriggerRadius;

        ExplorationPoint point = go.AddComponent<ExplorationPoint>();
        SerializedObject so = new SerializedObject(point);
        so.FindProperty("pointId").stringValue = spec.PointId;
        so.FindProperty("displayName").stringValue = spec.DisplayName;
        so.FindProperty("distanceType").enumValueIndex = (int)spec.Distance;
        so.FindProperty("proximityView").objectReferenceValue = view;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
