using DialogueTextControlExample.UI.Dialogue.TextEffects;
using System;
using System.Collections.Generic;

namespace DialogueTextControlExample.UI.Dialogue
{
    public static class DialogueTextEffectRegistry
    {
        delegate DialogueTextEffectBase CreateTextEffect();

        private readonly static Dictionary<string, CreateTextEffect> _effectTagNameToFactory = new(StringComparer.OrdinalIgnoreCase);

        static DialogueTextEffectRegistry()
        {
            // Hardcoded way of registering text effects
            // You could change this to use Dictionary.Add so it'll crash if you try to add a duplicate key
            _effectTagNameToFactory[BoldTextEffect.TagName] = () => new BoldTextEffect();
            _effectTagNameToFactory[ItalicTextEffect.TagName] = () => new ItalicTextEffect();
            _effectTagNameToFactory[ColorTextEffect.TagName] = () => new ColorTextEffect();
            _effectTagNameToFactory[WaveTextEffect.TagName] = () => new WaveTextEffect();
            _effectTagNameToFactory[PauseTextEffect.TagName] = () => new PauseTextEffect();
        }

        public static bool TryCreate(string tagName, Dictionary<string, string> properties, out DialogueTextEffectBase textEffect)
        {
            if (_effectTagNameToFactory.TryGetValue(tagName, out var createTextEffectFunc))
            {
                textEffect = createTextEffectFunc();
                textEffect.SetProperties(properties);
                return true;
            }
            textEffect = null;
            return false;
        }
    }
}
