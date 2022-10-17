namespace DialogueTextControlExample.UI.Renderers
{
    // IMPORTANT: If you change this, make sure it matches Effects/TextFontShaderShared.sdsl
    public struct TextEffectData
    {
        public int GlyphStartIndex;
        public int GlyphEndIndex;

        public ShaderTextEffectType ShaderTextEffectType;

        public float HeatWaveAmplitude;
        public float HeatWaveFrequency;
        public float HeatWavePeriodScale;
    }

    public enum ShaderTextEffectType
    {
        NotSet = 0,
        HeatWave = 1
    }
}
