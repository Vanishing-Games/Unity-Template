using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
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
            m_SceneLoadInfo = sceneLoadInfo;

            m_AddressableSceneHandle = default;
        }

        public override async UniTask LoadScene()
        {
            await (
                m_SceneLoadInfo.SceneAssetMode switch
                {
                    SceneAssetMode.FromBuildingSceneName => LoadSceneFromBuildingName(),
                    SceneAssetMode.FromBuildingSceneIndex => LoadSceneFromBuildingIndex(),
                    SceneAssetMode.FromEditorPath => LoadSceneFromEditorPath(),
                    SceneAssetMode.FromStreamingAssetsPath => LoadSceneFromStreamingAssetPath(),
                    _ => throw new LoadFailedException(
                        $"Unsupported scene asset mode: {m_SceneLoadInfo.SceneAssetMode}"
                    ),
                }
            );
        }

        private async UniTask LoadSceneFromBuildingName()
        {
            if (string.IsNullOrEmpty(m_SceneLoadInfo.SceneName))
                throw new LoadFailedException("Scene name is null or empty");

            var asyncOp =
                SceneManager.LoadSceneAsync(
                    m_SceneLoadInfo.SceneName,
                    m_SceneLoadInfo.SceneLoadParameters
                ) ?? throw new LoadFailedException("Failed to start loading scene by name");

            await asyncOp;
        }

        private async UniTask LoadSceneFromBuildingIndex()
        {
            if (m_SceneLoadInfo.SceneIndex < 0)
                throw new LoadFailedException($"Invalid scene index: {m_SceneLoadInfo.SceneIndex}");

            var asyncOp =
                SceneManager.LoadSceneAsync(
                    m_SceneLoadInfo.SceneIndex,
                    m_SceneLoadInfo.SceneLoadParameters
                ) ?? throw new LoadFailedException("Failed to start loading scene by index");

            await asyncOp;
        }

        private async UniTask LoadSceneFromEditorPath()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(m_SceneLoadInfo.EditorPath))
                throw new LoadFailedException("Editor path is null or empty");

            if (!System.IO.File.Exists(m_SceneLoadInfo.EditorPath))
            {
                throw new LoadFailedException(
                    $"Scene file not found at path: {m_SceneLoadInfo.EditorPath}"
                );
            }

            //TODO(vanish)
            await UniTask.Yield();
#else
            throw new LoadFailedException("Editor path loading is only supported in Unity Editor");
#endif
        }

        // TODO(vanish): Test
        private async UniTask LoadSceneFromStreamingAssetPath()
        {
            if (string.IsNullOrEmpty(m_SceneLoadInfo.StreamingAssetPath))
                throw new LoadFailedException("Streaming asset path is null or empty");
            try
            {
                var addressableKey = m_SceneLoadInfo.StreamingAssetPath;

                var sceneHandle = Addressables.LoadSceneAsync(
                    addressableKey,
                    m_SceneLoadInfo.SceneLoadParameters.loadSceneMode,
                    m_SceneLoadInfo.SceneLoadParameters.localPhysicsMode != LocalPhysicsMode.None
                );

                var sceneInstance = await sceneHandle.ToUniTask();

                if (!sceneInstance.Scene.isLoaded)
                {
                    throw new LoadFailedException(
                        $"Scene failed to load from Addressables: {addressableKey}"
                    );
                }

                m_AddressableSceneHandle = sceneHandle;
            }
            catch (InvalidKeyException ex)
            {
                throw new LoadFailedException(
                    $"Invalid Addressable key for scene: {m_SceneLoadInfo.StreamingAssetPath}",
                    ex
                );
            }
            catch (Exception ex)
            {
                throw new LoadFailedException(
                    $"Failed to load scene from Addressables: {m_SceneLoadInfo.StreamingAssetPath}",
                    ex
                );
            }
        }

        public async UniTask UnloadAddressableScene()
        {
            if (m_AddressableSceneHandle.IsValid())
            {
                await Addressables.UnloadSceneAsync(m_AddressableSceneHandle).ToUniTask();
                m_AddressableSceneHandle = default;
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

        private SceneLoadInfo m_SceneLoadInfo;

        private AsyncOperationHandle<SceneInstance> m_AddressableSceneHandle;
    }
}
