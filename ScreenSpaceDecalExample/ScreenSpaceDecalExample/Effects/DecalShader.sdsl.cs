// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Stride Shader Mixin Code Generator.
// To generate it yourself, please install Stride.VisualStudio.Package .vsix
// and re-save the associated .sdfx.
// </auto-generated>

using System;
using Stride.Core;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Shaders;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering
{
    public static partial class DecalShaderKeys
    {
        public static readonly ObjectParameterKey<Texture> DecalTexture = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> TextureScale = ParameterKeys.NewValue<float>(1.0f);
        public static readonly ValueParameterKey<Color4> TintColor = ParameterKeys.NewValue<Color4>();
        public static readonly ValueParameterKey<uint> IgnoreRenderGroups = ParameterKeys.NewValue<uint>();
        public static readonly ValueParameterKey<bool> IsAffectedByShadow = ParameterKeys.NewValue<bool>();
        public static readonly ValueParameterKey<Vector3> ProjectorUpDirection = ParameterKeys.NewValue<Vector3>();
    }
}
