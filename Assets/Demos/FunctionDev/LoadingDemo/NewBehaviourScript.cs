using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [Header("旋转设置")]
    [SerializeField]
    private Transform centerPoint; // 圆心位置

    [SerializeField]
    private float radius = 5f; // 旋转半径

    [SerializeField]
    private float rotationSpeed = 30f; // 旋转速度（度/秒）

    [SerializeField]
    private bool rotateClockwise = true; // 是否顺时针旋转

    private float currentAngle = 0f; // 当前角度

    void Start()
    {
        // 如果没有设置圆心，则使用世界原点作为圆心
        if (centerPoint == null)
        {
            GameObject centerObj = new GameObject("Center Point");
            centerPoint = centerObj.transform;
            centerPoint.position = Vector3.zero;
        }

        // 设置初始位置
        UpdatePosition();
    }

    void Update()
    {
        // 更新角度
        float direction = rotateClockwise ? 1f : -1f;
        currentAngle += rotationSpeed * direction * Time.deltaTime;

        // 保持角度在0-360度范围内
        currentAngle = currentAngle % 360f;

        // 更新位置
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        // 计算新位置
        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 newPosition =
            centerPoint.position
            + new Vector3(
                Mathf.Cos(radians) * radius,
                transform.position.y, // 保持Y轴不变
                Mathf.Sin(radians) * radius
            );

        transform.position = newPosition;

        // 可选：让物体面向旋转方向
        // Vector3 direction = (newPosition - centerPoint.position).normalized;
        // if (direction != Vector3.zero)
        // {
        //     transform.rotation = Quaternion.LookRotation(direction);
        // }
    }

    // 在Scene视图中绘制圆形轨迹（仅在编辑器中）
    private void OnDrawGizmosSelected()
    {
        if (centerPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centerPoint.position, radius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerPoint.position, 0.2f);
        }
    }
}
