using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public interface ILoadInfo
    {
        public LoaderType GetNeededLoaderType();
    }

    public class SceneLoadInfo : ILoadInfo
    {
        public SceneLoadInfo(string sceneName)
        {
            SceneName = sceneName;
        }

        public LoaderType GetNeededLoaderType()
        {
            return LoaderType.SceneLoader;
        }

        public string SceneName { get; private set; }
    }
}
