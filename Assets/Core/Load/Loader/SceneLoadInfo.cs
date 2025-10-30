using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    [System.Serializable]
    public class SceneLoadInfo : ILoadInfo
    {
        public SceneLoadInfo(string sceneName, LoadSceneParameters sceneLoadParameters = default)
        {
            SceneAssetMode = SceneAssetMode.FromBuildingSceneName;
            SceneName = sceneName;
            SceneIndex = -1;
            EditorPath = string.Empty;
            StreamingAssetPath = string.Empty;
            SceneLoadParameters = sceneLoadParameters.Equals(default(LoadSceneParameters))
                ? new LoadSceneParameters(LoadSceneMode.Single)
                : sceneLoadParameters;
        }

        public SceneLoadInfo(int sceneIndex, LoadSceneParameters sceneLoadParameters = default)
        {
            SceneAssetMode = SceneAssetMode.FromBuildingSceneIndex;
            SceneName = string.Empty;
            SceneIndex = sceneIndex;
            EditorPath = string.Empty;
            StreamingAssetPath = string.Empty;
            SceneLoadParameters = sceneLoadParameters.Equals(default(LoadSceneParameters))
                ? new LoadSceneParameters(LoadSceneMode.Single)
                : sceneLoadParameters;
        }

        public static SceneLoadInfo FromEditorPath(
            string editorPath,
            LoadSceneParameters sceneLoadParameters = default
        )
        {
            return new SceneLoadInfo
            {
                SceneAssetMode = SceneAssetMode.FromEditorPath,
                SceneName = System.IO.Path.GetFileNameWithoutExtension(editorPath),
                SceneIndex = -1,
                EditorPath = editorPath,
                StreamingAssetPath = string.Empty,
                SceneLoadParameters = sceneLoadParameters.Equals(default(LoadSceneParameters))
                    ? new LoadSceneParameters(LoadSceneMode.Single)
                    : sceneLoadParameters,
            };
        }

        public static SceneLoadInfo FromStreamingAssetPath(
            string streamingAssetPath,
            LoadSceneParameters sceneLoadParameters = default
        )
        {
            return new SceneLoadInfo
            {
                SceneAssetMode = SceneAssetMode.FromStreamingAssetsPath,
                SceneName = System.IO.Path.GetFileNameWithoutExtension(streamingAssetPath),
                SceneIndex = -1,
                EditorPath = string.Empty,
                StreamingAssetPath = streamingAssetPath,
                SceneLoadParameters = sceneLoadParameters.Equals(default(LoadSceneParameters))
                    ? new LoadSceneParameters(LoadSceneMode.Single)
                    : sceneLoadParameters,
            };
        }

        // Private constructor for factory methods
        private SceneLoadInfo() { }

        public LoaderType GetNeededLoaderType()
        {
            return LoaderType.SceneLoader;
        }

        public string SceneName { get; private set; }
        public int SceneIndex { get; private set; }
        public string EditorPath { get; private set; }
        public string StreamingAssetPath { get; private set; }
        public SceneAssetMode SceneAssetMode { get; private set; }
        public LoadSceneParameters SceneLoadParameters { get; private set; }
    }
}
