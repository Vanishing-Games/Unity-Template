using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarEngineAudio : MonoBehaviour
{
    [Header("FMOD事件设置")]
    [SerializeField]
    private EventReference engineEvent; // 引擎音频事件

    [Header("引擎参数")]
    [SerializeField]
    private float maxRPM = 8000f; // 最大转速

    [SerializeField]
    private float idleRPM = 800f; // 怠速转速

    [SerializeField]
    private float accelerationRate = 2000f; // 加速速率

    [SerializeField]
    private float decelerationRate = 1000f; // 自然减速速率

    [SerializeField]
    private float brakeDecelerationRate = 3000f; // 刹车减速速率

    [Header("换挡设置")]
    [SerializeField]
    private float[] gearShiftRPMs = { 2000f, 4000f, 6000f, 7500f }; // 各档位换挡转速

    [SerializeField]
    private int currentGear = 1; // 当前档位

    [SerializeField]
    private float gearShiftDelay = 0.2f; // 换挡延迟

    [Header("立体声效果")]
    [SerializeField]
    private float stereoWidth = 0.8f; // 立体声宽度

    [SerializeField]
    private float dopplerEffect = 0.5f; // 多普勒效应强度

    [SerializeField]
    private float reverbSend = 0.3f; // 混响发送量

    [Header("调试信息")]
    [SerializeField]
    private bool showDebugInfo = true;

    // 私有变量
    private FMOD.Studio.EventInstance engineInstance;
    private float currentRPM;
    private float targetRPM;
    private bool isShifting = false;
    private float shiftTimer = 0f;
    private Rigidbody cachedRigidbody;

    // 音频参数ID
    private FMOD.Studio.PARAMETER_ID rpmParameterId;
    private FMOD.Studio.PARAMETER_ID gearParameterId;
    private FMOD.Studio.PARAMETER_ID stereoWidthParameterId;
    private FMOD.Studio.PARAMETER_ID dopplerParameterId;
    private FMOD.Studio.PARAMETER_ID reverbParameterId;

    void Start()
    {
        InitializeAudio();
        currentRPM = idleRPM;
        targetRPM = idleRPM;
    }

    void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    void InitializeAudio()
    {
        // 创建引擎音频实例
        if (!engineEvent.IsNull)
        {
            engineInstance = RuntimeManager.CreateInstance(engineEvent);
            RuntimeManager.AttachInstanceToGameObject(engineInstance, transform, cachedRigidbody);

            // 获取参数ID
            FMOD.Studio.EventDescription eventDesc;
            engineInstance.getDescription(out eventDesc);

            FMOD.Studio.PARAMETER_DESCRIPTION paramDesc;
            eventDesc.getParameterDescriptionByName("RPM", out paramDesc);
            rpmParameterId = paramDesc.id;

            eventDesc.getParameterDescriptionByName("Gear", out paramDesc);
            gearParameterId = paramDesc.id;

            eventDesc.getParameterDescriptionByName("StereoWidth", out paramDesc);
            stereoWidthParameterId = paramDesc.id;

            eventDesc.getParameterDescriptionByName("Doppler", out paramDesc);
            dopplerParameterId = paramDesc.id;

            eventDesc.getParameterDescriptionByName("Reverb", out paramDesc);
            reverbParameterId = paramDesc.id;

            // 设置初始参数
            engineInstance.setParameterByID(rpmParameterId, currentRPM);
            engineInstance.setParameterByID(gearParameterId, currentGear);
            engineInstance.setParameterByID(stereoWidthParameterId, stereoWidth);
            engineInstance.setParameterByID(dopplerParameterId, dopplerEffect);
            engineInstance.setParameterByID(reverbParameterId, reverbSend);

            // 开始播放
            engineInstance.start();
        }
    }

    void Update()
    {
        HandleInput();
        UpdateRPM();
        UpdateAudioParameters();
        HandleGearShifting();

        // Ensure 3D attributes are updated each frame to reflect position/velocity
        if (engineInstance.isValid())
        {
            engineInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        }

        if (showDebugInfo)
        {
            DisplayDebugInfo();
        }
    }

    void HandleInput()
    {
        // W键加速
        if (Input.GetKey(KeyCode.W))
        {
            targetRPM = Mathf.Min(targetRPM + accelerationRate * Time.deltaTime, maxRPM);
        }
        // S键刹车
        else if (Input.GetKey(KeyCode.S))
        {
            targetRPM = Mathf.Max(targetRPM - brakeDecelerationRate * Time.deltaTime, idleRPM);
        }
        // 自然减速
        else
        {
            targetRPM = Mathf.Max(targetRPM - decelerationRate * Time.deltaTime, idleRPM);
        }
    }

    void UpdateRPM()
    {
        // 平滑过渡到目标转速
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * 5f);

        // 如果正在换挡，降低转速
        if (isShifting)
        {
            currentRPM = Mathf.Lerp(currentRPM, idleRPM, Time.deltaTime * 3f);
        }
    }

    void UpdateAudioParameters()
    {
        if (engineInstance.isValid())
        {
            // 更新RPM参数
            engineInstance.setParameterByID(rpmParameterId, currentRPM);

            // 更新档位参数
            engineInstance.setParameterByID(gearParameterId, currentGear);

            // 根据转速动态调整立体声效果
            float dynamicStereoWidth =
                stereoWidth * (1f + (currentRPM - idleRPM) / (maxRPM - idleRPM) * 0.5f);
            engineInstance.setParameterByID(stereoWidthParameterId, dynamicStereoWidth);

            // 根据转速调整多普勒效应
            float dynamicDoppler = dopplerEffect * (currentRPM / maxRPM);
            engineInstance.setParameterByID(dopplerParameterId, dynamicDoppler);

            // 根据转速调整混响
            float dynamicReverb =
                reverbSend * (1f - (currentRPM - idleRPM) / (maxRPM - idleRPM) * 0.3f);
            engineInstance.setParameterByID(reverbParameterId, dynamicReverb);
        }
    }

    void HandleGearShifting()
    {
        if (isShifting)
        {
            shiftTimer -= Time.deltaTime;
            if (shiftTimer <= 0f)
            {
                isShifting = false;
            }
            return;
        }

        // 检查是否需要升档
        if (currentGear < gearShiftRPMs.Length + 1 && currentRPM >= gearShiftRPMs[currentGear - 1])
        {
            ShiftGear(true);
        }
        // 检查是否需要降档
        else if (currentGear > 1 && currentRPM <= gearShiftRPMs[currentGear - 2] * 0.5f)
        {
            ShiftGear(false);
        }
    }

    void ShiftGear(bool upshift)
    {
        if (isShifting)
            return;

        isShifting = true;
        shiftTimer = gearShiftDelay;

        if (upshift)
        {
            currentGear++;
        }
        else
        {
            currentGear--;
        }

        // 换挡时短暂降低转速
        targetRPM = Mathf.Max(targetRPM * 0.7f, idleRPM);
    }

    void DisplayDebugInfo()
    {
        if (showDebugInfo)
        {
            // 在编辑器中显示调试信息
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string debugText = $"RPM: {currentRPM:F0}\n";
                debugText += $"Gear: {currentGear}\n";
                debugText += $"Target RPM: {targetRPM:F0}\n";
                debugText += $"Shifting: {isShifting}\n";
                debugText += $"Stereo Width: {stereoWidth:F2}\n";
                debugText += $"Doppler: {dopplerEffect:F2}\n";
                debugText += $"Reverb: {reverbSend:F2}";

                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, debugText);
            }
