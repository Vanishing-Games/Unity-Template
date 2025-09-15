namespace Core
{
    public class SceneLoadManager : SystemMonoModule<SceneLoadManager>
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
                MessageBroker.Global.PublishError<LoadRequestEvent>(
                    new LoadFailedException("LoadInfo is not a SceneLoadInfo")
                );
            }
        }

        public override void ReceiveLoadInfo(LoadRequestEvent loadEventInfo)
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
    }
}
