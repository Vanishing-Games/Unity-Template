using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Core
{
    public static class VgInput
    {
        public static InputSettings Settings
        {
            get
            {
                if (!initialized)
                    Initialize();
                return settings;
            }
        }

        public static InputBuffer Buffer
        {
            get { return inputBuffer ??= new InputBuffer(); }
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            settings = InputSettings.Load();

            keyboard = Keyboard.current;
            mouse = Mouse.current;
            gamepad = Gamepad.current;

            initialized = true;

            Logger.LogInfo("VgInput system initialized with New Input System.", LogTag.Input);
        }

        /// <summary>
        /// Should be called in Update(), which is done by VgInputManager
        /// </summary>
        public static void Update()
        {
            if (!initialized)
                Initialize();

            RefreshDevices();
            UpdateActions();
            UpdateAxes();
        }

        private static void RefreshDevices()
        {
            // Update device references if they change
            keyboard ??= Keyboard.current;
            mouse ??= Mouse.current;
            gamepad ??= Gamepad.current;
        }

        public static bool GetButtonDown(InputAction action)
        {
            if (!initialized)
                Initialize();

            foreach (var binding in settings.GetBindings(action))
            {
                if (binding.IsPressed())
                {
                    InputEvents.TriggerButtonPressed(action);
                    inputBuffer.AddInput(action);
                    return true;
                }
            }
            return false;
        }

        public static bool GetButton(InputAction action)
        {
            if (!initialized)
                Initialize();

            foreach (var binding in settings.GetBindings(action))
            {
                if (binding.IsHeld())
                {
                    return true;
                }
            }
            return false;
        }

        public static bool GetButtonUp(InputAction action)
        {
            if (!initialized)
                Initialize();

            foreach (var binding in settings.GetBindings(action))
            {
                if (binding.IsReleased())
                {
                    InputEvents.TriggerButtonReleased(action);
                    return true;
                }
            }
            return false;
        }

        public static float GetAxis(InputAxis axis)
        {
            if (!initialized)
                Initialize();

            var binding = settings.GetAxisBinding(axis);
            if (binding != null)
            {
                return binding.GetValue();
            }

            try
            {
                return Input.GetAxis(axis.ToString());
            }
            catch
            {
                return 0f;
            }
        }

        public static float GetAxisRaw(InputAxis axis)
        {
            if (!initialized)
                Initialize();

            var binding = settings.GetAxisBinding(axis);
            if (binding != null)
            {
                return binding.GetRawValue();
            }

            try
            {
                return Input.GetAxisRaw(axis.ToString());
            }
            catch
            {
                return 0f;
            }
        }

        public static Vector2 GetMovementVector()
        {
            float horizontal = GetAxis(InputAxis.LeftStickHorizontal);
            float vertical = GetAxis(InputAxis.LeftStickVertical);

            // Also check keyboard for WASD/Arrow keys
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    vertical = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    vertical = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    horizontal = 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    horizontal = -1f;
            }

            return new Vector2(horizontal, vertical);
        }

        public static Vector2 GetMovementVectorNormalized()
        {
            Vector2 movement = GetMovementVector();
            if (movement.sqrMagnitude > 1f)
                movement.Normalize();
            return movement;
        }

        public static Vector2 GetLookVector()
        {
            float mouseX = GetAxis(InputAxis.MouseX);
            float mouseY = GetAxis(InputAxis.MouseY);

            mouseX *= settings.mouseSensitivity;
            mouseY *= settings.mouseSensitivity;

            if (settings.invertMouseY)
                mouseY = -mouseY;

            return new Vector2(mouseX, mouseY);
        }

        public static Vector2 GetRightStickVector()
        {
            float horizontal = GetAxis(InputAxis.RightStickHorizontal);
            float vertical = GetAxis(InputAxis.RightStickVertical);

            horizontal *= settings.gamepadSensitivity;
            vertical *= settings.gamepadSensitivity;

            if (settings.invertGamepadY)
                vertical = -vertical;

            return new Vector2(horizontal, vertical);
        }

        public static float GetMouseScrollWheel()
        {
            return GetAxis(InputAxis.MouseScrollWheel);
        }

        public static Vector3 GetMousePosition()
        {
            if (mouse != null)
                return mouse.position.ReadValue();
            return Vector3.zero;
        }

        public static Vector3 GetMouseWorldPosition(Camera camera = null)
        {
            if (camera == null)
                camera = Camera.main;

            if (camera == null)
                return Vector3.zero;

            return camera.ScreenToWorldPoint(GetMousePosition());
        }

        public static bool AnyKeyDown()
        {
            if (keyboard?.anyKey.wasPressedThisFrame == true)
                return true;
            if (
                mouse != null
                && (
                    mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame
                )
            )
                return true;
            if (gamepad != null)
            {
                foreach (var button in gamepad.allControls)
                {
                    if (button is ButtonControl btnCtrl && btnCtrl.wasPressedThisFrame)
                        return true;
                }
            }
            return false;
        }

        public static void SaveSettings()
        {
            if (initialized)
            {
                settings.Save();
            }
        }

        public static void ReloadSettings()
        {
            settings = InputSettings.Load();
            Logger.LogInfo("Input settings reloaded.", LogTag.Input);
        }

        public static void ResetToDefault()
        {
            throw new NotImplementedException();
            // settings = InputSettings.GetDefault();
            // SaveSettings();
            // Logger.LogInfo("Input settings reset to default.", LogTag.Input);
        }

        public static void RebindKey(InputAction action, KeyCode newKey)
        {
            if (!initialized)
                Initialize();

            var oldBindings = settings
                .GetBindings(action)
                .Where(b => b.deviceType == InputDeviceType.Keyboard)
                .ToList();

            foreach (var binding in oldBindings)
            {
                settings.RemoveBinding(binding);
            }

            settings.AddBinding(new InputBinding(action, newKey));
            SaveSettings();
        }

        public static void AddBinding(InputAction action, KeyCode key)
        {
            if (!initialized)
                Initialize();

            settings.AddBinding(new InputBinding(action, key));
            SaveSettings();
        }

        public static void ClearBindings(InputAction action)
        {
            if (!initialized)
                Initialize();

            settings.ClearBindings(action);
            SaveSettings();
        }

        public static void SetMouseSensitivity(float sensitivity)
        {
            if (!initialized)
                Initialize();

            settings.mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            SaveSettings();
        }

        public static void SetGamepadSensitivity(float sensitivity)
        {
            if (!initialized)
                Initialize();

            settings.gamepadSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            SaveSettings();
        }

        public static void ToggleInvertMouseY()
        {
            if (!initialized)
                Initialize();

            settings.invertMouseY = !settings.invertMouseY;
            SaveSettings();
        }

        private static void UpdateActions()
        {
            foreach (var action in Enum.GetValues(typeof(InputAction)).Cast<InputAction>())
            {
                bool isHeld = GetButton(action);

                if (isHeld && heldActions.Contains(action))
                {
                    InputEvents.TriggerButtonHeld(action);
                }
                else if (isHeld)
                {
                    heldActions.Add(action);
                }
                else if (!isHeld && heldActions.Contains(action))
                {
                    heldActions.Remove(action);
                }
            }
        }

        private static void UpdateAxes()
        {
            foreach (var axis in Enum.GetValues(typeof(InputAxis)).Cast<InputAxis>())
            {
                float currentValue = GetAxis(axis);

                if (!previousAxisValues.ContainsKey(axis))
                {
                    previousAxisValues[axis] = currentValue;
                }

                if (Mathf.Abs(currentValue - previousAxisValues[axis]) > 0.01f)
                {
                    InputEvents.TriggerAxisChanged(axis, currentValue);
                    previousAxisValues[axis] = currentValue;
                }
            }
        }

        // csharpier-ignore-start
        private static InputSettings                settings           ;
        private static bool                         initialized        = false!; // Suppresses RCS1129
        private static InputBuffer                  inputBuffer        = new();
        private static Dictionary<InputAxis, float> previousAxisValues = new();
        private static HashSet<InputAction>         heldActions        = new();
        private static Keyboard                     keyboard           ;
        private static Mouse                        mouse              ;
        private static Gamepad                      gamepad            ;
        // csharpier-ignore-end
    }
}
