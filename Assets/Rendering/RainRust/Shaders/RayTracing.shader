Shader "Hidden/RainRust/RayTracing"
{
    Properties 
    { 
	    SrcMode ("SrcMode", Float) = 1
	    DstMode ("DstMode", Float) = 0
    } 
    SubShader
    {
        Cull Off // 不剔除
        ZWrite Off // 不写入深度
        ZTest Off // 不进行深度测试

        Pass    // 0
        {
            Name "RayTracing"
	        Blend [SrcMode] [DstMode]

            HLSLPROGRAM
            #include "Utils.hlsl"
            #include "Noise.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // =======================================================================
            
            // 随机数生成方式: shader内生成随机数, 从纹理采样随机数, 无随机
            #pragma multi_compile_local FRAGMENT_RANDOM TEXTURE_RANDOM _
            // 输出alpha通道方式: 全部为1, 使用对象遮罩, 颜色归一化后最大值
            #pragma multi_compile_local ONE_ALPHA OBJECTS_MASK_ALPHA NORMALIZED_ALPHA
            // Debug 可视化模式 (_ 表示关闭, 即正常渲染)
            #pragma multi_compile_local _ DEBUG_RAND DEBUG_SAMPLEDIR DEBUG_COLORALPHA DEBUG_EARLYEXIT DEBUG_RAYTERMSTEP DEBUG_HITFRACTION DEBUG_SDF DEBUG_PIXELINSPECTOR

            #pragma vertex Vert
            #pragma fragment Frag

            sampler2D _ColorTex; // 场景颜色纹理
            sampler2D _DistTex; // 场景距离纹理 (SDF)
            sampler2D _NoiseTex; // 随机噪声纹理

            float2 _Aspect; // 16:9 为 (1, 0.5625)
            float4 _NoiseTilingOffset; // 噪声纹理的缩放和偏移 (tiling.x, tiling.y, offset.x, offset.y)
            
            float  _Samples; // 光线采样数
            float _Intensity; // 光照强度
            float _LightFalloffAlpha; // GTR 衰减 alpha 参数
            float _LightFalloffGamma; // GTR 衰减 gamma 参数
            float3 _AmbientColor; // 环境光颜色

            // 噪声相关参数
            float _NoiseScale; // 噪声缩放
            float _NoiseIntensity; // 噪声强度
            float2 _NoiseVelocity; // 噪声移动速度
            int _NoiseType; // 噪声类型: 0-Value, 1-Perlin, 2-Simplex, 3-Voronoi

            // Pixel Inspector 模式: 要检查的像素 UV
            float2 _DebugPixelUV;

            // =======================================================================

            struct FragInputGI
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
#if defined(FRAGMENT_RANDOM) || defined(TEXTURE_RANDOM)
                float2 noise_uv : TEXCOORD1;
#endif
            };

            // =======================================================================

            float GTRAttenuation(float2 dist, float alpha, float gamma)
            {
                float x = length(dist);
                return 1.0 / pow(1.0 + pow(x / alpha, 2.0), gamma);
            }

            float3 Trace(const float2 uv, const float2 dir) // Ray Marching
            {
                float2 uvPos = uv; // 当前采样坐标

                // 若起始点已在光源上, 直接返回颜色
                const float4 color = tex2D(_ColorTex, uv).rgba;
                if (color.a > 0)
                    return color.rgb / color.a;
                
                // 步进
                uvPos += dir * tex2D(_DistTex, uvPos).rr;
                if (NotUVSpace(uvPos))
                    return _AmbientColor;
                
                [unroll]
                for (int n = 1; n < STEPS; n++)
                {
                    const float4 color = tex2D(_ColorTex, uvPos).rgba;
                    if (color.a > 0)
                    {
                        // 使用 GTR 衰减
                        float attenuation = GTRAttenuation((uv - uvPos) * _Aspect.xy, _LightFalloffAlpha * color.a, _LightFalloffGamma);
                        return color.rgb * attenuation;
                    }

                    uvPos += dir * tex2D(_DistTex, uvPos).rr;
                    if (NotUVSpace(uvPos))
                        return _AmbientColor;
                }
                
                return _AmbientColor;
            }

            // =======================================================================

            FragInputGI Vert(uint vertexID : SV_VertexID)
            {
                FragInputGI o;
            
                // 生成全屏三角形 UV
                float2 uv = float2(
                    (vertexID << 1) & 2,
                    vertexID & 2
                );
                
                o.uv = uv;
                
                // 裁剪空间位置
                float2 pos = uv * 2.0 - 1.0;
                
                o.vertex = float4(pos, 0.0, 1.0);
#if UNITY_UV_STARTS_AT_TOP
                o.vertex.y = -o.vertex.y;
#endif
                
            #if defined(FRAGMENT_RANDOM) || defined(TEXTURE_RANDOM)
                o.noise_uv = uv * _NoiseTilingOffset.xy + _NoiseTilingOffset.zw;
            #endif
                
                return o;
            }

            float GetShaderNoise(float2 uv)
            {
                float2 noiseUV = uv * _NoiseScale + _NoiseVelocity * _Time.y;
                float n = 0;
                
                if (_NoiseType == 0) n = value_noise(noiseUV);
                else if (_NoiseType == 1) n = perlin_noise(noiseUV);
                else if (_NoiseType == 2) n = simplex_noise(noiseUV);
                else if (_NoiseType == 3) n = voronoi_noise(noiseUV);
                
                return n * _NoiseIntensity;
            }

            float4 Frag(FragInputGI i) : SV_Target
            {
#if defined(FRAGMENT_RANDOM)
                const float rand = GetShaderNoise(i.noise_uv);
#elif defined(TEXTURE_RANDOM)
                const float rand = tex2D(_NoiseTex, i.noise_uv).r;
#else
                const float rand = 0;
#endif

// =======================================================================
// Debug 可视化路径 (任一 keyword 开启时, 直接 return, 跳过正常 ray tracing)
// =======================================================================
#if defined(DEBUG_RAND)
                // H1: 显示每像素 rand 值. 期望: 移动相机时图像不动 (噪声锚定在屏幕)
                return float4(rand, rand, rand, 1);

#elif defined(DEBUG_SAMPLEDIR)
                // H1/H4: 显示第 0 条采样光线的方向 (R=cos, G=sin, 范围映射到 [0,1])
                {
                    const float t0 = (0 + rand) / _Samples * float(3.1415926 * 2.0);
                    return float4(cos(t0) * 0.5 + 0.5, sin(t0) * 0.5 + 0.5, 0, 1);
                }

#elif defined(DEBUG_COLORALPHA)
                // H2: 直接显示 _ColorTex 的 alpha 通道, 让你看到光源边缘是否有部分覆盖
                {
                    const float a = tex2D(_ColorTex, i.uv).a;
                    return float4(a, a, a, 1);
                }

#elif defined(DEBUG_EARLYEXIT)
                // H2: 红色 = 走 early-exit 直接返回光源色; 暗绿 = 走 ray marching 路径
                {
                    const float ea = tex2D(_ColorTex, i.uv).a;
                    return (ea > 0) ? float4(1, 0, 0, 1) : float4(0, 0.25, 0, 1);
                }

#elif defined(DEBUG_SDF)
                // Sanity: 直接显示 SDF
                {
                    const float d = tex2D(_DistTex, i.uv).r;
                    return float4(d, d, d, 1);
                }

#elif defined(DEBUG_PIXELINSPECTOR)
                // PixelInspector: 把 _DebugPixelUV 这个像素的全部 ray 数据铺满屏幕
                //   - X 方向: ray 编号 (0 ~ _Samples-1)
                //   - Y 方向: 8 个条带, 每个条带显示一个属性
                {
                    const int totalBands = 8;
                    const int rayIdx = (int)floor(i.uv.x * _Samples);
                    const int band = (int)floor(i.uv.y * totalBands);

                    if (rayIdx < 0 || rayIdx >= (int)_Samples)
                        return float4(0, 0, 0, 1);

                    // 重新计算 _DebugPixelUV 的 rand (需要按其屏幕位置采样)
#if defined(FRAGMENT_RANDOM)
                    const float2 dbgNoiseUV = _DebugPixelUV * _NoiseTilingOffset.xy + _NoiseTilingOffset.zw;
                    const float dbgRand = GetShaderNoise(dbgNoiseUV);
#elif defined(TEXTURE_RANDOM)
                    const float2 dbgNoiseUV = _DebugPixelUV * _NoiseTilingOffset.xy + _NoiseTilingOffset.zw;
                    const float dbgRand = tex2D(_NoiseTex, dbgNoiseUV).r;
#else
                    const float dbgRand = 0;
#endif

                    const float t = (rayIdx + dbgRand) / _Samples * float(3.1415926 * 2.0);
                    const float2 dir = float2(cos(t), sin(t)) / _Aspect.xy;

                    float2 uvPos = _DebugPixelUV;
                    float used = STEPS;
                    float didHit = 0;
                    float4 hitC = float4(0, 0, 0, 0);

                    const float4 c0 = tex2D(_ColorTex, uvPos).rgba;
                    if (c0.a > 0)
                    {
                        used = 0;
                        didHit = 1;
                        hitC = c0;
                    }
                    else
                    {
                        uvPos += dir * tex2D(_DistTex, uvPos).rr;
                        if (NotUVSpace(uvPos))
                        {
                            used = 1;
                        }
                        else
                        {
                            [loop]
                            for (int n = 1; n < STEPS; n++)
                            {
                                const float4 cn = tex2D(_ColorTex, uvPos).rgba;
                                if (cn.a > 0)
                                {
                                    used = n;
                                    didHit = 1;
                                    hitC = cn;
                                    break;
                                }
                                uvPos += dir * tex2D(_DistTex, uvPos).rr;
                                if (NotUVSpace(uvPos))
                                {
                                    used = n;
                                    break;
                                }
                            }
                        }
                    }

                    float attenuation = 0;
                    float3 contribution = float3(0, 0, 0);
                    if (didHit > 0)
                    {
                        attenuation = GTRAttenuation(
                            (_DebugPixelUV - uvPos) * _Aspect.xy,
                            _LightFalloffAlpha * hitC.a,
                            _LightFalloffGamma
                        );
                        contribution = hitC.rgb * attenuation;
                    }

                    // 条带分发
                    if (band == 0) return float4(didHit, didHit, didHit, 1);                              // 0: hit/miss
                    if (band == 1) return float4(hitC.a, hitC.a, hitC.a, 1);                              // 1: bilinear hit alpha (关键嫌疑)
                    if (band == 2) return float4(attenuation, attenuation, attenuation, 1);                // 2: attenuation
                    if (band == 3) return float4(hitC.r, hitC.r, hitC.r, 1);                              // 3: hit color.r
                    if (band == 4) return float4(hitC.g, hitC.g, hitC.g, 1);                              // 4: hit color.g
                    if (band == 5) return float4(hitC.b, hitC.b, hitC.b, 1);                              // 5: hit color.b
                    if (band == 6) { float s = used / float(STEPS); return float4(s, s, s, 1); }          // 6: 终止步数 / STEPS
                    if (band == 7) { float m = length(contribution); return float4(m, m, m, 1); }         // 7: 贡献度

                    return float4(0, 0, 0, 1);
                }

#elif defined(DEBUG_RAYTERMSTEP) || defined(DEBUG_HITFRACTION)
                // H3: 重做 ray marching, 但记录每条光线的 "终止步数" 和 "是否命中光源"
                //   - RayTermStep: 平均终止步数 / STEPS  (亮 = 接近用满步数, 是命中边界候选)
                //   - HitFraction: 命中光源的光线占比 (亮 = 多数命中)
                {
                    float accSteps = 0;
                    float hits = 0;

                    for (float f = 0.; f < _Samples; f++)
                    {
                        const float t = (f + rand) / _Samples * float(3.1415926 * 2.0);
                        const float2 dir = float2(cos(t), sin(t)) / _Aspect.xy;
                        float2 uvPos = i.uv;
                        float used = STEPS;
                        float didHit = 0;

                        const float4 c0 = tex2D(_ColorTex, uvPos).rgba;
                        if (c0.a > 0)
                        {
                            used = 0;
                            didHit = 1;
                        }
                        else
                        {
                            [loop]
                            for (int n = 1; n < STEPS; n++)
                            {
                                uvPos += dir * tex2D(_DistTex, uvPos).rr;
                                if (NotUVSpace(uvPos)) { used = n; break; }
                                const float4 cn = tex2D(_ColorTex, uvPos).rgba;
                                if (cn.a > 0) { used = n; didHit = 1; break; }
                            }
                        }

                        accSteps += used;
                        hits += didHit;
                    }

                    #if defined(DEBUG_RAYTERMSTEP)
                    const float avg = accSteps / _Samples / float(STEPS);
                    return float4(avg, avg, avg, 1);
                    #else
                    const float frac = hits / _Samples;
                    return float4(frac, frac, frac, 1);
                    #endif
                }
#else
// =======================================================================
// 正常渲染路径 (无 DEBUG keyword)
// =======================================================================
                float3 result = _AmbientColor;

                // 发射光线
                for (float f = 0.; f < _Samples; f++)
                {
                    const float t = (f + rand) / _Samples * float(3.1415926 * 2.0); // 均匀分布在圆周上
                    result += Trace(i.uv, float2(cos(t), sin(t)) / _Aspect.xy);
                }

                result /= _Samples;

                // 亮度调节
                result *= _Intensity;

                // Alpha 通道处理
                #if   defined(ONE_ALPHA)
                return float4(result, 1);

                #elif defined(OBJECTS_MASK_ALPHA)
                const float mask = tex2D(_ColorTex, i.uv).a;
                return float4(result, mask);

                #elif defined(NORMALIZED_ALPHA)
                // 颜色归一化, Alpha 作为不透明度
                float norm = max(result.r, max(result.g, result.b));
                return float4(result / norm, norm);
                #endif
#endif // DEBUG modes
            }
            ENDHLSL
        }
    }
}
