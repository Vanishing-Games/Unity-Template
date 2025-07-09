using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using IngameDebugConsole;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 性能监控示例 - 展示如何使用调试控制台进行性能监控和分析
/// Performance Monitor Samples - Demonstrates using debug console for performance monitoring and analysis
/// </summary>
public class PerformanceMonitorSamples : MonoBehaviour
{
    [Header("性能监控设置 Performance Settings")]
    public bool autoMonitoring = false;
    public float monitoringInterval = 1f;
    public int maxProfilerSamples = 100;

    [Header("内存监控 Memory Monitoring")]
    public bool trackGCAllocations = true;
    public float gcWarningThreshold = 50f; // MB

    private Coroutine monitoringCoroutine;
    private List<PerformanceSnapshot> performanceHistory;
    private Dictionary<string, CustomProfiler> customProfilers;
    private int frameCount = 0;
    private float deltaTimeSum = 0f;
    private float minFrameTime = float.MaxValue;
    private float maxFrameTime = 0f;

    private void Start()
    {
        InitializePerformanceMonitoring();
        RegisterPerformanceCommands();
    }

    private void InitializePerformanceMonitoring()
    {
        performanceHistory = new List<PerformanceSnapshot>();
        customProfilers = new Dictionary<string, CustomProfiler>();

        if (autoMonitoring)
        {
            StartMonitoring();
        }
    }

    private void RegisterPerformanceCommands()
    {
        // ===== 基础性能监控 Basic Performance Monitoring =====
        DebugLogConsole.AddCommand("fps", "显示当前FPS Show current FPS", ShowCurrentFPS);
        DebugLogConsole.AddCommand(
            "frametime",
            "显示帧时间统计 Show frame time stats",
            ShowFrameTimeStats
        );
        DebugLogConsole.AddCommand("memory", "显示内存使用情况 Show memory usage", ShowMemoryUsage);
        DebugLogConsole.AddCommand(
            "gc",
            "强制执行垃圾回收 Force garbage collection",
            ForceGarbageCollection
        );

        // ===== 监控控制 Monitoring Control =====
        DebugLogConsole.AddCommand(
            "startmonitor",
            "开始性能监控 Start performance monitoring",
            StartMonitoring
        );
        DebugLogConsole.AddCommand(
            "stopmonitor",
            "停止性能监控 Stop performance monitoring",
            StopMonitoring
        );
        DebugLogConsole.AddCommand<float>(
            "setinterval",
            "设置监控间隔 Set monitoring interval",
            SetMonitoringInterval
        );
        DebugLogConsole.AddCommand(
            "clearhistory",
            "清空性能历史 Clear performance history",
            ClearPerformanceHistory
        );

        // ===== 性能分析 Performance Analysis =====
        DebugLogConsole.AddCommand(
            "analyze",
            "分析性能数据 Analyze performance data",
            AnalyzePerformance
        );
        DebugLogConsole.AddCommand<int>(
            "history",
            "显示性能历史 Show performance history",
            ShowPerformanceHistory
        );
        DebugLogConsole.AddCommand(
            "summary",
            "显示性能总结 Show performance summary",
            ShowPerformanceSummary
        );

        // ===== 自定义性能分析器 Custom Profilers =====
        DebugLogConsole.AddCommand<string>(
            "startprofiler",
            "开始自定义分析器 Start custom profiler",
            StartCustomProfiler
        );
        DebugLogConsole.AddCommand<string>(
            "stopprofiler",
            "停止自定义分析器 Stop custom profiler",
            StopCustomProfiler
        );
        DebugLogConsole.AddCommand(
            "listprofilers",
            "列出所有分析器 List all profilers",
            ListCustomProfilers
        );
        DebugLogConsole.AddCommand<string>(
            "profilerstats",
            "显示分析器统计 Show profiler stats",
            ShowProfilerStats
        );

        // ===== 系统信息 System Information =====
        DebugLogConsole.AddCommand(
            "sysinfo",
            "显示系统信息 Show system information",
            ShowSystemInfo
        );
        DebugLogConsole.AddCommand("gpuinfo", "显示GPU信息 Show GPU information", ShowGPUInfo);
        DebugLogConsole.AddCommand(
            "qualitysettings",
            "显示质量设置 Show quality settings",
            ShowQualitySettings
        );

        // ===== 压力测试 Stress Testing =====
        DebugLogConsole.AddCommand<int>(
            "stresstest",
            "执行压力测试 Execute stress test",
            ExecuteStressTest
        );
        DebugLogConsole.AddCommand<int, int>(
            "spawnobjects",
            "生成测试对象 Spawn test objects",
            SpawnTestObjects
        );
        DebugLogConsole.AddCommand(
            "clearobjects",
            "清理测试对象 Clear test objects",
            ClearTestObjects
        );
    }

