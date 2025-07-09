# Unity 游戏内调试控制台技术文档

## 项目概述

Unity 游戏内调试控制台是一个功能强大的运行时调试工具，允许开发者在游戏运行时查看日志信息并执行自定义命令。该系统使用 uGUI 构建，具有高性能优化和丰富的配置选项。

## 核心功能

### 1. 日志显示与管理

#### 1.1 支持的日志类型
- **Info 日志**：`Debug.Log()` 生成的一般信息
- **Warning 日志**：`Debug.LogWarning()` 生成的警告信息  
- **Error 日志**：`Debug.LogError()` 和 `Debug.LogException()` 生成的错误信息
- **Assert 日志**：`Debug.LogAssertion()` 生成的断言信息（仅编辑器）

#### 1.2 日志功能特性
- **日志折叠**：相同内容的日志可以折叠显示，显示出现次数
- **日志过滤**：按日志类型（Info/Warning/Error）进行过滤
- **日志搜索**：实时搜索日志内容和堆栈跟踪
- **时间戳记录**：可选择记录并显示日志到达时间
- **日志展开**：点击日志可查看详细的堆栈跟踪信息
- **日志复制**：支持复制单条或全部日志到剪贴板

### 2. 命令控制台系统

#### 2.1 命令注册方式

**方式一：ConsoleMethod 属性**
```csharp
[ConsoleMethod("cube", "Creates a cube at specified position")]
public static void CreateCubeAt(Vector3 position)
{
    GameObject.CreatePrimitive(PrimitiveType.Cube).transform.position = position;
}
```

**方式二：强类型函数注册**
```csharp
void Start()
{
    DebugLogConsole.AddCommand<Vector3>("cube", "Creates a cube", CreateCubeAt);
    DebugLogConsole.AddCommand("destroy", "Destroys object", Destroy);
}
```

**方式三：静态函数注册（弱类型）**
```csharp
DebugLogConsole.AddCommandStatic("cube", "Creates a cube", "CreateCubeAt", typeof(TestScript));
```

**方式四：实例函数注册（弱类型）**
```csharp
DebugLogConsole.AddCommandInstance("cube", "Creates a cube", "CreateCubeAt", this);
```

#### 2.2 支持的参数类型
- **基本类型**：int, float, bool, string, char, byte, etc.
- **Unity 类型**：Vector2/3/4, Color, Color32, Quaternion, Rect, Bounds, etc.
- **对象类型**：GameObject, Component 及其子类
- **集合类型**：支持上述类型的数组和 List

#### 2.3 命令执行特性
- **参数解析**：自动解析命令参数，支持引号包围字符串、方括号包围向量
- **命令历史**：支持上下箭头键浏览命令历史
- **命令提示**：输入时显示匹配的命令建议
- **返回值显示**：命令执行后显示返回值

### 3. Android Logcat 支持

在 Android 平台上可以接收和显示原生 logcat 日志，方便调试原生插件（如 Admob）的问题。

### 4. 弹窗提示系统

控制台隐藏时显示小弹窗，展示新日志数量，点击可重新打开控制台。

## 设置选项详解

