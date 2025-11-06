using Sirenix.OdinInspector;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "InputSettings", menuName = "Core/Input/Input Settings")]
    public class InputSettings : SerializedScriptableObject
    {
        public static InputSettings Load()
        {
            if (VgInputManager.Instance.inputSettings == null)
            {
                throw new CoreModuleException(
                    "VgInputManager.Instance or InputSettings is null, cannot load settings"
                );
            }

            return VgInputManager.Instance.inputSettings;
        }

        [BoxGroup("Sensitivity Settings")]
        [Range(0.1f, 10f)]
        public float mouseSensitivity = 1f;

        [BoxGroup("Sensitivity Settings")]
        [Range(0.1f, 10f)]
        public float gamepadSensitivity = 1f;

        [BoxGroup("Inversion Settings")]
        public bool invertMouseY;

        [BoxGroup("Inversion Settings")]
        public bool invertGamepadY;
    }
}
