using System;
using System.Collections.Generic;
using System.Globalization;

namespace DialogueTextControlExample.UI.Dialogue.TextEffects
{
    public class PauseTextEffect : DialogueTextEffectBase
    {
        public const string TagName = "pause";

        public float DelayInSeconds = 0.5f;

        /// <summary>
        /// Only applicable when <see cref="DialogueTextEffectBase.AffectedGlyphsCount"/> is greater than one.
        /// True means the first char within the &lt;pause&gt; tag is timed to display as per
        /// <see cref="Controls.DialogueText.CharacterAppearanceSpeed"/>, then subsequent chars are delayed
        /// by <see cref="DelayInSeconds"/>, and false means the first char is affected by <see cref="DelayInSeconds"/>.
        /// </summary>
        public bool SkipDelayOnFirstChar = true;

        public override void SetProperties(Dictionary<string, string> properties)
        {
            string valueText;
            if (properties.TryGetValue("", out valueText))   // Implicit key
            {
                if (float.TryParse(valueText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float value))
                {
                    DelayInSeconds = value;
                }
            }
            if (properties.TryGetValue("skip1", out valueText))
            {
                if (bool.TryParse(valueText, out bool value))
                {
                    SkipDelayOnFirstChar = value;
                }
            }
        }

        public override void OnCharacterAppear(DialogueTextGlyphRenderInfo glyphRenderInfo)
        {
            if (SkipDelayOnFirstChar && AffectedGlyphsCount > 1 && glyphRenderInfo.TextGlyph.GlyphIndex == GlyphStartIndex)
            {
                return;
            }
            var timeDelay = TimeSpan.FromSeconds(DelayInSeconds);
            glyphRenderInfo.TextControl.SetTextDisplayDelay(timeDelay);
        }
    }
}