### 基础设置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `singleton` | bool | true | 是否在场景切换时保持控制台实例 |
| `startMinimized` | bool | true | 是否初始状态下隐藏控制台 |
| `toggleWithKey` | bool | false | 是否启用快捷键切换控制台显示 |
| `toggleKey` | KeyCode | BackQuote | 切换控制台的快捷键（默认为 ` 键） |

### 窗口设置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `minimumHeight` | float | 200f | 控制台窗口最小高度 |
| `minimumWidth` | float | 240f | 控制台窗口最小宽度 |
| `enableHorizontalResizing` | bool | false | 是否允许水平调整窗口大小 |
| `resizeFromRight` | bool | true | 调整大小按钮位置（右下角/左下角） |
| `logWindowOpacity` | float | 1f | 控制台窗口透明度 (0-1) |
| `avoidScreenCutout` | bool | true | 是否避开屏幕刘海区域 |

### 弹窗设置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `popupOpacity` | float | 1f | 弹窗透明度 (0-1) |
| `popupVisibility` | PopupVisibility | Always | 弹窗显示时机 |
| `popupVisibilityLogFilter` | DebugLogFilter | All | 触发弹窗的日志类型 |
| `popupAvoidsScreenCutout` | bool | false | 弹窗是否避开屏幕刘海 |

### 日志设置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `receiveInfoLogs` | bool | true | 是否接收 Info 日志 |
| `receiveWarningLogs` | bool | true | 是否接收 Warning 日志 |
| `receiveErrorLogs` | bool | true | 是否接收 Error 日志 |
| `receiveExceptionLogs` | bool | true | 是否接收 Exception 日志 |
| `receiveLogsWhileInactive` | bool | false | 控制台隐藏时是否继续接收日志 |
| `captureLogTimestamps` | bool | false | 是否记录日志时间戳 |
| `alwaysDisplayTimestamps` | bool | false | 是否始终显示时间戳 |

### 性能设置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `maxLogCount` | int | int.MaxValue | 最大日志数量限制 |
| `logsToRemoveAfterMaxLogCount` | int | 16 | 达到上限后删除的日志数量 |
| `queuedLogLimit` | int | 256 | 队列中等待处理的日志限制 |
| `maxCollapsedLogLength` | int | 200 | 折叠日志的最大显示长度 |
| `maxExpandedLogLength` | int | 10000 | 展开日志的最大显示长度 |

### 搜索与命令设置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `enableSearchbar` | bool | true | 是否启用搜索栏 |
| `topSearchbarMinWidth` | float | 360f | 搜索栏显示在顶部的最小画布宽度 |
| `clearCommandAfterExecution` | bool | true | 执行命令后是否清空输入框 |
| `commandHistorySize` | int | 15 | 命令历史记录容量 |
| `showCommandSuggestions` | bool | true | 是否显示命令建议 |
| `autoFocusOnCommandInputField` | bool | true | 打开控制台时是否自动聚焦命令输入框 |

### Android 专用设置

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `receiveLogcatLogsInAndroid` | bool | false | 是否接收 Android logcat 日志 |
| `logcatArguments` | string | "" | logcat 过滤参数 |

## 项目入口

### 主要入口点

1. **预制件入口**：`IngameDebugConsole.prefab`
   - 将此预制件拖入场景即可使用
   - 预制件包含完整的 UI 结构和组件配置

2. **脚本入口**：`DebugLogManager` 组件
   - 控制台的核心管理器
   - 负责日志收集、显示和用户交互

3. **命令系统入口**：`DebugLogConsole` 静态类
   - 提供命令注册和执行的 API
   - 可在任何脚本中调用

### 使用步骤

1. **基础使用**：
   ```csharp
   // 1. 将 IngameDebugConsole.prefab 拖入场景
   // 2. 运行时按 ` 键（可配置）切换控制台显示
   // 3. 在控制台中输入 "help" 查看可用命令
   ```

2. **注册自定义命令**：
   ```csharp
   public class MyGameManager : MonoBehaviour
   {
       void Start()
       {
           // 注册命令
           DebugLogConsole.AddCommand("spawn", "Spawn enemy", SpawnEnemy);
           DebugLogConsole.AddCommand<string>("setlevel", "Set level", SetLevel);
       }
       
       void SpawnEnemy()
       {
           // 生成敌人逻辑
           Debug.Log("Enemy spawned!");
       }
       
       void SetLevel(string levelName)
       {
           // 设置关卡逻辑
           Debug.Log($"Level set to: {levelName}");
       }
   }
   ```

## 核心架构与原理

### 1. 系统架构

```
IngameDebugConsole
├── DebugLogManager (核心管理器)
│   ├── 日志收集与处理
│   ├── UI 状态管理
│   └── 用户交互处理
├── DebugLogConsole (命令系统)
│   ├── 命令注册与管理
│   ├── 参数解析
│   └── 命令执行
├── DebugLogRecycledListView (优化的列表视图)
│   ├── 对象池管理
│   ├── 可视化项回收
│   └── 滚动优化
└── 辅助组件
    ├── DebugLogPopup (弹窗管理)
    ├── DebugLogItem (日志项UI)
    └── DebugLogEntry (日志数据)
```

### 2. 性能优化原理

#### 2.1 对象池模式（Object Pooling）

**DebugLogEntry 对象池**：
```csharp
private Stack<DebugLogEntry> pooledLogEntries = new Stack<DebugLogEntry>(64);

