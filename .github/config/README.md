# CI/CD 配置文档

本目录包含了Unity CI/CD系统的所有配置文件，通过集中化配置管理提供灵活且可维护的自动化流程。

## 📁 配置文件结构

| 文件名 | 描述 | 用途 |
|--------|------|------|
| `defaults.json` | 🛠️ 默认配置 | 项目基础设置、超时时间、路径配置等 |
| `ci-defaults.json` | ⚙️ CI流水线默认值 | CI/CD流水线的默认参数配置 |
| `build-targets.json` | 🎯 构建目标配置 | 支持的构建平台及其运行环境 |
| `build-profiles.json` | 📋 构建配置文件 | 不同构建类型的Unity构建配置文件映射 |
| `deploy-targets.json` | 🚀 部署目标配置 | 支持的部署平台及其要求 |

## 🛠️ 构建类型 (buildType)

### 1. `preview`
- **用途**: 预览版/测试版，用于内部测试或提前体验新功能
- **特点**: 可能包含未完成或实验性功能，主要面向测试人员或开发团队
- **触发**: PR、develop分支推送
- **标签**: 无Git标签创建

### 2. `release_candidate`
- **用途**: 候选发布版，即将成为正式release的版本
- **特点**: 功能和release基本一致，但还在做最后的测试和验证
- **触发**: rc标签推送 (如 `v1.0.0-rc.1`)
- **标签**: 自动创建rc标签

### 3. `release`
- **用途**: 正式发布版，用于最终交付用户或上线的版本
- **特点**: 开启所有优化，关闭调试信息，版本稳定，经过完整测试
- **触发**: release标签推送 (如 `v1.0.0`)、release分支
- **标签**: 自动创建release标签

## 🎯 构建目标 (buildTargets)

配置文件: `build-targets.json`

支持的Unity构建平台：

| 构建目标 | 运行环境 | 最低构建类型 | 说明 |
|----------|----------|-------------|------|
| `Android` | ubuntu-latest | preview | Android移动端 |
| `WebGL` | ubuntu-latest | preview | Web浏览器平台 |
| `StandaloneLinux64-Client` | ubuntu-latest | preview | Linux客户端 |
| `StandaloneLinux64-Server` | ubuntu-latest | preview | Linux服务器端 |
| `StandaloneWindows` | ubuntu-latest | preview | Windows 32位 |
| `StandaloneWindows64` | ubuntu-latest | preview | Windows 64位 |
| `StandaloneOSX` | macos-latest | preview | macOS桌面端 |
| `iOS` | macos-latest | preview | iOS移动端 |

### 配置格式
```json
{
  "BuildTarget": {
    "os": "运行环境",
    "minimumBuildType": "最低构建类型"
  }
}
```

## 🚀 部署目标 (deployTargets)

配置文件: `deploy-targets.json`

支持的部署平台：

| 部署目标 | 运行环境 | 最低构建类型 | 兼容构建目标 | 需要合并产物 |
|----------|----------|-------------|-------------|-------------|
| `gh-pages` | ubuntu-latest | release | WebGL | ❌ |
| `itch.io` | ubuntu-latest | release | 桌面端+WebGL | ❌ |
| `steam` | ubuntu-latest | release | 桌面端 | ✅ |
| `firebase` | ubuntu-latest | release | WebGL | ✅ |
| `s3` | ubuntu-latest | preview | 全平台 | ✅ |
| `appcenter` | ubuntu-latest | preview | Android, iOS | ✅ |
| `testflight` | macos-latest | release_candidate | iOS | ❌ |
| `custom-server` | ubuntu-latest | preview | 全平台 | ✅ |

### 配置格式
```json
{
  "DeployTarget": {
    "os": "运行环境",
    "requiresCombinedArtifact": "是否需要合并产物",
    "minimumBuildType": "最低构建类型",
    "compatibleBuildTargets": ["兼容的构建目标"]
  }
}
```

## 📋 构建配置文件 (buildProfiles)

