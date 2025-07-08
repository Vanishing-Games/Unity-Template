<!--
 * // -----------------------------------------------------------------------------
 * //  Copyright (c) 2025 Vanishing Games. All Rights Reserved.
 * @Author: VanishXiao
 * @Date: 2025-07-08 22:38:59
 * @LastEditTime: 2025-07-08 22:40:26
 * // -----------------------------------------------------------------------------
-->
# CodeUnfucker

一个Unity项目的代码辅助工具，提供代码分析和格式化功能。

## 功能

1. **代码分析** - 静态分析C#代码并输出结果到Unity控制台
2. **代码格式化** - 按规范重新排列类成员并添加Region宏
3. **配置系统** - 通过JSON配置文件灵活控制所有功能

## 使用方法

### 构建

```bash
cd CodeUnfucker
dotnet build
```

### 命令行使用

```bash
# 分析代码
dotnet run -- analyze <目录路径>

# 格式化代码
dotnet run -- format <文件路径或目录路径>
```

### Unity Editor集成

1. **自动分析**: 每次编译完成后自动分析Assets/Scripts目录
2. **手动格式化**: 在Project窗口中选择文件/文件夹，然后选择菜单 `CodeUnfucker > Format Code`

## 配置系统

CodeUnfucker使用JSON配置文件来控制所有功能。配置文件位于 `Config/` 目录下。

### 配置文件

#### FormatterConfig.json - 格式化配置
控制代码格式化的所有行为：
- `MinLinesForRegion`: 当区域代码行数超过此值时添加Region宏 (默认: 15)
- `EnableRegionGeneration`: 是否启用Region生成 (默认: true)
- `CreateBackupFiles`: 是否创建备份文件 (默认: true)
- `BackupFileExtension`: 备份文件扩展名 (默认: ".backup")
- `UnityLifeCycleMethods`: Unity生命周期方法列表
- `RegionSettings`: Region名称和缩进设置

#### AnalyzerConfig.json - 分析配置
控制代码分析的所有行为：
- `EnableSyntaxAnalysis`: 是否启用语法分析 (默认: true)
- `EnableSemanticAnalysis`: 是否启用语义分析 (默认: true)
- `ShowReferencedAssemblies`: 是否显示引用的程序集 (默认: true)
- `ShowFileCount`: 是否显示文件数量 (默认: true)
- `FileFilters`: 文件过滤规则

### 配置示例

```json
{
  "FormatterSettings": {
    "MinLinesForRegion": 10,
    "EnableRegionGeneration": true,
    "CreateBackupFiles": false
  },
  "RegionSettings": {
    "PublicRegionName": "公有成员",
    "PrivateRegionName": "私有成员"
  }
}
```

## 代码格式化规则

格式化后的类成员顺序（可通过配置文件修改）：
1. **Public** - 公有成员
2. **Unity LifeCycle** - Unity生命周期方法 (Awake, Start, Update等)
3. **Protected** - 保护成员
4. **Private** - 私有成员
5. **Nested Classes** - 嵌套类
6. **Member Variables** - 成员变量

### Unity生命周期方法

支持的Unity生命周期方法（可通过配置文件扩展）：
- `Awake`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`
- `OnEnable`, `OnDisable`, `OnDestroy`
- `OnApplicationPause`, `OnApplicationFocus`, `OnApplicationQuit`
- `OnGUI`, `OnValidate`, `Reset`
- 所有碰撞和触发器事件方法
- `OnDrawGizmos`, `OnDrawGizmosSelected`

## 安全性

- 格式化时可选择性创建备份文件（通过配置控制）
- 支持单文件和批量格式化
- 格式化完成后会自动刷新Unity Asset Database
- 配置文件错误时自动使用默认配置
- 详细的错误日志和状态信息

## 高级功能

- **配置热重载**: 修改配置文件后无需重启
- **灵活的Region命名**: 可自定义所有Region的名称
- **可配置的Unity方法检测**: 支持添加新的Unity生命周期方法
- **详细的日志控制**: 可控制输出的详细程度 