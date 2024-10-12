using SceneEditorExtensionExample.Rendering.Materials;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Shaders;

namespace SceneEditorExtensionExample.Rendering.Materials.TextureParameters
{
    [DataContract]
    public class WavePanningNormalMap
    {
        public bool IsEnabled { get; set; } = true;
        /// <summary>
        /// Additional scaling of the normal map. Smaller value means less bumpiness.
        /// </summary>
        public float NormalMapStrength { get; set; } = 1f;
        /// <summary>
        /// Size of the normal map texture in world units.
        /// </summary>
        public float NormalMapWorldLength { get; set; } = 1f;
        public Vector2 PanDirection { get; set; } = new(1, 0);
        /// <summary>
        /// The speed of the texture panning in world units per second.
        /// </summary>
        public float PanSpeed { get; set; } = 1f;

        public virtual ShaderClassSource GenerateShaderSource()
        {
            var shaderGenerics = new string[]
            {
                IsEnabled.ToShaderString(),
                NormalMapStrength.ToShaderString(),
                NormalMapWorldLength.ToShaderString(),
                PanDirection.ToShaderString(),
                PanSpeed.ToShaderString()
            };
            var shaderClassSource = new ShaderClassSource("ComputeWaveNormalPanningUv", shaderGenerics);
            return shaderClassSource;
        }
    }
}
