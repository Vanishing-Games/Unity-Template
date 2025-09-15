using System.Collections;
using System.Collections.Generic;
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

        protected abstract void CreateLoader(ILoadInfo loadInfo);

        public abstract void ReceiveLoadInfo(LoadRequestEvent loadEventInfo);

        public virtual void RegisterLoadEvent()
        {
            m_LoadEventSubscription = MessageBroker
                .Global.Receive<LoadRequestEvent>()
                .Subscribe(ReceiveLoadInfo);

            Logger.EditorLogVerbose($"SystemMonoModule: {GetType()} RegisterLoadEvent", LogTag.Loading);
        }

        public virtual void UnregisterLoadEvent()
        {
            if (m_LoadEventSubscription != null)
            {
                m_LoadEventSubscription.Dispose();
                m_LoadEventSubscription = null;
            }

            Logger.EditorLogVerbose($"SystemMonoModule: {GetType()} UnregisterLoadEvent", LogTag.Loading);
        }

        private System.IDisposable m_LoadEventSubscription;
    }
}
