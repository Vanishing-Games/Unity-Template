using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor.PackageManager;
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
            if (!IsLogAllowed(true, false))
                return;
            Output(LogLevel.Verbose, message, tags);
        }

        public static void BuildLogVerbose(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(false, true))
                return;
            Output(LogLevel.Verbose, message, tags);
        }

        public static void BothLogVerbose(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(true, true))
                return;
            Output(LogLevel.Verbose, message, tags);
        }

        public static void EditorLogInfo(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(true, false))
                return;
            Output(LogLevel.Info, message, tags);
        }

        public static void BuildLogInfo(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(false, true))
                return;
            Output(LogLevel.Info, message, tags);
        }

        public static void BothLogInfo(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(true, true))
                return;
            Output(LogLevel.Info, message, tags);
        }

        public static void EditorLogWarn(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(true, false))
                return;
            Output(LogLevel.Warning, message, tags);
        }

        public static void BuildLogWarn(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(false, true))
                return;
            Output(LogLevel.Warning, message, tags);
        }

        public static void BothLogWarn(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(true, true))
                return;
            Output(LogLevel.Warning, message, tags);
        }

        public static void EditorLogError(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(true, false))
                return;
            Output(LogLevel.Error, message, tags);
        }

        public static void BuildLogError(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(false, true))
                return;
            Output(LogLevel.Error, message, tags);
        }

        public static void BothLogError(string message, params LogTag[] tags)
        {
            if (!IsLogAllowed(true, true))
                return;
            Output(LogLevel.Error, message, tags);
        }

        // ========== 内部方法 ==========

        private static bool IsLogAllowed(bool showInEditor, bool showInBuild)
        {
#if UNITY_EDITOR
            return showInEditor;
#else
            return showInBuild && Debug.isDebugBuild;
#endif
        }

        private static bool ShouldLog(LogLevel level)
        {
            var logType = LogLevelToLogType
                .Where(pair => pair.Key == level)
                .Select(pair => pair.Value)
                .FirstOrDefault();

            return level >= currentLogLevel;
        }

        private static Color GetTagColor(LogTag tag)
        {
            string key = string.Join("/", tag.Path);
            if (tagColorCache.TryGetValue(key, out var cachedColor))
                return cachedColor;

            string rootName = tag.Path[0];

            if (!rootTagColors.ContainsKey(rootName))
            {
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

            Color baseColor = rootTagColors[rootName];
            Color finalColor = baseColor;

            for (int level = 1; level < tag.Path.Count; level++)
            {
                Color.RGBToHSV(finalColor, out float h, out float s, out float v);
                s *= 0.7f;
                v *= 0.8f;
                finalColor = Color.HSVToRGB(h, s, v);
            }

            tagColorCache[key] = finalColor;
            return finalColor;
        }

        private static string ColorToHex(Color c)
        {
            Color32 c32 = c;
            return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}";
        }

        private static void Output(LogLevel level, string message, params LogTag[] tags)
        {
            if (!ShouldLog(level))
                return;

            if (tags == null || tags.Length == 0)
            {
                Debug.LogError(
                    "<color=red><b>[Logger]</b> Logger requires at least one LogTag.</color>"
                );
                return;
            }

            if (!IsLogAllowed(true, true))
                return;

            string assemblyName = GetCallingAssemblyName();
            string assemblyStr = $"<b><color=#2196F3>》〉》》{assemblyName}</color></b>";

            string logTypeText = level switch
            {
                LogLevel.Verbose => "<b><color=#9E9E9E>VERBOSE</color></b>",
                LogLevel.Info => "<b><color=#4CAF50>INFO</color></b>",
                LogLevel.Warning => "<b><color=#FFC107>WARNING</color></b>",
                LogLevel.Error => "<b><color=#F44336>ERROR</color></b>",
                _ => "<b>LOG</b>",
            };

            string tagStr = string.Join(
                " ",
                tags.Select(t =>
                {
                    Color c = GetTagColor(t);
                    string hex = ColorToHex(c);
                    string last = t.Path.Last();
                    return $"<color={hex}>#{last}</color>";
                })
            );

            string formatted = $"{assemblyStr}\n{logTypeText} {message}\n{tagStr}";

            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Info:
                    Debug.Log(formatted);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formatted);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formatted);
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
