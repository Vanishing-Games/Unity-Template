using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using static Core.LoadRequestEvent;

namespace Core
{
    public abstract class CoreModuleManagerBase<T, TLoadInfo, TLoader> : MonoSingletonPersistent<T>
        where T : MonoSingletonPersistent<T>
        where TLoadInfo : ILoadInfo
        where TLoader : LoaderBase<TLoadInfo>
    {
        protected override void Awake()
        {
            base.Awake();

            RegisterLoadEvent();

            Logger.LogVerbose($"SystemMonoModule: {GetType()} Awake", LogTag.Loading);
        }

        private void OnDestroy()
        {
            UnregisterLoadEvent();

            Logger.LogVerbose($"SystemMonoModule: {GetType()} OnDestroy", LogTag.Loading);
        }

        protected virtual void OnReceiveLoadRequest(LoadRequestEvent loadEventInfo)
        {
            var info = loadEventInfo.GetLoadInfo(GetLoaderType());

            if (info != null)
            {
                Logger.LogVerbose(
                    $"[CoreModuleManagerBase] {GetType()} ReceiveLoadInfo",
                    LogTag.CoreModule
                );
                CreateLoader(info);
            }
        }

        protected abstract LoaderType GetLoaderType();

        protected abstract void OnLoadingError(Exception exception);

        protected virtual async void OnLoadingEnd()
        {
            UnregisterLoadEvent();

            // Wait for MessageBroker to clear up the event to ensure subscribing new event
            await UniTask.Yield();
            RegisterLoadEvent();
        }

        protected virtual void CreateLoader(ILoadInfo loadInfo)
        {
            if (loadInfo is TLoadInfo typedLoadInfo)
            {
                var loader = GetComponent<TLoader>();
                if (loader != null)
                {
                    Destroy(loader);
                }

                loader = gameObject.AddComponent<TLoader>();
                loader.InitLoader(typedLoadInfo);
                loader.SendLoader();
            }
            else
            {
                MessageBroker.Global.PublishErrorResume<LoadRequestEvent>(
                    this,
                    new LoadFailedException($"LoadInfo is not a {typeof(TLoadInfo).Name}")
                );
            }
        }

        public virtual void RegisterLoadEvent()
        {
            m_LoadEventSubscription = MessageBroker.Global.Subscribe<LoadRequestEvent>(
                OnReceiveLoadRequest,
                OnLoadingError,
                OnLoadingEnd
            );

            Logger.LogVerbose(
                $"SystemMonoModule: {GetType()} RegisterLoadEvent",
                LogTag.Loading
            );
        }

        public virtual void UnregisterLoadEvent()
        {
            if (m_LoadEventSubscription != null)
            {
                m_LoadEventSubscription.Dispose();
                m_LoadEventSubscription = null;
            }
        }

        private System.IDisposable m_LoadEventSubscription;
    }
}
