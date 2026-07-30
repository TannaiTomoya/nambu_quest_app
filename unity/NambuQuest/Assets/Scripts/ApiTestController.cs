using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 開発確認用: FastAPI (POST /analyze) との固定JSON疎通テスト。
/// 本番ゲーム機能ではない。疎通確認後は ExplorationScene の ApiTestPanel ごと
/// 削除するか、GameObject を非アクティブにして無効化できる。
/// </summary>
public sealed class ApiTestController : MonoBehaviour
{
    /// <summary>API URL はここ1箇所のみで管理する（ローカル開発用）。</summary>
    private const string AnalyzeUrl = "http://127.0.0.1:8001/analyze";

    /// <summary>通信タイムアウト秒。停止中サーバーでも Unity が固まらないようにする。</summary>
    private const int TimeoutSeconds = 5;

    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button sendButton;

    /// <summary>通信中フラグ。連打で複数リクエストを送らないためのガード。</summary>
    private bool isSending;

    [System.Serializable]
    private sealed class AnalyzeRequest
    {
        public string session_id;
        public string[] visited_points;
        public string[] selected_records;
        public int remaining_air;
        public bool returned_safely;
    }

    [System.Serializable]
    private sealed class AnalyzeResponse
    {
        public string status;
        public string headline;
        public string message;
    }

    private void Awake()
    {
        if (statusText == null || sendButton == null)
        {
            Debug.LogError("ApiTestController: statusText または sendButton が未設定です。");
            enabled = false;
            return;
        }

        sendButton.onClick.AddListener(OnSendButtonClicked);
        statusText.text = "状態：未送信";
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }
    }

    private void OnSendButtonClicked()
    {
        if (isSending)
        {
            return;
        }

        StartCoroutine(SendFixedJson());
    }

    private IEnumerator SendFixedJson()
    {
        isSending = true;
        sendButton.interactable = false;
        statusText.text = "状態：通信中";

        AnalyzeRequest body = new AnalyzeRequest
        {
            session_id = "local-test-001",
            visited_points = new string[0],
            selected_records = new string[0],
            remaining_air = 100,
            returned_safely = true,
        };
        byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

        using (UnityWebRequest request =
            new UnityWebRequest(AnalyzeUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(payload);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = TimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AnalyzeResponse response =
                    JsonUtility.FromJson<AnalyzeResponse>(request.downloadHandler.text);
                string message =
                    (response != null && !string.IsNullOrEmpty(response.message))
                        ? response.message
                        : "(message なし)";
                statusText.text = "状態：通信成功\n" + message;
                Debug.Log("ApiTestController: 通信成功 " + request.downloadHandler.text);
            }
            else
            {
                statusText.text = "状態：通信失敗";
                Debug.LogWarning("ApiTestController: 通信失敗 " + request.error);
            }
        }

        sendButton.interactable = true;
        isSending = false;
    }
}
