using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeUnfucker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LogInfo("CodeUnfucker 启动");

            var program = new Program();
            program.Run(args);

            LogInfo("CodeUnfucker 运行结束");
        }

        public void Run(string[] args)
        {
            if (!ValidateArgs(args, out string scriptPath))
                return;

            LogInfo($"扫描路径: {scriptPath}");

            var csFiles = GetCsFiles(scriptPath);
            if (csFiles.Length == 0)
            {
                LogWarn("未找到任何 .cs 文件");
                return;
            }

            LogInfo($"找到 {csFiles.Length} 个 .cs 文件");

            var syntaxTrees = ParseSyntaxTrees(csFiles);
            var references = GetMetadataReferences();
            var compilation = CreateCompilation(syntaxTrees, references);

            LogReferencedAssemblies(compilation);
        }

        private static bool ValidateArgs(string[] args, out string scriptPath)
        {
            if (args.Length != 1)
            {
                LogError("用法: CodeUnfucker <path-to-scripts>");
                scriptPath = string.Empty;
                return false;
            }

            scriptPath = args[0];
            if (!Directory.Exists(scriptPath))
            {
                LogError($"路径不存在: {scriptPath}");
                return false;
            }

            return true;
        }

        private static string[] GetCsFiles(string path)
        {
            return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        }

        private static List<SyntaxTree> ParseSyntaxTrees(string[] files)
        {
            LogInfo("开始解析 .cs 文件为语法树");
            var trees = new List<SyntaxTree>();
            foreach (var file in files)
            {
                LogDebug($"解析文件: {file}");
                var code = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(code, path: file);
                trees.Add(tree);
            }
            return trees;
        }

        private static List<MetadataReference> GetMetadataReferences()
        {
            LogInfo("加载程序集引用");

            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            };

            return refs;
        }

        private static CSharpCompilation CreateCompilation(
            List<SyntaxTree> syntaxTrees,
            List<MetadataReference> references
        )
        {
            LogInfo("创建 Roslyn 编译对象");

            return CSharpCompilation.Create(
                "AnalyzerTempAssembly",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        private static void LogReferencedAssemblies(CSharpCompilation compilation)
        {
            LogInfo("以下是引用的程序集:");
            foreach (var reference in compilation.ReferencedAssemblyNames)
            {
                LogInfo($"  - {reference.Name}");
            }
        }

        #region LoggingHelpers

        static private void LogInfo(string message) => Console.WriteLine($"[INFO] {message}");

        private static void LogWarn(string message) => Console.WriteLine($"[WARN] {message}");

        private static void LogError(string message) => Console.WriteLine($"[ERROR] {message}");

        private static void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");

        #endregion
    }
}
