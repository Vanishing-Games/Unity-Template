<!--
 * // -----------------------------------------------------------------------------
 * //  Copyright (c) 2025 Vanishing Games. All Rights Reserved.
 * @Author: VanishXiao
 * @Date: 2025-07-07 18:41:51
 * @LastEditTime: 2025-07-07 23:21:23
 * // -----------------------------------------------------------------------------
-->

# ⚙️ CI/CD 配置文件文档

本目录包含Unity项目CI/CD流水线的所有配置文件。这些配置文件用于控制构建、测试、部署等各个环节的行为。

## 📋 目录

- [⚙️ CI/CD 配置文件文档](#️-cicd-配置文件文档)
  - [📋 目录](#-目录)
  - [📋 配置文件概览](#-配置文件概览)
  - [🔧 核心配置文件](#-核心配置文件)
    - [pipeline-config.json](#pipeline-configjson)
    - [ci-defaults.json](#ci-defaultsjson)
    - [defaults.json](#defaultsjson)
  - [🔨 构建配置](#-构建配置)
    - [build-matrix.json](#build-matrixjson)
    - [build-profiles.json](#build-profilesjson)
  - [🚀 部署配置](#-部署配置)
    - [deploy-targets.json](#deploy-targetsjson)
  - [📖 配置最佳实践](#-配置最佳实践)
    - [1. 版本控制](#1-版本控制)
    - [2. 配置验证](#2-配置验证)
    - [3. 环境特定配置](#3-环境特定配置)
  - [🎯 常见配置场景](#-常见配置场景)
    - [场景1: 添加新的构建平台](#场景1-添加新的构建平台)
    - [场景2: 配置新的部署目标](#场景2-配置新的部署目标)
    - [场景3: 调整构建超时时间](#场景3-调整构建超时时间)
  - [🔧 故障排除](#-故障排除)
    - [配置文件格式错误](#配置文件格式错误)
    - [常见错误及解决方案](#常见错误及解决方案)
      - [1. 构建目标不匹配](#1-构建目标不匹配)
      - [2. 部署平台不兼容](#2-部署平台不兼容)
      - [3. 缺少必需的secrets](#3-缺少必需的secrets)
    - [调试配置](#调试配置)

## 📋 配置文件概览

| 配置文件 | 功能描述 | 修改频率 |
|----------|----------|----------|
| `pipeline-config.json` | 流水线触发条件和全局设置 | 低 |
| `ci-defaults.json` | CI/CD默认参数 | 中等 |
| `defaults.json` | 项目通用默认设置 | 低 |
| `build-matrix.json` | 构建平台矩阵配置 | 低 |
| `build-profiles.json` | 不同构建类型的Unity配置文件映射 | 中等 |
| `deploy-targets.json` | 部署目标平台配置 | 中等 |
| `roslyn-lint-config.json` | Roslyn代码规范检查配置 | 中等 |

## 🔧 核心配置文件

### pipeline-config.json

**用途**: 定义流水线触发条件、验证规则和错误处理策略

<details>
<summary>点击查看详细配置选项</summary>

```json
{
  "triggers": {
    "ciOnly": {
      "description": "仅执行CI流程（测试+构建）",
      "events": [
        {
          "type": "pull_request",
          "branches": ["develop", "main"],
          "actions": ["opened", "synchronize", "reopened", "ready_for_review"]
        }
      ],

    },
    "fullCICD": {
      "description": "完整CI/CD流程（测试+构建+发布+部署）",
      "events": [
        {
          "type": "push",
          "tags": ["v[0-9]+.[0-9]+.[0-9]+"]
        }
      ]
    },
    "skip": {
      "commitKeywords": ["[SKIP CICD]", "[SKIP CI]"]
    }
  },
  "validation": {
    "requiredSecrets": ["UNITY_EMAIL", "UNITY_PASSWORD", "UNITY_LICENSE"],
    "optionalSecrets": ["BUTLER_API_KEY", "STEAM_USERNAME", "SLACK_WEBHOOK"]
  },
  "errorHandling": {
    "retryOnFailure": {
      "enabled": true,
      "maxRetries": 2,
      "retryableErrors": ["network", "timeout", "unity_license"]
    }
  }
}
```

**主要配置项**:
- `triggers` - 定义什么情况下触发流水线
- `validation` - 验证必需的secrets和配置
- `errorHandling` - 错误处理和重试策略
- `notifications` - 通知配置
- `debugging` - 调试模式设置

</details>

### ci-defaults.json

**用途**: CI/CD流水线的默认参数配置

```json
{
  "metadataConfig": {
    "projectName": "Unity-Template",
    "skipTests": false,
    "testsOnly": false,
    "buildType": "release",
    "retentionDays": 30,
    "timeoutMinutesTests": 30,
    "timeoutMinutesBuild": 60
  },
  "testDataConfig": {
    "unityVersion": "2022.3.58f1",
    "useGitLfs": true,
    "editModePath": "Assets/Tests/Editor",
    "playModePath": "Assets/Tests/PlayMode",
    "quietMode": false
  },
  "artifactConfig": {
    "requiresCombined": true,
    "skipPerBuildTarget": false 
  }
}
```

**修改指南**:
- `projectName` - 更新为您的项目名称
- `unityVersion` - 保持与项目Unity版本一致
- `timeoutMinutes*` - 根据项目大小调整超时时间
- `retentionDays` - 根据存储策略调整保留天数

### defaults.json

**用途**: 项目的通用默认设置

<details>
<summary>点击查看配置详情</summary>

```json
{
  "unity": {
    "version": "2022.3.58f1"
  },
  "project": {
    "name": "Unity-Template"
  },
  "pipeline": {
    "useGitLfs": true,
    "quietMode": false,
    "excludeUnityTests": false,
    "forceCombineArtifacts": true
  },
  "tests": {
    "editMode": { "path": "Assets/Tests/Editor" },
    "playMode": { "path": "Assets/Tests/PlayMode" },
    "timeoutMinutes": 20
  },
  "build": {
    "timeoutMinutes": 30,
    "retentionDays": {
      "preview": 7,
      "release_candidate": 14,
      "release": 30
    },
    "defaultTargets": ["StandaloneWindows64", "StandaloneOSX"],
    "availableTargets": ["Android", "WebGL", "iOS", "StandaloneLinux64"]
  },
  "deploy": {
    "defaultTargets": ["gh-pages"],
    "availableTargets": ["itch.io", "steam", "s3", "firebase"]
  }
}
```

</details>

## 🔨 构建配置

### build-matrix.json

**用途**: 定义构建平台矩阵和操作系统映射

```json
{
  "matrix": [
    {
      "os": "ubuntu-latest",
      "buildTarget": "StandaloneWindows64",
      "displayName": "Windows 64位"
    },
    {
      "os": "macos-latest", 
      "buildTarget": "StandaloneOSX",
      "displayName": "macOS"
    }
  ],
  "default_targets": ["StandaloneWindows64", "StandaloneOSX"]
}
```

**支持的构建目标**:
- `StandaloneWindows64` - Windows 64位 (推荐)
- `StandaloneOSX` - macOS
- `StandaloneLinux64` - Linux 64位
- `Android` - Android平台
- `iOS` - iOS平台
- `WebGL` - Web平台

**操作系统选择**:
- `ubuntu-latest` - 适用于Windows、Linux、Android、WebGL
- `macos-latest` - 必需用于macOS、iOS构建

### build-profiles.json

**用途**: 将构建目标映射到Unity构建配置文件

```json
{
  "Android": {
    "preview": "CI-Android-Preview",
    "release_candidate": "CI-Android-RC", 
    "release": "CI-Android-Release"
  },
  "StandaloneWindows64": {
    "preview": "CI-Windows64-Preview",
    "release_candidate": "CI-Windows64-RC",
    "release": "CI-Windows64-Release"
  }
}
```

**构建类型说明**:
- `preview` - 开发预览版，用于内部测试
- `release_candidate` - 发布候选版，用于测试和预发布
- `release` - 正式发布版

## � 代码规范配置

### roslyn-lint-config.json

**用途**: 配置Roslyn代码规范检查和自动格式化设置

```json
{
  "roslyn-lint": {
    "enabled": true,
    "autofix": true,
    "allowFailure": false,
    "description": "Roslyn 代码规范检查配置"
  },
  "checkPaths": [
    "Assets",
    "CodeUnfucker"
  ],
  "excludePaths": [
    "Assets/Tests",
    "Assets/StreamingAssets",
    "Assets/Plugins/Third-party"
  ],
  "formatSettings": {
    "printWidth": 128,
    "useTabs": true,
    "indentSize": 4,
    "endOfLine": "auto"
  },
  "triggers": {
    "onPush": true,
    "onPullRequest": true,
    "onDevelop": true,
    "onMain": true,
    "onRelease": true
  }
}
```

**主要配置项**:
- `roslyn-lint.enabled` - 是否启用Roslyn代码规范检查
- `roslyn-lint.autofix` - 是否自动修复格式问题并提交
- `roslyn-lint.allowFailure` - 是否允许格式检查失败而不影响CI
- `checkPaths` - 需要检查的文件夹路径数组
- `excludePaths` - 排除检查的文件夹路径数组
- `formatSettings` - CSharpier格式化设置
- `triggers` - 在哪些情况下触发检查

**修改指南**:
- 将项目特定的目录添加到`checkPaths`中
- 将第三方库或生成的代码路径添加到`excludePaths`中
- 根据团队编码规范调整`formatSettings`
- 建议保持`autofix: true`以自动修复格式问题

## �🚀 部署配置

### deploy-targets.json

**用途**: 定义各部署平台的配置和兼容性

```json
{
  "itch.io": {
    "os": "ubuntu-latest",
    "requiresCombinedArtifact": false,
    "minimumBuildType": "release",
    "compatibleBuildTargets": ["WebGL", "StandaloneWindows64", "StandaloneOSX"]
  },
  "steam": {
    "os": "ubuntu-latest", 
    "requiresCombinedArtifact": true,
    "minimumBuildType": "release",
    "compatibleBuildTargets": ["StandaloneWindows64", "StandaloneOSX", "StandaloneLinux64"]
  }
}
```

**部署平台说明**:

| 平台 | 支持的构建目标 | 最小构建类型 | 需要合并构建产物 |
|------|----------------|--------------|-------------------|
| `itch.io` | Desktop, WebGL | release | 否 |
| `steam` | Desktop only | release | 是 |
| `gh-pages` | WebGL only | release | 否 |
| `appcenter` | Mobile only | preview | 是 |
| `firebase` | WebGL only | release | 是 |
| `s3` | All platforms | preview | 是 |

## 📖 配置最佳实践

### 1. 版本控制
```bash
# 提交配置更改时使用描述性消息
git commit -m "config: 添加iOS构建支持 [BUILD TEST]"
git commit -m "config: 更新Unity版本到2022.3.60f1"
```

### 2. 配置验证
在修改配置后，建议：
1. 先验证配置文件格式正确
2. 检查GitHub Actions是否能正确解析配置
3. 确认所有必需的secrets已配置

### 3. 环境特定配置
```json
// 开发环境
{
  "build": {
    "retentionDays": { "preview": 3, "release": 7 }
  }
}

// 生产环境  
{
  "build": {
    "retentionDays": { "preview": 7, "release": 30 }
  }
}
```

## 🎯 常见配置场景

### 场景1: 添加新的构建平台

1. **更新build-matrix.json**:
```json
{
  "matrix": [
    // 现有配置...
    {
      "os": "ubuntu-latest",
      "buildTarget": "Android",
      "displayName": "Android"
    }
  ]
}
```

2. **更新build-profiles.json**:
```json
{
  "Android": {
    "preview": "CI-Android-Preview",
    "release_candidate": "CI-Android-RC",
    "release": "CI-Android-Release"
  }
}
```

3. **更新defaults.json**:
```json
{
  "build": {
    "availableTargets": ["Android", "StandaloneWindows64"]
  }
}
```

### 场景2: 配置新的部署目标

1. **更新deploy-targets.json**:
```json
{
  "新平台": {
    "os": "ubuntu-latest",
    "requiresCombinedArtifact": false,
    "minimumBuildType": "release_candidate",
    "compatibleBuildTargets": ["StandaloneWindows64"]
  }
}
```

2. **添加必需的secrets**:
- 在GitHub仓库设置中添加相应的secrets
- 更新pipeline-config.json中的requiredSecrets列表

### 场景3: 调整构建超时时间

```json
// ci-defaults.json
{
  "metadataConfig": {
    "timeoutMinutesTests": 45,    // 增加测试超时时间
    "timeoutMinutesBuild": 120    // 增加构建超时时间
  }
}
```

## 🔧 故障排除

### 配置文件格式错误
```bash
# 验证JSON格式
cat .github/config/pipeline-config.json | jq .
```

### 常见错误及解决方案

#### 1. 构建目标不匹配
```yaml
错误: BuildTarget 'XXX' not found in build-matrix.json
```
**解决**: 确保build-matrix.json中包含所需的构建目标

#### 2. 部署平台不兼容
```yaml
错误: Deploy target 'steam' is not compatible with build target 'WebGL'
```
**解决**: 检查deploy-targets.json中的compatibleBuildTargets列表

#### 3. 缺少必需的secrets
```yaml
错误: Required secret 'UNITY_LICENSE' not found
```
**解决**: 在GitHub仓库设置中添加缺少的secrets

### 调试配置
启用配置调试模式：
```json
// pipeline-config.json
{
  "debugging": {
    "enableConfigDump": true,
    "enableEnvironmentDump": true
  }
}
```

这将在工作流运行时输出详细的配置信息，便于调试。

---

> **⚠️ 重要提示**: 
> - 修改配置文件后建议先验证格式和配置项的正确性
> - 不要在配置文件中存储敏感信息，使用GitHub Secrets代替
> - 定期备份重要的配置文件

> **📚 相关文档**: 
> - [Workflows 文档](../workflows/README.md)
> - [GitHub Actions 官方文档](https://docs.github.com/en/actions)
