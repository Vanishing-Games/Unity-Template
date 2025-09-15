using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Core
{
    public interface ILoader
    {
        public LoaderType GetLoaderType();

        public void SendLoader();

        public UniTask LoadScene();

        public UniTask LoadResource();

        public UniTask LoadPrefab();

        public UniTask InstantiatePrefab();

        public UniTask InitLoadedThings();
    }

    /// <summary>
    /// - 错误处理: 直接抛出, 由上层处理
    /// </summary>
    public abstract class LoaderBase<TLoadInfo> : MonoBehaviour, ILoader where TLoadInfo : ILoadInfo
    {
        public abstract LoaderType GetLoaderType();

        public abstract void InitLoader(TLoadInfo loadInfo);

        public abstract UniTask LoadScene();

        public abstract UniTask LoadResource();

        public abstract UniTask LoadPrefab();

        public abstract UniTask InstantiatePrefab();

        public abstract UniTask InitLoadedThings();

        public virtual void SendLoader()
        {
            new SendLoaderCommand(this).Execute();
        }
    }
}
