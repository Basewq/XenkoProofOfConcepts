using DialogueTextControlExample.UI.Renderers;
using System.Collections.Generic;

namespace DialogueTextControlExample.UI.Dialogue.TextEffects
{
    /// <summary>
    /// Derive from this class to create your own text effect.
    /// You will also need to register the effect in <see cref="DialogueTextEffectRegistry"/>
    /// for <see cref="DialogueTextParser"/> to recognize the effect tag.
    /// Be careful about what you set on <see cref="DialogueTextGlyphRenderInfo"/> since you
    /// might overlap on another text effect.
    /// </summary>
    public abstract class DialogueTextEffectBase
    {
        public int GlyphStartIndex;
        public int AffectedGlyphsCount;
        public int GlyphEndIndex => GlyphStartIndex + AffectedGlyphsCount - 1;

        public virtual bool IsShaderEffect => false;

        /// <summary>
        /// The key-value properties within the text effect tag.
        /// <br />
        /// eg. &lt;wave amp=0.75 freq=2&gt; has key-value properties "amp" -> "0.75" and "freq" -> "2"
        /// <br />
        /// Some tags may allow for a implicit property value, which has <see cref="string.Empty"/> as the key.
        /// <br />
        /// eg. &lt;color=red&gt; will have a key-value "" -> "red"
        /// </summary>
        public virtual void SetProperties(Dictionary<string, string> properties) { }

        /// <summary>
        /// Apply one-off effects to the glyph where applicable.
        /// Note that this is called just after generating the glyphs, so some fields
        /// are not set up yet (eg. PositionX/PositionY)
        /// </summary>
        public virtual void Initialize(DialogueTextGlyphRenderInfo glyphRenderInfo) { }

        /// <summary>
        /// Apply one-off effects to the glyph where applicable.
        /// This is called just before rendering, so all fields should be ready.
        /// </summary>
        public virtual void PrepareForNewEffect(DialogueTextGlyphRenderInfo glyphRenderInfo) { }

        public virtual void OnCharacterAppear(DialogueTextGlyphRenderInfo glyphRenderInfo) { }

        public virtual void Update(Stride.Games.GameTime time, DialogueTextGlyphRenderInfo glyphRenderInfo) { }

        public virtual void SetShaderData(ref TextEffectData textEffectData) { }

        public bool IsIndexInRange(int glyphIndex)
        {
            if (GlyphStartIndex <= glyphIndex && glyphIndex <= GlyphEndIndex)
            {
                return true;
            }
            return false;
        }
    }
}
