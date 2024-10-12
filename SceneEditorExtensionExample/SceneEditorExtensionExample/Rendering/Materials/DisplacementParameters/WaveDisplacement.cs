using SceneEditorExtensionExample.Rendering.Materials;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Shaders;

namespace SceneEditorExtensionExample.Rendering.Materials.DisplacementParameters
{
    [DataContract(Inherited = true)]
    public abstract class WaveDisplacementBase
    {
        public bool IsEnabled { get; set; } = true;
        public float WaveLength { get; set; } = 1f;
        public Vector3 WaveDirection { get; set; } = new(1, 0, 0);
        public float WaveSpeedScale { get; set; } = 1f;
        public float WaveHeight { get; set; } = 1f;
        public float WaveSteepness { get; set; } = 1f;

        public abstract ShaderClassSource GenerateShaderSource();
    }

    public class GerstnerWave : WaveDisplacementBase
    {
        public override ShaderClassSource GenerateShaderSource()
        {
            var shaderGenerics = new string[]
            {
                IsEnabled.ToShaderString(),
                WaveLength.ToShaderString(),
                WaveDirection.ToShaderString(),
                WaveSpeedScale.ToShaderString(),
                WaveHeight.ToShaderString(),
                WaveSteepness.ToShaderString()
            };
            var shaderClassSource = new ShaderClassSource("ComputeGerstnerWave", shaderGenerics);
            return shaderClassSource;
        }
    }
}
