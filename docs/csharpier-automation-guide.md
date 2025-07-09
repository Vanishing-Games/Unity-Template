# 🧹 CSharpier 自动化流水线使用指南

本指南介绍如何使用增强的 CSharpier 格式化流水线来自动化 C# 代码格式化。

## 📋 功能特性

### ✅ 已实现的功能

1. **✨ 指定目录格式化** - 支持格式化多个指定目录
2. **📝 详细输出** - 打印所有被格式化的文件列表
3. **🚫 文件排除** - 支持排除单个或多个文件/目录
4. **⚙️ 配置文件** - 根目录已配置 `csharpierrc.json`
5. **🔧 自动修复** - 可选的自动格式化并提交功能
6. **📊 详细报告** - 生成格式化统计报告

## 🛠️ 使用方法

### 基本用法

在您的工作流中调用 `roslyn-lint.yml`：

```yaml
jobs:
  format-code:
    name: 格式化代码
    uses: ./.github/workflows/roslyn-lint.yml
    with:
      formatDirectories: "Assets,CodeUnfucker/Src"
      allowAutofix: false
      allowFailure: false
      verboseOutput: true
```

### 参数说明

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `formatDirectories` | string | `"Assets,CodeUnfucker/Src"` | 要格式化的目录列表，用逗号分隔 |
| `excludeFiles` | string | `""` | 要排除的文件模式，用逗号分隔 |
| `allowAutofix` | boolean | `false` | 是否允许自动格式化并提交 |
| `allowFailure` | boolean | `false` | 是否允许格式化失败 |
| `verboseOutput` | boolean | `true` | 是否显示详细输出 |

### 高级用法示例

#### 1. 多目录格式化 + 自定义排除

```yaml
uses: ./.github/workflows/roslyn-lint.yml
with:
  formatDirectories: "Assets,CodeUnfucker/Src,Scripts,Tools"
  excludeFiles: "**/Generated/*,**/Temp.*,**/*.Designer.cs,**/AssemblyInfo.cs"
  allowAutofix: false
  allowFailure: false
  verboseOutput: true
```

#### 2. 仅检查模式（PR 中使用）

```yaml
uses: ./.github/workflows/roslyn-lint.yml
with:
  formatDirectories: "Assets"
  excludeFiles: "Assets/Plugins/**,Assets/ThirdParty/**"
  allowAutofix: false
  allowFailure: false
  verboseOutput: true
```

#### 3. 自动修复模式

```yaml
uses: ./.github/workflows/roslyn-lint.yml
with:
  formatDirectories: "Assets,CodeUnfucker/Src"
  excludeFiles: "Assets/Plugins/**"
  allowAutofix: true    # 🔧 启用自动修复
  allowFailure: false
  verboseOutput: true
```

#### 4. 宽松模式（允许失败）

```yaml
uses: ./.github/workflows/roslyn-lint.yml
with:
  formatDirectories: "Assets,CodeUnfucker/Src"
  excludeFiles: "**/Legacy/**"
  allowAutofix: false
  allowFailure: true    # 🤝 允许失败
  verboseOutput: false
```

## 📁 配置文件

### csharpierrc.json

项目根目录的 `csharpierrc.json` 文件包含了格式化规则和全局排除项：

```json
{
    "printWidth": 128,
    "useTabs": true,
    "indentSize": 4,
    "endOfLine": "auto",
    "exclude": [
        "**/bin/**",
        "**/obj/**",
        "**/Packages/**",
        "**/Library/**",
        "**/Temp/**",
        "**/.git/**",
        "**/node_modules/**",
        "Assets/Plugins/Sirenix/**",
        "Assets/ThirdParty/**"
    ]
}
```

## 🚫 排除模式语法

支持的文件排除模式：

| 模式 | 描述 | 示例 |
|------|------|------|
| `**/Generated/*` | 排除所有 Generated 目录 | `Assets/Scripts/Generated/AutoCode.cs` |
| `**/*.Designer.cs` | 排除所有 Designer 文件 | `Form1.Designer.cs` |
| `**/AssemblyInfo.cs` | 排除程序集信息文件 | `Properties/AssemblyInfo.cs` |
| `Assets/Plugins/**` | 排除整个 Plugins 目录 | `Assets/Plugins/Unity/Script.cs` |
| `**/Temp.*` | 排除临时文件 | `TempScript.cs` |

