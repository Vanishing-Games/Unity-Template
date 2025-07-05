using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

public class MenuTreeSample : OdinMenuEditorWindow
{
    [AutoOdinWindowMenu]
    private static void OpenWindow()
    {
        GetWindow<MenuTreeSample>().Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;

        tree.Add("Menu Style", tree.DefaultMenuStyle);

        var allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(x => x.StartsWith("Assets/"))
            .OrderBy(x => x);

        foreach (var path in allAssets)
        {
            tree.AddAssetAtPath(path.Substring("Assets/".Length), path);
        }

        tree.EnumerateTree().AddThumbnailIcons();

        return tree;
    }
}