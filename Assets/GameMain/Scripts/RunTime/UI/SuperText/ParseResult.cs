using System.Collections.Generic;

namespace GameMain.RunTime
{
    public struct ParseResult
    {
        public string CleanText;
        public IReadOnlyList<EffectSpan> Spans;

        public ParseResult(string cleanText, IReadOnlyList<EffectSpan> spans)
        {
            CleanText = cleanText;
            Spans = spans;
        }
    }
}
