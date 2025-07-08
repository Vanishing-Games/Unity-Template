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
    static readonly string configRelativePath = Path.Combine(
        "Settings",
        ".codeunfuckerbridgeconfig"
    );
    static readonly string configFilePath;

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

    [MenuItem("CodeUnfucker/Format Code")]
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
    }

    public static void FormatCodeDirectory(string directoryPath)
    {
        ExecuteCodeUnfucker("format", directoryPath);
    }

    private static void ExecuteCodeUnfucker(string command, string path)
    {
        string dotnetExe = GetDotnetExecutablePath();

        if (string.IsNullOrEmpty(dotnetExe))
        {
            Logger.EditorLogError(
                "环境检测失败: 未找到 dotnet 命令.\n你可以在项目根目录创建 Settings/.codeunfuckerbridgeconfig 文件, 内容写入 dotnet 绝对路径, 或确保系统 PATH 中包含 dotnet.",
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
                // 格式化完成后刷新Asset Database
                AssetDatabase.Refresh();
                Logger.EditorLogInfo("代码格式化完成，已刷新Asset Database", LogTag.CodeUnfucker);
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"运行失败: {ex.Message}", LogTag.CodeUnfucker);
        }
    }

    static string GetDotnetExecutablePath()
    {
        if (File.Exists(configFilePath))
        {
            try
            {
                string configDotnetPath = File.ReadAllText(configFilePath).Trim();
                if (!string.IsNullOrEmpty(configDotnetPath) && File.Exists(configDotnetPath))
                {
                    return configDotnetPath;
                }
                else if (!string.IsNullOrEmpty(configDotnetPath))
                {
                    Logger.EditorLogWarn(
                        $"配置文件 Settings/.codeunfuckerbridgeconfig 中的 dotnet 路径无效或不存在\n绝对路径: {configFilePath}",
                        LogTag.CodeUnfucker
                    );
                }
                else
                {
                    Logger.EditorLogWarn(
                        $"配置文件 Settings/.codeunfuckerbridgeconfig 中的 dotnet 为空!",
                        LogTag.CodeUnfucker
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.EditorLogWarn(
                    $"读取配置文件 Settings/.codeunfuckerbridgeconfig 出错: {ex.Message}\n绝对路径: {configFilePath}",
                    LogTag.CodeUnfucker
                );
            }
        }
        else
        {
            Logger.EditorLogWarn(
                $"配置文件 Settings/.codeunfuckerbridgeconfig 不存在, 将尝试自动查找 dotnet.\n绝对路径: {configFilePath}",
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
}
