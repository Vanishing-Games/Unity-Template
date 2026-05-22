using Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

namespace RainRust.Rendering
{
    public class RainRustRenderingPass : ScriptableRenderPass
    {
        private Material m_RenderingMaterial;
        private Material m_BlitMaterial;
        private RainRustLighting.RainRustLightingSettings m_Settings;

        public RainRustRenderingPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public void Setup(RainRustLighting.RainRustLightingSettings settings)
        {
            m_Settings = settings;
        }

        class RainRustRenderingPassData
        {
            public Material material;
            public TextureHandle lightingRt;
            public TextureHandle receiverRt;
            public TextureHandle receiverDepthRt;
            public TextureHandle cameraColor;
            public RainRustLighting.BlendMode receiverBlendMode;
            public RainRustLighting.BlendMode lightingBlendMode;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Settings == null)
            {
                CLogger.LogError("[RainRust] RenderingPass: m_Settings is null!", LogTag.Rendering);
                return;
            }

            // Ensure material is created
            if (m_RenderingMaterial == null && m_Settings.compositionShader != null)
            {
                m_RenderingMaterial = CoreUtils.CreateEngineMaterial(m_Settings.compositionShader);
                if (m_RenderingMaterial == null)
                    CLogger.LogError(
                        "[RainRust] RenderingPass: Failed to create Composition Material!",
                        LogTag.Rendering
                    );
            }

            if (m_RenderingMaterial == null)
            {
                if (m_Settings.compositionShader == null)
                    CLogger.LogError(
                        "[RainRust] RenderingPass: Composition Shader is null!",
                        LogTag.Rendering
                    );
                return;
            }

            var rainRustContextData = frameData.Get<RainRustContextData>();
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.isPreviewCamera)
                return;

            var cameraColor = resourceData.cameraColor;
            if (!cameraColor.IsValid())
                return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;

            TextureHandle tempColor = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                desc,
                "RainRust Composition Temp",
                false
            );

            using (
                var builder = renderGraph.AddRasterRenderPass<RainRustRenderingPassData>(
                    "Rain Rust Rendering Pass",
                    out var passData
                )
            )
            {
                passData.material = m_RenderingMaterial;
                passData.lightingRt = rainRustContextData.lightingRt;
                passData.receiverRt = rainRustContextData.receiverRt;
                passData.receiverDepthRt = rainRustContextData.mainDepthRt;
                passData.cameraColor = cameraColor;
                passData.receiverBlendMode = m_Settings.receiverBlendMode;
                passData.lightingBlendMode = m_Settings.lightingBlendMode;

                builder.UseTexture(passData.lightingRt, AccessFlags.Read);
                builder.UseTexture(passData.receiverRt, AccessFlags.Read);
                builder.UseTexture(passData.receiverDepthRt, AccessFlags.Read);
                builder.UseTexture(passData.cameraColor, AccessFlags.Read);
                builder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);

                builder.SetRenderFunc(
                    static (RainRustRenderingPassData data, RasterGraphContext context) =>
                    {
                        var cmd = context.cmd;

                        data.material.SetTexture("_MainTex", data.cameraColor);
                        data.material.SetTexture("_LightingTex", data.lightingRt);
                        data.material.SetTexture("_ReceiverTex", data.receiverRt);
                        data.material.SetTexture("_ReceiverDepthTex", data.receiverDepthRt);

                        // Debug override: 在任一 RT debug 模式下, 让 composition 直接输出 lightingRt
                        var debugStack = VolumeManager.instance.stack.GetComponent<RainRustVolume>();
                        if (debugStack != null && debugStack.debugMode.value != RainRustDebugMode.None)
                            data.material.EnableKeyword("RAINRUST_DEBUG_OVERRIDE");
                        else
                            data.material.DisableKeyword("RAINRUST_DEBUG_OVERRIDE");

                        // Clear keywords
                        data.material.DisableKeyword("RECEIVER_BLEND_ADDITIVE");
                        data.material.DisableKeyword("RECEIVER_BLEND_ALPHABLEND");
                        data.material.DisableKeyword("RECEIVER_BLEND_MULTIPLY");
                        data.material.DisableKeyword("RECEIVER_BLEND_SCREEN");
                        data.material.DisableKeyword("RECEIVER_BLEND_OVERLAY");

                        data.material.DisableKeyword("LIGHTING_BLEND_ADDITIVE");
                        data.material.DisableKeyword("LIGHTING_BLEND_ALPHABLEND");
                        data.material.DisableKeyword("LIGHTING_BLEND_MULTIPLY");
                        data.material.DisableKeyword("LIGHTING_BLEND_SCREEN");
                        data.material.DisableKeyword("LIGHTING_BLEND_OVERLAY");

                        // Set receiver blend keywords
                        switch (data.receiverBlendMode)
                        {
                            case RainRustLighting.BlendMode.Additive:
                                data.material.EnableKeyword("RECEIVER_BLEND_ADDITIVE");
                                break;
                            case RainRustLighting.BlendMode.AlphaBlend:
                                data.material.EnableKeyword("RECEIVER_BLEND_ALPHABLEND");
                                break;
                            case RainRustLighting.BlendMode.Multiply:
                                data.material.EnableKeyword("RECEIVER_BLEND_MULTIPLY");
                                break;
                            case RainRustLighting.BlendMode.Screen:
                                data.material.EnableKeyword("RECEIVER_BLEND_SCREEN");
                                break;
                            case RainRustLighting.BlendMode.Overlay:
                                data.material.EnableKeyword("RECEIVER_BLEND_OVERLAY");
                                break;
                        }

                        // Set lighting blend keywords
                        switch (data.lightingBlendMode)
                        {
                            case RainRustLighting.BlendMode.Additive:
                                data.material.EnableKeyword("LIGHTING_BLEND_ADDITIVE");
                                break;
                            case RainRustLighting.BlendMode.AlphaBlend:
                                data.material.EnableKeyword("LIGHTING_BLEND_ALPHABLEND");
                                break;
                            case RainRustLighting.BlendMode.Multiply:
                                data.material.EnableKeyword("LIGHTING_BLEND_MULTIPLY");
                                break;
                            case RainRustLighting.BlendMode.Screen:
                                data.material.EnableKeyword("LIGHTING_BLEND_SCREEN");
                                break;
                            case RainRustLighting.BlendMode.Overlay:
                                data.material.EnableKeyword("LIGHTING_BLEND_OVERLAY");
                                break;
                        }

                        // Use a full-screen draw
                        cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            0,
                            MeshTopology.Triangles,
                            3
                        );
                    }
                );
            }

            // Ensure blit material is available
            if (m_BlitMaterial == null && m_Settings.blitShader != null)
            {
                m_BlitMaterial = CoreUtils.CreateEngineMaterial(m_Settings.blitShader);
                if (m_BlitMaterial == null)
                    CLogger.LogError(
                        "[RainRust] RenderingPass: Failed to create Blit Material!",
                        LogTag.Rendering
                    );
            }

            // Blit temp back to cameraColor
            if (m_BlitMaterial != null)
            {
                renderGraph.AddBlitPass(
                    new BlitMaterialParameters(tempColor, cameraColor, m_BlitMaterial, 0),
                    "Rain Rust Blit Back"
                );
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_RenderingMaterial);
            CoreUtils.Destroy(m_BlitMaterial);
        }
    }
}
