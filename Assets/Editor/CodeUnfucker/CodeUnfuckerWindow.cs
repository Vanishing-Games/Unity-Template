using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Logger = Core.Logger;

public class CodeUnfuckerWindow : OdinMenuEditorWindow
{
    #region Public
    public string DetectDotnetPath()
    {
        return operationPanel.GetDotnetPath();
    }

    [MenuItem("Tools/CodeUnfucker/Open CodeUnfucker Window")]
    public static void OpenWindow()
    {
        var window = GetWindow<CodeUnfuckerWindow>();
        window.titleContent = new GUIContent(
            "CodeUnfucker",
            EditorGUIUtility.IconContent("Tool").image
        );
        window.Show();
    }
    #endregion

    #region Protected
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree(supportsMultiSelect: true)
        {
            DefaultMenuStyle = OdinMenuStyle.TreeViewStyle,
            Config = { DrawSearchToolbar = true, SearchToolbarHeight = 25 },
        };
        var assetPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x =>
                x.StartsWith("Assets/") && (x.EndsWith(".cs") || AssetDatabase.IsValidFolder(x))
            )
            .OrderBy(x => x);
        foreach (var path in assetPaths)
        {
            if (path.EndsWith(".cs"))
            {
                var fileInfo = new FileTreeItem(path, false);
                tree.Add(path.Substring("Assets/".Length), fileInfo);
            }
            else if (AssetDatabase.IsValidFolder(path) && ContainsCsFiles(path))
            {
                var folderInfo = new FileTreeItem(path, true);
                tree.Add(path.Substring("Assets/".Length), folderInfo);
            }
        }

        var fileTreeItems = tree.EnumerateTree().Where(x => x.Value is FileTreeItem);
        foreach (var menuItem in fileTreeItems)
        {
            AddIcons(menuItem);
        }

        return tree;
    }

    protected override void DrawEditors()
    {
        var selected = this.MenuTree.Selection.FirstOrDefault();
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (selected != null && selected.Value is FileTreeItem)
            {
                var fileItem = selected.Value as FileTreeItem;
                GUILayout.Label($"已选择: {fileItem.Path}", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("选择文件或文件夹以查看详情", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新文件树", EditorStyles.toolbarButton))
            {
                ForceMenuTreeRebuild();
            }
        }

        GUILayout.EndHorizontal();
        var selectedItems = this
            .MenuTree.Selection.Where(x => x.Value is FileTreeItem)
            .Select(x => x.Value as FileTreeItem)
            .ToList();
        operationPanel.UpdateSelectedItems(selectedItems);
        GUILayout.BeginVertical();
        {
            if (operationPanelTree == null)
            {
                operationPanelTree = PropertyTree.Create(operationPanel);
            }

            operationPanelTree.Draw(false);
        }

        GUILayout.EndVertical();
    }
    #endregion

    #region Private
    private bool ContainsCsFiles(string folderPath)
    {
        return Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories).Length > 0;
    }

    private void AddIcons(OdinMenuItem menuItem)
    {
        if (menuItem.Value is FileTreeItem fileItem)
        {
            if (fileItem.IsDirectory)
            {
                menuItem.Icon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            }
            else
            {
                menuItem.Icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            }
        }
    }
    #endregion

    #region Nested Classes
    [System.Serializable]
    public class FileTreeItem
    {
        public string Path { get; private set; }
        public bool IsDirectory { get; private set; }
        public string DisplayName => System.IO.Path.GetFileName(Path);

        public FileTreeItem(string path, bool isDirectory)
        {
            Path = path;
            IsDirectory = isDirectory;
        }
    }

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
        [InfoBox("格式化功能会重新排列类成员并添加Region宏", InfoMessageType.None)]
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
                Logger.EditorLogWarn(
                    "请先从左侧文件树选择要处理的文件或文件夹",
                    LogTag.CodeUnfucker
                );
                return;
            }

            if (!enableFormatting && !enableAnalysis)
            {
                Logger.EditorLogWarn("请至少选择一个操作（格式化或分析）", LogTag.CodeUnfucker);
                return;
            }

            Logger.EditorLogInfo(
                $"开始执行操作，共 {selectedPaths.Count} 个项目",
                LogTag.CodeUnfucker
            );
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
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string codeUnfuckerPath = Path.Combine(projectRoot, "CodeUnfucker");
            if (!Directory.Exists(codeUnfuckerPath))
            {
                Logger.EditorLogError("CodeUnfucker 项目目录不存在", LogTag.CodeUnfucker);
                return;
            }

            try
            {
                string dotnetPath = GetDotnetPath();
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
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string configPath = Path.Combine(projectRoot, "ProjectConfig");
            if (Directory.Exists(configPath))
            {
                EditorUtility.RevealInFinder(configPath);
                Logger.EditorLogInfo($"已打开配置文件夹: {configPath}", LogTag.CodeUnfucker);
            }
            else
            {
                Logger.EditorLogError("配置文件夹不存在", LogTag.CodeUnfucker);
            }
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
            string detectedPath = GetDotnetPath();
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
            var config = LoadConfig();
            string dotnetPath = null;
            // 1. 检查环境变量
            foreach (var envVar in config.dotnetPaths.environmentVariables)
            {
                dotnetPath = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(dotnetPath))
                {
                    Logger.EditorLogInfo(
                        $"从环境变量 {envVar} 找到 dotnet: {dotnetPath}",
                        LogTag.CodeUnfucker
                    );
                    return dotnetPath;
                }
            }

            // 2. 检查默认搜索路径
            foreach (var path in config.dotnetPaths.defaultSearchPaths)
            {
                if (path == "dotnet")
                {
                    // 尝试直接调用，看是否在 PATH 中
                    try
                    {
                        var testProcess = new Process();
                        testProcess.StartInfo.FileName = "dotnet";
                        testProcess.StartInfo.Arguments = "--version";
                        testProcess.StartInfo.UseShellExecute = false;
                        testProcess.StartInfo.RedirectStandardOutput = true;
                        testProcess.StartInfo.RedirectStandardError = true;
                        testProcess.StartInfo.CreateNoWindow = true;
                        testProcess.Start();
                        testProcess.WaitForExit();
                        if (testProcess.ExitCode == 0)
                        {
                            dotnetPath = "dotnet";
                            Logger.EditorLogInfo(
                                "从默认搜索路径找到 dotnet: " + dotnetPath,
                                LogTag.CodeUnfucker
                            );
                            return dotnetPath;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                else if (File.Exists(path))
                {
                    dotnetPath = path;
                    Logger.EditorLogInfo(
                        "从默认搜索路径找到 dotnet: " + dotnetPath,
                        LogTag.CodeUnfucker
                    );
                    return dotnetPath;
                }
            }

            // 3. 检查自定义路径
            foreach (var customPath in config.dotnetPaths.customPaths)
            {
                if (File.Exists(customPath))
                {
                    dotnetPath = customPath;
                    Logger.EditorLogInfo(
                        "从自定义路径找到 dotnet: " + dotnetPath,
                        LogTag.CodeUnfucker
                    );
                    return dotnetPath;
                }
            }

            return null;
        }

        private CodeUnfuckerConfig LoadConfig()
        {
            string configDir = Path.Combine(Application.dataPath, "..", "ProjectConfig");
            string configPath = Path.Combine(configDir, "CodeUnfuckerConfig.json");
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    return JsonUtility.FromJson<CodeUnfuckerConfig>(json);
                }
                catch (Exception ex)
                {
                    Logger.EditorLogError($"加载配置文件失败: {ex.Message}", LogTag.CodeUnfucker);
                    return CreateDefaultConfig(configPath);
                }
            }
            else
            {
                Logger.EditorLogInfo("配置文件不存在，创建默认配置", LogTag.CodeUnfucker);
                return CreateDefaultConfig(configPath);
            }
        }

        private CodeUnfuckerConfig CreateDefaultConfig(string configPath)
        {
            var defaultConfig = new CodeUnfuckerConfig();
            SaveConfig(defaultConfig, configPath);
            return defaultConfig;
        }

        private void SaveConfig(CodeUnfuckerConfig config, string configPath = null)
        {
            if (configPath == null)
            {
                string configDir = Path.Combine(Application.dataPath, "..", "ProjectConfig");
                configPath = Path.Combine(configDir, "CodeUnfuckerConfig.json");
            }

            try
            {
                string configDir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(configPath, json);
                Logger.EditorLogInfo($"配置文件已保存: {configPath}", LogTag.CodeUnfucker);
            }
            catch (Exception ex)
            {
                Logger.EditorLogError($"保存配置文件失败: {ex.Message}", LogTag.CodeUnfucker);
            }
        }

        public void UpdateSelectedItems(List<FileTreeItem> items)
        {
            selectedPaths.Clear();
            selectedPaths.AddRange(items.Select(x => x.Path));
        }
    }

    [System.Serializable]
    public class CodeUnfuckerConfig
    {
        public DotnetPathConfig dotnetPaths = new DotnetPathConfig();
    }

    [System.Serializable]
    public class DotnetPathConfig
    {
        public List<string> environmentVariables = new List<string>
        {
            "DOTNET_ROOT",
            "DOTNET_CLI_HOME",
        };
        public List<string> defaultSearchPaths = new List<string>
        {
            "/opt/homebrew/bin/dotnet", // Homebrew on Apple Silicon
            "/usr/local/bin/dotnet", // Homebrew on Intel Mac
            "/usr/local/share/dotnet/dotnet", // Microsoft installer
            "/usr/bin/dotnet", // System installation
            "dotnet", // 如果在 PATH 中
        };
        public List<string> customPaths = new List<string>();
    }
    #endregion

    private OperationPanel operationPanel = new OperationPanel();
    private PropertyTree operationPanelTree;
}

