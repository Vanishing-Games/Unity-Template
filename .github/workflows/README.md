# 工作流概览

本项目采用模块化的 CI/CD 系统，支持 Unity 项目的自动化测试、构建、发布和部署。

## 🏗️ 系统架构

### 主要流水线

| 工作流文件 | 描述 | 触发条件 |
|------------|------|----------|
| `ci-cd-dispatcher.yml` | ⚙️ CI/CD 智能分发器 | Push to tags, Pull Request, Release分支 |
| `ci-cd-pipeline.yml` | 🚀 完整 CI/CD 流水线 | 手动触发，分发器调用 |
| `ci-pipeline.yml` | 🚀 CI 流水线 | 手动触发，develop分支，分发器调用 |

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

#### CI流水线 (仅测试+构建)
- **Pull Request**: develop、main分支的PR创建、更新、重开、ready_for_review
- **Push**: develop分支推送
- **Commit关键字**: `[TEST ONLY]` - 强制仅执行测试

#### 完整CI/CD流水线 (测试+构建+发布+部署)
- **标签推送**: 
  - `v*.*.*` (正式发布版本，如 v1.0.0)
  - `v*.*.*-rc.*` (候选发布版本，如 v1.0.0-rc.1)
- **分支推送**: `release/**` 分支

#### 跳过流程
- **Commit关键字**: 
  - `[SKIP CICD]` - 完全跳过所有CI/CD流程
  - `[SKIP CI]` - (向后兼容) 跳过所有CI/CD流程

### 手动触发

所有主要工作流都支持 `workflow_dispatch` 手动触发，并提供丰富的参数配置。

## 🔧 设置

### 必需的 GitHub Secrets

#### Unity 相关
- `UNITY_EMAIL`: Unity 账户邮箱
- `UNITY_PASSWORD`: Unity 账户密码  
- `UNITY_LICENSE`: Unity 许可证内容

#### CI/CD 相关
- `CICD_PAT`: GitHub Personal Access Token (需要 contents:write, actions:write 权限)

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
| `BUILD_TARGETS` | 构建目标 JSON 数组 | `["WebGL","StandaloneWindows64","StandaloneOSX"]` |
| `DEPLOY_TARGETS` | 部署目标 JSON 数组 | `["gh-pages"]` |
| `BUILD_VERSION` | 自定义构建版本 | 空 (使用自动生成) |

> 💡 **提示**: 其他配置项都已集中到 `.github/config/` 目录中的配置文件中管理。

## 📋 Commit关键字

在commit消息中使用以下关键字来控制CI/CD行为：

- `[SKIP CICD]` - 完全跳过CI/CD流程
- `[SKIP CI]` - (向后兼容) 跳过CI/CD流程  
- `[TEST ONLY]` - 仅执行测试，跳过构建和部署

### 使用示例

```bash
git commit -m "fix: 修复用户登录问题 [TEST ONLY]"
git commit -m "docs: 更新README文档 [SKIP CI]"
git commit -m "feat: 添加新功能"  # 正常触发CI
```

## 🎛️ 参数解析与一览

### 构建目标 (Build Targets)

支持的 Unity 构建平台：
- `StandaloneWindows64` - Windows 64位
- `StandaloneOSX` - macOS
- `StandaloneLinux64-Client` - Linux 64位客户端  
- `StandaloneLinux64-Server` - Linux 64位服务器
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

- `preview` - 预览构建 (PR、develop分支，无标签)
- `release_candidate` - 候选发布版本 (rc 标签、release分支)
- `release` - 正式发布版本 (release标签)

### 版本号格式

- 自动版本: `0.1.YYYY.MM.DD.RUN_NUMBER`
- 手动版本: `vX.Y.Z` 或 `vX.Y.Z-rc.N`

## 📁 配置文件结构

所有配置文件位于 `.github/config/` 目录：

| 文件名 | 描述 |
|--------|------|
| `defaults.json` | 项目基础配置 |
| `ci-defaults.json` | CI流水线默认参数 |
| `pipeline-config.json` | 流水线触发和行为配置 |
| `build-targets.json` | 构建目标配置 |
| `build-profiles.json` | Unity构建配置文件映射 |
| `deploy-targets.json` | 部署目标配置 |

