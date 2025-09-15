using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public abstract class AbastractManagerCommand<TManager> : IManagerCommand
        where TManager : MonoSingletonLasy<TManager>
    {
        protected TManager m_Manager => GetManager();

        protected TManager GetManager()
        {
            return MonoSingletonLasy<TManager>.Instance;
        }

        public abstract bool Execute();
    }
}
