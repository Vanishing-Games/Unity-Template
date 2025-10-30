# 本 Demo 由 cursor 生成

# Unity 游戏内调试控制台示例集
# Unity In-game Debug Console Samples

本文档介绍如何使用调试控制台的各种示例脚本，这些示例涵盖了从基础命令到高级性能监控的完整功能展示。

This document explains how to use the various debug console sample scripts, covering everything from basic commands to advanced performance monitoring.

## 📁 示例文件清单 Sample Files List

### 1. **BasicCommandSamples.cs** - 基础命令示例
展示控制台命令的基本使用模式，包括各种参数类型和命令注册方式。

Demonstrates basic usage patterns of console commands, including various parameter types and command registration methods.

### 2. **GameManagerSamples.cs** - 游戏管理示例
展示如何使用调试控制台进行游戏状态管理、关卡控制和作弊功能实现。

Shows how to use the debug console for game state management, level control, and cheat functionality.

### 3. **CustomParameterSamples.cs** - 自定义参数类型示例
展示如何扩展控制台支持自定义数据类型，包括复杂对象和枚举类型。

Demonstrates how to extend console support for custom data types, including complex objects and enum types.

### 4. **PerformanceMonitorSamples.cs** - 性能监控示例
展示如何使用调试控制台进行性能分析、监控和压力测试。

Shows how to use the debug console for performance analysis, monitoring, and stress testing.

---

## 🚀 快速开始 Quick Start

### 基础设置 Basic Setup

1. **添加控制台预制件 Add Console Prefab**
   ```
   将 IngameDebugConsole.prefab 拖入您的场景
   Drag IngameDebugConsole.prefab into your scene
   ```

2. **添加示例脚本 Add Sample Scripts**
   ```csharp
   // 创建一个空GameObject并添加您需要的示例脚本
   // Create an empty GameObject and add the sample scripts you need
   
   GameObject sampleManager = new GameObject("ConsoleSamples");
   sampleManager.AddComponent<BasicCommandSamples>();
   sampleManager.AddComponent<GameManagerSamples>();
   // 根据需要添加其他示例脚本
   ```

3. **运行游戏并打开控制台 Run Game and Open Console**
   ```
   - 默认快捷键：~ 或 ` (反引号键)
   - Default hotkey: ~ or ` (grave accent key)
   ```

---

## 📚 详细示例说明 Detailed Sample Descriptions

## 1. 📋 BasicCommandSamples - 基础命令示例

### 功能特性 Features
- ✅ 无参数命令 No-parameter commands
- ✅ 单参数命令 Single-parameter commands  
- ✅ 多参数命令 Multi-parameter commands
- ✅ 各种数据类型支持 Various data type support
- ✅ ConsoleMethod属性使用 ConsoleMethod attribute usage
- ✅ 返回值处理 Return value handling

### 可用命令 Available Commands

#### 基础命令 Basic Commands
```bash
# 问候信息 Greeting
hello                    # 显示问候信息 Show greeting message
time                     # 显示当前时间 Show current time
clear                    # 清空控制台 Clear console

# 文本和数值 Text and Numbers
say "Hello World"        # 说出指定文本 Say specified text
wait 2.5                 # 等待2.5秒 Wait for 2.5 seconds
repeat 3                 # 重复执行3次 Repeat 3 times

# 位置和移动 Position and Movement
teleport [0 5 0]         # 传送到指定位置 Teleport to position
move forward 5           # 向前移动5个单位 Move forward 5 units
setpos 10 0 10          # 设置位置坐标 Set position coordinates

# 对象控制 Object Control
visible true             # 设置可见性 Set visibility
freeze false             # 冻结/解冻对象 Freeze/unfreeze object
select Cube              # 选择游戏对象 Select game object
destroy Cube             # 销毁游戏对象 Destroy game object

# 颜色设置 Color Settings
setcolor [1 0 0 1]       # 设置红色 Set red color
setcolor [0 1 0 1]       # 设置绿色 Set green color
```

#### ConsoleMethod属性命令 ConsoleMethod Attribute Commands
```bash
spawn                    # 生成预制件 Spawn prefab
info                     # 显示对象信息 Show object info
random                   # 生成随机数 Generate random number
distance                 # 计算到原点距离 Calculate distance to origin
```

### 使用示例 Usage Examples

