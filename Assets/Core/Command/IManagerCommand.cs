using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public interface IManagerCommand : ITriggerCommand
    {
        public UniTask<bool> ExecuteAsync();
    }
}