private void PoolLogEntry(DebugLogEntry logEntry)
{
    if (pooledLogEntries.Count < 4096)
    {
        logEntry.Clear();
        pooledLogEntries.Push(logEntry);
    }
}
```

**DebugLogItem 对象池**：
```csharp
private Stack<DebugLogItem> pooledLogItems = new Stack<DebugLogItem>(16);

internal DebugLogItem PopLogItem()
{
    if (pooledLogItems.Count > 0)
    {
        DebugLogItem item = pooledLogItems.Pop();
        item.CanvasGroup.alpha = 1f;
        return item;
    }
    else
    {
        return Instantiate(logItemPrefab, logItemsContainer, false);
    }
}
```

#### 2.2 回收列表视图（Recycled List View）

**核心思想**：只为当前可见的日志条目创建 UI 元素，用户滚动时回收和复用这些元素。

**实现原理**：
```csharp
private void UpdateItemsInTheList(bool updateAllVisibleItemContents)
{
    // 计算当前应该显示的日志条目范围
    int newTopIndex = Mathf.FloorToInt(transformComponent.anchoredPosition.y / logItemHeight);
    int newBottomIndex = newTopIndex + visibleLogItemsCount;
    
    // 回收不再可见的UI元素
    if (newTopIndex > currentTopIndex)
        visibleLogItems.TrimStart(newTopIndex - currentTopIndex, poolLogItemAction);
    
    // 创建新可见的UI元素
    if (newBottomIndex > currentBottomIndex)
    {
        for (int i = 0, count = newBottomIndex - currentBottomIndex; i < count; i++)
            visibleLogItems.Add(manager.PopLogItem());
    }
}
```

#### 2.3 循环缓冲区（Circular Buffer）

用于高效管理固定大小的数据集合，避免频繁的内存分配：

```csharp
public class DynamicCircularBuffer<T>
{
    private T[] array;
    private int startIndex;
    private int count;
    
    public void Add(T item)
    {
        if (count == array.Length)
        {
            // 缓冲区已满，覆盖最旧的元素
            array[startIndex] = item;
            startIndex = (startIndex + 1) % array.Length;
        }
        else
        {
            array[(startIndex + count) % array.Length] = item;
            count++;
        }
    }
}
```

### 3. 日志收集原理

#### 3.1 Unity 日志回调

```csharp
private void OnEnable()
{
    Application.logMessageReceived += ReceivedLog;
}

private void ReceivedLog(string logString, string stackTrace, LogType logType)
{
    // 将日志加入队列，在主线程中处理
    lock (logEntriesLock)
    {
        queuedLogEntries.Add(new QueuedDebugLogEntry(logString, stackTrace, logType));
    }
}
```

#### 3.2 日志处理流程

1. **日志接收**：通过 `Application.logMessageReceived` 回调接收日志
2. **队列缓存**：将日志放入线程安全的队列中
3. **主线程处理**：在 Update 中处理队列中的日志
4. **去重折叠**：检查是否为重复日志，进行折叠处理
5. **过滤显示**：根据过滤条件决定是否显示
6. **UI 更新**：更新回收列表视图

### 4. 命令系统原理

#### 4.1 命令注册机制

**反射扫描**：
```csharp
// 扫描所有程序集，寻找带有 [ConsoleMethod] 属性的方法
Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
foreach (Assembly assembly in assemblies)
{
    foreach (Type type in assembly.GetTypes())
    {
        foreach (MethodInfo method in type.GetMethods())
        {
            ConsoleMethodAttribute attribute = method.GetCustomAttribute<ConsoleMethodAttribute>();
            if (attribute != null)
            {
                // 注册命令
                RegisterCommand(method, attribute);
            }
        }
    }
}
```

#### 4.2 参数解析系统

**支持的解析函数**：
```csharp
private static readonly Dictionary<Type, ParseFunction> parseFunctions = new Dictionary<Type, ParseFunction>()
{
    { typeof(string), ParseString },
    { typeof(bool), ParseBool },
    { typeof(int), ParseInt },
    { typeof(Vector3), ParseVector3 },
    { typeof(GameObject), ParseGameObject },
    // ... 更多类型
};
```

**Vector3 解析示例**：
```csharp
private static bool ParseVector3(string input, out object output)
{
    // 解析 "[1 2 3]" 或 "(1,2,3)" 格式
    input = input.Trim();
    if (input.StartsWith("[") && input.EndsWith("]"))
    {
        string[] values = input.Substring(1, input.Length - 2).Split(' ');
        if (values.Length == 3)
        {
            if (float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                output = new Vector3(x, y, z);
                return true;
            }
        }
    }
    output = null;
    return false;
}
```

### 5. 内存管理策略

#### 5.1 日志数量限制

```csharp
private void RemoveOldestLogs(int numberOfLogsToRemove)
{
    // 从未折叠日志列表中移除最旧的日志
    uncollapsedLogEntries.TrimStart(numberOfLogsToRemove, removeUncollapsedLogEntryAction);
    
    // 更新折叠日志的计数
    // 如果某个折叠日志的计数降为0，则从折叠列表中移除
}
```

#### 5.2 字符串截断

```csharp
// 为了优化滚动性能，对过长的日志进行截断
if (logEntry.logString.Length > maxCollapsedLogLength)
{
    logEntry.logString = logEntry.logString.Substring(0, maxCollapsedLogLength) + "...";
}
```

## 扩展与自定义

### 1. 自定义参数类型

```csharp
public class Person
{
    public string Name;
    public int Age;
}

