using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RainRust.Rendering
{
    [Serializable]
    [VolumeComponentMenu("Rain Rust/Rain Rust Volume")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class RainRustVolume : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isEnabled = new(true);
        public EnumParameter<RainRustNoiseMode> noiseMode = new(RainRustNoiseMode.Texture);
        public EnumParameter<RainRustNoiseType> noiseType = new(RainRustNoiseType.Perlin);
        public EnumParameter<RainRustAlphaMode> alphaMode = new(RainRustAlphaMode.OneAlpha);
        public TextureParameter noiseTexture = new(null);
        public Vector4Parameter noiseTilingOffset = new(Vector4.one);
        public Vector2Parameter noiseVelocity = new(Vector2.zero);
        public FloatParameter noiseScale = new(10.0f);
        public ClampedFloatParameter noiseIntensity = new(1.0f, 0f, 10f);
        public ClampedFloatParameter resolutionScalar = new(1.0f, 0.1f, 1.0f);
        public IntParameter lightSamples = new(16);
        public ClampedFloatParameter lightIntensity = new(0f, 0f, 10f);
        public ClampedFloatParameter lightFalloffAlpha = new(0.1f, 0.001f, 1f);
        public ClampedFloatParameter lightFalloffGamma = new(2.0f, 0.001f, 10f);
        public ClampedFloatParameter lightHitThreshold = new(0.1f, 0f, 1f);
        public ColorParameter ambientColor = new(Color.black);

        [Header("Debug")]
        public EnumParameter<RainRustDebugMode> debugMode = new(RainRustDebugMode.None);

        // 仅在 debugMode == PixelInspector 时使用; 屏幕 UV, 默认中心
        public Vector2Parameter debugPixelUV = new(new Vector2(0.5f, 0.5f));

        public bool IsActive() => isEnabled.value;

        public bool IsTileCompatible() => false;
    }

    public enum RainRustNoiseMode
    {
        None,
        Texture,
        Shader,
    }

    public enum RainRustNoiseType
    {
        Value,
        Perlin,
        Simplex,
        Voronoi,
    }

    public enum RainRustAlphaMode
    {
        OneAlpha,
        ObjectsMaskAlpha,
        NormalizedAlpha,
    }

    public enum RainRustDebugMode
    {
        None = 0,
        Rand = 1,
        SampleDir = 2,
        ColorAlpha = 3,
        EarlyExit = 4,
        RayTermStep = 5,
        HitFraction = 6,
        Sdf = 7,
        PixelInspector = 8,
        GTRBreakdown = 9,
    }
}
