using System;
using System.Collections;
using System.Collections.Generic;
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

        public override IEnumerator LoadScene()
        {
            AsyncOperation asyncOp;

            try
            {
                asyncOp = SceneManager.LoadSceneAsync(m_SceneName);

                if (asyncOp == null)
                    throw new LoadFailedException($"Failed to start loading scene: {m_SceneName}");
            }
            catch 
            {
                throw;
            }

            yield return asyncOp;
        }

        public override IEnumerator LoadResource()
        {
            yield return null;
        }

        public override IEnumerator LoadPrefab()
        {
            yield return null;
        }

        public override IEnumerator InstantiatePrefab()
        {
            yield return null;
        }

        public override IEnumerator InitLoadedThings()
        {
            yield return null;
        }

        private string m_SceneName;
        private AsyncOperation m_AsyncOperation;
    }
}