```csharp
// 在Inspector中设置目标对象和预制件
// Set target object and prefab in Inspector
public Transform targetTransform;  // 拖入要控制的对象
public GameObject prefabToSpawn;   // 拖入要生成的预制件

// 然后在控制台中使用命令
// Then use commands in console:
// teleport [0 10 0]     // 传送到Y=10的位置
// say "测试消息"        // 显示测试消息  
// setcolor [1 0 0 1]    // 设置为红色
```

---

## 2. 🎮 GameManagerSamples - 游戏管理示例

### 功能特性 Features
- ✅ 游戏状态控制 Game state control
- ✅ 关卡管理 Level management  
- ✅ 玩家管理 Player management
- ✅ 作弊功能 Cheat functionality
- ✅ 游戏设置 Game settings
- ✅ 场景加载 Scene loading

### 可用命令 Available Commands

#### 游戏状态控制 Game State Control
```bash
pause                    # 暂停/恢复游戏 Pause/resume game
speed 2.0               # 设置游戏速度为2倍 Set game speed to 2x
restart                 # 重启当前关卡 Restart current level
quit                    # 退出游戏 Quit game
```

#### 关卡管理 Level Management
```bash
loadlevel 3             # 加载第3关 Load level 3
nextlevel               # 下一关 Next level
prevlevel               # 上一关 Previous level
loadscene "MainMenu"    # 加载主菜单场景 Load main menu scene
```

#### 玩家管理 Player Management
```bash
respawn                 # 重生玩家 Respawn player
setlives 5              # 设置生命数为5 Set lives to 5
addscore 1000           # 增加1000分 Add 1000 score
setscore 0              # 重置分数 Reset score
```

#### 作弊功能 Cheat Functions
```bash
cheat GODMODE           # 激活无敌模式 Activate god mode
god true                # 开启无敌 Enable god mode
fly true                # 开启飞行模式 Enable fly mode
setspeed 2.0            # 设置玩家速度倍数 Set player speed multiplier
```

#### 游戏设置 Game Settings
```bash
setting debugMode true  # 设置调试模式 Set debug mode
getsetting debugMode    # 获取调试模式状态 Get debug mode status
listsettings            # 列出所有设置 List all settings
resetsettings           # 重置所有设置 Reset all settings
```

#### 系统信息 System Information
```bash
status                  # 显示游戏状态 Show game status
scenes                  # 列出所有场景 List all scenes
memory                  # 显示内存使用 Show memory usage
```

### 使用示例 Usage Examples

```csharp
// 在Inspector中设置玩家和重生点
// Set player and spawn points in Inspector
public GameObject playerPrefab;     // 玩家预制件
public Transform[] spawnPoints;     // 重生点数组

// 控制台命令示例
// Console command examples:
// pause                 // 暂停游戏
// loadlevel 2          // 加载第2关
// cheat GODMODE        // 激活作弊码
// status               // 查看当前游戏状态
```

---

## 3. 🔧 CustomParameterSamples - 自定义参数类型示例

### 功能特性 Features
- ✅ 自定义数据类型解析 Custom data type parsing
- ✅ 复杂对象参数支持 Complex object parameter support
- ✅ 枚举类型支持 Enum type support
- ✅ 中英文参数支持 Chinese and English parameter support
- ✅ 多种语法格式 Multiple syntax formats
- ✅ 错误处理和提示 Error handling and hints

### 自定义类型 Custom Types

#### 1. PlayerData - 玩家数据
```bash
# 语法格式 Syntax: (Name Level Health Mana)
createplayer (张三 10 100 50)
createplayer ("John Doe" 15 120 80)
```

#### 2. ItemInfo - 物品信息
```bash
# 语法格式 Syntax: {ItemName:Count:Quality}
additem {剑:1:传奇}
additem {Sword:1:Legendary}
```

#### 3. WeaponType - 武器类型
```bash
# 支持中英文和数字 Supports Chinese, English, and numbers
equipweapon 剑           # 中文 Chinese
equipweapon Sword        # 英文 English  
equipweapon 1           # 数字 Number
```

#### 4. Coordinate2D - 2D坐标
```bash
# 语法格式 Syntax: [x,y] or [x y]
goto2d [10,20]          # 逗号分隔 Comma separated
goto2d [10 20]          # 空格分隔 Space separated
```

