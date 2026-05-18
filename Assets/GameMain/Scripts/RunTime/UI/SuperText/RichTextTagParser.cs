using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Core;

namespace GameMain.RunTime
{
    public static class RichTextTagParser
    {
        public static ParseResult Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new ParseResult(string.Empty, Array.Empty<EffectSpan>());
            }

            var cleanText = new StringBuilder(input.Length);
            var spans = new List<EffectSpan>();
            var openStack = new Stack<OpenSpan>();
            int visibleCount = 0;
            int i = 0;

            while (i < input.Length)
            {
                char c = input[i];

                if (c == '\\' && i + 1 < input.Length)
                {
                    char next = input[i + 1];
                    if (next == '[' || next == ']' || next == '<' || next == '>' || next == '\\')
                    {
                        cleanText.Append(next);
                        visibleCount++;
                        i += 2;
                        continue;
                    }
                }

                if (c == '[')
                {
                    int closeIdx = input.IndexOf(']', i + 1);
                    if (closeIdx > i)
                    {
                        var body = input.Substring(i + 1, closeIdx - i - 1);
                        var parsed = s_TagSyntax.Match(body);
                        if (parsed.Success)
                        {
                            var tagName = parsed.Groups["name"].Value;
                            bool isClose = parsed.Groups["close"].Length > 0;

                            if (IsKnownTag(tagName))
                            {
                                if (isClose)
                                {
                                    CloseSpan(tagName, openStack, spans, visibleCount);
                                }
                                else
                                {
                                    var attrs = ParseAttributes(parsed.Groups["attrs"].Value);
                                    openStack.Push(new OpenSpan(tagName, visibleCount, attrs));
                                }
                                i = closeIdx + 1;
                                continue;
                            }
                        }
                    }

                    cleanText.Append('[');
                    visibleCount++;
                    i++;
                    continue;
                }

                if (c == '<')
                {
                    int gtIdx = input.IndexOf('>', i + 1);
                    if (gtIdx > i)
                    {
                        cleanText.Append(input, i, gtIdx - i + 1);
                        i = gtIdx + 1;
                        continue;
                    }
                }

                cleanText.Append(c);
                visibleCount++;
                i++;
            }

            while (openStack.Count > 0)
            {
                var open = openStack.Pop();
                CLogger.LogWarn(
                    $"RichTextTagParser: unclosed tag '[{open.TagName}]' extended to end of text.",
                    LogTag.UI
                );
                spans.Add(
                    new EffectSpan(
                        open.StartVisibleIndex,
                        visibleCount,
                        open.TagName,
                        open.Attributes
                    )
                );
            }

            return new ParseResult(cleanText.ToString(), spans);
        }

        private static void CloseSpan(
            string tagName,
            Stack<OpenSpan> openStack,
            List<EffectSpan> spans,
            int visibleCount
        )
        {
            if (!StackContainsTag(openStack, tagName))
            {
                CLogger.LogWarn(
                    $"RichTextTagParser: closing tag '[/{tagName}]' has no matching opener; ignored.",
                    LogTag.UI
                );
                return;
            }

            while (openStack.Count > 0)
            {
                var top = openStack.Pop();
                spans.Add(
                    new EffectSpan(top.StartVisibleIndex, visibleCount, top.TagName, top.Attributes)
                );
                if (top.TagName == tagName)
                {
                    return;
                }
                CLogger.LogWarn(
                    $"RichTextTagParser: implicit close of '[{top.TagName}]' due to outer '[/{tagName}]'.",
                    LogTag.UI
                );
            }
        }

        private static bool StackContainsTag(Stack<OpenSpan> stack, string tagName)
        {
            foreach (var s in stack)
            {
                if (s.TagName == tagName)
                {
                    return true;
                }
            }
            return false;
        }

        private static IReadOnlyDictionary<string, string> ParseAttributes(string attrString)
        {
            if (string.IsNullOrWhiteSpace(attrString))
            {
                return s_EmptyAttributes;
            }

            var dict = new Dictionary<string, string>();
            foreach (Match m in s_AttrSyntax.Matches(attrString))
            {
                dict[m.Groups["key"].Value] = m.Groups["value"].Value;
            }
            return dict.Count == 0 ? s_EmptyAttributes : dict;
        }

        private readonly struct OpenSpan
        {
            public OpenSpan(
                string tagName,
                int startVisibleIndex,
                IReadOnlyDictionary<string, string> attributes
            )
            {
                TagName = tagName;
                StartVisibleIndex = startVisibleIndex;
                Attributes = attributes;
            }

            public string TagName { get; }
            public int StartVisibleIndex { get; }
            public IReadOnlyDictionary<string, string> Attributes { get; }
        }

        private static readonly Regex s_TagSyntax = new(
            @"^(?<close>/)?(?<name>[A-Za-z][\w-]*)(?<attrs>.*)$",
            RegexOptions.Compiled
        );

        private static readonly Regex s_AttrSyntax = new(
            @"(?<key>[\w-]+)\s*=\s*""(?<value>[^""]*)""",
            RegexOptions.Compiled
        );

        private static bool IsKnownTag(string tagName)
        {
            return s_ReservedTags.Contains(tagName)
                || CharacterEffectRegistry.IsRegistered(tagName);
        }

        private static readonly HashSet<string> s_ReservedTags = new() { "typewriter" };

        private static readonly IReadOnlyDictionary<string, string> s_EmptyAttributes =
            new Dictionary<string, string>();
    }
}
