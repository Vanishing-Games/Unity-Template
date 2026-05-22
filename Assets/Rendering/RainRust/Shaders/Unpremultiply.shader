Shader "Hidden/RainRust/Unpremultiply"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unpremultiply"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #include "Utils.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            // AddBlitPass 自动绑定
            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            FragInput Vert(uint vertexID : SV_VertexID)
            {
                FragInput o;
                float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
                o.uv = uv;
                o.vertex = float4(uv * 2.0 - 1.0, 0.0, 1.0);
#if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1.0 - o.uv.y;
#endif
                return o;
            }

            float4 Frag(const FragInput i) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv);
                if (c.a > 1e-4)
                    return float4(c.rgb / c.a, c.a);
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
