using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Core
{
    public abstract class GameMonoModule : MonoBehaviour
    {
        private void Awake()
        {
            RegisterLoadEvent();
        }

        private void OnDestroy()
        {
            UnregisterLoadEvent();
        }

        protected abstract void CreateLoader();

        public abstract void ReceiveLoadInfo(LoadRequestEvent loadEventInfo);

        public virtual void RegisterLoadEvent()
        {
            m_LoadEventSubscription = MessageBroker
                .Global.Receive<LoadRequestEvent>()
                .Subscribe(ReceiveLoadInfo);
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
