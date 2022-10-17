// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Graphics;
using System;
using System.Runtime.InteropServices;

namespace DialogueTextControlExample.UI
{
    // The original code from VertexPositionColorTextureSwizzle.cs
    // GylphIndex field added and remove unused ColorAdd
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextFontVertex : IEquatable<TextFontVertex>, IVertex
    {
        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector4 Position;

        /// <summary>
        /// The vertex color.
        /// </summary>
        public Color4 TextColor;

        /// <summary>
        /// The vertex color.
        /// </summary>
        //public Color4 ColorAdd;

        /// <summary>
        /// UV texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate;

        /// <summary>
        /// The Swizzle mode.
        /// </summary>
        public float Swizzle;

        public int GlyphIndex;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new(
            VertexElement.Position<Vector4>(),
            VertexElement.Color<Color4>(0),
            //VertexElement.Color<Color4>(1),
            VertexElement.TextureCoordinate<Vector2>(),
            new VertexElement("BATCH_SWIZZLE", PixelFormat.R32_Float),
            new VertexElement("BATCH_GLYPH_INDEX", PixelFormat.R32_SInt));      // Struct size must remain multiple of 4 as required by the shader

        public bool Equals(TextFontVertex other)
        {
            return Position.Equals(other.Position)
                && TextColor.Equals(other.TextColor)
                //&& ColorAdd.Equals(other.ColorAdd)
                && TextureCoordinate.Equals(other.TextureCoordinate)
                && Swizzle.Equals(other.Swizzle)
                && GlyphIndex.Equals(other.GlyphIndex);
        }

        public override bool Equals(object obj)
        {
            return obj is TextFontVertex other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hashCode = HashCode.Combine(
                Position,
                TextColor,
                //ColorAdd,
                TextureCoordinate,
                Swizzle,
                GlyphIndex
            );
            return hashCode;
        }

        public static bool operator ==(TextFontVertex left, TextFontVertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextFontVertex left, TextFontVertex right)
        {
            return !left.Equals(right);
        }

        public override string ToString() =>
            $"Position: {Position}, TextColor: {TextColor}, Texcoord: {TextureCoordinate}, Swizzle: {Swizzle}, GlyphIndex: {GlyphIndex}";

        public readonly VertexDeclaration GetLayout() => Layout;

        public void FlipWinding()
        {
            TextureCoordinate.X = (1.0f - TextureCoordinate.X);
        }
    }
}
