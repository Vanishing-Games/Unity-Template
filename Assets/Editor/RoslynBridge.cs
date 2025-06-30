using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class RoslynBridge
{
    static readonly string configRelativePath = Path.Combine("Settings", ".roslynbridgeconfig");
    static readonly string configFilePath;

    static RoslynBridge()
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

        string dotnetExe = GetDotnetExecutablePath();
        if (string.IsNullOrEmpty(dotnetExe))
        {
            Logger.EditorLogError(
                "环境检测失败: 未找到 dotnet 命令.\n你可以在项目根目录创建 Settings/.roslynbridgeconfig 文件, 内容写入 dotnet 绝对路径, 或确保系统 PATH 中包含 dotnet.",
                LogTag.Roslyn
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
                LogTag.Roslyn
            );
            return;
        }

        var scriptsPath = Path.Combine(Application.dataPath, "Scripts");

        var process = new Process();
        process.StartInfo.FileName = dotnetExe;
        process.StartInfo.Arguments = $"\"{dllPath}\" \"{scriptsPath}\"";
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
                Logger.EditorLogInfo($"{e.Data}", LogTag.Roslyn);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Logger.EditorLogError($"{e.Data}", LogTag.Roslyn);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"运行失败: {ex.Message}", LogTag.Roslyn);
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
                        $"配置文件 Settings/.roslynbridgeconfig 中的 dotnet 路径无效或不存在\n绝对路径: {configFilePath}",
                        LogTag.Roslyn
                    );
                }
                else
                {
                    Logger.EditorLogWarn(
                        $"配置文件 Settings/.roslynbridgeconfig 中的 dotnet 为空!",
                        LogTag.Roslyn
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.EditorLogWarn(
                    $"读取配置文件 Settings/.roslynbridgeconfig 出错: {ex.Message}\n绝对路径: {configFilePath}",
                    LogTag.Roslyn
                );
            }
        }
        else
        {
            Logger.EditorLogWarn(
                $"配置文件 Settings/.roslynbridgeconfig 不存在, 将尝试自动查找 dotnet.\n绝对路径: {configFilePath}",
                LogTag.Roslyn
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
