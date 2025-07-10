using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Core;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Logger = Core.Logger;

[InitializeOnLoad]
public static class CodeUnfuckerBridge
{
    #region Constants
    private const string CONFIG_FOLDER_NAME = "ProjectConfig";
    private const string CONFIG_FILE_NAME = "CodeUnfuckerConfig.json";
    private const string CODEUNFUCKER_PROJECT_NAME = "CodeUnfucker";
    private const string CODEUNFUCKER_DLL_PATH = "bin/Debug/net9.0/CodeUnfucker.dll";
    #endregion

    #region Static Fields
    private static readonly string s_projectRoot;
    private static readonly string s_configFolderPath;
    private static readonly string s_configFilePath;
    private static readonly string s_codeUnfuckerProjectPath;
    private static readonly string s_codeUnfuckerDllPath;
    #endregion

    #region Initialization
    static CodeUnfuckerBridge()
    {
        s_projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        s_configFolderPath = Path.Combine(s_projectRoot, CONFIG_FOLDER_NAME);
        s_configFilePath = Path.Combine(s_configFolderPath, CONFIG_FILE_NAME);
        s_codeUnfuckerProjectPath = Path.Combine(s_projectRoot, CODEUNFUCKER_PROJECT_NAME);
        s_codeUnfuckerDllPath = Path.Combine(s_codeUnfuckerProjectPath, CODEUNFUCKER_DLL_PATH);
        
        CompilationPipeline.compilationFinished += OnCompilationFinished;
        
        Logger.EditorLogInfo($"CodeUnfucker Bridge 初始化完成", LogTag.CodeUnfucker);
        Logger.EditorLogInfo($"配置路径: {s_configFilePath}", LogTag.CodeUnfucker);
    }
    #endregion

    #region Public API
    [MenuItem("Tools/CodeUnfucker/Open CodeUnfucker Window")]
    public static void OpenCodeUnfuckerWindow()
    {
        CodeUnfuckerWindow.OpenWindow();
    }