#### 5. Range - 范围类型
```bash
# 语法格式 Syntax: min~max or min-max
setrange 1~10           # 波浪号分隔 Tilde separated
setrange 1-10           # 连字符分隔 Hyphen separated
```

### 可用命令 Available Commands

#### 单一自定义参数 Single Custom Parameters
```bash
createplayer (张三 10 100 50)     # 创建玩家
additem {剑:1:传奇}              # 添加物品
equipweapon 剑                   # 装备武器
goto2d [10,20]                  # 移动到2D坐标
setrange 1~10                   # 设置范围
```

#### 多个自定义参数 Multiple Custom Parameters
```bash
giveitem (张三 10 100 50) {剑:1:传奇}        # 给玩家物品
setweaponrange 剑 10~50                     # 设置武器射程
```

#### 信息查询 Information Query
```bash
listweapons             # 列出所有武器类型
testparse              # 测试所有自定义解析器
```

### 使用示例 Usage Examples

```csharp
// 自动注册自定义类型解析器
// Custom type parsers are automatically registered

// 创建角色并装备武器
createplayer (李四 20 150 100)
equipweapon 法杖

// 添加物品到背包
additem {回复药水:5:稀有}
additem {魔法卷轴:3:史诗}

// 移动和设置参数
goto2d [25,30]
setrange 5~15
```

---

## 4. 📊 PerformanceMonitorSamples - 性能监控示例

### 功能特性 Features
- ✅ 实时性能监控 Real-time performance monitoring
- ✅ 内存使用分析 Memory usage analysis
- ✅ 自定义性能分析器 Custom profilers
- ✅ 压力测试 Stress testing
- ✅ 系统信息查询 System information query
- ✅ 性能历史记录 Performance history

### 可用命令 Available Commands

#### 基础性能监控 Basic Performance Monitoring
```bash
fps                     # 显示当前FPS Show current FPS
frametime              # 显示帧时间统计 Show frame time stats
memory                 # 显示内存使用情况 Show memory usage
gc                     # 强制执行垃圾回收 Force garbage collection
```

#### 监控控制 Monitoring Control
```bash
startmonitor           # 开始性能监控 Start performance monitoring
stopmonitor            # 停止性能监控 Stop performance monitoring
setinterval 2.0        # 设置监控间隔为2秒 Set monitoring interval to 2s
clearhistory          # 清空性能历史 Clear performance history
```

#### 性能分析 Performance Analysis
```bash
analyze                # 分析性能数据 Analyze performance data
history 20             # 显示最近20条性能记录 Show recent 20 records
summary                # 显示性能总结 Show performance summary
```

#### 自定义性能分析器 Custom Profilers
```bash
startprofiler "AI更新"   # 开始AI更新分析器 Start AI update profiler
stopprofiler "AI更新"    # 停止AI更新分析器 Stop AI update profiler  
listprofilers          # 列出所有分析器 List all profilers
profilerstats "AI更新"  # 显示分析器统计 Show profiler stats
```

#### 系统信息 System Information
```bash
sysinfo                # 显示系统信息 Show system information
gpuinfo                # 显示GPU信息 Show GPU information
qualitysettings        # 显示质量设置 Show quality settings
```

#### 压力测试 Stress Testing
```bash
stresstest 3           # 执行强度为3的压力测试 Execute stress test intensity 3
spawnobjects 100 2     # 生成100个复杂度为2的测试对象 Spawn 100 test objects complexity 2
clearobjects           # 清理测试对象 Clear test objects
benchmark              # 执行性能基准测试 Execute performance benchmark
```

### 使用示例 Usage Examples

```csharp
// 开始监控性能
startmonitor

// 查看当前性能状态  
fps
memory

// 执行压力测试
stresstest 2

// 分析性能数据
analyze
summary

// 自定义分析器使用
startprofiler "我的功能"
// ... 执行您要测试的代码 ...
stopprofiler "我的功能"
profilerstats "我的功能"
```

### 在代码中使用自定义分析器 Using Custom Profilers in Code

```csharp
// 在您的代码中添加性能分析
public void MyExpensiveFunction()
{
    // 通过控制台启动分析器
    // Start profiler via console: startprofiler "MyFunction"
    
    // 您的代码逻辑
    // Your code logic here
    
    // 通过控制台停止分析器
    // Stop profiler via console: stopprofiler "MyFunction"
}
```

