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
        /// The number of times per second a wave cycles.
        /// </summary>
        public float Frequency = 1;
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
            // A full sine wave cycle = 2 * Pi = 6.28, therefore if an 'O' character is approximately
            // equal width and height, and has a height approximately equal to fontSize,
            // one 'O' character is:
            // OCharWidth / fontSize = 1
            // So a full sine wave cycle is about 6.28 'O' across by default
            // (this is only an approximation to get an idea how wide the wave should appear).
            var fontSize = glyphRenderInfo.TextControl.ActualTextSize;
            var dt = (float)time.Total.TotalSeconds;
            float waveDir = (Direction == WaveDirection.Right) ? -1 : 1;
            float phaseShift = waveDir * glyphRenderInfo.PositionX / fontSize;
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
