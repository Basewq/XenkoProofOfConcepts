﻿// <auto-generated>
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

namespace DialogueTextControlExample.UI.Renderers
{
    public static partial class TextFontShaderSharedKeys
    {
        public static readonly ValueParameterKey<float> GameTotalTimeSeconds = ParameterKeys.NewValue<float>();
        public static readonly ValueParameterKey<float> RealFontSizeY = ParameterKeys.NewValue<float>();
        public static readonly ObjectParameterKey<Buffer> TextEffects = ParameterKeys.NewObject<Buffer>();
        public static readonly ValueParameterKey<int> TextEffectCount = ParameterKeys.NewValue<int>();
    }
}