#endif
        }
    }

    // 在运行时显示调试信息
    void OnGUI()
    {
        if (showDebugInfo && Application.isPlaying)
        {
            // 获取屏幕坐标
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

            // 如果物体在屏幕内
            if (screenPos.z > 0)
            {
                // 创建调试信息窗口
                string debugText = $"RPM: {currentRPM:F0}\n";
                debugText += $"Gear: {currentGear}\n";
                debugText += $"Target RPM: {targetRPM:F0}\n";
                debugText += $"Shifting: {isShifting}\n";
                debugText += $"Stereo Width: {stereoWidth:F2}\n";
                debugText += $"Doppler: {dopplerEffect:F2}\n";
                debugText += $"Reverb: {reverbSend:F2}";

                // 计算窗口大小
                Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(debugText));
                float windowWidth = textSize.x + 20;
                float windowHeight = textSize.y + 20;

                // 绘制调试信息窗口
                GUI.Box(
                    new Rect(
                        screenPos.x - windowWidth / 2,
                        Screen.height - screenPos.y - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ),
                    ""
                );
                GUI.Label(
                    new Rect(
                        screenPos.x - windowWidth / 2 + 10,
                        Screen.height - screenPos.y - windowHeight / 2 + 10,
                        windowWidth - 20,
                        windowHeight - 20
                    ),
                    debugText
                );
            }
        }
    }

    // 公共方法：设置立体声宽度
    public void SetStereoWidth(float width)
    {
        stereoWidth = Mathf.Clamp01(width);
        if (engineInstance.isValid())
        {
            engineInstance.setParameterByID(stereoWidthParameterId, stereoWidth);
        }
    }

    // 公共方法：设置多普勒效应强度
    public void SetDopplerEffect(float doppler)
    {
        dopplerEffect = Mathf.Clamp01(doppler);
        if (engineInstance.isValid())
        {
            engineInstance.setParameterByID(dopplerParameterId, dopplerEffect);
        }
    }

    // 公共方法：设置混响发送量
    public void SetReverbSend(float reverb)
    {
        reverbSend = Mathf.Clamp01(reverb);
        if (engineInstance.isValid())
        {
            engineInstance.setParameterByID(reverbParameterId, reverbSend);
        }
    }

    // 公共方法：手动换挡
    public void ManualShiftGear(bool upshift)
    {
        ShiftGear(upshift);
    }

    // 公共方法：设置目标转速
    public void SetTargetRPM(float rpm)
    {
        targetRPM = Mathf.Clamp(rpm, idleRPM, maxRPM);
    }

    void OnDestroy()
    {
        // 清理音频实例
        if (engineInstance.isValid())
        {
            engineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            engineInstance.release();
        }
    }

    void OnValidate()
    {
        // 在编辑器中验证参数
        maxRPM = Mathf.Max(maxRPM, idleRPM + 1000f);
        idleRPM = Mathf.Max(idleRPM, 100f);
        accelerationRate = Mathf.Max(accelerationRate, 100f);
        decelerationRate = Mathf.Max(decelerationRate, 100f);
        brakeDecelerationRate = Mathf.Max(brakeDecelerationRate, 100f);
        stereoWidth = Mathf.Clamp01(stereoWidth);
        dopplerEffect = Mathf.Clamp01(dopplerEffect);
        reverbSend = Mathf.Clamp01(reverbSend);
        currentGear = Mathf.Clamp(currentGear, 1, gearShiftRPMs.Length + 1);
    }
}
