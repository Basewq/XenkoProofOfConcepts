using DialogueTextControlExample.UI.Controls;
using DialogueTextControlExample.UI.Dialogue;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Renderers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DialogueTextControlExample.UI.Renderers
{
    internal class DialogueTextRenderer : ElementRenderer
    {
        private readonly UIFontBatch _uiFontBatch;

        private const int MaxShaderTextEffects = 32;
        private List<int> _shaderTextEffectIndices = new();
        private Buffer<TextEffectData> _shaderTextEffectsDataBuffer;
        private TextEffectData[] _shaderTextEffectsDataArray;

        public DialogueTextRenderer(IServiceRegistry services)
            : base(services)
        {
            var effectSystem = services.GetSafeServiceAs<EffectSystem>();

            var defaultEffect = StrideInternalExtensions.StrideUIBatch.GetDefaultEffectField(Batch);
            var defaultEffectSRgb = StrideInternalExtensions.StrideUIBatch.GetDefaultEffectSRgbField(Batch);
            var defaultEffectByteCode = defaultEffect.Effect.Bytecode;
            var defaultEffectSRgbByteCode = defaultEffectSRgb.Effect.Bytecode;
            _uiFontBatch = new UIFontBatch(GraphicsDevice, effectSystem, defaultEffectByteCode, defaultEffectSRgbByteCode);

            _shaderTextEffectsDataBuffer = Stride.Graphics.Buffer.New<TextEffectData>(GraphicsDevice, MaxShaderTextEffects, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
            _shaderTextEffectsDataArray = new TextEffectData[MaxShaderTextEffects];
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var textControl = (DialogueText)element;
            if (textControl.Font == null || textControl.TextGlyphRenderInfos.Count == 0)
            {
                return;
            }

            DrawText(textControl, context);
        }

        private void DrawText(DialogueText textControl, UIRenderingContext context)
        {
            // Note the majority of this code is adapted from Stride's existing code
            // TextBlock.cs, UIBatch.cs & SpriteFont.cs
            // The biggest difference is that TextBlock/SpriteFont takes some (internal) shortcuts
            // and calculates things World-View-Projection space, whereas we must work in World space

            var spriteFont = textControl.Font;
            var commandList = context.GraphicsContext.CommandList;
            var layoutingContext = textControl.GetLayoutingContext();

            var drawCommand = new UIDrawCommand
            {
                RenderOpacity = textControl.RenderOpacity,
                DepthBias = context.DepthBias,
                RealVirtualResolutionRatio = layoutingContext.RealVirtualResolutionRatio,
                RequestedFontSizeVirtual = textControl.ActualTextSize,
                //Batch = Batch,
                SnapText = context.ShouldSnapText && !textControl.DoNotSnapText,
                //WorldMatrix = textControl.WorldMatrix,    // Calculated down below
                Alignment = textControl.TextAlignment,      // Not implemented, refer to SpriteFont.ForEachGlyph<T> to adapt the rest!
                TextBoxSize = new Vector2(textControl.ActualWidth, textControl.ActualHeight),
                Swizzle = StrideInternalExtensions.StrideSpriteFont.GetSwizzleField(spriteFont)
            };

            // shift the string position so that it is written from the left/top corner of the element
            var textBoxSize = new Vector2(textControl.ActualWidth, textControl.ActualHeight);
            var leftTopCornerOffset = textBoxSize / 2;

            var worldMatrix = textControl.WorldMatrix;
            worldMatrix.M41 -= worldMatrix.M11 * leftTopCornerOffset.X + worldMatrix.M21 * leftTopCornerOffset.Y;
            worldMatrix.M42 -= worldMatrix.M12 * leftTopCornerOffset.X + worldMatrix.M22 * leftTopCornerOffset.Y;
            worldMatrix.M43 -= worldMatrix.M13 * leftTopCornerOffset.X + worldMatrix.M23 * leftTopCornerOffset.Y;
            drawCommand.WorldMatrix = worldMatrix;

            switch (spriteFont.FontType)
            {
                case SpriteFontType.SDF:
                    {
                        drawCommand.SnapText = false;
                        float scaling = textControl.ActualTextSize / spriteFont.Size;
                        drawCommand.RealVirtualResolutionRatio = 1 / new Vector2(scaling, scaling);
                    }
                    break;
                case SpriteFontType.Static:
                    {
                        drawCommand.RealVirtualResolutionRatio = Vector2.One; // ensure that static font are not scaled internally
                    }
                    break;
                case SpriteFontType.Dynamic:
                    {
                        // Dynamic: if we're not displaying in a situation where we can snap text, we're probably in 3D.
                        // Let's use virtual resolution (otherwise requested size might change on every camera move)
                        // TODO: some step function to have LOD without regenerating on every small change?
                        if (!drawCommand.SnapText)
                        {
                            drawCommand.RealVirtualResolutionRatio = Vector2.One;
                        }
                    }
                    break;
            }

            var textGlyphRenderInfos = textControl.TextGlyphRenderInfos;

            // We don't want to have letters with non uniform ratio
            var fontSizeReal = new Vector2(textControl.ActualTextSize * drawCommand.RealVirtualResolutionRatio.Y);
            // Round due to low float precision
            fontSizeReal.X = MathF.Round(fontSizeReal.X, 4);
            fontSizeReal.Y = MathF.Round(fontSizeReal.Y, 4);

            var defaultTextColor = textControl.RenderOpacity * textControl.TextColor;
            float horizontalExtraSpacing = spriteFont.GetExtraSpacing(fontSizeReal.X);
            float lineSpacingDistance = spriteFont.GetTotalLineSpacing(fontSizeReal.Y);
            int kerningKey = 0;
            float posX = 0;
            float posY = 0;
            int curLineIndex = 0;
            for (int i = 0; i < textGlyphRenderInfos.Count; i++)
            {
                var textGlyphRenderInfo = textGlyphRenderInfos[i];
                if (textGlyphRenderInfo.IgnoreRender)
                {
                    continue;
                }
                var textGlyph = textGlyphRenderInfo.TextGlyph;
                if (curLineIndex != textGlyphRenderInfo.LineIndex)
                {
                    posY += lineSpacingDistance;
                    posX = 0;
                    curLineIndex = textGlyphRenderInfo.LineIndex;
                }

                char ch = textGlyph.Character;
                const bool uploadGpuResources = true;
                textGlyphRenderInfo.SpriteFont ??= spriteFont;  // Ensure sprite font is set
                var glyphSpriteFont = textGlyphRenderInfo.SpriteFont ?? spriteFont;
                var glyph = StrideInternalExtensions.StrideSpriteFont.GetGlyphMethod(glyphSpriteFont, commandList, ch, fontSizeReal, uploadGpuResources, out Vector2 auxiliaryScaling);
                if (glyph == null && !glyphSpriteFont.IgnoreUnkownCharacters && glyphSpriteFont.DefaultCharacter.HasValue)
                {
                    glyph = StrideInternalExtensions.StrideSpriteFont.GetGlyphMethod(glyphSpriteFont, commandList, glyphSpriteFont.DefaultCharacter.Value, fontSizeReal, uploadGpuResources, out auxiliaryScaling);
                }
                if (glyph != null)
                {
                    var dx = glyph.Offset.X;

                    var kerningMap = StrideInternalExtensions.StrideSpriteFont.GetKerningMapField(glyphSpriteFont);
                    if (kerningMap != null && kerningMap.TryGetValue(kerningKey, out float kerningOffset))
                    {
                        dx += kerningOffset;
                    }

                    float nextX = posX + (glyph.XAdvance + horizontalExtraSpacing) * auxiliaryScaling.X;
                    float drawPosX = posX + dx * auxiliaryScaling.X;
                    float drawPosY = posY;
                    textGlyphRenderInfo.PositionX = drawPosX;
                    textGlyphRenderInfo.PositionY = drawPosY;
                    textGlyphRenderInfo.AuxiliaryScaling = auxiliaryScaling;
                    textGlyphRenderInfo.SpriteFontGlyph = glyph;
                    posX = nextX;
                }

                // Shift the kerning key
                kerningKey = (kerningKey << 16);
            }
            textControl.UpdateTextEffects(context.Time);

            _shaderTextEffectIndices.Clear();
            var textEffects = textControl.TextEffects;
            if (textEffects != null)
            {
                for (int i = 0; i < textEffects.Count; i++)
                {
                    var fx = textEffects[i];
                    if (fx.IsShaderEffect)
                    {
                        _shaderTextEffectIndices.Add(i);
                    }
                }
            }

            bool isFirstBatchBeginCall = true;
            bool hasCalledBatchBegin = false;
            EffectInstance currentEffectInstance = null;
            for (int glyphIndex = 0; glyphIndex < textGlyphRenderInfos.Count; glyphIndex++)
            {
                var textGlyphRenderInfo = textGlyphRenderInfos[glyphIndex];
                if (textGlyphRenderInfo.IsVisible && !textGlyphRenderInfo.IsWhitespace)
                {
                    var fontEffectInstance = _uiFontBatch.GetFontEffectInstance(textGlyphRenderInfo.SpriteFont);
                    if (currentEffectInstance != fontEffectInstance)
                    {
                        if (isFirstBatchBeginCall)
                        {
                            // Always end the default batch to prevent conflicts with our UIFontBatch.
                            Batch.End();
                            isFirstBatchBeginCall = false;
                        }
                        if (hasCalledBatchBegin)
                        {
                            _uiFontBatch.End();
                        }
                        currentEffectInstance = fontEffectInstance;
                        CallBatchBeginAndDoEffectSetUp(context, commandList, textControl, fontSizeReal.Y, lineSpacingDistance, currentEffectInstance);
                        hasCalledBatchBegin = true;
                    }
                    UpdateCommonRealtimeEffectParameters(context, currentEffectInstance);

                    DrawGlyph(ref drawCommand, textGlyphRenderInfo, fontSizeReal);
                }
            }

            if (hasCalledBatchBegin)
            {
                _uiFontBatch.End();
                // Reenable the default batch.
                Batch.BeginCustom(context.GraphicsContext, 0);
            }
        }

        private void UpdateCommonRealtimeEffectParameters(UIRenderingContext context, EffectInstance effectInstance)
        {
            effectInstance.Parameters.Set(TextFontShaderSharedKeys.GameTotalTimeSeconds, (float)context.Time.Total.TotalSeconds);
        }

        private void CallBatchBeginAndDoEffectSetUp(UIRenderingContext context, CommandList commandList, DialogueText textControl,
            float fontSizeRealY, float lineSpacingDistance,
            EffectInstance effectInstance)
        {
            var graphicsContext = context.GraphicsContext;
            // We need to get the batch settings from the original UIBatch since it holds the main UI rendering information
            // set by the UIRenderFeature
            BlendStateDescription? currentBlendState = StrideInternalExtensions.StrideUIBatch.GetCurrentBlendStateField(Batch);
            SamplerState currentSamplerState = StrideInternalExtensions.StrideUIBatch.GetCurrentSamplerStateField(Batch);
            RasterizerStateDescription? currentRasterizerState = StrideInternalExtensions.StrideUIBatch.GetCurrentRasterizerStateField(Batch);
            DepthStencilStateDescription? currentDepthStencilState = StrideInternalExtensions.StrideUIBatch.GetCurrentDepthStencilStateField(Batch);
            int currentStencilValue = StrideInternalExtensions.StrideUIBatch.GetCurrentStencilValueField(Batch);
            Matrix viewProjection = StrideInternalExtensions.StrideUIBatch.GetViewProjectionMatrixField(Batch);

            _uiFontBatch.BeginFontDraw(graphicsContext, effectInstance, ref viewProjection,
                currentBlendState, currentSamplerState, currentDepthStencilState, currentRasterizerState, currentStencilValue);

            effectInstance.Parameters.Set(TextFontShaderSharedKeys.RealFontSizeY, fontSizeRealY);
            //effectInstance.Parameters.Set(TextFontShaderBaseKeys.LineSpacingDistance, lineSpacingDistance);

            var textEffects = textControl.TextEffects;
            if (textEffects != null)
            {
                Debug.Assert(_shaderTextEffectIndices.Count <= MaxShaderTextEffects, "Too many shader text effects - to increase the const if needed");

                if (_shaderTextEffectIndices.Count > 0)
                {
                    var textEffectsBuffer = effectInstance.Parameters.Get(TextFontShaderSharedKeys.TextEffects);
                    if (textEffectsBuffer == null)
                    {
                        effectInstance.Parameters.Set(TextFontShaderSharedKeys.TextEffects, _shaderTextEffectsDataBuffer);
                        textEffectsBuffer = _shaderTextEffectsDataBuffer;
                    }

                    for (int i = 0; i < _shaderTextEffectIndices.Count; i++)
                    {
                        int shaderTextEffectIndex = _shaderTextEffectIndices[i];
                        var fx = textEffects[shaderTextEffectIndex];
                        ref var textEffectsData = ref _shaderTextEffectsDataArray[i];
                        textEffectsData.GlyphStartIndex = fx.GlyphStartIndex;
                        textEffectsData.GlyphEndIndex = fx.GlyphEndIndex;
                        fx.SetShaderData(ref textEffectsData);
                    }
                    textEffectsBuffer.SetData(commandList, _shaderTextEffectsDataArray);

                }
                effectInstance.Parameters.Set(TextFontShaderSharedKeys.TextEffectCount, _shaderTextEffectIndices.Count);
            }
        }

        private void DrawGlyph(
            ref UIDrawCommand drawCommand, DialogueTextGlyphRenderInfo textGlyphRenderInfo,
            Vector2 requestedFontSizeReal)
        {
            var spriteFont = textGlyphRenderInfo.SpriteFont;
            var glyph = textGlyphRenderInfo.SpriteFontGlyph;
            var texture = spriteFont.Textures[glyph.BitmapIndex];
            RectangleF sourceRectangle = glyph.Subrect;
            Vector4 borderSize = default;   // Unused
            Color textColor = textGlyphRenderInfo.TextColor * drawCommand.RenderOpacity;
            int depthBias = drawCommand.DepthBias;
            var imageOrientation = ImageOrientation.AsIs;
            var swizzle = drawCommand.Swizzle;
            bool snapImage = drawCommand.SnapText;

            var realVirtualResolutionRatio = requestedFontSizeReal / drawCommand.RequestedFontSizeVirtual;
            var auxiliaryScaling = textGlyphRenderInfo.AuxiliaryScaling;

            // Skip items with null size
            var elementSize = new Vector3(
                auxiliaryScaling.X * glyph.Subrect.Width / realVirtualResolutionRatio.X,
                auxiliaryScaling.Y * glyph.Subrect.Height / realVirtualResolutionRatio.Y,
                z: 1);
            if (elementSize.LengthSquared() < MathUtil.ZeroTolerance)
            {
                return;
            }

            float posX = textGlyphRenderInfo.PositionX + textGlyphRenderInfo.PositionOffsetX;
            float posY = textGlyphRenderInfo.PositionY + textGlyphRenderInfo.PositionOffsetY;

            float baseOffsetY = StrideInternalExtensions.StrideSpriteFont.GetBaseOffsetYMethod(spriteFont, requestedFontSizeReal.Y);
            float srcOffsetX = sourceRectangle.Width * 0.5f;
            float srcOffsetY = sourceRectangle.Height * 0.5f;
            float xShift = posX + srcOffsetX;
            float yShift = posY + (baseOffsetY + glyph.Offset.Y * auxiliaryScaling.Y) + srcOffsetY;

            var xScaledShift = xShift / realVirtualResolutionRatio.X;
            var yScaledShift = yShift / realVirtualResolutionRatio.Y;

            var worldMatrix = drawCommand.WorldMatrix;

            worldMatrix.M41 += worldMatrix.M11 * xScaledShift + worldMatrix.M21 * yScaledShift;
            worldMatrix.M42 += worldMatrix.M12 * xScaledShift + worldMatrix.M22 * yScaledShift;
            worldMatrix.M43 += worldMatrix.M13 * xScaledShift + worldMatrix.M23 * yScaledShift;
            worldMatrix.M44 += worldMatrix.M14 * xScaledShift + worldMatrix.M24 * yScaledShift;

            _uiFontBatch.DrawImage(
                textGlyphRenderInfo.TextGlyph.GlyphIndex,
                texture, ref worldMatrix, ref sourceRectangle, ref elementSize, ref borderSize,
                ref textColor, depthBias, imageOrientation, swizzle, snapImage);
        }

        private struct UIDrawCommand
        {
            public float RenderOpacity;
            public int DepthBias;
            public Vector2 RealVirtualResolutionRatio;
            /// <summary>
            /// Font size in virtual resolution.
            /// </summary>
            public float RequestedFontSizeVirtual;
            //public UIBatch Batch;
            public bool SnapText;
            public Matrix WorldMatrix;
            public TextAlignment Alignment;
            public Vector2 TextBoxSize;
            public SwizzleMode Swizzle;
        }
    }
}