public class DotnetConfigWindow : OdinEditorWindow
{
    public void SetConfig(CodeUnfuckerWindow.CodeUnfuckerConfig config)
    {
        this.config = config;
        if (configTree != null)
        {
            configTree.Dispose();
        }

        configTree = PropertyTree.Create(config);
    }

    #region Unity LifeCycle
    protected override void OnEnable()
    {
        base.OnEnable();
        if (config == null)
        {
            config = new CodeUnfuckerWindow.CodeUnfuckerConfig();
        }

        configTree = PropertyTree.Create(config);
    }

    protected override void OnDestroy()
    {
        configTree?.Dispose();
        base.OnDestroy();
    }

    protected override void OnGUI()
    {
        if (configTree == null || config == null)
        {
            GUILayout.Label("配置未加载", EditorStyles.boldLabel);
            return;
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "环境变量: 系统会按顺序检查这些环境变量\n"
                + "默认搜索路径: 系统默认的 dotnet 安装位置\n"
                + "自定义路径: 您可以添加自己的 dotnet 路径",
            MessageType.Info
        );
        GUILayout.Space(10);
        configTree.Draw(false);
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("💾 保存配置", GUILayout.Height(30)))
            {
                SaveConfig();
            }

            if (GUILayout.Button("🔄 重置为默认", GUILayout.Height(30)))
            {
                ResetToDefault();
            }

