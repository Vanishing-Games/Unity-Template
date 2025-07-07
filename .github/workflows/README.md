# 工作流概览

本项目采用模块化的 CI/CD 系统，支持 Unity 项目的自动化测试、构建、发布和部署。

## 🏗️ 系统架构

### 主要流水线

| 工作流文件 | 描述 | 触发条件 |
|------------|------|----------|
| `ci-cd-dispatcher.yml` | ⚙️ CI/CD 分发器 | Push to tags, Pull Request |
| `ci-cd-pipeline.yml` | 🚀 完整 CI/CD 流水线 | 手动触发 |
| `ci-pipeline.yml` | 🚀 简化 CI 流水线 | Push to develop |

### 步骤化工作流 (5步法)

| 步骤 | 工作流文件 | 描述 | 功能 |
|------|------------|------|------|
| 1 | `step-1-test.yml` | 📋 测试 | EditMode + PlayMode 测试 |
| 2 | `step-2-build.yml` | 🧩 构建 | 多平台并行构建 |
| 3 | `step-3-release.yml` | 📦 发布 | GitHub Release 创建 |
| 4 | `step-4-deploy.yml` | 🌍 部署 | 多平台部署支持 |
| 5 | `step-5-notify.yml` | 📣 通知 | Slack/Discord 通知 |

### 辅助工作流

| 工作流文件 | 描述 | 用途 |
|------------|------|------|
| `build-version-tagger.yml` | 🏷️ 版本打标签器 | 自动创建/校验 Git 标签 |
| `prepare-metadata.yml` | ⏳ 元数据准备 | 解析和准备流水线配置 |
| `unity-tests-runner.yml` | 🧪 Unity 测试运行器 | 执行具体的 Unity 测试 |
| `unity-license-uploader.yml` | 📥 Unity 许可证上传 | 管理 Unity 许可证 |
| `roslyn-lint.yml` | 📈 代码规范检查 | CSharpier 格式化 |

### 组合与汇总工作流

| 工作流文件 | 描述 |
|------------|------|
| `combine-builds.yml` | 🔗 合并构建产物 |
| `summarize-builds.yml` | 📄 构建结果汇总 |
| `summarize-tests.yml` | 📋 测试结果汇总 |
| `summarize-deploys.yml` | 🌍 部署结果汇总 |
| `summarize-metadata.yml` | 📊 元数据汇总 |

### 矩阵生成工作流

| 工作流文件 | 描述 |
|------------|------|
| `group-build-targets-by-os.yml` | 🧮 按操作系统分组构建目标 |
| `resolve-deploy-matrix.yml` | 🎯 解析部署矩阵 |
| `build-version-resolver.yml` | 🔢 构建版本解析器 |

## 🚦 触发条件

### 自动触发

- **Push to tags** (`v*.*.*`, `v*.*.*-rc.*`): 触发完整 CI/CD 流水线
- **Pull Request** (ready_for_review, synchronize, reopened): 触发验证流程
- **Push to develop**: 触发简化 CI 流水线
- **Push to main**: 触发完整 CI/CD 流水线

### 手动触发

所有主要工作流都支持 `workflow_dispatch` 手动触发。

## 🔧 设置

### 必需的 GitHub Secrets

#### Unity 相关
- `UNITY_EMAIL`: Unity 账户邮箱
- `UNITY_PASSWORD`: Unity 账户密码  
- `UNITY_LICENSE`: Unity 许可证内容

#### CI/CD 相关
- `CICD_PAT`: GitHub Personal Access Token

#### 部署平台 Secrets (可选)
- `BUTLER_API_KEY`: itch.io API 密钥
- `ITCH_USERNAME`: itch.io 用户名
- `ITCH_PROJECT`: itch.io 项目名
- `APPCENTER_OWNER_NAME`: App Center 所有者名称
- `DEPLOY_API_KEY`: App Center API 密钥
- `FIREBASE_TOKEN`: Firebase 部署令牌
- `AWS_ACCESS_KEY_ID`: AWS 访问密钥 ID
- `AWS_SECRET_ACCESS_KEY`: AWS 秘密访问密钥
- `S3_BUCKET`: S3 存储桶名称
- `STEAM_USERNAME`: Steam 用户名
- `STEAM_PASSWORD`: Steam 密码
- `STEAM_APP_ID`: Steam 应用 ID
- `STEAM_DEPOT_VDF_PATH`: Steam Depot VDF 文件路径
- `APPSTORE_API_KEY_ID`: App Store Connect API 密钥 ID
- `APPSTORE_API_ISSUER_ID`: App Store Connect API 发行者 ID
- `APPSTORE_API_PRIVATE_KEY`: App Store Connect API 私钥
- `CUSTOM_SERVER_HOST`: 自定义服务器主机
- `CUSTOM_SERVER_USER`: 自定义服务器用户
- `CUSTOM_SERVER_KEY`: 自定义服务器 SSH 密钥

