using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
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

    #endregion

    private OperationPanel operationPanel = new OperationPanel();
    private PropertyTree operationPanelTree;
}
