using IngameDebugConsole;
using UnityEngine;

/// <summary>
/// 基础命令示例 - 展示控制台命令的基本使用方法
/// Basic Command Samples - Demonstrates basic usage patterns of console commands
/// </summary>
public class BasicCommandSamples : MonoBehaviour
{
    [Header("示例对象 Sample Objects")]
    public Transform targetTransform;
    public GameObject prefabToSpawn;

    private void Start()
    {
        RegisterCommands();
    }

    private void RegisterCommands()
    {
        // ===== 基础命令注册示例 Basic Command Registration Examples =====

        // 无参数命令 - No Parameter Commands
        DebugLogConsole.AddCommand("hello", "打印问候信息 Print greeting message", SayHello);
        DebugLogConsole.AddCommand("time", "显示当前时间 Show current time", ShowCurrentTime);
        DebugLogConsole.AddCommand("clear", "清空控制台 Clear console", ClearConsole);

        // 单参数命令 - Single Parameter Commands
        DebugLogConsole.AddCommand<string>("say", "说出指定文本 Say specified text", Say);
        DebugLogConsole.AddCommand<float>("wait", "等待指定秒数 Wait for specified seconds", Wait);
        DebugLogConsole.AddCommand<int>("repeat", "重复执行次数 Repeat count", SetRepeatCount);

        // 多参数命令 - Multi Parameter Commands
        DebugLogConsole.AddCommand<Vector3>(
            "teleport",
            "传送到指定位置 Teleport to position",
            TeleportTo
        );
        DebugLogConsole.AddCommand<string, float>(
            "move",
            "移动到指定方向 Move in direction",
            MoveInDirection
        );
        DebugLogConsole.AddCommand<float, float, float>(
            "setpos",
            "设置位置坐标 Set position coordinates",
            SetPosition
        );

        // 布尔参数命令 - Boolean Parameter Commands
        DebugLogConsole.AddCommand<bool>("visible", "设置可见性 Set visibility", SetVisible);
        DebugLogConsole.AddCommand<bool>(
            "freeze",
            "冻结/解冻对象 Freeze/unfreeze object",
            FreezeObject
        );

        // GameObject 参数命令 - GameObject Parameter Commands
        DebugLogConsole.AddCommand<GameObject>(
            "select",
            "选择游戏对象 Select game object",
            SelectObject
        );
        DebugLogConsole.AddCommand<GameObject>(
            "destroy",
            "销毁游戏对象 Destroy game object",
            DestroyObject
        );

        // 颜色参数命令 - Color Parameter Commands
        DebugLogConsole.AddCommand<Color>(
            "setcolor",
            "设置对象颜色 Set object color",
            SetObjectColor
        );
    }

    // ===== 命令实现方法 Command Implementation Methods =====

    #region 无参数命令 No Parameter Commands

    private void SayHello()
    {
        Debug.Log("🎮 Hello from Debug Console! 你好，来自调试控制台！");
    }

    private void ShowCurrentTime()
    {
        Debug.Log($"⏰ 当前时间 Current Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    private void ClearConsole()
    {
        Debug.Log("🧹 控制台已清空 Console cleared");
        // 注意：实际的清空功能由控制台内部处理
        // Note: Actual clearing is handled by the console internally
    }

    #endregion

    #region 单参数命令 Single Parameter Commands

    private void Say(string message)
    {
        Debug.Log($"💬 说话 Say: {message}");
    }

    private void Wait(float seconds)
    {
        Debug.Log($"⏳ 等待 Waiting for {seconds} seconds...");
        StartCoroutine(WaitCoroutine(seconds));
    }

    private System.Collections.IEnumerator WaitCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Debug.Log($"✅ 等待完成 Wait completed after {seconds} seconds");
    }

    private void SetRepeatCount(int count)
    {
        Debug.Log($"🔄 设置重复次数 Set repeat count to: {count}");
        for (int i = 1; i <= count; i++)
        {
            Debug.Log($"  #{i}: 重复执行 Repeat execution");
        }
    }

    #endregion

    #region 多参数命令 Multi Parameter Commands

    private void TeleportTo(Vector3 position)
    {
        if (targetTransform != null)
        {
            targetTransform.position = position;
            Debug.Log($"🚀 传送到 Teleported to: {position}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未设置目标Transform No target transform set");
        }
    }

    private void MoveInDirection(string direction, float distance)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("⚠️ 未设置目标Transform No target transform set");
            return;
        }

        Vector3 moveVector = Vector3.zero;
        direction = direction.ToLower();

        switch (direction)
        {
            case "forward":
            case "前":
                moveVector = Vector3.forward;
                break;
            case "back":
            case "后":
                moveVector = Vector3.back;
                break;
            case "left":
            case "左":
                moveVector = Vector3.left;
                break;
            case "right":
            case "右":
                moveVector = Vector3.right;
                break;
            case "up":
            case "上":
                moveVector = Vector3.up;
                break;
            case "down":
            case "下":
                moveVector = Vector3.down;
                break;
            default:
                Debug.LogError($"❌ 无效方向 Invalid direction: {direction}");
                return;
        }

