using System.Linq;
using GameMain.RunTime;
using NUnit.Framework;

namespace Test.Editor.UI.SuperText
{
    public class RichTextTagParserTests
    {
        [Test]
        public void Parse_PlainText_ReturnsTextUnchangedWithNoSpans()
        {
            var result = RichTextTagParser.Parse("Hello world");

            Assert.AreEqual("Hello world", result.CleanText);
            Assert.AreEqual(0, result.Spans.Count);
        }

        [Test]
        public void Parse_TypewriterSpan_StripsTagsAndProducesSpan()
        {
            var result = RichTextTagParser.Parse(
                "Pre [typewriter speed=\"30\"]middle[/typewriter] post"
            );

            Assert.AreEqual("Pre middle post", result.CleanText);
            Assert.AreEqual(1, result.Spans.Count);

            var span = result.Spans[0];
            Assert.AreEqual("typewriter", span.TagName);
            Assert.AreEqual("30", span.Attributes["speed"]);
            Assert.AreEqual(4, span.StartVisibleIndex);
            Assert.AreEqual(10, span.EndVisibleIndex);
        }

        [Test]
        public void Parse_TmpNativeTag_IsKeptInCleanTextAndNotCounted()
        {
            var result = RichTextTagParser.Parse(
                "A [typewriter speed=\"30\"]<color=red>RED</color> tail[/typewriter] Z"
            );

            Assert.AreEqual("A <color=red>RED</color> tail Z", result.CleanText);
            Assert.AreEqual(1, result.Spans.Count);

            var span = result.Spans.Single();
            Assert.AreEqual(2, span.StartVisibleIndex);
            Assert.AreEqual(10, span.EndVisibleIndex);
        }

        [Test]
        public void Parse_EmptyInput_ReturnsEmptyResult()
        {
            var result = RichTextTagParser.Parse(string.Empty);

            Assert.AreEqual(string.Empty, result.CleanText);
            Assert.AreEqual(0, result.Spans.Count);
        }
    }
}
