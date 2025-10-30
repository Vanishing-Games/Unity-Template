using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundMove : MonoBehaviour
{
    [Header("圆形运动参数")]
    [SerializeField]
    private Vector3 centerPoint = Vector3.zero; // 圆心位置

    [SerializeField]
    private float radius = 5f; // 运动半径

    [SerializeField]
    private float speed = 2f; // 运动速度（弧度/秒）

    [SerializeField]
    private bool clockwise = true; // 是否顺时针运动

    [SerializeField]
    private bool startFromCurrentPosition = true; // 是否从当前位置开始运动

    [Header("调试选项")]
    [SerializeField]
    private bool showGizmos = true; // 是否显示Gizmos

    [SerializeField]
    private Color gizmoColor = Color.yellow; // Gizmos颜色

    private float currentAngle; // 当前角度
    private Vector3 initialPosition; // 初始位置

    void Start()
    {
        if (startFromCurrentPosition)
        {
            // 从当前位置计算初始角度
            Vector3 direction = transform.position - centerPoint;
            currentAngle = Mathf.Atan2(direction.z, direction.x);
        }
        else
        {
            // 从0度开始
            currentAngle = 0f;
        }

        initialPosition = transform.position;
    }

    void Update()
    {
        // 计算运动方向
        float direction = clockwise ? -1f : 1f;

        // 更新角度
        currentAngle += direction * speed * Time.deltaTime;

        // 计算新位置（在XZ平面上）
        float x = centerPoint.x + radius * Mathf.Cos(currentAngle);
        float z = centerPoint.z + radius * Mathf.Sin(currentAngle);
        float y = transform.position.y; // 保持Y轴不变

        // 更新物体位置
        transform.position = new Vector3(x, y, z);

        // 可选：让物体面向运动方向
        Vector3 moveDirection = new Vector3(-Mathf.Sin(currentAngle), 0, Mathf.Cos(currentAngle));
        if (clockwise)
        {
            moveDirection = -moveDirection;
        }

        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    // 在Scene视图中绘制Gizmos
    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = gizmoColor;

        // 绘制圆心
        Gizmos.DrawWireSphere(centerPoint, 0.5f);

        // 绘制圆形轨迹
        int segments = 32;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            float x = centerPoint.x + radius * Mathf.Cos(angle);
            float z = centerPoint.z + radius * Mathf.Sin(angle);
            Vector3 point = new Vector3(x, centerPoint.y, z);

            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }

            prevPoint = point;
        }

        // 绘制半径线
        Gizmos.DrawLine(centerPoint, transform.position);
    }

    // 公共方法：设置圆心
    public void SetCenterPoint(Vector3 center)
    {
        centerPoint = center;
    }

    // 公共方法：设置半径
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
    }

    // 公共方法：设置速度
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    // 公共方法：切换运动方向
    public void ToggleDirection()
    {
        clockwise = !clockwise;
    }

    // 公共方法：重置到初始位置
    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
        Vector3 direction = transform.position - centerPoint;
        currentAngle = Mathf.Atan2(direction.z, direction.x);
    }
}
