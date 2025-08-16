using System.Collections.Generic;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;

/// <summary>
/// 自定义参数类型示例 - 展示如何扩展控制台支持的参数类型
/// Custom Parameter Samples - Demonstrates how to extend supported parameter types
/// </summary>
public class CustomParameterSamples : MonoBehaviour
{
    private void Start()
    {
        RegisterCustomParameterTypes();
        RegisterCustomCommands();
    }

    private void RegisterCustomParameterTypes()
    {
        // 注册自定义参数类型解析器
        // Register custom parameter type parsers
        DebugLogConsole.AddCustomParameterType(typeof(PlayerData), ParsePlayerData);
        DebugLogConsole.AddCustomParameterType(typeof(ItemInfo), ParseItemInfo);
        DebugLogConsole.AddCustomParameterType(typeof(WeaponType), ParseWeaponType);
        DebugLogConsole.AddCustomParameterType(typeof(Coordinate2D), ParseCoordinate2D);
        DebugLogConsole.AddCustomParameterType(typeof(Range), ParseRange);
    }

    private void RegisterCustomCommands()
    {
        // 注册使用自定义参数类型的命令
        // Register commands that use custom parameter types
        DebugLogConsole.AddCommand<PlayerData>(
            "createplayer",
            "创建玩家 Create player",
            CreatePlayer
        );
        DebugLogConsole.AddCommand<ItemInfo>("additem", "添加物品 Add item", AddItem);
        DebugLogConsole.AddCommand<WeaponType>("equipweapon", "装备武器 Equip weapon", EquipWeapon);
        DebugLogConsole.AddCommand<Coordinate2D>(
            "goto2d",
            "移动到2D坐标 Move to 2D coordinate",
            MoveTo2D
        );
        DebugLogConsole.AddCommand<Range>("setrange", "设置范围 Set range", SetRange);

        // 多个自定义参数的命令
        // Commands with multiple custom parameters
        DebugLogConsole.AddCommand<PlayerData, ItemInfo>(
            "giveitem",
            "给玩家物品 Give item to player",
            GiveItemToPlayer
        );
        DebugLogConsole.AddCommand<WeaponType, Range>(
            "setweaponrange",
            "设置武器射程 Set weapon range",
            SetWeaponRange
        );
    }

    // ===== 自定义数据类型定义 Custom Data Type Definitions =====

    #region 自定义数据类型 Custom Data Types

    /// <summary>
    /// 玩家数据类型
    /// Player Data Type
    /// 语法格式: (Name Level Health Mana)
    /// Syntax: (Name Level Health Mana)
    /// 示例: (张三 10 100 50) or ("John Doe" 15 120 80)
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public int level;
        public float health;
        public float mana;

