using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterControllerDemo;
using VanishingGames.ECC.Runtime;

public class BoxCastDebugger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("引用 PlayerMovementComponent 来获取检测参数")]
    public PlayerMovementComponent playerMovement;

    [Tooltip("玩家的 Transform（通常和 PlayerMovementComponent 在同一个 GameObject 上）")]
    public Transform playerTransform;

    [Header("Debug Settings")]
    [Tooltip("检测到碰撞时的颜色")]
    public Color hitColor = Color.green;

    [Tooltip("未检测到碰撞时的颜色")]
    public Color noHitColor = Color.red;

    [Tooltip("是否显示碰撞检测结果文本")]
    public bool showResultText = true;

    void Awake()
    {
        playerMovement = playerTransform
            .GetComponent<EccSystem>()
            .GetEccComponent<PlayerMovementComponent>();
    }

	private void OnDrawGizmos()
    {
        if (playerMovement == null || playerTransform == null)
            return;

        DrawPlatformGrabChecks();
    }

    private void DrawPlatformGrabChecks()
    {
        Transform mTransform = playerTransform;

        // Top Box Check
        Vector2 topPos =
            (Vector2)mTransform.position
            + new Vector2(
                playerMovement.PlatformCheckBoxOffsetX,
                playerMovement.PlatformTopCheckBoxOffsetY
            );

        Vector2 topSize = new Vector2(
            playerMovement.PlatformCheckBoxWidth,
            playerMovement.PlatformTopCheckBoxHeight
        );

        Collider2D topHit = Physics2D.OverlapBox(topPos, topSize, 0);
        bool hasTopHit = topHit != null;

        // 绘制 Top Box
        Gizmos.color = hasTopHit ? hitColor : noHitColor;
        DrawWireBox(topPos, topSize);

        // Bottom Box Check
        Vector2 bottomPos =
            (Vector2)mTransform.position
            + new Vector2(
                playerMovement.PlatformCheckBoxOffsetX,
                playerMovement.PlatformButtomCheckBoxOffsetY
            );

        Vector2 bottomSize = new Vector2(
            playerMovement.PlatformCheckBoxWidth,
            playerMovement.PlatformButtomCheckBoxHeight
        );

        Collider2D bottomHit = Physics2D.OverlapBox(bottomPos, bottomSize, 0);
        bool hasBottomHit = bottomHit != null;

        // 绘制 Bottom Box
        Gizmos.color = hasBottomHit ? hitColor : noHitColor;
        DrawWireBox(bottomPos, bottomSize);

        // 绘制中心点
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(topPos, 0.05f);
        Gizmos.DrawWireSphere(bottomPos, 0.05f);

#if UNITY_EDITOR
        // 显示检测结果文本
        if (showResultText)
        {
            UnityEditor.Handles.color = hasTopHit ? hitColor : noHitColor;
            UnityEditor.Handles.Label(
                topPos + Vector2.right * (topSize.x / 2 + 0.2f),
                $"Top: {(hasTopHit ? "HIT" : "NO HIT")}"
            );

            UnityEditor.Handles.color = hasBottomHit ? hitColor : noHitColor;
            UnityEditor.Handles.Label(
                bottomPos + Vector2.right * (bottomSize.x / 2 + 0.2f),
                $"Bottom: {(hasBottomHit ? "HIT" : "NO HIT")}"
            );

            // 显示总体结果
            bool bothHit = hasTopHit && hasBottomHit;
            UnityEditor.Handles.color = bothHit ? hitColor : noHitColor;
            UnityEditor.Handles.Label(
                (Vector2)mTransform.position + Vector2.up * 1.5f,
                $"Grab Check: {(bothHit ? "PASS" : "FAIL")}"
            );
        }
#endif
    }

    private void DrawWireBox(Vector2 center, Vector2 size)
    {
        Vector2 halfSize = size / 2;

        Vector3 topLeft = new Vector3(center.x - halfSize.x, center.y + halfSize.y, 0);
        Vector3 topRight = new Vector3(center.x + halfSize.x, center.y + halfSize.y, 0);
        Vector3 bottomLeft = new Vector3(center.x - halfSize.x, center.y - halfSize.y, 0);
        Vector3 bottomRight = new Vector3(center.x + halfSize.x, center.y - halfSize.y, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