    // ===== 性能数据结构 Performance Data Structures =====

    #region 性能数据结构 Performance Data Structures

    [System.Serializable]
    public class PerformanceSnapshot
    {
        public float timestamp;
        public float fps;
        public float frameTime;
        public long memoryUsed;
        public int drawCalls;
        public int triangles;

        public PerformanceSnapshot()
        {
            timestamp = Time.realtimeSinceStartup;
            fps = 1f / Time.unscaledDeltaTime;
            frameTime = Time.unscaledDeltaTime * 1000f; // ms
            memoryUsed = System.GC.GetTotalMemory(false);
            drawCalls =
                UnityEngine.Rendering.RenderPipelineManager.currentPipeline != null
                    ? 0
                    : UnityStats.batches;
            triangles = UnityStats.triangles;
        }
    }

    public class CustomProfiler
    {
        public string name;
        public List<float> samples;
        public Stopwatch stopwatch;
        public bool isRunning;

        public CustomProfiler(string name)
        {
            this.name = name;
            this.samples = new List<float>();
            this.stopwatch = new Stopwatch();
            this.isRunning = false;
        }

        public void Start()
        {
            stopwatch.Restart();
            isRunning = true;
        }

        public void Stop()
        {
            if (isRunning)
            {
                stopwatch.Stop();
                samples.Add((float)stopwatch.Elapsed.TotalMilliseconds);
                isRunning = false;

                // 限制样本数量
                if (samples.Count > 1000)
                {
                    samples.RemoveAt(0);
                }
            }
        }

        public float GetAverageTime()
        {
            if (samples.Count == 0)
                return 0f;
            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample;
            }
            return sum / samples.Count;
        }

        public float GetMinTime()
        {
            if (samples.Count == 0)
                return 0f;
            float min = float.MaxValue;
            foreach (float sample in samples)
            {
                if (sample < min)
                    min = sample;
            }
            return min;
        }

