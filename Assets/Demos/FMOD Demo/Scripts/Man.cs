using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class Man : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float lookSensitivity = 1.5f;
    [SerializeField] private bool lockAndHideCursor = true;

    [Header("FMOD Event")]
    [SerializeField] private EventReference ambienceEvent;

    [Header("Audio Parameters")]
    [SerializeField] private float wallaMaxDistanceDefault = 20f;
    [SerializeField] private float trafficRadius = 15f;
    [SerializeField] private int trafficMaxCount = 10;

    private Camera playerCamera;
    private CharacterController characterController;
    private float yaw;
    private float pitch;

    private EventInstance ambienceInstance;
    private bool fmodReady;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        if (lockAndHideCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Start()
    {
        if (ambienceEvent.IsNull)
        {
            Debug.LogWarning("FMOD EventReference for Man is not set.");
            return;
        }

        ambienceInstance = RuntimeManager.CreateInstance(ambienceEvent);
        ambienceInstance.start();
        fmodReady = true;
    }

    private void OnDestroy()
    {
        if (fmodReady)
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambienceInstance.release();
        }
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        UpdateAudio();
    }

    private void HandleLook()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        yaw += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, -85f, 85f);

        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;
        Vector3 velocity = moveDirection * moveSpeed;

        if (characterController != null)
        {
            characterController.SimpleMove(velocity);
        }
        else
        {
            transform.position += velocity * Time.deltaTime;
        }
    }

    private void UpdateAudio()
    {
        if (!fmodReady) return;

        var attributes = RuntimeUtils.To3DAttributes(gameObject);
        ambienceInstance.set3DAttributes(attributes);

        // Walla increases as the player approaches any bar. If bars have per-instance max distance, use it; otherwise default.
        float walla = ComputeWallaAtPosition(transform.position);
        ambienceInstance.setParameterByName("Walla", walla);

        // Traffic increases with number of cars near the player.
        int carCountNearby = Car.CountNearby(transform.position, trafficRadius);
        float traffic = Mathf.Clamp01((float)carCountNearby / Mathf.Max(1, trafficMaxCount));
        ambienceInstance.setParameterByName("Traffic", traffic);
    }

    private float ComputeWallaAtPosition(Vector3 position)
    {
        // If bars register themselves, compute against them; otherwise fallback to default radius with zero value.
        if (Bar.Instances.Count == 0)
        {
            return 0f;
        }

        float maxContribution = 0f;
        for (int i = 0; i < Bar.Instances.Count; i++)
        {
            Bar bar = Bar.Instances[i];
            if (bar == null) continue;

            float radius = bar.wallaMaxDistance <= 0f ? wallaMaxDistanceDefault : bar.wallaMaxDistance;
            float distance = Vector3.Distance(position, bar.transform.position);
            float contribution = 1f - Mathf.Clamp01(distance / Mathf.Max(0.0001f, radius));
            if (contribution > maxContribution)
            {
                maxContribution = contribution;
            }
        }

        return maxContribution;
    }
}
