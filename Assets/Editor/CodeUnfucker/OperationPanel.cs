using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Core;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using static CodeUnfuckerWindow;
using Logger = Core.Logger;

[System.Serializable]
public class OperationPanel
{
    [Title("CodeUnfucker 操作面板", TitleAlignment = TitleAlignments.Centered)]
    [InfoBox("从左侧文件树选择要处理的文件或文件夹，然后选择操作并执行", InfoMessageType.Info)]
    [Space(10)]
    [SerializeField, HideInInspector]
    private bool spacer1;

    [LabelText("当前选中的项目")]
    [ShowInInspector, ReadOnly]
    private List<string> selectedPaths = new List<string>();

    [Space(15)]
    [SerializeField, HideInInspector]
    private bool spacer3;

    [Title("选择操作", TitleAlignment = TitleAlignments.Left)]
    [LabelText("代码格式化")]
    public bool enableFormatting = true;

    [LabelText("代码分析")]
    public bool enableAnalysis = false;

    [Space(10)]
    [SerializeField, HideInInspector]
    private bool spacer2;

    [ShowIf("enableFormatting")]
    [InfoBox(
        "格式化功能会重新排列类成员并添加Region宏，然后使用CSharpier进行最终格式化",
        InfoMessageType.None
    )]
    [LabelText("创建备份文件")]
    public bool createBackup = true;

    [ShowIf("enableAnalysis")]
    [InfoBox("分析功能会输出代码统计信息到控制台", InfoMessageType.None)]
    [LabelText("详细分析")]
    public bool detailedAnalysis = true;

    [Title("执行操作", TitleAlignment = TitleAlignments.Left)]
    [Button("🚀 开始执行选中的操作", ButtonSizes.Large)]
    [GUIColor(0.3f, 0.8f, 0.3f)]
    private void ExecuteOperations()
    {
        if (selectedPaths.Count == 0)
        {
            Logger.EditorLogWarn("请先从左侧文件树选择要处理的文件或文件夹", LogTag.CodeUnfucker);
            return;
        }

        if (!enableFormatting && !enableAnalysis)
        {
            Logger.EditorLogWarn("请至少选择一个操作（格式化或分析）", LogTag.CodeUnfucker);
            return;
        }

        Logger.EditorLogInfo($"开始执行操作，共 {selectedPaths.Count} 个项目", LogTag.CodeUnfucker);
        foreach (var path in selectedPaths)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            if (enableFormatting)
            {
                ExecuteFormatting(fullPath, path);
            }

            if (enableAnalysis)
            {
                ExecuteAnalysis(fullPath, path);
            }
        }

