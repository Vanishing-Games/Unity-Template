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
            Logger.LogInfo("[GameCore] Runing Game Check...", LogTag.GameRunCheck);

#if UNITY_EDITOR
            if (!GameRunInEditorCheck())
                return false;
#endif
            return true;
        }

        private async UniTask InitiatingGame()
        {
            Logger.LogInfo("[GameCore] Initiating Game...", LogTag.GameCoreStart);

            await InitProgressBar();

            using (var loadProgressManager = VgLoadProgressManager.Instance)
            {
                loadProgressManager.Show();

                LoadRequestEvent loadEvent = new("Loading Game Start Scene");
                loadEvent.AddLoadInfo(new SceneLoadInfo("GameStartScene"));
                var loadGameEntry = new LoadRequestCommand(loadEvent);

                bool loadCompleted = false;

                await UniTask.WhenAny(
                    loadGameEntry.ExecuteAsync(),
                    UniTask.Create(async () =>
                    {
                        while (!loadCompleted)
                        {
                            if (loadProgressManager.GetProgress() < 0.99f)
                                loadProgressManager.AddProgress(0.01f);

                            await UniTask.DelayFrame(1);
                        }
                    })
                );

                loadCompleted = true;
            }

            Logger.LogInfo("[GameCore] Initiating Game Done", LogTag.GameCoreStart);
        }

        private async UniTask InitProgressBar()
        {
            Logger.LogInfo("Initiating ProgressBar...", LogTag.GameCoreStart);

            var loadEvent = new LoadRequestEvent("Load Progress Bar");
            loadEvent.AddLoadInfo(new ProgressBarLoadInfo());
            var loadProgressBar = new LoadRequestCommand(loadEvent);

            await loadProgressBar.ExecuteAsync();

            Logger.LogInfo("Initiating ProgressBar Done", LogTag.GameCoreStart);

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
                Logger.LogError(
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
