using TMPro;
using UnityEngine;

/// <summary>
/// T2-4: 現在接近中の探索地点名を UI テキストへ表示するビュー。
///
/// どの地点に接近中かの状態を1箇所で持ち、範囲を離れたら表示を解除する。
/// 範囲が重なった場合に別地点の Exit で表示が消えないよう、
/// 現在表示中の pointId と一致したときだけ解除する。
/// </summary>
public sealed class PointProximityView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI proximityText;

    private string currentPointId;

    private void Awake()
    {
        if (proximityText == null)
        {
            Debug.LogError("PointProximityView: proximityText が未設定です。");
            return;
        }

        proximityText.text = string.Empty;
    }

    /// <summary>地点範囲へ入ったときに呼ばれ、地点名を表示する。</summary>
    public void ShowApproach(string pointId, string displayName)
    {
        if (proximityText == null)
        {
            return;
        }

        currentPointId = pointId;
        proximityText.text = "接近中：" + displayName;
    }

    /// <summary>地点範囲から出たときに呼ばれ、表示を解除する。</summary>
    public void ClearApproach(string pointId)
    {
        if (proximityText == null || currentPointId != pointId)
        {
            return;
        }

        currentPointId = null;
        proximityText.text = string.Empty;
    }
}
