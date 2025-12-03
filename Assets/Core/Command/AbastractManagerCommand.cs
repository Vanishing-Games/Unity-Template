using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public abstract class AbastractManagerCommand<TManager> : IUniTaskCommand<bool>
        where TManager : MonoSingletonLasy<TManager>
    {
        protected TManager Manager => GetManager();

        protected TManager GetManager()
        {
            return MonoSingletonLasy<TManager>.Instance;
        }

        public abstract bool Execute();

        public virtual UniTask<bool> ExecuteAsync()
        {
            throw new System.NotImplementedException();
        }

        public bool Undo()
        {
            return false;
        }
    }
}