配置文件: `build-profiles.json`

Unity构建配置文件映射，支持不同构建类型使用不同的构建设置：

```json
{
  "BuildTarget": {
    "preview": "CI-BuildTarget-Preview",
    "release_candidate": "CI-BuildTarget-RC", 
    "release": "CI-BuildTarget-Release"
  }
}
```

## ⚙️ 默认配置 (defaults.json)

### Unity配置
```json
{
  "unity": {
    "version": "2022.3.58f1"  // Unity版本
  }
}
```

### 项目配置
```json
{
  "project": {
    "name": "Unity-CI-CD-Template"  // 项目名称
  }
}
```

### 流水线配置
```json
{
  "pipeline": {
    "useGitLfs": true,              // 是否使用Git LFS
    "quietMode": false,             // 静默模式
    "excludeUnityTests": false,     // 排除Unity测试
    "forceCombineArtifacts": true   // 强制合并产物
  }
}
```

### 测试配置
```json
{
  "tests": {
    "editMode": {
      "path": "Assets/Tests/Editor"    // EditMode测试路径
    },
    "playMode": {
      "path": "Assets/Tests/PlayMode"  // PlayMode测试路径
    },
    "timeoutMinutes": 20               // 测试超时时间
  }
}
```

### 构建配置
```json
{
  "build": {
    "timeoutMinutes": 30,              // 构建超时时间
    "retentionDays": {                 // 产物保留天数
      "preview": 7,
      "release_candidate": 14,
      "release": 30
    },
    "defaultTargets": [                // 默认构建目标
      "WebGL",
      "StandaloneWindows64",
      "StandaloneOSX"
    ],
    "availableTargets": [...]          // 可用构建目标
  }
}
```

### 部署配置
```json
{
  "deploy": {
    "defaultTargets": ["gh-pages"],    // 默认部署目标
    "availableTargets": [...]          // 可用部署目标
  }
}
```

## 🔧 Commit关键字

在commit消息中使用以下关键字来控制CI/CD行为：

- `[SKIP CICD]` - 完全跳过CI/CD流程
- `[TEST ONLY]` - 仅执行测试，跳过构建和部署
- `[SKIP CI]` - (向后兼容) 跳过CI/CD流程

## 🚦 触发条件规则

### 仅CI流程 (测试+构建)
- **PR事件**: develop、main分支的PR创建、更新、重开
- **Commit关键字**: `[TEST ONLY]`

### 完整CI/CD流程 (测试+构建+发布+部署)
- **标签推送**: `v*.*.*` (release), `v*.*.*-rc.*` (release_candidate)
- **分支推送**: `release/*`

### 跳过流程
- **Commit关键字**: `[SKIP CICD]`, `[SKIP CI]`

## 🛡️ 错误处理和调试

### 详细错误信息
- 所有关键步骤都包含详细的错误上下文
- 失败时自动生成调试摘要
- 配置验证失败时提供修复建议

### 调试技巧
1. 检查GitHub Actions日志中的"🔍 配置验证"步骤
2. 查看"📊 流水线摘要"获取整体状态
3. 使用"🧪 配置测试"工作流验证配置更改
4. 检查必需的Secrets和仓库变量设置

## 📝 配置修改指南

### 添加新的构建目标
1. 在`build-targets.json`中添加目标配置
2. 在`build-profiles.json`中添加对应的构建配置文件
3. 更新`defaults.json`中的`availableTargets`列表

### 添加新的部署目标
1. 在`deploy-targets.json`中添加目标配置
2. 在对应的step-4-deploy.yml中添加部署逻辑
3. 更新所需的Secrets文档

### 修改默认设置
1. 编辑`defaults.json`中的对应字段
2. 可选：更新`ci-defaults.json`覆盖特定流水线设置

## 🔒 安全配置

### 必需的Secrets
- `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE`
- `CICD_PAT` (GitHub Personal Access Token)

### 可选的部署Secrets
根据启用的部署目标配置相应的认证信息，详见主README文档。