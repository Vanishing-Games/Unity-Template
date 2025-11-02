using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    [Serializable]
    public class InputBinding
    {
        public InputAction action;
        public KeyCode keyCode;
        public InputDeviceType deviceType;
        public int mouseButton = -1; // 0=left, 1=right, 2=middle
        public string gamepadButton = ""; // Gamepad button name

        // New Input System properties
        public string controlPath = ""; // e.g., "<Keyboard>/space", "<Mouse>/leftButton"

        public InputBinding(InputAction action, KeyCode keyCode)
        {
            this.action = action;
            this.keyCode = keyCode;
            this.deviceType = InputDeviceType.Keyboard;
            this.controlPath = KeyCodeToControlPath(keyCode);
        }

        public InputBinding(InputAction action, int mouseButton)
        {
            this.action = action;
            this.mouseButton = mouseButton;
            this.deviceType = InputDeviceType.Mouse;
            this.keyCode = KeyCode.None;
            this.controlPath = MouseButtonToControlPath(mouseButton);
        }

        public InputBinding(InputAction action, string gamepadButton)
        {
            this.action = action;
            this.gamepadButton = gamepadButton;
            this.deviceType = InputDeviceType.Gamepad;
            this.keyCode = KeyCode.None;
            this.controlPath = GamepadButtonToControlPath(gamepadButton);
        }

        public InputBinding(InputAction action, string controlPath, InputDeviceType deviceType)
        {
            this.action = action;
            this.controlPath = controlPath;
            this.deviceType = deviceType;
        }

        public bool IsPressed()
        {
            if (!string.IsNullOrEmpty(controlPath))
            {
                var control = InputSystem.FindControl(controlPath);
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button)
                {
                    return button.wasPressedThisFrame;
                }
            }
            return false;
        }

        public bool IsHeld()
        {
            if (!string.IsNullOrEmpty(controlPath))
            {
                var control = InputSystem.FindControl(controlPath);
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button)
                {
                    return button.isPressed;
                }
            }
            return false;
        }

        public bool IsReleased()
        {
            if (!string.IsNullOrEmpty(controlPath))
            {
                var control = InputSystem.FindControl(controlPath);
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button)
                {
                    return button.wasReleasedThisFrame;
                }
            }
            return false;
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(controlPath))
            {
                return controlPath.Replace("<", "").Replace(">", "").Replace("/", " ");
            }

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    return keyCode.ToString();
                case InputDeviceType.Mouse:
                    return $"Mouse{mouseButton}";
                case InputDeviceType.Gamepad:
                    return gamepadButton;
                default:
                    return "Unknown";
            }
        }

        private static string KeyCodeToControlPath(KeyCode keyCode)
        {
            return $"<Keyboard>/{keyCode.ToString().ToLower()}";
        }

        private static string MouseButtonToControlPath(int mouseButton)
        {
            return mouseButton switch
            {
                0 => "<Mouse>/leftButton",
                1 => "<Mouse>/rightButton",
                2 => "<Mouse>/middleButton",
                _ => "",
            };
        }

        private static string GamepadButtonToControlPath(string buttonName)
        {
            // Map common Unity legacy button names to Input System paths
            return buttonName.ToLower() switch
            {
                "jump" => "<Gamepad>/buttonSouth",
                "fire1" => "<Gamepad>/buttonSouth",
                "fire2" => "<Gamepad>/buttonEast",
                "fire3" => "<Gamepad>/buttonWest",
                _ => $"<Gamepad>/{buttonName.ToLower()}",
            };
        }
    }

    [Serializable]
    public class AxisBinding
    {
        public InputAxis axis;
        public string axisName; // Unity Input Manager中的轴名称 (legacy)
        public bool inverted = false;
        public float sensitivity = 1f;
        public float deadzone = 0.1f;

        // New Input System properties
        public string controlPath = ""; // e.g., "<Gamepad>/leftStick/x", "<Mouse>/delta/x"

        public AxisBinding(InputAxis axis, string axisName)
        {
            this.axis = axis;
            this.axisName = axisName;
            this.controlPath = AxisNameToControlPath(axis);
        }

        public AxisBinding(InputAxis axis, string controlPath, bool isNewInputSystem)
        {
            this.axis = axis;
            this.controlPath = controlPath;
            this.axisName = axis.ToString(); // Fallback for legacy
        }

        public float GetValue()
        {
            float value = 0f;

            if (!string.IsNullOrEmpty(controlPath))
            {
                var control = InputSystem.FindControl(controlPath);
                if (control is UnityEngine.InputSystem.Controls.AxisControl axisControl)
                {
                    value = axisControl.ReadValue();
                }
                else if (control is UnityEngine.InputSystem.Controls.Vector2Control vector2Control)
                {
                    // For Vector2 controls, we need to extract the right component
                    var vec = vector2Control.ReadValue();
                    value = controlPath.Contains("/x") ? vec.x : vec.y;
                }
            }

            if (Mathf.Abs(value) < deadzone)
                value = 0f;

            value *= sensitivity;
            if (inverted)
                value = -value;

            return Mathf.Clamp(value, -1f, 1f);
        }

        public float GetRawValue()
        {
            float value = 0f;

            if (!string.IsNullOrEmpty(controlPath))
            {
                var control = InputSystem.FindControl(controlPath);
                if (control is UnityEngine.InputSystem.Controls.AxisControl axisControl)
                {
                    value = axisControl.ReadValue();
                }
                else if (control is UnityEngine.InputSystem.Controls.Vector2Control vector2Control)
                {
                    var vec = vector2Control.ReadValue();
                    value = controlPath.Contains("/x") ? vec.x : vec.y;
                }
            }

            if (inverted)
                value = -value;
            return value;
        }

        private static string AxisNameToControlPath(InputAxis axis)
        {
            return axis switch
            {
                InputAxis.LeftStickHorizontal => "<Gamepad>/leftStick/x",
                InputAxis.LeftStickVertical => "<Gamepad>/leftStick/y",
                InputAxis.RightStickHorizontal => "<Gamepad>/rightStick/x",
                InputAxis.RightStickVertical => "<Gamepad>/rightStick/y",
                InputAxis.LeftTrigger => "<Gamepad>/leftTrigger",
                InputAxis.RightTrigger => "<Gamepad>/rightTrigger",
                InputAxis.MouseX => "<Mouse>/delta/x",
                InputAxis.MouseY => "<Mouse>/delta/y",
                InputAxis.MouseScrollWheel => "<Mouse>/scroll/y",
                _ => "",
            };
        }
    }
}
