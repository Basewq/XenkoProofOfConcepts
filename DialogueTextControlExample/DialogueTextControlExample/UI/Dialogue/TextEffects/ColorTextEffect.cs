using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;

namespace DialogueTextControlExample.UI.Dialogue.TextEffects
{
    public class ColorTextEffect : DialogueTextEffectBase
    {
        public const string TagName = "color";

        public Color TextColor = Color.White;

        public override void SetProperties(Dictionary<string, string> properties)
        {
            if (properties.TryGetValue("", out string valueText))   // Implicit key
            {
                // Lazy proof of concept, only accepting rgb as key names.
                // Should adapt to allow more color names
                if (ColorExtensions.CanConvertStringToRgba(valueText))
                {
                    uint colorValue = ColorExtensions.StringToRgba(valueText);
                    TextColor = Color.FromRgba(colorValue);
                }
                else if ("red".Equals(valueText, StringComparison.OrdinalIgnoreCase))
                {
                    TextColor = Color.Red;
                }
                else if ("blue".Equals(valueText, StringComparison.OrdinalIgnoreCase))
                {
                    TextColor = Color.Blue;
                }
                else if ("green".Equals(valueText, StringComparison.OrdinalIgnoreCase))
                {
                    TextColor = Color.Green;
                }
            }
        }

        public override void Initialize(DialogueTextGlyphRenderInfo glyphRenderInfo)
        {
            glyphRenderInfo.TextColor = TextColor;
        }
    }
}
