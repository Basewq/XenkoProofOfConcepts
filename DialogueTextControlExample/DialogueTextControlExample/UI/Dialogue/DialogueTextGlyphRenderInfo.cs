using DialogueTextControlExample.UI.Controls;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Font;

namespace DialogueTextControlExample.UI.Dialogue
{
    public class DialogueTextGlyphRenderInfo
    {
        public DialogueTextGlyph TextGlyph;
        public DialogueText TextControl;

        /// <summary>
        /// True if this character should be rendered.
        /// If false, it will still take up space.
        /// </summary>
        public bool IsVisible;

        /// <summary>
        /// True if this is a whitespace character (ie. not renderable), but will still take up space.
        /// </summary>
        public bool IsWhitespace;

        /// <summary>
        /// Tells the renderer to ignore this glyph completely (will not take up space).
        /// </summary>
        internal bool IgnoreRender;

        public Color TextColor;

        //public float TextSize;

        public bool IsBold;
        public bool IsItalic;

        /// <summary>
        /// The index of the line this character belongs to, when rendered on screen.
        /// <br />
        /// eg. <see cref="LineIndex"/> = 0 means it's part of the text on the first line.
        /// </summary>
        /// <remarks>
        /// When <see cref="DialogueText.WrapText"/> is true, this may differ from <see cref="DialogueTextGlyph.LineIndex"/>.
        /// </remarks>
        public int LineIndex { get; internal set; }
        /// <summary>
        /// The index of the character on the text line, when rendered on screen.
        /// <br />
        /// eg. <see cref="TextIndex"/> = 0 means it's the first character of the line text.
        /// </summary>
        /// <remarks>
        /// When <see cref="DialogueText.WrapText"/> is true, this may differ from <see cref="DialogueTextGlyph.LineIndex"/>.
        /// </remarks>
        public int TextIndex { get; internal set; }

        /// <summary>
        /// Unit of measure is in the SpriteFont's glyph space.
        /// Do not overwrite with a TextEffect, use <see cref="PositionOffsetX"/> instead.
        /// </summary>
        public float PositionX { get; internal set; }
        /// <summary>
        /// Unit of measure is in the SpriteFont's glyph space.
        /// Do not overwrite with a TextEffect, use <see cref="PositionOffsetY"/> instead.
        /// </summary>
        public float PositionY { get; internal set; }

        public float PositionOffsetX;
        public float PositionOffsetY;

        internal Vector2 AuxiliaryScaling;

        /// <summary>
        /// Each glyph that does not use DialogueText.Font should set this
        /// </summary>
        internal SpriteFont SpriteFont;
        internal Glyph SpriteFontGlyph;

        /// <summary>
        /// True if the renderer needs to re-initialize the default settings & text effects on this glyph.
        /// </summary>
        internal bool IsNew;

        public void Reset()
        {
            TextGlyph = null;
            TextControl = null;
            IsVisible = false;
            IsWhitespace = false;
            TextColor = default;
            //TextSize = 0;
            LineIndex = 0;
            TextIndex = 0;
            PositionX = 0;
            PositionY = 0;
            PositionOffsetX = 0;
            PositionOffsetY = 0;
            AuxiliaryScaling = default;
            SpriteFont = null;
            SpriteFontGlyph = null;
            IsNew = true;
        }
    }
}