        Logger.EditorLogInfo("✅ 所有操作执行完成", LogTag.CodeUnfucker);
        AssetDatabase.Refresh();
    }

    private void ExecuteFormatting(string fullPath, string assetPath)
    {
        Logger.EditorLogInfo($"🔧 正在格式化: {assetPath}", LogTag.CodeUnfucker);
        // 1. 更新 CodeUnfucker 的备份配置
        UpdateCodeUnfuckerBackupConfig();
        // 2. 执行 CodeUnfucker 格式化
        if (File.Exists(fullPath) && fullPath.EndsWith(".cs"))
        {
            CodeUnfuckerBridge.FormatCodeFile(fullPath);
        }
        else if (Directory.Exists(fullPath))
        {
            CodeUnfuckerBridge.FormatCodeDirectory(fullPath);
        }
    }

    private void ExecuteAnalysis(string fullPath, string assetPath)
    {
        Logger.EditorLogInfo($"📊 正在分析: {assetPath}", LogTag.CodeUnfucker);
        if (Directory.Exists(fullPath))
        {
            CodeUnfuckerBridge.ExecuteCodeUnfucker("analyze", fullPath);
        }
        else if (File.Exists(fullPath))
        {
            // 对于单个文件，分析其所在目录
            string directory = Path.GetDirectoryName(fullPath);
            CodeUnfuckerBridge.ExecuteCodeUnfucker("analyze", directory);
        }
    }

    [HorizontalGroup("SystemOps")]
    [Button("🔨 构建工具", ButtonSizes.Medium)]
    [GUIColor(0.9f, 0.7f, 0.4f)]
    private void BuildCodeUnfucker()
    {
        string codeUnfuckerPath = CodeUnfuckerBridge.GetCodeUnfuckerProjectPath();
        if (!Directory.Exists(codeUnfuckerPath))
        {
            Logger.EditorLogError($"CodeUnfucker 项目目录不存在: {codeUnfuckerPath}", LogTag.CodeUnfucker);
            return;
        }

        try
        {
            string dotnetPath = CodeUnfuckerBridge.GetDotnetExecutablePath();
            if (string.IsNullOrEmpty(dotnetPath))
            {
                Logger.EditorLogError(
                    "未找到 dotnet 命令，请确保已安装 .NET SDK",
                    LogTag.CodeUnfucker
                );
                return;
            }

            var process = new Process();
            process.StartInfo.FileName = dotnetPath;
            process.StartInfo.Arguments = "build";
            process.StartInfo.WorkingDirectory = codeUnfuckerPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Logger.EditorLogInfo($"[BUILD] {e.Data}", LogTag.CodeUnfucker);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Logger.EditorLogError($"[BUILD] {e.Data}", LogTag.CodeUnfucker);
            };
            Logger.EditorLogInfo("开始构建 CodeUnfucker...", LogTag.CodeUnfucker);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                Logger.EditorLogInfo("✅ CodeUnfucker 构建成功", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError(
                    $"❌ CodeUnfucker 构建失败，退出代码: {process.ExitCode}",
                    LogTag.CodeUnfucker
                );
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"构建失败: {ex.Message}", LogTag.CodeUnfucker);
        }
    }

    [HorizontalGroup("SystemOps")]
    [Button("⚙️ 打开配置", ButtonSizes.Medium)]
    [GUIColor(0.7f, 0.7f, 0.9f)]
    private void OpenConfigFolder()
    {
        CodeUnfuckerConfigManager.OpenConfigFolder();
    }

    [Title("配置管理", TitleAlignment = TitleAlignments.Left)]
    [Button("📝 编辑 Dotnet 路径配置", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.7f, 0.9f)]
    private void EditDotnetConfig()
    {
        var config = LoadConfig();
        var configWindow = EditorWindow.GetWindow<DotnetConfigWindow>();
        configWindow.titleContent = new GUIContent("Dotnet 路径配置");
        configWindow.SetConfig(config);
        configWindow.Show();
    }

    [Button("🔍 检测当前 Dotnet 路径", ButtonSizes.Medium)]
    [GUIColor(0.9f, 0.7f, 0.4f)]
    private void DetectCurrentDotnetPath()
    {
        string detectedPath = CodeUnfuckerBridge.GetDotnetExecutablePath();
        if (string.IsNullOrEmpty(detectedPath))
        {
            Logger.EditorLogWarn("未检测到 dotnet 路径", LogTag.CodeUnfucker);
        }
        else
        {
            Logger.EditorLogInfo($"检测到的 dotnet 路径: {detectedPath}", LogTag.CodeUnfucker);
        }
    }

    public string GetDotnetPath()
    {
        return CodeUnfuckerBridge.GetDotnetExecutablePath();
    }

    private CodeUnfuckerConfig LoadConfig()
    {
        return CodeUnfuckerConfigManager.GetConfig();
    }

    private CodeUnfuckerConfig CreateDefaultConfig(string configPath)
    {
        return CodeUnfuckerConfigManager.ResetToDefault();
    }

    private void SaveConfig(CodeUnfuckerConfig config, string configPath = null)
    {
        CodeUnfuckerConfigManager.SaveConfig(config);
    }

    public void UpdateSelectedItems(List<FileTreeItem> items)
    {
        selectedPaths.Clear();
        selectedPaths.AddRange(items.Select(x => x.Path));
    }

    private void UpdateCodeUnfuckerBackupConfig()
    {
        try
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            // 源配置文件路径
            string sourceConfigPath = Path.Combine(
                CodeUnfuckerBridge.GetCodeUnfuckerProjectPath(),
                "Config",
                "FormatterConfig.json"
            );
            // 构建输出目录的配置文件路径
            string outputConfigPath = Path.Combine(
                CodeUnfuckerBridge.GetCodeUnfuckerProjectPath(),
                "bin",
                "Debug",
                "net9.0",
                "Config",
                "FormatterConfig.json"
            );
            bool updated = false;
            string backupValue = createBackup ? "true" : "false";
            string pattern = "\"CreateBackupFiles\"\\s*:\\s*(true|false)";
            string replacement = $"\"CreateBackupFiles\": {backupValue}";
            // 更新源配置文件
            if (File.Exists(sourceConfigPath))
            {
                string jsonContent = File.ReadAllText(sourceConfigPath);
                string updatedContent = System.Text.RegularExpressions.Regex.Replace(
                    jsonContent,
                    pattern,
                    replacement,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                File.WriteAllText(sourceConfigPath, updatedContent);
                updated = true;
                Logger.EditorLogInfo($"已更新源配置文件: {sourceConfigPath}", LogTag.CodeUnfucker);
            }

            // 更新构建输出目录的配置文件
            if (File.Exists(outputConfigPath))
            {
                string jsonContent = File.ReadAllText(outputConfigPath);
                string updatedContent = System.Text.RegularExpressions.Regex.Replace(
                    jsonContent,
                    pattern,
                    replacement,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                File.WriteAllText(outputConfigPath, updatedContent);
                updated = true;
                Logger.EditorLogInfo(
                    $"已更新构建输出配置文件: {outputConfigPath}",
                    LogTag.CodeUnfucker
                );
            }
            else
            {
                Logger.EditorLogWarn(
                    $"构建输出配置文件不存在: {outputConfigPath}\n请先构建 CodeUnfucker 项目",
                    LogTag.CodeUnfucker
                );
            }

            if (updated)
            {
                Logger.EditorLogInfo(
                    $"✅ CodeUnfucker 备份配置已更新: {createBackup}",
                    LogTag.CodeUnfucker
                );
            }
            else
            {
                Logger.EditorLogWarn("未找到任何配置文件进行更新", LogTag.CodeUnfucker);
            }
        }
        catch (Exception ex)
        {
            Logger.EditorLogError(
                $"更新 CodeUnfucker 备份配置失败: {ex.Message}",
                LogTag.CodeUnfucker
            );
        }
    }

    private void ExecuteCSharpierFormatting(string filePath)
    {
        try
        {
            Logger.EditorLogInfo(
                $"🎨 CSharpier 格式化文件: {Path.GetFileName(filePath)}",
                LogTag.CodeUnfucker
            );
            string dotnetPath = GetDotnetPath();
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

    private void ExecuteCSharpierFormattingForDirectory(string directoryPath)
    {
        try
        {
            Logger.EditorLogInfo($"🎨 CSharpier 格式化目录: {directoryPath}", LogTag.CodeUnfucker);
            string dotnetPath = GetDotnetPath();
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
}
