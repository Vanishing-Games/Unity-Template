using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CodeUnfucker
{
    public static class ConfigManager
    {
        private static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

        private static FormatterConfig? _formatterConfig;
        private static AnalyzerConfig? _analyzerConfig;

        public static FormatterConfig GetFormatterConfig()
        {
            if (_formatterConfig == null)
            {
                _formatterConfig = LoadConfig<FormatterConfig>("FormatterConfig.json");
            }
            return _formatterConfig;
        }

        public static AnalyzerConfig GetAnalyzerConfig()
        {
            if (_analyzerConfig == null)
            {
                _analyzerConfig = LoadConfig<AnalyzerConfig>("AnalyzerConfig.json");
            }
            return _analyzerConfig;
        }

        private static T LoadConfig<T>(string fileName)
            where T : new()
        {
            try
            {
                string configFile = Path.Combine(ConfigPath, fileName);

                if (!File.Exists(configFile))
                {
                    Console.WriteLine($"[WARN] 配置文件不存在: {configFile}，使用默认配置");
                    return new T();
                }

                string jsonContent = File.ReadAllText(configFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                };

                var config = JsonSerializer.Deserialize<T>(jsonContent, options);
                Console.WriteLine($"[INFO] 成功加载配置文件: {fileName}");
                return config ?? new T();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 加载配置文件失败 {fileName}: {ex.Message}");
                Console.WriteLine($"[INFO] 使用默认配置");
                return new T();
            }
        }

        public static void ReloadConfigs()
        {
            _formatterConfig = null;
            _analyzerConfig = null;
            Console.WriteLine("[INFO] 配置已重新加载");
        }
    }

    // 配置类定义
    public class FormatterConfig
    {
        public string Description { get; set; } = "CodeUnfucker 代码格式化功能配置";
        public string Version { get; set; } = "1.0.0";
        public FormatterSettings FormatterSettings { get; set; } = new();
        public MemberOrdering MemberOrdering { get; set; } = new();
        public List<string> UnityLifeCycleMethods { get; set; } = new();
        public RegionSettings RegionSettings { get; set; } = new();
    }

    public class FormatterSettings
    {
        public int MinLinesForRegion { get; set; } = 15;
        public bool EnableRegionGeneration { get; set; } = true;
        public bool CreateBackupFiles { get; set; } = true;
        public string BackupFileExtension { get; set; } = ".backup";
    }

    public class MemberOrdering
    {
        public List<string> Order { get; set; } =
            new()
            {
                "Public",
                "UnityLifeCycle",
                "Protected",
                "Private",
                "NestedClasses",
                "MemberVariables",
            };
        public string Description { get; set; } = "类成员的排序规则，按此顺序重新排列";
    }

    public class RegionSettings
    {
        public string PublicRegionName { get; set; } = "Public";
        public string UnityLifeCycleRegionName { get; set; } = "Unity LifeCycle";
        public string ProtectedRegionName { get; set; } = "Protected";
        public string PrivateRegionName { get; set; } = "Private";
        public string NestedClassesRegionName { get; set; } = "Nested Classes";
        public string MemberVariablesRegionName { get; set; } = "Member Variables";
        public int IndentationSpaces { get; set; } = 8;
    }

    public class AnalyzerConfig
    {
        public string Description { get; set; } = "CodeUnfucker 代码分析功能配置";
        public string Version { get; set; } = "1.0.0";
        public AnalyzerSettings AnalyzerSettings { get; set; } = new();
        public FileFilters FileFilters { get; set; } = new();
        public OutputSettings OutputSettings { get; set; } = new();
        public StaticAnalysisRules StaticAnalysisRules { get; set; } = new();
    }

    public class AnalyzerSettings
    {
        public bool EnableSyntaxAnalysis { get; set; } = true;
        public bool EnableSemanticAnalysis { get; set; } = true;
        public bool EnableDiagnostics { get; set; } = true;
        public bool ShowReferencedAssemblies { get; set; } = true;
        public bool VerboseLogging { get; set; } = false;
    }

    public class FileFilters
    {
        public List<string> IncludePatterns { get; set; } = new() { "*.cs" };
        public List<string> ExcludePatterns { get; set; } =
            new() { "*.Designer.cs", "*.generated.cs", "**/bin/**", "**/obj/**", "**/Temp/**" };
        public bool SearchSubdirectories { get; set; } = true;
    }

    public class OutputSettings
    {
        public string LogLevel { get; set; } = "Info";
        public bool ShowFileCount { get; set; } = true;
        public bool ShowProcessingTime { get; set; } = true;
        public bool ShowDetailedErrors { get; set; } = true;
    }

    public class StaticAnalysisRules
    {
        public bool CheckNamingConventions { get; set; } = true;
        public bool CheckCodeComplexity { get; set; } = false;
        public bool CheckUnusedVariables { get; set; } = false;
        public bool CheckDocumentationComments { get; set; } = false;
        public int MaxComplexityThreshold { get; set; } = 10;
    }
}
