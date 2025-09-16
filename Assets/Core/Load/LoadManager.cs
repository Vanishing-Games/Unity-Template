using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Text;

namespace Core
{
    public class LoadManager : MonoSingletonLasy<LoadManager>
    {
        public void PrepareForLoad(LoadRequestEvent loadEvent)
        {
            SubscribeEvents();
            m_LoadInfos.AddRange(loadEvent.m_LoadInfos);

            StringBuilder sb = new();
            sb.AppendLine(
                $"PrepareForLoad: {loadEvent.m_LoadDesc}. Needed Loaders: {loadEvent.m_LoadInfos.Count}"
            );
            foreach (var loadInfo in loadEvent.m_LoadInfos)
            {
                sb.AppendLine($"  {loadInfo.GetNeededLoaderType()}");
            }
            Logger.ReleaseLogInfo(sb.ToString(), LogTag.Loading);
        }

        public void RegisterLoader(ILoader newLoader)
        {
            Logger.DebugLogVerbose(
                $"Registering loader {newLoader.GetLoaderType()}",
                LogTag.Loading
            );

            bool isLoaderNeeded = false;
            foreach (var loadInfo in m_LoadInfos)
            {
                if (loadInfo.GetNeededLoaderType() == newLoader.GetLoaderType())
                {
                    isLoaderNeeded = true;
                    break;
                }
            }

            if (!isLoaderNeeded)
            {
                Logger.DebugLogWarn(
                    $"Loader {newLoader.GetLoaderType()} is not needed",
                    LogTag.Loading
                );
                return;
            }

            foreach (var loader in m_Loaders)
            {
                if (loader.GetLoaderType() == newLoader.GetLoaderType())
                {
                    Logger.DebugLogWarn(
                        $"Loader {loader.GetLoaderType()} already registed",
                        LogTag.Loading
                    );
                    return;
                }
            }

            m_Loaders.Add(newLoader);
            Logger.ReleaseLogInfo($"Registed loader {newLoader.GetLoaderType()}", LogTag.Loading);

            if (m_Loaders.Count == m_LoadInfos.Count)
            {
                Logger.ReleaseLogInfo("All loaders registered, starting loading", LogTag.Loading);
                Load();
            }
        }

        private void Reset()
        {
            m_Loaders.Clear();
            m_LoadInfos.Clear();
            UnSubscribeEvents();
        }

        private void Load()
        {
            MessageBroker.Global.PublishComplete(new LoadStartEvent());
            LoadAsync().Forget();
        }

        private async UniTask LoadAsync()
        {
            try
            {
                MessageBroker.Global.Publish(new LoadProgressEvent("Loaders Preparing..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.BeforeLoad();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Loading Scenes..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.LoadScene();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Reading Resources..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.LoadResource();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Loading Prefabs..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.LoadPrefab();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Instantiating Prefabs..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.InstantiatePrefab();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Initializing..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.InitLoadedThings();
                }

                Reset();

                MessageBroker.Global.PublishComplete(new LoadProgressEvent("Loading Done"));
                MessageBroker.Global.Complete<LoadRequestEvent>();
            }
            catch (Exception ex)
            {
                Logger.ReleaseLogError(
                    $"Loading failed with exception: \n {ex.Message}",
                    LogTag.Loading
                );
                Reset();

                MessageBroker.Global.PublishErrorStop<LoadProgressEvent>(this, ex);
                MessageBroker.Global.PublishErrorStop<LoadRequestEvent>(this, ex);
            }
        }

        private void SubscribeEvents()
        {
            m_LoadRequestEventSubscription = MessageBroker.Global.Subscribe<LoadRequestEvent>(
                OnLoadRequest,
                OnLoadRequestError,
                OnLoadRequestComplete
            );
        }

        private void OnLoadRequestComplete(R3.Result result)
        {
            Logger.ReleaseLogInfo("LoadRequestComplete", LogTag.Loading);
        }

        private void OnLoadRequestError(Exception exception)
        {
            Logger.ReleaseLogError("LoadRequestError", LogTag.Loading);
        }

        private void OnLoadRequest(LoadRequestEvent loadEvent)
        {
            var waitMs = (int)loadEvent.m_LoadSettings.maxWaitTimeInMs;
            if (waitMs <= 0)
            {
                return;
            }

            UniTask.Void(async () =>
            {
                await UniTask.Delay(waitMs);

                if (m_LoadRequestEventSubscription != null)
                {
                    var ex = new TimeoutException(
                        $"Loading timeout after {waitMs} ms for '{loadEvent.m_LoadDesc}'. Registered loaders: {m_Loaders.Count}/{m_LoadInfos.Count}"
                    );
                    Logger.ReleaseLogError(ex.Message, LogTag.Loading);

                    MessageBroker.Global.PublishErrorStop<LoadRequestEvent>(this, ex);
                    Reset();
                }
            });
        }

        private void UnSubscribeEvents()
        {
            m_LoadRequestEventSubscription?.Dispose();
            m_LoadRequestEventSubscription = null;
        }

        private List<ILoader> m_Loaders = new();
        private List<ILoadInfo> m_LoadInfos = new();
        private IDisposable m_LoadRequestEventSubscription;
    }
}
