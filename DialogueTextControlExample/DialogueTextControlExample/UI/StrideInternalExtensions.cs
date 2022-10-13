using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Font;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DialogueTextControlExample.UI
{
    // Hacks required because Stride do not always expose the necessary things publicly.
    internal static class StrideInternalExtensions
    {
        private static TDelegate CreateGetFieldDelegate<TObject, TFieldValue, TDelegate>(string fieldName)
        {
            var instExp = Expression.Parameter(typeof(TObject));
            var fieldExp = Expression.Field(instExp, fieldName);
            return Expression.Lambda<TDelegate>(fieldExp, instExp).Compile();
        }

        public delegate Glyph GetGlyphMethod(SpriteFont spriteFont, CommandList commandList, char character, in Vector2 fontSize, bool uploadGpuResources, out Vector2 auxiliaryScaling);
        public delegate SwizzleMode GetSwizzleField(SpriteFont spriteFont);
        public delegate float GetBaseOffsetYMethod(SpriteFont spriteFont, float fontSize);
        public delegate Dictionary<int, float> GetKerningMapField(SpriteFont spriteFont);

        public static class StrideSpriteFont
        {
            private static GetGlyphMethod _getGlyphMethodDelegate;
            public static GetGlyphMethod GetGlyphMethod
            {
                get
                {
                    if (_getGlyphMethodDelegate == null)
                    {
                        var getGlyphMethodInfo = typeof(SpriteFont).GetMethod("GetGlyph", BindingFlags.Instance | BindingFlags.NonPublic);
                        _getGlyphMethodDelegate = (GetGlyphMethod)Delegate.CreateDelegate(typeof(GetGlyphMethod), getGlyphMethodInfo);
                    }
                    return _getGlyphMethodDelegate;
                }
            }

            private static GetSwizzleField _getSwizzleFieldDelegate;
            public static GetSwizzleField GetSwizzleField
            {
                get
                {
                    if (_getSwizzleFieldDelegate == null)
                    {
                        _getSwizzleFieldDelegate = CreateGetFieldDelegate<SpriteFont, SwizzleMode, GetSwizzleField>("swizzle");
                    }
                    return _getSwizzleFieldDelegate;
                }
            }

            private static GetBaseOffsetYMethod _getBaseOffsetYMethodDelegate;
            public static GetBaseOffsetYMethod GetBaseOffsetYMethod
            {
                get
                {
                    if (_getBaseOffsetYMethodDelegate == null)
                    {
                        var getBaseOffsetYMethodInfo = typeof(SpriteFont).GetMethod("GetBaseOffsetY", BindingFlags.Instance | BindingFlags.NonPublic);
                        _getBaseOffsetYMethodDelegate = (GetBaseOffsetYMethod)Delegate.CreateDelegate(typeof(GetBaseOffsetYMethod), getBaseOffsetYMethodInfo);
                    }
                    return _getBaseOffsetYMethodDelegate;
                }
            }

            private static GetKerningMapField _getKerningMapFieldDelegate;
            public static GetKerningMapField GetKerningMapField
            {
                get
                {
                    if (_getKerningMapFieldDelegate == null)
                    {
                        _getKerningMapFieldDelegate = CreateGetFieldDelegate<SpriteFont, Dictionary<int, float>, GetKerningMapField>("KerningMap");
                    }
                    return _getKerningMapFieldDelegate;
                }
            }
        }
    }
}
