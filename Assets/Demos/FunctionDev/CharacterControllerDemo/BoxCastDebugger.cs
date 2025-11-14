using System.Collections;
using System.Collections.Generic;
using CharacterControllerDemo;
using UnityEngine;
using VanishingGames.ECC.Runtime;

public class BoxCastDebugger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("引用 PlayerMovementComponent 来获取检测参数")]
    public PlayerMovementComponent playerMovement;

    [Tooltip("玩家的 Transform（通常和 PlayerMovementComponent 在同一个 GameObject 上）")]
    public Transform playerTransform;

    [Header("Debug Settings")]
    [Tooltip("顶部盒子的基础颜色")]
    public Color topBoxColor = Color.yellow;

    [Tooltip("底部盒子的基础颜色")]
    public Color bottomBoxColor = Color.cyan;

    [Tooltip("碰撞检测到时颜色变深的程度 (0-1)")]
    [Range(0f, 1f)]
    public float darkenAmount = 0.5f;

    [Tooltip("Gizmo 透明度")]
    [Range(0f, 1f)]
    public float gizmoAlpha = 0.5f;

    void Start()
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
        // Top Box 检测
        Vector2 topBoxPos =
            (Vector2)playerTransform.position
            + new Vector2(
                playerMovement.PlatformCheckBoxOffsetX * Mathf.Sign(playerMovement.Velocity.x),
                playerMovement.PlatformTopCheckBoxOffsetY
            );

        Vector2 topBoxSize = new Vector2(
            playerMovement.PlatformCheckBoxWidth,
            playerMovement.PlatformTopCheckBoxHeight
        );

        bool topBoxHit = Physics2D.OverlapBox(topBoxPos, topBoxSize, 0) != null;

        // 设置顶部盒子颜色
        Color topColor = topBoxHit ? DarkenColor(topBoxColor) : topBoxColor;
        topColor.a = gizmoAlpha;
        Gizmos.color = topColor;

        // 绘制顶部盒子
        Gizmos.DrawCube(new Vector3(topBoxPos.x, topBoxPos.y, 0), new Vector3(topBoxSize.x, topBoxSize.y, 1));

        // Bottom Box 检测
        Vector2 bottomBoxPos =
            (Vector2)playerTransform.position
            + new Vector2(
                playerMovement.PlatformCheckBoxOffsetX * Mathf.Sign(playerMovement.Velocity.x),
                playerMovement.PlatformButtomCheckBoxOffsetY
            );

        Vector2 bottomBoxSize = new Vector2(
            playerMovement.PlatformCheckBoxWidth,
            playerMovement.PlatformButtomCheckBoxHeight
        );

        bool bottomBoxHit = Physics2D.OverlapBox(bottomBoxPos, bottomBoxSize, 0) != null;

        // 设置底部盒子颜色
        Color bottomColor = bottomBoxHit ? DarkenColor(bottomBoxColor) : bottomBoxColor;
        bottomColor.a = gizmoAlpha;
        Gizmos.color = bottomColor;

        // 绘制底部盒子
        Gizmos.DrawCube(new Vector3(bottomBoxPos.x, bottomBoxPos.y, 0), new Vector3(bottomBoxSize.x, bottomBoxSize.y, 1));
    }

    /// <summary>
    /// 将颜色变深
    /// </summary>
    private Color DarkenColor(Color color)
    {
        return new Color(
            color.r * (1f - darkenAmount),
            color.g * (1f - darkenAmount),
            color.b * (1f - darkenAmount),
            color.a
        );
    }
}
