using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core
{
    public interface ILoader
    {
        public LoaderType GetLoaderType();

        public void SendLoader();

        public UniTask BeforeLoad();

        public UniTask LoadScene();

        public UniTask LoadResource();

        public UniTask LoadPrefab();

        public UniTask InstantiatePrefab();

        public UniTask InitLoadedThings();
    }

    /// <summary>
    /// - 错误处理: 直接抛出, 由上层处理
    /// </summary>
    public abstract class LoaderBase<TLoadInfo> : MonoBehaviour, ILoader
        where TLoadInfo : ILoadInfo
    {
        public abstract LoaderType GetLoaderType();

        public abstract void InitLoader(TLoadInfo loadInfo);

        public virtual UniTask BeforeLoad()
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask LoadScene()
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask LoadResource()
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask LoadPrefab()
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask InstantiatePrefab()
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask InitLoadedThings()
        {
            return UniTask.CompletedTask;
        }

        public virtual void SendLoader()
        {
            new SendLoaderCommand(this).Execute();
        }
    }
}
