using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace Core
{
    public class VgInputManager : MonoSingletonPersistent<VgInputManager>
    {
        protected override void Awake()
        {
            base.Awake();
            InputSystem.onDeviceChange += OnDeviceChange;
#if UNITY_EDITOR
            InitializeDebugData();
#endif
        }

        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void Update()
        {
            VgInput.Update();

#if UNITY_EDITOR
            if (showRealTimeInput)
            {
                UpdateDebugDisplay();
            }
#endif
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    Logger.LogInfo($"Input device added: {device.displayName}", LogTag.Input);
                    break;
                case InputDeviceChange.Removed:
                    Logger.LogInfo($"Input device removed: {device.displayName}", LogTag.Input);
                    break;
                case InputDeviceChange.Reconnected:
                    Logger.LogInfo($"Input device reconnected: {device.displayName}", LogTag.Input);
                    break;
                case InputDeviceChange.Disconnected:
                    Logger.LogInfo(
                        $"Input device disconnected: {device.displayName}",
                        LogTag.Input
                    );
                    break;
            }
        }

#if UNITY_EDITOR
        private void InitializeDebugData()
        {
            inputActionStates.Clear();
            inputAxisValues.Clear();

            foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
            {
                inputActionStates[action] = new InputActionState();
            }

            foreach (InputAxis axis in Enum.GetValues(typeof(InputAxis)))
            {
                inputAxisValues[axis] = 0f;
            }
        }

        private void UpdateDebugDisplay()
        {
            // Update InputAction states
            foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
            {
                var state = inputActionStates[action];
                state.isPressed = VgInput.GetButtonDown(action);
                state.isHeld = VgInput.GetButton(action);
                state.isReleased = VgInput.GetButtonUp(action);

                // Get bindings info
                var bindings = VgInput.Settings.GetBindings(action);
                state.bindingCount = bindings.Count;
                state.bindingInfo = string.Join(
                    ", ",
                    bindings.Select(b => b.GetDisplayName())
                );
            }

            // Update InputAxis values
            foreach (InputAxis axis in Enum.GetValues(typeof(InputAxis)))
            {
                inputAxisValues[axis] = VgInput.GetAxis(axis);
            }

            // Update composite inputs
            movementVector = VgInput.GetMovementVector();
            movementVectorNormalized = VgInput.GetMovementVectorNormalized();
            lookVector = VgInput.GetLookVector();
            rightStickVector = VgInput.GetRightStickVector();
            mousePosition = VgInput.GetMousePosition();
            mouseScrollWheel = VgInput.GetMouseScrollWheel();
        }

        [Serializable]
        public class InputActionState
        {
            [HorizontalGroup("State")]
            [LabelWidth(80)]
            [ShowInInspector, ReadOnly]
            public bool isPressed;

            [HorizontalGroup("State")]
            [LabelWidth(60)]
            [ShowInInspector, ReadOnly]
            public bool isHeld;

            [HorizontalGroup("State")]
            [LabelWidth(80)]
            [ShowInInspector, ReadOnly]
            public bool isReleased;

            [ShowInInspector, ReadOnly]
            [LabelText("Bindings")]
            public string bindingInfo = "";

            [ShowInInspector, ReadOnly]
            [LabelText("Count")]
            public int bindingCount = 0;
        }

        #region Settings
        [BoxGroup("Settings")]
        [LabelText("Input Settings Asset")]
        public InputSettings inputSettings;
        #endregion

        #region Real-Time Input Debug Display
        [BoxGroup("Debug Display")]
        [LabelText("Show Real-Time Input")]
        [ToggleLeft]
        public bool showRealTimeInput = true;

        [BoxGroup("Debug Display/Input Actions")]
        [ShowInInspector, ReadOnly]
        [LabelText("Action States")]
        private Dictionary<InputAction, InputActionState> inputActionStates = new();

        [BoxGroup("Debug Display/Input Axes")]
        [ShowInInspector, ReadOnly]
        [LabelText("Axis Values")]
        private Dictionary<InputAxis, float> inputAxisValues = new();

        [BoxGroup("Debug Display/Composite Inputs")]
        [ShowInInspector, ReadOnly]
        [LabelText("Movement Vector")]
        private Vector2 movementVector;

        [BoxGroup("Debug Display/Composite Inputs")]
        [ShowInInspector, ReadOnly]
        [LabelText("Movement (Normalized)")]
        private Vector2 movementVectorNormalized;

        [BoxGroup("Debug Display/Composite Inputs")]
        [ShowInInspector, ReadOnly]
        [LabelText("Look Vector")]
        private Vector2 lookVector;

        [BoxGroup("Debug Display/Composite Inputs")]
        [ShowInInspector, ReadOnly]
        [LabelText("Right Stick Vector")]
        private Vector2 rightStickVector;

        [BoxGroup("Debug Display/Mouse")]
        [ShowInInspector, ReadOnly]
        [LabelText("Mouse Position")]
        private Vector3 mousePosition;

        [BoxGroup("Debug Display/Mouse")]
        [ShowInInspector, ReadOnly]
        [LabelText("Mouse Scroll Wheel")]
        [ProgressBar(-5, 5, ColorGetter = "GetScrollColor")]
        private float mouseScrollWheel;
        #endregion

        #region Odin Helper Methods
        private Color GetScrollColor(float value)
        {
            if (Mathf.Abs(value) < 0.01f)
                return Color.gray;
            return value > 0 ? Color.green : Color.red;
        }
        #endregion
#else
        public InputSettings inputSettings;
#endif
    }
}
