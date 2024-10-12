using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System.Collections.Generic;

namespace SceneEditorExtensionExample.Rendering.Materials;

[DataContract]
[Display("Grass Diffuse Map")]
public class MaterialGrassDiffuseMapFeature : MaterialFeature, IMaterialDiffuseFeature, IMaterialStreamProvider
{
    public bool IsBrightnessMapEnabled { get; set; } = true;
    public Texture BrightnessMap { get; set; }
    public float BrightnessMapWorldLength { get; set; } = 10;
    [DataMemberRange(minimum: 0, maximum: 1, smallStep: 0.01, largeStep: 0.1, decimalPlaces: 4)]
    public float BrightnessMapMinValue { get; set; } = 0.8f;
    [DataMemberRange(minimum: 0, maximum: 1, smallStep: 0.01, largeStep: 0.1, decimalPlaces: 4)]
    public float BrightnessMapMaxValue { get; set; } = 1f;
    [DataMemberRange(minimum: 0, maximum: 2, smallStep: 0.01, largeStep: 0.1, decimalPlaces: 4)]
    public bool IsBrightnessMapLevelsEnabled { get; set; } = true;
    public int BrightnessMapLevels { get; set; } = 4;
    public float GlobalBrightness { get; set; } = 1f;
    public Color3 TopColor { get; set; } = Color.White.ToColor3();
    public Color3 BottomColor { get; set; } = Color.White.ToColor3();

    public override void GenerateShader(MaterialGeneratorContext context)
    {
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.BrightnessMap, BrightnessMap);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.BrightnessMapWorldLength, BrightnessMapWorldLength);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.BrightnessMapMinValue, BrightnessMapMinValue);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.BrightnessMapMaxValue, BrightnessMapMaxValue);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.IsBrightnessMapLevelsEnabled, IsBrightnessMapLevelsEnabled);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.BrightnessMapLevels, BrightnessMapLevels);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.GlobalBrightness, GlobalBrightness);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.GrassTopColor, TopColor);
        context.Parameters.Set(MaterialGrassDiffuseMapKeys.GrassBottomColor, BottomColor);

        bool hasBrightnessMap = BrightnessMap is not null;
        var mixin = new ShaderMixinSource();
        var shaderGenerics = new string[]
        {
            IsBrightnessMapEnabled.ToShaderString(),
            hasBrightnessMap.ToShaderString(),
        };
        mixin.Mixins.Add(new ShaderClassSource("MaterialGrassDiffuseMap", shaderGenerics));

        context.UseStream(MaterialShaderStage.Pixel, MaterialDiffuseMapFeature.DiffuseStream.Stream);
        context.UseStream(MaterialShaderStage.Pixel, MaterialDiffuseMapFeature.ColorBaseStream.Stream);
        context.AddShaderSource(MaterialShaderStage.Pixel, mixin);
    }

    public IEnumerable<MaterialStreamDescriptor> GetStreams()
    {
        yield return MaterialDiffuseMapFeature.ColorBaseStream;
        yield return MaterialDiffuseMapFeature.DiffuseStream;
    }
}
