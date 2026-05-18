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

        [Test]
        public void Parse_ShakeSpanWithAttributes_StripsTagsAndCapturesParams()
        {
            var result = RichTextTagParser.Parse("aa[shake amp=\"0.3\" speed=\"20\"]foo[/shake]z");

            Assert.AreEqual("aafooz", result.CleanText);
            Assert.AreEqual(1, result.Spans.Count);

            var span = result.Spans.Single();
            Assert.AreEqual("shake", span.TagName);
            Assert.AreEqual(2, span.StartVisibleIndex);
            Assert.AreEqual(5, span.EndVisibleIndex);
            Assert.AreEqual("0.3", span.Attributes["amp"]);
            Assert.AreEqual("20", span.Attributes["speed"]);
        }

        [Test]
        public void Parse_ShakeSpanWithoutAttributes_IsLegalAndProducesEmptyAttributes()
        {
            var result = RichTextTagParser.Parse("[shake]bar[/shake]");

            Assert.AreEqual("bar", result.CleanText);
            Assert.AreEqual(1, result.Spans.Count);

            var span = result.Spans.Single();
            Assert.AreEqual("shake", span.TagName);
            Assert.AreEqual(0, span.StartVisibleIndex);
            Assert.AreEqual(3, span.EndVisibleIndex);
            Assert.AreEqual(0, span.Attributes.Count);
        }

        [Test]
        public void Parse_NestedTags_ProduceCorrectSpans()
        {
            // Setup register to recognize color-flow
            var result = RichTextTagParser.Parse("[shake][color-flow]foo[/color-flow][/shake]");

            Assert.AreEqual("foo", result.CleanText);
            Assert.AreEqual(2, result.Spans.Count);

            var innerSpan = result.Spans[0];
            Assert.AreEqual("color-flow", innerSpan.TagName);
            Assert.AreEqual(0, innerSpan.StartVisibleIndex);
            Assert.AreEqual(3, innerSpan.EndVisibleIndex);

            var outerSpan = result.Spans[1];
            Assert.AreEqual("shake", outerSpan.TagName);
            Assert.AreEqual(0, outerSpan.StartVisibleIndex);
            Assert.AreEqual(3, outerSpan.EndVisibleIndex);
        }

        [Test]
        public void Parse_AdjacentTags_ProduceCorrectSpans()
        {
            var result = RichTextTagParser.Parse("a[shake]b[/shake]c[color-flow]d[/color-flow]e");

            Assert.AreEqual("abcde", result.CleanText);
            Assert.AreEqual(2, result.Spans.Count);

            Assert.AreEqual("shake", result.Spans[0].TagName);
            Assert.AreEqual(1, result.Spans[0].StartVisibleIndex);
            Assert.AreEqual(2, result.Spans[0].EndVisibleIndex);

            Assert.AreEqual("color-flow", result.Spans[1].TagName);
            Assert.AreEqual(3, result.Spans[1].StartVisibleIndex);
            Assert.AreEqual(4, result.Spans[1].EndVisibleIndex);
        }

        [Test]
        public void Parse_UnknownTag_TreatedAsLiteralWithNoSpans()
        {
            var result = RichTextTagParser.Parse("[unknown]foo[/unknown]");

            Assert.AreEqual("[unknown]foo[/unknown]", result.CleanText);
            Assert.AreEqual(0, result.Spans.Count);
        }

        [Test]
        public void Parse_UnclosedTag_ExtendsToEndAndWarns()
        {
            // Note: Since CLogger is static, we can't easily assert the log here without mocking,
            // but we can assert the span logic.
            var result = RichTextTagParser.Parse("[shake]foo");

            Assert.AreEqual("foo", result.CleanText);
            Assert.AreEqual(1, result.Spans.Count);

            var span = result.Spans.Single();
            Assert.AreEqual("shake", span.TagName);
            Assert.AreEqual(0, span.StartVisibleIndex);
            Assert.AreEqual(3, span.EndVisibleIndex);
        }

        [Test]
        public void Parse_Escape_ProducesLiteralBracket()
        {
            var result = RichTextTagParser.Parse("\\[not-a-tag]");

            Assert.AreEqual("[not-a-tag]", result.CleanText);
            Assert.AreEqual(0, result.Spans.Count);
        }

        [Test]
        public void Parse_PureWhitespace_PreservedWithNoSpans()
        {
            var result = RichTextTagParser.Parse("   ");

            Assert.AreEqual("   ", result.CleanText);
            Assert.AreEqual(0, result.Spans.Count);
        }
    }
}
