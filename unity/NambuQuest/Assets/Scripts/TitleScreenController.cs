using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面。「ゲーム開始」でフェード付きで TutorialScene へ遷移する。
/// </summary>
public sealed class TitleScreenController : MonoBehaviour
{
    private const string TutorialSceneName = "TutorialScene";

    [SerializeField] private Button startButton;

    private void Awake()
    {
        if (startButton == null)
        {
            startButton = GetComponentInChildren<Button>(true);
        }

        if (startButton == null)
        {
            Debug.LogError("TitleScreenController: StartButton が見つかりません。");
            return;
        }

        startButton.onClick.RemoveListener(OnStartButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }

    public void OnStartButtonClicked()
    {
        SceneTransitionController.TransitionTo(TutorialSceneName);
    }
}
