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

            inputActions = new VgInputActions();
            inputActions.Enable();

            keyboard = Keyboard.current;
            mouse = Mouse.current;
            gamepad = Gamepad.current;

            initialized = true;

            Logger.LogInfo("VgInput system initialized with New Input System.", LogTag.Input);
        }

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
            keyboard ??= Keyboard.current;
            mouse ??= Mouse.current;
            gamepad ??= Gamepad.current;
        }

        public static bool GetButtonDown(InputAction action)
        {
            if (!initialized)
                Initialize();

            var unityAction = GetUnityInputAction(action);
            if (unityAction?.WasPressedThisFrame() == true)
            {
                InputEvents.TriggerButtonPressed(action);
                inputBuffer.AddInput(action);
                return true;
            }
            return false;
        }

        public static bool GetButton(InputAction action)
        {
            if (!initialized)
                Initialize();

            var unityAction = GetUnityInputAction(action);
            return unityAction?.IsPressed() == true;
        }

        public static bool GetButtonUp(InputAction action)
        {
            if (!initialized)
                Initialize();

            var unityAction = GetUnityInputAction(action);
            if (unityAction != null && unityAction.WasReleasedThisFrame())
            {
                InputEvents.TriggerButtonReleased(action);
                return true;
            }
            return false;
        }

        public static float GetAxis(InputAxis axis)
        {
            if (!initialized)
                Initialize();

            float value = GetAxisRaw(axis);
            return Mathf.Clamp(value, -1f, 1f);
        }

        public static float GetAxisRaw(InputAxis axis)
        {
            if (!initialized)
                Initialize();

            return axis switch
            {
                InputAxis.LeftStickHorizontal => GetVector2Value(inputActions.Gameplay.Move).x,
                InputAxis.LeftStickVertical => GetVector2Value(inputActions.Gameplay.Move).y,
                InputAxis.RightStickHorizontal => GetVector2Value(
                    inputActions.Gameplay.RightStick
                ).x,
                InputAxis.RightStickVertical => GetVector2Value(inputActions.Gameplay.RightStick).y,
                InputAxis.LeftTrigger => GetFloatValue(inputActions.Gameplay.LeftTrigger),
                InputAxis.RightTrigger => GetFloatValue(inputActions.Gameplay.RightTrigger),
                InputAxis.MouseX => GetVector2Value(inputActions.Gameplay.Look).x,
                InputAxis.MouseY => GetVector2Value(inputActions.Gameplay.Look).y,
                InputAxis.MouseScrollWheel => GetFloatValue(inputActions.Gameplay.ScrollWheel),
                _ => 0f,
            };
        }

        public static Vector2 GetMovementVector()
        {
            if (!initialized)
                Initialize();

            return GetVector2Value(inputActions.Gameplay.Move);
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
            if (!initialized)
                Initialize();

            return GetVector2Value(inputActions.Gameplay.MousePosition);
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

        public static void SetMouseSensitivity(float sensitivity)
        {
            if (!initialized)
                Initialize();

            settings.mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }

        public static void SetGamepadSensitivity(float sensitivity)
        {
            if (!initialized)
                Initialize();

            settings.gamepadSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }

        public static void ToggleInvertMouseY()
        {
            if (!initialized)
                Initialize();

            settings.invertMouseY = !settings.invertMouseY;
        }

        private static UnityEngine.InputSystem.InputAction GetUnityInputAction(InputAction action)
        {
            if (inputActions == null)
                return null;

            if (!actionCache.TryGetValue(action, out var unityAction))
            {
                var actionName = action.ToString();
                var gameplayActions = inputActions.Gameplay;
                var actionProperty = gameplayActions
                    .GetType()
                    .GetProperty(
                        actionName,
                        System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.Instance
                    );

                if (
                    actionProperty != null
                    && actionProperty.PropertyType == typeof(UnityEngine.InputSystem.InputAction)
                )
                {
                    unityAction =
                        actionProperty.GetValue(gameplayActions)
                        as UnityEngine.InputSystem.InputAction;
                    actionCache[action] = unityAction;
                }
            }

            return unityAction;
        }

        private static Vector2 GetVector2Value(UnityEngine.InputSystem.InputAction action)
        {
            if (action == null)
                return Vector2.zero;

            return action.ReadValue<Vector2>();
        }

        private static float GetFloatValue(UnityEngine.InputSystem.InputAction action)
        {
            if (action == null)
                return 0f;

            return action.ReadValue<float>();
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
        private static VgInputActions                                                     inputActions       ;
        private static InputSettings                                                      settings           ;
        private static bool                                                               initialized        = false!;
        private static InputBuffer                                                        inputBuffer        = new();
        private static Dictionary<InputAxis, float>                                       previousAxisValues = new();
        private static HashSet<InputAction>                                               heldActions        = new();
        private static Dictionary<InputAction, UnityEngine.InputSystem.InputAction>       actionCache        = new();
        private static Keyboard                                                           keyboard           ;
        private static Mouse                                                              mouse              ;
        private static Gamepad                                                            gamepad            ;
        // csharpier-ignore-end
    }
}
