using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public interface ILoadInfo
    {
        public LoaderType GetNeededLoaderType();
    }

    public enum SceneAssetMode
    {
        FromBuildingSceneName, // Get Scene Asset from Building Scene Name
        FromBuildingSceneIndex, // Get Scene Asset from Building Scene Index
        FromEditorPath, // Get Scene Asset from Editor Path,like Assets/GameMain/Scenes/SampleScene.unity
        FromStreamingAssetsPath, // Get Scene Asset from Streaming Assets Path,like Scenes/SampleScene.unity
    }
}
