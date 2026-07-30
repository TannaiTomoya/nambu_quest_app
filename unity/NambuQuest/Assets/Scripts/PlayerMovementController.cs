using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// T2-3: プレイヤーの2D上下左右移動。
///
/// 責務は次の4つに限定する。
/// 1. キーボード入力の取得（WASD + 矢印キー）
/// 2. 移動方向の正規化（斜め移動が速くならないようにする）
/// 3. 速度の適用（Time.fixedDeltaTime でフレームレート非依存）
/// 4. 移動可能範囲への制限（Clamp 方式。境界値は Inspector で調整可能）
///
/// 空気消費・探索地点・記録取得・帰還などのゲームルールは
/// このクラスへ書き込まず、後続タスクで別クラスとして追加する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovementController : MonoBehaviour
{
    [Header("移動速度（units/sec）")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Header("移動可能範囲（ワールド座標・Inspectorで調整）")]
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;
    [SerializeField] private float minY = -4.4f;
    [SerializeField] private float maxY = 0.5f;

    private Rigidbody2D body;
    private Vector2 inputDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        inputDirection = ReadInputDirection();
    }

    private void FixedUpdate()
    {
        Vector2 next = body.position + inputDirection * (moveSpeed * Time.fixedDeltaTime);
        next.x = Mathf.Clamp(next.x, minX, maxX);
        next.y = Mathf.Clamp(next.y, minY, maxY);
        body.MovePosition(next);
    }

    /// <summary>
    /// WASD と矢印キーを同じ移動入力として読む。
    /// 左右（上下）同時押しは打ち消し合って 0 になる。
    /// 斜め入力は正規化して速度が √2 倍にならないようにする。
    /// </summary>
    private static Vector2 ReadInputDirection()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            y += 1f;
        }

        Vector2 direction = new Vector2(x, y);
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }
}
