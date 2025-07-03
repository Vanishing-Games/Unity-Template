using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ComplexPage
{
    [Title("复杂页面示例")]
    [InfoBox("这是一个复杂页面的示例，展示多种控件和交互。")]
    [PropertyOrder(-10)]
    [HorizontalGroup("TopRow")]
    [LabelText("名称")]
    [ValidateInput("ValidateName", "名称不能为空且不能超过10个字符")]
    public string userName;

    [HorizontalGroup("TopRow")]
    [LabelText("年龄")]
    [Range(0, 120)]
    public int age;

    [Space]
    [EnumToggleButtons, LabelText("用户角色")]
    public UserRole role;

    [ColorPalette, LabelText("主题颜色")]
    public Color themeColor = Color.green;

    [Space]
    [ProgressBar(0, 100, ColorMember = nameof(GetProgressColor))]
    [LabelText("任务完成度")]
    [PropertyRange(0, 100)]
    public int progress = 30;

    [LabelText("进度调整")]
    [PropertyRange(0, 100)]
    [OnValueChanged(nameof(OnProgressChanged))]
    public int progressSlider = 30;

    [Space(10)]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = false, Expanded = true)]
    [LabelText("动态待办事项")]
    public List<TodoItem> todoList = new List<TodoItem>()
    {
        new TodoItem() { title = "完成代码", isDone = false },
        new TodoItem() { title = "测试功能", isDone = true },
    };

    [Button("添加新待办项")]
    private void AddTodo()
    {
        todoList.Add(new TodoItem() { title = "新任务", isDone = false });
    }

    [Space]
    [ShowIf(nameof(role), UserRole.Admin)]
    [LabelText("管理员专用开关")]
    public bool adminToggle = false;

    private bool ValidateName(string name)
    {
        return !string.IsNullOrEmpty(name) && name.Length <= 10;
    }

    private Color GetProgressColor()
    {
        if (progress < 30)
            return Color.red;
        if (progress < 70)
            return Color.yellow;
        return Color.green;
    }

    private void OnProgressChanged()
    {
        progress = progressSlider;
    }

    public enum UserRole
    {
        Guest,
        User,
        Admin,
    }

    [System.Serializable]
    public class TodoItem
    {
        [LabelText("任务标题")]
        public string title;

        [LabelText("完成")]
        public bool isDone;
    }
}
