using System;
using System.IO;
using Core;
using UnityEditor;
using UnityEngine;
using Logger = Core.Logger;

/// <summary>
/// CodeUnfucker 配置管理器
/// 统一管理配置的加载、保存和验证
/// </summary>
public static class CodeUnfuckerConfigManager
{
    #region Constants
    private const string CONFIG_FOLDER_NAME = "ProjectConfig";
    private const string CONFIG_FILE_NAME = "CodeUnfuckerConfig.json";
    #endregion

    #region Static Fields
    private static readonly string s_projectRoot;
    private static readonly string s_configFolderPath;
    private static readonly string s_configFilePath;
    private static CodeUnfuckerConfig s_cachedConfig;
    private static DateTime s_lastConfigLoadTime;
    #endregion

    #region Initialization
    static CodeUnfuckerConfigManager()
    {
        s_projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        s_configFolderPath = Path.Combine(s_projectRoot, CONFIG_FOLDER_NAME);
        s_configFilePath = Path.Combine(s_configFolderPath, CONFIG_FILE_NAME);
        
        Logger.EditorLogInfo($"CodeUnfucker Config Manager 初始化完成", LogTag.CodeUnfucker);
        Logger.EditorLogInfo($"配置路径: {s_configFilePath}", LogTag.CodeUnfucker);
    }
    #endregion

    #region Public API
    /// <summary>
    /// 获取当前配置
    /// </summary>
    public static CodeUnfuckerConfig GetConfig()
    {
        // 检查配置文件是否被修改
        if (File.Exists(s_configFilePath))
        {
            DateTime lastWriteTime = File.GetLastWriteTime(s_configFilePath);
            if (s_cachedConfig == null || lastWriteTime > s_lastConfigLoadTime)
            {
                s_cachedConfig = LoadConfigFromFile();
                s_lastConfigLoadTime = lastWriteTime;
            }
        }
        else if (s_cachedConfig == null)
        {
            s_cachedConfig = CreateDefaultConfig();
        }

        return s_cachedConfig;
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    public static bool SaveConfig(CodeUnfuckerConfig config)
    {
        try
        {
            // 确保配置目录存在
            if (!Directory.Exists(s_configFolderPath))
            {
                Directory.CreateDirectory(s_configFolderPath);
            }

            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(s_configFilePath, json);
            
            // 更新缓存
            s_cachedConfig = config;
            s_lastConfigLoadTime = File.GetLastWriteTime(s_configFilePath);
            
            Logger.EditorLogInfo($"配置已保存到: {s_configFilePath}", LogTag.CodeUnfucker);
            return true;
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"保存配置失败: {ex.Message}", LogTag.CodeUnfucker);
            return false;
        }
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public static CodeUnfuckerConfig ResetToDefault()
    {
        var defaultConfig = CreateDefaultConfig();
        SaveConfig(defaultConfig);
        return defaultConfig;
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    public static string GetConfigFilePath()
    {
        return s_configFilePath;
    }

    /// <summary>
    /// 获取配置文件夹路径
    /// </summary>
    public static string GetConfigFolderPath()
    {
        return s_configFolderPath;
    }

    /// <summary>
    /// 检查配置是否有效
    /// </summary>
    public static bool ValidateConfig(CodeUnfuckerConfig config)
    {
        if (config == null)
            return false;

        if (config.dotnetPaths == null)
            return false;

        // 检查是否有至少一个有效的路径配置
        bool hasValidPath = false;
        
        // 检查环境变量
        if (config.dotnetPaths.environmentVariables != null && config.dotnetPaths.environmentVariables.Count > 0)
        {
            hasValidPath = true;
        }
        
        // 检查默认搜索路径
        if (config.dotnetPaths.defaultSearchPaths != null && config.dotnetPaths.defaultSearchPaths.Count > 0)
        {
            hasValidPath = true;
        }
        
        // 检查自定义路径
        if (config.dotnetPaths.customPaths != null && config.dotnetPaths.customPaths.Count > 0)
        {
            hasValidPath = true;
        }

        return hasValidPath;
    }

    /// <summary>
    /// 打开配置文件夹
    /// </summary>
    public static void OpenConfigFolder()
    {
        if (Directory.Exists(s_configFolderPath))
        {
            EditorUtility.RevealInFinder(s_configFolderPath);
            Logger.EditorLogInfo($"已打开配置文件夹: {s_configFolderPath}", LogTag.CodeUnfucker);
        }
        else
        {
            Logger.EditorLogWarn("配置文件夹不存在，将创建默认配置", LogTag.CodeUnfucker);
            ResetToDefault();
            EditorUtility.RevealInFinder(s_configFolderPath);
        }
    }
    #endregion

    #region Private Methods
    private static CodeUnfuckerConfig LoadConfigFromFile()
    {
        if (!File.Exists(s_configFilePath))
        {
            Logger.EditorLogInfo("配置文件不存在，创建默认配置", LogTag.CodeUnfucker);
            return CreateDefaultConfig();
        }

        try
        {
            string json = File.ReadAllText(s_configFilePath);
            var config = JsonUtility.FromJson<CodeUnfuckerConfig>(json);
            
            if (ValidateConfig(config))
            {
                Logger.EditorLogInfo("配置加载成功", LogTag.CodeUnfucker);
                return config;
            }
            else
            {
                Logger.EditorLogWarn("配置文件格式无效，使用默认配置", LogTag.CodeUnfucker);
                return CreateDefaultConfig();
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"加载配置文件失败: {ex.Message}", LogTag.CodeUnfucker);
            return CreateDefaultConfig();
        }
    }

    private static CodeUnfuckerConfig CreateDefaultConfig()
    {
        var config = new CodeUnfuckerConfig();
        
        // 设置默认的环境变量
        config.dotnetPaths.environmentVariables = new System.Collections.Generic.List<string>
        {
            "DOTNET_ROOT",
            "DOTNET_CLI_HOME"
        };

        // 设置默认的搜索路径
        config.dotnetPaths.defaultSearchPaths = new System.Collections.Generic.List<string>
        {
            "/opt/homebrew/bin/dotnet", // Homebrew on Apple Silicon
            "/usr/local/bin/dotnet", // Homebrew on Intel Mac
            "/usr/local/share/dotnet/dotnet", // Microsoft installer
            "/usr/bin/dotnet", // System installation
            "dotnet", // 如果在 PATH 中
        };

        // 初始化自定义路径列表
        config.dotnetPaths.customPaths = new System.Collections.Generic.List<string>();

        Logger.EditorLogInfo("已创建默认配置", LogTag.CodeUnfucker);
        return config;
    }
    #endregion
}