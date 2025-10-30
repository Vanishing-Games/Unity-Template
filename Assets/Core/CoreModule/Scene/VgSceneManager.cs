using System;

namespace Core
{
    /// <summary>
    /// Vg aka. Vanishing Games
    /// </summary>
    public class VgSceneManager : CoreModuleManagerBase<VgSceneManager, SceneLoadInfo, SceneLoader>
    {
        protected override LoaderType GetLoaderType() => LoaderType.SceneLoader;

        protected override void OnLoadingError(Exception exception) { }
    }
}
