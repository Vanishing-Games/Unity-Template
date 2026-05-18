using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameMain.RunTime
{
    public sealed class ColorFlowEffect : ICharacterEffect
    {
        public void Apply(ref CharacterEffectContext ctx)
        {
            if (!m_Initialized)
            {
                m_Params = ColorFlowParams.Parse(ctx.Attributes);
                m_Initialized = true;
            }

            float t = ctx.TotalTime * m_Params.Speed;
            float offset = ctx.VisibleCharIndex * 0.1f;
            float progress = (t - offset) % 1f;
            if (progress < 0f)
                progress += 1f;

            Color32 targetColor = EvaluatePalette(m_Params.PaletteName, progress);

            int vi = ctx.VertexIndex;
            // Overwrite color but keep alpha (useful if there are other effects or fading)
            targetColor.a = ctx.Colors[vi + 0].a;
            ctx.Colors[vi + 0] = targetColor;

            targetColor.a = ctx.Colors[vi + 1].a;
            ctx.Colors[vi + 1] = targetColor;

            targetColor.a = ctx.Colors[vi + 2].a;
            ctx.Colors[vi + 2] = targetColor;

            targetColor.a = ctx.Colors[vi + 3].a;
            ctx.Colors[vi + 3] = targetColor;
        }

        private static Color EvaluatePalette(string paletteName, float t)
        {
            // For v1, hardcode 'rainbow'
            // We treat t as Hue from 0 to 1
            return Color.HSVToRGB(t, 1f, 1f);
        }

        private readonly struct ColorFlowParams
        {
            public ColorFlowParams(string palette, float speed)
            {
                PaletteName = palette;
                Speed = speed;
            }

            public static ColorFlowParams Parse(IReadOnlyDictionary<string, string> attrs)
            {
                string palette = DefaultPalette;
                float speed = DefaultSpeed;
                if (attrs != null)
                {
                    if (attrs.TryGetValue("palette", out var palStr))
                    {
                        palette = palStr;
                    }
                    if (
                        attrs.TryGetValue("speed", out var spStr)
                        && float.TryParse(
                            spStr,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var parsedSpeed
                        )
                    )
                    {
                        speed = parsedSpeed;
                    }
                }
                return new ColorFlowParams(palette, speed);
            }

            public string PaletteName { get; }
            public float Speed { get; }

            private const string DefaultPalette = "rainbow";
            private const float DefaultSpeed = 1f;
        }

        private bool m_Initialized;
        private ColorFlowParams m_Params;
    }
}
