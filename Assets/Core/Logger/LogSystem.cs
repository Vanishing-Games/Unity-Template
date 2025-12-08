/*
 * --------------------------------------------------------------------------------
 * Copyright (c) 2025 Vanishing Games. All Rights Reserved.
 * @Author: VanishXiao
 * @Date: 2025-10-30 16:25:39
 * @LastEditTime: 2025-12-03 19:17:50
 * --------------------------------------------------------------------------------
 */
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
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error,
        Exception,
    }

    /// <summary>
    /// 日志系统
    /// 通过编译宏控制日志输出：
    /// - UNITY_EDITOR: 编辑器模式，输出所有级别日志
    /// - ENABLE_DEBUG_LOG: Debug 构建，输出 Verbose/Info/Warning/Error
    /// - ENABLE_RELEASE_LOG: Release 构建，仅输出 Warning/Error
    /// - 如果都没定义，则不输出任何日志（Production 模式）
    /// </summary>
    public static class Logger
    {
        // ========== 对外 API ==========

        /// <summary>
        /// 设置最低日志级别（运行时过滤）
        /// </summary>
        public static void SetLogLevel(LogLevel logLevel)
        {
            currentLogLevel = logLevel;
        }

        /// <summary>
        /// 输出 Verbose 级别日志
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("ENABLE_DEBUG_LOG")]
        public static void LogVerbose(string message, params LogTag[] tags)
        {
            Output(LogLevel.Verbose, message, tags);
        }

        /// <summary>
        /// 输出 Info 级别日志
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("ENABLE_DEBUG_LOG")]
        public static void LogInfo(string message, params LogTag[] tags)
        {
            Output(LogLevel.Info, message, tags);
        }

        /// <summary>
        /// 输出 Warning 级别日志
        /// </summary>
        [
            Conditional("UNITY_EDITOR"),
            Conditional("ENABLE_DEBUG_LOG"),
            Conditional("ENABLE_RELEASE_LOG")
        ]
        public static void LogWarn(string message, params LogTag[] tags)
        {
            Output(LogLevel.Warning, message, tags);
        }

        /// <summary>
        /// 输出 Error 级别日志
        /// </summary>
        [
            Conditional("UNITY_EDITOR"),
            Conditional("ENABLE_DEBUG_LOG"),
            Conditional("ENABLE_RELEASE_LOG")
        ]
        public static void LogError(string message, params LogTag[] tags)
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
            string assemblyStr = $"<b><color=#2196F3>>>>{assemblyName}</color></b>";

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
                    tagLinesBuilder.AppendJoin(" ", parts).AppendLine();
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
        private static readonly Dictionary<string, Color> rootTagColors = new();

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
