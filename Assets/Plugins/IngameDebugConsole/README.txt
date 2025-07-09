# Unity 3D 游戏内调试控制台

<img height="235" src="Images/1.png" alt="screenshot" /> <img height="235" src="Images/2.png" alt="screenshot2" />

**Asset Store 可用:** https://assetstore.unity.com/packages/tools/gui/in-game-debug-console-68068

**论坛主题:** http://forum.unity3d.com/threads/in-game-debug-console-with-ugui-free.411323/

**Discord:** https://discord.gg/UJJt549AaV

**[GitHub 赞助 ☕](https://github.com/sponsors/yasirkula)**

## 关于

此资源帮助您在构建的运行时中查看调试消息（日志、警告、错误、异常）（在编辑器中也包括断言），并使用其内置控制台执行命令。它还支持在 Android 平台上将 *logcat* 消息记录到控制台。

用户界面使用 **uGUI** 创建，当启用 *Sprite Packing* 时仅消耗 **1 SetPass call**（和 6 到 10 个批次）。可以在游戏过程中调整控制台窗口大小或隐藏它。一旦控制台被隐藏，将出现一个小弹窗来替代它（可以拖动）。弹窗将显示自出现以来收到的日志数量。点击弹窗后控制台窗口将重新出现。

![popup](Images/3.png)

控制台窗口使用定制的回收列表视图进行优化，减少了 *Instantiate* 函数的调用。

## 安装

有 5 种方式安装此插件：

- 通过 *Assets-Import Package* 导入 [IngameDebugConsole.unitypackage](https://github.com/yasirkula/UnityIngameDebugConsole/releases)
- 克隆/[下载](https://github.com/yasirkula/UnityIngameDebugConsole/archive/master.zip) 此存储库并将 *Plugins* 文件夹移动到您的 Unity 项目的 *Assets* 文件夹中
- 从 [Asset Store](https://assetstore.unity.com/packages/tools/gui/in-game-debug-console-68068) 导入
- *（通过 Package Manager）* 点击 + 按钮并从以下 git URL 安装包：
  - `https://github.com/yasirkula/UnityIngameDebugConsole.git`
- *（通过 [OpenUPM](https://openupm.com)）* 安装 [openupm-cli](https://github.com/openupm/openupm-cli) 后，运行以下命令：
  - `openupm add com.yasirkula.ingamedebugconsole`

## 常见问题

- **Unity 2019.2.5 或更早版本不支持新输入系统**

将 `ENABLE_INPUT_SYSTEM` 编译器指令添加到 **Player Settings/Scripting Define Symbols**（这些符号是平台特定的，因此如果您稍后更改活动平台，则必须再次添加编译器指令）。

- **Unity 2018.4 或更早版本无法解析 "Unity.InputSystem" 程序集**

从 **IngameDebugConsole.Runtime** Assembly Definition File 的 *Assembly Definition References* 列表中删除 `Unity.InputSystem` 程序集。

- **"在 Android 中接收 Logcat 日志" 不工作，在 Logcat 中显示 "java.lang.ClassNotFoundException: com.yasirkula.unity.DebugConsoleLogcatLogger"**

如果您确定您的插件是最新的，那么从 *Player Settings* 启用 **Custom Proguard File** 选项，并在该文件中添加以下行：`-keep class com.yasirkula.unity.* { *; }`

## 如何使用

只需将 **IngameDebugConsole** 预制件放置到您的场景中。将光标悬停在 Inspector 中的属性上将显示解释性工具提示。

在 Unity 编辑器中测试时，右键单击日志条目将在外部脚本编辑器中打开相应的行，类似于在 Unity Console 中双击日志。

## 命令控制台

### 执行命令

您可以使用控制台底部的输入字段输入命令。要查看所有可用命令，只需输入 "*help*"。

命令基本上是一个可以通过控制台命令输入字段调用的函数。此函数可以是 **静态** 或 **实例函数**（非静态），在这种情况下，需要一个活动实例来调用该函数。函数的返回类型可以是任何类型（包括 *void*）。如果函数返回一个对象，它将被打印到控制台。函数也可以接受任意数量的参数；唯一的限制适用于这些参数的类型。支持的参数类型有：

**基本类型、枚举、字符串、Vector2、Vector3、Vector4、Color、Color32、Vector2Int、Vector3Int、Quaternion、Rect、RectInt、RectOffset、Bounds、BoundsInt、GameObject、任何 Component 类型、这些支持类型的数组/列表**

注意 *GameObject* 和 *Component* 参数使用 *GameObject.Find* 分配值。

要调用已注册的命令，只需写下命令然后提供必要的参数。例如：

`cube [0 2.5 0]`

要查看命令的语法，请查看帮助日志：

`- cube: Creates a cube at specified position -> TestScript.CreateCubeAt(Vector3 position)`

这里，命令是 *cube*，它接受一个 *Vector3* 参数。此命令调用 *TestScript* 脚本中的 *CreateCubeAt* 函数（有关实现详细信息，请参见下面的示例代码）。

控制台使用简单的算法来解析命令输入，并有一些限制：

- 用引号包装字符串（" 或 '）
- 用方括号（\[\]）或圆括号（()）包装向量

但是，语法中也有一些灵活性：

- 您可以提供空向量来表示 Vector_.zero：\[\]
- 您可以输入 1 而不是 true，或 0 而不是 false
- 对于空的 GameObject 和/或 Component 参数，您可以输入 `null`

### 注册自定义命令

如果函数的所有参数都是支持的类型，您可以通过四种不同的方式将函数注册到控制台（所有这些方法都在末尾接受可选的字符串参数，以为注册函数的参数指定自定义显示名称）：

- **ConsoleMethod 属性** *（UWP 平台不支持）*

只需将 **IngameDebugConsole.ConsoleMethod** 属性添加到您的函数中。这些函数必须是 *public static* 并且必须位于 *public* 类中。这些约束不适用于其他 3 种方法。

```csharp
using UnityEngine;
using IngameDebugConsole;

public class TestScript : MonoBehaviour
{
	[ConsoleMethod( "cube", "Creates a cube at specified position" )]
	public static void CreateCubeAt( Vector3 position )
	{
		GameObject.CreatePrimitive( PrimitiveType.Cube ).transform.position = position;
	}
}
```

- **强类型函数**

使用 `DebugLogConsole.AddCommand( string command, string description, System.Action method )` 变体之一：

```csharp
using UnityEngine;
using IngameDebugConsole;

public class TestScript : MonoBehaviour
{
	void Start()
	{
		DebugLogConsole.AddCommand( "destroy", "Destroys " + name, Destroy );
		DebugLogConsole.AddCommand<Vector3>( "cube", "Creates a cube at specified position", CreateCubeAt );
		DebugLogConsole.AddCommand<string, GameObject>( "child", "Creates a new child object under " + name, AddChild );
	}

	void Destroy()
	{
		Destroy( gameObject );
	}

	public static void CreateCubeAt( Vector3 position )
	{
		GameObject.CreatePrimitive( PrimitiveType.Cube ).transform.position = position;
	}

	private GameObject AddChild( string name )
	{
		GameObject child = new GameObject( name );
		child.transform.SetParent( transform );

		return child;
	}
}
```

- **静态函数（弱类型）**

使用 `DebugLogConsole.AddCommandStatic( string command, string description, string methodName, System.Type ownerType )`。这里，**methodName** 是字符串格式的方法名称，**ownerType** 是拥有者类的类型。以字符串形式提供方法名称和/或提供类的类型可能看起来很奇怪；但是，经过数小时的研究，我发现这是在不知道方法签名的情况下将任何具有任意数量参数和参数类型的函数注册到系统中的最佳方法。

```csharp
using UnityEngine;
using IngameDebugConsole;

public class TestScript : MonoBehaviour
{
	void Start()
	{
		DebugLogConsole.AddCommandStatic( "cube", "Creates a cube at specified position", "CreateCubeAt", typeof( TestScript ) );
	}
	
	public static void CreateCubeAt( Vector3 position )
	{
		GameObject.CreatePrimitive( PrimitiveType.Cube ).transform.position = position;
	}
}
```

- **实例函数（弱类型）**

使用 `DebugLogConsole.AddCommandInstance( string command, string description, string methodName, object instance )`：

```csharp
using UnityEngine;
using IngameDebugConsole;

public class TestScript : MonoBehaviour
{
	void Start()
	{
		DebugLogConsole.AddCommandInstance( "cube", "Creates a cube at specified position", "CreateCubeAt", this );
	}
	
	void CreateCubeAt( Vector3 position )
	{
		GameObject.CreatePrimitive( PrimitiveType.Cube ).transform.position = position;
	}
}
```

与 *AddCommandStatic* 的唯一区别是，您必须提供拥有该函数的类的实际实例，而不是类的类型。

### 移除命令

使用 `DebugLogConsole.RemoveCommand( string command )` 或 `DebugLogConsole.RemoveCommand( System.Action method )` 变体之一。

### 扩展支持的参数类型

- **强类型函数**

使用 `DebugLogConsole.AddCustomParameterType( Type type, ParseFunction parseFunction, string typeReadableName = null )`：

```csharp
using UnityEngine;
using IngameDebugConsole;

public class TestScript : MonoBehaviour
{
	public class Person
	{
		public string Name;
		public int Age;
	}

	void Start()
	{
		// Person 参数现在可以在命令中使用，例如 ('John Doe' 18)
		DebugLogConsole.AddCustomParameterType( typeof( Person ), ParsePerson );
	}
	
	private static bool ParsePerson( string input, out object output )
	{
		// 分割输入
		// 这将把 ('John Doe' 18) 转换为 2 个字符串："John Doe"（不带引号）和 "18"（不带引号）
		List<string> inputSplit = new List<string>( 2 );
		DebugLogConsole.FetchArgumentsFromCommand( input, inputSplit );

		// 我们需要 2 个参数：Name 和 Age
		if( inputSplit.Count != 2 )
		{
			output = null;
			return false;
		}

		// 尝试解析年龄
		object age;
		if( !DebugLogConsole.ParseInt( inputSplit[1], out age ) )
		{
			output = null;
			return false;
		}

		// 创建结果对象并将其分配给 output
		output = new Person()
		{
			Name = inputSplit[0],
			Age = (int) age
		};

		// 成功解析！
		return true;
	}
}
```

- **ConsoleCustomTypeParser 属性**

只需将 **IngameDebugConsole.ConsoleCustomTypeParser** 属性添加到您的函数中。这些函数必须具有以下签名：`public static bool ParseFunction( string input, out object output );`

```csharp
using UnityEngine;
using IngameDebugConsole;

public class TestScript : MonoBehaviour
{
	[ConsoleCustomTypeParser( typeof( Person ) )]
	public static bool ParsePerson( string input, out object output )
	{
		// 与上面相同...
	}
}
```

### 移除支持的自定义参数类型

使用 `DebugLogConsole.RemoveCustomParameterType( Type type )`。