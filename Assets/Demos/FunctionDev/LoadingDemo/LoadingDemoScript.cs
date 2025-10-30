using System.Collections;
using System.Collections.Generic;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class LoadingDemoScript : MonoBehaviour
{
    [ShowInInspector]
    public SceneAssetMode assetMode;

    [ShowInInspector]
    public string sceneName;

    [ShowInInspector]
    public int sceneIndex;

    [ShowInInspector]
    public string editorPath;

    [ShowInInspector]
    public string streamingAssetPath;

    [ShowInInspector]
    public LoadSceneParameters sceneLoadParameters;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var loadEvent = new LoadRequestEvent("Testing Demo");

            switch (assetMode)
            {
                case SceneAssetMode.FromBuildingSceneName:
                    loadEvent.AddLoadInfo(new SceneLoadInfo(sceneName, sceneLoadParameters));
                    break;
                case SceneAssetMode.FromBuildingSceneIndex:
                    loadEvent.AddLoadInfo(new SceneLoadInfo(sceneIndex, sceneLoadParameters));
                    break;
                case SceneAssetMode.FromEditorPath:
                    loadEvent.AddLoadInfo(SceneLoadInfo.FromEditorPath(editorPath, sceneLoadParameters));
                    break;
                case SceneAssetMode.FromStreamingAssetsPath:
                    loadEvent.AddLoadInfo(
                        SceneLoadInfo.FromStreamingAssetPath(streamingAssetPath, sceneLoadParameters)
                    );
                    break;
            }

            var command = new LoadRequestCommand(loadEvent);
            command.Execute();
        }
    }
}
