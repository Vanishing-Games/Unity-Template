using System.Collections.Generic;
using System.Globalization;
using Core;
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
            m_TmpText.ForceMeshUpdate();

            m_TotalVisibleChars = m_TmpText.textInfo.characterCount;
            m_Elapsed = 0f;

            BuildTypewriterSchedule(result.Spans);

            bool typewriterDisabled = !m_HasTypewriterSpan && m_GlobalCharsPerSecond <= 0f;
            if (typewriterDisabled || m_TotalVisibleChars == 0)
            {
                m_TmpText.maxVisibleCharacters = m_TotalVisibleChars;
                m_IsRevealing = false;
                CLogger.LogVerbose(
                    "EffectTextRenderer.ShowText: showing all characters immediately.",
                    LogTag.UI
                );
                return;
            }

            m_TmpText.maxVisibleCharacters = 0;
            m_IsRevealing = true;
        }

        public void Clear()
        {
            EnsureTextComponent();
            m_TmpText.text = string.Empty;
            m_TmpText.ForceMeshUpdate();
            m_TotalVisibleChars = 0;
            m_Elapsed = 0f;
            m_IsRevealing = false;
            m_TypewriterSchedule = null;
            m_HasTypewriterSpan = false;
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
            if (!m_IsRevealing)
            {
                return;
            }

            m_Elapsed += Time.deltaTime;

            int revealed;
            if (m_HasTypewriterSpan && m_TypewriterSchedule != null)
            {
                revealed = ScheduledRevealCount(
                    m_Elapsed,
                    m_TypewriterSchedule,
                    m_TotalVisibleChars
                );
            }
            else
            {
                revealed = ComputeRevealedCount(
                    m_Elapsed,
                    m_GlobalCharsPerSecond,
                    m_TotalVisibleChars
                );
            }

            m_TmpText.maxVisibleCharacters = revealed;

            if (revealed >= m_TotalVisibleChars)
            {
                m_IsRevealing = false;
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

        private float[] m_TypewriterSchedule;
        private bool m_HasTypewriterSpan;
        private int m_TotalVisibleChars;
        private float m_Elapsed;
        private bool m_IsRevealing;
    }
}
