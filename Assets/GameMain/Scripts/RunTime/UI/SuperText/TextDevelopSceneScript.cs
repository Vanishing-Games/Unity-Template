using UnityEngine;

namespace GameMain.RunTime
{
    public class TextDevelopSceneScript : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var renderers = FindObjectsByType<EffectTextRenderer>(FindObjectsSortMode.None);
                foreach (var renderer in renderers)
                {
                    renderer.SkipToEnd();
                }
            }
        }
    }
}
