using Stride.Core.Mathematics;

namespace DialogueTextControlExample.UI.Dialogue
{
    public class DialogueTextGlyph
    {
        public char Character;

        /// <summary>
        /// The index of the line this character belongs to.
        /// <br />
        /// eg. <see cref="LineIndex"/> = 0 means it's part of the text on the first line.
        /// </summary>
        public int LineIndex;
        /// <summary>
        /// The index of the character on the text line.
        /// <br />
        /// eg. <see cref="TextIndex"/> = 0 means it's the first character of the line text.
        /// </summary>
        public int TextIndex;

        public int WrappedLineIndex;
        public int WrappedTextIndex;

        /// <summary>
        /// The index of the character relative to the entire glyph list.
        /// Note that '\n' is NOT counted, as it is not part of the glyph list.
        /// </summary>
        public int GlyphIndex;

        public DialogueTextGlyph(char character)
        {
            Character = character;
        }
    }
}
