using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class SimpleOdinMultiWindow : OdinMenuEditorWindow
{
    [AutoOdinWindowMenu]
    private static void OpenWindow()
    {
        var window = GetWindow<SimpleOdinMultiWindow>();
        window.position = new Rect(100, 100, 600, 400);
        window.Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();

        tree.Add("首页", new HomePage());
        tree.Add("设置", new SettingsPage());
        tree.Add("关于", new AboutPage());

        return tree;
    }

    public class HomePage
    {
        [Title("主页")]
        public string message = "欢迎使用工具！";

        [Button]
        private void Hello() => Debug.Log("Hello from HomePage");
    }

    public class SettingsPage
    {
        [Range(0, 100)]
        public int volume = 50;

        [ToggleLeft]
        public bool isEnabled = true;
    }

    public class AboutPage
    {
        [ReadOnly]
        public string version = "v1.0";

        [Multiline]
        public string notes = "这是 Odin 制作的工具。";
    }
}
