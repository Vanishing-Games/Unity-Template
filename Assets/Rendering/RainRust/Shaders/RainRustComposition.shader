Shader "Hidden/RainRust/Composition"
{
    Properties
    {
        _MainTex("Background", 2D) = "white" {}
        _LightingTex("Lighting", 2D) = "white" {}
        _ReceiverTex("Receiver", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "RainRustComposition"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ RECEIVER_BLEND_ADDITIVE RECEIVER_BLEND_ALPHABLEND RECEIVER_BLEND_MULTIPLY RECEIVER_BLEND_SCREEN RECEIVER_BLEND_OVERLAY
            #pragma multi_compile _ LIGHTING_BLEND_ADDITIVE LIGHTING_BLEND_ALPHABLEND LIGHTING_BLEND_MULTIPLY LIGHTING_BLEND_SCREEN LIGHTING_BLEND_OVERLAY
            #pragma multi_compile _ RAINRUST_DEBUG_OVERRIDE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_LightingTex);
            SAMPLER(sampler_LightingTex);
            TEXTURE2D(_ReceiverTex);
            SAMPLER(sampler_ReceiverTex);
            TEXTURE2D(_ReceiverDepthTex);
            SAMPLER(sampler_ReceiverDepthTex);

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.uv = float2((vertexID << 1) & 2, vertexID & 2);
                output.positionCS = float4(output.uv * 2.0 - 1.0, 0.0, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1.0 - output.uv.y;
                #endif

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 background = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 lighting = SAMPLE_TEXTURE2D(_LightingTex, sampler_LightingTex, input.uv);

                // Debug 模式: 直接显示 RT (绕过 receiver mask 和 blend mode)
                #if defined(RAINRUST_DEBUG_OVERRIDE)
                return float4(lighting.rgb, 1);
                #endif

                float4 receiver = SAMPLE_TEXTURE2D(_ReceiverTex, sampler_ReceiverTex, input.uv);
                float depth = SAMPLE_TEXTURE2D(_ReceiverDepthTex, sampler_ReceiverDepthTex, input.uv).r;

                // Check for record (depth != far plane OR alpha > 0)
                // In reverse-Z, far is 0.
                #if UNITY_REVERSED_Z
                bool hasRecord = depth > 0.0001;
                #else
                bool hasRecord = depth < 0.9999;
                #endif
                
                // Fallback to alpha if depth is not available or ZWrite was off
                hasRecord = hasRecord || (receiver.a > 0.01);

                float3 finalColor;

                if (hasRecord)
                {
                    // 直接使用 Receiver 的结果 (与背景进行 Alpha 混合以保证透明物体正确渲染)
                    finalColor = lerp(background.rgb, receiver.rgb, receiver.a);
                }
                else
                {
                    // 没有记录的地方: 混合光照结果和 main 的结果
                    #if defined(LIGHTING_BLEND_ADDITIVE)
                    finalColor = background.rgb + lighting.rgb;
                    #elif defined(LIGHTING_BLEND_ALPHABLEND)
                    finalColor = lerp(background.rgb, lighting.rgb, lighting.a);
                    #elif defined(LIGHTING_BLEND_MULTIPLY)
                    finalColor = background.rgb * lighting.rgb;
                    #elif defined(LIGHTING_BLEND_SCREEN)
                    finalColor = 1.0 - (1.0 - background.rgb) * (1.0 - lighting.rgb);
                    #elif defined(LIGHTING_BLEND_OVERLAY)
                    finalColor = (background.rgb < 0.5) ? (2.0 * background.rgb * lighting.rgb) : (1.0 - 2.0 * (1.0 - background.rgb) * (1.0 - lighting.rgb));
                    #else
                    finalColor = background.rgb + lighting.rgb;
                    #endif
                }

                return float4(finalColor, background.a);
            }
            ENDHLSL
        }
    }
}
