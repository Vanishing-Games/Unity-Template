using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Core
{
    public enum ConsoleDisplayMode
    {
        Full,
        Bottom,
        SideRight,
        SideLeft,
    }

    public class VgIngameConsoleManager
        : CoreModuleManagerBase<VgIngameConsoleManager>,
            ICoreModuleSystem
    {
        public string SystemName => "VgIngameConsoleManager";
        public Type[] Dependencies => Array.Empty<Type>();

        public void RegisterHooks(IGameCoreHookRegistry registry)
        {
            registry.OnBootStart(async () =>
            {
                InitConsole();
                await UniTask.CompletedTask;
            });
            registry.OnUpdate(Tick);
        }

        public void ShowConsole()
        {
            if (m_DebugLogManager == null)
                return;

            SetDisplayMode(m_CurrentMode);
            m_DebugLogManager.gameObject.SetActive(true);
            m_DebugLogManager.ShowLogWindow();
            m_IsVisible = true;
            MessageBroker.Global.Publish(
                new VgIngameConsoleManagerEvents.ConsoleVisibilityChangedEvent(true)
            );
            CLogger.LogInfo("[VgIngameConsoleManager] Console shown.", LogTag.Game);
        }

        public void HideConsole()
        {
            if (m_DebugLogManager == null)
                return;

            m_DebugLogManager.gameObject.SetActive(false);
            m_DebugLogManager.HideLogWindow();
            m_IsVisible = false;
            MessageBroker.Global.Publish(
                new VgIngameConsoleManagerEvents.ConsoleVisibilityChangedEvent(false)
            );
            CLogger.LogInfo("[VgIngameConsoleManager] Console hidden.", LogTag.Game);
        }

        public void SetDisplayMode(ConsoleDisplayMode mode)
        {
            if (m_LogWindowTR == null)
                return;

            m_CurrentMode = mode;
            (Vector2 anchorMin, Vector2 anchorMax) = mode switch
            {
                ConsoleDisplayMode.Full => (Vector2.zero, Vector2.one),
                ConsoleDisplayMode.Bottom => (Vector2.zero, new Vector2(1f, 0.4f)),
                ConsoleDisplayMode.SideRight => (new Vector2(0.5f, 0f), Vector2.one),
                ConsoleDisplayMode.SideLeft => (Vector2.zero, new Vector2(0.5f, 1f)),
                _ => (Vector2.zero, Vector2.one),
            };

            m_LogWindowTR.anchorMin = anchorMin;
            m_LogWindowTR.anchorMax = anchorMax;
            m_LogWindowTR.offsetMin = Vector2.zero;
            m_LogWindowTR.offsetMax = Vector2.zero;

            MessageBroker.Global.Publish(
                new VgIngameConsoleManagerEvents.ConsoleDisplayModeChangedEvent(mode)
            );
        }

        [ConsoleMethod(
            "console_mode",
            "Set console display mode: full | bottom | side_right | side_left"
        )]
        public static void SetConsoleModeCommand(string modeName)
        {
            ConsoleDisplayMode? mode = modeName.ToLowerInvariant() switch
            {
                "full" => ConsoleDisplayMode.Full,
                "bottom" => ConsoleDisplayMode.Bottom,
                "side_right" => ConsoleDisplayMode.SideRight,
                "side_left" => ConsoleDisplayMode.SideLeft,
                _ => null,
            };

            if (mode == null)
            {
                CLogger.LogError(
                    $"[VgIngameConsoleManager] Unknown mode '{modeName}'. Use: full | bottom | side_right | side_left",
                    LogTag.Game
                );
                return;
            }

            Instance.SetDisplayMode(mode.Value);
        }

        private void InitConsole()
        {
            m_DebugLogManager = DebugLogManager.Instance;
            m_DebugLogManager ??= GetComponentInChildren<DebugLogManager>(true);
            if (m_DebugLogManager == null)
            {
                CLogger.LogError(
                    "[VgIngameConsoleManager] DebugLogManager not found.",
                    LogTag.Game
                );

                return;
            }

            var field = typeof(DebugLogManager).GetField(
                "logWindowTR",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            m_LogWindowTR = field?.GetValue(m_DebugLogManager) as RectTransform;

            if (m_LogWindowTR == null)
                CLogger.LogError(
                    "[VgIngameConsoleManager] Could not access logWindowTR via reflection.",
                    LogTag.Game
                );

#if UNITY_EDITOR || BUILD_MODE_DEBUG
            m_DebugLogManager.PopupEnabled = true;
#else
            m_DebugLogManager.PopupEnabled = false;
#endif

            m_DebugLogManager.HideLogWindow();
        }

        private void Tick()
        {
            TickSequenceDetection();

            if (!m_IsVisible)
                return;

            TickEsc();
            TickTabCycle();
        }

        private void TickSequenceDetection()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            DirectionKey? pressed = null;

            if (keyboard.upArrowKey.wasPressedThisFrame)
                pressed = DirectionKey.Up;
            else if (keyboard.downArrowKey.wasPressedThisFrame)
                pressed = DirectionKey.Down;
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
                pressed = DirectionKey.Right;
            else if (keyboard.leftArrowKey.wasPressedThisFrame)
                pressed = DirectionKey.Left;

            var gamepad = Gamepad.current;
            if (gamepad != null && pressed == null)
            {
                if (gamepad.dpad.up.wasPressedThisFrame)
                    pressed = DirectionKey.Up;
                else if (gamepad.dpad.down.wasPressedThisFrame)
                    pressed = DirectionKey.Down;
                else if (gamepad.dpad.right.wasPressedThisFrame)
                    pressed = DirectionKey.Right;
                else if (gamepad.dpad.left.wasPressedThisFrame)
                    pressed = DirectionKey.Left;
            }

            if (pressed == null)
                return;

            if (m_SequenceIndex > 0 && Time.unscaledTime - m_LastInputTime > k_SequenceTimeout)
            {
                m_SequenceIndex = 0;
                CLogger.LogInfo(
                    "[VgIngameConsoleManager] Unlock sequence timed out, reset.",
                    LogTag.Game
                );
            }

            if (pressed.Value == s_UnlockSequence[m_SequenceIndex])
            {
                m_SequenceIndex++;
                m_LastInputTime = Time.unscaledTime;

                if (m_SequenceIndex >= s_UnlockSequence.Length)
                {
                    m_SequenceIndex = 0;
                    ShowConsole();
                }
            }
            else
            {
                m_SequenceIndex = pressed.Value == s_UnlockSequence[0] ? 1 : 0;
                m_LastInputTime = Time.unscaledTime;
            }
        }

        private void TickEsc()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                HideConsole();
        }

        private void TickTabCycle()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (!keyboard.tabKey.wasPressedThisFrame)
                return;

            var selected = EventSystem.current?.currentSelectedGameObject;
            bool inputFieldFocused =
                selected != null && selected.GetComponent("TMP_InputField") != null;

            if (!inputFieldFocused)
                CycleDisplayMode();
        }

        private void CycleDisplayMode()
        {
            ConsoleDisplayMode next = m_CurrentMode switch
            {
                ConsoleDisplayMode.Full => ConsoleDisplayMode.Bottom,
                ConsoleDisplayMode.Bottom => ConsoleDisplayMode.SideRight,
                ConsoleDisplayMode.SideRight => ConsoleDisplayMode.SideLeft,
                ConsoleDisplayMode.SideLeft => ConsoleDisplayMode.Full,
                _ => ConsoleDisplayMode.Full,
            };
            SetDisplayMode(next);
        }

        private enum DirectionKey
        {
            Up,
            Down,
            Left,
            Right,
        }

        // HAVE A CUP OF LIBERATEA !!!
        private static readonly DirectionKey[] s_UnlockSequence =
        {
            DirectionKey.Up,
            DirectionKey.Down,
            DirectionKey.Right,
            DirectionKey.Left,
            DirectionKey.Up,
        };

        private const float k_SequenceTimeout = 3f;

        private DebugLogManager m_DebugLogManager;
        private RectTransform m_LogWindowTR;
        private ConsoleDisplayMode m_CurrentMode = ConsoleDisplayMode.Full;
        private bool m_IsVisible;
        private int m_SequenceIndex;
        private float m_LastInputTime;
    }
}
