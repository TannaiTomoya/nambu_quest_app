#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 【一時スクリプト】用途: ExplorationScene に T2-3 のプレイヤーを追加する。
///
/// 実施内容:
/// 1. Canvas 直下の不透明 UI「Background」を削除する
///    （Overlay Canvas がワールド側スプライトを隠すため。カメラ背景色が
///    同じ深海色なので見た目は変わらない）
/// 2. Global Light 2D が無ければ作成する（URP 2D で Lit スプライトが
///    黒くならないようにするため）
/// 3. World / Player（SpriteRenderer + Rigidbody2D + CircleCollider2D +
///    PlayerMovementController）を作成する。接触イベントのロジックは追加しない
///
/// 再実行の安全性: 既存の「World」を削除してから作り直すため重複しない。
/// ApiTestPanel・StatusText・EventSystem には触れない。
///
/// 実行方法（Unity を閉じた状態で）:
///   Unity -batchmode -quit -projectPath unity/NambuQuest \
///     -executeMethod BuildT23Player.Run
///
/// T2-3 の動作確認が完了したら本スクリプトは削除してよい。
/// </summary>
public static class BuildT23Player
{
    private const string ScenePath = "Assets/Scenes/ExplorationScene.unity";

    /// <summary>プレイヤー仮スプライトの色（深海背景で視認しやすい明色）。</summary>
    private static readonly Color PlayerColor = new Color(0.98f, 0.86f, 0.40f, 1f);

    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RemoveUiBackground();
        FixCameraPosition();
        EnsureGlobalLight2D();
        CreateWorldAndPlayer();

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new System.Exception("ExplorationScene の保存に失敗しました。");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("BuildT23Player: 完了");
    }

    /// <summary>
    /// Canvas 直下の不透明 Background(Image) を削除する。
    /// カメラの背景色が同じ深海色のため、見た目は変わらない。
    /// </summary>
    private static void RemoveUiBackground()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            throw new System.Exception("ExplorationScene に Canvas が見つかりません。");
        }

        Transform background = canvas.transform.Find("Background");
        if (background != null)
        {
            Object.DestroyImmediate(background.gameObject);
        }
    }

    /// <summary>
    /// カメラが z=0 のままだと z=0 のワールドオブジェクトが視錐台の外になり
    /// 描画されないため、2D 標準の z=-10 へ移動する。
    /// </summary>
    private static void FixCameraPosition()
    {
        Camera camera = Object.FindAnyObjectByType<Camera>();
        if (camera != null && camera.transform.position.z >= 0f)
        {
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }

    private static void EnsureGlobalLight2D()
    {
        System.Type lightType = FindLight2DType();
        if (lightType == null)
        {
            Debug.LogWarning("BuildT23Player: Light2D 型が見つからないためライト作成を省略");
            return;
        }

        Object light = Object.FindAnyObjectByType(lightType);
        if (light == null)
        {
            GameObject lightGo = new GameObject("Global Light 2D");
            light = lightGo.AddComponent(lightType);
            var lightTypeProp = lightType.GetProperty("lightType");
            if (lightTypeProp != null)
            {
                foreach (object value in System.Enum.GetValues(lightTypeProp.PropertyType))
                {
                    if (value.ToString() == "Global")
                    {
                        lightTypeProp.SetValue(light, value);
                        break;
                    }
                }
            }
        }

        // AddComponent 直後は適用ソーティングレイヤーが空で、Lit スプライトが
        // 真っ黒になる。Editor 手動追加時と同じく全レイヤーへ適用する
        SerializedObject so = new SerializedObject(light);
        SerializedProperty layers = so.FindProperty("m_ApplyToSortingLayers");
        if (layers != null)
        {
            SortingLayer[] allLayers = SortingLayer.layers;
            layers.arraySize = allLayers.Length;
            for (int i = 0; i < allLayers.Length; i++)
            {
                layers.GetArrayElementAtIndex(i).intValue = allLayers[i].id;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>Light2D 型を全アセンブリから探す（URPのアセンブリ分割差異に対応）。</summary>
    private static System.Type FindLight2DType()
    {
        foreach (System.Reflection.Assembly assembly in
            System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type type = assembly.GetType("UnityEngine.Rendering.Universal.Light2D");
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static void CreateWorldAndPlayer()
    {
        // 再実行時の重複防止：既存の World を削除してから作り直す
        GameObject existing = GameObject.Find("World");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject world = new GameObject("World");

        GameObject player = new GameObject("Player");
        player.transform.SetParent(world.transform, false);
        player.transform.position = new Vector3(0f, -2f, 0f);
        player.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

        // Unity 標準の単色スプライト（円）。ドット絵・アニメーションは範囲外
        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        renderer.color = PlayerColor;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 将来の地点接触判定に備えて Collider だけ用意する（接触ロジックは追加しない）
        player.AddComponent<CircleCollider2D>();

        // 移動範囲などの初期値は PlayerMovementController の SerializeField 既定値を使う
        player.AddComponent<PlayerMovementController>();
    }
}
#endif
