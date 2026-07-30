using UnityEngine;

/// <summary>探索地点の距離区分。</summary>
public enum DistanceType
{
    Near,
    Middle,
    Far,
}

/// <summary>
/// T2-4: 探索地点1つ分のデータと接近判定。
///
/// 責務は「自分の範囲（Trigger）へ Player が入った／出たことを
/// PointProximityView へ通知する」ことだけに限定する。
/// 判定処理を PlayerMovementController へ書かないための分離。
///
/// 現段階で持つ情報は pointId / displayName / distanceType のみ。
/// 空気消費量・報酬・称号・API送信データ・DB識別子は後続タスクで
/// 別の仕組みとして追加し、このクラスへ直接書き込まない。
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public sealed class ExplorationPoint : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [SerializeField] private string pointId;
    [SerializeField] private string displayName;
    [SerializeField] private DistanceType distanceType;

    [Header("接近中の地点名を表示するビュー")]
    [SerializeField] private PointProximityView proximityView;

    /// <summary>後続タスク（地点選択・探索実行）で参照するための公開情報。</summary>
    public string PointId => pointId;
    public string DisplayName => displayName;
    public DistanceType Distance => distanceType;

    private void Awake()
    {
        if (proximityView == null)
        {
            Debug.LogError("ExplorationPoint: proximityView が未設定です: " + name);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (proximityView == null || !other.CompareTag(PlayerTag))
        {
            return;
        }

        proximityView.ShowApproach(pointId, displayName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (proximityView == null || !other.CompareTag(PlayerTag))
        {
            return;
        }

        proximityView.ClearApproach(pointId);
    }
}
