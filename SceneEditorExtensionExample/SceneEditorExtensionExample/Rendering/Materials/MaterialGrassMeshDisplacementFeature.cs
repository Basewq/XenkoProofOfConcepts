using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace SceneEditorExtensionExample.Rendering.Materials;

[DataContract]
[Display("Grass Mesh Displacement")]
public class MaterialGrassMeshDisplacementFeature : MaterialFeature, IMaterialDisplacementFeature
{
    public Texture WindNoiseMap { get; set; }

    public float AmbientWindNoiseMapWorldLength { get; set; } = 5f;
    public float AmbientWindMaxDisplacementY { get; set; } = 0.1f;

    public float ActiveWindNoiseMapWorldLength { get; set; } = 10f;
    public float ActiveWindMaxDisplacementXZ { get; set; } = 0.75f;

    public override void GenerateShader(MaterialGeneratorContext context)
    {
        if (WindNoiseMap is not null)
        {
            context.Parameters.Set(MaterialGrassMeshDisplacementKeys.WindNoiseMap, WindNoiseMap);
        }
        context.Parameters.Set(MaterialGrassMeshDisplacementKeys.AmbientWindNoiseMapWorldLength, AmbientWindNoiseMapWorldLength);
        context.Parameters.Set(MaterialGrassMeshDisplacementKeys.AmbientWindMaxDisplacementY, AmbientWindMaxDisplacementY);

        context.Parameters.Set(MaterialGrassMeshDisplacementKeys.ActiveWindNoiseMapWorldLength, ActiveWindNoiseMapWorldLength);
        context.Parameters.Set(MaterialGrassMeshDisplacementKeys.ActiveWindMaxDisplacementXZ, ActiveWindMaxDisplacementXZ);

        var mixin = new ShaderMixinSource();
        mixin.Mixins.Add(new ShaderClassSource("MaterialGrassMeshDisplacement"));

        context.AddShaderSource(MaterialShaderStage.Vertex, mixin);
    }
}
