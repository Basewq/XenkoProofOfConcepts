using DialogueTextControlExample.UI.Dialogue;
using DialogueTextControlExample.UI.Dialogue.TextEffects;
using Stride.Core.Mathematics;

namespace DialogueTextControlExample.Tests
{
    public class DialogueTextParserTests
    {
        [Fact]
        public void ParseTestNormal()
        {
            string expectedTextToDisplay = "This is a bold and italic text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("bold", "<b>bold</b>")
                                    .Replace("italic", "<i>italic</i>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
        }

        [Fact]
        public void ParseTestMultipleTags()
        {
            string expectedTextToDisplay = "This is a bold and italic text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("bold and italic", "<b>bold and italic</b>")
                                    .Replace("bold and italic", "<i>bold and italic</i>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
        }

        [Fact]
        public void ParseTestPartialOverlapTags()
        {
            string expectedTextToDisplay = "This is a bold and italic text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("bold and italic", "<b>bold and italic</b>")
                                    .Replace("italic</b> text", "<i>italic</b> text</i>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
        }

        [Fact]
        public void ParseTestImplicitAttributeValue()
        {
            string expectedTextToDisplay = "This is a red text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("red", "<color=\"red\">red</color>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
            var colorTextEffect = builder.TextEffects.FirstOrDefault(x => x is ColorTextEffect) as ColorTextEffect;
            Assert.NotNull(colorTextEffect);
            Assert.Equal(Color.Red, colorTextEffect?.TextColor ?? default);
        }

        [Fact]
        public void ParseTestImplicitAndExplicitAttributeValues()
        {
            string expectedTextToDisplay = "This is paused text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("paused", "<pause=0.5 skip1=false>paused</pause>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
            var pauseTextEffect = builder.TextEffects.FirstOrDefault(x => x is PauseTextEffect) as PauseTextEffect;
            Assert.NotNull(pauseTextEffect);
            Assert.Equal(0.5f, pauseTextEffect?.DelayInSeconds ?? float.NaN);
            Assert.False(pauseTextEffect?.SkipDelayOnFirstChar);
        }

        [Fact]
        public void ParseTestExplicitAttributeValueSingle()
        {
            float expectedAmplitude = 2.5f;
            string expectedTextToDisplay = "This is a wave text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("wave", $"<wave amp=2.5>wave</wave>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
            var waveTextEffect = builder.TextEffects.FirstOrDefault(x => x is WaveTextEffect) as WaveTextEffect;
            Assert.NotNull(waveTextEffect);
            Assert.Equal(expectedAmplitude, waveTextEffect?.Amplitude ?? float.NaN);
        }

        [Fact]
        public void ParseTestExplicitAttributeValueMultiple()
        {
            float expectedAmplitude = 2.5f;
            float expectedFrequency = 3.0f;
            string expectedTextToDisplay = "This is a wave text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("wave", $"<wave amp=2.5 freq=3>wave</wave>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
            var waveTextEffect = builder.TextEffects.FirstOrDefault(x => x is WaveTextEffect) as WaveTextEffect;
            Assert.NotNull(waveTextEffect);
            Assert.Equal(expectedAmplitude, waveTextEffect?.Amplitude ?? float.NaN);
            Assert.Equal(expectedFrequency, waveTextEffect?.Frequency ?? float.NaN);
        }

        [Fact]
        public void ParseTestExplicitAttributeValueMultipleV2()
        {
            float expectedAmplitude = 2.5f;
            float expectedFrequency = 3.0f;
            string expectedTextToDisplay = "This is a wave text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("wave", $"<wave amp=2.5 dir=left freq=3>wave</wave>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
            var waveTextEffect = builder.TextEffects.FirstOrDefault(x => x is WaveTextEffect) as WaveTextEffect;
            Assert.NotNull(waveTextEffect);
            Assert.Equal(expectedAmplitude, waveTextEffect?.Amplitude ?? float.NaN);
            Assert.Equal(expectedFrequency, waveTextEffect?.Frequency ?? float.NaN);
        }

        [Fact]
        public void ParseTestExplicitAttributeValueMultipleWithQuotedValues()
        {
            float expectedAmplitude = 2.5f;
            float expectedFrequency = 3.0f;
            string expectedTextToDisplay = "This is a wave text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("wave", $"<wave amp=\"2.5\" freq=\"3\">wave</wave>");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
            var waveTextEffect = builder.TextEffects.FirstOrDefault(x => x is WaveTextEffect) as WaveTextEffect;
            Assert.NotNull(waveTextEffect);
            Assert.Equal(expectedAmplitude, waveTextEffect?.Amplitude ?? float.NaN);
            Assert.Equal(expectedFrequency, waveTextEffect?.Frequency ?? float.NaN);
        }

        [Fact]
        public void ParseTestExplicitAttributeValueMultipleSelfClosing()
        {
            float expectedAmplitude = 2.5f;
            float expectedFrequency = 3.0f;
            string expectedTextToDisplay = "This is a wave text";
            string dialogueText = expectedTextToDisplay
                                    .Replace("wave", $"<wave amp=2.5 freq=3/>wave");

            var parser = new DialogueTextParser();
            var builder = parser.Parse(dialogueText);
            var displayText = builder.GetDisplayText();
            Assert.Equal(expectedTextToDisplay, displayText);
            var waveTextEffect = builder.TextEffects.FirstOrDefault(x => x is WaveTextEffect) as WaveTextEffect;
            Assert.NotNull(waveTextEffect);
            Assert.Equal(expectedAmplitude, waveTextEffect?.Amplitude ?? float.NaN);
            Assert.Equal(expectedFrequency, waveTextEffect?.Frequency ?? float.NaN);
        }

    }
}
