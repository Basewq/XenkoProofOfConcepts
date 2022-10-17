using DialogueTextControlExample.UI.Dialogue.TextEffects;
using System.Collections.Generic;
using System.Text;

namespace DialogueTextControlExample.UI.Dialogue
{
    public class DialogueTextBuilder
    {
        public readonly List<DialogueTextGlyph> Glyphs = new();
        public int CurrentLineIndex = 0;
        public int CurrentTextIndex = 0;

        public readonly List<DialogueTextEffectBase> TextEffects = new();

        public void AddGylph(DialogueTextGlyph glyph)
        {
            glyph.LineIndex = CurrentLineIndex;
            glyph.TextIndex = CurrentTextIndex;
            CurrentTextIndex++;
            glyph.GlyphIndex = Glyphs.Count;

            Glyphs.Add(glyph);
        }

        public void SetNewLine()
        {
            CurrentLineIndex++;
            CurrentTextIndex = 0;
        }

        public string GetDisplayText()
        {
            // This method is only for debug purposes
            var stringBuilder = new StringBuilder();

            int currentLineIndex = 0;
            foreach (var glyph in Glyphs)
            {
                int glpyhLineIndex = glyph.LineIndex;
                if (currentLineIndex != glpyhLineIndex)
                {
                    stringBuilder.Append('\n');
                    currentLineIndex = glpyhLineIndex;
                }

                stringBuilder.Append(glyph.Character);
            }

            return stringBuilder.ToString();
        }

        public void Reset()
        {
            Glyphs.Clear();
            CurrentLineIndex = 0;
            CurrentTextIndex = 0;

            TextEffects.Clear();
        }
    }
}
