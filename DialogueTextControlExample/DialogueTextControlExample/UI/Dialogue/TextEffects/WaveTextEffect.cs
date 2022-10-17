using Stride.Core.Mathematics;
using Stride.Games;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DialogueTextControlExample.UI.Dialogue.TextEffects
{
    public class WaveTextEffect : DialogueTextEffectBase
    {
        public const string TagName = "wave";

        public float Amplitude = 1;
        /// <summary>
        /// The number of times per second a wave cycles up and down.
        /// <br />
        /// By default, a wave cycles once per second.
        /// </summary>
        public float Frequency = 1;
        /// <summary>
        /// The scale multiplier of the wave period.
        /// <br />
        /// eg. 2 means there are twice as many waves compared to the default (ie. waves appear narrower),
        /// 0.5 means half as many (waves appear wider), zero is effectively flat.
        /// <br />
        /// By default, one period is 10 * DialogueText.ActualTextSize.
        /// </summary>
        public float PeriodScale = 1;
        /// <summary>
        /// Shift the wave, to the left, as a multiple of <see cref="MathUtil.TwoPi"/>.
        /// <br />
        /// eg. 0.25 means shifting the start of the wave by 90 degrees (ie. effectively a cosine wave).
        /// </summary>
        public float PhaseShift = 0;
        /// <summary>
        /// The direction of the wave peaks.
        /// </summary>
        public WaveDirection Direction = WaveDirection.Right;

        public override void SetProperties(Dictionary<string, string> properties)
        {
            string valueText;
            if (properties.TryGetValue("amp", out valueText))
            {
                if (float.TryParse(valueText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float value))
                {
                    Amplitude = value;
                }
            }
            if (properties.TryGetValue("freq", out valueText))
            {
                if (float.TryParse(valueText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float value))
                {
                    Frequency = value;
                }
            }
            if (properties.TryGetValue("per", out valueText))
            {
                if (float.TryParse(valueText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float value))
                {
                    PeriodScale = value;
                }
            }
            if (properties.TryGetValue("shift", out valueText))
            {
                if (float.TryParse(valueText, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out float value))
                {
                    PhaseShift = value;
                }
            }
            if (properties.TryGetValue("dir", out valueText))
            {
                if ("right".Equals(valueText, StringComparison.OrdinalIgnoreCase))
                {
                    Direction = WaveDirection.Right;
                }
                else if ("left".Equals(valueText, StringComparison.OrdinalIgnoreCase))
                {
                    Direction = WaveDirection.Left;
                }
            }
        }

        public override void Update(GameTime time, DialogueTextGlyphRenderInfo glyphRenderInfo)
        {
            var fontSize = glyphRenderInfo.TextControl.ActualTextSize;
            var dt = (float)time.Total.TotalSeconds;
            float waveDir = (Direction == WaveDirection.Right) ? -1 : 1;
            float startEffectGlyphPositionX = 0;
            if (GlyphStartIndex < glyphRenderInfo.TextControl.TextGlyphRenderInfos.Count)
            {
                // This is to make the start of the sine wave at zero for the first character of the effect.
                // The only real purpose for this is if you make Frequency = 0 (ie. a static wave)
                startEffectGlyphPositionX = glyphRenderInfo.TextControl.TextGlyphRenderInfos[GlyphStartIndex].PositionX;
            }
            float relativePosition = glyphRenderInfo.PositionX - startEffectGlyphPositionX;
            // One wave is MathUtil.TwoPi, and this TextEffect will arbitrarily pick (10 * fontSize) as the default length of the wave.
            // For Arial font, this is just under 9 'O' characters.
            float periodLength = PeriodScale * MathUtil.TwoPi  / (10 * fontSize);
            float phaseShift = waveDir * periodLength * relativePosition;
            phaseShift -= MathUtil.TwoPi * PhaseShift;  // Additional shift to the left (for positive shift values)
            float amp = Amplitude * fontSize * 0.5f;
            glyphRenderInfo.PositionOffsetY += amp * MathF.Sin(Frequency * dt * MathUtil.TwoPi + phaseShift);
        }

        public enum WaveDirection
        {
            /// <summary>
            /// Moves to the right, ie. from left to right.
            /// </summary>
            Right,
            /// <summary>
            /// Moves to the left, ie. from right to left.
            /// </summary>
            Left,
        }
    }
}
