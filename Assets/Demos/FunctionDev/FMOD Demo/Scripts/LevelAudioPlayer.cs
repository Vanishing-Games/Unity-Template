using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class LevelAudioPlayer : MonoBehaviour
{
    [Header("FMOD Events")]
    [SerializeField]
    private EventReference level01Event;

    [SerializeField]
    private EventReference level02Event;

    [SerializeField]
    private EventReference level03Event;

    [Header("Controls")]
    [SerializeField]
    private KeyCode switchLevelKey = KeyCode.Space;

    [SerializeField]
    private KeyCode progressKey = KeyCode.W;

    [SerializeField]
    [Tooltip("Progress increase speed per second while holding progress key")]
    private float progressPerSecond = 0.35f;

    [SerializeField]
    [Tooltip("How long the Stinger parameter stays at 1 before resetting to 0")]
    private float stingerPulseDuration = 0.08f;

    [SerializeField]
    private bool showDebugInfo = true;

    private FMOD.Studio.EventInstance currentInstance;
    private int currentLevelIndex = 0; // 0,1,2 correspond to Level01..Level03
    private float progress01 = 0f; // 0..1
    private float stingerTimer = 0f;

    void Start()
    {
        StartLevel(0);
    }

    void Update()
    {
        HandleInput();
        ApplyProgressToParameters();
        UpdateStingerPulse();

        if (currentInstance.isValid())
        {
            currentInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(switchLevelKey))
        {
            int next = (currentLevelIndex + 1) % 3;
            StartLevel(next);
        }

        if (Input.GetKey(progressKey))
        {
            progress01 = Mathf.Clamp01(progress01 + progressPerSecond * Time.deltaTime);
        }

        if (Input.GetMouseButtonDown(0))
        {
            TriggerStinger();
        }
    }

    private void StartLevel(int levelIndex)
    {
        StopAndReleaseCurrent();
        currentLevelIndex = levelIndex;
        progress01 = 0f; // 进入关卡开头
        stingerTimer = 0f;

        EventReference evRef = GetEventReferenceForLevel(currentLevelIndex);
        if (evRef.IsNull)
        {
            return;
        }

        currentInstance = RuntimeManager.CreateInstance(evRef);
        currentInstance.start();

        // 初始化为关卡开头的参数
        InitializeParametersForLevelStart();
    }

    private EventReference GetEventReferenceForLevel(int levelIndex)
    {
        switch (levelIndex)
        {
            case 0:
                return level01Event;
            case 1:
                return level02Event;
            case 2:
                return level03Event;
            default:
                return new EventReference();
        }
    }

    private void InitializeParametersForLevelStart()
    {
        if (!currentInstance.isValid())
        {
            return;
        }

        switch (currentLevelIndex)
        {
            // Level01: Progression(Intro, Main), Stinger(0.0-1.0)
            case 0:
                currentInstance.setParameterByNameWithLabel("Progression", "Intro", true);
                currentInstance.setParameterByName("Stinger", 0f, true);
                break;
            // Level02: Area(0.0,80.0)
            case 1:
                currentInstance.setParameterByName("Area", 0f, true);
                break;
            // Level03: Intensity(0-4), Stinger(0.0-1.0)
            case 2:
                currentInstance.setParameterByName("Intensity", 0f, true);
                currentInstance.setParameterByName("Stinger", 0f, true);
                break;
        }
    }

    private void ApplyProgressToParameters()
    {
        if (!currentInstance.isValid())
        {
            return;
        }

        switch (currentLevelIndex)
        {
            case 0:
                // 进度 < 阈值时保持 Intro，之后切到 Main
                string label = progress01 < 0.35f ? "Intro" : "Main";
                currentInstance.setParameterByNameWithLabel("Progression", label);
                break;
            case 1:
                // Area 从 0..80 映射
                float area = Mathf.Lerp(0f, 80f, progress01);
                currentInstance.setParameterByName("Area", area);
                break;
            case 2:
                // Intensity 从 0..4 映射（四舍五入以适配离散档位）
                int intensity = Mathf.RoundToInt(Mathf.Lerp(0f, 4f, progress01));
                currentInstance.setParameterByName("Intensity", intensity);
                break;
        }
    }

    private void TriggerStinger()
    {
        if (!currentInstance.isValid())
        {
            return;
        }

        // Only levels with Stinger parameter: Level01 and Level03
        if (currentLevelIndex == 0 || currentLevelIndex == 2)
        {
            currentInstance.setParameterByName("Stinger", 1f, true);
            stingerTimer = stingerPulseDuration;
        }
    }

    private void UpdateStingerPulse()
    {
        if (stingerTimer > 0f)
        {
            stingerTimer -= Time.deltaTime;
            if (stingerTimer <= 0f && currentInstance.isValid())
            {
                // Reset back to 0 to finish the pulse
                currentInstance.setParameterByName("Stinger", 0f, true);
            }
        }
    }

    private void StopAndReleaseCurrent()
    {
        if (currentInstance.isValid())
        {
            currentInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentInstance.release();
        }
    }

    void OnDestroy()
    {
        StopAndReleaseCurrent();
    }

    void OnGUI()
    {
        if (!showDebugInfo)
        {
            return;
        }

        GUI.Label(
            new Rect(10, 10, 480, 80),
            $"Level: {currentLevelIndex + 1}  Progress: {progress01:0.00}\n[Space] 切换关卡  [W] 推进进度  [LMB] 触发Stinger"
        );
    }
}
