using DialogueTextControlExample.UI.Dialogue.TextEffects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DialogueTextControlExample.UI.Dialogue
{
    public class DialogueTextParser
    {
        public DialogueTextBuilder Parse(string dialogueText)
        {
            var dialogueBuilder = new DialogueTextBuilder();
            var pendingCloseTags = new List<TextTagBuilder>();

            for (int processCharIndex = 0; processCharIndex < dialogueText.Length; processCharIndex++)
            {
                int charsRead = ProcessCharacters(dialogueText, processCharIndex, dialogueBuilder, pendingCloseTags);
                processCharIndex += charsRead - 1;      // -1 because for-loop will increment by 1
            }
            //Debug.Assert(pendingCloseTags.Count == 0);

            return dialogueBuilder;
        }

        private int ProcessCharacters(
            string dialogueText, int processCharIndex,
            DialogueTextBuilder dialogueBuilder, List<TextTagBuilder> pendingCloseTags)
        {
            char ch = dialogueText[processCharIndex];
            switch (ch)
            {
                case '\n':
                    dialogueBuilder.SetNewLine();
                    return 1;
                case '\r':
                    // Do nothing
                    return 1;
                //case '\\':
                //    // Is this an escape character
                //    // TODO?
                //    return 1;
                case '<':
                    {
                        bool wasTagRead = TryReadTag(dialogueText, processCharIndex, dialogueBuilder, pendingCloseTags, out int charsRead);
                        if (wasTagRead)
                        {
                            return charsRead;
                        }
                        else
                        {
                            // Treat as normal text
                            AddCharacterAsGlyph(dialogueBuilder, pendingCloseTags, ch);
                            return 1;
                        }
                    }
                default:
                    AddCharacterAsGlyph(dialogueBuilder, pendingCloseTags, ch);
                    return 1;
            }

            static void AddCharacterAsGlyph(DialogueTextBuilder dialogueBuilder, List<TextTagBuilder> pendingCloseTags, char ch)
            {
                var glyph = new DialogueTextGlyph(ch);
                dialogueBuilder.AddGylph(glyph);
                foreach (var tag in pendingCloseTags)
                {
                    tag.TextEffect.AffectedGlyphsCount++;
                }
            }
        }

        private bool TryReadTag(string dialogueText, int processCharIndex,
            DialogueTextBuilder dialogueBuilder, List<TextTagBuilder> pendingCloseTags, out int charsRead)
        {
            var subText = dialogueText.AsSpan(processCharIndex);

            int tagEndBracketIndex = -1;
            // Get the closing bracket of the tag
            // Start at 1 to skip the first '<'
            for (int i = 1; i < subText.Length; i++)
            {
                char ch = subText[i];
                if (ch == '>')
                {
                    tagEndBracketIndex = i;
                    break;
                }
            }

            if (tagEndBracketIndex < 0)
            {
                charsRead = 0;
                return false;   // No name found, treat as normal text
            }

            var tagText = subText.Slice(0, length: tagEndBracketIndex + 1);
            if (tagText[1] == '/')
            {
                // This is a closing tag, eg. </b>
                for (int i = pendingCloseTags.Count - 1; i >= 0; i--)
                {
                    var tag = pendingCloseTags[i];
                    if (tag.CloseTagString.AsSpan().Equals(tagText, StringComparison.OrdinalIgnoreCase))
                    {
                        pendingCloseTags.RemoveAt(i);     // Finished
                        charsRead = tagText.Length;
                        return true;
                    }
                }
                // Unknown tag ending.
                //Debug.Fail($"Unknown tag ending '{tagText}'");
                charsRead = 0;
                return false;
            }

            var textTagBuilder = new TextTagBuilder();
            ReadOpenTagName(tagText, textTagBuilder);
            if (textTagBuilder.TagName.Length == 0)
            {
                charsRead = 0;
                return false;   // No name found, treat as normal text
            }

            if (textTagBuilder.IsSelfClosingTag)
            {
                // Tag will be in the form <tagname attr1="val1" />
                int tagAttrStartIdx = 1 + textTagBuilder.TagName.Length;
                int tagAttrLength = (tagText.Length - 3) - textTagBuilder.TagName.Length;   // 3 chars to exclude (the brackets and '/'), along with the tag name
                ReadTagAttributes(tagText.Slice(tagAttrStartIdx, tagAttrLength), textTagBuilder);
            }
            else
            {
                string closeTagString = $"</{textTagBuilder.TagName}>";
                textTagBuilder.CloseTagString = closeTagString;

                // Tag will be in the form <tagname attr1="val1" attr2="val2">
                int tagAttrStartIdx = 1 + textTagBuilder.TagName.Length;
                int tagAttrLength = (tagText.Length - 2) - textTagBuilder.TagName.Length;   // 2 chars to exclude (the brackets), along with the tag name
                ReadTagAttributes(tagText.Slice(tagAttrStartIdx, tagAttrLength), textTagBuilder);
            }
            if (DialogueTextEffectRegistry.TryCreate(textTagBuilder.TagName.ToString(), textTagBuilder.Attributes, out var textEffect))
            {
                if (!textTagBuilder.IsSelfClosingTag)
                {
                    pendingCloseTags.Add(textTagBuilder);
                }

                textTagBuilder.TextEffect = textEffect;
                textEffect.GlyphStartIndex = dialogueBuilder.Glyphs.Count;  // The will effect start for the next glyph
                textEffect.AffectedGlyphsCount = textTagBuilder.IsSelfClosingTag ? 1 : 0;   // Zero because we wait until we actually encounter subsequent glyphs
                dialogueBuilder.TextEffects.Add(textEffect);
            }
            else
            {
                //Debug.Fail($"Text Effect for tag <{textTagBuilder.TagName}> is not registered.");
                charsRead = 0;
                return false;
            }

            charsRead = tagText.Length;
            return true;
        }

        private void ReadTagAttributes(ReadOnlySpan<char> tagAttributeText, TextTagBuilder textTagBuilder)
        {
            int initialCharsRead = 0;
            if (tagAttributeText.Length > 0 && tagAttributeText[0] == '=')
            {
                // This is an implicit attribute value, eg. <color="red">
                string attrName = "";
                int charsRead = ReadAttributeValue(tagAttributeText.Slice(1), out string attrValueText);

                textTagBuilder.Attributes[attrName] = attrValueText;

                initialCharsRead += charsRead + 1;  // +1 because we skipped the '='
            }

            for (int curIdx = initialCharsRead; curIdx < tagAttributeText.Length; curIdx++)
            {
                if (char.IsWhiteSpace(tagAttributeText[curIdx]))
                {
                    continue;
                }

                int attrNameStartIndex = curIdx;
                for (int endStrIdx = curIdx; endStrIdx < tagAttributeText.Length; endStrIdx++)
                {
                    char ch = tagAttributeText[endStrIdx];
                    if (ch == '=')
                    {
                        string attrName = tagAttributeText.Slice(attrNameStartIndex, endStrIdx - attrNameStartIndex).ToString();
                        int charsRead = ReadAttributeValue(tagAttributeText.Slice(endStrIdx + 1), out string attrValueText);

                        textTagBuilder.Attributes[attrName] = attrValueText;    // Note that any duplicate attribute names will overwrite the previous

                        curIdx += attrName.Length + 1 + charsRead - 1;    // +1 for the '=' char and -1 because for-loop will increment by 1
                        break;
                    }
                }
            }
        }

        private int ReadAttributeValue(ReadOnlySpan<char> tagAttributeText, out string attrValueText)
        {
            for (int i = 0; i < tagAttributeText.Length; i++)
            {
                char ch = tagAttributeText[i];
                if (char.IsWhiteSpace(ch))
                {
                    var attrValue = tagAttributeText.Slice(0, i);
                    attrValueText = RemoveStringQuotes(attrValue).ToString();
                    return i;
                }
            }
            // Entire remaining text is the value
            attrValueText = RemoveStringQuotes(tagAttributeText).ToString();
            return tagAttributeText.Length;
        }

        private ReadOnlySpan<char> RemoveStringQuotes(ReadOnlySpan<char> stringSpan)
        {
            if (stringSpan.Length >= 2 && stringSpan[0] == '"' && stringSpan[stringSpan.Length - 1] == '"')
            {
                return stringSpan.Slice(1, stringSpan.Length - 2);
            }
            return stringSpan;
        }

        private static void ReadOpenTagName(ReadOnlySpan<char> tagText, TextTagBuilder textTagBuilderOutput)
        {
            var name = textTagBuilderOutput.TagName;
            // Start at 1 to skip the first '<' and skip the closing bracket
            for (int i = 1; i < tagText.Length - 1; i++)
            {
                char ch = tagText[i];
                if (char.IsWhiteSpace(ch))
                {
                    break;     // Finished with the name
                }
                else if (ch == '=')
                {
                    // Implicit attribute marker, eg. <color="blue">
                    break;     // Finished with the name
                }
                else if (ch == '/')
                {
                    break;     // Finished with the name
                }
                name.Append(ch);
            }
            // Check if tag ends with />
            int closingTagMarkerIndex = tagText.Length - 2;
            if (closingTagMarkerIndex > 0 && closingTagMarkerIndex < tagText.Length
                && tagText[closingTagMarkerIndex] == '/')
            {
                // Self closing tag, eg. <br />
                textTagBuilderOutput.IsSelfClosingTag = true;
            }
        }

        private class TextTagBuilder
        {
            /// <summary>
            /// Name of the tag (without the brackets).
            /// </summary>
            public readonly StringBuilder TagName = new();
            public bool IsSelfClosingTag = false;
            /// <summary>
            /// The string that identifies the closing tag, eg. </b>
            /// </summary>
            public string CloseTagString;
            /// <summary>
            /// Dictionary of AttributeName -> AttributeValue
            /// </summary>
            public readonly Dictionary<string, string> Attributes = new();

            public DialogueTextEffectBase TextEffect;

            public void Clear()
            {
                TagName.Clear();
                IsSelfClosingTag = false;
                CloseTagString = null;
                Attributes.Clear();
                TextEffect = null;
            }
        }
    }
}
