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
        private bool GameRunCheck()
        {
            Logger.ReleaseLogInfo("[GameCore] Runing Game Check...", LogTag.GameRunCheck);

#if UNITY_EDITOR
            if (!GameRunInEditorCheck())
                return false;
#endif
            return true;
        }

        private async UniTask InitiatingGame()
        {
            Logger.ReleaseLogInfo("[GameCore] Initiating Game...", LogTag.GameCoreAwake);

            await InitProgressBar();

            Logger.ReleaseLogInfo("[GameCore] Initiating Game Done", LogTag.GameCoreAwake);
        }

        private async UniTask InitProgressBar()
        {
            Logger.ReleaseLogInfo("Initiating ProgressBar...", LogTag.GameCoreAwake);

            var loadEvent = new LoadRequestEvent("Load Progress Bar");
            loadEvent.AddLoadInfo(new ProgressBarLoadInfo());
            var loadProgressBar = new LoadRequestCommand(loadEvent);

            await loadProgressBar.ExecuteAsync();

            Logger.ReleaseLogInfo("Initiating ProgressBar Done", LogTag.GameCoreAwake);

            return;
        }

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
