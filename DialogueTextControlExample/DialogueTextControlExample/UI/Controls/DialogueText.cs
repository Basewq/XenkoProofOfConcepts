using DialogueTextControlExample.UI.Dialogue;
using DialogueTextControlExample.UI.Dialogue.TextEffects;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DialogueTextControlExample.UI.Controls
{
    public delegate void TextCharacterAppearedEventHandler(DialogueTextGlyph textGlyph);

    [DataContract]
    [DataContractMetadataType(typeof(DialogueTextMetadata))]
    public class DialogueText : TextBlock
    {
        private const bool DefaultWrapText = true;
        private const float DefaultCharacterAppearanceSpeed = 20;

        private bool _isPlayingTextDisplay = false;
        private TimeSpan _timeToDisplayNextGlyph;
        private TimeSpan _nextGlyphDisplayTimeRemaining;
        private int _currentGlyphIndex = -1;

        public event TextCharacterAppearedEventHandler TextCharacterAppeared;

        [DataMember(order: 10)]
        [Display(category: AppearanceCategory)]
        public SpriteFont BoldFont { get; set; }
        [DataMember(order: 11)]
        [Display(category: AppearanceCategory)]
        public SpriteFont ItalicFont { get; set; }
        [DataMember(order: 12)]
        [Display(category: AppearanceCategory)]
        public SpriteFont BoldAndItalicFont { get; set; }

        private float _characterAppearanceSpeed;
        /// <summary>
        /// Number of characters to appear per second.
        /// </summary>
        [DataMember(order: 20)]
        [Display(category: BehaviorCategory)]
        [DefaultValue(DefaultCharacterAppearanceSpeed)]
        public float CharacterAppearanceSpeed
        {
            get => _characterAppearanceSpeed;
            set
            {
                if (value == 0 || float.IsNaN(value))
                {
                    // Ignore
                    return;
                }
                _characterAppearanceSpeed = value;
                _timeToDisplayNextGlyph = TimeSpan.FromSeconds(1 / value);
            }
        }

        private bool _displayTextImmediately = false;
        [DataMember(order: 21)]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool DisplayTextImmediately
        {
            get => _displayTextImmediately;
            set
            {
                _displayTextImmediately = value;
                if (_displayTextImmediately && _currentGlyphIndex < 0 && !string.IsNullOrWhiteSpace(Text))
                {
                    DisplayAllText();
                }
            }
        }

        private readonly DialogueTextParser _parser = new();
        private DialogueTextBuilder _textBuilder;
        [DataMemberIgnore]
        public List<DialogueTextGlyph> TextGlyphs => _textBuilder?.Glyphs;
        /// <summary>
        /// The list maps one-to-one with <see cref="TextGlyphs"/>.
        /// This is to be used by <see cref="Renderers.DialogueTextRenderer"/>.
        /// </summary>
        [DataMemberIgnore]
        public List<DialogueTextGlyphRenderInfo> TextGlyphRenderInfos { get; } = new();
        [DataMemberIgnore]
        public List<DialogueTextEffectBase> TextEffects => _textBuilder?.TextEffects;

        public DialogueText()
        {
            WrapText = DefaultWrapText;    // Warning: this must also match in DialogueTextMetadata
            CharacterAppearanceSpeed = DefaultCharacterAppearanceSpeed;
        }

        private bool _rebuildDisplayTextRequired = true;
        private string _displayText;

        /// <summary>
        /// Not used for rendering, only for debug purposes.
        /// Use <see cref="TextGlyphs"/> instead.
        /// </summary>
        public override string TextToDisplay
        {
            get
            {
                if (_rebuildDisplayTextRequired)
                {
                    var stringBuilder = new StringBuilder();

                    int currentLineIndex = 0;
                    foreach (var rendInfo in TextGlyphRenderInfos)
                    {
                        var glyph = rendInfo.TextGlyph;
                        int glpyhLineIndex = glyph.LineIndex;
                        if (currentLineIndex != glpyhLineIndex)
                        {
                            stringBuilder.Append('\n');
                            currentLineIndex = glpyhLineIndex;
                        }

                        stringBuilder.Append(glyph.Character);
                    }

                    _displayText = stringBuilder.ToString();
                    _rebuildDisplayTextRequired = false;
                }
                return _displayText;
            }
        }

        public LayoutingContext GetLayoutingContext()
        {
            return LayoutingContext;    // Expose publicly so our renderer can access it.
        }

        public SpriteFont GetFont(bool isBold, bool isItalic)
        {
            if (isBold && isItalic)
            {
                return BoldAndItalicFont ?? Font;
            }
            else if (isBold)
            {
                return BoldFont ?? Font;
            }
            else if (isItalic)
            {
                return ItalicFont ?? Font;
            }
            return Font;
        }

        public void ResetTextAppearance()
        {
            ResetTextAppearanceInternal(updateRenderInfos: true);
        }

        private void ResetTextAppearanceInternal(bool updateRenderInfos)
        {
            _nextGlyphDisplayTimeRemaining = _timeToDisplayNextGlyph;
            _currentGlyphIndex = -1;
            if (updateRenderInfos)
            {
                foreach (var rendInfo in TextGlyphRenderInfos)
                {
                    rendInfo.IsVisible = false;
                }
            }
        }

        public void PlayTextDisplay()
        {
            ResetTextAppearance();
            _isPlayingTextDisplay = true;
        }

        public void SetTextDisplayDelay(TimeSpan time)
        {
            _nextGlyphDisplayTimeRemaining = time;
        }

        public void DisplayAllText()
        {
            for (int i = 0; i < TextGlyphRenderInfos.Count; i++)
            {
                var rendInfo = TextGlyphRenderInfos[i];
                if (!rendInfo.IsVisible)
                {
                    SetRenderInfoVisible(rendInfo);

                }
                _currentGlyphIndex = TextGlyphRenderInfos.Count;
                _isPlayingTextDisplay = false;
            }
        }

        private void SetRenderInfoVisible(DialogueTextGlyphRenderInfo rendInfo)
        {
            rendInfo.IsVisible = true;
            TextCharacterAppeared?.Invoke(rendInfo.TextGlyph);
            if (_textBuilder.TextEffects != null)
            {
                foreach (var txtFx in _textBuilder.TextEffects)
                {
                    if (txtFx.IsIndexInRange(_currentGlyphIndex))
                    {
                        txtFx.OnCharacterAppear(rendInfo);
                    }
                }
            }
        }

        internal void UpdateTextEffects(GameTime time)
        {
            if (_isPlayingTextDisplay)
            {
                _nextGlyphDisplayTimeRemaining -= time.Elapsed;
                if (_nextGlyphDisplayTimeRemaining <= TimeSpan.Zero)
                {
                    _nextGlyphDisplayTimeRemaining += _timeToDisplayNextGlyph;
                    _currentGlyphIndex++;
                    while (_currentGlyphIndex >= 0 && _currentGlyphIndex < TextGlyphRenderInfos.Count)
                    {
                        var rendInfo = TextGlyphRenderInfos[_currentGlyphIndex];
                        SetRenderInfoVisible(rendInfo);
                        if (rendInfo.IgnoreRender)
                        {
                            // Note we skip non-renderable glyphs (eg. leading/trailing whitespaces that were trimmed due to word-wrap)
                            _currentGlyphIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (_currentGlyphIndex < 0)
            {
                // Nothing is currently showing
                return;
            }

            var textEffects = _textBuilder?.TextEffects;
            if (textEffects != null && textEffects.Count == 0)
            {
                return;
            }
            foreach (var rendInfo in TextGlyphRenderInfos)
            {
                var glyph = rendInfo.TextGlyph;
                // Always reset position offsets
                rendInfo.PositionOffsetX = 0;
                rendInfo.PositionOffsetY = 0;

                // Apply one-off effects to new glyphs
                if (rendInfo.IsNew && !rendInfo.IgnoreRender)
                {
                    foreach (var txtFx in textEffects)
                    {
                        if (txtFx.IsIndexInRange(glyph.GlyphIndex))
                        {
                            txtFx.PrepareForNewEffect(rendInfo);
                        }
                    }
                    rendInfo.IsNew = false;
                }
            }

            // Apply update effects to all glyphs
            foreach (var txtFx in textEffects)
            {
                foreach (var rendInfo in TextGlyphRenderInfos)
                {
                    var glyph = rendInfo.TextGlyph;
                    if (!rendInfo.IgnoreRender && txtFx.IsIndexInRange(glyph.GlyphIndex))
                    {
                        txtFx.Update(time, rendInfo);
                    }
                }
            }
        }

        protected override void OnTextChanged()
        {
            _textBuilder = _parser.Parse(Text);
            RebuildRenderInfos();
            _rebuildDisplayTextRequired = true;
            if (DisplayTextImmediately)
            {
                DisplayAllText();
            }

            base.OnTextChanged();
        }

        private void RebuildRenderInfos()
        {
            ResetTextAppearanceInternal(updateRenderInfos: false);

            var textGlyphs = TextGlyphs;
            if (textGlyphs == null)
            {
                TextGlyphRenderInfos.Clear();
                return;
            }
            int rendInfoCountDiff = textGlyphs.Count - TextGlyphRenderInfos.Count;
            if (rendInfoCountDiff > 0)
            {
                TextGlyphRenderInfos.EnsureCapacity(rendInfoCountDiff);
                for (int i = 0; i < rendInfoCountDiff; i++)
                {
                    TextGlyphRenderInfos.Add(new DialogueTextGlyphRenderInfo());
                }
            }
            else if (rendInfoCountDiff < 0)
            {
                int removeCount = -rendInfoCountDiff;
                TextGlyphRenderInfos.RemoveRange(TextGlyphRenderInfos.Count - removeCount, removeCount);
            }
            // Initialize all fields
            for (int i = 0; i < textGlyphs.Count; i++)
            {
                var txtGlyph = textGlyphs[i];
                var rendInfo = TextGlyphRenderInfos[i];
                rendInfo.Reset();
                rendInfo.TextGlyph = txtGlyph;
                rendInfo.TextControl = this;
                rendInfo.TextColor = TextColor;
                //rendInfo.TextSize = ActualTextSize;
                rendInfo.IsWhitespace = char.IsWhiteSpace(txtGlyph.Character);
                // UpdateWrappedText will overwrite these when called.
                rendInfo.LineIndex = txtGlyph.LineIndex;
                rendInfo.TextIndex = txtGlyph.TextIndex;

                foreach (var txtFx in _textBuilder.TextEffects)
                {
                    if (txtFx.IsIndexInRange(txtGlyph.GlyphIndex))
                    {
                        txtFx.Initialize(rendInfo);
                    }
                }
            }
        }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            if (WrapText)
            {
                UpdateWrappedText(finalSizeWithoutMargins);
            }

            return base.ArrangeOverride(finalSizeWithoutMargins);
        }

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            if (WrapText)
            {
                UpdateWrappedText(availableSizeWithoutMargins);
            }

            return new Vector3(CalculateTextSize(), 0);
        }

        private void UpdateWrappedText(Vector3 availableSpace)
        {
            // Code is mostly adapted from Stride's existing code
            // TextBlock.cs

            var glyphs = TextGlyphs;
            if ((glyphs?.Count ?? 0) == 0)
            {
                return;
            }
            var glyphRenderInfos = TextGlyphRenderInfos;
            foreach (var rendInfo in glyphRenderInfos)
            {
                // Because there might be leading or trailing whitespaces, so just set all to ignore render,
                // then enable all visible characters when we build the lines.
                // We don't truly remove them because TextEffects may still be relevant to them
                // (or is within range of a TextEffect).
                rendInfo.IgnoreRender = true;
            }

            float availableWidth = availableSpace.X;
            var currentLineGlyphRenderInfos = new List<DialogueTextGlyphRenderInfo>(glyphs.Count);
            int currentWrappedLineIndex = 0;
            var currentLine = new StringBuilder(glyphs.Count);
            //var currentText = new StringBuilder(2 * glyphs.Count);

            int currentIndexOfUnwrappedNewLine = 0;
            int indexOfNewLine = 0;
            while (true)
            {
                float lineCurrentSize = 0;
                int indexNextCharacter = 0;
                int indexOfLastSpace = -1;

                while (true)
                {
                    lineCurrentSize = CalculateTextSize(currentLine).X;

                    if (lineCurrentSize > availableWidth || indexOfNewLine + indexNextCharacter >= glyphs.Count)
                    {
                        break;
                    }

                    var currentGlyph = glyphs[indexOfNewLine + indexNextCharacter];
                    var curGlyphRendInfo = glyphRenderInfos[indexOfNewLine + indexNextCharacter];
                    char currentCharacter = currentGlyph.Character;

                    if (currentGlyph.LineIndex != currentIndexOfUnwrappedNewLine)
                    {
                        currentIndexOfUnwrappedNewLine = currentGlyph.LineIndex;
                        indexOfNewLine += indexNextCharacter;
                        goto AppendLine;
                    }

                    currentLineGlyphRenderInfos.Add(curGlyphRendInfo);
                    currentLine.Append(currentCharacter);

                    if (char.IsWhiteSpace(currentCharacter))
                    {
                        indexOfLastSpace = indexNextCharacter;
                    }

                    indexNextCharacter++;
                }

                if (lineCurrentSize <= availableWidth) // we reached the end of the text.
                {
                    // append the final part of the text and quit the main loop
                    //currentText.Append(currentLine);
                    UpdateWrappedTextIndices(currentLineGlyphRenderInfos, currentWrappedLineIndex);
                    break;
                }

                // we reached the end of the line.
                if (indexOfLastSpace < 0) // no space in the line
                {
                    // remove last extra character
                    currentLine.Remove(currentLine.Length - 1, 1);
                    //currentLineGlyphRenderInfos.RemoveAt(currentLine.Length - 1);     // Don't need to remove glyphs
                    indexOfNewLine += indexNextCharacter - 1;
                }
                else // at least one white space in the line
                {
                    // remove all extra characters until last space (included)
                    if (indexNextCharacter > indexOfLastSpace)
                    {
                        currentLine.Remove(indexOfLastSpace, indexNextCharacter - indexOfLastSpace);
                        //currentLineGlyphRenderInfos.RemoveRange(indexOfLastSpace, indexNextCharacter - indexOfLastSpace);     // Don't need to remove glyphs
                    }
                    indexOfNewLine += indexOfLastSpace + 1;
                }

            AppendLine:

                // add the next line to the current text
                //currentLine.Append('\n');
                //currentText.Append(currentLine);

                // reset current line
                currentLine.Clear();
                UpdateWrappedTextIndices(currentLineGlyphRenderInfos, currentWrappedLineIndex);
                currentLineGlyphRenderInfos.Clear();
                currentWrappedLineIndex++;
            }

            static void UpdateWrappedTextIndices(List<DialogueTextGlyphRenderInfo> currentLineGlyphRenderInfos, int currentWrappedLineIndex)
            {
                for (int i = 0; i < currentLineGlyphRenderInfos.Count; i++)
                {
                    var rendInfo = currentLineGlyphRenderInfos[i];
                    rendInfo.TextIndex = i;
                    rendInfo.LineIndex = currentWrappedLineIndex;
                    rendInfo.IgnoreRender = false;
                }
            }
        }

        private Vector2 CalculateTextSize(StringBuilder textToMeasureStringBuilder)
        {
            if (Font == null)
            {
                return Vector2.Zero;
            }

            var sizeRatio = LayoutingContext.RealVirtualResolutionRatio;
            var measureFontSize = new Vector2(sizeRatio.Y * ActualTextSize); // we don't want letters non-uniform ratio
            var realSize = Font.MeasureString(textToMeasureStringBuilder, ref measureFontSize);

            // force pre-generation if synchronous generation is required
            if (SynchronousCharacterGeneration)
            {
                var textToMeasure = textToMeasureStringBuilder.ToString();
                Font.PreGenerateGlyphs(textToMeasure, measureFontSize);
            }

            if (Font.FontType == SpriteFontType.Dynamic)
            {
                // rescale the real size to the virtual size
                realSize.X /= sizeRatio.X;
                realSize.Y /= sizeRatio.Y;
            }
            else if (Font.FontType == SpriteFontType.SDF)
            {
                var scaleRatio = ActualTextSize / Font.Size;
                realSize.X *= scaleRatio;
                realSize.Y *= scaleRatio;
            }

            return realSize;
        }

        private class DialogueTextMetadata
        {
            [DefaultValue(DefaultWrapText)]
            public bool WrapText { get; }
        }
    }
}
