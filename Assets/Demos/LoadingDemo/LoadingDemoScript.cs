using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class LoadingDemoScript : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var loadEvent = new LoadRequestEvent("Testing Demo");
            loadEvent.AddLoadInfo(new SceneLoadInfo("LoadingDemo"));

            var command = new LoadRequestCommand(loadEvent);
            command.Execute();
        }
    }
}
