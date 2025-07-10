using System;
using Core;
using UnityEditor;
using UnityEngine;
using Logger = Core.Logger;

/// <summary>
/// CodeUnfucker 配置测试脚本
/// 用于验证配置管理器的功能
/// </summary>
public static class CodeUnfuckerConfigTest
{
    [MenuItem("Tools/CodeUnfucker/Test Config Manager")]
    public static void TestConfigManager()
    {
        Logger.EditorLogInfo("开始测试 CodeUnfucker 配置管理器...", LogTag.CodeUnfucker);
        
        try
        {
            // 测试1: 获取配置
            Logger.EditorLogInfo("测试1: 获取配置", LogTag.CodeUnfucker);
            var config1 = CodeUnfuckerConfigManager.GetConfig();
            if (config1 != null && config1.dotnetPaths != null)
            {
                Logger.EditorLogInfo("✅ 配置获取成功", LogTag.CodeUnfucker);
                Logger.EditorLogInfo($"环境变量数量: {config1.dotnetPaths.environmentVariables?.Count ?? 0}", LogTag.CodeUnfucker);
                Logger.EditorLogInfo($"默认路径数量: {config1.dotnetPaths.defaultSearchPaths?.Count ?? 0}", LogTag.CodeUnfucker);
                Logger.EditorLogInfo($"自定义路径数量: {config1.dotnetPaths.customPaths?.Count ?? 0}", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError("❌ 配置获取失败", LogTag.CodeUnfucker);
                return;
            }

            // 测试2: 验证配置
            Logger.EditorLogInfo("测试2: 验证配置", LogTag.CodeUnfucker);
            bool isValid = CodeUnfuckerConfigManager.ValidateConfig(config1);
            if (isValid)
            {
                Logger.EditorLogInfo("✅ 配置验证通过", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError("❌ 配置验证失败", LogTag.CodeUnfucker);
                return;
            }

            // 测试3: 保存配置
            Logger.EditorLogInfo("测试3: 保存配置", LogTag.CodeUnfucker);
            bool saveSuccess = CodeUnfuckerConfigManager.SaveConfig(config1);
            if (saveSuccess)
            {
                Logger.EditorLogInfo("✅ 配置保存成功", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError("❌ 配置保存失败", LogTag.CodeUnfucker);
                return;
            }

            // 测试4: 重置为默认配置
            Logger.EditorLogInfo("测试4: 重置为默认配置", LogTag.CodeUnfucker);
            var defaultConfig = CodeUnfuckerConfigManager.ResetToDefault();
            if (defaultConfig != null && CodeUnfuckerConfigManager.ValidateConfig(defaultConfig))
            {
                Logger.EditorLogInfo("✅ 默认配置重置成功", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError("❌ 默认配置重置失败", LogTag.CodeUnfucker);
                return;
            }

            // 测试5: 路径获取
            Logger.EditorLogInfo("测试5: 路径获取", LogTag.CodeUnfucker);
            string configPath = CodeUnfuckerConfigManager.GetConfigFilePath();
            string configFolder = CodeUnfuckerConfigManager.GetConfigFolderPath();
            
            if (!string.IsNullOrEmpty(configPath) && !string.IsNullOrEmpty(configFolder))
            {
                Logger.EditorLogInfo($"✅ 配置路径获取成功", LogTag.CodeUnfucker);
                Logger.EditorLogInfo($"配置文件路径: {configPath}", LogTag.CodeUnfucker);
                Logger.EditorLogInfo($"配置文件夹路径: {configFolder}", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError("❌ 配置路径获取失败", LogTag.CodeUnfucker);
                return;
            }

            // 测试6: Bridge 集成测试
            Logger.EditorLogInfo("测试6: Bridge 集成测试", LogTag.CodeUnfucker);
            string bridgeConfigPath = CodeUnfuckerBridge.GetConfigFilePath();
            string bridgeConfigFolder = CodeUnfuckerBridge.GetConfigFolderPath();
            
            if (bridgeConfigPath == configPath && bridgeConfigFolder == configFolder)
            {
                Logger.EditorLogInfo("✅ Bridge 集成测试通过", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError("❌ Bridge 集成测试失败", LogTag.CodeUnfucker);
                return;
            }

            Logger.EditorLogInfo("🎉 所有测试通过！CodeUnfucker 配置管理器工作正常", LogTag.CodeUnfucker);
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"测试过程中发生异常: {ex.Message}", LogTag.CodeUnfucker);
        }
    }

    [MenuItem("Tools/CodeUnfucker/Test Dotnet Path Detection")]
    public static void TestDotnetPathDetection()
    {
        Logger.EditorLogInfo("开始测试 dotnet 路径检测...", LogTag.CodeUnfucker);
        
        try
        {
            string dotnetPath = CodeUnfuckerBridge.GetDotnetExecutablePath();
            if (!string.IsNullOrEmpty(dotnetPath))
            {
                Logger.EditorLogInfo($"✅ 检测到 dotnet 路径: {dotnetPath}", LogTag.CodeUnfucker);
                
                // 验证路径是否有效
                if (System.IO.File.Exists(dotnetPath))
                {
                    Logger.EditorLogInfo("✅ dotnet 路径有效", LogTag.CodeUnfucker);
                }
                else
                {
                    Logger.EditorLogWarn("⚠️ dotnet 路径无效", LogTag.CodeUnfucker);
                }
            }
            else
            {
                Logger.EditorLogWarn("⚠️ 未检测到 dotnet 路径", LogTag.CodeUnfucker);
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"dotnet 路径检测过程中发生异常: {ex.Message}", LogTag.CodeUnfucker);
        }
    }
}