using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "InputSettings", menuName = "Core/Input/Input Settings")]
    public class InputSettings : SerializedScriptableObject
    {
        public List<InputBinding> GetBindings(InputAction action)
        {
            return actionBindings.Where(b => b.action == action).ToList();
        }

        public void AddBinding(InputBinding binding)
        {
            if (!actionBindings.Contains(binding))
            {
                actionBindings.Add(binding);
            }
        }

        public void RemoveBinding(InputBinding binding)
        {
            actionBindings.Remove(binding);
        }

        public void ClearBindings(InputAction action)
        {
            actionBindings.RemoveAll(b => b.action == action);
        }

        public AxisBinding GetAxisBinding(InputAxis axis)
        {
            return axisBindings.FirstOrDefault(b => b.axis == axis);
        }

        public void Save()
        {
            try
            {
                string path = GetSettingsFilePath();
                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(path, json);
                Logger.LogInfo($"Input settings saved to: {path}", LogTag.Input);
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to save input settings: {e.Message}", LogTag.Input);
            }
        }

        public static InputSettings Load()
        {
            // Trying to load from file, fallback to Singleton if fails

            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonUtility.FromJson<InputSettings>(json);
                    Logger.LogInfo($"Input settings loaded from: {path}", LogTag.Input);
                    return settings;
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to load input settings: {e.Message}", LogTag.Input);
            }

            Logger.LogWarn(
                "Failed to load input settings from file, fallback to Singleton",
                LogTag.Input
            );

            if (VgInputManager.Instance.inputSettings == null)
            {
                throw new CoreModuleException(
                    "VgInputManager Settings is null, cannot fallback to Singleton"
                );
            }

            return VgInputManager.Instance.inputSettings!;
        }

        private static string GetSettingsFilePath()
        {
            return Path.Combine(Application.persistentDataPath, SETTINGS_FILE_NAME);
        }

        [BoxGroup("Input System")]
        [Tooltip("The Input Actions asset used by the new Input System")]
        public UnityEngine.InputSystem.InputActionAsset inputActionAsset;

        [BoxGroup("Input System")]
        [Tooltip("Enable/disable the new Input System")]
        public bool useNewInputSystem = true;

        [BoxGroup("Bindings")]
        [ShowInInspector]
        public List<InputBinding> actionBindings = new List<InputBinding>();

        [BoxGroup("Bindings")]
        [ShowInInspector]
        public List<AxisBinding> axisBindings = new List<AxisBinding>();

        [BoxGroup("Sensitivity Settings")]
        [Range(0.1f, 10f)]
        public float mouseSensitivity = 1f;

        [BoxGroup("Sensitivity Settings")]
        [Range(0.1f, 10f)]
        public float gamepadSensitivity = 1f;

        [BoxGroup("Inversion Settings")]
        public bool invertMouseY = false;

        [BoxGroup("Inversion Settings")]
        public bool invertGamepadY = false;

        private const string SETTINGS_FILE_NAME = "InputSettings.json";
    }
}
