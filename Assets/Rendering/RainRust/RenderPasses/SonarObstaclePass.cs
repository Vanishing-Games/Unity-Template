using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RainRust.Rendering
{
    public class SonarObstaclePass : ScriptableRenderPass
    {
        private LayerMask m_ObstacleLayerMask;
        private Material m_ObstacleMaterial;
        private static readonly int s_SonarObstacleTexId = Shader.PropertyToID("_SonarObstacleTex");

        public SonarObstaclePass(LayerMask layerMask, Material material)
        {
            m_ObstacleLayerMask = layerMask;
            m_ObstacleMaterial = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        class PassData
        {
            public RendererListHandle obstacleRendererList;
            public TextureHandle obstacleMask;
        }

        class GlobalSetData
        {
            public TextureHandle obstacleMask;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            if (cameraData.isPreviewCamera)
                return;

            // 1. Create Descriptor for the mask
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.R8; // 只需要单通道
            desc.msaaSamples = 1;

            TextureHandle obstacleMask = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                desc,
                "_SonarObstacleTex",
                true
            );

            // 2. Setup the Drawing Pass
            using (
                var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "Sonar Obstacle Mask Pass",
                    out var passData
                )
            )
            {
                passData.obstacleMask = obstacleMask;
                builder.SetRenderAttachment(obstacleMask, 0, AccessFlags.Write);

                // Configure Renderer List
                SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
                FilteringSettings filteringSettings = new FilteringSettings(
                    RenderQueueRange.opaque,
                    m_ObstacleLayerMask
                );

                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    new ShaderTagId("UniversalForward"),
                    renderingData,
                    cameraData,
                    lightData,
                    sortingCriteria
                );
                drawingSettings.overrideMaterial = m_ObstacleMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;

                passData.obstacleRendererList = renderGraph.CreateRendererList(
                    new RendererListParams(
                        renderingData.cullResults,
                        drawingSettings,
                        filteringSettings
                    )
                );
                builder.UseRendererList(passData.obstacleRendererList);

                builder.SetRenderFunc(
                    static (PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1.0f, 0);
                        context.cmd.DrawRendererList(data.obstacleRendererList);
                    }
                );
            }

            // 3. Setup a separate Pass to set the Global Texture
            // 这样可以避免“同一 Pass 中纹理既是 Attachment 又是 Texture”的冲突
            using (
                var builder = renderGraph.AddRasterRenderPass<GlobalSetData>(
                    "Sonar Global Set Pass",
                    out var passData
                )
            )
            {
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                passData.obstacleMask = obstacleMask;
                // 声明对此纹理的读取访问
                builder.UseTexture(obstacleMask, AccessFlags.Read);

                builder.SetRenderFunc(
                    static (GlobalSetData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(s_SonarObstacleTexId, data.obstacleMask);
                    }
                );
            }
        }
    }
}