#### 通知平台 Secrets (可选)
- `SLACK_WEBHOOK`: Slack Webhook URL
- `DISCORD_WEBHOOK`: Discord Webhook URL

### 仓库变量 (Repository Variables)

| 变量名 | 描述 | 默认值 |
|--------|------|--------|
| `PROJECT_NAME` | 项目名称 | Unity-CI-CD-Template |
| `UNITY_VERSION` | Unity 版本 | 2022.3.58f1 |
| `BUILD_TARGETS` | 构建目标 JSON 数组 | `["StandaloneWindows64","StandaloneOSX"]` |
| `DEPLOY_TARGETS` | 部署目标 JSON 数组 | `[]` |
| `USE_GIT_LFS` | 是否使用 Git LFS | true |
| `UNITY_TESTS_EDITMODE_PATH` | EditMode 测试路径 | Assets/Tests/Editor |
| `UNITY_TESTS_PLAYMODE_PATH` | PlayMode 测试路径 | Assets/Tests/PlayMode |
| `UNITY_TESTS_QUIET_MODE` | 测试静默模式 | false |
| `EXCLUDE_UNITY_TESTS` | 排除 Unity 测试 | false |
| `TIMEOUT_MINUTES_TESTS` | 测试超时时间 | 30 |
| `TIMEOUT_MINUTES_BUILD` | 构建超时时间 | 60 |
| `RETENTION_DAYS_RELEASE` | Release 产物保留天数 | 90 |
| `RETENTION_DAYS_RC` | RC 产物保留天数 | 30 |
| `RETENTION_DAYS_PREVIEW` | Preview 产物保留天数 | 7 |

## 📋 Commit关键字

- `[SKIP CI]`: 跳过 CI/CD 流程

## 🎛️ 参数解析与一览

### 构建目标 (Build Targets)

支持的 Unity 构建目标：
- `StandaloneWindows64` - Windows 64位
- `StandaloneOSX` - macOS
- `StandaloneLinux64` - Linux 64位  
- `WebGL` - WebGL
- `iOS` - iOS
- `Android` - Android

### 部署目标 (Deploy Targets)

支持的部署平台：
- `gh-pages` - GitHub Pages (WebGL)
- `itch.io` - itch.io 游戏平台
- `appcenter` - Microsoft App Center (移动端)
- `firebase` - Firebase Hosting (WebGL)
- `s3` - AWS S3 (WebGL)
- `steam` - Steam (桌面端)
- `testflight` - Apple TestFlight (iOS)
- `custom-server` - 自定义服务器

### 构建类型 (Build Types)

- `preview` - 预览构建 (无标签)
- `release_candidate` - 候选发布版本 (rc 标签)
- `release` - 正式发布版本 (正式标签)

### 版本号格式

- 自动版本: `0.1.YYYY.MM.DD.RUN_NUMBER`
- 手动版本: `vX.Y.Z` 或 `vX.Y.Z-rc.N`

### 默认配置文件

配置文件位置: `.github/config/ci-defaults.json`

```json
{
  "metadataConfig": {
    "projectName": "Unity-CI-CD-Template",
    "skipTests": false,
    "testsOnly": false,
    "buildType": "release",
    "buildVersion": "0.1.${{ github.event.date | fromJson | format('YYYY.MM.DD') }}.${{ github.run_number }}",
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

## 🔄 工作流程序

1. **触发阶段**: 根据 Git 事件自动触发或手动触发
2. **验证阶段**: 检查配置、Secrets 和跳过条件
3. **元数据准备**: 解析构建配置和参数
4. **版本标记**: 创建或验证 Git 标签
5. **测试执行**: 并行运行 EditMode 和 PlayMode 测试
6. **构建执行**: 按操作系统分组并行构建多个目标平台
7. **发布创建**: 创建 GitHub Release 并上传产物
8. **部署执行**: 部署到指定的目标平台
9. **结果通知**: 发送构建结果到 Slack/Discord

## 🚨 故障排除

### 常见问题

- 待补充