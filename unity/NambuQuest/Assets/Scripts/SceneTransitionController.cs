using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// シーン遷移用の共通フェード処理。
/// 黒フェードアウト（0.4秒）→ シーン読込 → 黒フェードイン（0.4秒）。
/// 遷移中は入力をブロックし、多重遷移を防ぐ。
/// </summary>
public sealed class SceneTransitionController : MonoBehaviour
{
    private const float FadeOutSeconds = 0.4f;
    private const float FadeInSeconds = 0.4f;

    private static SceneTransitionController instance;

    private CanvasGroup fadeGroup;
    private bool isTransitioning;

    /// <summary>遷移中かどうか。</summary>
    public static bool IsTransitioning
    {
        get { return instance != null && instance.isTransitioning; }
    }

    /// <summary>
    /// フェード付きで指定シーンへ遷移する。遷移中の呼び出しは無視する。
    /// </summary>
    public static void TransitionTo(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneTransitionController: シーン名が空です。");
            return;
        }

        EnsureInstance();

        if (instance.isTransitioning)
        {
            return;
        }

        instance.StartCoroutine(instance.TransitionRoutine(sceneName));
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("SceneTransitionController");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<SceneTransitionController>();
        instance.CreateFadeOverlay(root.transform);
    }

    private void CreateFadeOverlay(Transform parent)
    {
        GameObject canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(parent, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageGo.AddComponent<Image>();
        image.color = Color.black;

        fadeGroup = imageGo.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;
        fadeGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f, FadeOutSeconds);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        if (load == null)
        {
            Debug.LogError("SceneTransitionController: シーンを読み込めません: " + sceneName);
            yield return Fade(1f, 0f, FadeInSeconds);
            fadeGroup.blocksRaycasts = false;
            isTransitioning = false;
            yield break;
        }

        while (!load.isDone)
        {
            yield return null;
        }

        yield return Fade(1f, 0f, FadeInSeconds);

        fadeGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float elapsed = 0f;
        fadeGroup.alpha = from;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / seconds);
            yield return null;
        }

        fadeGroup.alpha = to;
    }
}
