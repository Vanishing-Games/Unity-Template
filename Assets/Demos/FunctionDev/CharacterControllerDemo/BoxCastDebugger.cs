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
    [Tooltip("检测到碰撞时的颜色")]
    public Color hitColor = Color.green;

    [Tooltip("未检测到碰撞时的颜色")]
    public Color noHitColor = Color.red;

    [Tooltip("是否显示碰撞检测结果文本")]
    public bool showResultText = true;

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
        // Top Box
        Gizmos.DrawCube(
            transform.position
                + new Vector3(
                    playerMovement.PlatformCheckBoxOffsetX,
                    playerMovement.PlatformTopCheckBoxOffsetY,
                    0
                ),
            new Vector3(
                playerMovement.PlatformCheckBoxWidth,
                playerMovement.PlatformTopCheckBoxHeight,
                1
            )
        );

        // Buttom Box
        Gizmos.DrawCube(
            transform.position
                + new Vector3(
                    playerMovement.PlatformCheckBoxOffsetX,
                    playerMovement.PlatformButtomCheckBoxOffsetY,
                    0
                ),
            new Vector3(
                playerMovement.PlatformCheckBoxWidth,
                playerMovement.PlatformButtomCheckBoxHeight,
                1
            )
        );
    }
}