> 📖 详细配置说明请查看 [.github/config/README.md](.github/config/README.md)

## 🔄 工作流程序

### 智能分发流程

1. **触发检测**: 分析GitHub事件和commit消息
2. **关键字解析**: 检查跳过条件和特殊指令
3. **配置验证**: 验证Secrets和配置文件
4. **流水线选择**: 根据触发条件选择CI或完整CI/CD
5. **元数据准备**: 解析构建配置和参数
6. **版本标记**: 创建或验证Git标签(仅完整CI/CD)
7. **流水线触发**: 调用对应的具体流水线

### CI流水线流程

1. **配置解析**: 从配置文件加载参数
2. **环境验证**: 检查Unity版本和构建目标
3. **测试执行**: 并行运行EditMode和PlayMode测试
4. **构建执行**: 按操作系统分组并行构建多个目标平台
5. **结果汇总**: 生成详细的执行报告

### 完整CI/CD流程

1. 执行CI流水线的所有步骤
2. **发布创建**: 创建GitHub Release并上传产物
3. **部署执行**: 部署到指定的目标平台
4. **结果通知**: 发送构建结果到Slack/Discord

## 🛡️ 错误处理和调试

### 增强的错误信息

- **详细日志**: 所有关键步骤都包含详细的上下文信息
- **配置验证**: 启动前验证所有必需的配置和Secrets
- **失败摘要**: 自动生成包含错误详情的GitHub Step Summary
- **调试模式**: 手动触发时可启用详细调试输出

### 鲁棒性改进

- **配置集中化**: 所有可配置项都在 `.github/config/` 中集中管理
- **格式验证**: 自动验证JSON配置文件格式
- **依赖检查**: 验证必需的Secrets和仓库变量
- **优雅失败**: 明确的错误消息和修复建议

### 调试技巧

1. **查看执行摘要**: 每个工作流都会生成详细的执行报告
2. **启用调试模式**: 手动触发时设置 `enableDebug: true`
3. **检查配置验证**: 查看"🔍 配置验证"步骤的输出
4. **使用commit关键字**: 使用 `[TEST ONLY]` 仅运行测试进行快速验证
5. **验证配置文件**: 确保 `.github/config/` 中的JSON文件格式正确

## 🚨 故障排除

### 常见问题及解决方案

#### 1. 配置相关问题

**问题**: "缺少配置文件"错误
```
❌ 缺少配置文件: .github/config/pipeline-config.json
```
**解决**: 确保所有必需的配置文件都存在于 `.github/config/` 目录中

**问题**: "配置文件格式错误"
```
❌ 配置文件格式错误: defaults.json
```
**解决**: 使用JSON验证器检查文件格式，确保语法正确

#### 2. Secrets相关问题

**问题**: "必需的Secret未设置"
```
❌ 必需的Secret 'UNITY_LICENSE' 未设置
```
**解决**: 在仓库设置中添加缺失的Secrets

#### 3. 触发条件问题

**问题**: 流水线未按预期触发
**解决**: 
- 检查分支名称是否匹配触发条件
- 验证commit消息中是否包含跳过关键字
- 确认文件路径是否在监控范围内

#### 4. 构建失败问题

**问题**: Unity构建失败
**解决**:
- 检查Unity版本是否与项目兼容
- 验证构建目标配置是否正确
- 确认Unity许可证是否有效且未过期

#### 5. 部署失败问题

**问题**: 部署到特定平台失败
**解决**:
- 验证对应平台的Secrets是否正确配置
- 检查构建产物是否与部署目标兼容
- 确认部署目标的最低构建类型要求

### 高级调试

#### 启用详细日志
在手动触发时启用调试模式：
```yaml
enableDebug: true
```

#### 配置测试
使用仅测试模式验证配置：
```bash
git commit -m "test: 验证配置 [TEST ONLY]"
```

#### 检查配置文件
使用jq验证配置文件格式：
```bash
jq empty .github/config/*.json
```

### 支持资源

- **配置文档**: [.github/config/README.md](.github/config/README.md)
- **GitHub Actions日志**: 查看具体的执行步骤和错误信息
- **工作流执行摘要**: 每次运行后的详细报告
- **示例配置**: 参考默认配置文件进行自定义