using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理示例 - 展示如何使用调试控制台进行游戏管理
/// Game Manager Samples - Demonstrates using debug console for game management
/// </summary>
public class GameManagerSamples : MonoBehaviour
{
    [Header("游戏状态 Game State")]
    public bool isGamePaused = false;
    public float gameSpeed = 1f;
    public int playerLives = 3;
    public int currentScore = 0;
    public int currentLevel = 1;

    [Header("玩家设置 Player Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    private GameObject currentPlayer;
    private Dictionary<string, object> gameSettings;
    private List<string> cheatCodes;

    private void Start()
    {
        InitializeGameSettings();
        RegisterGameCommands();
    }

    private void InitializeGameSettings()
    {
        gameSettings = new Dictionary<string, object>
        {
            ["invincible"] = false,
            ["unlimitedAmmo"] = false,
            ["speedMultiplier"] = 1f,
            ["debugMode"] = false,
        };

        cheatCodes = new List<string> { "GODMODE", "NOCLIP", "SPEEDRUN", "SHOWFPS" };
    }

    private void RegisterGameCommands()
    {
        // ===== 游戏状态控制 Game State Control =====
        DebugLogConsole.AddCommand("pause", "暂停/恢复游戏 Pause/Resume game", TogglePause);
        DebugLogConsole.AddCommand<float>("speed", "设置游戏速度 Set game speed", SetGameSpeed);
        DebugLogConsole.AddCommand("restart", "重启当前关卡 Restart current level", RestartLevel);
        DebugLogConsole.AddCommand("quit", "退出游戏 Quit game", QuitGame);

        // ===== 关卡管理 Level Management =====
        DebugLogConsole.AddCommand<int>(
            "loadlevel",
            "加载指定关卡 Load specified level",
            LoadLevel
        );
        DebugLogConsole.AddCommand("nextlevel", "下一关 Next level", NextLevel);
        DebugLogConsole.AddCommand("prevlevel", "上一关 Previous level", PreviousLevel);
        DebugLogConsole.AddCommand<string>("loadscene", "加载场景 Load scene", LoadScene);

        // ===== 玩家管理 Player Management =====
        DebugLogConsole.AddCommand("respawn", "重生玩家 Respawn player", RespawnPlayer);
        DebugLogConsole.AddCommand<int>("setlives", "设置生命数 Set lives", SetPlayerLives);
        DebugLogConsole.AddCommand<int>("addscore", "增加分数 Add score", AddScore);
        DebugLogConsole.AddCommand<int>("setscore", "设置分数 Set score", SetScore);

        // ===== 作弊功能 Cheat Functions =====
        DebugLogConsole.AddCommand<string>("cheat", "输入作弊码 Enter cheat code", EnterCheatCode);
        DebugLogConsole.AddCommand<bool>("god", "无敌模式 God mode", SetGodMode);
        DebugLogConsole.AddCommand<bool>("fly", "飞行模式 Fly mode", SetFlyMode);
        DebugLogConsole.AddCommand<float>(
            "setspeed",
            "设置玩家速度 Set player speed",
            SetPlayerSpeed
        );

        // ===== 游戏设置 Game Settings =====
        DebugLogConsole.AddCommand<string, object>(
            "setting",
            "设置游戏参数 Set game setting",
            SetGameSetting
        );
        DebugLogConsole.AddCommand<string>(
            "getsetting",
            "获取游戏参数 Get game setting",
            GetGameSetting
        );
        DebugLogConsole.AddCommand(
            "listsettings",
            "列出所有设置 List all settings",
            ListGameSettings
        );
        DebugLogConsole.AddCommand(
            "resetsettings",
            "重置所有设置 Reset all settings",
            ResetGameSettings
        );
    }

    // ===== 游戏状态控制实现 Game State Control Implementation =====

    #region 游戏状态控制 Game State Control

    private void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : gameSpeed;
        Debug.Log(
            $"⏯️ 游戏{(isGamePaused ? "暂停" : "继续")} Game {(isGamePaused ? "Paused" : "Resumed")}"
        );
    }

    private void SetGameSpeed(float speed)
    {
        if (speed < 0.1f || speed > 10f)
        {
            Debug.LogWarning("⚠️ 游戏速度范围: 0.1 - 10.0 Game speed range: 0.1 - 10.0");
            return;
        }

        gameSpeed = speed;
        if (!isGamePaused)
        {
            Time.timeScale = speed;
        }
        Debug.Log($"⚡ 游戏速度设置为 Game speed set to: {speed}x");
    }

    private void RestartLevel()
    {
        Debug.Log("🔄 重启关卡 Restarting level...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void QuitGame()
    {
        Debug.Log("👋 退出游戏 Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region 关卡管理 Level Management

    private void LoadLevel(int levelNumber)
    {
        if (levelNumber < 1)
        {
            Debug.LogWarning("⚠️ 关卡号必须大于0 Level number must be greater than 0");
            return;
        }

        currentLevel = levelNumber;
        string sceneName = $"Level{levelNumber:D2}";
        Debug.Log($"🎮 加载关卡 Loading level: {levelNumber} ({sceneName})");

        // 这里可以添加实际的关卡加载逻辑
        // Here you can add actual level loading logic
        StartCoroutine(LoadLevelCoroutine(sceneName));
    }

    private IEnumerator LoadLevelCoroutine(string sceneName)
    {
        Debug.Log($"⏳ 正在加载 Loading: {sceneName}...");

        // 模拟加载过程 Simulate loading process
        yield return new WaitForSeconds(1f);

        // 检查场景是否存在 Check if scene exists
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
            {
                sceneExists = true;
                break;
            }
        }

        if (sceneExists)
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"⚠️ 场景不存在 Scene does not exist: {sceneName}");
        }
    }

    private void NextLevel()
    {
        LoadLevel(currentLevel + 1);
    }

    private void PreviousLevel()
    {
        LoadLevel(Mathf.Max(1, currentLevel - 1));
    }

    private void LoadScene(string sceneName)
    {
        Debug.Log($"🎭 加载场景 Loading scene: {sceneName}");
        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 加载场景失败 Failed to load scene: {e.Message}");
        }
    }

    #endregion

    #region 玩家管理 Player Management

    private void RespawnPlayer()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }

