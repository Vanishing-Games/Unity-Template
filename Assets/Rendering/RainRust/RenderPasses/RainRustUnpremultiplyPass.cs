using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace RainRust.Rendering
{
    public class RainRustUnpremultiplyPass : ScriptableRenderPass
    {
        public RainRustUnpremultiplyPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public void Setup(RainRustLighting.RainRustLightingSettings settings)
        {
            m_Settings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var rainRustData = frameData.Get<RainRustContextData>();
            if (rainRustData == null || m_Settings == null)
                return;

            if (m_UnpremultiplyMaterial == null && m_Settings.unpremultiplyShader != null)
            {
                m_UnpremultiplyMaterial = CoreUtils.CreateEngineMaterial(
                    m_Settings.unpremultiplyShader
                );
            }

            if (m_UnpremultiplyMaterial == null)
                return;

            if (!rainRustData.mainRt.IsValid())
                return;

            var desc = renderGraph.GetTextureDesc(rainRustData.mainRt);
            desc.name = "RainRust Main Texture (Straight Alpha)";
            desc.clearBuffer = true;

            TextureHandle straightRt = renderGraph.CreateTexture(desc);

            renderGraph.AddBlitPass(
                new RenderGraphUtils.BlitMaterialParameters(
                    rainRustData.mainRt,
                    straightRt,
                    m_UnpremultiplyMaterial,
                    0
                ),
                "RainRust Unpremultiply"
            );

            rainRustData.mainRt = straightRt;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_UnpremultiplyMaterial);
        }

        private Material m_UnpremultiplyMaterial;
        private RainRustLighting.RainRustLightingSettings m_Settings;
    }
}
