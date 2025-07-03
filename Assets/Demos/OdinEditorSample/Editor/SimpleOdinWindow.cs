using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class SimpleOdinWindow : OdinEditorWindow
{
    [AutoOdinWindowMenu]
    private static void OpenWindow()
    {
        GetWindow<SimpleOdinWindow>().Show();
    }

    [Title("My Odin Window")]
    [InfoBox("这是一个自定义的 Odin 编辑器窗口。")]
    [LabelText("需要打印的文本")]
    public string text;

    [Button("打印文本")]
    private void PrintText()
    {
        Debug.Log(text);
    }
}
