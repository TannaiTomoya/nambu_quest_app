#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TitleScene の UI クリックが効かない原因（InputSystemUIInputModule の
/// actionsAsset のみ設定・Point/Click 未割当）を修正する。
/// </summary>
public static class FixTitleUiClick
{
    private const string ScenePath = "Assets/Scenes/TitleScene.unity";
    private const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        InputSystemUIInputModule module = Object.FindAnyObjectByType<InputSystemUIInputModule>();
        if (module == null)
        {
            throw new System.Exception("InputSystemUIInputModule が見つかりません。");
        }

        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (asset == null)
        {
            throw new System.Exception("InputActionAsset を読み込めません: " + ActionsPath);
        }

        module.actionsAsset = asset;
        module.point = InputActionReference.Create(FindRequired(asset, "UI/Point"));
        module.leftClick = InputActionReference.Create(FindRequired(asset, "UI/Click"));
        module.middleClick = InputActionReference.Create(FindRequired(asset, "UI/MiddleClick"));
        module.rightClick = InputActionReference.Create(FindRequired(asset, "UI/RightClick"));
        module.scrollWheel = InputActionReference.Create(FindRequired(asset, "UI/ScrollWheel"));
        module.move = InputActionReference.Create(FindRequired(asset, "UI/Navigate"));
        module.submit = InputActionReference.Create(FindRequired(asset, "UI/Submit"));
        module.cancel = InputActionReference.Create(FindRequired(asset, "UI/Cancel"));
        module.trackedDevicePosition = InputActionReference.Create(
            FindRequired(asset, "UI/TrackedDevicePosition"));
        module.trackedDeviceOrientation = InputActionReference.Create(
            FindRequired(asset, "UI/TrackedDeviceOrientation"));

        EditorUtility.SetDirty(module);

        // クリームパネル側でも確実にヒットするよう調整
        Transform startButton = GameObject.Find("StartButton")?.transform;
        if (startButton != null)
        {
            Button button = startButton.GetComponent<Button>();
            Transform inner = startButton.Find("InnerPanel");
            if (button != null && inner != null)
            {
                Image innerImage = inner.GetComponent<Image>();
                if (innerImage != null)
                {
                    innerImage.raycastTarget = true;
                    button.targetGraphic = innerImage;
                    EditorUtility.SetDirty(innerImage);
                    EditorUtility.SetDirty(button);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
        {
            throw new System.Exception("TitleScene の保存に失敗しました。");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("FixTitleUiClick: 完了（UI Point/Click を割当）");
    }

    private static InputAction FindRequired(InputActionAsset asset, string actionPath)
    {
        InputAction action = asset.FindAction(actionPath, throwIfNotFound: false);
        if (action == null)
        {
            throw new System.Exception("InputAction が見つかりません: " + actionPath);
        }

        return action;
    }
}
#endif