    [MenuItem("Tools/CodeUnfucker/Format Selected", false, 20)]
    public static void FormatSelectedCode()
    {
        var selection = Selection.objects;
        if (selection.Length == 0)
        {
            Logger.EditorLogWarn("请选择要格式化的文件或文件夹", LogTag.CodeUnfucker);
            return;
        }

        foreach (var obj in selection)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath))
                continue;
                
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (File.Exists(fullPath) && fullPath.EndsWith(".cs"))
            {
                FormatCodeFile(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                FormatCodeDirectory(fullPath);
            }
        }
    }

    public static void FormatCodeFile(string filePath)
    {
        if (!ValidateCodeUnfuckerSetup())
            return;
            
        ExecuteCodeUnfucker("format", filePath);
        ExecuteCSharpierFormatting(filePath);
    }

    public static void FormatCodeDirectory(string directoryPath)
    {
        if (!ValidateCodeUnfuckerSetup())
            return;
            
        ExecuteCodeUnfucker("format", directoryPath);
        ExecuteCSharpierFormattingForDirectory(directoryPath);
    }

    public static void ExecuteCodeUnfucker(string command, string path)
    {
        string dotnetExe = GetDotnetExecutablePath();
        if (string.IsNullOrEmpty(dotnetExe))
        {
            Logger.EditorLogError(
                "环境检测失败: 未找到 dotnet 命令.\n请使用 CodeUnfucker 窗口配置 dotnet 路径，或确保系统 PATH 中包含 dotnet.",
                LogTag.CodeUnfucker
            );
            return;
        }

        if (!File.Exists(s_codeUnfuckerDllPath))
        {
            Logger.EditorLogWarn(
                $"分析器工具未找到: {s_codeUnfuckerDllPath}\n请先运行 dotnet build\n或者运行 Scripts/构建CodeUnfucker.bat 脚本",
                LogTag.CodeUnfucker
            );
            return;
        }

        var process = new Process();
        process.StartInfo.FileName = dotnetExe;
        process.StartInfo.Arguments = $"\"{s_codeUnfuckerDllPath}\" {command} \"{path}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        
        if (IsDefaultDotnet(dotnetExe))
        {
            string dotnetDir = Path.GetDirectoryName(dotnetExe);
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!currentPath.Split(Path.PathSeparator).Contains(dotnetDir))
            {
                currentPath = dotnetDir + Path.PathSeparator + currentPath;
                process.StartInfo.EnvironmentVariables["PATH"] = currentPath;
            }
        }

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Logger.EditorLogInfo($"{e.Data}", LogTag.CodeUnfucker);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Logger.EditorLogError($"{e.Data}", LogTag.CodeUnfucker);
        };
        
        try
        {
            Logger.EditorLogInfo($"执行 CodeUnfucker {command} 命令: {path}", LogTag.CodeUnfucker);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            
            if (command == "format")
            {
                AssetDatabase.Refresh();
                Logger.EditorLogInfo("代码格式化完成，已刷新Asset Database", LogTag.CodeUnfucker);
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"运行失败: {ex.Message}", LogTag.CodeUnfucker);
        }
    }

    public static string GetDotnetExecutablePath()
    {
        var config = CodeUnfuckerConfigManager.GetConfig();
        
        // 1. 检查环境变量
        foreach (var envVar in config.dotnetPaths.environmentVariables)
        {
            string envPath = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            {
                return envPath;
            }
        }

        // 2. 检查自定义路径
        foreach (var customPath in config.dotnetPaths.customPaths)
        {
            if (File.Exists(customPath))
            {
                return customPath;
            }
        }

        // 3. 检查默认搜索路径
        foreach (var defaultPath in config.dotnetPaths.defaultSearchPaths)
        {
            if (defaultPath == "dotnet")
            {
                string foundPath = FindExecutableInPath("dotnet");
                if (!string.IsNullOrEmpty(foundPath))
                {
                    return foundPath;
                }
            }
            else if (File.Exists(defaultPath))
            {
                return defaultPath;
            }
        }

        return FindExecutableInPath("dotnet");
    }

    public static string GetConfigFilePath()
    {
        return CodeUnfuckerConfigManager.GetConfigFilePath();
    }

    public static string GetConfigFolderPath()
    {
        return CodeUnfuckerConfigManager.GetConfigFolderPath();
    }

    public static string GetCodeUnfuckerProjectPath()
    {
        return s_codeUnfuckerProjectPath;
    }

    public static string GetCodeUnfuckerDllPath()
    {
        return s_codeUnfuckerDllPath;
    }
    #endregion

    #region Private Methods
    private static void OnCompilationFinished(object obj)
    {
        if (Application.isBatchMode)
            return;
            
        var scriptsPath = Path.Combine(Application.dataPath, "Scripts");
        ExecuteCodeUnfucker("analyze", scriptsPath);
    }

    private static bool ValidateCodeUnfuckerSetup()
    {
        if (!Directory.Exists(s_codeUnfuckerProjectPath))
        {
            Logger.EditorLogError($"CodeUnfucker 项目目录不存在: {s_codeUnfuckerProjectPath}", LogTag.CodeUnfucker);
            return false;
        }

        if (!File.Exists(s_codeUnfuckerDllPath))
        {
            Logger.EditorLogWarn(
                $"CodeUnfucker DLL 未找到: {s_codeUnfuckerDllPath}\n请先构建 CodeUnfucker 项目",
                LogTag.CodeUnfucker
            );
            return false;
        }

        return true;
    }

    private static bool IsDefaultDotnet(string dotnetPath)
    {
        return Path.IsPathRooted(dotnetPath) && dotnetPath.Contains("dotnet");
    }

    private static string FindExecutableInPath(string exeName)
    {
        string pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;
            
        string[] paths = pathEnv.Split(Path.PathSeparator);
        string[] extensions =
            Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new[] { ".exe", ".bat", ".cmd", "" }
                : new[] { "" };
                
        foreach (var path in paths)
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(path, exeName + ext);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }

    private static void ExecuteCSharpierFormatting(string filePath)
    {
        try
        {
            Logger.EditorLogInfo(
                $"🎨 CSharpier 格式化文件: {Path.GetFileName(filePath)}",
                LogTag.CodeUnfucker
            );
            
            string dotnetPath = GetDotnetExecutablePath();
            if (string.IsNullOrEmpty(dotnetPath))
            {
                Logger.EditorLogWarn(
                    "未找到 dotnet 路径，跳过 CSharpier 格式化",
                    LogTag.CodeUnfucker
                );
                return;
            }

            var process = new Process();
            process.StartInfo.FileName = dotnetPath;
            process.StartInfo.Arguments = $"csharpier \"{filePath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = s_projectRoot;
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Logger.EditorLogInfo($"[CSharpier] {e.Data}", LogTag.CodeUnfucker);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Logger.EditorLogWarn($"[CSharpier] {e.Data}", LogTag.CodeUnfucker);
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            
            if (process.ExitCode == 0)
            {
                Logger.EditorLogInfo(
                    $"✅ CSharpier 格式化完成: {Path.GetFileName(filePath)}",
                    LogTag.CodeUnfucker
                );
            }
            else
            {
                Logger.EditorLogWarn(
                    $"⚠️ CSharpier 格式化警告，退出代码: {process.ExitCode}",
                    LogTag.CodeUnfucker
                );
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError(
                $"CSharpier 格式化失败 {filePath}: {ex.Message}",
                LogTag.CodeUnfucker
            );
        }
    }

    private static void ExecuteCSharpierFormattingForDirectory(string directoryPath)
    {
        try
        {
            Logger.EditorLogInfo($"🎨 CSharpier 格式化目录: {directoryPath}", LogTag.CodeUnfucker);
            
            string dotnetPath = GetDotnetExecutablePath();
            if (string.IsNullOrEmpty(dotnetPath))
            {
                Logger.EditorLogWarn(
                    "未找到 dotnet 路径，跳过 CSharpier 格式化",
                    LogTag.CodeUnfucker
                );
                return;
            }

            var process = new Process();
            process.StartInfo.FileName = dotnetPath;
            process.StartInfo.Arguments = $"csharpier \"{directoryPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = s_projectRoot;
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Logger.EditorLogInfo($"[CSharpier] {e.Data}", LogTag.CodeUnfucker);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Logger.EditorLogWarn($"[CSharpier] {e.Data}", LogTag.CodeUnfucker);
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            
            if (process.ExitCode == 0)
            {
                Logger.EditorLogInfo($"✅ CSharpier 目录格式化完成", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogWarn(
                    $"⚠️ CSharpier 目录格式化警告，退出代码: {process.ExitCode}",
                    LogTag.CodeUnfucker
                );
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError(
                $"CSharpier 目录格式化失败 {directoryPath}: {ex.Message}",
                LogTag.CodeUnfucker
            );
        }
    }
    #endregion
}
