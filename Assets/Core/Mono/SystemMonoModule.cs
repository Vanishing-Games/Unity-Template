using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using static Core.LoadRequestEvent;

namespace Core
{
    public abstract class SystemMonoModule<T> : MonoSingletonLasy<T>
        where T : MonoSingletonLasy<T>
    {
        private void Awake()
        {
            RegisterLoadEvent();

            Logger.EditorLogVerbose($"SystemMonoModule: {GetType()} Awake", LogTag.Loading);
        }

        private void OnDestroy()
        {
            UnregisterLoadEvent();

            Logger.EditorLogVerbose($"SystemMonoModule: {GetType()} OnDestroy", LogTag.Loading);
        }

        protected abstract void OnReceiveLoadRequest(LoadRequestEvent loadEventInfo);

        protected abstract void OnLoadingError(Exception exception);

        protected virtual async void OnLoadingEnd()
        {
            UnregisterLoadEvent();

            // Wait for MessageBroker to clear up the event to ensure subscribing new event
            await UniTask.DelayFrame(1);
            RegisterLoadEvent();
        }

        protected abstract void CreateLoader(ILoadInfo loadInfo);

        public virtual void RegisterLoadEvent()
        {
            m_LoadEventSubscription = MessageBroker.Global.Subscribe<LoadRequestEvent>(
                OnReceiveLoadRequest,
                OnLoadingError,
                OnLoadingEnd
            );

            Logger.EditorLogVerbose(
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

            Logger.EditorLogVerbose(
                $"SystemMonoModule: {GetType()} UnregisterLoadEvent",
                LogTag.Loading
            );
        }

        private System.IDisposable m_LoadEventSubscription;
    }
}
