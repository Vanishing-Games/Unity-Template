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
    #region Public
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
        ExecuteCodeUnfucker("format", filePath);
        ExecuteCSharpierFormatting(filePath);
    }

    public static void FormatCodeDirectory(string directoryPath)
    {
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

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string dllPath = Path.Combine(
            projectRoot,
            "CodeUnfucker",
            "bin",
            "Debug",
            "net9.0",
            "CodeUnfucker.dll"
        );
        if (!File.Exists(dllPath))
        {
            Logger.EditorLogWarn(
                $"分析器工具未找到: {dllPath}\n请先运行 dotnet build\n或者运行 Scripts/构建CodeUnfucker.bat 脚本",
                LogTag.CodeUnfucker
            );
            return;
        }

        var process = new Process();
        process.StartInfo.FileName = dotnetExe;
        process.StartInfo.Arguments = $"\"{dllPath}\" {command} \"{path}\"";
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
    #endregion

    #region Private
    static CodeUnfuckerBridge()
    {
        configFilePath = Path.Combine(
            Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
            configRelativePath
        );
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }

    static void OnCompilationFinished(object obj)
    {
        if (Application.isBatchMode)
            return;
        var scriptsPath = Path.Combine(Application.dataPath, "Scripts");
        ExecuteCodeUnfucker("analyze", scriptsPath);
    }

    static string GetDotnetExecutablePath()
    {
        if (File.Exists(configFilePath))
        {
            try
            {
                string json = File.ReadAllText(configFilePath);
                var config = JsonUtility.FromJson<CodeUnfuckerWindow.CodeUnfuckerConfig>(json);
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
            }
            catch (Exception ex)
            {
                Logger.EditorLogWarn(
                    $"读取配置文件 ProjectConfig/CodeUnfuckerConfig.json 出错: {ex.Message}\n绝对路径: {configFilePath}",
                    LogTag.CodeUnfucker
                );
            }
        }
        else
        {
            Logger.EditorLogInfo(
                $"配置文件 ProjectConfig/CodeUnfuckerConfig.json 不存在, 将尝试自动查找 dotnet.\n绝对路径: {configFilePath}",
                LogTag.CodeUnfucker
            );
        }

        return FindExecutableInPath("dotnet");
    }

    static bool IsDefaultDotnet(string dotnetPath)
    {
        return Path.IsPathRooted(dotnetPath) && dotnetPath.Contains("dotnet");
    }

    static string FindExecutableInPath(string exeName)
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

    static void ExecuteCSharpierFormatting(string filePath)
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
            process.StartInfo.WorkingDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..")
            );
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

    static void ExecuteCSharpierFormattingForDirectory(string directoryPath)
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
            process.StartInfo.WorkingDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..")
            );
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
    static readonly string configRelativePath = Path.Combine(
        "ProjectConfig",
        "CodeUnfuckerConfig.json"
    );
    static readonly string configFilePath;
}