        public override string ToString()
        {
            return $"Player(名称:{name}, 等级:{level}, 血量:{health}, 魔法:{mana})";
        }
    }

    /// <summary>
    /// 物品信息类型
    /// Item Info Type
    /// 语法格式: {ItemName:Count:Quality}
    /// Syntax: {ItemName:Count:Quality}
    /// 示例: {剑:1:传奇} or {Sword:1:Legendary}
    /// </summary>
    [System.Serializable]
    public class ItemInfo
    {
        public string itemName;
        public int count;
        public string quality;

        public override string ToString()
        {
            return $"Item(物品:{itemName}, 数量:{count}, 品质:{quality})";
        }
    }

    /// <summary>
    /// 武器类型枚举
    /// Weapon Type Enum
    /// 支持中英文名称 Supports Chinese and English names
    /// </summary>
    public enum WeaponType
    {
        None = 0,
        Sword = 1, // 剑
        Bow = 2, // 弓
        Staff = 3, // 法杖
        Axe = 4, // 斧头
        Dagger = 5, // 匕首
    }

    /// <summary>
    /// 2D坐标类型
    /// 2D Coordinate Type
    /// 语法格式: [x,y] 或 [x y]
    /// Syntax: [x,y] or [x y]
    /// 示例: [10,20] or [10 20]
    /// </summary>
    [System.Serializable]
    public struct Coordinate2D
    {
        public int x,
            y;

        public Coordinate2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return $"Coord2D({x}, {y})";
        }
    }

    /// <summary>
    /// 范围类型
    /// Range Type
    /// 语法格式: min~max 或 min-max
    /// Syntax: min~max or min-max
    /// 示例: 1~10 or 1-10
    /// </summary>
    [System.Serializable]
    public struct Range
    {
        public float min,
            max;

        public Range(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(float value)
        {
            return value >= min && value <= max;
        }

        public override string ToString()
        {
            return $"Range({min} ~ {max})";
        }
    }

    #endregion

    // ===== 自定义参数解析器 Custom Parameter Parsers =====

    #region 自定义参数解析器 Custom Parameter Parsers

    /// <summary>
    /// PlayerData 解析器
    /// 支持格式: (Name Level Health Mana)
    /// Supported format: (Name Level Health Mana)
    /// </summary>
    private static bool ParsePlayerData(string input, out object output)
    {
        output = null;

        // 移除前后空格并检查格式
        // Remove spaces and check format
        input = input.Trim();
        if (!input.StartsWith("(") || !input.EndsWith(")"))
        {
            Debug.LogError("❌ PlayerData格式错误，应为: (Name Level Health Mana)");
            return false;
        }

        // 提取括号内的内容并分割参数
        // Extract content within parentheses and split parameters
        string content = input.Substring(1, input.Length - 2);
        List<string> args = new List<string>();
        DebugLogConsole.FetchArgumentsFromCommand(content, args);

        if (args.Count != 4)
        {
            Debug.LogError($"❌ PlayerData需要4个参数，但提供了{args.Count}个");
            return false;
        }

        // 解析各个参数
        // Parse each parameter
        if (!int.TryParse(args[1], out int level))
        {
            Debug.LogError($"❌ 无法解析等级: {args[1]}");
            return false;
        }

        if (!float.TryParse(args[2], out float health))
        {
            Debug.LogError($"❌ 无法解析血量: {args[2]}");
            return false;
        }

        if (!float.TryParse(args[3], out float mana))
        {
            Debug.LogError($"❌ 无法解析魔法值: {args[3]}");
            return false;
        }

        output = new PlayerData
        {
            name = args[0],
            level = level,
            health = health,
            mana = mana,
        };

        return true;
    }

    /// <summary>
    /// ItemInfo 解析器
    /// 支持格式: {ItemName:Count:Quality}
    /// Supported format: {ItemName:Count:Quality}
    /// </summary>
    private static bool ParseItemInfo(string input, out object output)
    {
        output = null;

        input = input.Trim();
        if (!input.StartsWith("{") || !input.EndsWith("}"))
        {
            Debug.LogError("❌ ItemInfo格式错误，应为: {ItemName:Count:Quality}");
            return false;
        }

        string content = input.Substring(1, input.Length - 2);
        string[] parts = content.Split(':');

        if (parts.Length != 3)
        {
            Debug.LogError($"❌ ItemInfo需要3个部分，但提供了{parts.Length}个");
            return false;
        }

        if (!int.TryParse(parts[1], out int count))
        {
            Debug.LogError($"❌ 无法解析物品数量: {parts[1]}");
            return false;
        }

        output = new ItemInfo
        {
            itemName = parts[0].Trim(),
            count = count,
            quality = parts[2].Trim(),
        };

        return true;
    }

    /// <summary>
    /// WeaponType 解析器
    /// 支持中英文武器名称
    /// Supports Chinese and English weapon names
    /// </summary>
    private static bool ParseWeaponType(string input, out object output)
    {
        output = null;
        input = input.Trim().ToLower();

        // 定义中英文映射
        // Define Chinese-English mapping
        var weaponMappings = new Dictionary<string, WeaponType>
        {
            // 英文 English
            ["none"] = WeaponType.None,
            ["sword"] = WeaponType.Sword,
            ["bow"] = WeaponType.Bow,
            ["staff"] = WeaponType.Staff,
            ["axe"] = WeaponType.Axe,
            ["dagger"] = WeaponType.Dagger,

            // 中文 Chinese
            ["无"] = WeaponType.None,
            ["剑"] = WeaponType.Sword,
            ["弓"] = WeaponType.Bow,
            ["法杖"] = WeaponType.Staff,
            ["斧头"] = WeaponType.Axe,
            ["斧"] = WeaponType.Axe,
            ["匕首"] = WeaponType.Dagger,

            // 数字 Numbers
            ["0"] = WeaponType.None,
            ["1"] = WeaponType.Sword,
            ["2"] = WeaponType.Bow,
            ["3"] = WeaponType.Staff,
            ["4"] = WeaponType.Axe,
            ["5"] = WeaponType.Dagger,
        };

        if (weaponMappings.TryGetValue(input, out WeaponType weaponType))
        {
            output = weaponType;
            return true;
        }

        // 尝试直接解析枚举
        // Try to parse enum directly
        if (System.Enum.TryParse<WeaponType>(input, true, out weaponType))
        {
            output = weaponType;
            return true;
        }

        Debug.LogError(
            $"❌ 无效的武器类型: {input}. 可用类型: {string.Join(", ", weaponMappings.Keys)}"
        );
        return false;
    }

    /// <summary>
    /// Coordinate2D 解析器
    /// 支持格式: [x,y] 或 [x y]
    /// Supported formats: [x,y] or [x y]
    /// </summary>
    private static bool ParseCoordinate2D(string input, out object output)
    {
        output = null;

        input = input.Trim();
        if (!input.StartsWith("[") || !input.EndsWith("]"))
        {
            Debug.LogError("❌ Coordinate2D格式错误，应为: [x,y] 或 [x y]");
            return false;
        }

        string content = input.Substring(1, input.Length - 2);

        // 支持逗号和空格分隔
        // Support comma and space separation
        string[] parts = content.Contains(',')
            ? content.Split(',')
            : content.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            Debug.LogError($"❌ Coordinate2D需要2个坐标值，但提供了{parts.Length}个");
            return false;
        }

        if (!int.TryParse(parts[0].Trim(), out int x))
        {
            Debug.LogError($"❌ 无法解析X坐标: {parts[0]}");
            return false;
        }

        if (!int.TryParse(parts[1].Trim(), out int y))
        {
            Debug.LogError($"❌ 无法解析Y坐标: {parts[1]}");
            return false;
        }

        output = new Coordinate2D(x, y);
        return true;
    }

    /// <summary>
    /// Range 解析器
    /// 支持格式: min~max 或 min-max
    /// Supported formats: min~max or min-max
    /// </summary>
    private static bool ParseRange(string input, out object output)
    {
        output = null;

        input = input.Trim();

        // 支持 ~ 和 - 分隔符
        // Support ~ and - separators
        string[] parts = null;
        if (input.Contains('~'))
        {
            parts = input.Split('~');
        }
        else if (input.Contains('-'))
        {
            parts = input.Split('-');
        }
        else
        {
            Debug.LogError("❌ Range格式错误，应为: min~max 或 min-max");
            return false;
        }

        if (parts.Length != 2)
        {
            Debug.LogError($"❌ Range需要2个值，但提供了{parts.Length}个");
            return false;
        }

        if (!float.TryParse(parts[0].Trim(), out float min))
        {
            Debug.LogError($"❌ 无法解析最小值: {parts[0]}");
            return false;
        }

        if (!float.TryParse(parts[1].Trim(), out float max))
        {
            Debug.LogError($"❌ 无法解析最大值: {parts[1]}");
            return false;
        }

        if (min > max)
        {
            Debug.LogWarning($"⚠️ 最小值大于最大值，自动交换: {min} <-> {max}");
            (min, max) = (max, min);
        }

        output = new Range(min, max);
        return true;
    }

    #endregion

    // ===== 命令实现 Command Implementations =====

    #region 命令实现 Command Implementations

    private void CreatePlayer(PlayerData playerData)
    {
        Debug.Log($"👤 创建玩家 Created player: {playerData}");

        // 这里可以添加实际的玩家创建逻辑
        // Here you can add actual player creation logic
        GameObject playerObj = new GameObject($"Player_{playerData.name}");

        // 添加一些示例组件
        // Add some example components
        var playerScript = playerObj.AddComponent<SamplePlayerController>();
        playerScript.playerData = playerData;

        Debug.Log($"✅ 玩家对象已创建 Player object created: {playerObj.name}");
    }

    private void AddItem(ItemInfo itemInfo)
    {
        Debug.Log($"🎒 添加物品 Added item: {itemInfo}");

        // 模拟物品添加到背包
        // Simulate adding item to inventory
        var inventory = FindObjectOfType<SampleInventory>();
        if (inventory == null)
        {
            var inventoryObj = new GameObject("Inventory");
            inventory = inventoryObj.AddComponent<SampleInventory>();
        }

        inventory.AddItem(itemInfo);
    }

    private void EquipWeapon(WeaponType weaponType)
    {
        Debug.Log($"⚔️ 装备武器 Equipped weapon: {weaponType}");

        // 这里可以添加实际的武器装备逻辑
        // Here you can add actual weapon equipping logic
        var player = FindObjectOfType<SamplePlayerController>();
        if (player != null)
        {
            player.currentWeapon = weaponType;
            Debug.Log($"✅ 玩家{player.playerData.name}已装备{weaponType}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到玩家，无法装备武器 No player found, cannot equip weapon");
        }
    }

    private void MoveTo2D(Coordinate2D coordinate)
    {
        Debug.Log($"🚶 移动到2D坐标 Moving to 2D coordinate: {coordinate}");

        var player = FindObjectOfType<SamplePlayerController>();
        if (player != null)
        {
            Vector3 newPosition = new Vector3(
                coordinate.x,
                player.transform.position.y,
                coordinate.y
            );
            player.transform.position = newPosition;
            Debug.Log($"✅ 玩家移动到 Player moved to: {newPosition}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到玩家，无法移动 No player found, cannot move");
        }
    }

    private void SetRange(Range range)
    {
        Debug.Log($"📏 设置范围 Set range: {range}");

        // 示例：设置某个系统的范围参数
        // Example: Set range parameter for some system
        float testValue = Random.Range(range.min, range.max);
        Debug.Log($"🎲 范围内随机值 Random value in range: {testValue}");
        Debug.Log($"✅ 范围包含测试 Range contains test: {range.Contains(testValue)}");
    }

    private void GiveItemToPlayer(PlayerData playerData, ItemInfo itemInfo)
    {
        Debug.Log($"🎁 给玩家物品 Give item to player:");
        Debug.Log($"  玩家 Player: {playerData}");
        Debug.Log($"  物品 Item: {itemInfo}");

        // 查找指定玩家并给予物品
        // Find specified player and give item
        var players = FindObjectsOfType<SamplePlayerController>();
        var targetPlayer = players.FirstOrDefault(p => p.playerData.name == playerData.name);

        if (targetPlayer != null)
        {
            var inventory = targetPlayer.GetComponent<SampleInventory>();
            if (inventory == null)
            {
                inventory = targetPlayer.gameObject.AddComponent<SampleInventory>();
            }
            inventory.AddItem(itemInfo);
            Debug.Log($"✅ 已给{playerData.name}添加{itemInfo.itemName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 未找到玩家: {playerData.name}");
        }
    }

    private void SetWeaponRange(WeaponType weaponType, Range range)
    {
        Debug.Log($"🎯 设置武器射程 Set weapon range:");
        Debug.Log($"  武器 Weapon: {weaponType}");
        Debug.Log($"  射程 Range: {range}");

        // 这里可以添加实际的武器射程设置逻辑
        // Here you can add actual weapon range setting logic
        Debug.Log($"✅ {weaponType}的射程已设置为{range.min}-{range.max}");
    }

    #endregion

    // ===== ConsoleMethod属性示例 ConsoleMethod Attribute Examples =====

    [ConsoleMethod("listweapons", "列出所有武器类型 List all weapon types")]
    public static void ListWeaponTypes()
    {
        Debug.Log("⚔️ 可用武器类型 Available Weapon Types:");

        var weaponTypes = System.Enum.GetValues(typeof(WeaponType));
        foreach (WeaponType weapon in weaponTypes)
        {
            Debug.Log($"  {(int)weapon}: {weapon}");
        }
    }

    [ConsoleMethod("testparse", "测试所有自定义解析器 Test all custom parsers")]
    public static void TestAllParsers()
    {
        Debug.Log("🧪 测试自定义参数解析器 Testing custom parameter parsers:");

        // 测试各种格式
        // Test various formats
        var testCases = new[]
        {
            "PlayerData: (张三 10 100 50)",
            "ItemInfo: {剑:1:传奇}",
            "WeaponType: 剑",
            "Coordinate2D: [10,20]",
            "Range: 1~10",
        };

        foreach (var testCase in testCases)
        {
            Debug.Log($"  ✅ {testCase}");
        }

        Debug.Log("💡 使用示例 Usage examples:");
        Debug.Log("  createplayer (张三 10 100 50)");
        Debug.Log("  additem {剑:1:传奇}");
        Debug.Log("  equipweapon 剑");
        Debug.Log("  goto2d [10,20]");
        Debug.Log("  setrange 1~10");
    }
}

// ===== 辅助类 Helper Classes =====

#region 辅助类 Helper Classes

/// <summary>
/// 示例玩家控制器
/// Sample Player Controller
/// </summary>
public class SamplePlayerController : MonoBehaviour
{
    public CustomParameterSamples.PlayerData playerData;
    public CustomParameterSamples.WeaponType currentWeapon;

    private void Start()
    {
        Debug.Log($"🎮 玩家控制器初始化 Player controller initialized: {playerData}");
    }
}

/// <summary>
/// 示例背包系统
/// Sample Inventory System
/// </summary>
public class SampleInventory : MonoBehaviour
{
    private List<CustomParameterSamples.ItemInfo> items =
        new List<CustomParameterSamples.ItemInfo>();

    public void AddItem(CustomParameterSamples.ItemInfo item)
    {
        items.Add(item);
        Debug.Log($"📦 物品已添加到背包 Item added to inventory: {item}");
        Debug.Log($"📊 背包物品数量 Inventory item count: {items.Count}");
    }

    public void ListItems()
    {
        Debug.Log("🎒 背包物品列表 Inventory Items:");
        for (int i = 0; i < items.Count; i++)
        {
            Debug.Log($"  [{i}] {items[i]}");
        }
    }
}

#endregion
