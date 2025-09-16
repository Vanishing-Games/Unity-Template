using System;

namespace Core
{
    /// <summary>
    /// Vg aka. Vanishing Games
    /// </summary>
    public class VgSceneManager : CoreModuleManagerBase<VgSceneManager>
    {
        protected override void CreateLoader(ILoadInfo loadInfo)
        {
            if (loadInfo is SceneLoadInfo sceneLoadInfo)
            {
                var loader = GetComponent<SceneLoader>();
                if (loader != null)
                {
                    Destroy(loader);
                }

                loader = gameObject.AddComponent<SceneLoader>();
                loader.InitLoader(sceneLoadInfo);
                loader.SendLoader();
            }
            else
            {
                MessageBroker.Global.PublishErrorResume<LoadRequestEvent>(
                    this,
                    new LoadFailedException("LoadInfo is not a SceneLoadInfo")
                );
            }
        }

        protected override void OnReceiveLoadRequest(LoadRequestEvent loadEventInfo)
        {
            Logger.EditorLogVerbose(
                "[SceneLoadManager] SceneLoadManager ReceiveLoadInfo",
                LogTag.SceneLoader
            );

            var info = loadEventInfo.GetLoadInfo(LoaderType.SceneLoader);

            if (info != null)
            {
                CreateLoader(info);
            }
        }

        protected override void OnLoadingError(Exception exception) { }
    }
}
