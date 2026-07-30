#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 日本語TMP Font Assetを作成し、TitleSceneのTitleText/Labelへ割り当てる一時スクリプト。
/// </summary>
public static class CreateJapaneseFontAndAssign
{
    private const string ScenePath = "Assets/Scenes/TitleScene.unity";
    private const string FontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/HiraginoSans JP SDF.asset";

    private const string RequiredCharacters =
        "南部もぐりRPGゲーム開始ボタンが押されました" +
        "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん" +
        "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
        "一二三四五六七八九十百千万円年月日時分秒" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        ".,!?「」『』（）【】・ー〜：；％＋－＝＜＞";

    private static readonly string[] FontFileCandidates =
    {
        "/System/Library/Fonts/ヒラギノ角ゴシック W3.ttc",
        "/System/Library/Fonts/ヒラギノ角ゴシック W4.ttc",
        "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
        "/System/Library/Fonts/Hiragino Sans GB.ttc",
    };

    public static void Run()
    {
        TMP_FontAsset fontAsset = CreateOrLoadJapaneseFontAsset();
        AssignToTitleScene(fontAsset);
        Debug.Log("CreateJapaneseFontAndAssign: 完了 - " + FontAssetPath);
    }

    private static TMP_FontAsset CreateOrLoadJapaneseFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            EnsureCharacters(existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        string fontPath = FindExistingFontFile();
        if (fontPath == null)
        {
            throw new System.Exception("日本語フォントファイルが見つかりませんでした。");
        }

        Debug.Log("CreateJapaneseFontAndAssign: 使用フォント = " + fontPath);

        // OS上のフォントファイルから直接生成（Include Font Data 不要）
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            fontPath,
            0,
            90,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048);

        if (fontAsset == null)
        {
            throw new System.Exception("TMP_FontAsset.CreateFontAsset に失敗しました: " + fontPath);
        }

        fontAsset.name = "HiraginoSans JP SDF";

        string directory = Path.GetDirectoryName(FontAssetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "HiraginoSans JP SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        if (fontAsset.atlasTexture != null)
        {
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        // Dynamic のまま文字を焼き込み、完了後に Static へ固定
        EnsureCharacters(fontAsset);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        TMP_FontAsset loaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (loaded == null)
        {
            throw new System.Exception("作成した Font Asset の再読み込みに失敗しました。");
        }

        return loaded;
    }

    private static void EnsureCharacters(TMP_FontAsset fontAsset)
    {
        bool added = fontAsset.TryAddCharacters(RequiredCharacters, out string missing);
        if (!added)
        {
            Debug.LogWarning("CreateJapaneseFontAndAssign: TryAddCharacters が false を返しました。");
        }

        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning("一部文字を追加できませんでした: " + missing);
        }
    }

    private static string FindExistingFontFile()
    {
        foreach (string path in FontFileCandidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static void AssignToTitleScene(TMP_FontAsset fontAsset)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include);

        int assigned = 0;
        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null)
            {
                continue;
            }

            string objectName = text.gameObject.name;
            if (objectName != "TitleText" && objectName != "Label")
            {
                continue;
            }

            text.font = fontAsset;
            if (fontAsset.material != null)
            {
                text.fontSharedMaterial = fontAsset.material;
            }

            EditorUtility.SetDirty(text);
            assigned++;
        }

        if (assigned < 2)
        {
            throw new System.Exception(
                "TitleText / Label への割り当てが不足しています。assigned=" + assigned);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
        {
            throw new System.Exception("TitleScene の保存に失敗しました。");
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
