using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace GameMain.RunTime
{
    [RequireComponent(typeof(TMP_Text))]
    public class EffectTextRenderer : MonoBehaviour
    {
        public void ShowText(string markup)
        {
            EnsureTextComponent();

            var result = RichTextTagParser.Parse(markup ?? string.Empty);
            m_TmpText.text = result.CleanText;
            m_TmpText.maxVisibleCharacters = int.MaxValue;
            m_TmpText.ForceMeshUpdate();

            m_TotalVisibleChars = m_TmpText.textInfo.characterCount;
            m_Elapsed = 0f;

            BuildTypewriterSchedule(result.Spans);
            BuildCharacterEffectSpans(result.Spans);
            CacheBaselineMeshData();

            bool typewriterDisabled = !m_HasTypewriterSpan && m_GlobalCharsPerSecond <= 0f;
            if (typewriterDisabled || m_TotalVisibleChars == 0)
            {
                m_RevealedCount = m_TotalVisibleChars;
                m_IsRevealing = false;
                CLogger.LogVerbose(
                    "EffectTextRenderer.ShowText: showing all characters immediately.",
                    LogTag.UI
                );
                SyncMesh(revealChanged: true);
                if (m_TotalVisibleChars > 0)
                {
                    for (int i = 0; i < m_TotalVisibleChars; i++)
                    {
                        MessageBroker.Global.Publish(new SuperTextEvents.CharacterRevealedEvent(i));
                    }
                    MessageBroker.Global.Publish(new SuperTextEvents.TextRevealCompletedEvent());
                }
            }
            else
            {
                m_RevealedCount = 0;
                m_IsRevealing = true;
                SyncMesh(revealChanged: true);
            }
        }

        public void Clear()
        {
            EnsureTextComponent();
            m_TmpText.text = string.Empty;
            m_TmpText.ForceMeshUpdate();
            m_TotalVisibleChars = 0;
            m_RevealedCount = 0;
            m_Elapsed = 0f;
            m_IsRevealing = false;
            m_TypewriterSchedule = null;
            m_HasTypewriterSpan = false;
            m_EffectSpans.Clear();
            m_HasCharacterEffects = false;
            m_BaselineVertices = null;
            m_BaselineColors = null;
        }

        public void SkipToEnd()
        {
            if (m_TotalVisibleChars > 0 && m_RevealedCount < m_TotalVisibleChars)
            {
                int oldRevealed = m_RevealedCount;
                m_RevealedCount = m_TotalVisibleChars;
                m_IsRevealing = false;
                SyncMesh(true);
                for (int i = oldRevealed; i < m_RevealedCount; i++)
                {
                    MessageBroker.Global.Publish(new SuperTextEvents.CharacterRevealedEvent(i));
                }
                MessageBroker.Global.Publish(new SuperTextEvents.TextRevealCompletedEvent());
            }
        }

        public async UniTask WaitForCompleteAsync(CancellationToken ct = default)
        {
            if (!m_IsRevealing && m_RevealedCount >= m_TotalVisibleChars)
            {
                return;
            }
            await MessageBroker
                .Global.Receive<SuperTextEvents.TextRevealCompletedEvent>()
                .FirstAsync(ct);
        }

        public static int ComputeRevealedCount(float elapsed, float charsPerSecond, int total)
        {
            if (total <= 0)
            {
                return 0;
            }
            if (elapsed <= 0f || charsPerSecond <= 0f)
            {
                return 0;
            }
            long count = (long)(elapsed * charsPerSecond);
            if (count <= 0)
            {
                return 0;
            }
            return count >= total ? total : (int)count;
        }

        private void Awake()
        {
            EnsureTextComponent();
        }

        private void Start()
        {
            if (m_PlayOnStart && !string.IsNullOrEmpty(m_InitialMarkup))
            {
                ShowText(m_InitialMarkup);
            }
        }

        private void Update()
        {
            if (!m_IsRevealing && !m_HasCharacterEffects)
            {
                return;
            }

            m_Elapsed += Time.deltaTime;

            int newRevealed = m_RevealedCount;
            if (m_IsRevealing)
            {
                newRevealed =
                    m_HasTypewriterSpan && m_TypewriterSchedule != null
                        ? ScheduledRevealCount(m_Elapsed, m_TypewriterSchedule, m_TotalVisibleChars)
                        : ComputeRevealedCount(
                            m_Elapsed,
                            m_GlobalCharsPerSecond,
                            m_TotalVisibleChars
                        );

                if (newRevealed >= m_TotalVisibleChars)
                {
                    m_IsRevealing = false;
                }
            }

            bool revealChanged = newRevealed != m_RevealedCount;
            int oldRevealed = m_RevealedCount;
            m_RevealedCount = newRevealed;

            if (revealChanged || m_HasCharacterEffects)
            {
                SyncMesh(revealChanged);
            }

            if (revealChanged)
            {
                for (int i = oldRevealed; i < newRevealed; i++)
                {
                    MessageBroker.Global.Publish(new SuperTextEvents.CharacterRevealedEvent(i));
                }
                if (newRevealed >= m_TotalVisibleChars && m_TotalVisibleChars > 0)
                {
                    MessageBroker.Global.Publish(new SuperTextEvents.TextRevealCompletedEvent());
                }
            }
        }

        private void EnsureTextComponent()
        {
            if (m_TmpText == null)
            {
                m_TmpText = GetComponent<TMP_Text>();
            }
        }

        private void BuildTypewriterSchedule(IReadOnlyList<EffectSpan> spans)
        {
            m_HasTypewriterSpan = false;
            m_TypewriterSchedule = null;

            if (m_TotalVisibleChars <= 0)
            {
                return;
            }

            float defaultSpeed = m_GlobalCharsPerSecond;
            float[] perCharSpeed = new float[m_TotalVisibleChars];
            for (int i = 0; i < perCharSpeed.Length; i++)
            {
                perCharSpeed[i] = defaultSpeed;
            }

            for (int idx = 0; idx < spans.Count; idx++)
            {
                var span = spans[idx];
                if (span.TagName != "typewriter")
                {
                    continue;
                }

                m_HasTypewriterSpan = true;

                float speed = defaultSpeed;
                if (
                    span.Attributes.TryGetValue("speed", out var speedStr)
                    && float.TryParse(
                        speedStr,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var parsedSpeed
                    )
                )
                {
                    speed = parsedSpeed;
                }

                int start = Mathf.Clamp(span.StartVisibleIndex, 0, perCharSpeed.Length);
                int end = Mathf.Clamp(span.EndVisibleIndex, 0, perCharSpeed.Length);
                for (int i = start; i < end; i++)
                {
                    perCharSpeed[i] = speed;
                }
            }

            if (!m_HasTypewriterSpan)
            {
                return;
            }

            float[] cumulative = new float[m_TotalVisibleChars + 1];
            for (int i = 0; i < m_TotalVisibleChars; i++)
            {
                float speed = perCharSpeed[i];
                float dt = speed <= 0f ? 0f : 1f / speed;
                cumulative[i + 1] = cumulative[i] + dt;
            }
            m_TypewriterSchedule = cumulative;
        }

        private void BuildCharacterEffectSpans(IReadOnlyList<EffectSpan> spans)
        {
            m_EffectSpans.Clear();
            m_HasCharacterEffects = false;

            for (int i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                if (!CharacterEffectRegistry.IsRegistered(span.TagName))
                {
                    continue;
                }
                var effect = CharacterEffectRegistry.Create(span.TagName);
                if (effect == null)
                {
                    continue;
                }
                m_EffectSpans.Add(new EffectSpanInstance(span, effect));
                m_HasCharacterEffects = true;
            }
        }

        private void CacheBaselineMeshData()
        {
            if (m_TotalVisibleChars == 0)
            {
                m_BaselineVertices = null;
                m_BaselineColors = null;
                return;
            }

            var meshInfo = m_TmpText.textInfo.meshInfo;
            m_BaselineVertices = new Vector3[meshInfo.Length][];
            m_BaselineColors = new Color32[meshInfo.Length][];
            for (int i = 0; i < meshInfo.Length; i++)
            {
                var srcVerts = meshInfo[i].vertices;
                var srcColors = meshInfo[i].colors32;
                m_BaselineVertices[i] = new Vector3[srcVerts.Length];
                m_BaselineColors[i] = new Color32[srcColors.Length];
                System.Array.Copy(srcVerts, m_BaselineVertices[i], srcVerts.Length);
                System.Array.Copy(srcColors, m_BaselineColors[i], srcColors.Length);
            }
        }

        private void SyncMesh(bool revealChanged)
        {
            if (m_BaselineVertices == null || m_TotalVisibleChars == 0)
            {
                return;
            }

            var textInfo = m_TmpText.textInfo;
            var meshInfo = textInfo.meshInfo;

            if (revealChanged || m_HasCharacterEffects)
            {
                ApplyVisibilityColorsAndBaseVertices(textInfo, meshInfo);
            }

            if (m_HasCharacterEffects)
            {
                ApplyCharacterEffects(textInfo, meshInfo);
            }

            m_TmpText.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32
            );
        }

        private void ApplyVisibilityColorsAndBaseVertices(
            TMP_TextInfo textInfo,
            TMP_MeshInfo[] meshInfo
        )
        {
            for (int i = 0; i < m_TotalVisibleChars; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                {
                    continue;
                }

                int matIdx = charInfo.materialReferenceIndex;
                int vi = charInfo.vertexIndex;
                var liveColors = meshInfo[matIdx].colors32;
                var baseColors = m_BaselineColors[matIdx];
                var liveVerts = meshInfo[matIdx].vertices;
                var baseVerts = m_BaselineVertices[matIdx];

                liveVerts[vi + 0] = baseVerts[vi + 0];
                liveVerts[vi + 1] = baseVerts[vi + 1];
                liveVerts[vi + 2] = baseVerts[vi + 2];
                liveVerts[vi + 3] = baseVerts[vi + 3];

                if (i < m_RevealedCount)
                {
                    liveColors[vi + 0] = baseColors[vi + 0];
                    liveColors[vi + 1] = baseColors[vi + 1];
                    liveColors[vi + 2] = baseColors[vi + 2];
                    liveColors[vi + 3] = baseColors[vi + 3];
                }
                else
                {
                    Color32 c0 = baseColors[vi + 0];
                    Color32 c1 = baseColors[vi + 1];
                    Color32 c2 = baseColors[vi + 2];
                    Color32 c3 = baseColors[vi + 3];
                    c0.a = 0;
                    c1.a = 0;
                    c2.a = 0;
                    c3.a = 0;
                    liveColors[vi + 0] = c0;
                    liveColors[vi + 1] = c1;
                    liveColors[vi + 2] = c2;
                    liveColors[vi + 3] = c3;
                }
            }
        }

        private void ApplyCharacterEffects(TMP_TextInfo textInfo, TMP_MeshInfo[] meshInfo)
        {
            for (int s = 0; s < m_EffectSpans.Count; s++)
            {
                var spanInstance = m_EffectSpans[s];
                int start = Mathf.Clamp(spanInstance.Span.StartVisibleIndex, 0, m_RevealedCount);
                int end = Mathf.Clamp(spanInstance.Span.EndVisibleIndex, 0, m_RevealedCount);

                for (int charIdx = start; charIdx < end; charIdx++)
                {
                    var charInfo = textInfo.characterInfo[charIdx];
                    if (!charInfo.isVisible)
                    {
                        continue;
                    }

                    int matIdx = charInfo.materialReferenceIndex;
                    int vi = charInfo.vertexIndex;
                    var liveVerts = meshInfo[matIdx].vertices;
                    var liveColors = meshInfo[matIdx].colors32;

                    var ctx = new CharacterEffectContext
                    {
                        Vertices = liveVerts,
                        Colors = liveColors,
                        VertexIndex = vi,
                        VisibleCharIndex = charIdx,
                        TotalTime = m_Elapsed,
                        Attributes = spanInstance.Span.Attributes,
                    };
                    spanInstance.Effect.Apply(ref ctx);
                }
            }
        }

        private static int ScheduledRevealCount(float elapsed, float[] cumulative, int total)
        {
            if (elapsed <= 0f)
            {
                return 0;
            }

            int lo = 0;
            int hi = total;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (cumulative[mid] <= elapsed)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return lo;
        }

        private readonly struct EffectSpanInstance
        {
            public EffectSpanInstance(EffectSpan span, ICharacterEffect effect)
            {
                Span = span;
                Effect = effect;
            }

            public EffectSpan Span { get; }
            public ICharacterEffect Effect { get; }
        }

        [SerializeField]
        private TMP_Text m_TmpText;

        [SerializeField]
        [Tooltip(
            "Characters per second outside any [typewriter] span. <= 0 shows all characters immediately when no typewriter span exists."
        )]
        private float m_GlobalCharsPerSecond = 30f;

        [SerializeField]
        private bool m_PlayOnStart;

        [SerializeField, TextArea(2, 6)]
        private string m_InitialMarkup;

        private readonly List<EffectSpanInstance> m_EffectSpans = new();
        private float[] m_TypewriterSchedule;
        private Vector3[][] m_BaselineVertices;
        private Color32[][] m_BaselineColors;
        private bool m_HasTypewriterSpan;
        private bool m_HasCharacterEffects;
        private int m_TotalVisibleChars;
        private int m_RevealedCount;
        private float m_Elapsed;
        private bool m_IsRevealing;
    }
}
