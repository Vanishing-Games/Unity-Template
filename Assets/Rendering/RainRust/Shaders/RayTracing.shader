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
            #pragma multi_compile_local _ DEBUG_RAND DEBUG_SAMPLEDIR DEBUG_COLORALPHA DEBUG_EARLYEXIT DEBUG_RAYTERMSTEP DEBUG_HITFRACTION DEBUG_SDF DEBUG_PIXELINSPECTOR DEBUG_GTRBREAKDOWN

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
                //   - X 方向: ray 编号 0 .. _Samples-1
                //   - Y 方向: 6 个条带
                //   - 7 个条带 (从 Y=0 即屏幕顶端往下):
                //       0: hit/miss (1/0)
                //       1: contribution RGB  (color*atten | color/alpha | _AmbientColor)
                //       2: share = length(thisContrib) / sum_k length(contrib_k)
                //       3: used / STEPS
                //       4: GTR attenuation
                //       5: 光线方向 (R=cos*0.5+0.5, G=sin*0.5+0.5)
                //       6: 最终像素颜色 (与正常 Frag 一致, 所有列应相同)
                {
                    const int totalBands = 7;
                    const int N = clamp((int)_Samples, 1, 64);
                    const int rayIdx = (int)floor(i.uv.x * N);
                    const int band = (int)floor(i.uv.y * totalBands);

                    if (rayIdx < 0 || rayIdx >= N)
                        return float4(0, 0, 0, 1);
                    if (band < 0 || band >= totalBands)
                        return float4(0, 0, 0, 1);

                    // _DebugPixelUV 处的 rand (按其屏幕坐标采样, 与正常渲染一致)
                    #if defined(FRAGMENT_RANDOM)
                    const float2 dbgNoiseUV = _DebugPixelUV * _NoiseTilingOffset.xy + _NoiseTilingOffset.zw;
                    const float dbgRand = GetShaderNoise(dbgNoiseUV);
                    #elif defined(TEXTURE_RANDOM)
                    const float2 dbgNoiseUV = _DebugPixelUV * _NoiseTilingOffset.xy + _NoiseTilingOffset.zw;
                    const float dbgRand = tex2D(_NoiseTex, dbgNoiseUV).r;
                    #else
                    const float dbgRand = 0;
                    #endif

                    // 一次循环遍历所有光线: 边走边累 sumMag / sumContrib, 记录 thisRay 的细节
                    float  sumMag     = 0;
                    float3 sumContrib = float3(0, 0, 0);

                    // thisRay 默认值 = miss 语义
                    float  thisHit     = 0;
                    float3 thisContrib = _AmbientColor;
                    float  thisAtten   = 0;
                    float  thisUsed    = STEPS;
                    float2 thisDir     = float2(0, 0);

                    [loop]
                    for (int r = 0; r < N; r++)
                    {
                        const float  tR    = (r + dbgRand) / float(N) * float(3.1415926 * 2.0);
                        const float2 dirR  = float2(cos(tR), sin(tR));   // 原始单位方向
                        const float2 dirRA = dirR / _Aspect.xy;          // ray marching 用的 aspect 修正方向

                        // 每条光线的局部变量都在迭代内显式声明 + 初始化, 避免编译器跨迭代复用
                        float2 uvP     = _DebugPixelUV;
                        float  usedR   = STEPS;
                        float  hitR    = 0;
                        float  attenR  = 0;
                        float3 contribR = _AmbientColor;   // miss 默认贡献 = 环境光

                        const float4 c0 = tex2D(_ColorTex, uvP).rgba;
                        if (c0.a > 0)
                        {
                            // 起点就在光源上 (原 Trace 的 early-exit 路径)
                            usedR    = 0;
                            hitR     = 1;
                            attenR   = 1;
                            contribR = c0.rgb / c0.a;
                        }
                        else
                        {
                            uvP += dirRA * tex2D(_DistTex, uvP).rr;
                            if (NotUVSpace(uvP))
                            {
                                usedR = 1;
                                // miss; contribR 保持 _AmbientColor
                            }
                            else
                            {
                                [loop]
                                for (int n = 1; n < STEPS; n++)
                                {
                                    const float4 cn = tex2D(_ColorTex, uvP).rgba;
                                    if (cn.a > 0)
                                    {
                                        usedR    = n;
                                        hitR     = 1;
                                        attenR   = GTRAttenuation(
                                            (_DebugPixelUV - uvP) * _Aspect.xy,
                                            _LightFalloffAlpha * cn.a,
                                            _LightFalloffGamma
                                        );
                                        contribR = cn.rgb * attenR;
                                        break;
                                    }
                                    uvP += dirRA * tex2D(_DistTex, uvP).rr;
                                    if (NotUVSpace(uvP))
                                    {
                                        usedR = n;
                                        break;
                                    }
                                }
                                // 自然走完循环未命中: contribR 保持 _AmbientColor
                            }
                        }

                        sumMag     += length(contribR);
                        sumContrib += contribR;

                        if (r == rayIdx)
                        {
                            thisHit     = hitR;
                            thisContrib = contribR;
                            thisAtten   = attenR;
                            thisUsed    = usedR;
                            thisDir     = dirR;
                        }
                    }

                    const float thisMag = length(thisContrib);
                    const float share   = sumMag > 1e-6 ? thisMag / sumMag : 0;

                    if (band == 0) return float4(thisHit, thisHit, thisHit, 1);
                    if (band == 1) return float4(thisContrib, 1);
                    if (band == 2) return float4(share, share, share, 1);
                    if (band == 3) { const float s = thisUsed / float(STEPS); return float4(s, s, s, 1); }
                    if (band == 4) return float4(thisAtten, thisAtten, thisAtten, 1);
                    if (band == 5) return float4(thisDir.x * 0.5 + 0.5, thisDir.y * 0.5 + 0.5, 0, 1);
                    if (band == 6)
                    {
                        // 最终像素颜色: 与正常 Frag 路径完全一致
                        //   result = _AmbientColor + sum(per-ray contribR); result /= N; result *= _Intensity
                        const float3 finalRGB = (_AmbientColor + sumContrib) / float(N) * _Intensity;
                        return float4(finalRGB, 1);
                    }

                    return float4(0, 0, 0, 1);
                }

