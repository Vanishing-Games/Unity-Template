using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Core
{
    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error,
        Exception,
    }

    public static class Logger
    {
        // ========== 对外 API ==========

        public static void SetLogLevel(LogLevel logLevel)
        {
            currentLogLevel = logLevel;
        }

        public static void EditorLogVerbose(string message, params LogTag[] tags)
        {
#if UNITY_EDITOR
            Output(LogLevel.Verbose, message, tags);
#endif
        }

        public static void DebugLogVerbose(string message, params LogTag[] tags)
        {
#if BUILD_MODE_DEBUG
            Output(LogLevel.Verbose, message, tags);
#endif
        }

        public static void ReleaseLogVerbose(string message, params LogTag[] tags)
        {
            Output(LogLevel.Verbose, message, tags);
        }

        public static void EditorLogInfo(string message, params LogTag[] tags)
        {
#if UNITY_EDITOR
            Output(LogLevel.Info, message, tags);
#endif
        }

        public static void DebugLogInfo(string message, params LogTag[] tags)
        {
#if BUILD_MODE_DEBUG
            Output(LogLevel.Info, message, tags);
#endif
        }

        public static void ReleaseLogInfo(string message, params LogTag[] tags)
        {
            Output(LogLevel.Info, message, tags);
        }

        public static void EditorLogWarn(string message, params LogTag[] tags)
        {
#if UNITY_EDITOR
            Output(LogLevel.Warning, message, tags);
#endif
        }

        public static void DebugLogWarn(string message, params LogTag[] tags)
        {
#if BUILD_MODE_DEBUG
            Output(LogLevel.Warning, message, tags);
#endif
        }

        public static void ReleaseLogWarn(string message, params LogTag[] tags)
        {
            Output(LogLevel.Warning, message, tags);
        }

        public static void EditorLogError(string message, params LogTag[] tags)
        {
#if UNITY_EDITOR
            Output(LogLevel.Error, message, tags);
#endif
        }

        public static void DebugLogError(string message, params LogTag[] tags)
        {
#if BUILD_MODE_DEBUG
            Output(LogLevel.Error, message, tags);
#endif
        }

        public static void ReleaseLogError(string message, params LogTag[] tags)
        {
            Output(LogLevel.Error, message, tags);
        }

        private static bool ShouldLog(LogLevel level)
        {
            var logType = LogLevelToLogType
                .Where(pair => pair.Key == level)
                .Select(pair => pair.Value)
                .FirstOrDefault();

            return level >= currentLogLevel;
        }

        private static void EnsureRootColors()
        {
            if (rootTagColors.Count > 0)
                return;

            var allRoots = typeof(LogTag)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => f.GetValue(null) as LogTag)
                .Where(t => t.Path.Count == 1)
                .Select(t => t.Name)
                .Distinct()
                .ToList();

            for (int i = 0; i < allRoots.Count; i++)
            {
                float hue = i / (float)allRoots.Count;
                rootTagColors[allRoots[i]] = Color.HSVToRGB(hue, 0.8f, 0.8f);
            }
        }

        private static Color GetTagColorAtDepth(LogTag tag, int depthIndex)
        {
            if (tag == null || tag.Path == null || tag.Path.Count == 0)
                return Color.white;

            if (depthIndex < 0)
                depthIndex = 0;
            if (depthIndex >= tag.Path.Count)
                depthIndex = tag.Path.Count - 1;

            EnsureRootColors();
            string rootName = tag.Path[0];
            Color finalColor = rootTagColors[rootName];

            for (int level = 1; level <= depthIndex; level++)
            {
                Color.RGBToHSV(finalColor, out float h, out float s, out float v);
                s *= 0.7f;
                v *= 0.8f;
                finalColor = Color.HSVToRGB(h, s, v);
            }

            return finalColor;
        }

        private static string ColorToHex(Color c)
        {
            Color32 c32 = c;
            return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}";
        }

        private static async void Output(LogLevel level, string message, params LogTag[] tags)
        {
            if (!ShouldLog(level))
                return;

            if (tags == null || tags.Length == 0)
            {
                Debug.LogError("<color=red><b>[Logger]</b> =============================</color>");
                Debug.LogError("<color=red><b>[Logger]</b> PUNISHMENT LANDING </color>");
                Debug.LogError(
                    "<color=red><b>[Logger]</b> Logger REQUIRES at least one LogTag.</color>"
                );
                Debug.LogError("<color=red><b>[Logger]</b> PUNISHMENT LANDING </color>");
                Debug.LogError("<color=red><b>[Logger]</b> =============================</color>");

                await UniTask.WhenAny(GameCore.Instance.QuitGame(), UniTask.Delay(10000));
                return;
            }

            string assemblyName = GetCallingAssemblyName();
            string assemblyStr = $"<b><color=#2196F3>▶▶▶{assemblyName}</color></b>";

            string logTypeText = level switch
            {
                LogLevel.Verbose => "<b><color=#9E9E9E>VERBOSE</color></b>",
                LogLevel.Info => "<b><color=#4CAF50>INFO</color></b>",
                LogLevel.Warning => "<b><color=#FFC107>WARNING</color></b>",
                LogLevel.Error => "<b><color=#F44336>ERROR</color></b>",
                _ => "<b>LOG</b>",
            };

            StringBuilder tagLinesBuilder = new();
            foreach (var t in tags)
            {
                var parts = new List<string>();
                for (int depth = 0; depth < t.Path.Count; depth++)
                {
                    Color c = GetTagColorAtDepth(t, depth);
                    string hex = ColorToHex(c);
                    string seg = t.Path[depth];
                    parts.Add($"<color={hex}>#{seg}</color>");
                }
                if (parts.Count > 0)
                {
                    tagLinesBuilder.AppendLine(string.Join(" ", parts));
                }
            }
            string tagStr = tagLinesBuilder.ToString().TrimEnd();

            StringBuilder formatted = new();
            formatted.AppendLine($"{logTypeText} {assemblyStr}");
            formatted.AppendLine(message);
            formatted.AppendLine(tagStr);

            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Info:
                    Debug.Log(formatted.ToString());
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formatted.ToString());
                    break;
                case LogLevel.Error:
                    Debug.LogError(formatted.ToString());
                    break;
            }
        }

        private static string GetCallingAssemblyName()
        {
            var stackTrace = new StackTrace();
            for (int i = 1; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame.GetMethod();
                if (method == null)
                    continue;

                var declaringType = method.DeclaringType;
                if (declaringType == null)
                    continue;

                if (declaringType == typeof(Logger))
                    continue;

                var assembly = declaringType.Assembly;
                return assembly.GetName().Name ?? "UnknownAssembly";
            }
            return "UnknownAssembly";
        }

        private static LogLevel currentLogLevel = LogLevel.Verbose;
        private static readonly System.Collections.Generic.Dictionary<string, Color> rootTagColors =
            new();
        private static readonly System.Collections.Generic.Dictionary<string, Color> tagColorCache =
            new();

        public static Dictionary<LogLevel, LogType> LogLevelToLogType = new()
        {
            { LogLevel.Verbose, LogType.Log },
            { LogLevel.Info, LogType.Log },
            { LogLevel.Warning, LogType.Warning },
            { LogLevel.Error, LogType.Error },
            { LogLevel.Exception, LogType.Exception },
        };
    }
}
