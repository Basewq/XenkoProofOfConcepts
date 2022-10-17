using DialogueTextControlExample.UI.Renderers;
using System.Collections.Generic;
using System.Globalization;

namespace DialogueTextControlExample.UI.Dialogue.TextEffects
{
    public class HeatWaveTextEffect : DialogueTextEffectBase
    {
        public const string TagName = "heatwave";

        public float Amplitude = 1;
        /// <summary>
        /// The number of times per second a wave cycles left and right.
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

        public override bool IsShaderEffect => true;

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
        }

        public override void SetShaderData(ref TextEffectData textEffectData)
        {
            textEffectData.ShaderTextEffectType = ShaderTextEffectType.HeatWave;
            textEffectData.HeatWaveAmplitude = Amplitude;
            textEffectData.HeatWaveFrequency = Frequency;
            textEffectData.HeatWavePeriodScale = PeriodScale;
        }
    }
}