#elif defined(DEBUG_GTRBREAKDOWN)
                // GTRBreakdown: 把 _DebugPixelUV 这个像素的 GTR 输入分量铺满屏幕
                //   - X 方向: ray 编号 0 .. _Samples-1 (与 PixelInspector 列对齐)
                //   - Y 方向: 7 个条带
                //   - 7 个条带 (从 Y=0 即屏幕顶端往下):
                //       0: hit/miss (1/0) sanity
                //       1: x = length((uv - hitUV) * aspect)  [灰度, 原值]
                //       2: cn.a  [灰度, 0..1, bilinear 命中点 alpha]
                //       3: eff_alpha = _LightFalloffAlpha * cn.a  [灰度]
                //       4: x / eff_alpha  [灰度, saturate 到 [0,1]]
                //       5: GTR 最终值  [灰度, 0..1]
                //       6: 命中 UV (R=hitUV.x, G=hitUV.y)
                //
                //   用法: RenderDoc 抓两次 (相机移动前/后), 对比同一列各 band 的精确数值;
                //         band 1 跳 → 是命中距离在跳
                //         band 2 跳 → 是 _ColorTex bilinear 出来的 alpha 在跳
                //         band 6 跳 → 是命中点位置 (texel) 本身在跳
                {
                    const int totalBands = 7;
                    const int N = clamp((int)_Samples, 1, 64);
                    const int rayIdx = (int)floor(i.uv.x * N);
                    const int band = (int)floor(i.uv.y * totalBands);

                    if (rayIdx < 0 || rayIdx >= N)
                        return float4(0, 0, 0, 1);
                    if (band < 0 || band >= totalBands)
                        return float4(0, 0, 0, 1);

                    // _DebugPixelUV 处的 rand (按其屏幕坐标采样, 与正常渲染一致)
                    #if defined(FRAGMENT_RANDOM)
                    const float2 dbgNoiseUV = _DebugPixelUV * _NoiseTilingOffset.xy + _NoiseTilingOffset.zw;
                    const float dbgRand = GetShaderNoise(dbgNoiseUV);
                    #elif defined(TEXTURE_RANDOM)
                    const float2 dbgNoiseUV = _DebugPixelUV * _NoiseTilingOffset.xy + _NoiseTilingOffset.zw;
                    const float dbgRand = tex2D(_NoiseTex, dbgNoiseUV).r;
                    #else
                    const float dbgRand = 0;
                    #endif

                    // 只重做 rayIdx 这一条光线的 ray-march, 拿到它的 GTR 输入分量
                    const float  tR    = (rayIdx + dbgRand) / float(N) * float(3.1415926 * 2.0);
                    const float2 dirR  = float2(cos(tR), sin(tR));
                    const float2 dirRA = dirR / _Aspect.xy;

                    float2 uvP    = _DebugPixelUV;
                    float  hitR   = 0;
                    float  hitA   = 0;          // cn.a 命中点 alpha
                    float2 hitUV  = float2(0, 0);

                    const float4 c0 = tex2D(_ColorTex, uvP).rgba;
                    if (c0.a > 0)
                    {
                        // 起点就在光源上, 距离 = 0, alpha = 起点 alpha
                        hitR  = 1;
                        hitA  = c0.a;
                        hitUV = uvP;
                    }
                    else
                    {
                        uvP += dirRA * tex2D(_DistTex, uvP).rr;
                        if (!NotUVSpace(uvP))
                        {
                            [loop]
                            for (int n = 1; n < STEPS; n++)
                            {
                                const float4 cn = tex2D(_ColorTex, uvP).rgba;
                                if (cn.a > 0)
                                {
                                    hitR  = 1;
                                    hitA  = cn.a;
                                    hitUV = uvP;
                                    break;
                                }
                                uvP += dirRA * tex2D(_DistTex, uvP).rr;
                                if (NotUVSpace(uvP))
                                    break;
                            }
                        }
                    }

                    if (band == 0) return float4(hitR, hitR, hitR, 1);
                    if (hitR < 0.5)
                    {
                        // miss: 距离/alpha/GTR 都没有意义, 全显黑, 但 band 6 仍显示当前 uvP 帮助看出射范围
                        if (band == 6) return float4(uvP.x, uvP.y, 0, 1);
                        return float4(0, 0, 0, 1);
                    }

                    const float  x        = length((_DebugPixelUV - hitUV) * _Aspect.xy);
                    const float  effAlpha = _LightFalloffAlpha * hitA;
                    const float  ratio    = effAlpha > 1e-8 ? (x / effAlpha) : 1e8;
                    const float  gtr      = 1.0 / pow(1.0 + pow(ratio, 2.0), _LightFalloffGamma);

                    if (band == 1) return float4(x, x, x, 1);
                    if (band == 2) return float4(hitA, hitA, hitA, 1);
                    if (band == 3) return float4(effAlpha, effAlpha, effAlpha, 1);
                    if (band == 4) { const float r = saturate(ratio); return float4(r, r, r, 1); }
                    if (band == 5) return float4(gtr, gtr, gtr, 1);
                    if (band == 6) return float4(hitUV.x, hitUV.y, 0, 1);

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
