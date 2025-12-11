using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PipelineSetter : MonoBehaviour
{
    public RenderPipelineAsset mRenderPipelineAsset;

    void OnEnable()
    {
        GraphicsSettings.defaultRenderPipeline = mRenderPipelineAsset;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        GraphicsSettings.defaultRenderPipeline = mRenderPipelineAsset;
    }
#endif
}
