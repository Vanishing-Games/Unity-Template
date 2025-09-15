using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneLoader : LoaderBase<SceneLoadInfo>
    {
        public override LoaderType GetLoaderType()
        {
            return LoaderType.SceneLoader;
        }

        public override void InitLoader(SceneLoadInfo sceneLoadInfo)
        {
            m_SceneName = sceneLoadInfo.SceneName;
            m_AsyncOperation = null;
        }

        public override async UniTask LoadScene()
        {
            AsyncOperation asyncOp;

            try
            {
                asyncOp = SceneManager.LoadSceneAsync(m_SceneName);

                if (asyncOp == null)
                    throw new LoadFailedException($"Failed to start loading scene: {m_SceneName}");

                await asyncOp;
            }
            catch
            {
                throw;
            }
        }

        public override async UniTask LoadResource()
        {
            await UniTask.Yield();
        }

        public override async UniTask LoadPrefab()
        {
            await UniTask.Yield();
        }

        public override async UniTask InstantiatePrefab()
        {
            await UniTask.Yield();
        }

        public override async UniTask InitLoadedThings()
        {
            await UniTask.Yield();
        }

        private string m_SceneName;
        private AsyncOperation m_AsyncOperation;
    }
}
