using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarEngineAudio : MonoBehaviour
{
    [Header("FMOD Events")]
    [SerializeField]
    private EventReference engineEvent;

    [Header("Engine Parameters")]
    [SerializeField]
    private float maxRPM = 8000f;

    [SerializeField]
    private float idleRPM = 800f;

    [SerializeField]
    private float accelerationRate = 2000f;

    [SerializeField]
    private float decelerationRate = 1000f; // Coasting deceleration rate

    [SerializeField]
    private float brakeDecelerationRate = 3000f;

    [Header("Gear Shift Settings")]
    [SerializeField]
    private float[] gearShiftRPMs = { 2000f, 4000f, 6000f, 7500f };

    [SerializeField]
    private int currentGear = 1;

    [SerializeField]
    private float gearShiftDelay = 0.2f;

    [Header("Spatial Effects")]
    [SerializeField]
    private float stereoWidth = 0.8f;

    [SerializeField]
    private float dopplerEffect = 0.5f;

    [SerializeField]
    private float reverbSend = 0.3f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugInfo = true;

    private FMOD.Studio.EventInstance engineInstance;
    private float currentRPM;
    private float targetRPM;
    private bool isShifting = false;
    private float shiftTimer = 0f;
    private Rigidbody cachedRigidbody;

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
        if (!engineEvent.IsNull)
        {
            engineInstance = RuntimeManager.CreateInstance(engineEvent);
            RuntimeManager.AttachInstanceToGameObject(engineInstance, transform, cachedRigidbody);

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

            engineInstance.setParameterByID(rpmParameterId, currentRPM);
            engineInstance.setParameterByID(gearParameterId, currentGear);
            engineInstance.setParameterByID(stereoWidthParameterId, stereoWidth);
            engineInstance.setParameterByID(dopplerParameterId, dopplerEffect);
            engineInstance.setParameterByID(reverbParameterId, reverbSend);

            engineInstance.start();
        }
    }

    void Update()
    {
        HandleInput();
        UpdateRPM();
        UpdateAudioParameters();
        HandleGearShifting();

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
        if (Input.GetKey(KeyCode.W))
        {
            targetRPM = Mathf.Min(targetRPM + accelerationRate * Time.deltaTime, maxRPM);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            targetRPM = Mathf.Max(targetRPM - brakeDecelerationRate * Time.deltaTime, idleRPM);
        }
        else
        {
            targetRPM = Mathf.Max(targetRPM - decelerationRate * Time.deltaTime, idleRPM);
        }
    }

    void UpdateRPM()
    {
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * 5f);

        if (isShifting)
        {
            currentRPM = Mathf.Lerp(currentRPM, idleRPM, Time.deltaTime * 3f);
        }
    }

    void UpdateAudioParameters()
    {
        if (engineInstance.isValid())
        {
            engineInstance.setParameterByID(rpmParameterId, currentRPM);
            engineInstance.setParameterByID(gearParameterId, currentGear);
            float dynamicStereoWidth =
                stereoWidth * (1f + (currentRPM - idleRPM) / (maxRPM - idleRPM) * 0.5f);
            engineInstance.setParameterByID(stereoWidthParameterId, dynamicStereoWidth);
            float dynamicDoppler = dopplerEffect * (currentRPM / maxRPM);
            engineInstance.setParameterByID(dopplerParameterId, dynamicDoppler);
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

        if (currentGear < gearShiftRPMs.Length + 1 && currentRPM >= gearShiftRPMs[currentGear - 1])
        {
            ShiftGear(true);
        }
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

        targetRPM = Mathf.Max(targetRPM * 0.7f, idleRPM);
    }

    void DisplayDebugInfo()
    {
        if (showDebugInfo)
        {
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

    void OnGUI()
    {
        if (showDebugInfo && Application.isPlaying)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

            if (screenPos.z > 0)
            {
                string debugText = $"RPM: {currentRPM:F0}\n";
                debugText += $"Gear: {currentGear}\n";
                debugText += $"Target RPM: {targetRPM:F0}\n";
                debugText += $"Shifting: {isShifting}\n";
                debugText += $"Stereo Width: {stereoWidth:F2}\n";
                debugText += $"Doppler: {dopplerEffect:F2}\n";
                debugText += $"Reverb: {reverbSend:F2}";
                Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(debugText));
                float windowWidth = textSize.x + 20;
                float windowHeight = textSize.y + 20;
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

    public void SetStereoWidth(float width)
    {
        stereoWidth = Mathf.Clamp01(width);
        if (engineInstance.isValid())
        {
            engineInstance.setParameterByID(stereoWidthParameterId, stereoWidth);
        }
    }

    public void SetDopplerEffect(float doppler)
    {
        dopplerEffect = Mathf.Clamp01(doppler);
        if (engineInstance.isValid())
        {
            engineInstance.setParameterByID(dopplerParameterId, dopplerEffect);
        }
    }

    public void SetReverbSend(float reverb)
    {
        reverbSend = Mathf.Clamp01(reverb);
        if (engineInstance.isValid())
        {
            engineInstance.setParameterByID(reverbParameterId, reverbSend);
        }
    }

    public void ManualShiftGear(bool upshift)
    {
        ShiftGear(upshift);
    }

    public void SetTargetRPM(float rpm)
    {
        targetRPM = Mathf.Clamp(rpm, idleRPM, maxRPM);
    }

    void OnDestroy()
    {
        if (engineInstance.isValid())
        {
            engineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            engineInstance.release();
        }
    }

    void OnValidate()
    {
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
