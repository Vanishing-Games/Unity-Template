{
  "metadataConfig": {
    "projectName": "Unity-CI-CD-Template",
    "skipTests": false,
    "testsOnly": false,
    "buildType": "release", 
    "buildVersion": "0.0.0", // TODO
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
    "requiresCombined": true, // TODO
    "skipPerBuildTarget": false // TODO
  }
}

# buildType

1. release
正式发布版，用于最终交付用户或上线的版本。
通常会开启所有优化，关闭调试信息和日志。
版本稳定，经过完整测试。

2. preview
预览版/测试版，用于内部测试或提前体验新功能。
可能包含未完成或实验性功能。
稳定性和完整性不如 release，主要面向测试人员或开发团队。

3. release_candidate
候选发布版，即将成为正式 release 的版本。
功能和 release 基本一致，但还在做最后的测试和验证。
如果没有重大问题，会直接作为正式版发布。

# buildTarget

配置在: .github/config/build-targets.json

# deploy-targets

配置在: .github/config/deploy-targets.json