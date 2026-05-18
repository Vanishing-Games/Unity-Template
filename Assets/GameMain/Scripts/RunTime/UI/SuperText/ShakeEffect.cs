using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameMain.RunTime
{
    public sealed class ShakeEffect : ICharacterEffect
    {
        public void Apply(ref CharacterEffectContext ctx)
        {
            if (!m_Initialized)
            {
                m_Params = ShakeParams.Parse(ctx.Attributes);
                m_Initialized = true;
            }

            float t = ctx.TotalTime * m_Params.Speed;
            float seed = ctx.VisibleCharIndex * 0.7321f;
            float dx = (Mathf.PerlinNoise(t + seed, 0f) - 0.5f) * 2f * m_Params.Amp;
            float dy = (Mathf.PerlinNoise(0f, t + seed) - 0.5f) * 2f * m_Params.Amp;
            Vector3 delta = new(dx, dy, 0f);

            int vi = ctx.VertexIndex;
            ctx.Vertices[vi + 0] += delta;
            ctx.Vertices[vi + 1] += delta;
            ctx.Vertices[vi + 2] += delta;
            ctx.Vertices[vi + 3] += delta;
        }

        private readonly struct ShakeParams
        {
            public ShakeParams(float amp, float speed)
            {
                Amp = amp;
                Speed = speed;
            }

            public static ShakeParams Parse(IReadOnlyDictionary<string, string> attrs)
            {
                float amp = DefaultAmp;
                float speed = DefaultSpeed;
                if (attrs != null)
                {
                    if (
                        attrs.TryGetValue("amp", out var ampStr)
                        && float.TryParse(
                            ampStr,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var parsedAmp
                        )
                    )
                    {
                        amp = parsedAmp;
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
                return new ShakeParams(amp, speed);
            }

            public float Amp { get; }
            public float Speed { get; }

            private const float DefaultAmp = 0.1f;
            private const float DefaultSpeed = 10f;
        }

        private bool m_Initialized;
        private ShakeParams m_Params;
    }
}
