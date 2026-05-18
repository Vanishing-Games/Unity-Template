using System.Collections.Generic;

namespace GameMain.RunTime
{
    public struct EffectSpan
    {
        public int StartVisibleIndex;
        public int EndVisibleIndex; // Half-open interval
        public string TagName;
        public IReadOnlyDictionary<string, string> Attributes;

        public EffectSpan(
            int start,
            int end,
            string tagName,
            IReadOnlyDictionary<string, string> attributes
        )
        {
            StartVisibleIndex = start;
            EndVisibleIndex = end;
            TagName = tagName;
            Attributes = attributes;
        }
    }
}