// 注册自定义解析函数
DebugLogConsole.AddCustomParameterType(typeof(Person), ParsePerson);

private static bool ParsePerson(string input, out object output)
{
    // 自定义解析逻辑
    // 例如：('John Doe' 25)
    List<string> args = new List<string>();
    DebugLogConsole.FetchArgumentsFromCommand(input, args);
    
    if (args.Count == 2)
    {
        if (int.TryParse(args[1], out int age))
        {
            output = new Person { Name = args[0], Age = age };
            return true;
        }
    }
    
    output = null;
    return false;
}
```

### 2. 命令执行回调

```csharp
DebugLogConsole.OnCommandExecuted += (command, parameters) =>
{
    Debug.Log($"Command executed: {command} with {parameters.Length} parameters");
};
```

### 3. 自定义 UI 主题

通过修改预制件中的颜色、字体、图标等资源来自定义外观：

- `logItemPrefab`：日志条目预制件
- `infoLog`, `warningLog`, `errorLog`：日志类型图标
- `filterButtonsSelectedColor`：过滤按钮选中颜色
- `logItemNormalColor1/2`：日志条目背景色

## 最佳实践

### 1. 性能优化建议

- 设置合理的 `maxLogCount` 避免内存溢出
- 在发布版本中考虑禁用或限制控制台功能
- 避免在频繁调用的代码中使用 `Debug.Log`
- 使用 `receiveLogsWhileInactive = false` 在控制台隐藏时节省性能

### 2. 安全性考虑

- 在发布版本中移除或限制敏感命令
- 使用条件编译指令控制命令的可用性：
  ```csharp
  #if DEVELOPMENT_BUILD || UNITY_EDITOR
  [ConsoleMethod("cheat", "Enable cheat mode")]
  public static void EnableCheat() { /* ... */ }
  #endif
  ```

### 3. 用户体验优化

- 为命令提供清晰的描述和参数说明
- 使用命令分组和命名约定提高可发现性
- 提供帮助命令和使用示例

## 常见问题解决

### 1. 新输入系统兼容性

在 Unity 2019.2.5 或更早版本中，需要添加编译器指令：
```
Player Settings -> Scripting Define Symbols -> ENABLE_INPUT_SYSTEM
```

### 2. Android Logcat 功能

如果遇到 ClassNotFoundException，在 Proguard 文件中添加：
```
-keep class com.yasirkula.unity.* { *; }
```

### 3. 程序集引用问题

在 Unity 2018.4 或更早版本中，需要从 Assembly Definition References 中移除 `Unity.InputSystem`。

## 总结

Unity 游戏内调试控制台是一个设计精良的调试工具，通过巧妙的架构设计和性能优化，在提供强大功能的同时保持了良好的性能表现。其模块化的设计使得扩展和自定义变得简单，是 Unity 开发中不可多得的调试利器。

关键特性总结：
- 🚀 **高性能**：对象池 + 回收列表视图 + 循环缓冲区
- 🎯 **功能完整**：日志管理 + 命令系统 + 搜索过滤
- 🔧 **易于扩展**：支持自定义命令和参数类型
- 📱 **跨平台**：支持所有 Unity 目标平台
- ⚙️ **高度可配置**：30+ 配置选项满足不同需求 