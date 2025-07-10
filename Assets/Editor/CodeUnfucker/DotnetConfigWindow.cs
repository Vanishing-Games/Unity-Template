using System;
using System.IO;
using Core;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Logger = Core.Logger;

public class DotnetConfigWindow : OdinEditorWindow
{
    public void SetConfig(CodeUnfuckerConfig config)
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
            config = new CodeUnfuckerConfig();
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
        if (CodeUnfuckerConfigManager.SaveConfig(config))
        {
            ShowNotification(new GUIContent("配置已保存"));
        }
        else
        {
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
            config = new CodeUnfuckerConfig();
            SetConfig(config);
            ShowNotification(new GUIContent("已重置为默认配置"));
        }
    }

    private void TestConfig()
    {
        string detectedPath = CodeUnfuckerBridge.GetDotnetExecutablePath();
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

    private CodeUnfuckerConfig config;
    private PropertyTree configTree;
}
