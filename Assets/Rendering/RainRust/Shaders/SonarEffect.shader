Shader "RainRust/SonarEffect"
{
    Properties
    {
        [HDR] _Color("Wave Color", Color) = (0, 1, 0.5, 1)
        _Radius("Current Radius", Float) = 0.5
        _Thickness("Thickness", Float) = 0.05
        _Hardness("Hardness", Range(0, 1)) = 0.9
        _RaymarchSteps("Raymarch Steps", Int) = 16
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Blend One One // Additive blend for light-like effect
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Radius;
                float _Thickness;
                float _Hardness;
                int _RaymarchSteps;
                float _OcclusionStrength;
                float2 _SonarCenterScreenUV;
            CBUFFER_END

            TEXTURE2D(_SonarObstacleTex);
            SAMPLER(sampler_SonarObstacleTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 1. 基本圆环计算 (基于物体 UV, 假设模型中心在 0.5, 0.5)
                float2 center = float2(0.5, 0.5);
                float dist = distance(input.uv, center);
                
                // 构造圆环 mask
                float ring = smoothstep(_Radius + _Thickness, _Radius + _Thickness * _Hardness, dist) * 
                             smoothstep(_Radius - _Thickness, _Radius - _Thickness * _Hardness, dist);
                
                if (ring <= 0) discard;

                // 2. 射线步进检测阻挡 (基于屏幕 UV)
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 sonarCenterUV = _SonarCenterScreenUV;
                
                // 射线方向：从当前像素指向中心
                float occlusion = 0;
                float2 rayDir = (sonarCenterUV - screenUV);
                
                // [loop] 强制编译器不要展开循环，解决 "unable to unroll loop" 报错
                [loop]
                for(int s = 1; s <= _RaymarchSteps; s++)
                {
                    float t = (float)s / (float)_RaymarchSteps;
                    float2 sampleUV = screenUV + rayDir * t;
                    
                    // 使用 _LOD 采样，避免在循环内部计算导数 (Gradients)
                    float mask = SAMPLE_TEXTURE2D_LOD(_SonarObstacleTex, sampler_SonarObstacleTex, sampleUV, 0).r;
                    if(mask > 0.5) 
                    {
                        occlusion = 1.0;
                        break;
                    }
                }

                float finalAlpha = ring * (1.0 - occlusion * _OcclusionStrength);
                return _Color * finalAlpha;
            }
            ENDHLSL
        }
    }
}