        targetTransform.position += moveVector * distance;
        Debug.Log($"➡️ 移动 Moved {direction} by {distance} units");
    }

    private void SetPosition(float x, float y, float z)
    {
        Vector3 newPosition = new Vector3(x, y, z);
        TeleportTo(newPosition);
    }

    #endregion

    #region 布尔参数命令 Boolean Parameter Commands

    private void SetVisible(bool visible)
    {
        if (targetTransform != null)
        {
            Renderer renderer = targetTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = visible;
                Debug.Log($"👁️ 可见性设置为 Visibility set to: {visible}");
            }
            else
            {
                Debug.LogWarning(
                    "⚠️ 目标对象没有Renderer组件 Target object has no Renderer component"
                );
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未设置目标Transform No target transform set");
        }
    }

    private void FreezeObject(bool freeze)
    {
        if (targetTransform != null)
        {
            Rigidbody rb = targetTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = freeze;
                Debug.Log($"🧊 对象冻结状态 Object freeze state: {freeze}");
            }
            else
            {
                Debug.LogWarning(
                    "⚠️ 目标对象没有Rigidbody组件 Target object has no Rigidbody component"
                );
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未设置目标Transform No target transform set");
        }
    }

    #endregion

    #region GameObject参数命令 GameObject Parameter Commands

    private void SelectObject(GameObject obj)
    {
        if (obj != null)
        {
            targetTransform = obj.transform;
            Debug.Log($"🎯 已选择对象 Selected object: {obj.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ 对象为空 Object is null");
        }
    }

    private void DestroyObject(GameObject obj)
    {
        if (obj != null)
        {
            string objName = obj.name;
            Destroy(obj);
            Debug.Log($"💥 已销毁对象 Destroyed object: {objName}");
        }
        else
        {
            Debug.LogWarning("⚠️ 对象为空 Object is null");
        }
    }

    #endregion

    #region 颜色参数命令 Color Parameter Commands

    private void SetObjectColor(Color color)
    {
        if (targetTransform != null)
        {
            Renderer renderer = targetTransform.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
                Debug.Log($"🎨 颜色设置为 Color set to: {color}");
            }
            else
            {
                Debug.LogWarning(
                    "⚠️ 目标对象没有Renderer或Material Target object has no Renderer or Material"
                );
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未设置目标Transform No target transform set");
        }
    }

    #endregion

    // ===== 使用ConsoleMethod属性的示例 ConsoleMethod Attribute Examples =====

    [ConsoleMethod("spawn", "生成预制件 Spawn prefab")]
    public static void SpawnPrefab()
    {
        var sample = FindObjectOfType<BasicCommandSamples>();
        if (sample != null && sample.prefabToSpawn != null)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(0f, 3f),
                Random.Range(-5f, 5f)
            );
            GameObject spawned = Instantiate(sample.prefabToSpawn, spawnPos, Quaternion.identity);
            Debug.Log($"✨ 已生成预制件 Spawned prefab: {spawned.name} at {spawnPos}");
        }
        else
        {
            Debug.LogWarning(
                "⚠️ 未找到BasicCommandSamples或未设置预制件 BasicCommandSamples not found or prefab not set"
            );
        }
    }

    [ConsoleMethod("info", "显示对象信息 Show object info")]
    public static void ShowObjectInfo()
    {
        var sample = FindObjectOfType<BasicCommandSamples>();
        if (sample != null && sample.targetTransform != null)
        {
            Transform t = sample.targetTransform;
            Debug.Log(
                $"📋 对象信息 Object Info:\n"
                    + $"  名称 Name: {t.name}\n"
                    + $"  位置 Position: {t.position}\n"
                    + $"  旋转 Rotation: {t.rotation.eulerAngles}\n"
                    + $"  缩放 Scale: {t.localScale}"
            );
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到目标对象 No target object found");
        }
    }

    // ===== 返回值示例 Return Value Examples =====

    [ConsoleMethod("random", "生成随机数 Generate random number")]
    public static float GenerateRandomNumber()
    {
        float randomValue = Random.Range(0f, 100f);
        Debug.Log($"🎲 生成随机数 Generated random number: {randomValue}");
        return randomValue; // 返回值会显示在控制台中 Return value will be shown in console
    }

    [ConsoleMethod("distance", "计算到原点距离 Calculate distance to origin")]
    public static float CalculateDistanceToOrigin()
    {
        var sample = FindObjectOfType<BasicCommandSamples>();
        if (sample != null && sample.targetTransform != null)
        {
            float distance = Vector3.Distance(sample.targetTransform.position, Vector3.zero);
            Debug.Log($"📏 到原点距离 Distance to origin: {distance}");
            return distance;
        }
        return -1f;
    }
}