        if (playerPrefab != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            currentPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log($"👤 玩家重生 Player respawned at: {spawnPoint.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未设置玩家预制件或重生点 Player prefab or spawn points not set");
        }
    }

    private void SetPlayerLives(int lives)
    {
        playerLives = Mathf.Max(0, lives);
        Debug.Log($"❤️ 玩家生命设置为 Player lives set to: {playerLives}");
    }

    private void AddScore(int points)
    {
        currentScore += points;
        Debug.Log($"🏆 分数增加 Score increased by {points}, 总分 Total: {currentScore}");
    }

    private void SetScore(int score)
    {
        currentScore = Mathf.Max(0, score);
        Debug.Log($"🎯 分数设置为 Score set to: {currentScore}");
    }

    #endregion

    #region 作弊功能 Cheat Functions

    private void EnterCheatCode(string code)
    {
        code = code.ToUpper();

        if (cheatCodes.Contains(code))
        {
            Debug.Log($"✅ 作弊码激活 Cheat code activated: {code}");

            switch (code)
            {
                case "GODMODE":
                    SetGodMode(true);
                    break;
                case "NOCLIP":
                    SetFlyMode(true);
                    break;
                case "SPEEDRUN":
                    SetPlayerSpeed(2f);
                    break;
                case "SHOWFPS":
                    SetGameSetting("debugMode", true);
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"❌ 无效作弊码 Invalid cheat code: {code}");
            Debug.Log($"💡 可用作弊码 Available codes: {string.Join(", ", cheatCodes)}");
        }
    }

    private void SetGodMode(bool enabled)
    {
        gameSettings["invincible"] = enabled;
        Debug.Log($"🛡️ 无敌模式 God mode: {(enabled ? "ON" : "OFF")}");

        // 这里可以添加实际的无敌逻辑
        // Here you can add actual invincibility logic
        if (currentPlayer != null)
        {
            // 示例：修改玩家碰撞层
            // Example: Modify player collision layer
            var collider = currentPlayer.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = enabled;
            }
        }
    }

    private void SetFlyMode(bool enabled)
    {
        Debug.Log($"✈️ 飞行模式 Fly mode: {(enabled ? "ON" : "OFF")}");

        // 这里可以添加实际的飞行逻辑
        // Here you can add actual flying logic
        if (currentPlayer != null)
        {
            var rb = currentPlayer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = !enabled;
            }
        }
    }

    private void SetPlayerSpeed(float multiplier)
    {
        gameSettings["speedMultiplier"] = multiplier;
        Debug.Log($"🏃 玩家速度倍数 Player speed multiplier: {multiplier}x");

        // 这里可以添加实际的速度修改逻辑
        // Here you can add actual speed modification logic
    }

    #endregion

    #region 游戏设置 Game Settings

    private void SetGameSetting(string key, object value)
    {
        if (gameSettings.ContainsKey(key))
        {
            gameSettings[key] = value;
            Debug.Log($"⚙️ 设置更新 Setting updated: {key} = {value}");
        }
        else
        {
            gameSettings.Add(key, value);
            Debug.Log($"➕ 新设置添加 New setting added: {key} = {value}");
        }
    }

    private void GetGameSetting(string key)
    {
        if (gameSettings.ContainsKey(key))
        {
            Debug.Log($"📖 设置值 Setting value: {key} = {gameSettings[key]}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 设置不存在 Setting not found: {key}");
        }
    }

    private void ListGameSettings()
    {
        Debug.Log("📋 所有游戏设置 All Game Settings:");
        foreach (var setting in gameSettings)
        {
            Debug.Log($"  {setting.Key}: {setting.Value}");
        }
    }

    private void ResetGameSettings()
    {
        InitializeGameSettings();
        Debug.Log("🔄 游戏设置已重置 Game settings reset to defaults");
    }

    #endregion

    // ===== ConsoleMethod属性示例 ConsoleMethod Attribute Examples =====

    [ConsoleMethod("status", "显示游戏状态 Show game status")]
    public static void ShowGameStatus()
    {
        var manager = FindObjectOfType<GameManagerSamples>();
        if (manager != null)
        {
            Debug.Log(
                $"📊 游戏状态 Game Status:\n"
                    + $"  关卡 Level: {manager.currentLevel}\n"
                    + $"  分数 Score: {manager.currentScore}\n"
                    + $"  生命 Lives: {manager.playerLives}\n"
                    + $"  速度 Speed: {manager.gameSpeed}x\n"
                    + $"  暂停 Paused: {manager.isGamePaused}"
            );
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到GameManagerSamples GameManagerSamples not found");
        }
    }

    [ConsoleMethod("scenes", "列出所有场景 List all scenes")]
    public static void ListAllScenes()
    {
        Debug.Log("🎭 可用场景 Available Scenes:");

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            string status =
                SceneManager.GetActiveScene().name == sceneName ? " (当前 Current)" : "";
            Debug.Log($"  [{i}] {sceneName}{status}");
        }
    }

    [ConsoleMethod("memory", "显示内存使用 Show memory usage")]
    public static void ShowMemoryUsage()
    {
        System.GC.Collect();

        long totalMemory = System.GC.GetTotalMemory(false);
        float memoryMB = totalMemory / (1024f * 1024f);

        Debug.Log(
            $"💾 内存使用 Memory Usage:\n"
                + $"  总内存 Total: {memoryMB:F2} MB\n"
                + $"  已分配 Allocated: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024f * 1024f):F2} MB\n"
                + $"  已保留 Reserved: {UnityEngine.Profiling.Profiler.GetTotalReservedMemory() / (1024f * 1024f):F2} MB"
        );
    }

    // ===== 实用工具方法 Utility Methods =====

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("📱 应用程序暂停 Application paused");
        }
        else
        {
            Debug.Log("📱 应用程序恢复 Application resumed");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Debug.Log("👁️ 应用程序失去焦点 Application lost focus");
        }
        else
        {
            Debug.Log("👁️ 应用程序获得焦点 Application gained focus");
        }
    }
}