        public float GetMaxTime()
        {
            if (samples.Count == 0)
                return 0f;
            float max = 0f;
            foreach (float sample in samples)
            {
                if (sample > max)
                    max = sample;
            }
            return max;
        }
    }

    #endregion

    // ===== 基础性能监控实现 Basic Performance Monitoring Implementation =====

    #region 基础性能监控 Basic Performance Monitoring

    private void Update()
    {
        // 更新帧时间统计
        frameCount++;
        deltaTimeSum += Time.unscaledDeltaTime;

        float currentFrameTime = Time.unscaledDeltaTime;
        if (currentFrameTime < minFrameTime)
            minFrameTime = currentFrameTime;
        if (currentFrameTime > maxFrameTime)
            maxFrameTime = currentFrameTime;
    }

    private void ShowCurrentFPS()
    {
        float currentFPS = 1f / Time.unscaledDeltaTime;
        float averageFPS = frameCount / deltaTimeSum;

        UnityEngine.Debug.Log(
            $"📊 FPS信息 FPS Information:\n"
                + $"  当前FPS Current: {currentFPS:F1}\n"
                + $"  平均FPS Average: {averageFPS:F1}\n"
                + $"  最低FPS Min: {1f / maxFrameTime:F1}\n"
                + $"  最高FPS Max: {1f / minFrameTime:F1}"
        );
    }

    private void ShowFrameTimeStats()
    {
        float currentFrameTime = Time.unscaledDeltaTime * 1000f;
        float averageFrameTime = (deltaTimeSum / frameCount) * 1000f;
        float minFrameTimeMs = minFrameTime * 1000f;
        float maxFrameTimeMs = maxFrameTime * 1000f;

        UnityEngine.Debug.Log(
            $"⏱️ 帧时间统计 Frame Time Statistics:\n"
                + $"  当前帧时间 Current: {currentFrameTime:F2}ms\n"
                + $"  平均帧时间 Average: {averageFrameTime:F2}ms\n"
                + $"  最短帧时间 Min: {minFrameTimeMs:F2}ms\n"
                + $"  最长帧时间 Max: {maxFrameTimeMs:F2}ms\n"
                + $"  总帧数 Total Frames: {frameCount}"
        );
    }

    private void ShowMemoryUsage()
    {
        System.GC.Collect();

        long totalMemory = System.GC.GetTotalMemory(false);
        float memoryMB = totalMemory / (1024f * 1024f);

        long allocatedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
        float allocatedMB = allocatedMemory / (1024f * 1024f);

        long reservedMemory = UnityEngine.Profiling.Profiler.GetTotalReservedMemory();
        float reservedMB = reservedMemory / (1024f * 1024f);

        UnityEngine.Debug.Log(
            $"💾 内存使用 Memory Usage:\n"
                + $"  总内存 Total: {memoryMB:F2} MB\n"
                + $"  已分配 Allocated: {allocatedMB:F2} MB\n"
                + $"  已保留 Reserved: {reservedMB:F2} MB\n"
                + $"  单帧分配 Frame Alloc: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024f * 1024f):F2} MB"
        );

        // 检查内存警告
        if (memoryMB > gcWarningThreshold)
        {
            UnityEngine.Debug.LogWarning(
                $"⚠️ 内存使用过高 High memory usage: {memoryMB:F2} MB > {gcWarningThreshold} MB"
            );
        }
    }

    private void ForceGarbageCollection()
    {
        long beforeGC = System.GC.GetTotalMemory(false);
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        long afterGC = System.GC.GetTotalMemory(false);

        float freedMB = (beforeGC - afterGC) / (1024f * 1024f);

        UnityEngine.Debug.Log(
            $"🧹 垃圾回收完成 Garbage Collection Completed:\n"
                + $"  回收前 Before: {beforeGC / (1024f * 1024f):F2} MB\n"
                + $"  回收后 After: {afterGC / (1024f * 1024f):F2} MB\n"
                + $"  释放内存 Freed: {freedMB:F2} MB"
        );
    }

    #endregion

    #region 监控控制 Monitoring Control

    private void StartMonitoring()
    {
        if (monitoringCoroutine != null)
        {
            StopCoroutine(monitoringCoroutine);
        }

        monitoringCoroutine = StartCoroutine(PerformanceMonitoringCoroutine());
        autoMonitoring = true;
        UnityEngine.Debug.Log(
            $"🔍 性能监控已开始 Performance monitoring started (间隔 interval: {monitoringInterval}s)"
        );
    }

    private void StopMonitoring()
    {
        if (monitoringCoroutine != null)
        {
            StopCoroutine(monitoringCoroutine);
            monitoringCoroutine = null;
        }

        autoMonitoring = false;
        UnityEngine.Debug.Log("⏹️ 性能监控已停止 Performance monitoring stopped");
    }

    private void SetMonitoringInterval(float interval)
    {
        if (interval < 0.1f || interval > 60f)
        {
            UnityEngine.Debug.LogWarning(
                "⚠️ 监控间隔范围: 0.1 - 60.0 秒 Monitoring interval range: 0.1 - 60.0 seconds"
            );
            return;
        }

        monitoringInterval = interval;
        UnityEngine.Debug.Log($"⏱️ 监控间隔设置为 Monitoring interval set to: {interval}s");

        if (autoMonitoring)
        {
            StartMonitoring(); // 重启监控以应用新间隔
        }
    }

    private void ClearPerformanceHistory()
    {
        performanceHistory.Clear();
        UnityEngine.Debug.Log($"🗑️ 性能历史已清空 Performance history cleared");
    }

    private IEnumerator PerformanceMonitoringCoroutine()
    {
        while (autoMonitoring)
        {
            var snapshot = new PerformanceSnapshot();
            performanceHistory.Add(snapshot);

            // 限制历史记录数量
            if (performanceHistory.Count > maxProfilerSamples)
            {
                performanceHistory.RemoveAt(0);
            }

            yield return new WaitForSeconds(monitoringInterval);
        }
    }

    #endregion

    #region 性能分析 Performance Analysis

    private void AnalyzePerformance()
    {
        if (performanceHistory.Count == 0)
        {
            UnityEngine.Debug.LogWarning(
                "⚠️ 无性能数据，请先开始监控 No performance data, please start monitoring first"
            );
            return;
        }

        var stats = CalculatePerformanceStats();

        UnityEngine.Debug.Log(
            $"📈 性能分析报告 Performance Analysis Report:\n"
                + $"  样本数量 Samples: {performanceHistory.Count}\n"
                + $"  时间跨度 Time Span: {stats.timeSpan:F1}s\n"
                + $"  平均FPS Avg FPS: {stats.avgFPS:F1}\n"
                + $"  最低FPS Min FPS: {stats.minFPS:F1}\n"
                + $"  最高FPS Max FPS: {stats.maxFPS:F1}\n"
                + $"  平均帧时间 Avg Frame Time: {stats.avgFrameTime:F2}ms\n"
                + $"  内存使用 Avg Memory: {stats.avgMemory:F2}MB\n"
                + $"  性能等级 Performance Grade: {GetPerformanceGrade(stats.avgFPS)}"
        );
    }

    private void ShowPerformanceHistory(int count = 10)
    {
        if (performanceHistory.Count == 0)
        {
            UnityEngine.Debug.LogWarning("⚠️ 无性能历史数据 No performance history data");
            return;
        }

        count = Mathf.Min(count, performanceHistory.Count);
        UnityEngine.Debug.Log($"📊 最近{count}条性能记录 Recent {count} Performance Records:");

        for (int i = performanceHistory.Count - count; i < performanceHistory.Count; i++)
        {
            var snapshot = performanceHistory[i];
            UnityEngine.Debug.Log(
                $"  [{i}] FPS:{snapshot.fps:F1} 帧时间:{snapshot.frameTime:F2}ms 内存:{snapshot.memoryUsed / (1024f * 1024f):F1}MB"
            );
        }
    }

    private void ShowPerformanceSummary()
    {
        if (performanceHistory.Count == 0)
        {
            UnityEngine.Debug.LogWarning("⚠️ 无性能数据 No performance data");
            return;
        }

        var stats = CalculatePerformanceStats();
        string performanceGrade = GetPerformanceGrade(stats.avgFPS);
        string memoryStatus = stats.avgMemory > gcWarningThreshold ? "⚠️ 高" : "✅ 正常";

        UnityEngine.Debug.Log(
            $"📋 性能总结 Performance Summary:\n"
                + $"  🎯 总体评级 Overall Grade: {performanceGrade}\n"
                + $"  📊 平均性能 Average Performance: {stats.avgFPS:F1} FPS\n"
                + $"  📈 性能稳定性 Stability: {(stats.maxFPS - stats.minFPS):F1} FPS 波动\n"
                + $"  💾 内存状态 Memory Status: {memoryStatus} ({stats.avgMemory:F1}MB)\n"
                + $"  ⏱️ 监控时长 Monitoring Duration: {stats.timeSpan:F1}s"
        );
    }

    private PerformanceStats CalculatePerformanceStats()
    {
        var stats = new PerformanceStats();

        if (performanceHistory.Count == 0)
            return stats;

        float totalFPS = 0f;
        float totalFrameTime = 0f;
        long totalMemory = 0L;

        stats.minFPS = float.MaxValue;
        stats.maxFPS = 0f;

        foreach (var snapshot in performanceHistory)
        {
            totalFPS += snapshot.fps;
            totalFrameTime += snapshot.frameTime;
            totalMemory += snapshot.memoryUsed;

            if (snapshot.fps < stats.minFPS)
                stats.minFPS = snapshot.fps;
            if (snapshot.fps > stats.maxFPS)
                stats.maxFPS = snapshot.fps;
        }

        int count = performanceHistory.Count;
        stats.avgFPS = totalFPS / count;
        stats.avgFrameTime = totalFrameTime / count;
        stats.avgMemory = (totalMemory / count) / (1024f * 1024f);
        stats.timeSpan = performanceHistory[count - 1].timestamp - performanceHistory[0].timestamp;

        return stats;
    }

    private string GetPerformanceGrade(float avgFPS)
    {
        if (avgFPS >= 55f)
            return "🏆 优秀 Excellent";
        if (avgFPS >= 45f)
            return "🥇 良好 Good";
        if (avgFPS >= 30f)
            return "🥈 一般 Fair";
        if (avgFPS >= 20f)
            return "🥉 较差 Poor";
        return "❌ 很差 Very Poor";
    }

    private struct PerformanceStats
    {
        public float avgFPS;
        public float minFPS;
        public float maxFPS;
        public float avgFrameTime;
        public float avgMemory;
        public float timeSpan;
    }

    #endregion

    #region 自定义性能分析器 Custom Profilers

    private void StartCustomProfiler(string profilerName)
    {
        if (string.IsNullOrEmpty(profilerName))
        {
            UnityEngine.Debug.LogWarning("⚠️ 分析器名称不能为空 Profiler name cannot be empty");
            return;
        }

        if (!customProfilers.ContainsKey(profilerName))
        {
            customProfilers[profilerName] = new CustomProfiler(profilerName);
        }

        customProfilers[profilerName].Start();
        UnityEngine.Debug.Log($"▶️ 分析器已启动 Profiler started: {profilerName}");
    }

    private void StopCustomProfiler(string profilerName)
    {
        if (customProfilers.ContainsKey(profilerName))
        {
            customProfilers[profilerName].Stop();
            UnityEngine.Debug.Log($"⏹️ 分析器已停止 Profiler stopped: {profilerName}");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"⚠️ 未找到分析器 Profiler not found: {profilerName}");
        }
    }

    private void ListCustomProfilers()
    {
        if (customProfilers.Count == 0)
        {
            UnityEngine.Debug.Log("📝 无自定义分析器 No custom profilers");
            return;
        }

        UnityEngine.Debug.Log($"📝 自定义分析器列表 Custom Profilers ({customProfilers.Count}):");
        foreach (var profiler in customProfilers.Values)
        {
            string status = profiler.isRunning ? "🟢 运行中" : "🔴 已停止";
            UnityEngine.Debug.Log($"  {profiler.name}: {status} ({profiler.samples.Count} 样本)");
        }
    }

    private void ShowProfilerStats(string profilerName)
    {
        if (!customProfilers.ContainsKey(profilerName))
        {
            UnityEngine.Debug.LogWarning($"⚠️ 未找到分析器 Profiler not found: {profilerName}");
            return;
        }

        var profiler = customProfilers[profilerName];
        if (profiler.samples.Count == 0)
        {
            UnityEngine.Debug.LogWarning($"⚠️ 分析器无数据 No data for profiler: {profilerName}");
            return;
        }

        UnityEngine.Debug.Log(
            $"📊 分析器统计 Profiler Stats: {profilerName}\n"
                + $"  样本数量 Samples: {profiler.samples.Count}\n"
                + $"  平均时间 Average: {profiler.GetAverageTime():F2}ms\n"
                + $"  最短时间 Min: {profiler.GetMinTime():F2}ms\n"
                + $"  最长时间 Max: {profiler.GetMaxTime():F2}ms\n"
                + $"  运行状态 Status: {(profiler.isRunning ? "运行中" : "已停止")}"
        );
    }

    #endregion

    #region 系统信息 System Information

    private void ShowSystemInfo()
    {
        UnityEngine.Debug.Log(
            $"💻 系统信息 System Information:\n"
                + $"  操作系统 OS: {SystemInfo.operatingSystem}\n"
                + $"  处理器 Processor: {SystemInfo.processorType}\n"
                + $"  核心数 Cores: {SystemInfo.processorCount}\n"
                + $"  内存 RAM: {SystemInfo.systemMemorySize}MB\n"
                + $"  显卡 GPU: {SystemInfo.graphicsDeviceName}\n"
                + $"  显存 VRAM: {SystemInfo.graphicsMemorySize}MB\n"
                + $"  Unity版本 Unity: {Application.unityVersion}\n"
                + $"  平台 Platform: {Application.platform}"
        );
    }

    private void ShowGPUInfo()
    {
        UnityEngine.Debug.Log(
            $"🎮 GPU信息 GPU Information:\n"
                + $"  设备名称 Device: {SystemInfo.graphicsDeviceName}\n"
                + $"  制造商 Vendor: {SystemInfo.graphicsDeviceVendor}\n"
                + $"  类型 Type: {SystemInfo.graphicsDeviceType}\n"
                + $"  版本 Version: {SystemInfo.graphicsDeviceVersion}\n"
                + $"  显存 VRAM: {SystemInfo.graphicsMemorySize}MB\n"
                + $"  多线程渲染 Multi-threading: {SystemInfo.graphicsMultiThreaded}\n"
                + $"  着色器级别 Shader Level: {SystemInfo.graphicsShaderLevel}\n"
                + $"  最大纹理尺寸 Max Texture Size: {SystemInfo.maxTextureSize}"
        );
    }

    private void ShowQualitySettings()
    {
        UnityEngine.Debug.Log(
            $"⚙️ 质量设置 Quality Settings:\n"
                + $"  质量级别 Level: {QualitySettings.GetQualityLevel()}\n"
                + $"  质量名称 Name: {QualitySettings.names[QualitySettings.GetQualityLevel()]}\n"
                + $"  VSync: {QualitySettings.vSyncCount}\n"
                + $"  抗锯齿 Anti Aliasing: {QualitySettings.antiAliasing}\n"
                + $"  阴影 Shadows: {QualitySettings.shadows}\n"
                + $"  阴影质量 Shadow Quality: {QualitySettings.shadowResolution}\n"
                + $"  纹理质量 Texture Quality: {QualitySettings.globalTextureMipmapLimit}\n"
                + $"  像素光源数 Pixel Light Count: {QualitySettings.pixelLightCount}"
        );
    }

    #endregion

    #region 压力测试 Stress Testing

    private List<GameObject> testObjects = new List<GameObject>();

    private void ExecuteStressTest(int intensity = 1)
    {
        intensity = Mathf.Clamp(intensity, 1, 5);
        UnityEngine.Debug.Log(
            $"🔥 开始压力测试 Starting stress test (强度 intensity: {intensity})"
        );

        StartCoroutine(StressTestCoroutine(intensity));
    }

    private IEnumerator StressTestCoroutine(int intensity)
    {
        float testDuration = 10f; // 测试持续时间
        float startTime = Time.realtimeSinceStartup;

        // 记录测试前性能
        var beforeSnapshot = new PerformanceSnapshot();
        UnityEngine.Debug.Log(
            $"📊 测试前性能 Before test: FPS={beforeSnapshot.fps:F1}, Memory={beforeSnapshot.memoryUsed / (1024f * 1024f):F1}MB"
        );

        // 生成测试对象
        int objectCount = intensity * 50;
        for (int i = 0; i < objectCount; i++)
        {
            CreateStressTestObject();

            if (i % 10 == 0)
            {
                yield return null; // 分帧生成
            }
        }

        // 等待测试完成
        yield return new WaitForSeconds(testDuration);

        // 记录测试后性能
        var afterSnapshot = new PerformanceSnapshot();
        UnityEngine.Debug.Log(
            $"📊 测试后性能 After test: FPS={afterSnapshot.fps:F1}, Memory={afterSnapshot.memoryUsed / (1024f * 1024f):F1}MB"
        );

        // 分析性能差异
        float fpsDiff = beforeSnapshot.fps - afterSnapshot.fps;
        float memoryDiff = (afterSnapshot.memoryUsed - beforeSnapshot.memoryUsed) / (1024f * 1024f);

        UnityEngine.Debug.Log(
            $"🎯 压力测试结果 Stress Test Results:\n"
                + $"  测试强度 Intensity: {intensity}\n"
                + $"  测试对象 Objects: {objectCount}\n"
                + $"  FPS下降 FPS Drop: {fpsDiff:F1}\n"
                + $"  内存增加 Memory Increase: {memoryDiff:F1}MB\n"
                + $"  性能等级 Performance: {GetStressTestGrade(fpsDiff)}"
        );

        // 清理测试对象
        ClearTestObjects();
    }

    private void SpawnTestObjects(int count, int complexity = 1)
    {
        UnityEngine.Debug.Log(
            $"✨ 生成测试对象 Spawning test objects: {count} (复杂度 complexity: {complexity})"
        );

        for (int i = 0; i < count; i++)
        {
            CreateStressTestObject(complexity);
        }

        UnityEngine.Debug.Log($"✅ 已生成{count}个测试对象 Spawned {count} test objects");
    }

    private void CreateStressTestObject(int complexity = 1)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"StressTestObject_{testObjects.Count}";

        // 随机位置
        obj.transform.position = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(0f, 5f),
            Random.Range(-10f, 10f)
        );

        // 添加组件增加复杂度
        if (complexity >= 2)
        {
            obj.AddComponent<Rigidbody>();
        }
        if (complexity >= 3)
        {
            obj.AddComponent<AudioSource>();
        }
        if (complexity >= 4)
        {
            var animator = obj.AddComponent<Animator>();
            // 可以在这里添加动画控制器
        }

        // 添加旋转脚本
        obj.AddComponent<TestObjectRotator>();

        testObjects.Add(obj);
    }

    private void ClearTestObjects()
    {
        int count = testObjects.Count;
        foreach (var obj in testObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        testObjects.Clear();

        UnityEngine.Debug.Log($"🗑️ 已清理{count}个测试对象 Cleared {count} test objects");
    }

    private string GetStressTestGrade(float fpsDrop)
    {
        if (fpsDrop <= 5f)
            return "🏆 优秀 Excellent";
        if (fpsDrop <= 15f)
            return "🥇 良好 Good";
        if (fpsDrop <= 30f)
            return "🥈 一般 Fair";
        if (fpsDrop <= 50f)
            return "🥉 较差 Poor";
        return "❌ 很差 Very Poor";
    }

    #endregion

    // ===== ConsoleMethod属性示例 ConsoleMethod Attribute Examples =====

    [ConsoleMethod("benchmark", "执行性能基准测试 Execute performance benchmark")]
    public static void ExecuteBenchmark()
    {
        var monitor = FindObjectOfType<PerformanceMonitorSamples>();
        if (monitor != null)
        {
            monitor.StartCoroutine(monitor.BenchmarkCoroutine());
        }
        else
        {
            UnityEngine.Debug.LogWarning("⚠️ 未找到PerformanceMonitorSamples");
        }
    }

    private IEnumerator BenchmarkCoroutine()
    {
        UnityEngine.Debug.Log("🏁 开始性能基准测试 Starting performance benchmark...");

        // CPU测试
        yield return StartCoroutine(CPUBenchmark());

        // 内存测试
        yield return StartCoroutine(MemoryBenchmark());

        // 渲染测试
        yield return StartCoroutine(RenderingBenchmark());

        UnityEngine.Debug.Log("🏆 性能基准测试完成 Performance benchmark completed!");
    }

    private IEnumerator CPUBenchmark()
    {
        UnityEngine.Debug.Log("⚡ CPU基准测试 CPU Benchmark...");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 简单的CPU密集计算
        float result = 0f;
        for (int i = 0; i < 1000000; i++)
        {
            result += Mathf.Sin(i) * Mathf.Cos(i);
            if (i % 100000 == 0)
                yield return null;
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log(
            $"💻 CPU测试完成 CPU test completed: {stopwatch.ElapsedMilliseconds}ms"
        );
    }

    private IEnumerator MemoryBenchmark()
    {
        UnityEngine.Debug.Log("💾 内存基准测试 Memory Benchmark...");

        long beforeMemory = System.GC.GetTotalMemory(false);
        var tempObjects = new List<object>();

        // 分配大量临时对象
        for (int i = 0; i < 100000; i++)
        {
            tempObjects.Add(new Vector3(i, i, i));
            if (i % 10000 == 0)
                yield return null;
        }

        long afterMemory = System.GC.GetTotalMemory(false);
        tempObjects.Clear();
        System.GC.Collect();

        float memoryUsed = (afterMemory - beforeMemory) / (1024f * 1024f);
        UnityEngine.Debug.Log(
            $"💾 内存测试完成 Memory test completed: {memoryUsed:F2}MB allocated"
        );
    }

    private IEnumerator RenderingBenchmark()
    {
        UnityEngine.Debug.Log("🎨 渲染基准测试 Rendering Benchmark...");

        var testObjs = new List<GameObject>();

        // 创建大量渲染对象
        for (int i = 0; i < 100; i++)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.position = Random.insideUnitSphere * 10f;
            testObjs.Add(obj);

            if (i % 10 == 0)
                yield return null;
        }

        // 等待几帧测量渲染性能
        yield return new WaitForSeconds(2f);

        // 清理
        foreach (var obj in testObjs)
        {
            DestroyImmediate(obj);
        }

        UnityEngine.Debug.Log("🎨 渲染测试完成 Rendering test completed");
    }
}

// ===== 辅助类 Helper Classes =====

/// <summary>
/// 测试对象旋转器
/// Test Object Rotator
/// </summary>
public class TestObjectRotator : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(Vector3.up * Time.deltaTime * 90f);
    }
}
