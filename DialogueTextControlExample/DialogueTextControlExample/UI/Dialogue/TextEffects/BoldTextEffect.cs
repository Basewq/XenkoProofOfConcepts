namespace DialogueTextControlExample.UI.Dialogue.TextEffects
{
    public class BoldTextEffect : DialogueTextEffectBase
    {
        public const string TagName = "b";

        public override void Initialize(DialogueTextGlyphRenderInfo glyphRenderInfo)
        {
            glyphRenderInfo.IsBold = true;
            var spriteFont = glyphRenderInfo.TextControl?.GetFont(glyphRenderInfo.IsBold, glyphRenderInfo.IsItalic);
            glyphRenderInfo.SpriteFont = spriteFont;
        }
    }
}
