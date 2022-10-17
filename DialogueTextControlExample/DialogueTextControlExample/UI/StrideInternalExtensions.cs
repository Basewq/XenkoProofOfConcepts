using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DialogueTextControlExample.UI
{
    // Hacks required because Stride do not always expose the necessary things publicly.
    internal static class StrideInternalExtensions
    {
        private static TDelegate CreateGetPropertyOrFieldDelegate<TObject, TDelegate>(string fieldName)
        {
            var objExp = Expression.Parameter(typeof(TObject));
            var fieldExp = Expression.PropertyOrField(objExp, fieldName);   // PropertyOrField is only allowed for instance objects
            return Expression.Lambda<TDelegate>(fieldExp, objExp).Compile();
        }

        private static TDelegate CreateGetStaticFieldDelegate<TObject, TDelegate>(string fieldName)
        {
            var objExp = Expression.Parameter(typeof(TObject));
            var fieldExp = Expression.Field(null, typeof(TObject), fieldName);
            return Expression.Lambda<TDelegate>(fieldExp, objExp).Compile();
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
                    _getSwizzleFieldDelegate ??= CreateGetPropertyOrFieldDelegate<SpriteFont, GetSwizzleField>("swizzle");
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
                    _getKerningMapFieldDelegate ??= CreateGetPropertyOrFieldDelegate<SpriteFont, GetKerningMapField>("KerningMap");
                    return _getKerningMapFieldDelegate;
                }
            }
        }

        public delegate EffectInstance GetEffectInstanceField(UIBatch batch);
        public delegate BlendStateDescription? GetCurrentBlendStateField(UIBatch batch);
        public delegate DepthStencilStateDescription? GetCurrentDepthStencilStateField(UIBatch batch);
        public delegate RasterizerStateDescription? GetCurrentRasterizerStateField(UIBatch batch);
        public delegate SamplerState GetCurrentSamplerStateField(UIBatch batch);
        public delegate int GetCurrentStencilValueField(UIBatch batch);
        public delegate Matrix GetViewProjectionMatrixField(UIBatch batch);
        public delegate short[][] GetPrimitiveTypeToIndicesStaticField(UIBatch batch = null);
        public static class StrideUIBatch
        {
            private static GetEffectInstanceField _getDefaultEffectFieldDelegate;
            public static GetEffectInstanceField GetDefaultEffectField
            {
                get
                {
                    _getDefaultEffectFieldDelegate ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetEffectInstanceField>("DefaultEffect");
                    return _getDefaultEffectFieldDelegate;
                }
            }

            private static GetEffectInstanceField _getDefaultEffectSRgbFieldDelegate;
            public static GetEffectInstanceField GetDefaultEffectSRgbField
            {
                get
                {
                    _getDefaultEffectSRgbFieldDelegate ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetEffectInstanceField>("DefaultEffectSRgb");
                    return _getDefaultEffectSRgbFieldDelegate;
                }
            }

            private static GetCurrentBlendStateField _getCurrentBlendStateFieldDelegate;
            public static GetCurrentBlendStateField GetCurrentBlendStateField
            {
                get
                {
                    _getCurrentBlendStateFieldDelegate ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetCurrentBlendStateField>("currentBlendState");
                    return _getCurrentBlendStateFieldDelegate;
                }
            }

            private static GetCurrentDepthStencilStateField _getCurrentDepthStencilStateFieldDelegate;
            public static GetCurrentDepthStencilStateField GetCurrentDepthStencilStateField
            {
                get
                {
                    _getCurrentDepthStencilStateFieldDelegate ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetCurrentDepthStencilStateField>("currentDepthStencilState");
                    return _getCurrentDepthStencilStateFieldDelegate;
                }
            }

            private static GetCurrentRasterizerStateField _getCurrentRasterizerStateFieldDelegate;
            public static GetCurrentRasterizerStateField GetCurrentRasterizerStateField
            {
                get
                {
                    _getCurrentRasterizerStateFieldDelegate ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetCurrentRasterizerStateField>("currentRasterizerState");
                    return _getCurrentRasterizerStateFieldDelegate;
                }
            }

            private static GetCurrentSamplerStateField _getCurrentSamplerStateFieldDelegate;
            public static GetCurrentSamplerStateField GetCurrentSamplerStateField
            {
                get
                {
                    _getCurrentSamplerStateFieldDelegate ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetCurrentSamplerStateField>("currentSamplerState");
                    return _getCurrentSamplerStateFieldDelegate;
                }
            }

            private static GetCurrentStencilValueField _getCurrentStencilValueFieldDelegate;
            public static GetCurrentStencilValueField GetCurrentStencilValueField
            {
                get
                {
                    _getCurrentStencilValueFieldDelegate ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetCurrentStencilValueField>("currentStencilValue");
                    return _getCurrentStencilValueFieldDelegate;
                }
            }

            private static GetViewProjectionMatrixField _getViewProjectionMatrixField;
            public static GetViewProjectionMatrixField GetViewProjectionMatrixField
            {
                get
                {
                    _getViewProjectionMatrixField ??= CreateGetPropertyOrFieldDelegate<UIBatch, GetViewProjectionMatrixField>("viewProjectionMatrix");
                    return _getViewProjectionMatrixField;
                }
            }

            private static GetPrimitiveTypeToIndicesStaticField _getPrimitiveTypeToIndicesStaticFieldDelegate;
            public static GetPrimitiveTypeToIndicesStaticField GetPrimitiveTypeToIndicesStaticField
            {
                get
                {
                    _getPrimitiveTypeToIndicesStaticFieldDelegate ??= CreateGetStaticFieldDelegate<UIBatch, GetPrimitiveTypeToIndicesStaticField>("PrimitiveTypeToIndices");
                    return _getPrimitiveTypeToIndicesStaticFieldDelegate;
                }
            }
        }
    }
}
