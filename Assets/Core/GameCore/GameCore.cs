using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Core
{
    public partial class GameCore : MonoSingletonPersistent<GameCore>
    {
        private async UniTask<bool> GameRunCheck()
        {
#if UNITY_EDITOR
            if (!GameRunInEditorCheck())
                return false;
#endif
            return true;
        }

        private async UniTask InitiatingGame() { }

        internal async UniTask QuitGame()
        {
            try
            {
                MessageBroker.Global.Publish(new SaveRequestEvent());

                bool saveCompleted = false;
                var subscription = MessageBroker.Global.Subscribe<SaveRequestEvent>(
                    _ => { },
                    () => saveCompleted = true
                );

                using (subscription)
                {
                    await UniTask.WaitUntil(() => saveCompleted).Timeout(TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex)
            {
                Logger.EditorLogError(
                    $"Error during save operation: {ex.Message}",
                    LogTag.GameQuit
                );
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
