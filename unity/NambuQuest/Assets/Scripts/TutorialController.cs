using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 操作説明画面（3ページ）。
/// 同一シーン内でページパネルを切り替え、
/// 最終ページの「わかった」で ExplorationScene へフェード付き遷移する。
/// </summary>
public sealed class TutorialController : MonoBehaviour
{
    private const string ExplorationSceneName = "ExplorationScene";

    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;
    [SerializeField] private GameObject page3;
    [SerializeField] private Button page1NextButton;
    [SerializeField] private Button page2NextButton;
    [SerializeField] private Button understoodButton;

    private void Awake()
    {
        if (page1 == null || page2 == null || page3 == null ||
            page1NextButton == null || page2NextButton == null || understoodButton == null)
        {
            Debug.LogError("TutorialController: ページまたはボタンの参照が未設定です。");
            return;
        }

        page1NextButton.onClick.RemoveListener(ShowPage2);
        page1NextButton.onClick.AddListener(ShowPage2);
        page2NextButton.onClick.RemoveListener(ShowPage3);
        page2NextButton.onClick.AddListener(ShowPage3);
        understoodButton.onClick.RemoveListener(OnUnderstoodButtonClicked);
        understoodButton.onClick.AddListener(OnUnderstoodButtonClicked);

        ShowPage(1);
    }

    private void OnDestroy()
    {
        if (page1NextButton != null)
        {
            page1NextButton.onClick.RemoveListener(ShowPage2);
        }

        if (page2NextButton != null)
        {
            page2NextButton.onClick.RemoveListener(ShowPage3);
        }

        if (understoodButton != null)
        {
            understoodButton.onClick.RemoveListener(OnUnderstoodButtonClicked);
        }
    }

    private void ShowPage2()
    {
        ShowPage(2);
    }

    private void ShowPage3()
    {
        ShowPage(3);
    }

    private void ShowPage(int pageNumber)
    {
        page1.SetActive(pageNumber == 1);
        page2.SetActive(pageNumber == 2);
        page3.SetActive(pageNumber == 3);
    }

    public void OnUnderstoodButtonClicked()
    {
        SceneTransitionController.TransitionTo(ExplorationSceneName);
    }
}