---

## 💡 高级使用技巧 Advanced Usage Tips

### 1. 命令组合使用 Command Combinations

```bash
# 性能监控流程 Performance monitoring workflow
startmonitor           # 开始监控
stresstest 2          # 执行压力测试
analyze               # 分析结果
clearhistory          # 清空历史
```

### 2. 批量操作 Batch Operations

```bash
# 游戏状态重置 Game state reset
setscore 0
setlives 3
respawn
```

### 3. 调试场景设置 Debug Scene Setup

```bash
# 创建测试环境 Create test environment
createplayer (测试员 1 100 100)
additem {测试武器:1:普通}
equipweapon 剑
goto2d [0,0]
```

### 4. 性能优化流程 Performance Optimization Workflow

```bash
# 1. 基准测试 Baseline test
benchmark

# 2. 开始监控 Start monitoring  
startmonitor

# 3. 执行游戏逻辑 Execute game logic
# (运行您的游戏场景)

# 4. 分析性能 Analyze performance
analyze
summary

# 5. 优化后重测 Re-test after optimization
# (应用优化后重复步骤2-4)
```

---

## 🔧 自定义扩展 Custom Extensions

### 添加新的命令类型 Adding New Command Types

```csharp
// 1. 继承示例基类
public class MyCustomSamples : MonoBehaviour
{
    private void Start()
    {
        RegisterMyCommands();
    }
    
    private void RegisterMyCommands()
    {
        DebugLogConsole.AddCommand("mycommand", "我的自定义命令", MyCommand);
    }
    
    private void MyCommand()
    {
        Debug.Log("执行我的自定义命令");
    }
}
```

### 添加新的参数类型 Adding New Parameter Types

```csharp
// 1. 定义数据类型
[System.Serializable]
public class MyDataType
{
    public string name;
    public float value;
}

// 2. 注册解析器
DebugLogConsole.AddCustomParameterType(typeof(MyDataType), ParseMyDataType);

// 3. 实现解析器
private static bool ParseMyDataType(string input, out object output)
{
    // 解析逻辑
    output = new MyDataType();
    return true;
}
```

---

## ❓ 常见问题 FAQ

### Q: 为什么控制台没有显示？
**A:** 检查以下几点：
- 确保IngameDebugConsole预制件在场景中
- 尝试按 ` 或 ~ 键打开控制台
- 检查控制台的Canvas设置

### Q: 命令无法识别怎么办？
**A:** 确认以下几点：
- 示例脚本已添加到场景中的GameObject上
- 脚本的Start()方法已执行（可查看控制台日志）
- 命令名称拼写正确

### Q: 自定义参数解析失败？
**A:** 检查语法格式：
- PlayerData: `(Name Level Health Mana)`
- ItemInfo: `{ItemName:Count:Quality}`
- Coordinate2D: `[x,y]` 或 `[x y]`
- Range: `min~max` 或 `min-max`

### Q: 性能监控数据不准确？
**A:** 建议：
- 在Release模式下测试
- 关闭编辑器的其他窗口
- 等待几秒让性能稳定后再查看数据

---

## 📞 技术支持 Technical Support

如果您遇到问题或需要更多功能，请：

1. 查看项目的技术文档：`Unity游戏内调试控制台技术文档.md`
2. 检查Unity控制台的错误信息
3. 确保Unity版本兼容性
4. 参考官方文档和示例代码

---

## 📝 更新日志 Update Log

- **v1.0.0** - 初始版本，包含4个核心示例脚本
- 基础命令示例完成
- 游戏管理示例完成  
- 自定义参数类型示例完成
- 性能监控示例完成

---

## 🎯 总结 Summary

这些示例涵盖了Unity游戏内调试控制台的主要使用场景：

1. **BasicCommandSamples** - 学习基础命令语法和参数类型
2. **GameManagerSamples** - 实现游戏管理和作弊功能
3. **CustomParameterSamples** - 扩展支持自定义数据类型
4. **PerformanceMonitorSamples** - 进行性能分析和优化

通过这些示例，您可以快速掌握调试控制台的强大功能，并将其集成到您的Unity项目中，大大提升开发和调试效率！

These samples cover the main usage scenarios of Unity's in-game debug console. Use them to quickly master the powerful features and integrate them into your Unity projects for enhanced development and debugging efficiency! 