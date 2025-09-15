using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core
{
    public interface ILoader
    {
        public LoaderType GetLoaderType();

        public void SendLoader();

        public IEnumerator LoadScene();

        public IEnumerator LoadResource();

        public IEnumerator LoadPrefab();

        public IEnumerator InstantiatePrefab();

        public IEnumerator InitLoadedThings();
    }

    public abstract class LoaderBase<TLoadInfo> : MonoBehaviour, ILoader where TLoadInfo : ILoadInfo
    {
        public abstract LoaderType GetLoaderType();

        public abstract void InitLoader(TLoadInfo loadInfo);

        public abstract IEnumerator LoadScene();

        public abstract IEnumerator LoadResource();

        public abstract IEnumerator LoadPrefab();

        public abstract IEnumerator InstantiatePrefab();

        public abstract IEnumerator InitLoadedThings();

        public virtual void SendLoader()
        {
            new SendLoaderCommand(this).Execute();
        }
    }
}
