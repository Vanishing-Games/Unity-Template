Shader "Hidden/RainRust/JfaSeedInit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "JFAInit"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #include "Utils.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            // =======================================================================

            // AddBlitPass 自动绑定
            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            // =======================================================================

            FragInput Vert(uint vertexID : SV_VertexID)
            {
                FragInput o;
            
                // 生成全屏三角形 UV
                float2 uv = float2(
                    (vertexID << 1) & 2,
                    vertexID & 2
                );
                
                o.uv = uv;
                o.vertex = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                
#if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1.0 - o.uv.y;
#endif
                
                return o;
            }

            float4 Frag(const FragInput i) : SV_Target
            {
                // 采样 Alpha 通道作为种子标记
                float alpha = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv).a;

                // 使用 Z 通道作为种子有效性标记 (1.0 表示是种子, 0.0 表示不是)
                if (alpha > 0)
                {
                    return float4(i.uv, 1, 1);
                }

                return float4(0, 0, 0, 0);
            }

            ENDHLSL
        }
    }
}