            if (GUILayout.Button("🔍 测试配置", GUILayout.Height(30)))
            {
                TestConfig();
            }
        }

        GUILayout.EndHorizontal();
    }
    #endregion

    #region Private
    private void SaveConfig()
    {
        try
        {
            string configDir = Path.Combine(Application.dataPath, "..", "ProjectConfig");
            string configPath = Path.Combine(configDir, "CodeUnfuckerConfig.json");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(configPath, json);
            Logger.EditorLogInfo($"配置已保存到: {configPath}", LogTag.CodeUnfucker);
            ShowNotification(new GUIContent("配置已保存"));
        }
        catch (Exception ex)
        {
            Logger.EditorLogError($"保存配置失败: {ex.Message}", LogTag.CodeUnfucker);
            ShowNotification(new GUIContent("保存失败"));
        }
    }

    private void ResetToDefault()
    {
        if (
            EditorUtility.DisplayDialog(
                "重置配置",
                "确定要重置为默认配置吗？这将丢失所有自定义设置。",
                "确定",
                "取消"
            )
        )
        {
            config = new CodeUnfuckerWindow.CodeUnfuckerConfig();
            SetConfig(config);
            ShowNotification(new GUIContent("已重置为默认配置"));
        }
    }

    private void TestConfig()
    {
        var window = EditorWindow.GetWindow<CodeUnfuckerWindow>();
        string detectedPath = window.DetectDotnetPath();
        if (string.IsNullOrEmpty(detectedPath))
        {
            Logger.EditorLogWarn("使用当前配置未检测到 dotnet 路径", LogTag.CodeUnfucker);
            ShowNotification(new GUIContent("未检测到 dotnet"));
        }
        else
        {
            Logger.EditorLogInfo(
                $"使用当前配置检测到 dotnet 路径: {detectedPath}",
                LogTag.CodeUnfucker
            );
            ShowNotification(new GUIContent($"检测到: {Path.GetFileName(detectedPath)}"));
        }
    }
    #endregion

    private CodeUnfuckerWindow.CodeUnfuckerConfig config;
    private PropertyTree configTree;
}
