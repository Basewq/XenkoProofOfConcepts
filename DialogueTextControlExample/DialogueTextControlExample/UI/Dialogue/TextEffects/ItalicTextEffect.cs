namespace DialogueTextControlExample.UI.Dialogue.TextEffects
{
    public class ItalicTextEffect : DialogueTextEffectBase
    {
        public const string TagName = "i";

        public override void Initialize(DialogueTextGlyphRenderInfo glyphRenderInfo)
        {
            glyphRenderInfo.IsItalic = true;
            var spriteFont = glyphRenderInfo.TextControl?.GetFont(glyphRenderInfo.IsBold, glyphRenderInfo.IsItalic);
            glyphRenderInfo.SpriteFont = spriteFont;
        }
    }
}
