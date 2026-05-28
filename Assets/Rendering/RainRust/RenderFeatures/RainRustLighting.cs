using System;
using Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RainRust.Rendering
{
    public class RainRustLighting : ScriptableRendererFeature
    {
        public enum BlendMode
        {
            Additive,
            AlphaBlend,
            Multiply,
            Screen,
            Overlay,
        }

        [Serializable]
        public class RainRustLightingSettings
        {
            // csharpier-ignore-start
            public RenderPassEvent injectionPoint  = RenderPassEvent.BeforeRenderingOpaques;
            public LayerMask lightSourcesLayerMask = -1;
            public LayerMask receiversLayerMask    = -1;

            [Header("Composition Settings")]
            public BlendMode receiverBlendMode = BlendMode.AlphaBlend;
            public BlendMode lightingBlendMode = BlendMode.Additive;

            [Header("Shaders")]
            public Shader jfaInitShader       ;
            public Shader jfaShader           ;
            public Shader distanceShader      ;
            public Shader unpremultiplyShader ;
            public Shader rayTracingShader    ;
            public Shader compositionShader   ;
            public Shader blitShader          ;
            // csharpier-ignore-end
        }

        /** Called when
    * - When the Scriptable Renderer Feature loads the first time.
    * - When you enable or disable the Scriptable Renderer Feature.
    * - When you change a property in the Inspector window of the Renderer Feature.
    */
        public override void Create()
        {
            // csharpier-ignore-start
            m_RainRustDrawObjectsPass = new RainRustDrawObjectsPass(); // 绘制所有光源, 用于后续光场计算
            m_UnpremultiplyPass       = new RainRustUnpremultiplyPass(); // 把 mainRt 从 premultiplied 转为 straight alpha
            m_JfaInitPass             = new RainRustJfaInitPass(); // 从光源数据获得JFA初始种子
            m_JfaPass                 = new RainRustJfaPass(); // Jump Flood Algorithm
            m_DistancePass            = new RainRustDistancePass(); // JFA -> Distance Field
            m_RainRustRayTracingPass  = new RainRustRayTracingPass(); // 通过光场与光源颜色进行屏幕空间Ray Tracing, 获得光照信息
            m_RainRustRenderingPass   = new RainRustRenderingPass(); // 渲染最终结果
            // csharpier-ignore-end
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData
        )
        {
            if (renderingData.cameraData.isPreviewCamera)
                return;

            if (settings == null)
            {
                CLogger.LogError(
                    "[RainRust] Settings are null in AddRenderPasses!",
                    LogTag.Rendering
                );
                return;
            }

            // 检查关键 Shader 是否丢失
            if (settings.jfaInitShader == null)
                CLogger.LogError("[RainRust] JFA Init Shader is MISSING!", LogTag.Rendering);
            if (settings.jfaShader == null)
                CLogger.LogError("[RainRust] JFA Shader is MISSING!", LogTag.Rendering);
            if (settings.distanceShader == null)
                CLogger.LogError("[RainRust] Distance Shader is MISSING!", LogTag.Rendering);
            if (settings.unpremultiplyShader == null)
                CLogger.LogError("[RainRust] Unpremultiply Shader is MISSING!", LogTag.Rendering);
            if (settings.rayTracingShader == null)
                CLogger.LogError("[RainRust] RayTracing Shader is MISSING!", LogTag.Rendering);
            if (settings.compositionShader == null)
                CLogger.LogError("[RainRust] Composition Shader is MISSING!", LogTag.Rendering);
            if (settings.blitShader == null)
                CLogger.LogError("[RainRust] Blit Shader is MISSING!", LogTag.Rendering);
            // csharpier-ignore-start
            // Apply settings to all passes
            m_RainRustDrawObjectsPass.renderPassEvent = settings.injectionPoint;
            m_UnpremultiplyPass.renderPassEvent       = settings.injectionPoint;
            m_JfaInitPass.renderPassEvent             = settings.injectionPoint;
            m_JfaPass.renderPassEvent                 = settings.injectionPoint;
            m_DistancePass.renderPassEvent            = settings.injectionPoint;
            m_RainRustRayTracingPass.renderPassEvent  = settings.injectionPoint;
            m_RainRustRenderingPass.renderPassEvent   = settings.injectionPoint;

            m_RainRustDrawObjectsPass.Setup(settings);
            m_UnpremultiplyPass      .Setup(settings);
            m_JfaInitPass            .Setup(settings);
            m_JfaPass                .Setup(settings);
            m_DistancePass           .Setup(settings);
            m_RainRustRayTracingPass .Setup(settings);
            m_RainRustRenderingPass  .Setup(settings);
            // csharpier-ignore-end

            renderer.EnqueuePass(m_RainRustDrawObjectsPass);
            renderer.EnqueuePass(m_UnpremultiplyPass);
            renderer.EnqueuePass(m_JfaInitPass);
            renderer.EnqueuePass(m_JfaPass);
            renderer.EnqueuePass(m_DistancePass);
            renderer.EnqueuePass(m_RainRustRayTracingPass);
            renderer.EnqueuePass(m_RainRustRenderingPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_JfaInitPass?.Dispose();
            m_JfaPass?.Dispose();
            m_UnpremultiplyPass?.Dispose();
            m_RainRustRenderingPass?.Dispose();
        }

        public RainRustLightingSettings settings = new();

        private void OnValidate()
        {
            if (settings == null)
                return;

            // 自动寻找 Shader, 避免用户手动寻找 Hidden Shader
            if (settings.jfaInitShader == null)
                settings.jfaInitShader = Shader.Find("Hidden/RainRust/JfaSeedInit");
            if (settings.jfaShader == null)
                settings.jfaShader = Shader.Find("Hidden/RainRust/JumpFloodAlgorithm");
            if (settings.distanceShader == null)
                settings.distanceShader = Shader.Find("Hidden/RainRust/Distance");
            if (settings.unpremultiplyShader == null)
                settings.unpremultiplyShader = Shader.Find("Hidden/RainRust/Unpremultiply");
            if (settings.rayTracingShader == null)
                settings.rayTracingShader = Shader.Find("Hidden/RainRust/RayTracing");
            if (settings.compositionShader == null)
                settings.compositionShader = Shader.Find("Hidden/RainRust/Composition");
            if (settings.blitShader == null)
                settings.blitShader = Shader.Find("Hidden/Universal Render Pipeline/Blit");
        }

        // csharpier-ignore-start

        private RainRustDrawObjectsPass   m_RainRustDrawObjectsPass;
        private RainRustUnpremultiplyPass m_UnpremultiplyPass;
        private RainRustJfaInitPass       m_JfaInitPass;
        private RainRustJfaPass           m_JfaPass;
        private RainRustDistancePass      m_DistancePass;
        private RainRustRayTracingPass    m_RainRustRayTracingPass;
        private RainRustRenderingPass     m_RainRustRenderingPass;
        // csharpier-ignore-end
    }
}