## 📊 输出报告

格式化完成后，将生成详细的报告：

```
🔍 CSharpier 增强格式化脚本
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📂 目标目录: Assets,CodeUnfucker/Src
❌ 排除模式: Assets/Plugins/**,**/Generated/**
🔧 模式: 检查模式

📁 处理目录: Assets
  ✓ 找到 45 个 .cs 文件
  ✓ 已格式化: Assets/Scripts/Player.cs
  ❌ 需要格式化: Assets/Scripts/Enemy.cs
  🚫 排除: Assets/Plugins/Unity/Script.cs

📁 处理目录: CodeUnfucker/Src
  ✓ 找到 1 个 .cs 文件
  ✅ 已格式化: CodeUnfucker/Src/Program.cs

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 格式化总结
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📁 总文件数: 46
❌ 需要格式化: 1

📋 需要格式化的文件列表:
  • Assets/Scripts/Enemy.cs
```

## 🔄 集成到现有流水线

### 方式1: 在测试步骤前添加

```yaml
jobs:
  format-check:
    name: 代码格式检查
    uses: ./.github/workflows/roslyn-lint.yml
    with:
      formatDirectories: "Assets,CodeUnfucker/Src"
      allowAutofix: false
      allowFailure: false

  run_tests:
    name: 运行测试
    needs: format-check  # 🔗 依赖格式检查
    uses: ./.github/workflows/step-1-test.yml
    # ... 其他配置
```

### 方式2: 独立的格式化流水线

```yaml
name: 🧹 代码格式化

on:
  push:
    paths: ['**/*.cs']
  pull_request:
    paths: ['**/*.cs']

jobs:
  format:
    uses: ./.github/workflows/roslyn-lint.yml
    with:
      formatDirectories: "Assets,CodeUnfucker/Src"
      allowAutofix: ${{ github.event_name == 'push' }}
      allowFailure: false
```

## 🎯 最佳实践

### 1. PR 检查
- 在 Pull Request 中使用 `allowAutofix: false`
- 设置 `allowFailure: false` 强制修复格式问题

### 2. 主分支自动修复
- 在 main/master 分支推送时使用 `allowAutofix: true`
- 自动提交格式化更改

### 3. 开发分支宽松模式
- 在功能分支使用 `allowFailure: true`
- 避免阻塞开发流程

### 4. 排除第三方代码
- 排除 `Assets/Plugins/**`
- 排除 `Assets/ThirdParty/**`
- 排除自动生成的代码

## 🛠️ 本地使用

开发者也可以在本地使用 CSharpier：

```bash
# 安装 CSharpier
dotnet tool install -g csharpier

# 检查格式
dotnet csharpier --check .

# 格式化所有文件
dotnet csharpier .

# 格式化指定目录
dotnet csharpier Assets CodeUnfucker/Src
```

## 🐛 故障排除

### 常见问题

1. **脚本权限错误**
   - 确保 `.github/scripts/csharpier-enhanced.sh` 有执行权限
   - 流水线会自动设置权限

2. **找不到 .cs 文件**
   - 检查 `formatDirectories` 参数是否正确
   - 确保目录存在

3. **排除模式不生效**
   - 检查模式语法是否正确
   - 使用 `verboseOutput: true` 查看排除日志

4. **自动提交失败**
   - 确保有推送权限
   - 检查分支保护规则

### 调试技巧

1. 启用详细输出：`verboseOutput: true`
2. 查看 GitHub Actions 日志
3. 本地测试排除模式
4. 使用 `allowFailure: true` 临时允许失败

## 📚 相关链接

- [CSharpier 官方文档](https://csharpier.com/)
- [GitHub Actions 文档](https://docs.github.com/en/actions)
- [项目配置文件](../csharpierrc.json)